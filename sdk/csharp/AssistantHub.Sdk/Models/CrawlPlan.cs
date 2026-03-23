namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Crawl plan record.
    /// </summary>
    public class CrawlPlan
    {
        /// <summary>
        /// Unique identifier with prefix cplan_.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Display name for the crawl plan.
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Repository type.
        /// </summary>
        [JsonPropertyName("RepositoryType")]
        public RepositoryTypeEnum RepositoryType { get; set; }

        /// <summary>
        /// Ingestion settings.
        /// </summary>
        [JsonPropertyName("IngestionSettings")]
        public CrawlIngestionSettings IngestionSettings { get; set; }

        /// <summary>
        /// Repository settings.
        /// </summary>
        [JsonPropertyName("RepositorySettings")]
        public WebCrawlRepositorySettings RepositorySettings { get; set; }

        /// <summary>
        /// Schedule settings.
        /// </summary>
        [JsonPropertyName("Schedule")]
        public CrawlScheduleSettings Schedule { get; set; }

        /// <summary>
        /// Filter settings.
        /// </summary>
        [JsonPropertyName("Filter")]
        public CrawlFilterSettings Filter { get; set; }

        /// <summary>
        /// Process new objects found during crawling.
        /// </summary>
        [JsonPropertyName("ProcessAdditions")]
        public bool ProcessAdditions { get; set; }

        /// <summary>
        /// Process updated objects found during crawling.
        /// </summary>
        [JsonPropertyName("ProcessUpdates")]
        public bool ProcessUpdates { get; set; }

        /// <summary>
        /// Process deleted objects found during crawling.
        /// </summary>
        [JsonPropertyName("ProcessDeletions")]
        public bool ProcessDeletions { get; set; }

        /// <summary>
        /// Maximum concurrent drain tasks (1-64).
        /// </summary>
        [JsonPropertyName("MaxDrainTasks")]
        public int MaxDrainTasks { get; set; }

        /// <summary>
        /// Operation retention in days (0-14).
        /// </summary>
        [JsonPropertyName("RetentionDays")]
        public int RetentionDays { get; set; }

        /// <summary>
        /// Current state of the crawl plan.
        /// </summary>
        [JsonPropertyName("State")]
        public CrawlPlanStateEnum State { get; set; }

        /// <summary>
        /// Timestamp of the last crawl start in UTC.
        /// </summary>
        [JsonPropertyName("LastCrawlStartUtc")]
        public DateTime? LastCrawlStartUtc { get; set; }

        /// <summary>
        /// Timestamp of the last crawl finish in UTC.
        /// </summary>
        [JsonPropertyName("LastCrawlFinishUtc")]
        public DateTime? LastCrawlFinishUtc { get; set; }

        /// <summary>
        /// Whether the last crawl was successful.
        /// </summary>
        [JsonPropertyName("LastCrawlSuccess")]
        public bool? LastCrawlSuccess { get; set; }

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
