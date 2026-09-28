namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Summary of a cross-encoder reranker configured in server settings (API key omitted).
    /// </summary>
    public class RerankerSummary
    {
        /// <summary>
        /// Reranker identifier.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Display name.
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Wire format: "Tei" or "Cohere".
        /// </summary>
        [JsonPropertyName("Format")]
        public string Format { get; set; }

        /// <summary>
        /// Rerank endpoint URL.
        /// </summary>
        [JsonPropertyName("Endpoint")]
        public string Endpoint { get; set; }

        /// <summary>
        /// Model name, when the format requires one.
        /// </summary>
        [JsonPropertyName("Model")]
        public string Model { get; set; }

        /// <summary>
        /// Request timeout in milliseconds.
        /// </summary>
        [JsonPropertyName("TimeoutMs")]
        public int TimeoutMs { get; set; }

        /// <summary>
        /// Whether an API key is configured for this reranker.
        /// </summary>
        [JsonPropertyName("HasApiKey")]
        public bool HasApiKey { get; set; }
    }
}
