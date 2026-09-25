namespace Test.Benchmark
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Thin REST client for the AssistantHub routes the harness uses. Every call is timed. The harness never
    /// references AssistantHub assemblies, so request and response bodies are handled as JSON.
    /// </summary>
    public class AssistantHubClient
    {
        #region Public-Members

        /// <summary>
        /// Server base URL.
        /// </summary>
        public string BaseUrl { get; }

        /// <summary>
        /// Chat calls retried after a transient model failure.
        /// </summary>
        public long RetriedChatCalls
        {
            get
            {
                return Interlocked.Read(ref _RetriedChatCalls);
            }
        }

        #endregion

        #region Private-Members

        private long _RetriedChatCalls = 0;
        private readonly HttpClient _Http;
        private readonly string _Token;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="http">HTTP client.</param>
        /// <param name="baseUrl">Server URL, e.g. http://127.0.0.1:38800.</param>
        /// <param name="token">Bearer token of a tenant administrator.</param>
        public AssistantHubClient(HttpClient http, string baseUrl, string token)
        {
            _Http = http ?? throw new ArgumentNullException(nameof(http));
            BaseUrl = baseUrl.TrimEnd('/');
            _Token = token;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Send a request and time it.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="path">Path starting with /v1.0.</param>
        /// <param name="body">Optional JSON body.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Timed call.</returns>
        public async Task<TimedCall> SendAsync(HttpMethod method, string path, string? body, CancellationToken token)
        {
            TimedCall call = new TimedCall();
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                using HttpRequestMessage request = new HttpRequestMessage(method, BaseUrl + path);
                request.Headers.Add("Authorization", "Bearer " + _Token);
                if (body != null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await _Http.SendAsync(request, token).ConfigureAwait(false);
                call.Body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                call.StatusCode = (int)response.StatusCode;
            }
            catch (HttpRequestException e)
            {
                call.Error = e.Message;
            }
            catch (TaskCanceledException e) when (!token.IsCancellationRequested)
            {
                call.Error = "timeout: " + e.Message;
            }

            sw.Stop();
            call.ElapsedMs = sw.Elapsed.TotalMilliseconds;
            return call;
        }

        /// <summary>
        /// Send and parse a JSON response, throwing on failure.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="path">Path.</param>
        /// <param name="body">Optional body.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Parsed JSON.</returns>
        public async Task<JsonNode> SendJsonAsync(HttpMethod method, string path, JsonNode? body, CancellationToken token)
        {
            TimedCall call = await SendAsync(method, path, body?.ToJsonString(), token).ConfigureAwait(false);
            if (!call.IsSuccess) throw new InvalidOperationException(method + " " + path + " failed: " + call.Describe());
            return string.IsNullOrWhiteSpace(call.Body) ? new JsonObject() : JsonNode.Parse(call.Body) ?? new JsonObject();
        }

        /// <summary>
        /// Enumerate every object of a paginated list route.
        /// </summary>
        /// <param name="path">List path, optionally with a query string.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>All objects.</returns>
        public async Task<List<JsonNode>> ListAllAsync(string path, CancellationToken token)
        {
            List<JsonNode> all = new List<JsonNode>();
            string? continuation = null;
            for (int page = 0; page < 10000; page++)
            {
                string separator = path.Contains('?') ? "&" : "?";
                string url = path + separator + "maxResults=1000" + (continuation != null ? "&continuationToken=" + Uri.EscapeDataString(continuation) : string.Empty);
                JsonNode result = await SendJsonAsync(HttpMethod.Get, url, null, token).ConfigureAwait(false);
                if (result["Objects"] is JsonArray objects)
                {
                    foreach (JsonNode? item in objects)
                    {
                        if (item != null) all.Add(item.DeepClone());
                    }
                }

                continuation = result["ContinuationToken"]?.GetValue<string>();
                bool end = result["EndOfResults"]?.GetValue<bool>() ?? true;
                if (end || string.IsNullOrEmpty(continuation)) break;
            }

            return all;
        }

        /// <summary>
        /// Run the retrieval-only pipeline.
        /// </summary>
        /// <param name="assistantId">Assistant id.</param>
        /// <param name="body">Request body.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Timed call and parsed result (null on failure).</returns>
        public async Task<(TimedCall Call, RetrieveResult? Result)> RetrieveAsync(string assistantId, JsonObject body, CancellationToken token)
        {
            TimedCall call = await SendAsync(HttpMethod.Post, "/v1.0/assistants/" + assistantId + "/retrieve", body.ToJsonString(), token).ConfigureAwait(false);
            if (!call.IsSuccess) return (call, null);
            return (call, JsonSerializer.Deserialize<RetrieveResult>(call.Body));
        }

        /// <summary>
        /// Ask a non-streaming chat question.
        /// </summary>
        /// <param name="assistantId">Assistant id.</param>
        /// <param name="body">Chat completion request body.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Timed call and parsed result (null on failure).</returns>
        public async Task<(TimedCall Call, ChatResult? Result)> ChatAsync(string assistantId, JsonObject body, CancellationToken token)
        {
            // AssistantHub does not retry a transient failure of the answer model (a model proxy at capacity returns
            // 429, surfaced as 502), so the harness retries those to measure answers rather than proxy capacity.
            // RetriedChatCalls counts how often this happened.
            TimedCall call = null!;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                call = await SendAsync(HttpMethod.Post, "/v1.0/assistants/" + assistantId + "/chat", body.ToJsonString(), token).ConfigureAwait(false);
                bool transient = call.StatusCode == 429 || call.StatusCode == 503 || call.StatusCode == 504
                    || (call.StatusCode == 502 && (call.Body.Contains("returned 429") || call.Body.Contains("returned 502") || call.Body.Contains("returned 503") || call.Body.Contains("returned 504")));
                if (!transient) break;
                Interlocked.Increment(ref _RetriedChatCalls);
                await Task.Delay(5000 * (attempt + 1), token).ConfigureAwait(false);
            }

            if (!call.IsSuccess) return (call, null);
            return (call, ChatResult.Parse(call.Body));
        }

        #endregion
    }
}
