namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Completion endpoint test response.
    /// </summary>
    public class EndpointExplorerCompletionResponse
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
        /// Completion endpoint identifier.
        /// </summary>
        [JsonPropertyName("EndpointId")]
        public string EndpointId { get; set; }

        /// <summary>
        /// Model used by the endpoint.
        /// </summary>
        [JsonPropertyName("Model")]
        public string Model { get; set; }

        /// <summary>
        /// Prompt sent to the endpoint.
        /// </summary>
        [JsonPropertyName("Prompt")]
        public string Prompt { get; set; }

        /// <summary>
        /// System prompt sent to the endpoint.
        /// </summary>
        [JsonPropertyName("SystemPrompt")]
        public string SystemPrompt { get; set; }

        /// <summary>
        /// Completion output.
        /// </summary>
        [JsonPropertyName("Output")]
        public string Output { get; set; }

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
        /// Raw completion call telemetry entries.
        /// </summary>
        [JsonPropertyName("CompletionCalls")]
        public List<JsonElement> CompletionCalls { get; set; }
    }
}
