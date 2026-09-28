namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using WatsonWebserver.Core;
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;

    /// <summary>
    /// Routes for the cross-encoder rerank services configured in server settings.
    /// </summary>
    public class RerankerHandler : HandlerBase
    {
        private static readonly string _Header = "[RerankerHandler] ";
        private readonly CrossEncoderRerankClient _RerankClient;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RerankerHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            IObjectStorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference,
            CrossEncoderRerankClient rerankClient = null)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference)
        {
            _RerankClient = rerankClient ?? new CrossEncoderRerankClient();
        }

        /// <summary>
        /// GET /v1.0/rerankers - List the configured rerankers (without API keys).
        /// </summary>
        public async Task GetRerankersAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                if (!await RequireAdminAsync(ctx).ConfigureAwait(false)) return;

                List<RerankerSummary> rerankers = (Settings.Rerankers ?? new List<RerankerSettings>())
                    .Where(r => r != null && !String.IsNullOrWhiteSpace(r.Id))
                    .Select(RerankerSummary.From)
                    .ToList();

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(rerankers)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetRerankersAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/rerankers/{rerankerId}/test - Score passages against a query with a configured reranker.
        /// </summary>
        public async Task TestRerankerAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                if (!await RequireAdminAsync(ctx).ConfigureAwait(false)) return;

                string rerankerId = ctx.Request.Url.Parameters["rerankerId"];
                RerankerSettings reranker = (Settings.Rerankers ?? new List<RerankerSettings>())
                    .FirstOrDefault(r => r != null && String.Equals(r.Id, rerankerId, StringComparison.OrdinalIgnoreCase));
                if (reranker == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "Reranker not found."))).ConfigureAwait(false);
                    return;
                }

                RerankerTestRequest request = String.IsNullOrWhiteSpace(ctx.Request.DataAsString)
                    ? null
                    : Serializer.DeserializeJson<RerankerTestRequest>(ctx.Request.DataAsString);
                if (request == null || String.IsNullOrWhiteSpace(request.Query) || request.Documents == null || request.Documents.Count == 0)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Query and at least one document are required."))).ConfigureAwait(false);
                    return;
                }

                if (request.Documents.Count > 100)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "At most 100 documents can be tested at once."))).ConfigureAwait(false);
                    return;
                }

                RerankerTestResult result = new RerankerTestResult { RerankerId = reranker.Id };
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    List<double> scores = await _RerankClient.ScoreAsync(reranker, request.Query, request.Documents).ConfigureAwait(false);
                    result.Success = true;
                    result.Scores = scores.Select(s => Math.Round(s, 6)).ToList();
                }
                catch (Exception e) when (e is InvalidOperationException || e is TimeoutException || e is System.Net.Http.HttpRequestException)
                {
                    result.Success = false;
                    result.ErrorMessage = e.Message;
                }

                sw.Stop();
                result.DurationMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (JsonException e)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, e.Message))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in TestRerankerAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        private async Task<bool> RequireAdminAsync(HttpContextBase ctx)
        {
            AuthContext auth = RequireAuth(ctx);
            if (auth == null)
            {
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthenticationFailed))).ConfigureAwait(false);
                return false;
            }

            if (!auth.IsGlobalAdmin && !auth.IsTenantAdmin)
            {
                ctx.Response.StatusCode = 403;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// A configured reranker, as listed by the API (the API key is never returned).
    /// </summary>
    public class RerankerSummary
    {
        /// <summary>Identifier referenced by assistant settings (RerankEndpointId).</summary>
        public string Id { get; set; } = null;

        /// <summary>Display name.</summary>
        public string Name { get; set; } = null;

        /// <summary>Request format: Tei or Cohere.</summary>
        public string Format { get; set; } = null;

        /// <summary>Service base URL.</summary>
        public string Endpoint { get; set; } = null;

        /// <summary>Model name (Cohere format).</summary>
        public string Model { get; set; } = null;

        /// <summary>Request timeout in milliseconds.</summary>
        public int TimeoutMs { get; set; } = 0;

        /// <summary>Whether an API key is configured.</summary>
        public bool HasApiKey { get; set; } = false;

        /// <summary>Build a summary from settings.</summary>
        public static RerankerSummary From(RerankerSettings settings)
        {
            return new RerankerSummary
            {
                Id = settings.Id,
                Name = settings.Name ?? settings.Id,
                Format = settings.Format,
                Endpoint = settings.Endpoint,
                Model = settings.Model,
                TimeoutMs = settings.TimeoutMs,
                HasApiKey = !String.IsNullOrWhiteSpace(settings.ApiKey)
            };
        }
    }

    /// <summary>
    /// Request to score passages with a reranker.
    /// </summary>
    public class RerankerTestRequest
    {
        /// <summary>Query text.</summary>
        public string Query { get; set; } = null;

        /// <summary>Passages to score (at most 100).</summary>
        public List<string> Documents { get; set; } = null;
    }

    /// <summary>
    /// Result of a reranker test.
    /// </summary>
    public class RerankerTestResult
    {
        /// <summary>Reranker identifier.</summary>
        public string RerankerId { get; set; } = null;

        /// <summary>Whether the reranker returned scores.</summary>
        public bool Success { get; set; } = false;

        /// <summary>One score per document, in request order (0 to 1 for cross-encoders).</summary>
        public List<double> Scores { get; set; } = null;

        /// <summary>Error when the reranker failed.</summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>Round-trip duration in milliseconds.</summary>
        public double DurationMs { get; set; } = 0;
    }
}
