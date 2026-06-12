namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
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
    using NetHttpMethod = System.Net.Http.HttpMethod;

    /// <summary>
    /// Handles inverted index routes by proxying to Verbex (admin only).
    /// </summary>
    public class IndexHandler : HandlerBase
    {
        private static readonly string _Header = "[IndexHandler] ";
        private readonly IInvertedIndexService _InvertedIndex;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public IndexHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            IObjectStorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference,
            IInvertedIndexService invertedIndex)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference)
        {
            _InvertedIndex = invertedIndex ?? throw new ArgumentNullException(nameof(invertedIndex));
        }

        /// <summary>
        /// GET /v1.0/indices - List indices.
        /// </summary>
        public Task GetIndicesAsync(HttpContextBase ctx) => ProxyAsync(ctx, NetHttpMethod.Get, AppendRequestQuery(ctx, "/v1.0/indices"));

        /// <summary>
        /// PUT /v1.0/indices - Create an index.
        /// </summary>
        public async Task PutIndexAsync(HttpContextBase ctx)
        {
            AuthContext auth = RequireGlobalAdmin(ctx);
            if (!await EnsureAuthorizedAsync(ctx, auth).ConfigureAwait(false)) return;
            string body = InjectTenantId(ctx.Request.DataAsString, auth.TenantId);
            await ProxyAuthorizedAsync(ctx, NetHttpMethod.Post, "/v1.0/indices", body).ConfigureAwait(false);
        }

        /// <summary>
        /// GET /v1.0/indices/{indexId} - Get index metadata.
        /// </summary>
        public Task GetIndexAsync(HttpContextBase ctx) => ProxyIndexAsync(ctx, NetHttpMethod.Get, null);

        /// <summary>
        /// HEAD /v1.0/indices/{indexId} - Check index existence.
        /// </summary>
        public Task HeadIndexAsync(HttpContextBase ctx) => ProxyIndexAsync(ctx, NetHttpMethod.Head, null);

        /// <summary>
        /// PUT /v1.0/indices/{indexId} - Update index metadata.
        /// </summary>
        public Task PutIndexByIdAsync(HttpContextBase ctx) => ProxyIndexAsync(ctx, NetHttpMethod.Put, null, ctx.Request.DataAsString);

        /// <summary>
        /// DELETE /v1.0/indices/{indexId} - Delete an index.
        /// </summary>
        public Task DeleteIndexAsync(HttpContextBase ctx) => ProxyIndexAsync(ctx, NetHttpMethod.Delete, null);

        /// <summary>
        /// PUT /v1.0/indices/{indexId}/labels - Update index labels.
        /// </summary>
        public Task PutIndexLabelsAsync(HttpContextBase ctx) => ProxyIndexAsync(ctx, NetHttpMethod.Put, "labels", ctx.Request.DataAsString);

        /// <summary>
        /// PUT /v1.0/indices/{indexId}/tags - Update index tags.
        /// </summary>
        public Task PutIndexTagsAsync(HttpContextBase ctx) => ProxyIndexAsync(ctx, NetHttpMethod.Put, "tags", ctx.Request.DataAsString);

        /// <summary>
        /// PUT /v1.0/indices/{indexId}/custom-metadata - Update index custom metadata.
        /// </summary>
        public Task PutIndexCustomMetadataAsync(HttpContextBase ctx) => ProxyIndexAsync(ctx, NetHttpMethod.Put, "customMetadata", ctx.Request.DataAsString);

        /// <summary>
        /// GET /v1.0/indices/{indexId}/terms/top - Get top index terms.
        /// </summary>
        public Task GetTopTermsAsync(HttpContextBase ctx) => ProxyIndexAsync(ctx, NetHttpMethod.Get, AppendRequestQuery(ctx, "terms/top"));

        /// <summary>
        /// GET /v1.0/indices/{indexId}/records - List index records.
        /// </summary>
        public Task GetRecordsAsync(HttpContextBase ctx) => ProxyRecordCollectionAsync(ctx, NetHttpMethod.Get, null, null, true);

        /// <summary>
        /// PUT /v1.0/indices/{indexId}/records - Create an index record.
        /// </summary>
        public Task PutRecordAsync(HttpContextBase ctx) => ProxyRecordCollectionAsync(ctx, NetHttpMethod.Post, null, PopulateIndexRecordNames(ctx.Request.DataAsString), false);

        /// <summary>
        /// POST /v1.0/indices/{indexId}/records/batch - Create records in batch.
        /// </summary>
        public Task PostRecordBatchAsync(HttpContextBase ctx) => ProxyRecordCollectionAsync(ctx, NetHttpMethod.Post, "batch", PopulateIndexRecordNames(ctx.Request.DataAsString), false);

        /// <summary>
        /// POST /v1.0/indices/{indexId}/records/exists - Check record existence in batch.
        /// </summary>
        public Task PostRecordExistsAsync(HttpContextBase ctx) => ProxyRecordCollectionAsync(ctx, NetHttpMethod.Post, "exists", ctx.Request.DataAsString, false);

        /// <summary>
        /// POST /v1.0/indices/{indexId}/records/delete - Delete records in batch.
        /// </summary>
        public async Task DeleteRecordsAsync(HttpContextBase ctx)
        {
            List<string> recordIds = BulkDeleteRequestParser.ParseRecordIds(ctx.Request.DataAsString);
            if (recordIds.Count == 0)
            {
                await SendBadRequestAsync(ctx).ConfigureAwait(false);
                return;
            }

            string body = Serializer.SerializeJson(new { DocumentIds = recordIds }, false);
            await ProxyRecordCollectionAsync(ctx, NetHttpMethod.Post, "delete", body, false).ConfigureAwait(false);
        }

        /// <summary>
        /// GET /v1.0/indices/{indexId}/records/{recordId} - Get an index record.
        /// </summary>
        public Task GetRecordAsync(HttpContextBase ctx) => ProxyRecordAsync(ctx, NetHttpMethod.Get, null);

        /// <summary>
        /// HEAD /v1.0/indices/{indexId}/records/{recordId} - Check index record existence.
        /// </summary>
        public Task HeadRecordAsync(HttpContextBase ctx) => ProxyRecordAsync(ctx, NetHttpMethod.Head, null);

        /// <summary>
        /// DELETE /v1.0/indices/{indexId}/records/{recordId} - Delete an index record.
        /// </summary>
        public Task DeleteRecordAsync(HttpContextBase ctx) => ProxyRecordAsync(ctx, NetHttpMethod.Delete, null);

        /// <summary>
        /// PUT /v1.0/indices/{indexId}/records/{recordId}/labels - Update record labels.
        /// </summary>
        public Task PutRecordLabelsAsync(HttpContextBase ctx) => ProxyRecordAsync(ctx, NetHttpMethod.Put, "labels", ctx.Request.DataAsString);

        /// <summary>
        /// PUT /v1.0/indices/{indexId}/records/{recordId}/tags - Update record tags.
        /// </summary>
        public Task PutRecordTagsAsync(HttpContextBase ctx) => ProxyRecordAsync(ctx, NetHttpMethod.Put, "tags", ctx.Request.DataAsString);

        /// <summary>
        /// PUT /v1.0/indices/{indexId}/records/{recordId}/custom-metadata - Update record custom metadata.
        /// </summary>
        public Task PutRecordCustomMetadataAsync(HttpContextBase ctx) => ProxyRecordAsync(ctx, NetHttpMethod.Put, "customMetadata", ctx.Request.DataAsString);

        /// <summary>
        /// POST /v1.0/indices/{indexId}/search - Search an index.
        /// </summary>
        public Task PostSearchAsync(HttpContextBase ctx) => ProxyIndexAsync(ctx, NetHttpMethod.Post, "search", ctx.Request.DataAsString);

        #region Private-Methods

        private async Task ProxyAsync(HttpContextBase ctx, NetHttpMethod method, string verbexPath, string body = null)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            AuthContext auth = RequireGlobalAdmin(ctx);
            if (!await EnsureAuthorizedAsync(ctx, auth).ConfigureAwait(false)) return;
            await ProxyAuthorizedAsync(ctx, method, verbexPath, body).ConfigureAwait(false);
        }

        private async Task ProxyAuthorizedAsync(HttpContextBase ctx, NetHttpMethod method, string verbexPath, string body = null)
        {
            try
            {
                using (HttpResponseMessage resp = await _InvertedIndex.SendAsync(method, verbexPath, body).ConfigureAwait(false))
                {
                    ctx.Response.StatusCode = (int)resp.StatusCode;
                    if (method == NetHttpMethod.Head || ctx.Response.StatusCode == 204)
                    {
                        await ctx.Response.Send().ConfigureAwait(false);
                        return;
                    }

                    string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(respBody).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception proxying Verbex request: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        private async Task<bool> EnsureAuthorizedAsync(HttpContextBase ctx, AuthContext auth)
        {
            if (auth != null) return true;

            ctx.Response.StatusCode = 403;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
            return false;
        }

        private async Task ProxyIndexAsync(HttpContextBase ctx, NetHttpMethod method, string childPath, string body = null)
        {
            string indexId = ctx.Request.Url.Parameters["indexId"];
            if (String.IsNullOrEmpty(indexId))
            {
                await SendBadRequestAsync(ctx).ConfigureAwait(false);
                return;
            }

            string path = "/v1.0/indices/" + Uri.EscapeDataString(indexId);
            if (!String.IsNullOrEmpty(childPath))
                path += "/" + childPath.TrimStart('/');

            await ProxyAsync(ctx, method, path, body).ConfigureAwait(false);
        }

        private async Task ProxyRecordCollectionAsync(HttpContextBase ctx, NetHttpMethod method, string childPath, string body, bool includeQuery)
        {
            string indexId = ctx.Request.Url.Parameters["indexId"];
            if (String.IsNullOrEmpty(indexId))
            {
                await SendBadRequestAsync(ctx).ConfigureAwait(false);
                return;
            }

            string path = "/v1.0/indices/" + Uri.EscapeDataString(indexId) + "/documents";
            if (!String.IsNullOrEmpty(childPath))
                path += "/" + childPath.TrimStart('/');

            if (includeQuery)
                path = AppendRequestQuery(ctx, path);

            await ProxyAsync(ctx, method, path, body).ConfigureAwait(false);
        }

        private async Task ProxyRecordAsync(HttpContextBase ctx, NetHttpMethod method, string childPath, string body = null)
        {
            string indexId = ctx.Request.Url.Parameters["indexId"];
            string recordId = ctx.Request.Url.Parameters["recordId"];
            if (String.IsNullOrEmpty(indexId) || String.IsNullOrEmpty(recordId))
            {
                await SendBadRequestAsync(ctx).ConfigureAwait(false);
                return;
            }

            string path = "/v1.0/indices/" + Uri.EscapeDataString(indexId)
                + "/documents/" + Uri.EscapeDataString(recordId);
            if (!String.IsNullOrEmpty(childPath))
                path += "/" + childPath.TrimStart('/');

            await ProxyAsync(ctx, method, path, body).ConfigureAwait(false);
        }

        private string AppendRequestQuery(HttpContextBase ctx, string path)
        {
            if (ctx?.Request?.Url?.RawWithQuery == null) return path;
            string raw = ctx.Request.Url.RawWithQuery;
            int queryStart = raw.IndexOf('?');
            if (queryStart < 0) return path;
            return path + raw.Substring(queryStart);
        }

        private string InjectTenantId(string body, string tenantId)
        {
            string effectiveTenantId = String.IsNullOrEmpty(tenantId) ? Constants.DefaultTenantId : tenantId;
            JsonObject obj = null;

            if (!String.IsNullOrEmpty(body))
            {
                JsonNode node = JsonNode.Parse(body);
                obj = node as JsonObject;
            }

            if (obj == null)
                obj = new JsonObject();

            bool hasTenantId = false;
            foreach (string key in obj.Select(kvp => kvp.Key))
            {
                if (String.Equals(key, "TenantId", StringComparison.OrdinalIgnoreCase))
                {
                    hasTenantId = true;
                    break;
                }
            }

            if (!hasTenantId)
                obj["TenantId"] = effectiveTenantId;

            return obj.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }

        private string PopulateIndexRecordNames(string body)
        {
            if (String.IsNullOrWhiteSpace(body)) return body;

            try
            {
                JsonNode node = JsonNode.Parse(body);
                if (node == null) return body;

                if (node is JsonObject obj)
                {
                    bool normalizedCollection = false;
                    normalizedCollection |= PopulateIndexRecordNames(obj, "Documents");
                    normalizedCollection |= PopulateIndexRecordNames(obj, "Objects");
                    normalizedCollection |= PopulateIndexRecordNames(obj, "Records");

                    if (!normalizedCollection)
                        PopulateIndexRecordName(obj);
                }
                else if (node is JsonArray array)
                {
                    PopulateIndexRecordNames(array);
                }

                return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
            }
            catch
            {
                return body;
            }
        }

        private bool PopulateIndexRecordNames(JsonObject container, string propertyName)
        {
            JsonNode node = GetJsonNode(container, propertyName);
            if (node is not JsonArray array) return false;
            PopulateIndexRecordNames(array);
            return true;
        }

        private void PopulateIndexRecordNames(JsonArray array)
        {
            foreach (JsonNode node in array)
            {
                if (node is JsonObject record)
                    PopulateIndexRecordName(record);
            }
        }

        private void PopulateIndexRecordName(JsonObject record)
        {
            string name = GetStringProperty(record, "Name");

            if (String.IsNullOrWhiteSpace(name))
            {
                name = GetStringProperty(record, "ObjectName")
                    ?? GetStringMetadata(record, "ObjectName")
                    ?? ExtractObjectName(GetStringProperty(record, "ObjectKey"))
                    ?? ExtractObjectName(GetStringMetadata(record, "ObjectKey"))
                    ?? ExtractObjectName(GetStringProperty(record, "Key"))
                    ?? ExtractObjectName(GetStringMetadata(record, "SourceUrl"))
                    ?? GetStringProperty(record, "Id")
                    ?? GetStringProperty(record, "Identifier")
                    ?? "record";

                SetStringProperty(record, "Name", name.Trim());
            }
            else
            {
                name = name.Trim();
            }

            PopulateObjectNameMetadata(record, name);
        }

        private void PopulateObjectNameMetadata(JsonObject record, string name)
        {
            if (String.IsNullOrWhiteSpace(name)) return;

            JsonNode metadataNode = GetJsonNode(record, "CustomMetadata");
            JsonObject metadata = metadataNode as JsonObject;
            if (metadata == null)
            {
                if (metadataNode != null) return;
                metadata = new JsonObject();
                SetJsonProperty(record, "CustomMetadata", metadata);
            }

            if (String.IsNullOrWhiteSpace(GetStringProperty(metadata, "ObjectName")))
                SetStringProperty(metadata, "ObjectName", name.Trim());
        }

        private string GetStringMetadata(JsonObject record, string propertyName)
        {
            JsonObject metadata = GetJsonObject(record, "CustomMetadata");
            return metadata != null ? GetStringProperty(metadata, propertyName) : null;
        }

        private JsonObject GetJsonObject(JsonObject obj, string propertyName)
        {
            return GetJsonNode(obj, propertyName) as JsonObject;
        }

        private JsonNode GetJsonNode(JsonObject obj, string propertyName)
        {
            if (obj == null) return null;

            foreach (KeyValuePair<string, JsonNode?> kvp in obj)
            {
                if (String.Equals(kvp.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            return null;
        }

        private string GetStringProperty(JsonObject obj, string propertyName)
        {
            if (obj == null) return null;

            foreach (KeyValuePair<string, JsonNode?> kvp in obj)
            {
                if (!String.Equals(kvp.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (kvp.Value == null) return null;

                if (kvp.Value is JsonValue value && value.TryGetValue(out string text))
                    return text;

                return kvp.Value.ToJsonString();
            }

            return null;
        }

        private void SetStringProperty(JsonObject obj, string propertyName, string value)
        {
            SetJsonProperty(obj, propertyName, value);
        }

        private void SetJsonProperty(JsonObject obj, string propertyName, JsonNode value)
        {
            foreach (string key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (String.Equals(key, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    obj[key] = value;
                    return;
                }
            }

            obj[propertyName] = value;
        }

        private string ExtractObjectName(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;

            string candidate = value.Trim();
            if (Uri.TryCreate(candidate, UriKind.Absolute, out Uri uri) && !String.IsNullOrWhiteSpace(uri.AbsolutePath))
                candidate = uri.AbsolutePath;

            candidate = candidate.TrimEnd('/', '\\');
            if (String.IsNullOrWhiteSpace(candidate)) return null;

            int slash = candidate.LastIndexOf('/');
            int backslash = candidate.LastIndexOf('\\');
            int separator = Math.Max(slash, backslash);

            if (separator >= 0 && separator < candidate.Length - 1)
                return candidate.Substring(separator + 1);

            return candidate;
        }

        private async Task SendBadRequestAsync(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
        }

        #endregion
    }
}
