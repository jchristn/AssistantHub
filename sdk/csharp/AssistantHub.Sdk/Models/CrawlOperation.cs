namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Crawl operation record representing a single crawl execution.
    /// </summary>
    public class CrawlOperation
    {
        /// <summary>
        /// Unique identifier with prefix cop_.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Associated crawl plan identifier.
        /// </summary>
        [JsonPropertyName("CrawlPlanId")]
        public string CrawlPlanId { get; set; }

        /// <summary>
        /// Current state of the operation.
        /// </summary>
        [JsonPropertyName("State")]
        public CrawlOperationStateEnum State { get; set; }

        /// <summary>
        /// Status message providing additional details.
        /// </summary>
        [JsonPropertyName("StatusMessage")]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Number of objects enumerated.
        /// </summary>
        [JsonPropertyName("ObjectsEnumerated")]
        public long ObjectsEnumerated { get; set; }

        /// <summary>
        /// Total bytes enumerated.
        /// </summary>
        [JsonPropertyName("BytesEnumerated")]
        public long BytesEnumerated { get; set; }

        /// <summary>
        /// Number of objects added.
        /// </summary>
        [JsonPropertyName("ObjectsAdded")]
        public long ObjectsAdded { get; set; }

        /// <summary>
        /// Total bytes added.
        /// </summary>
        [JsonPropertyName("BytesAdded")]
        public long BytesAdded { get; set; }

        /// <summary>
        /// Number of objects updated.
        /// </summary>
        [JsonPropertyName("ObjectsUpdated")]
        public long ObjectsUpdated { get; set; }

        /// <summary>
        /// Total bytes updated.
        /// </summary>
        [JsonPropertyName("BytesUpdated")]
        public long BytesUpdated { get; set; }

        /// <summary>
        /// Number of objects deleted.
        /// </summary>
        [JsonPropertyName("ObjectsDeleted")]
        public long ObjectsDeleted { get; set; }

        /// <summary>
        /// Total bytes deleted.
        /// </summary>
        [JsonPropertyName("BytesDeleted")]
        public long BytesDeleted { get; set; }

        /// <summary>
        /// Number of objects successfully processed.
        /// </summary>
        [JsonPropertyName("ObjectsSuccess")]
        public long ObjectsSuccess { get; set; }

        /// <summary>
        /// Total bytes successfully processed.
        /// </summary>
        [JsonPropertyName("BytesSuccess")]
        public long BytesSuccess { get; set; }

        /// <summary>
        /// Number of objects that failed processing.
        /// </summary>
        [JsonPropertyName("ObjectsFailed")]
        public long ObjectsFailed { get; set; }

        /// <summary>
        /// Total bytes that failed processing.
        /// </summary>
        [JsonPropertyName("BytesFailed")]
        public long BytesFailed { get; set; }

        /// <summary>
        /// Path to the enumeration JSON file on disk.
        /// </summary>
        [JsonPropertyName("EnumerationFile")]
        public string EnumerationFile { get; set; }

        /// <summary>
        /// Operation start timestamp in UTC.
        /// </summary>
        [JsonPropertyName("StartUtc")]
        public DateTime? StartUtc { get; set; }

        /// <summary>
        /// Enumeration start timestamp in UTC.
        /// </summary>
        [JsonPropertyName("StartEnumerationUtc")]
        public DateTime? StartEnumerationUtc { get; set; }

        /// <summary>
        /// Enumeration finish timestamp in UTC.
        /// </summary>
        [JsonPropertyName("FinishEnumerationUtc")]
        public DateTime? FinishEnumerationUtc { get; set; }

        /// <summary>
        /// Retrieval start timestamp in UTC.
        /// </summary>
        [JsonPropertyName("StartRetrievalUtc")]
        public DateTime? StartRetrievalUtc { get; set; }

        /// <summary>
        /// Retrieval finish timestamp in UTC.
        /// </summary>
        [JsonPropertyName("FinishRetrievalUtc")]
        public DateTime? FinishRetrievalUtc { get; set; }

        /// <summary>
        /// Operation finish timestamp in UTC.
        /// </summary>
        [JsonPropertyName("FinishUtc")]
        public DateTime? FinishUtc { get; set; }

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        [JsonPropertyName("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated in UTC.
        /// </summary>
        [JsonPropertyName("LastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; }
    }
}
