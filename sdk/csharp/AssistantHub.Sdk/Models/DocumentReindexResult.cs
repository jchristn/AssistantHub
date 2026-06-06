namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Result for a single document Verbex reindex operation.
    /// </summary>
    public class DocumentReindexResult
    {
        /// <summary>
        /// AssistantHub document identifier.
        /// </summary>
        [JsonPropertyName("DocumentId")]
        public string DocumentId { get; set; }

        /// <summary>
        /// Whether the reindex operation succeeded or was intentionally skipped.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Operation status.
        /// </summary>
        [JsonPropertyName("Status")]
        public string Status { get; set; }

        /// <summary>
        /// Human-readable operation result.
        /// </summary>
        [JsonPropertyName("Message")]
        public string Message { get; set; }

        /// <summary>
        /// Verbex tenant identifier used for indexing.
        /// </summary>
        [JsonPropertyName("VerbexTenantId")]
        public string VerbexTenantId { get; set; }

        /// <summary>
        /// Verbex index identifier used for indexing.
        /// </summary>
        [JsonPropertyName("VerbexIndexId")]
        public string VerbexIndexId { get; set; }

        /// <summary>
        /// Verbex record identifier used for indexing.
        /// </summary>
        [JsonPropertyName("VerbexRecordId")]
        public string VerbexRecordId { get; set; }

        /// <summary>
        /// Total milliseconds elapsed.
        /// </summary>
        [JsonPropertyName("TotalMs")]
        public double TotalMs { get; set; }
    }
}
