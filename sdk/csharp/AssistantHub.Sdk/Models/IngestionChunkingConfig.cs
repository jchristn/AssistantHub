namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Ingestion chunking configuration.
    /// </summary>
    public class IngestionChunkingConfig
    {
        /// <summary>
        /// Chunking strategy.
        /// </summary>
        [JsonPropertyName("Strategy")]
        public string Strategy { get; set; }

        /// <summary>
        /// Fixed token count per chunk.
        /// </summary>
        [JsonPropertyName("FixedTokenCount")]
        public int FixedTokenCount { get; set; }

        /// <summary>
        /// Overlap count.
        /// </summary>
        [JsonPropertyName("OverlapCount")]
        public int OverlapCount { get; set; }

        /// <summary>
        /// Overlap percentage (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("OverlapPercentage")]
        public double OverlapPercentage { get; set; }

        /// <summary>
        /// Overlap strategy.
        /// </summary>
        [JsonPropertyName("OverlapStrategy")]
        public string OverlapStrategy { get; set; }

        /// <summary>
        /// Row group size.
        /// </summary>
        [JsonPropertyName("RowGroupSize")]
        public int RowGroupSize { get; set; }

        /// <summary>
        /// Context prefix.
        /// </summary>
        [JsonPropertyName("ContextPrefix")]
        public string ContextPrefix { get; set; }

        /// <summary>
        /// Regex pattern.
        /// </summary>
        [JsonPropertyName("RegexPattern")]
        public string RegexPattern { get; set; }
    }
}
