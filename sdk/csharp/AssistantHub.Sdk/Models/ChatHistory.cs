namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A single persisted chat history turn.
    /// </summary>
    public class ChatHistory
    {
        /// <summary>
        /// Chat history identifier.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Correlation identifier shared by request history and telemetry events.
        /// </summary>
        [JsonPropertyName("TraceId")]
        public string TraceId { get; set; }

        /// <summary>
        /// Request-history identifier associated with this chat turn, when available.
        /// </summary>
        [JsonPropertyName("RequestHistoryId")]
        public string RequestHistoryId { get; set; }

        /// <summary>
        /// Performance telemetry schema version.
        /// </summary>
        [JsonPropertyName("PerformanceSchemaVersion")]
        public int PerformanceSchemaVersion { get; set; }

        /// <summary>
        /// JSON-serialized provider-agnostic performance telemetry.
        /// </summary>
        [JsonPropertyName("PerformanceJson")]
        public string PerformanceJson { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Thread identifier.
        /// </summary>
        [JsonPropertyName("ThreadId")]
        public string ThreadId { get; set; }

        /// <summary>
        /// Assistant identifier.
        /// </summary>
        [JsonPropertyName("AssistantId")]
        public string AssistantId { get; set; }

        /// <summary>
        /// Collection identifier from assistant settings.
        /// </summary>
        [JsonPropertyName("CollectionId")]
        public string CollectionId { get; set; }

        /// <summary>
        /// UTC timestamp of user message receipt.
        /// </summary>
        [JsonPropertyName("UserMessageUtc")]
        public DateTime UserMessageUtc { get; set; }

        /// <summary>
        /// The user message content.
        /// </summary>
        [JsonPropertyName("UserMessage")]
        public string UserMessage { get; set; }

        /// <summary>
        /// UTC timestamp when retrieval started.
        /// </summary>
        [JsonPropertyName("RetrievalStartUtc")]
        public DateTime? RetrievalStartUtc { get; set; }

        /// <summary>
        /// Duration of retrieval in milliseconds.
        /// </summary>
        [JsonPropertyName("RetrievalDurationMs")]
        public double RetrievalDurationMs { get; set; }

        /// <summary>
        /// Retrieval gate decision.
        /// </summary>
        [JsonPropertyName("RetrievalGateDecision")]
        public string RetrievalGateDecision { get; set; }

        /// <summary>
        /// Duration of the retrieval gate LLM call in milliseconds.
        /// </summary>
        [JsonPropertyName("RetrievalGateDurationMs")]
        public double RetrievalGateDurationMs { get; set; }

        /// <summary>
        /// Query rewrite output.
        /// </summary>
        [JsonPropertyName("QueryRewriteResult")]
        public string QueryRewriteResult { get; set; }

        /// <summary>
        /// Duration of the query rewrite LLM call in milliseconds.
        /// </summary>
        [JsonPropertyName("QueryRewriteDurationMs")]
        public double QueryRewriteDurationMs { get; set; }

        /// <summary>
        /// Duration of the re-ranking LLM call in milliseconds.
        /// </summary>
        [JsonPropertyName("RerankDurationMs")]
        public double RerankDurationMs { get; set; }

        /// <summary>
        /// Number of chunks sent to the re-ranker.
        /// </summary>
        [JsonPropertyName("RerankInputCount")]
        public int RerankInputCount { get; set; }

        /// <summary>
        /// Number of chunks that survived re-ranking.
        /// </summary>
        [JsonPropertyName("RerankOutputCount")]
        public int RerankOutputCount { get; set; }

        /// <summary>
        /// Text retrieved from the vector database.
        /// </summary>
        [JsonPropertyName("RetrievalContext")]
        public string RetrievalContext { get; set; }

        /// <summary>
        /// UTC timestamp when the prompt was sent to the model.
        /// </summary>
        [JsonPropertyName("PromptSentUtc")]
        public DateTime? PromptSentUtc { get; set; }

        /// <summary>
        /// Estimated prompt token count sent to the model.
        /// </summary>
        [JsonPropertyName("PromptTokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// Duration of endpoint resolution in milliseconds.
        /// </summary>
        [JsonPropertyName("EndpointResolutionDurationMs")]
        public double EndpointResolutionDurationMs { get; set; }

        /// <summary>
        /// Duration of conversation compaction in milliseconds.
        /// </summary>
        [JsonPropertyName("CompactionDurationMs")]
        public double CompactionDurationMs { get; set; }

        /// <summary>
        /// Time from sending the inference request to receiving response headers.
        /// </summary>
        [JsonPropertyName("InferenceConnectionDurationMs")]
        public double InferenceConnectionDurationMs { get; set; }

        /// <summary>
        /// Time to first token from the model in milliseconds.
        /// </summary>
        [JsonPropertyName("TimeToFirstTokenMs")]
        public double TimeToFirstTokenMs { get; set; }

        /// <summary>
        /// Time to last token from the model in milliseconds.
        /// </summary>
        [JsonPropertyName("TimeToLastTokenMs")]
        public double TimeToLastTokenMs { get; set; }

        /// <summary>
        /// Estimated completion token count.
        /// </summary>
        [JsonPropertyName("CompletionTokens")]
        public int CompletionTokens { get; set; }

        /// <summary>
        /// End-to-end completion throughput in tokens per second.
        /// </summary>
        [JsonPropertyName("TokensPerSecondOverall")]
        public double TokensPerSecondOverall { get; set; }

        /// <summary>
        /// Generation-only completion throughput in tokens per second.
        /// </summary>
        [JsonPropertyName("TokensPerSecondGeneration")]
        public double TokensPerSecondGeneration { get; set; }

        /// <summary>
        /// JSON-serialized metadata filter used during retrieval.
        /// </summary>
        [JsonPropertyName("MetadataFilter")]
        public string MetadataFilter { get; set; }

        /// <summary>
        /// Origin of the chat turn.
        /// </summary>
        [JsonPropertyName("Origin")]
        public string Origin { get; set; }

        /// <summary>
        /// The assistant response content.
        /// </summary>
        [JsonPropertyName("AssistantResponse")]
        public string AssistantResponse { get; set; }

        /// <summary>
        /// Timestamp when the record was created.
        /// </summary>
        [JsonPropertyName("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated.
        /// </summary>
        [JsonPropertyName("LastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; }
    }
}
