namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request body for testing an embedding endpoint.
    /// </summary>
    public class EndpointExplorerEmbeddingRequest
    {
        /// <summary>
        /// Embedding endpoint identifier.
        /// </summary>
        [JsonPropertyName("EndpointId")]
        public string EndpointId { get; set; }

        /// <summary>
        /// Input text to embed.
        /// </summary>
        [JsonPropertyName("Input")]
        public string Input { get; set; }

        /// <summary>
        /// True to apply L2 normalization to the embedding.
        /// </summary>
        [JsonPropertyName("L2Normalization")]
        public bool L2Normalization { get; set; }
    }
}
