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
    /// Additional API surface to keep the C# SDK aligned with the server and other SDKs.
    /// </summary>
    public partial class AssistantHubClient
    {
        #region Private-Helpers

        private static string UrlEncode(string value)
        {
            return Uri.EscapeDataString(value ?? String.Empty);
        }

        private string AppendQueryString(string path, Dictionary<string, string> parameters)
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

        private string AppendEnumerationQuery(string path, EnumerationQuery query)
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

            return AppendQueryString(path, parameters);
        }

        private async Task<byte[]> DownloadBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, BaseUrl + path))
            {
                using (HttpResponseMessage response = await SendRawAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task<T> SendWithOptionalThreadAsync<T>(HttpMethod method, string path, object body, string threadId = null, CancellationToken cancellationToken = default)
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

        private async Task<T> SendContentAsync<T>(HttpMethod method, string path, HttpContent content, CancellationToken cancellationToken = default)
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

        public async Task<bool> AssistantExistsAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            return await HeadAsync("/v1.0/assistants/" + UrlEncode(assistantId), cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> CollectionExistsAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            return await HeadAsync("/v1.0/collections/" + UrlEncode(collectionId), cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            return await HeadAsync("/v1.0/tenants/" + UrlEncode(tenantId), cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> UserExistsAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            return await HeadAsync("/v1.0/tenants/" + UrlEncode(tenantId) + "/users/" + UrlEncode(userId), cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> CredentialExistsAsync(string tenantId, string credentialId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(credentialId))
                throw new ArgumentNullException(nameof(credentialId));

            return await HeadAsync("/v1.0/tenants/" + UrlEncode(tenantId) + "/credentials/" + UrlEncode(credentialId), cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> IngestionRuleExistsAsync(string ruleId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(ruleId))
                throw new ArgumentNullException(nameof(ruleId));

            return await HeadAsync("/v1.0/ingestion-rules/" + UrlEncode(ruleId), cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> DocumentExistsAsync(string documentId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(documentId))
                throw new ArgumentNullException(nameof(documentId));

            return await HeadAsync("/v1.0/documents/" + UrlEncode(documentId), cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> EmbeddingEndpointExistsAsync(string endpointId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));

            return await HeadAsync("/v1.0/endpoints/embedding/" + UrlEncode(endpointId), cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> CompletionEndpointExistsAsync(string endpointId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));

            return await HeadAsync("/v1.0/endpoints/completion/" + UrlEncode(endpointId), cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));

            return await HeadAsync("/v1.0/buckets/" + UrlEncode(bucketName), cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> CrawlPlanExistsAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await HeadAsync("/v1.0/crawlplans/" + UrlEncode(planId), cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Assistants-and-Threads

        /// <summary>
        /// List assistants with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<Assistant>> ListAssistantsAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<Assistant>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/assistants", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve public assistant information.
        /// </summary>
        public async Task<AssistantPublicInfo> GetAssistantPublicAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            return await SendAsync<AssistantPublicInfo>(HttpMethod.Get, "/v1.0/assistants/" + UrlEncode(assistantId) + "/public", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve assistant settings.
        /// </summary>
        public async Task<AssistantSettings> GetAssistantSettingsAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            return await SendAsync<AssistantSettings>(HttpMethod.Get, "/v1.0/assistants/" + UrlEncode(assistantId) + "/settings", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create or update assistant settings.
        /// </summary>
        public async Task<AssistantSettings> UpdateAssistantSettingsAsync(string assistantId, AssistantSettings settings, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            return await SendAsync<AssistantSettings>(HttpMethod.Put, "/v1.0/assistants/" + UrlEncode(assistantId) + "/settings", settings, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Verify Slack configuration for an assistant without persisting it.
        /// </summary>
        public async Task<SlackVerificationResponse> VerifySlackAsync(string assistantId, SlackVerificationRequest request, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendAsync<SlackVerificationResponse>(HttpMethod.Post, "/v1.0/assistants/" + UrlEncode(assistantId) + "/settings/slack/verify", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List distinct document labels for an assistant's configured collection.
        /// </summary>
        public async Task<List<string>> GetAssistantDistinctLabelsAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            return await SendAsync<List<string>>(HttpMethod.Get, "/v1.0/assistants/" + UrlEncode(assistantId) + "/labels/distinct", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List distinct document tags for an assistant's configured collection.
        /// </summary>
        public async Task<List<string>> GetAssistantDistinctTagsAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            return await SendAsync<List<string>>(HttpMethod.Get, "/v1.0/assistants/" + UrlEncode(assistantId) + "/tags/distinct", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve detailed thread summaries.
        /// </summary>
        public async Task<List<ThreadSummary>> ListThreadSummariesAsync(EnumerationQuery query = null, CancellationToken cancellationToken = default)
        {
            return await SendAsync<List<ThreadSummary>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/threads", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve thread history entries with the server's history schema.
        /// </summary>
        public async Task<List<ChatHistory>> GetThreadHistoryAsync(string assistantId, string threadId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (String.IsNullOrWhiteSpace(threadId))
                throw new ArgumentNullException(nameof(threadId));

            return await SendAsync<List<ChatHistory>>(HttpMethod.Get, "/v1.0/assistants/" + UrlEncode(assistantId) + "/threads/" + UrlEncode(threadId) + "/history", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Force compaction of a conversation.
        /// </summary>
        public async Task<JsonElement> CompactAsync(string assistantId, ChatCompletionRequest request, string threadId = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendWithOptionalThreadAsync<JsonElement>(HttpMethod.Post, "/v1.0/assistants/" + UrlEncode(assistantId) + "/compact", request, threadId, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Submit feedback for an assistant response.
        /// </summary>
        public async Task<AssistantFeedback> SubmitFeedbackAsync(string assistantId, FeedbackRequest request, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendAsync<AssistantFeedback>(HttpMethod.Post, "/v1.0/assistants/" + UrlEncode(assistantId) + "/feedback", request, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Documents-and-History

        /// <summary>
        /// Retrieve a document processing log payload.
        /// </summary>
        public async Task<JsonElement> GetDocumentProcessingLogAsync(string documentId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(documentId))
                throw new ArgumentNullException(nameof(documentId));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/documents/" + UrlEncode(documentId) + "/processing-log", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Download a stored document.
        /// </summary>
        public async Task<byte[]> DownloadDocumentAsync(string documentId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(documentId))
                throw new ArgumentNullException(nameof(documentId));

            return await DownloadBytesAsync("/v1.0/documents/" + UrlEncode(documentId) + "/download", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Download a public document for an assistant.
        /// </summary>
        public async Task<byte[]> DownloadDocumentPublicAsync(string assistantId, string documentId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (String.IsNullOrWhiteSpace(documentId))
                throw new ArgumentNullException(nameof(documentId));

            return await DownloadBytesAsync("/v1.0/assistants/" + UrlEncode(assistantId) + "/documents/" + UrlEncode(documentId) + "/download", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List feedback records.
        /// </summary>
        public async Task<EnumerationResult<AssistantFeedback>> ListFeedbackAsync(EnumerationQuery query = null, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<AssistantFeedback>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/feedback", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a feedback record by identifier.
        /// </summary>
        public async Task<AssistantFeedback> GetFeedbackAsync(string feedbackId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(feedbackId))
                throw new ArgumentNullException(nameof(feedbackId));

            return await SendAsync<AssistantFeedback>(HttpMethod.Get, "/v1.0/feedback/" + UrlEncode(feedbackId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a feedback record.
        /// </summary>
        public async Task DeleteFeedbackAsync(string feedbackId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(feedbackId))
                throw new ArgumentNullException(nameof(feedbackId));

            await SendAsync(HttpMethod.Delete, "/v1.0/feedback/" + UrlEncode(feedbackId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List chat history entries.
        /// </summary>
        public async Task<EnumerationResult<ChatHistory>> ListHistoryAsync(EnumerationQuery query = null, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<ChatHistory>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/history", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a chat history record by identifier.
        /// </summary>
        public async Task<ChatHistory> GetHistoryAsync(string historyId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(historyId))
                throw new ArgumentNullException(nameof(historyId));

            return await SendAsync<ChatHistory>(HttpMethod.Get, "/v1.0/history/" + UrlEncode(historyId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a chat history record.
        /// </summary>
        public async Task DeleteHistoryAsync(string historyId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(historyId))
                throw new ArgumentNullException(nameof(historyId));

            await SendAsync(HttpMethod.Delete, "/v1.0/history/" + UrlEncode(historyId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Endpoints-and-Models

        /// <summary>
        /// Retrieve a specific embedding endpoint's health state.
        /// </summary>
        public async Task<EndpointHealthStatus> CheckEmbeddingEndpointHealthAsync(string endpointId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));

            return await SendAsync<EndpointHealthStatus>(HttpMethod.Get, "/v1.0/endpoints/embedding/" + UrlEncode(endpointId) + "/health", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Test an embedding endpoint.
        /// </summary>
        public async Task<EndpointExplorerEmbeddingResponse> TestEmbeddingEndpointAsync(string endpointId, EndpointExplorerEmbeddingRequest request, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendAsync<EndpointExplorerEmbeddingResponse>(HttpMethod.Post, "/v1.0/endpoints/embedding/" + UrlEncode(endpointId) + "/test", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve a specific completion endpoint's health state.
        /// </summary>
        public async Task<EndpointHealthStatus> CheckCompletionEndpointHealthAsync(string endpointId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));

            return await SendAsync<EndpointHealthStatus>(HttpMethod.Get, "/v1.0/endpoints/completion/" + UrlEncode(endpointId) + "/health", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Test a completion endpoint.
        /// </summary>
        public async Task<EndpointExplorerCompletionResponse> TestCompletionEndpointAsync(string endpointId, EndpointExplorerCompletionRequest request, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendAsync<EndpointExplorerCompletionResponse>(HttpMethod.Post, "/v1.0/endpoints/completion/" + UrlEncode(endpointId) + "/test", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List models, optionally filtered by assistant identifier.
        /// </summary>
        public async Task<List<InferenceModel>> ListModelsAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            string path = AppendQueryString("/v1.0/models", new Dictionary<string, string>
            {
                ["assistantId"] = assistantId
            });

            return await SendAsync<List<InferenceModel>>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Collections-and-Buckets

        /// <summary>
        /// List collections with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<Collection>> ListCollectionsAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<Collection>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/collections", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get distinct labels from a collection.
        /// </summary>
        public async Task<List<string>> GetCollectionDistinctLabelsAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            return await SendAsync<List<string>>(HttpMethod.Get, "/v1.0/collections/" + UrlEncode(collectionId) + "/labels/distinct", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get distinct tags from a collection.
        /// </summary>
        public async Task<List<string>> GetCollectionDistinctTagsAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            return await SendAsync<List<string>>(HttpMethod.Get, "/v1.0/collections/" + UrlEncode(collectionId) + "/tags/distinct", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a record in a collection.
        /// </summary>
        public async Task<CollectionRecord> CreateCollectionRecordAsync(string collectionId, CollectionRecord record, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            return await SendAsync<CollectionRecord>(HttpMethod.Put, "/v1.0/collections/" + UrlEncode(collectionId) + "/records", record, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List collection records.
        /// </summary>
        public async Task<EnumerationResult<CollectionRecord>> ListCollectionRecordsAsync(string collectionId, EnumerationQuery query = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            string path = AppendEnumerationQuery("/v1.0/collections/" + UrlEncode(collectionId) + "/records", query);
            return await SendAsync<EnumerationResult<CollectionRecord>>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a single collection record.
        /// </summary>
        public async Task<CollectionRecord> GetCollectionRecordAsync(string collectionId, string recordId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));
            if (String.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));

            return await SendAsync<CollectionRecord>(HttpMethod.Get, "/v1.0/collections/" + UrlEncode(collectionId) + "/records/" + UrlEncode(recordId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a single collection record.
        /// </summary>
        public async Task DeleteCollectionRecordAsync(string collectionId, string recordId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));
            if (String.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));

            await SendAsync(HttpMethod.Delete, "/v1.0/collections/" + UrlEncode(collectionId) + "/records/" + UrlEncode(recordId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete multiple collection records.
        /// </summary>
        public async Task BatchDeleteCollectionRecordsAsync(string collectionId, List<string> recordIds, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));
            if (recordIds == null)
                throw new ArgumentNullException(nameof(recordIds));

            await SendAsync(HttpMethod.Post, "/v1.0/collections/" + UrlEncode(collectionId) + "/records/batch/delete", recordIds, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a bucket.
        /// </summary>
        public async Task<JsonElement> CreateBucketAsync(BucketCreateRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/buckets", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List buckets.
        /// </summary>
        public async Task<JsonElement> ListBucketsAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/buckets", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a bucket.
        /// </summary>
        public async Task<JsonElement> GetBucketAsync(string bucketName, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/buckets/" + UrlEncode(bucketName), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a bucket.
        /// </summary>
        public async Task DeleteBucketAsync(string bucketName, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));

            await SendAsync(HttpMethod.Delete, "/v1.0/buckets/" + UrlEncode(bucketName), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create an empty object marker in a bucket.
        /// </summary>
        public async Task<JsonElement> PutBucketObjectAsync(string bucketName, string key, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            string path = AppendQueryString("/v1.0/buckets/" + UrlEncode(bucketName) + "/objects", new Dictionary<string, string>
            {
                ["key"] = key
            });

            return await SendAsync<JsonElement>(HttpMethod.Put, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List objects in a bucket.
        /// </summary>
        public async Task<JsonElement> ListBucketObjectsAsync(string bucketName, string prefix = null, string delimiter = "/", CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));

            string path = AppendQueryString("/v1.0/buckets/" + UrlEncode(bucketName) + "/objects", new Dictionary<string, string>
            {
                ["prefix"] = prefix,
                ["delimiter"] = delimiter
            });

            return await SendAsync<JsonElement>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an object from a bucket.
        /// </summary>
        public async Task DeleteBucketObjectAsync(string bucketName, string key, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            string path = AppendQueryString("/v1.0/buckets/" + UrlEncode(bucketName) + "/objects", new Dictionary<string, string>
            {
                ["key"] = key
            });

            await SendAsync(HttpMethod.Delete, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve object metadata from a bucket.
        /// </summary>
        public async Task<JsonElement> GetBucketObjectMetadataAsync(string bucketName, string key, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            string path = AppendQueryString("/v1.0/buckets/" + UrlEncode(bucketName) + "/objects/metadata", new Dictionary<string, string>
            {
                ["key"] = key
            });

            return await SendAsync<JsonElement>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Download an object from a bucket.
        /// </summary>
        public async Task<byte[]> DownloadBucketObjectAsync(string bucketName, string key, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            string path = AppendQueryString("/v1.0/buckets/" + UrlEncode(bucketName) + "/objects/download", new Dictionary<string, string>
            {
                ["key"] = key
            });

            return await DownloadBytesAsync(path, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Upload binary content to a bucket object.
        /// </summary>
        public async Task<JsonElement> UploadBucketObjectAsync(string bucketName, string key, byte[] data, string contentType = "application/octet-stream", CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string path = AppendQueryString("/v1.0/buckets/" + UrlEncode(bucketName) + "/objects/upload", new Dictionary<string, string>
            {
                ["key"] = key
            });

            ByteArrayContent content = new ByteArrayContent(data);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(String.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

            return await SendContentAsync<JsonElement>(HttpMethod.Post, path, content, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Crawl-and-Eval

        /// <summary>
        /// List crawl plans with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<CrawlPlan>> ListCrawlPlansAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<CrawlPlan>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/crawlplans", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Test connectivity for a crawl plan.
        /// </summary>
        public async Task<JsonElement> TestCrawlConnectivityAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await SendAsync<JsonElement>(HttpMethod.Post, "/v1.0/crawlplans/" + UrlEncode(planId) + "/connectivity", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Enumerate crawl plan contents.
        /// </summary>
        public async Task<JsonElement> EnumerateCrawlAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/crawlplans/" + UrlEncode(planId) + "/enumerate", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List crawl operations with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<CrawlOperation>> ListCrawlOperationsAsync(string planId, EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await SendAsync<EnumerationResult<CrawlOperation>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/crawlplans/" + UrlEncode(planId) + "/operations", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get an operation's saved enumeration payload.
        /// </summary>
        public async Task<JsonElement> GetCrawlOperationEnumerationAsync(string planId, string operationId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));
            if (String.IsNullOrWhiteSpace(operationId))
                throw new ArgumentNullException(nameof(operationId));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/crawlplans/" + UrlEncode(planId) + "/operations/" + UrlEncode(operationId) + "/enumeration", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List evaluation facts with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<EvalFact>> ListEvalFactsAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<EvalFact>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/eval/facts", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List evaluation runs with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<EvalRun>> ListEvalRunsAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<EvalRun>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/eval/runs", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve an evaluation result by identifier.
        /// </summary>
        public async Task<EvalResult> GetEvalResultAsync(string resultId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(resultId))
                throw new ArgumentNullException(nameof(resultId));

            return await SendAsync<EvalResult>(HttpMethod.Get, "/v1.0/eval/results/" + UrlEncode(resultId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Stream evaluation run updates via SSE.
        /// </summary>
        public async IAsyncEnumerable<string> StreamEvalRunAsync(string runId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(runId))
                throw new ArgumentNullException(nameof(runId));

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, BaseUrl + "/v1.0/eval/runs/" + UrlEncode(runId) + "/stream"))
            {
                using (HttpResponseMessage response = await SendRawAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        while (!reader.EndOfStream)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            string line = await reader.ReadLineAsync().ConfigureAwait(false);
                            if (line == null)
                                break;

                            if (line.StartsWith("data: "))
                            {
                                string data = line.Substring(6);
                                if (data == "[DONE]")
                                    yield break;

                                yield return data;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Tenants-Users-Credentials

        /// <summary>
        /// List tenants with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<TenantMetadata>> ListTenantsAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<TenantMetadata>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/tenants", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List users with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<UserMaster>> ListUsersAsync(string tenantId, EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            return await SendAsync<EnumerationResult<UserMaster>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/tenants/" + UrlEncode(tenantId) + "/users", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List credentials with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<Credential>> ListCredentialsAsync(string tenantId, EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            return await SendAsync<EnumerationResult<Credential>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/tenants/" + UrlEncode(tenantId) + "/credentials", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List ingestion rules with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<IngestionRule>> ListIngestionRulesAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<IngestionRule>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/ingestion-rules", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
