namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Web crawl repository settings.
    /// </summary>
    public class WebCrawlRepositorySettings : CrawlRepositorySettings
    {
        /// <summary>
        /// Web authentication type.
        /// </summary>
        [JsonPropertyName("AuthenticationType")]
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
        /// User agent string sent with HTTP requests.
        /// </summary>
        [JsonPropertyName("UserAgent")]
        public string UserAgent { get; set; }

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
        /// Whether to extract URLs from sitemaps.
        /// </summary>
        [JsonPropertyName("ExtractSitemapLinks")]
        public bool ExtractSitemapLinks { get; set; }

        /// <summary>
        /// Whether to ignore robots.txt.
        /// </summary>
        [JsonPropertyName("IgnoreRobotsTxt")]
        public bool IgnoreRobotsTxt { get; set; }

        /// <summary>
        /// Whether to use a headless browser.
        /// </summary>
        [JsonPropertyName("UseHeadlessBrowser")]
        public bool UseHeadlessBrowser { get; set; }

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

        /// <summary>
        /// Instantiate.
        /// </summary>
        public WebCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.Web;
        }
    }
}
