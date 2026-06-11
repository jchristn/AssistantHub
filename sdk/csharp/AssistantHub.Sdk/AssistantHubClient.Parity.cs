namespace AssistantHub.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk.Enums;
    using AssistantHub.Sdk.Models;
    /// <summary>
    /// Base client surface that keeps the C# SDK aligned with the server and other SDKs.
    /// </summary>
    public abstract class AssistantHubClientParityBase : AssistantHubClientBase
    {
        #region Private-Helpers

        /// <summary>
        /// Header carrying the AssistantHub thread identifier.
        /// </summary>
        protected const string _ThreadIdHeader = "X-Thread-ID";

        /// <summary>
        /// Instantiate the parity client base.
        /// </summary>
        /// <param name="baseUrl">Base URL of the AssistantHub server.</param>
        /// <param name="apiKey">Optional API key for authentication.</param>
        protected AssistantHubClientParityBase(string baseUrl, string apiKey = null)
            : base(baseUrl, apiKey)
        {
        }

        /// <summary>
        /// Instantiate the parity client base with a provided HttpClient.
        /// </summary>
        /// <param name="baseUrl">Base URL of the AssistantHub server.</param>
        /// <param name="httpClient">HttpClient instance to use.</param>
        /// <param name="apiKey">Optional API key for authentication.</param>
        protected AssistantHubClientParityBase(string baseUrl, HttpClient httpClient, string apiKey = null)
            : base(baseUrl, httpClient, apiKey)
        {
        }

        private protected static string UrlEncode(string value)
        {
            return Uri.EscapeDataString(value ?? String.Empty);
        }

        private protected string AppendQueryString(string path, Dictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count < 1)
                return path;

            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, string> kvp in parameters)
            {
                if (!String.IsNullOrWhiteSpace(kvp.Value))
                {
                    parts.Add(kvp.Key + "=" + UrlEncode(kvp.Value));
                }
            }

            if (parts.Count < 1)
                return path;

            return path + "?" + String.Join("&", parts);
        }

        /// <summary>
        /// Append enumeration query parameters to a path.
        /// </summary>
        /// <param name="path">Base path.</param>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Path with query string.</returns>
        private protected string AppendEnumerationQuery(string path, EnumerationQuery query)
        {
            if (query == null)
                return path;

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            if (query.MaxResults > 0)
                parameters["maxResults"] = query.MaxResults.ToString();
            if (!String.IsNullOrWhiteSpace(query.ContinuationToken))
                parameters["continuationToken"] = query.ContinuationToken;
            if (query.Ordering == EnumerationOrderEnum.CreatedDescending)
                parameters["ordering"] = "CreatedDescending";
            if (!String.IsNullOrWhiteSpace(query.AssistantIdFilter))
                parameters["assistantId"] = query.AssistantIdFilter;
            if (!String.IsNullOrWhiteSpace(query.BucketNameFilter))
                parameters["bucketName"] = query.BucketNameFilter;
            if (!String.IsNullOrWhiteSpace(query.CollectionIdFilter))
                parameters["collectionId"] = query.CollectionIdFilter;
            if (!String.IsNullOrWhiteSpace(query.ThreadIdFilter))
                parameters["threadId"] = query.ThreadIdFilter;
            if (!String.IsNullOrWhiteSpace(query.RequestHistoryIdFilter))
                parameters["requestHistoryId"] = query.RequestHistoryIdFilter;
            if (!String.IsNullOrWhiteSpace(query.ChatHistoryIdFilter))
                parameters["chatHistoryId"] = query.ChatHistoryIdFilter;
            if (!String.IsNullOrWhiteSpace(query.TraceIdFilter))
                parameters["traceId"] = query.TraceIdFilter;
            if (!String.IsNullOrWhiteSpace(query.ToolNameFilter))
                parameters["toolName"] = query.ToolNameFilter;
            if (query.SuccessFilter.HasValue)
                parameters["success"] = query.SuccessFilter.Value ? "true" : "false";
            if (query.DeniedFilter.HasValue)
                parameters["denied"] = query.DeniedFilter.Value ? "true" : "false";
            if (query.StartUtc.HasValue)
                parameters["startUtc"] = query.StartUtc.Value.ToString("O");
            if (query.EndUtc.HasValue)
                parameters["endUtc"] = query.EndUtc.Value.ToString("O");

            return AppendQueryString(path, parameters);
        }

        private protected string AppendRequestHistoryFilter(string path, RequestHistorySearchFilter filter)
        {
            if (filter == null)
                return path;

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            if (filter.MaxResults > 0)
                parameters["maxResults"] = filter.MaxResults.ToString();
            if (!String.IsNullOrWhiteSpace(filter.ContinuationToken))
                parameters["continuationToken"] = filter.ContinuationToken;
            parameters["ordering"] = filter.Ordering.ToString();
            if (!String.IsNullOrWhiteSpace(filter.RequestType))
                parameters["requestType"] = filter.RequestType;
            if (!String.IsNullOrWhiteSpace(filter.HttpMethod))
                parameters["method"] = filter.HttpMethod;
            if (!String.IsNullOrWhiteSpace(filter.PathContains))
                parameters["path"] = filter.PathContains;
            if (filter.StatusCode.HasValue)
                parameters["statusCode"] = filter.StatusCode.Value.ToString();
            if (filter.Success.HasValue)
                parameters["success"] = filter.Success.Value ? "true" : "false";
            if (!String.IsNullOrWhiteSpace(filter.TenantId))
                parameters["tenantId"] = filter.TenantId;
            if (!String.IsNullOrWhiteSpace(filter.UserId))
                parameters["userId"] = filter.UserId;
            if (!String.IsNullOrWhiteSpace(filter.CredentialId))
                parameters["credentialId"] = filter.CredentialId;
            if (!String.IsNullOrWhiteSpace(filter.AssistantId))
                parameters["assistantId"] = filter.AssistantId;
            if (!String.IsNullOrWhiteSpace(filter.ThreadId))
                parameters["threadId"] = filter.ThreadId;
            if (!String.IsNullOrWhiteSpace(filter.SourceType))
                parameters["sourceType"] = filter.SourceType;
            if (!String.IsNullOrWhiteSpace(filter.SearchText))
                parameters["search"] = filter.SearchText;
            if (filter.StartUtc.HasValue)
                parameters["startUtc"] = filter.StartUtc.Value.ToString("O");
            if (filter.EndUtc.HasValue)
                parameters["endUtc"] = filter.EndUtc.Value.ToString("O");
            if (filter.BucketSeconds > 0)
                parameters["bucketSeconds"] = filter.BucketSeconds.ToString();

            return AppendQueryString(path, parameters);
        }

        private protected string AppendAssistantAnalyticsQuery(string path, AssistantAnalyticsQuery query)
        {
            if (query == null)
                return path;

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (!String.IsNullOrWhiteSpace(query.Range))
                parameters["range"] = query.Range;
            if (query.StartUtc.HasValue)
                parameters["startUtc"] = query.StartUtc.Value.ToString("O");
            if (query.EndUtc.HasValue)
                parameters["endUtc"] = query.EndUtc.Value.ToString("O");
            if (query.BucketSeconds.HasValue)
                parameters["bucketSeconds"] = query.BucketSeconds.Value.ToString();
            if (query.Metrics != null && query.Metrics.Count > 0)
                parameters["metrics"] = String.Join(",", query.Metrics);
            if (!String.IsNullOrWhiteSpace(query.Stage))
                parameters["stage"] = query.Stage;
            if (!String.IsNullOrWhiteSpace(query.EndpointId))
                parameters["endpointId"] = query.EndpointId;
            if (!String.IsNullOrWhiteSpace(query.EndpointType))
                parameters["endpointType"] = query.EndpointType;
            if (!String.IsNullOrWhiteSpace(query.Model))
                parameters["model"] = query.Model;
            if (query.Limit.HasValue)
                parameters["limit"] = query.Limit.Value.ToString();

            return AppendQueryString(path, parameters);
        }

        private protected async Task<byte[]> DownloadBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, BaseUrl + path))
            {
                using (HttpResponseMessage response = await SendRawAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                }
            }
        }

        private protected async Task<T> SendWithOptionalThreadAsync<T>(HttpMethod method, string path, object body, string threadId = null, CancellationToken cancellationToken = default)
        {
            string json = SerializeJson(body);

            using (HttpRequestMessage request = new HttpRequestMessage(method, BaseUrl + path))
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrWhiteSpace(threadId))
                    request.Headers.Add(_ThreadIdHeader, threadId);

                using (HttpResponseMessage response = await SendRawAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return DeserializeJson<T>(responseBody);
                }
            }
        }

        private protected async Task<T> SendContentAsync<T>(HttpMethod method, string path, HttpContent content, CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(method, BaseUrl + path))
            {
                request.Content = content;

                using (HttpResponseMessage response = await SendRawAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (String.IsNullOrWhiteSpace(responseBody))
                        return default;

                    return DeserializeJson<T>(responseBody);
                }
            }
        }

        #endregion

        #region Health-and-Authentication

        /// <summary>
        /// Retrieve product health metadata from the root endpoint.
        /// </summary>
        public async Task<JsonElement> HealthAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<JsonElement>(HttpMethod.Get, "/", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Authenticate with AssistantHub.
        /// </summary>
        public async Task<AuthenticateResult> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendAsync<AuthenticateResult>(HttpMethod.Post, "/v1.0/authenticate", request, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Existence-Checks

        /// <summary>
        /// Check whether an assistant exists.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the assistant exists.</returns>
        public async Task<bool> AssistantExistsAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            return await HeadAsync("/v1.0/assistants/" + UrlEncode(assistantId), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether a collection exists.
        /// </summary>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the collection exists.</returns>
        public async Task<bool> CollectionExistsAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            return await HeadAsync("/v1.0/collections/" + UrlEncode(collectionId), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether an inverted index exists.
        /// </summary>
        /// <param name="indexId">Index identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the index exists.</returns>
        public async Task<bool> IndexExistsAsync(string indexId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));

            return await HeadAsync("/v1.0/indices/" + UrlEncode(indexId), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether an inverted-index record exists.
        /// </summary>
        /// <param name="indexId">Index identifier.</param>
        /// <param name="recordId">Record identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the index record exists.</returns>
        public async Task<bool> IndexRecordExistsAsync(string indexId, string recordId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (String.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));

            return await HeadAsync("/v1.0/indices/" + UrlEncode(indexId) + "/records/" + UrlEncode(recordId), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether a tenant exists.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the tenant exists.</returns>
        public async Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            return await HeadAsync("/v1.0/tenants/" + UrlEncode(tenantId), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether a user exists in a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the user exists.</returns>
        public async Task<bool> UserExistsAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            return await HeadAsync("/v1.0/tenants/" + UrlEncode(tenantId) + "/users/" + UrlEncode(userId), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether a credential exists in a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="credentialId">Credential identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the credential exists.</returns>
        public async Task<bool> CredentialExistsAsync(string tenantId, string credentialId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(credentialId))
                throw new ArgumentNullException(nameof(credentialId));

            return await HeadAsync("/v1.0/tenants/" + UrlEncode(tenantId) + "/credentials/" + UrlEncode(credentialId), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether an ingestion rule exists.
        /// </summary>
        /// <param name="ruleId">Ingestion rule identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the ingestion rule exists.</returns>
        public async Task<bool> IngestionRuleExistsAsync(string ruleId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(ruleId))
                throw new ArgumentNullException(nameof(ruleId));

            return await HeadAsync("/v1.0/ingestion-rules/" + UrlEncode(ruleId), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether a document exists.
        /// </summary>
        /// <param name="documentId">Document identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the document exists.</returns>
        public async Task<bool> DocumentExistsAsync(string documentId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(documentId))
                throw new ArgumentNullException(nameof(documentId));

            return await HeadAsync("/v1.0/documents/" + UrlEncode(documentId), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether an embedding endpoint exists.
        /// </summary>
        /// <param name="endpointId">Endpoint identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the embedding endpoint exists.</returns>
        public async Task<bool> EmbeddingEndpointExistsAsync(string endpointId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));

            return await HeadAsync("/v1.0/endpoints/embedding/" + UrlEncode(endpointId), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether a completion endpoint exists.
        /// </summary>
        /// <param name="endpointId">Endpoint identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the completion endpoint exists.</returns>
        public async Task<bool> CompletionEndpointExistsAsync(string endpointId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));

            return await HeadAsync("/v1.0/endpoints/completion/" + UrlEncode(endpointId), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether a bucket exists.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the bucket exists.</returns>
        public async Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));

            return await HeadAsync("/v1.0/buckets/" + UrlEncode(bucketName), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether a crawl plan exists.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the crawl plan exists.</returns>
        public async Task<bool> CrawlPlanExistsAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await HeadAsync("/v1.0/crawlplans/" + UrlEncode(planId), cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
