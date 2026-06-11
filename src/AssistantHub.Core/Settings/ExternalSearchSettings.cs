namespace AssistantHub.Core.Settings
{
    using System.Collections.Generic;

    /// <summary>
    /// Global external web-search settings.
    /// </summary>
    public class ExternalSearchSettings
    {
        /// <summary>
        /// Whether external web search is enabled globally.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Whether a later provider may be tried when the default provider fails.
        /// </summary>
        public bool AllowFallback { get; set; } = true;

        /// <summary>
        /// Maximum results any assistant may request from a web-search provider.
        /// </summary>
        public int MaxResults { get; set; } = 10;

        /// <summary>
        /// Default provider timeout in milliseconds when the provider does not specify one.
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Whether web searches should request provider safe-search filtering.
        /// </summary>
        public bool SafeSearch { get; set; } = true;

        /// <summary>
        /// Whether assistants may request raw provider page content when also permitted by assistant policy.
        /// </summary>
        public bool AllowRawContent { get; set; } = false;

        /// <summary>
        /// Global web-search domain allowlist. Empty allows provider-default public web search.
        /// </summary>
        public List<string> IncludeDomains { get; set; } = new List<string>();

        /// <summary>
        /// Global web-search domain denylist.
        /// </summary>
        public List<string> ExcludeDomains { get; set; } = new List<string>();

        /// <summary>
        /// Search provider configurations.
        /// </summary>
        public List<ExternalSearchProviderSettings> Providers { get; set; } = new List<ExternalSearchProviderSettings>();
    }
}
