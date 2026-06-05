namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Crawl ingestion settings sub-object.
    /// </summary>
    public class CrawlIngestionSettings
    {
        /// <summary>
        /// Ingestion rule identifier.
        /// </summary>
        [JsonPropertyName("IngestionRuleId")]
        public string IngestionRuleId { get; set; }

        /// <summary>
        /// Store crawled documents in S3.
        /// </summary>
        [JsonPropertyName("StoreInS3")]
        public bool StoreInS3 { get; set; }

        /// <summary>
        /// S3 bucket name for storing crawled documents.
        /// </summary>
        [JsonPropertyName("S3BucketName")]
        public string S3BucketName { get; set; }
    }
}
