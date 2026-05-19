namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request body for testing a completion endpoint.
    /// </summary>
    public class EndpointExplorerCompletionRequest
    {
        [JsonPropertyName("EndpointId")]
        public string EndpointId { get; set; }

        [JsonPropertyName("Prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("SystemPrompt")]
        public string SystemPrompt { get; set; }

        [JsonPropertyName("MaxTokens")]
        public int MaxTokens { get; set; } = 512;

        [JsonPropertyName("TimeoutMs")]
        public int TimeoutMs { get; set; } = 60000;
    }

    /// <summary>
    /// Completion endpoint test response.
    /// </summary>
    public class EndpointExplorerCompletionResponse
    {
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        [JsonPropertyName("StatusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("Error")]
        public string Error { get; set; }

        [JsonPropertyName("EndpointId")]
        public string EndpointId { get; set; }

        [JsonPropertyName("Model")]
        public string Model { get; set; }

        [JsonPropertyName("Prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("SystemPrompt")]
        public string SystemPrompt { get; set; }

        [JsonPropertyName("Output")]
        public string Output { get; set; }

        [JsonPropertyName("ResponseTimeMs")]
        public long ResponseTimeMs { get; set; }

        [JsonPropertyName("RequestHistoryId")]
        public string RequestHistoryId { get; set; }

        [JsonPropertyName("CompletionCalls")]
        public List<JsonElement> CompletionCalls { get; set; }
    }

    /// <summary>
    /// Request body for testing an embedding endpoint.
    /// </summary>
    public class EndpointExplorerEmbeddingRequest
    {
        [JsonPropertyName("EndpointId")]
        public string EndpointId { get; set; }

        [JsonPropertyName("Input")]
        public string Input { get; set; }

        [JsonPropertyName("L2Normalization")]
        public bool L2Normalization { get; set; }
    }

    /// <summary>
    /// Embedding endpoint test response.
    /// </summary>
    public class EndpointExplorerEmbeddingResponse
    {
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        [JsonPropertyName("StatusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("Error")]
        public string Error { get; set; }

        [JsonPropertyName("EndpointId")]
        public string EndpointId { get; set; }

        [JsonPropertyName("Model")]
        public string Model { get; set; }

        [JsonPropertyName("Input")]
        public string Input { get; set; }

        [JsonPropertyName("Embedding")]
        public List<float> Embedding { get; set; }

        [JsonPropertyName("Dimensions")]
        public int Dimensions { get; set; }

        [JsonPropertyName("ResponseTimeMs")]
        public long ResponseTimeMs { get; set; }

        [JsonPropertyName("RequestHistoryId")]
        public string RequestHistoryId { get; set; }

        [JsonPropertyName("EmbeddingCalls")]
        public List<JsonElement> EmbeddingCalls { get; set; }
    }
}
