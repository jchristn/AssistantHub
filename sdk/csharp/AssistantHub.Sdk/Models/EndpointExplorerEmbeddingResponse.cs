namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Embedding endpoint test response.
    /// </summary>
    public class EndpointExplorerEmbeddingResponse
    {
        /// <summary>
        /// True when the endpoint test succeeded.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// HTTP status code returned by the test.
        /// </summary>
        [JsonPropertyName("StatusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// Error message when the test fails.
        /// </summary>
        [JsonPropertyName("Error")]
        public string Error { get; set; }

        /// <summary>
        /// Embedding endpoint identifier.
        /// </summary>
        [JsonPropertyName("EndpointId")]
        public string EndpointId { get; set; }

        /// <summary>
        /// Model used by the endpoint.
        /// </summary>
        [JsonPropertyName("Model")]
        public string Model { get; set; }

        /// <summary>
        /// Input text sent to the endpoint.
        /// </summary>
        [JsonPropertyName("Input")]
        public string Input { get; set; }

        /// <summary>
        /// Embedding vector returned by the endpoint.
        /// </summary>
        [JsonPropertyName("Embedding")]
        public List<float> Embedding { get; set; }

        /// <summary>
        /// Embedding vector dimensionality.
        /// </summary>
        [JsonPropertyName("Dimensions")]
        public int Dimensions { get; set; }

        /// <summary>
        /// End-to-end response time in milliseconds.
        /// </summary>
        [JsonPropertyName("ResponseTimeMs")]
        public long ResponseTimeMs { get; set; }

        /// <summary>
        /// Request-history identifier created by the test.
        /// </summary>
        [JsonPropertyName("RequestHistoryId")]
        public string RequestHistoryId { get; set; }

        /// <summary>
        /// Raw embedding call telemetry entries.
        /// </summary>
        [JsonPropertyName("EmbeddingCalls")]
        public List<JsonElement> EmbeddingCalls { get; set; }
    }
}
