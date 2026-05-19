namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A single persisted chat history turn.
    /// </summary>
    public class ChatHistory
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        [JsonPropertyName("ThreadId")]
        public string ThreadId { get; set; }

        [JsonPropertyName("AssistantId")]
        public string AssistantId { get; set; }

        [JsonPropertyName("CollectionId")]
        public string CollectionId { get; set; }

        [JsonPropertyName("UserMessageUtc")]
        public DateTime UserMessageUtc { get; set; }

        [JsonPropertyName("UserMessage")]
        public string UserMessage { get; set; }

        [JsonPropertyName("RetrievalStartUtc")]
        public DateTime? RetrievalStartUtc { get; set; }

        [JsonPropertyName("RetrievalDurationMs")]
        public double RetrievalDurationMs { get; set; }

        [JsonPropertyName("RetrievalGateDecision")]
        public string RetrievalGateDecision { get; set; }

        [JsonPropertyName("RetrievalGateDurationMs")]
        public double RetrievalGateDurationMs { get; set; }

        [JsonPropertyName("QueryRewriteResult")]
        public string QueryRewriteResult { get; set; }

        [JsonPropertyName("QueryRewriteDurationMs")]
        public double QueryRewriteDurationMs { get; set; }

        [JsonPropertyName("RerankDurationMs")]
        public double RerankDurationMs { get; set; }

        [JsonPropertyName("RerankInputCount")]
        public int RerankInputCount { get; set; }

        [JsonPropertyName("RerankOutputCount")]
        public int RerankOutputCount { get; set; }

        [JsonPropertyName("RetrievalContext")]
        public string RetrievalContext { get; set; }

        [JsonPropertyName("PromptSentUtc")]
        public DateTime? PromptSentUtc { get; set; }

        [JsonPropertyName("PromptTokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("EndpointResolutionDurationMs")]
        public double EndpointResolutionDurationMs { get; set; }

        [JsonPropertyName("CompactionDurationMs")]
        public double CompactionDurationMs { get; set; }

        [JsonPropertyName("InferenceConnectionDurationMs")]
        public double InferenceConnectionDurationMs { get; set; }

        [JsonPropertyName("TimeToFirstTokenMs")]
        public double TimeToFirstTokenMs { get; set; }

        [JsonPropertyName("TimeToLastTokenMs")]
        public double TimeToLastTokenMs { get; set; }

        [JsonPropertyName("CompletionTokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("TokensPerSecondOverall")]
        public double TokensPerSecondOverall { get; set; }

        [JsonPropertyName("TokensPerSecondGeneration")]
        public double TokensPerSecondGeneration { get; set; }

        [JsonPropertyName("MetadataFilter")]
        public string MetadataFilter { get; set; }

        [JsonPropertyName("Origin")]
        public string Origin { get; set; }

        [JsonPropertyName("AssistantResponse")]
        public string AssistantResponse { get; set; }

        [JsonPropertyName("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        [JsonPropertyName("LastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; }
    }
}
