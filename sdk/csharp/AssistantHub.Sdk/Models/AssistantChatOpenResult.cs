namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Result returned when a chat window is opened for an assistant.
    /// </summary>
    public class AssistantChatOpenResult
    {
        /// <summary>
        /// Whether all configured model-load requests succeeded.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Whether chat-open model loading is enabled for the assistant.
        /// </summary>
        [JsonPropertyName("Enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Whether at least one configured endpoint model was loaded.
        /// </summary>
        [JsonPropertyName("Loaded")]
        public bool Loaded { get; set; }

        /// <summary>
        /// Number of completion endpoints considered.
        /// </summary>
        [JsonPropertyName("CompletionEndpointCount")]
        public int CompletionEndpointCount { get; set; }

        /// <summary>
        /// Number of embedding endpoints considered.
        /// </summary>
        [JsonPropertyName("EmbeddingEndpointCount")]
        public int EmbeddingEndpointCount { get; set; }

        /// <summary>
        /// Per-endpoint model-load results.
        /// </summary>
        [JsonPropertyName("Results")]
        public List<AssistantChatOpenModelLoadResult> Results { get; set; } = new List<AssistantChatOpenModelLoadResult>();
    }
}
