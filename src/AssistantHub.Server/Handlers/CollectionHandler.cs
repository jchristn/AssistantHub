namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json.Nodes;
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
    /// Handles collection CRUD routes by proxying to RecallDB (admin only).
    /// </summary>
    public class CollectionHandler : HandlerBase
    {
        private static readonly string _Header = "[CollectionHandler] ";
        private readonly IVectorStoreService _VectorStore;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">Application settings.</param>
        /// <param name="authentication">Authentication service.</param>
        /// <param name="storage">Storage service.</param>
        /// <param name="ingestion">Ingestion service.</param>
        /// <param name="retrieval">Retrieval service.</param>
        /// <param name="inference">Inference service.</param>
        /// <param name="vectorStore">Vector-store service.</param>
        public CollectionHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            IObjectStorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference,
            IVectorStoreService vectorStore)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference)
        {
            _VectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        }

        /// <summary>
        /// PUT /v1.0/collections - Create a new collection.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PutCollectionAsync(HttpContextBase ctx)
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
                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Put, BuildRecallDbPath(auth.TenantId, null), body).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutCollectionAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/collections - List collections via RecallDB enumerate.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetCollectionsAsync(HttpContextBase ctx)
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

                EnumerationQuery query = BuildEnumerationQuery(ctx);
                string enumerateBody = BuildEnumerateRequestBody(query);

                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Post, BuildRecallDbPath(auth.TenantId, "enumerate"), enumerateBody).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetCollectionsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/collections/{collectionId} - Get collection by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetCollectionAsync(HttpContextBase ctx)
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

                string collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Get, BuildRecallDbPath(auth.TenantId, collectionId)).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetCollectionAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// PUT /v1.0/collections/{collectionId} - Update collection by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PutCollectionByIdAsync(HttpContextBase ctx)
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

                string collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                string body = ctx.Request.DataAsString;
                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Put, BuildRecallDbPath(auth.TenantId, collectionId), body).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutCollectionByIdAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/collections/{collectionId} - Delete collection by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteCollectionAsync(HttpContextBase ctx)
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

                string collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Delete, BuildRecallDbPath(auth.TenantId, collectionId)).ConfigureAwait(false);

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
                Logging.Warn(_Header + "exception in DeleteCollectionAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// HEAD /v1.0/collections/{collectionId} - Check collection existence.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task HeadCollectionAsync(HttpContextBase ctx)
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

                string collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Head, BuildRecallDbPath(auth.TenantId, collectionId)).ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in HeadCollectionAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/collections/{collectionId}/search - Search records in a RecallDB collection.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task SearchCollectionAsync(HttpContextBase ctx)
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

                string collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                string body = NormalizeCollectionSearchBody(ctx.Request.DataAsString);
                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Post, BuildRecallDbPath(auth.TenantId, collectionId + "/search"), body).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in SearchCollectionAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        private static string NormalizeCollectionSearchBody(string body)
        {
            if (String.IsNullOrWhiteSpace(body)) return body;

            try
            {
                JsonNode node = JsonNode.Parse(body);
                if (node is not JsonObject obj) return body;

                string propertyName = null;
                JsonNode includeNeighbors = null;
                foreach (var kvp in obj)
                {
                    if (String.Equals(kvp.Key, "IncludeNeighbors", StringComparison.OrdinalIgnoreCase))
                    {
                        propertyName = kvp.Key;
                        includeNeighbors = kvp.Value;
                        break;
                    }
                }

                if (String.IsNullOrEmpty(propertyName) || includeNeighbors == null) return body;

                try
                {
                    bool boolValue = includeNeighbors.GetValue<bool>();
                    if (boolValue)
                        obj[propertyName] = 1;
                    else
                        obj.Remove(propertyName);
                    return obj.ToJsonString();
                }
                catch
                {
                    return body;
                }
            }
            catch
            {
                return body;
            }
        }

        #region Record-Operations

        /// <summary>
        /// GET /v1.0/collections/{collectionId}/records - List records in a collection via RecallDB enumerate.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetRecordsAsync(HttpContextBase ctx)
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

                string collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                EnumerationQuery query = BuildEnumerationQuery(ctx);
                string enumerateBody = BuildEnumerateRequestBody(query);

                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Post, BuildRecallDbDocumentPath(auth.TenantId, collectionId, "enumerate"), enumerateBody).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetRecordsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/collections/{collectionId}/records/{recordId} - Get a record by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetRecordAsync(HttpContextBase ctx)
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

                string collectionId = ctx.Request.Url.Parameters["collectionId"];
                string recordId = ctx.Request.Url.Parameters["recordId"];
                if (String.IsNullOrEmpty(collectionId) || String.IsNullOrEmpty(recordId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Get, BuildRecallDbDocumentPath(auth.TenantId, collectionId, recordId)).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetRecordAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/collections/{collectionId}/records/{recordId} - Delete a record by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteRecordAsync(HttpContextBase ctx)
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

                string collectionId = ctx.Request.Url.Parameters["collectionId"];
                string recordId = ctx.Request.Url.Parameters["recordId"];
                if (String.IsNullOrEmpty(collectionId) || String.IsNullOrEmpty(recordId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Delete, BuildRecallDbDocumentPath(auth.TenantId, collectionId, recordId)).ConfigureAwait(false);

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
                Logging.Warn(_Header + "exception in DeleteRecordAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/collections/{collectionId}/records/batch/delete - Batch delete records in a collection.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task BatchDeleteRecordsAsync(HttpContextBase ctx)
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

                string collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                string body = ctx.Request.DataAsString;
                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Post, BuildRecallDbDocumentPath(auth.TenantId, collectionId, "batch/delete"), body).ConfigureAwait(false);

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
                Logging.Warn(_Header + "exception in BatchDeleteRecordsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// PUT /v1.0/collections/{collectionId}/records - Create a new record (document) in a collection.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PutRecordAsync(HttpContextBase ctx)
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

                string collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                string body = ctx.Request.DataAsString;
                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Put, BuildRecallDbDocumentPath(auth.TenantId, collectionId, null), body).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutRecordAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/collections/{collectionId}/labels/distinct - Get distinct label values (admin).
        /// </summary>
        public async Task GetDistinctLabelsAsync(HttpContextBase ctx)
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

                string collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Get, BuildRecallDbPath(auth.TenantId, collectionId + "/labels/distinct")).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetDistinctLabelsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/collections/{collectionId}/tags/distinct - Get distinct tag keys (admin).
        /// </summary>
        public async Task GetDistinctTagsAsync(HttpContextBase ctx)
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

                string collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                HttpResponseMessage resp = await _VectorStore.SendAsync(System.Net.Http.HttpMethod.Get, BuildRecallDbPath(auth.TenantId, collectionId + "/tags/distinct")).ConfigureAwait(false);
                string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                ctx.Response.StatusCode = (int)resp.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(respBody).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetDistinctTagsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        #endregion

        #region Private-Methods

        private string BuildRecallDbPath(string tenantId, string path)
        {
            string url = "/v1.0/tenants/" + tenantId + "/collections";
            if (!String.IsNullOrEmpty(path))
                url += "/" + path;
            return url;
        }

        private string BuildRecallDbDocumentPath(string tenantId, string collectionId, string path)
        {
            string url = "/v1.0/tenants/" + tenantId + "/collections/" + collectionId + "/documents";
            if (!String.IsNullOrEmpty(path))
                url += "/" + path;
            return url;
        }

        private string BuildEnumerateRequestBody(EnumerationQuery query)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"MaxResults\":" + query.MaxResults);
            if (!String.IsNullOrEmpty(query.ContinuationToken))
                sb.Append(",\"ContinuationToken\":\"" + query.ContinuationToken + "\"");
            sb.Append(",\"Ordering\":\"" + query.Ordering.ToString() + "\"");
            sb.Append("}");
            return sb.ToString();
        }

        #endregion
    }
}
