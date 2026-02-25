namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Chat history record representing a single turn in a conversation thread.
    /// </summary>
    public class ChatHistory
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix chist_.
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId
        {
            get => _TenantId;
            set => _TenantId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(TenantId));
        }

        /// <summary>
        /// Thread identifier with prefix thr_.
        /// </summary>
        public string ThreadId
        {
            get => _ThreadId;
            set => _ThreadId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(ThreadId));
        }

        /// <summary>
        /// Assistant identifier to which this history belongs.
        /// </summary>
        public string AssistantId
        {
            get => _AssistantId;
            set => _AssistantId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(AssistantId));
        }

        /// <summary>
        /// Collection identifier from assistant settings.
        /// </summary>
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// UTC timestamp of user message receipt.
        /// </summary>
        public DateTime UserMessageUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The user message content.
        /// </summary>
        public string UserMessage { get; set; } = null;

        /// <summary>
        /// UTC timestamp when retrieval started.
        /// </summary>
        public DateTime? RetrievalStartUtc { get; set; } = null;

        /// <summary>
        /// Duration of retrieval in milliseconds.
        /// </summary>
        public double RetrievalDurationMs { get; set; } = 0;

        /// <summary>
        /// Retrieval gate decision: "RETRIEVE", "SKIP", or null (gate disabled).
        /// </summary>
        public string RetrievalGateDecision { get; set; } = null;

        /// <summary>
        /// Duration of the retrieval gate LLM call in milliseconds.
        /// </summary>
        public double RetrievalGateDurationMs { get; set; } = 0;

        /// <summary>
        /// Text retrieved from the vector database.
        /// </summary>
        public string RetrievalContext { get; set; } = null;

        /// <summary>
        /// UTC timestamp when the prompt was sent to the model.
        /// </summary>
        public DateTime? PromptSentUtc { get; set; } = null;

        /// <summary>
        /// Estimated prompt token count sent to the model.
        /// </summary>
        public int PromptTokens { get; set; } = 0;

        /// <summary>
        /// Duration of endpoint resolution in milliseconds (HTTP GET to Partio for endpoint details).
        /// </summary>
        public double EndpointResolutionDurationMs { get; set; } = 0;

        /// <summary>
        /// Duration of conversation compaction in milliseconds (0 if skipped).
        /// </summary>
        public double CompactionDurationMs { get; set; } = 0;

        /// <summary>
        /// Time from sending the HTTP request to receiving response headers in milliseconds.
        /// Measures network latency and model loading time, separate from prompt processing.
        /// </summary>
        public double InferenceConnectionDurationMs { get; set; } = 0;

        /// <summary>
        /// Time to first token from the model in milliseconds.
        /// </summary>
        public double TimeToFirstTokenMs { get; set; } = 0;

        /// <summary>
        /// Time to last token from the model in milliseconds.
        /// </summary>
        public double TimeToLastTokenMs { get; set; } = 0;

        /// <summary>
        /// Estimated completion (response) token count from the model.
        /// </summary>
        public int CompletionTokens { get; set; } = 0;

        /// <summary>
        /// Tokens per second (overall): CompletionTokens / (TimeToLastTokenMs / 1000).
        /// Measures end-to-end generation throughput from prompt sent to last token.
        /// </summary>
        public double TokensPerSecondOverall { get; set; } = 0;

        /// <summary>
        /// Tokens per second (generation only): CompletionTokens / ((TimeToLastTokenMs - TimeToFirstTokenMs) / 1000).
        /// Measures pure token generation throughput excluding prompt processing.
        /// </summary>
        public double TokensPerSecondGeneration { get; set; } = 0;

        /// <summary>
        /// The assistant response content.
        /// </summary>
        public string AssistantResponse { get; set; } = null;

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the record was last updated in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.NewChatHistoryId();
        private string _TenantId = Constants.DefaultTenantId;
        private string _ThreadId = "thr_placeholder";
        private string _AssistantId = "asst_placeholder";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatHistory()
        {
        }

        /// <summary>
        /// Create a ChatHistory from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>ChatHistory instance or null.</returns>
        public static ChatHistory FromDataRow(DataRow row)
        {
            if (row == null) return null;
            ChatHistory obj = new ChatHistory();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.TenantId = DataTableHelper.GetStringValue(row, "tenant_id");
            obj.ThreadId = DataTableHelper.GetStringValue(row, "thread_id");
            obj.AssistantId = DataTableHelper.GetStringValue(row, "assistant_id");
            obj.CollectionId = DataTableHelper.GetStringValue(row, "collection_id");
            obj.UserMessageUtc = DataTableHelper.GetDateTimeValue(row, "user_message_utc");
            obj.UserMessage = DataTableHelper.GetStringValue(row, "user_message");
            obj.RetrievalStartUtc = DataTableHelper.GetNullableDateTimeValue(row, "retrieval_start_utc");
            obj.RetrievalDurationMs = DataTableHelper.GetDoubleValue(row, "retrieval_duration_ms");
            obj.RetrievalGateDecision = DataTableHelper.GetStringValue(row, "retrieval_gate_decision");
            obj.RetrievalGateDurationMs = DataTableHelper.GetDoubleValue(row, "retrieval_gate_duration_ms");
            obj.RetrievalContext = DataTableHelper.GetStringValue(row, "retrieval_context");
            obj.PromptSentUtc = DataTableHelper.GetNullableDateTimeValue(row, "prompt_sent_utc");
            obj.PromptTokens = DataTableHelper.GetIntValue(row, "prompt_tokens");
            obj.EndpointResolutionDurationMs = DataTableHelper.GetDoubleValue(row, "endpoint_resolution_duration_ms");
            obj.CompactionDurationMs = DataTableHelper.GetDoubleValue(row, "compaction_duration_ms");
            obj.InferenceConnectionDurationMs = DataTableHelper.GetDoubleValue(row, "inference_connection_duration_ms");
            obj.TimeToFirstTokenMs = DataTableHelper.GetDoubleValue(row, "time_to_first_token_ms");
            obj.TimeToLastTokenMs = DataTableHelper.GetDoubleValue(row, "time_to_last_token_ms");
            obj.CompletionTokens = DataTableHelper.GetIntValue(row, "completion_tokens");
            obj.TokensPerSecondOverall = DataTableHelper.GetDoubleValue(row, "tokens_per_second_overall");
            obj.TokensPerSecondGeneration = DataTableHelper.GetDoubleValue(row, "tokens_per_second_generation");
            obj.AssistantResponse = DataTableHelper.GetStringValue(row, "assistant_response");
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion
    }
}
