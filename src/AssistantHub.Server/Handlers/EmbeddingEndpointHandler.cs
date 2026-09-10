namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Services;
    using SyslogLogging;
    using WatsonWebserver.Core;
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;

    /// <summary>
    /// Handles embedding endpoint CRUD routes by proxying to Partio (admin only).
    /// </summary>
    public class EmbeddingEndpointHandler : HandlerBase
    {
        private static readonly string _Header = "[EmbeddingEndpointHandler] ";
        private static readonly JsonSerializerOptions _PartioJsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly IEmbeddingEndpointService _EmbeddingEndpoints;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EmbeddingEndpointHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            IObjectStorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference,
            IEmbeddingEndpointService embeddingEndpoints)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference)
        {
            _EmbeddingEndpoints = embeddingEndpoints ?? throw new ArgumentNullException(nameof(embeddingEndpoints));
        }

        /// <summary>
        /// PUT /v1.0/endpoints/embedding - Create a new embedding endpoint.
        /// </summary>
        public async Task CreateEmbeddingEndpointAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = RequireGlobalAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string body = PartioEndpointMerge.BuildCreateBody(ctx.Request.DataAsString);

                HttpResponseMessage resp = await _EmbeddingEndpoints.SendAsync(System.Net.Http.HttpMethod.Put, "/v1.0/endpoints/embedding", body).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (resp.IsSuccessStatusCode)
                    AssistantHubServer.HealthCheckService?.OnEndpointCreated(respBody);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in CreateEmbeddingEndpointAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// POST /v1.0/endpoints/embedding/enumerate - List embedding endpoints.
        /// </summary>
        public async Task EnumerateEmbeddingEndpointsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = RequireGlobalAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string body = ctx.Request.DataAsString;

                HttpResponseMessage resp = await _EmbeddingEndpoints.SendAsync(System.Net.Http.HttpMethod.Post, "/v1.0/endpoints/embedding/enumerate", body).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";

                if (resp.IsSuccessStatusCode)
                {
                    string converted = ConvertPartioEnvelopeToEnumerationResult(respBody);
                    await ctx.Response.Send(converted).ConfigureAwait(false);
                }
                else
                {
                    await ctx.Response.Send(respBody).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in EnumerateEmbeddingEndpointsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// GET /v1.0/endpoints/embedding/{endpointId} - Get embedding endpoint by ID.
        /// </summary>
        public async Task GetEmbeddingEndpointAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = RequireGlobalAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string endpointId = ctx.Request.Url.Parameters["endpointId"];

                HttpResponseMessage resp = await _EmbeddingEndpoints.SendAsync(System.Net.Http.HttpMethod.Get, "/v1.0/endpoints/embedding/" + endpointId).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetEmbeddingEndpointAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// PUT /v1.0/endpoints/embedding/{endpointId} - Update embedding endpoint by ID.
        /// </summary>
        public async Task UpdateEmbeddingEndpointAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = RequireGlobalAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string endpointId = ctx.Request.Url.Parameters["endpointId"];
                string existingRaw = await ReadExistingEmbeddingEndpointRawAsync(endpointId).ConfigureAwait(false);
                string body = PartioEndpointMerge.BuildUpdateBody(existingRaw, ctx.Request.DataAsString);

                HttpResponseMessage resp = await _EmbeddingEndpoints.SendAsync(System.Net.Http.HttpMethod.Put, "/v1.0/endpoints/embedding/" + endpointId, body).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (resp.IsSuccessStatusCode)
                    AssistantHubServer.HealthCheckService?.OnEndpointUpdated(respBody);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in UpdateEmbeddingEndpointAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// DELETE /v1.0/endpoints/embedding/{endpointId} - Delete embedding endpoint by ID.
        /// </summary>
        public async Task DeleteEmbeddingEndpointAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = RequireGlobalAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string endpointId = ctx.Request.Url.Parameters["endpointId"];

                HttpResponseMessage resp = await _EmbeddingEndpoints.SendAsync(System.Net.Http.HttpMethod.Delete, "/v1.0/endpoints/embedding/" + endpointId).ConfigureAwait(false);

                if (resp.IsSuccessStatusCode)
                    AssistantHubServer.HealthCheckService?.OnEndpointDeleted(endpointId);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                if (ctx.Response.StatusCode == 204)
                {
                    await ctx.Response.Send().ConfigureAwait(false);
                }
                else
                {
                    string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(respBody).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteEmbeddingEndpointAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// HEAD /v1.0/endpoints/embedding/{endpointId} - Check embedding endpoint existence.
        /// </summary>
        public async Task HeadEmbeddingEndpointAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = RequireGlobalAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                string endpointId = ctx.Request.Url.Parameters["endpointId"];

                HttpResponseMessage resp = await _EmbeddingEndpoints.SendAsync(System.Net.Http.HttpMethod.Head, "/v1.0/endpoints/embedding/" + endpointId).ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in HeadEmbeddingEndpointAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/endpoints/embedding/{endpointId}/test - Exercise an embedding endpoint through Partio.
        /// </summary>
        public async Task TestEmbeddingEndpointAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = RequireGlobalAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string endpointId = ctx.Request.Url.Parameters["endpointId"];
                EndpointExplorerEmbeddingRequest request = String.IsNullOrEmpty(ctx.Request.DataAsString)
                    ? new EndpointExplorerEmbeddingRequest()
                    : JsonSerializer.Deserialize<EndpointExplorerEmbeddingRequest>(ctx.Request.DataAsString);

                if (request == null)
                    request = new EndpointExplorerEmbeddingRequest();

                request.EndpointId = endpointId;

                // Use Partio's production embed endpoint (POST /v1.0/embed), not the explorer endpoint.
                // /v1.0/embed takes a batch (Input as an array) and returns one vector per input, so we
                // send the single test input as a one-element batch and map the first vector back into
                // the single-vector shape the dashboard test modal expects.
                object embedRequest = new
                {
                    request.EndpointId,
                    Input = new List<string> { request.Input ?? string.Empty },
                    request.L2Normalization
                };

                HttpResponseMessage resp = await _EmbeddingEndpoints.SendAsync(System.Net.Http.HttpMethod.Post, "/v1.0/embed", Serializer.SerializeJson(embedRequest)).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                EndpointExplorerEmbeddingResponse mapped = new EndpointExplorerEmbeddingResponse
                {
                    EndpointId = endpointId,
                    Input = request.Input
                };

                PartioEmbedResponse embedResponse = null;
                try { embedResponse = JsonSerializer.Deserialize<PartioEmbedResponse>(respBody, _PartioJsonOptions); }
                catch (Exception parseEx) { Logging.Warn(_Header + "failed to parse embed response: " + parseEx.Message); }

                if (resp.IsSuccessStatusCode && embedResponse != null)
                {
                    mapped.Success = embedResponse.Success;
                    mapped.StatusCode = embedResponse.StatusCode != 0 ? embedResponse.StatusCode : (int)resp.StatusCode;
                    mapped.Error = embedResponse.Error;
                    mapped.Model = embedResponse.Model;
                    mapped.Dimensions = embedResponse.Dimensions;
                    mapped.ResponseTimeMs = (long)Math.Round(embedResponse.ResponseTimeMs);
                    if (embedResponse.Embeddings != null && embedResponse.Embeddings.Count > 0)
                        mapped.Embedding = embedResponse.Embeddings[0];
                }
                else
                {
                    // Surface the failure in the response body (HTTP 200) so the test modal renders a
                    // failure card with the upstream status and error message.
                    mapped.Success = false;
                    mapped.StatusCode = embedResponse != null && embedResponse.StatusCode != 0 ? embedResponse.StatusCode : (int)resp.StatusCode;
                    mapped.Error = embedResponse?.Error ?? respBody;
                    mapped.Model = embedResponse?.Model;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(mapped)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in TestEmbeddingEndpointAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/endpoints/embedding/{endpointId}/load - Load or warm an embedding endpoint model through Partio.
        /// </summary>
        public async Task LoadEmbeddingEndpointModelAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = RequireGlobalAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string endpointId = ctx.Request.Url.Parameters["endpointId"];
                string body = String.IsNullOrWhiteSpace(ctx.Request.DataAsString) ? "{}" : ctx.Request.DataAsString;

                using (HttpResponseMessage resp = await _EmbeddingEndpoints.SendAsync(System.Net.Http.HttpMethod.Post, "/v1.0/endpoints/embedding/" + endpointId + "/load", body).ConfigureAwait(false))
                {
                    string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    CopyModelLoadHeaders(resp, ctx);

                    ctx.Response.StatusCode = (int)resp.StatusCode;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(respBody).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in LoadEmbeddingEndpointModelAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        #region Private-Methods

        /// <summary>
        /// Copy model-load metadata response headers from Partio.
        /// </summary>
        private static void CopyModelLoadHeaders(HttpResponseMessage resp, HttpContextBase ctx)
        {
            CopyHeader(resp, ctx, "X-Partio-Endpoint-Id");
            CopyHeader(resp, ctx, "X-Model");
            CopyHeader(resp, ctx, "X-Partio-Model");
        }

        /// <summary>
        /// Copy a response header from Partio when present.
        /// </summary>
        private static void CopyHeader(HttpResponseMessage resp, HttpContextBase ctx, string name)
        {
            if (resp.Headers.TryGetValues(name, out IEnumerable<string> values))
                ctx.Response.Headers.Add(name, String.Join(",", values));
        }

        /// <summary>
        /// GET the current embedding endpoint's raw JSON so <see cref="PartioEndpointMerge.BuildUpdateBody"/>
        /// can overlay only the caller's fields and preserve everything AssistantHub does not manage
        /// (health-check config, tokenization, context size, other services' tags/labels). Returns "{}" when
        /// the endpoint cannot be read.
        /// </summary>
        private async Task<string> ReadExistingEmbeddingEndpointRawAsync(string endpointId)
        {
            if (String.IsNullOrWhiteSpace(endpointId)) return "{}";

            try
            {
                using HttpResponseMessage resp = await _EmbeddingEndpoints.SendAsync(
                    System.Net.Http.HttpMethod.Get,
                    "/v1.0/endpoints/embedding/" + endpointId).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                    return "{}";

                string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                return String.IsNullOrWhiteSpace(body) ? "{}" : body;
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "failed to read existing embedding endpoint for merge " + endpointId + ": " + e.Message);
                return "{}";
            }
        }

        /// <summary>
        /// Convert Partio's envelope format { Data, TotalCount, HasMore } to
        /// AssistantHub's standard EnumerationResult format { Objects, TotalRecords, EndOfResults, ... }.
        /// </summary>
        private string ConvertPartioEnvelopeToEnumerationResult(string partioJson)
        {
            PartioEnumerationEnvelope<PartioEndpointConfig> envelope =
                JsonSerializer.Deserialize<PartioEnumerationEnvelope<PartioEndpointConfig>>(partioJson, _PartioJsonOptions);

            EnumerationResult<PartioEndpointConfig> result = new EnumerationResult<PartioEndpointConfig>
            {
                Success = true,
                MaxResults = envelope?.Data != null && envelope.Data.Count > 0 ? envelope.Data.Count : 100,
                TotalRecords = envelope?.TotalCount ?? 0,
                RecordsRemaining = envelope != null && envelope.HasMore
                    ? Math.Max(envelope.TotalCount - (envelope.Data?.Count ?? 0), 0)
                    : 0,
                ContinuationToken = null,
                EndOfResults = !(envelope?.HasMore ?? false),
                Objects = envelope?.Data ?? new List<PartioEndpointConfig>(),
                TotalMs = 0
            };

            return Serializer.SerializeJson(result);
        }

        #endregion

        /// <summary>
        /// GET /v1.0/endpoints/embedding/{endpointId}/health - Get embedding endpoint health from local state.
        /// </summary>
        public async Task GetEmbeddingEndpointHealthAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = RequireGlobalAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string endpointId = ctx.Request.Url.Parameters["endpointId"];
                EndpointHealthState state = AssistantHubServer.HealthCheckService?.GetHealthState(endpointId);

                if (state == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                EndpointHealthStatus status = EndpointHealthStatus.FromState(state);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(status)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetEmbeddingEndpointHealthAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/endpoints/embedding/health - Get all embedding endpoint health statuses.
        /// </summary>
        public async Task GetAllEmbeddingEndpointHealthAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                AuthContext auth = RequireGlobalAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                List<EndpointHealthState> states = AssistantHubServer.HealthCheckService?.GetAllHealthStates() ?? new List<EndpointHealthState>();
                List<EndpointHealthStatus> statuses = new List<EndpointHealthStatus>();
                foreach (EndpointHealthState state in states)
                {
                    statuses.Add(EndpointHealthStatus.FromState(state));
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(statuses)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetAllEmbeddingEndpointHealthAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
    }
}
