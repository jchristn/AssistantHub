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
    /// Adds assistant, document, history, endpoint, and model parity APIs.
    /// </summary>
    public abstract class AssistantHubClientAssistantParityBase : AssistantHubClientParityBase
    {

        private protected AssistantHubClientAssistantParityBase(string baseUrl, string apiKey = null)
            : base(baseUrl, apiKey)
        {
        }

        private protected AssistantHubClientAssistantParityBase(string baseUrl, HttpClient httpClient, string apiKey = null)
            : base(baseUrl, httpClient, apiKey)
        {
        }

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
        /// List safe public document metadata selectable in assistant chat.
        /// </summary>
        public async Task<EnumerationResult<AssistantDocumentSelectionItem>> ListAssistantDocumentsAsync(
            string assistantId,
            EnumerationQuery query = null,
            string searchQuery = null,
            CancellationToken cancellationToken = default)
        {
            return await ListAssistantDocumentsAsync(assistantId, query, searchQuery, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List safe public document metadata selectable in assistant chat.
        /// </summary>
        public async Task<EnumerationResult<AssistantDocumentSelectionItem>> ListAssistantDocumentsAsync(
            string assistantId,
            EnumerationQuery query,
            string searchQuery,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            string path = AppendEnumerationQuery("/v1.0/assistants/" + UrlEncode(assistantId) + "/documents", query);
            if (!String.IsNullOrWhiteSpace(searchQuery))
                path += (path.Contains("?") ? "&" : "?") + "query=" + UrlEncode(searchQuery);
            if (!String.IsNullOrWhiteSpace(contentType))
                path += (path.Contains("?") ? "&" : "?") + "contentType=" + UrlEncode(contentType);

            return await SendAsync<EnumerationResult<AssistantDocumentSelectionItem>>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Notify the server that an assistant chat window was opened, loading configured endpoint models when enabled.
        /// </summary>
        public async Task<AssistantChatOpenResult> OpenAssistantChatAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            return await SendAsync<AssistantChatOpenResult>(HttpMethod.Post, "/v1.0/assistants/" + UrlEncode(assistantId) + "/chat/open", cancellationToken: cancellationToken).ConfigureAwait(false);
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
        /// Retrieve effective tool availability for an assistant.
        /// </summary>
        public async Task<List<AssistantToolDescriptor>> GetAssistantToolsAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            return await SendAsync<List<AssistantToolDescriptor>>(HttpMethod.Get, "/v1.0/assistants/" + UrlEncode(assistantId) + "/tools", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Validate draft tool policy for an assistant without persisting it.
        /// </summary>
        public async Task<AssistantToolPolicyValidationResult> ValidateAssistantToolPolicyAsync(
            string assistantId,
            AssistantToolPolicyValidationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendAsync<AssistantToolPolicyValidationResult>(HttpMethod.Post, "/v1.0/assistants/" + UrlEncode(assistantId) + "/settings/tools/validate", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Run administrator dry-run diagnostics for an assistant tool policy without executing tools.
        /// </summary>
        public async Task<AssistantToolPolicyTestResult> TestAssistantToolPolicyAsync(
            string assistantId,
            AssistantToolPolicyValidationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendAsync<AssistantToolPolicyTestResult>(HttpMethod.Post, "/v1.0/assistants/" + UrlEncode(assistantId) + "/settings/tools/test", request, cancellationToken).ConfigureAwait(false);
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
        /// Retrieve assistant analytics overview.
        /// </summary>
        public async Task<AssistantAnalyticsOverviewResult> GetAssistantAnalyticsOverviewAsync(string assistantId, AssistantAnalyticsQuery query = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            string path = AppendAssistantAnalyticsQuery("/v1.0/assistants/" + UrlEncode(assistantId) + "/analytics/overview", query);
            return await SendAsync<AssistantAnalyticsOverviewResult>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve assistant analytics time series.
        /// </summary>
        public async Task<AssistantAnalyticsTimeSeriesResult> GetAssistantAnalyticsTimeSeriesAsync(string assistantId, AssistantAnalyticsQuery query = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            string path = AppendAssistantAnalyticsQuery("/v1.0/assistants/" + UrlEncode(assistantId) + "/analytics/timeseries", query);
            return await SendAsync<AssistantAnalyticsTimeSeriesResult>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve assistant analytics stage summaries.
        /// </summary>
        public async Task<AssistantAnalyticsStageResult> GetAssistantAnalyticsStagesAsync(string assistantId, AssistantAnalyticsQuery query = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            string path = AppendAssistantAnalyticsQuery("/v1.0/assistants/" + UrlEncode(assistantId) + "/analytics/stages", query);
            return await SendAsync<AssistantAnalyticsStageResult>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve assistant analytics endpoint summaries.
        /// </summary>
        public async Task<AssistantAnalyticsEndpointResult> GetAssistantAnalyticsEndpointsAsync(string assistantId, AssistantAnalyticsQuery query = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            string path = AppendAssistantAnalyticsQuery("/v1.0/assistants/" + UrlEncode(assistantId) + "/analytics/endpoints", query);
            return await SendAsync<AssistantAnalyticsEndpointResult>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve slowest assistant requests.
        /// </summary>
        public async Task<AssistantAnalyticsSlowestResult> GetAssistantAnalyticsSlowestAsync(string assistantId, AssistantAnalyticsQuery query = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            string path = AppendAssistantAnalyticsQuery("/v1.0/assistants/" + UrlEncode(assistantId) + "/analytics/slowest", query);
            return await SendAsync<AssistantAnalyticsSlowestResult>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve assistant feedback analytics.
        /// </summary>
        public async Task<AssistantAnalyticsFeedbackResult> GetAssistantAnalyticsFeedbackAsync(string assistantId, AssistantAnalyticsQuery query = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            string path = AppendAssistantAnalyticsQuery("/v1.0/assistants/" + UrlEncode(assistantId) + "/analytics/feedback", query);
            return await SendAsync<AssistantAnalyticsFeedbackResult>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Enumerate redacted tool-call traces for an assistant.
        /// </summary>
        public async Task<EnumerationResult<AssistantToolCallRecord>> ListAssistantToolCallsAsync(string assistantId, EnumerationQuery query = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            string path = AppendEnumerationQuery("/v1.0/assistants/" + UrlEncode(assistantId) + "/tool-calls", query);
            return await SendAsync<EnumerationResult<AssistantToolCallRecord>>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve one redacted assistant tool-call trace.
        /// </summary>
        public async Task<AssistantToolCallRecord> GetAssistantToolCallAsync(string assistantId, string toolCallRecordId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (String.IsNullOrWhiteSpace(toolCallRecordId))
                throw new ArgumentNullException(nameof(toolCallRecordId));

            return await SendAsync<AssistantToolCallRecord>(HttpMethod.Get, "/v1.0/assistants/" + UrlEncode(assistantId) + "/tool-calls/" + UrlEncode(toolCallRecordId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete assistant tool-call traces matching the supplied filters.
        /// </summary>
        public async Task<RequestHistoryDeleteResult> DeleteAssistantToolCallsAsync(string assistantId, EnumerationQuery query = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            string path = AppendEnumerationQuery("/v1.0/assistants/" + UrlEncode(assistantId) + "/tool-calls", query);
            return await SendAsync<RequestHistoryDeleteResult>(HttpMethod.Delete, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete one assistant tool-call trace.
        /// </summary>
        public async Task DeleteAssistantToolCallAsync(string assistantId, string toolCallRecordId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (String.IsNullOrWhiteSpace(toolCallRecordId))
                throw new ArgumentNullException(nameof(toolCallRecordId));

            await SendAsync(HttpMethod.Delete, "/v1.0/assistants/" + UrlEncode(assistantId) + "/tool-calls/" + UrlEncode(toolCallRecordId), cancellationToken: cancellationToken).ConfigureAwait(false);
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
    }
}
