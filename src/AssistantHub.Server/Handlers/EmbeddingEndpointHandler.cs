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
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Handles embedding endpoint CRUD routes by proxying to Partio (admin only).
    /// </summary>
    public class EmbeddingEndpointHandler : HandlerBase
    {
        private static readonly string _Header = "[EmbeddingEndpointHandler] ";
        private static readonly HttpClient _HttpClient = new HttpClient();

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EmbeddingEndpointHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            StorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference)
        {
        }

        /// <summary>
        /// PUT /v1.0/endpoints/embedding - Create a new embedding endpoint.
        /// </summary>
        public async Task CreateEmbeddingEndpointAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                if (!IsAdmin(ctx))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string body = InjectTenantId(ctx.Request.DataAsString);
                string partioUrl = Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/embedding";

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Put, partioUrl);
                req.Headers.Add("Authorization", "Bearer " + Settings.Chunking.AccessKey);
                if (!String.IsNullOrEmpty(body))
                    req.Content = new StringContent(body, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

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
                if (!IsAdmin(ctx))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string body = ctx.Request.DataAsString;
                string partioUrl = Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/embedding/enumerate";

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, partioUrl);
                req.Headers.Add("Authorization", "Bearer " + Settings.Chunking.AccessKey);
                if (!String.IsNullOrEmpty(body))
                    req.Content = new StringContent(body, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
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
                if (!IsAdmin(ctx))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string endpointId = ctx.Request.Url.Parameters["endpointId"];
                string partioUrl = Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/embedding/" + endpointId;

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, partioUrl);
                req.Headers.Add("Authorization", "Bearer " + Settings.Chunking.AccessKey);

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
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
                if (!IsAdmin(ctx))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string endpointId = ctx.Request.Url.Parameters["endpointId"];
                string body = ctx.Request.DataAsString;
                string partioUrl = Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/embedding/" + endpointId;

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Put, partioUrl);
                req.Headers.Add("Authorization", "Bearer " + Settings.Chunking.AccessKey);
                if (!String.IsNullOrEmpty(body))
                    req.Content = new StringContent(body, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

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
                if (!IsAdmin(ctx))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string endpointId = ctx.Request.Url.Parameters["endpointId"];
                string partioUrl = Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/embedding/" + endpointId;

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Delete, partioUrl);
                req.Headers.Add("Authorization", "Bearer " + Settings.Chunking.AccessKey);

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);

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
                if (!IsAdmin(ctx))
                {
                    ctx.Response.StatusCode = 403;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                string endpointId = ctx.Request.Url.Parameters["endpointId"];
                string partioUrl = Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/embedding/" + endpointId;

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, partioUrl);
                req.Headers.Add("Authorization", "Bearer " + Settings.Chunking.AccessKey);

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);

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
        #region Private-Methods

        /// <summary>
        /// Inject the default TenantId into a JSON request body if not already present.
        /// Partio requires a TenantId to scope endpoints to the correct tenant.
        /// </summary>
        private string InjectTenantId(string body)
        {
            if (String.IsNullOrEmpty(body)) return "{\"TenantId\":\"default\"}";

            using JsonDocument doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("TenantId", out JsonElement tid) &&
                tid.ValueKind == JsonValueKind.String &&
                !String.IsNullOrEmpty(tid.GetString()))
            {
                return body;
            }

            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
            dict["TenantId"] = JsonSerializer.Deserialize<JsonElement>("\"default\"");
            return JsonSerializer.Serialize(dict);
        }

        /// <summary>
        /// Convert Partio's envelope format { Data, TotalCount, HasMore } to
        /// AssistantHub's standard EnumerationResult format { Objects, TotalRecords, EndOfResults, ... }.
        /// </summary>
        private string ConvertPartioEnvelopeToEnumerationResult(string partioJson)
        {
            using JsonDocument doc = JsonDocument.Parse(partioJson);
            JsonElement root = doc.RootElement;

            JsonElement data = root.TryGetProperty("Data", out JsonElement d) ? d : default;
            long totalCount = root.TryGetProperty("TotalCount", out JsonElement tc) && tc.ValueKind == JsonValueKind.Number ? tc.GetInt64() : 0;
            bool hasMore = root.TryGetProperty("HasMore", out JsonElement hm) && hm.ValueKind == JsonValueKind.True;

            int objectCount = data.ValueKind == JsonValueKind.Array ? data.GetArrayLength() : 0;
            string objectsJson = data.ValueKind == JsonValueKind.Array ? data.GetRawText() : "[]";

            return "{" +
                "\"Success\":true," +
                "\"MaxResults\":" + (objectCount > 0 ? objectCount : 100) + "," +
                "\"TotalRecords\":" + totalCount + "," +
                "\"RecordsRemaining\":" + (hasMore ? Math.Max(totalCount - objectCount, 0) : 0) + "," +
                "\"ContinuationToken\":null," +
                "\"EndOfResults\":" + (!hasMore ? "true" : "false") + "," +
                "\"Objects\":" + objectsJson + "," +
                "\"TotalMs\":0" +
                "}";
        }

        #endregion

        /// <summary>
        /// GET /v1.0/endpoints/embedding/{endpointId}/health - Get embedding endpoint health.
        /// </summary>
        public async Task GetEmbeddingEndpointHealthAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            try
            {
                if (!IsAdmin(ctx))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string endpointId = ctx.Request.Url.Parameters["endpointId"];
                string partioUrl = Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/embedding/" + endpointId + "/health";

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, partioUrl);
                req.Headers.Add("Authorization", "Bearer " + Settings.Chunking.AccessKey);

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetEmbeddingEndpointHealthAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }
    }
}
