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
        /// <summary>
        /// Completion endpoint identifier.
        /// </summary>
        [JsonPropertyName("EndpointId")]
        public string EndpointId { get; set; }

        /// <summary>
        /// User prompt to send to the endpoint.
        /// </summary>
        [JsonPropertyName("Prompt")]
        public string Prompt { get; set; }

        /// <summary>
        /// Optional system prompt.
        /// </summary>
        [JsonPropertyName("SystemPrompt")]
        public string SystemPrompt { get; set; }

        /// <summary>
        /// Maximum tokens to request.
        /// </summary>
        [JsonPropertyName("MaxTokens")]
        public int MaxTokens { get; set; } = 512;

        /// <summary>
        /// Request timeout in milliseconds.
        /// </summary>
        [JsonPropertyName("TimeoutMs")]
        public int TimeoutMs { get; set; } = 60000;
    }
}
