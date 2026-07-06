namespace AssistantHub.Core.Database.Postgresql.Implementations
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// PostgreSQL assistant settings methods implementation.
    /// </summary>
    public class AssistantSettingsMethods : IAssistantSettingsMethods
    {
        #region Private-Members

        private PostgresqlDatabaseDriver _Driver;
        private DatabaseSettings _Settings;
        private LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="driver">PostgreSQL database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public AssistantSettingsMethods(PostgresqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<AssistantSettings> CreateAsync(AssistantSettings assistantSettings, CancellationToken token = default)
        {
            if (assistantSettings == null) throw new ArgumentNullException(nameof(assistantSettings));

            assistantSettings.CreatedUtc = DateTime.UtcNow;
            assistantSettings.LastUpdateUtc = assistantSettings.CreatedUtc;

            string query =
                "INSERT INTO assistant_settings " +
                "(id, assistant_id, temperature, top_p, system_prompt, max_tokens, context_window, " +
                "enable_rag, enable_retrieval_gate, enable_query_rewrite, query_rewrite_prompt, " +
                "enable_reranking, reranker_top_k, reranker_score_threshold, rerank_prompt, " +
                "enable_citations, citation_link_mode, enable_document_attachments, document_attachment_max_count, expose_document_source_urls, collection_id, retrieval_top_k, retrieval_score_threshold, " +
                "search_mode, text_weight, fulltext_search_type, fulltext_language, fulltext_normalization, fulltext_minimum_score, " +
                "retrieval_include_neighbors, " +
                "inference_endpoint_id, tool_routing_inference_endpoint_id, retrieval_gate_inference_endpoint_id, query_rewrite_inference_endpoint_id, rerank_inference_endpoint_id, enable_answerability_check, answerability_inference_endpoint_id, answerability_mode, answerability_prompt, embedding_endpoint_id, load_models_on_chat_open, expose_thinking, title, logo_url, favicon_url, retrieval_label_filter, retrieval_tag_filter, streaming, enable_slack, slack_app_token, slack_bot_token, slack_channel_id, slack_message_prefix, tool_policy_json, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(assistantSettings.Id) + "', " +
                "'" + _Driver.Sanitize(assistantSettings.AssistantId) + "', " +
                _Driver.FormatDouble(assistantSettings.Temperature) + ", " +
                _Driver.FormatDouble(assistantSettings.TopP) + ", " +
                _Driver.FormatNullableString(assistantSettings.SystemPrompt) + ", " +
                assistantSettings.MaxTokens + ", " +
                assistantSettings.ContextWindow + ", " +
                (assistantSettings.EnableRag ? 1 : 0) + ", " +
                (assistantSettings.EnableRetrievalGate ? 1 : 0) + ", " +
                FormatBooleanColumn(assistantSettings.EnableQueryRewrite) + ", " +
                _Driver.FormatNullableString(assistantSettings.QueryRewritePrompt) + ", " +
                FormatBooleanColumn(assistantSettings.EnableReranking) + ", " +
                assistantSettings.RerankerTopK + ", " +
                _Driver.FormatDouble(assistantSettings.RerankerScoreThreshold) + ", " +
                _Driver.FormatNullableString(assistantSettings.RerankPrompt) + ", " +
                (assistantSettings.EnableCitations ? 1 : 0) + ", " +
                _Driver.FormatNullableString(assistantSettings.CitationLinkMode) + ", " +
                FormatBooleanColumn(assistantSettings.EnableDocumentAttachments) + ", " +
                assistantSettings.DocumentAttachmentMaxCount + ", " +
                FormatBooleanColumn(assistantSettings.ExposeDocumentSourceUrls) + ", " +
                _Driver.FormatNullableString(assistantSettings.CollectionId) + ", " +
                assistantSettings.RetrievalTopK + ", " +
                _Driver.FormatDouble(assistantSettings.RetrievalScoreThreshold) + ", " +
                _Driver.FormatNullableString(assistantSettings.SearchMode) + ", " +
                _Driver.FormatDouble(assistantSettings.TextWeight) + ", " +
                _Driver.FormatNullableString(assistantSettings.FullTextSearchType) + ", " +
                _Driver.FormatNullableString(assistantSettings.FullTextLanguage) + ", " +
                assistantSettings.FullTextNormalization + ", " +
                (assistantSettings.FullTextMinimumScore.HasValue ? _Driver.FormatDouble(assistantSettings.FullTextMinimumScore.Value) : "NULL") + ", " +
                assistantSettings.RetrievalIncludeNeighbors + ", " +
                _Driver.FormatNullableString(assistantSettings.InferenceEndpointId) + ", " +
                _Driver.FormatNullableString(assistantSettings.ToolRoutingInferenceEndpointId) + ", " +
                _Driver.FormatNullableString(assistantSettings.RetrievalGateInferenceEndpointId) + ", " +
                _Driver.FormatNullableString(assistantSettings.QueryRewriteInferenceEndpointId) + ", " +
                _Driver.FormatNullableString(assistantSettings.RerankInferenceEndpointId) + ", " +
                FormatBooleanColumn(assistantSettings.EnableAnswerabilityCheck) + ", " +
                _Driver.FormatNullableString(assistantSettings.AnswerabilityInferenceEndpointId) + ", " +
                _Driver.FormatNullableString(assistantSettings.AnswerabilityMode) + ", " +
                _Driver.FormatNullableString(assistantSettings.AnswerabilityPrompt) + ", " +
                _Driver.FormatNullableString(assistantSettings.EmbeddingEndpointId) + ", " +
                FormatBooleanColumn(assistantSettings.LoadModelsOnChatOpen) + ", " +
                FormatBooleanColumn(assistantSettings.ExposeThinking) + ", " +
                _Driver.FormatNullableString(assistantSettings.Title) + ", " +
                _Driver.FormatNullableString(assistantSettings.LogoUrl) + ", " +
                _Driver.FormatNullableString(assistantSettings.FaviconUrl) + ", " +
                _Driver.FormatNullableString(assistantSettings.RetrievalLabelFilter) + ", " +
                _Driver.FormatNullableString(assistantSettings.RetrievalTagFilter) + ", " +
                (assistantSettings.Streaming ? 1 : 0) + ", " +
                FormatBooleanColumn(assistantSettings.EnableSlack) + ", " +
                _Driver.FormatNullableString(assistantSettings.SlackAppToken) + ", " +
                _Driver.FormatNullableString(assistantSettings.SlackBotToken) + ", " +
                _Driver.FormatNullableString(assistantSettings.SlackChannelId) + ", " +
                _Driver.FormatNullableString(assistantSettings.SlackMessagePrefix) + ", " +
                _Driver.FormatNullableString(assistantSettings.ToolPolicyJson) + ", " +
                "'" + _Driver.FormatDateTime(assistantSettings.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(assistantSettings.LastUpdateUtc) + "'" +
                ")";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return assistantSettings;
        }

        /// <inheritdoc />
        public async Task<AssistantSettings> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM assistant_settings WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return AssistantSettings.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<AssistantSettings> ReadByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(assistantId)) throw new ArgumentNullException(nameof(assistantId));

            string query = "SELECT * FROM assistant_settings WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return AssistantSettings.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<AssistantSettings> UpdateAsync(AssistantSettings assistantSettings, CancellationToken token = default)
        {
            if (assistantSettings == null) throw new ArgumentNullException(nameof(assistantSettings));

            assistantSettings.LastUpdateUtc = DateTime.UtcNow;

            string query =
                "UPDATE assistant_settings SET " +
                "assistant_id = '" + _Driver.Sanitize(assistantSettings.AssistantId) + "', " +
                "temperature = " + _Driver.FormatDouble(assistantSettings.Temperature) + ", " +
                "top_p = " + _Driver.FormatDouble(assistantSettings.TopP) + ", " +
                "system_prompt = " + _Driver.FormatNullableString(assistantSettings.SystemPrompt) + ", " +
                "max_tokens = " + assistantSettings.MaxTokens + ", " +
                "context_window = " + assistantSettings.ContextWindow + ", " +
                "enable_rag = " + (assistantSettings.EnableRag ? 1 : 0) + ", " +
                "enable_retrieval_gate = " + (assistantSettings.EnableRetrievalGate ? 1 : 0) + ", " +
                "enable_query_rewrite = " + FormatBooleanColumn(assistantSettings.EnableQueryRewrite) + ", " +
                "query_rewrite_prompt = " + _Driver.FormatNullableString(assistantSettings.QueryRewritePrompt) + ", " +
                "enable_reranking = " + FormatBooleanColumn(assistantSettings.EnableReranking) + ", " +
                "reranker_top_k = " + assistantSettings.RerankerTopK + ", " +
                "reranker_score_threshold = " + _Driver.FormatDouble(assistantSettings.RerankerScoreThreshold) + ", " +
                "rerank_prompt = " + _Driver.FormatNullableString(assistantSettings.RerankPrompt) + ", " +
                "enable_citations = " + (assistantSettings.EnableCitations ? 1 : 0) + ", " +
                "citation_link_mode = " + _Driver.FormatNullableString(assistantSettings.CitationLinkMode) + ", " +
                "enable_document_attachments = " + FormatBooleanColumn(assistantSettings.EnableDocumentAttachments) + ", " +
                "document_attachment_max_count = " + assistantSettings.DocumentAttachmentMaxCount + ", " +
                "expose_document_source_urls = " + FormatBooleanColumn(assistantSettings.ExposeDocumentSourceUrls) + ", " +
                "collection_id = " + _Driver.FormatNullableString(assistantSettings.CollectionId) + ", " +
                "retrieval_top_k = " + assistantSettings.RetrievalTopK + ", " +
                "retrieval_score_threshold = " + _Driver.FormatDouble(assistantSettings.RetrievalScoreThreshold) + ", " +
                "search_mode = " + _Driver.FormatNullableString(assistantSettings.SearchMode) + ", " +
                "text_weight = " + _Driver.FormatDouble(assistantSettings.TextWeight) + ", " +
                "fulltext_search_type = " + _Driver.FormatNullableString(assistantSettings.FullTextSearchType) + ", " +
                "fulltext_language = " + _Driver.FormatNullableString(assistantSettings.FullTextLanguage) + ", " +
                "fulltext_normalization = " + assistantSettings.FullTextNormalization + ", " +
                "fulltext_minimum_score = " + (assistantSettings.FullTextMinimumScore.HasValue ? _Driver.FormatDouble(assistantSettings.FullTextMinimumScore.Value) : "NULL") + ", " +
                "retrieval_include_neighbors = " + assistantSettings.RetrievalIncludeNeighbors + ", " +
                "inference_endpoint_id = " + _Driver.FormatNullableString(assistantSettings.InferenceEndpointId) + ", " +
                "tool_routing_inference_endpoint_id = " + _Driver.FormatNullableString(assistantSettings.ToolRoutingInferenceEndpointId) + ", " +
                "retrieval_gate_inference_endpoint_id = " + _Driver.FormatNullableString(assistantSettings.RetrievalGateInferenceEndpointId) + ", " +
                "query_rewrite_inference_endpoint_id = " + _Driver.FormatNullableString(assistantSettings.QueryRewriteInferenceEndpointId) + ", " +
                "rerank_inference_endpoint_id = " + _Driver.FormatNullableString(assistantSettings.RerankInferenceEndpointId) + ", " +
                "enable_answerability_check = " + FormatBooleanColumn(assistantSettings.EnableAnswerabilityCheck) + ", " +
                "answerability_inference_endpoint_id = " + _Driver.FormatNullableString(assistantSettings.AnswerabilityInferenceEndpointId) + ", " +
                "answerability_mode = " + _Driver.FormatNullableString(assistantSettings.AnswerabilityMode) + ", " +
                "answerability_prompt = " + _Driver.FormatNullableString(assistantSettings.AnswerabilityPrompt) + ", " +
                "embedding_endpoint_id = " + _Driver.FormatNullableString(assistantSettings.EmbeddingEndpointId) + ", " +
                "load_models_on_chat_open = " + FormatBooleanColumn(assistantSettings.LoadModelsOnChatOpen) + ", " +
                "expose_thinking = " + FormatBooleanColumn(assistantSettings.ExposeThinking) + ", " +
                "title = " + _Driver.FormatNullableString(assistantSettings.Title) + ", " +
                "logo_url = " + _Driver.FormatNullableString(assistantSettings.LogoUrl) + ", " +
                "favicon_url = " + _Driver.FormatNullableString(assistantSettings.FaviconUrl) + ", " +
                "retrieval_label_filter = " + _Driver.FormatNullableString(assistantSettings.RetrievalLabelFilter) + ", " +
                "retrieval_tag_filter = " + _Driver.FormatNullableString(assistantSettings.RetrievalTagFilter) + ", " +
                "streaming = " + (assistantSettings.Streaming ? 1 : 0) + ", " +
                "enable_slack = " + FormatBooleanColumn(assistantSettings.EnableSlack) + ", " +
                "slack_app_token = " + _Driver.FormatNullableString(assistantSettings.SlackAppToken) + ", " +
                "slack_bot_token = " + _Driver.FormatNullableString(assistantSettings.SlackBotToken) + ", " +
                "slack_channel_id = " + _Driver.FormatNullableString(assistantSettings.SlackChannelId) + ", " +
                "slack_message_prefix = " + _Driver.FormatNullableString(assistantSettings.SlackMessagePrefix) + ", " +
                "tool_policy_json = " + _Driver.FormatNullableString(assistantSettings.ToolPolicyJson) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(assistantSettings.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(assistantSettings.Id) + "'";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return assistantSettings;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM assistant_settings WHERE id = '" + _Driver.Sanitize(id) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(assistantId)) throw new ArgumentNullException(nameof(assistantId));

            string query = "DELETE FROM assistant_settings WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private static string FormatBooleanColumn(bool value)
        {
            return value ? "TRUE" : "FALSE";
        }

        #endregion
    }
}
