namespace Test.Benchmark.Stub
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// An in-process, Ollama-compatible stub model server for load tests. It answers <c>/api/embed</c>,
    /// <c>/api/embeddings</c>, <c>/api/chat</c>, <c>/api/generate</c>, <c>/api/tags</c> and <c>/api/show</c> with
    /// deterministic output after a fixed delay, so AssistantHub, Partio, DocumentAtom and RecallDB can be measured
    /// without the model. Embeddings are unit vectors seeded by a hash of the text, so identical text always embeds
    /// identically. Partio reaches it from its container through host.docker.internal.
    /// </summary>
    public class StubModelServer : IAsyncDisposable
    {
        #region Public-Members

        /// <summary>
        /// Default port.
        /// </summary>
        public const int DefaultPort = 38950;

        /// <summary>
        /// Stub embedding model name.
        /// </summary>
        public const string EmbeddingModel = "stub-embed";

        /// <summary>
        /// Stub completion model name.
        /// </summary>
        public const string CompletionModel = "stub-chat";

        /// <summary>
        /// URL the stub listens on.
        /// </summary>
        public string Url { get; }

        #endregion

        #region Private-Members

        private readonly int _Port;
        private readonly int _Dimensions;
        private readonly int _LatencyMs;
        private WebApplication? _App = null;
        private long _Requests = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="port">Port.</param>
        /// <param name="dimensions">Embedding dimensions.</param>
        /// <param name="latencyMs">Delay per request.</param>
        public StubModelServer(int port, int dimensions, int latencyMs)
        {
            _Port = port;
            _Dimensions = dimensions;
            _LatencyMs = Math.Max(0, latencyMs);
            Url = "http://0.0.0.0:" + port;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Requests served.
        /// </summary>
        public long Requests
        {
            get
            {
                return Interlocked.Read(ref _Requests);
            }
        }

        /// <summary>
        /// Start listening on all interfaces (so containers can reach it).
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task StartAsync(CancellationToken token)
        {
            WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls(Url);
            _App = builder.Build();

            _App.MapGet("/", () => "stub ok");
            _App.MapGet("/api/tags", () => Results.Json(new
            {
                models = new object[]
                {
                    new { name = EmbeddingModel, model = EmbeddingModel, digest = "stub", size = 0 },
                    new { name = CompletionModel, model = CompletionModel, digest = "stub", size = 0 }
                }
            }));
            _App.MapPost("/api/show", () => Results.Json(new { model_info = new Dictionary<string, object> { ["general.architecture"] = "stub", ["stub.embedding_length"] = _Dimensions } }));
            _App.MapPost("/api/embed", (Delegate)HandleEmbedAsync);
            _App.MapPost("/api/embeddings", (Delegate)HandleEmbeddingsAsync);
            _App.MapPost("/api/chat", (Delegate)HandleChatAsync);
            _App.MapPost("/api/generate", (Delegate)HandleGenerateAsync);
            await _App.StartAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        /// <returns>Task.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_App != null)
            {
                await _App.StopAsync().ConfigureAwait(false);
                await _App.DisposeAsync().ConfigureAwait(false);
                _App = null;
            }
        }

        #endregion

        #region Private-Methods

        private async Task<IResult> HandleEmbedAsync(HttpContext context)
        {
            JsonNode? body = await ReadAsync(context).ConfigureAwait(false);
            List<string> inputs = new List<string>();
            JsonNode? input = body?["input"];
            if (input is JsonArray array)
            {
                foreach (JsonNode? item in array) inputs.Add(item?.GetValue<string>() ?? string.Empty);
            }
            else
            {
                inputs.Add(input?.GetValue<string>() ?? string.Empty);
            }

            await DelayAsync(context.RequestAborted).ConfigureAwait(false);
            List<float[]> embeddings = new List<float[]>();
            foreach (string text in inputs) embeddings.Add(Embed(text));
            return Results.Json(new { model = EmbeddingModel, embeddings = embeddings, prompt_eval_count = inputs.Count });
        }

        private async Task<IResult> HandleEmbeddingsAsync(HttpContext context)
        {
            JsonNode? body = await ReadAsync(context).ConfigureAwait(false);
            await DelayAsync(context.RequestAborted).ConfigureAwait(false);
            return Results.Json(new { embedding = Embed(body?["prompt"]?.GetValue<string>() ?? string.Empty) });
        }

        private async Task<IResult> HandleChatAsync(HttpContext context)
        {
            await ReadAsync(context).ConfigureAwait(false);
            await DelayAsync(context.RequestAborted).ConfigureAwait(false);
            return Results.Json(new
            {
                model = CompletionModel,
                created_at = DateTime.UtcNow.ToString("o"),
                message = new { role = "assistant", content = "Stub answer." },
                done = true,
                done_reason = "stop",
                prompt_eval_count = 100,
                eval_count = 3
            });
        }

        private async Task<IResult> HandleGenerateAsync(HttpContext context)
        {
            await ReadAsync(context).ConfigureAwait(false);
            await DelayAsync(context.RequestAborted).ConfigureAwait(false);
            return Results.Json(new { model = CompletionModel, response = "Stub answer.", done = true, prompt_eval_count = 100, eval_count = 3 });
        }

        private async Task<JsonNode?> ReadAsync(HttpContext context)
        {
            Interlocked.Increment(ref _Requests);
            try
            {
                return await JsonNode.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private Task DelayAsync(CancellationToken token)
        {
            return _LatencyMs > 0 ? Task.Delay(_LatencyMs, token) : Task.CompletedTask;
        }

        private float[] Embed(string text)
        {
            byte[] seed = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            Random random = new Random(BitConverter.ToInt32(seed, 0));
            float[] vector = new float[_Dimensions];
            double norm = 0;
            for (int i = 0; i < _Dimensions; i++)
            {
                vector[i] = (float)(random.NextDouble() * 2 - 1);
                norm += vector[i] * vector[i];
            }

            float scale = (float)(1.0 / Math.Sqrt(norm));
            for (int i = 0; i < _Dimensions; i++) vector[i] *= scale;
            return vector;
        }

        #endregion
    }
}
