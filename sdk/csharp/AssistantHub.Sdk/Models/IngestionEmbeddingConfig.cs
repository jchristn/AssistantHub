namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Ingestion embedding configuration.
    /// </summary>
    public class IngestionEmbeddingConfig
    {
        /// <summary>
        /// Embedding endpoint identifier.
        /// </summary>
        [JsonPropertyName("EmbeddingEndpointId")]
        public string EmbeddingEndpointId { get; set; }

        /// <summary>
        /// Whether to apply L2 normalization.
        /// </summary>
        [JsonPropertyName("L2Normalization")]
        public bool L2Normalization { get; set; }
    }
}
