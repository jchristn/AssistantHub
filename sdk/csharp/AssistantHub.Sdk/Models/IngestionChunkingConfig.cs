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

        /// <summary>
        /// Cell mode: "Flat" or "Structured".
        /// </summary>
        [JsonPropertyName("CellMode")]
        public string CellMode { get; set; }

        /// <summary>
        /// Table strategy: "Row", "RowWithHeaders", "RowGroupWithHeaders", "KeyValuePairs", or "WholeTable".
        /// </summary>
        [JsonPropertyName("TableStrategy")]
        public string TableStrategy { get; set; }

        /// <summary>
        /// List strategy: "WholeList" or "ListEntry".
        /// </summary>
        [JsonPropertyName("ListStrategy")]
        public string ListStrategy { get; set; }

        /// <summary>
        /// Context header prepended to chunks: "None", "Title", or "TitleAndHeadings".
        /// </summary>
        [JsonPropertyName("ContextHeader")]
        public string ContextHeader { get; set; }
    }
}
