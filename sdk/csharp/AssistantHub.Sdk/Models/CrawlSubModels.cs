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

    /// <summary>
    /// Web crawl repository settings.
    /// </summary>
    public class WebCrawlRepositorySettings
    {
        /// <summary>
        /// Web authentication type.
        /// </summary>
        [JsonPropertyName("AuthType")]
        public WebAuthTypeEnum AuthType { get; set; }

        /// <summary>
        /// Authentication username (for Basic auth).
        /// </summary>
        [JsonPropertyName("Username")]
        public string Username { get; set; }

        /// <summary>
        /// Authentication password (for Basic auth).
        /// </summary>
        [JsonPropertyName("Password")]
        public string Password { get; set; }

        /// <summary>
        /// API key value (for ApiKey auth).
        /// </summary>
        [JsonPropertyName("ApiKeyValue")]
        public string ApiKeyValue { get; set; }

        /// <summary>
        /// API key header name (for ApiKey auth).
        /// </summary>
        [JsonPropertyName("ApiKeyHeader")]
        public string ApiKeyHeader { get; set; }

        /// <summary>
        /// Bearer token (for BearerToken auth).
        /// </summary>
        [JsonPropertyName("BearerToken")]
        public string BearerToken { get; set; }

        /// <summary>
        /// Starting URL for the crawl.
        /// </summary>
        [JsonPropertyName("StartUrl")]
        public string StartUrl { get; set; }

        /// <summary>
        /// Maximum crawl depth (1-100).
        /// </summary>
        [JsonPropertyName("MaxDepth")]
        public int MaxDepth { get; set; }

        /// <summary>
        /// Maximum parallel crawl tasks (1-64).
        /// </summary>
        [JsonPropertyName("MaxParallelTasks")]
        public int MaxParallelTasks { get; set; }

        /// <summary>
        /// Delay between crawl requests in milliseconds (0-60000).
        /// </summary>
        [JsonPropertyName("CrawlDelayMs")]
        public int CrawlDelayMs { get; set; }

        /// <summary>
        /// Whether to follow links found on pages.
        /// </summary>
        [JsonPropertyName("FollowLinks")]
        public bool FollowLinks { get; set; }

        /// <summary>
        /// Whether to follow redirects.
        /// </summary>
        [JsonPropertyName("FollowRedirects")]
        public bool FollowRedirects { get; set; }

        /// <summary>
        /// Whether to respect robots.txt.
        /// </summary>
        [JsonPropertyName("RespectRobotsTxt")]
        public bool RespectRobotsTxt { get; set; }

        /// <summary>
        /// Whether to use sitemaps.
        /// </summary>
        [JsonPropertyName("UseSitemaps")]
        public bool UseSitemaps { get; set; }

        /// <summary>
        /// Restrict crawl to child URLs of the start URL.
        /// </summary>
        [JsonPropertyName("RestrictToChildUrls")]
        public bool RestrictToChildUrls { get; set; }

        /// <summary>
        /// Restrict crawl to same subdomain.
        /// </summary>
        [JsonPropertyName("RestrictToSubdomain")]
        public bool RestrictToSubdomain { get; set; }

        /// <summary>
        /// Restrict crawl to same root domain.
        /// </summary>
        [JsonPropertyName("RestrictToRootDomain")]
        public bool RestrictToRootDomain { get; set; }
    }

    /// <summary>
    /// Crawl schedule settings sub-object.
    /// </summary>
    public class CrawlScheduleSettings
    {
        /// <summary>
        /// Schedule interval type.
        /// </summary>
        [JsonPropertyName("IntervalType")]
        public ScheduleIntervalEnum IntervalType { get; set; }

        /// <summary>
        /// Schedule interval value (1-10080).
        /// </summary>
        [JsonPropertyName("IntervalValue")]
        public int IntervalValue { get; set; }
    }

    /// <summary>
    /// Crawl filter settings sub-object.
    /// </summary>
    public class CrawlFilterSettings
    {
        /// <summary>
        /// Object key prefix filter.
        /// </summary>
        [JsonPropertyName("ObjectPrefix")]
        public string ObjectPrefix { get; set; }

        /// <summary>
        /// Object key suffix filter.
        /// </summary>
        [JsonPropertyName("ObjectSuffix")]
        public string ObjectSuffix { get; set; }

        /// <summary>
        /// Allowed content types filter.
        /// </summary>
        [JsonPropertyName("AllowedContentTypes")]
        public List<string> AllowedContentTypes { get; set; }

        /// <summary>
        /// Minimum object size in bytes.
        /// </summary>
        [JsonPropertyName("MinimumSize")]
        public long MinimumSize { get; set; }

        /// <summary>
        /// Maximum object size in bytes.
        /// </summary>
        [JsonPropertyName("MaximumSize")]
        public long? MaximumSize { get; set; }
    }
}
