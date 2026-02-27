namespace AssistantHub.Core.Models
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Base class for crawl repository settings.
    /// </summary>
    public class CrawlRepositorySettings
    {
        #region Public-Members

        /// <summary>
        /// Repository type.
        /// </summary>
        public RepositoryTypeEnum RepositoryType { get; set; } = RepositoryTypeEnum.Web;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CrawlRepositorySettings()
        {
        }

        #endregion
    }

    /// <summary>
    /// Web crawl repository settings.
    /// </summary>
    public class WebCrawlRepositorySettings : CrawlRepositorySettings
    {
        #region Public-Members

        /// <summary>
        /// Authentication type.
        /// Default: None.
        /// </summary>
        public WebAuthTypeEnum AuthenticationType { get; set; } = WebAuthTypeEnum.None;

        /// <summary>
        /// Username for Basic authentication.
        /// </summary>
        public string Username { get; set; } = null;

        /// <summary>
        /// Password for Basic authentication.
        /// </summary>
        public string Password { get; set; } = null;

        /// <summary>
        /// API key header name.
        /// </summary>
        public string ApiKeyHeader { get; set; } = null;

        /// <summary>
        /// API key value.
        /// </summary>
        public string ApiKeyValue { get; set; } = null;

        /// <summary>
        /// Bearer token.
        /// </summary>
        public string BearerToken { get; set; } = null;

        /// <summary>
        /// User agent string.
        /// Default: assistanthub-crawler.
        /// </summary>
        public string UserAgent
        {
            get => _UserAgent;
            set => _UserAgent = !String.IsNullOrEmpty(value) ? value : "assistanthub-crawler";
        }

        /// <summary>
        /// Start URL for crawling.
        /// </summary>
        public string StartUrl
        {
            get => _StartUrl;
            set => _StartUrl = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(StartUrl));
        }

        /// <summary>
        /// Use headless browser for JavaScript-heavy sites.
        /// Default: false.
        /// </summary>
        public bool UseHeadlessBrowser { get; set; } = false;

        /// <summary>
        /// Follow links during crawling.
        /// Default: true.
        /// </summary>
        public bool FollowLinks { get; set; } = true;

        /// <summary>
        /// Follow HTTP redirects.
        /// Default: true.
        /// </summary>
        public bool FollowRedirects { get; set; } = true;

        /// <summary>
        /// Extract and follow sitemap links.
        /// Default: true.
        /// </summary>
        public bool ExtractSitemapLinks { get; set; } = true;

        /// <summary>
        /// Restrict crawling to child URLs of the start URL.
        /// Default: true.
        /// </summary>
        public bool RestrictToChildUrls { get; set; } = true;

        /// <summary>
        /// Restrict crawling to the same subdomain.
        /// Default: false.
        /// </summary>
        public bool RestrictToSubdomain { get; set; } = false;

        /// <summary>
        /// Restrict crawling to the same root domain.
        /// Default: true.
        /// </summary>
        public bool RestrictToRootDomain { get; set; } = true;

        /// <summary>
        /// Ignore robots.txt directives.
        /// Default: false.
        /// </summary>
        public bool IgnoreRobotsTxt { get; set; } = false;

        /// <summary>
        /// Maximum crawl depth.
        /// Default: 5. Clamped 1-100.
        /// </summary>
        public int MaxDepth
        {
            get => _MaxDepth;
            set => _MaxDepth = Math.Clamp(value, 1, 100);
        }

        /// <summary>
        /// Maximum parallel crawl tasks.
        /// Default: 8. Clamped 1-64.
        /// </summary>
        public int MaxParallelTasks
        {
            get => _MaxParallelTasks;
            set => _MaxParallelTasks = Math.Clamp(value, 1, 64);
        }

        /// <summary>
        /// Delay between requests in milliseconds.
        /// Default: 100. Clamped 0-60000.
        /// </summary>
        public int CrawlDelayMs
        {
            get => _CrawlDelayMs;
            set => _CrawlDelayMs = Math.Clamp(value, 0, 60000);
        }

        #endregion

        #region Private-Members

        private string _UserAgent = "assistanthub-crawler";
        private string _StartUrl = "https://example.com";
        private int _MaxDepth = 5;
        private int _MaxParallelTasks = 8;
        private int _CrawlDelayMs = 100;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public WebCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.Web;
        }

        #endregion
    }

    /// <summary>
    /// JSON converter for CrawlRepositorySettings polymorphic deserialization.
    /// Uses the RepositoryType property to determine the derived type.
    /// </summary>
    public class CrawlRepositorySettingsConverter : JsonConverter<CrawlRepositorySettings>
    {
        /// <inheritdoc />
        public override CrawlRepositorySettings Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<WebCrawlRepositorySettings>(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, CrawlRepositorySettings value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
