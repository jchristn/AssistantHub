namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Ingestion extraction configuration.
    /// </summary>
    public class IngestionExtractionConfig
    {
        /// <summary>
        /// Whether to OCR images embedded in documents. Null uses the server default.
        /// </summary>
        [JsonPropertyName("OcrEmbeddedImages")]
        public bool? OcrEmbeddedImages { get; set; }

        /// <summary>
        /// Whether CSV/TSV files have a header row. Null uses the server default.
        /// </summary>
        [JsonPropertyName("CsvHasHeaderRow")]
        public bool? CsvHasHeaderRow { get; set; }

        /// <summary>
        /// Number of CSV/TSV rows per extracted atom. Null uses the server default.
        /// </summary>
        [JsonPropertyName("CsvRowsPerAtom")]
        public int? CsvRowsPerAtom { get; set; }

        /// <summary>
        /// Score threshold for detecting Excel header rows. Null uses the server default.
        /// </summary>
        [JsonPropertyName("ExcelHeaderRowScoreThreshold")]
        public int? ExcelHeaderRowScoreThreshold { get; set; }

        /// <summary>
        /// Duplicate policy: "Allow", "Warn", or "Reject".
        /// </summary>
        [JsonPropertyName("DuplicatePolicy")]
        public string DuplicatePolicy { get; set; } = "Allow";

        /// <summary>
        /// Similarity threshold (0.0 to 1.0) above which documents are treated as near-duplicates.
        /// </summary>
        [JsonPropertyName("NearDuplicateThreshold")]
        public double NearDuplicateThreshold { get; set; } = 0.85;
    }
}
