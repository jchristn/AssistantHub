namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Assistant document record.
    /// </summary>
    public class AssistantDocument
    {
        /// <summary>
        /// Unique identifier with prefix adoc_.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Display name for the document.
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Original filename as uploaded by the user.
        /// </summary>
        [JsonPropertyName("OriginalFilename")]
        public string OriginalFilename { get; set; }

        /// <summary>
        /// MIME content type of the document.
        /// </summary>
        [JsonPropertyName("ContentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Size of the document in bytes.
        /// </summary>
        [JsonPropertyName("SizeBytes")]
        public long SizeBytes { get; set; }

        /// <summary>
        /// S3 object key for the stored document.
        /// </summary>
        [JsonPropertyName("S3Key")]
        public string S3Key { get; set; }

        /// <summary>
        /// Processing status of the document.
        /// </summary>
        [JsonPropertyName("Status")]
        public DocumentStatusEnum Status { get; set; }

        /// <summary>
        /// Status message providing additional processing details.
        /// </summary>
        [JsonPropertyName("StatusMessage")]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Ingestion rule identifier used to process this document.
        /// </summary>
        [JsonPropertyName("IngestionRuleId")]
        public string IngestionRuleId { get; set; }

        /// <summary>
        /// S3 bucket name where the document is stored.
        /// </summary>
        [JsonPropertyName("BucketName")]
        public string BucketName { get; set; }

        /// <summary>
        /// RecallDB collection identifier for embeddings.
        /// </summary>
        [JsonPropertyName("CollectionId")]
        public string CollectionId { get; set; }

        /// <summary>
        /// Verbex tenant identifier used for full-text indexing.
        /// </summary>
        [JsonPropertyName("VerbexTenantId")]
        public string VerbexTenantId { get; set; }

        /// <summary>
        /// Verbex index identifier used for full-text indexing.
        /// </summary>
        [JsonPropertyName("VerbexIndexId")]
        public string VerbexIndexId { get; set; }

        /// <summary>
        /// Verbex record identifier used for full-text indexing.
        /// </summary>
        [JsonPropertyName("VerbexRecordId")]
        public string VerbexRecordId { get; set; }

        /// <summary>
        /// Labels associated with this document (JSON).
        /// </summary>
        [JsonPropertyName("Labels")]
        public string Labels { get; set; }

        /// <summary>
        /// Tags associated with this document (JSON).
        /// </summary>
        [JsonPropertyName("Tags")]
        public string Tags { get; set; }

        /// <summary>
        /// Chunk record IDs stored in RecallDB (JSON array).
        /// </summary>
        [JsonPropertyName("ChunkRecordIds")]
        public string ChunkRecordIds { get; set; }

        /// <summary>
        /// Crawl plan identifier that produced this document.
        /// </summary>
        [JsonPropertyName("CrawlPlanId")]
        public string CrawlPlanId { get; set; }

        /// <summary>
        /// Crawl operation identifier that produced this document.
        /// </summary>
        [JsonPropertyName("CrawlOperationId")]
        public string CrawlOperationId { get; set; }

        /// <summary>
        /// Original URL or key from the crawl source.
        /// </summary>
        [JsonPropertyName("SourceUrl")]
        public string SourceUrl { get; set; }

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
