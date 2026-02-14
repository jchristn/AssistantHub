namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Net.Http;
    using System.Text;
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
    /// Handles collection CRUD routes by proxying to RecallDB (admin only).
    /// </summary>
    public class CollectionHandler : HandlerBase
    {
        private static readonly string _Header = "[CollectionHandler] ";
        private static readonly HttpClient _HttpClient = new HttpClient();

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
        public CollectionHandler(
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
        /// PUT /v1.0/collections - Create a new collection.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PutCollectionAsync(HttpContextBase ctx)
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
                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Put, BuildRecallDbUrl(null));
                req.Headers.Add("Authorization", "Bearer " + Settings.RecallDb.AccessKey);
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
                if (!IsAdmin(ctx))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                EnumerationQuery query = BuildEnumerationQuery(ctx);
                string enumerateBody = BuildEnumerateRequestBody(query);

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, BuildRecallDbUrl("enumerate"));
                req.Headers.Add("Authorization", "Bearer " + Settings.RecallDb.AccessKey);
                req.Content = new StringContent(enumerateBody, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
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
                if (!IsAdmin(ctx))
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

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, BuildRecallDbUrl(collectionId));
                req.Headers.Add("Authorization", "Bearer " + Settings.RecallDb.AccessKey);

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
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
                if (!IsAdmin(ctx))
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
                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Put, BuildRecallDbUrl(collectionId));
                req.Headers.Add("Authorization", "Bearer " + Settings.RecallDb.AccessKey);
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
                if (!IsAdmin(ctx))
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

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Delete, BuildRecallDbUrl(collectionId));
                req.Headers.Add("Authorization", "Bearer " + Settings.RecallDb.AccessKey);

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
                if (!IsAdmin(ctx))
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

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, BuildRecallDbUrl(collectionId));
                req.Headers.Add("Authorization", "Bearer " + Settings.RecallDb.AccessKey);

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);

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
                if (!IsAdmin(ctx))
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

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, BuildRecallDbDocumentUrl(collectionId, "enumerate"));
                req.Headers.Add("Authorization", "Bearer " + Settings.RecallDb.AccessKey);
                req.Content = new StringContent(enumerateBody, Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
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
                if (!IsAdmin(ctx))
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

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, BuildRecallDbDocumentUrl(collectionId, recordId));
                req.Headers.Add("Authorization", "Bearer " + Settings.RecallDb.AccessKey);

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
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
                if (!IsAdmin(ctx))
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

                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Delete, BuildRecallDbDocumentUrl(collectionId, recordId));
                req.Headers.Add("Authorization", "Bearer " + Settings.RecallDb.AccessKey);

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
                Logging.Warn(_Header + "exception in DeleteRecordAsync: " + e.Message);
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
                if (!IsAdmin(ctx))
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
                HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Put, BuildRecallDbDocumentUrl(collectionId, null));
                req.Headers.Add("Authorization", "Bearer " + Settings.RecallDb.AccessKey);
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
                Logging.Warn(_Header + "exception in PutRecordAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        #endregion

        #region Private-Methods

        private string BuildRecallDbUrl(string path)
        {
            string baseUrl = Settings.RecallDb.Endpoint.TrimEnd('/');
            string url = baseUrl + "/v1.0/tenants/" + Settings.RecallDb.TenantId + "/collections";
            if (!String.IsNullOrEmpty(path))
                url += "/" + path;
            return url;
        }

        private string BuildRecallDbDocumentUrl(string collectionId, string path)
        {
            string baseUrl = Settings.RecallDb.Endpoint.TrimEnd('/');
            string url = baseUrl + "/v1.0/tenants/" + Settings.RecallDb.TenantId + "/collections/" + collectionId + "/documents";
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
