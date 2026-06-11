namespace AssistantHub.Core.Settings
{
    /// <summary>
    /// External search provider configuration.
    /// </summary>
    public class ExternalSearchProviderSettings
    {
        /// <summary>
        /// Provider display/configuration name.
        /// </summary>
        public string Name { get; set; } = "tavily";

        /// <summary>
        /// Provider type, for example Tavily.
        /// </summary>
        public string ProviderType { get; set; } = "Tavily";

        /// <summary>
        /// Provider endpoint.
        /// </summary>
        public string Endpoint { get; set; } = "https://api.tavily.com/search";

        /// <summary>
        /// Provider API key or environment-variable reference.
        /// </summary>
        public string ApiKey { get; set; } = null;

        /// <summary>
        /// Whether this provider is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Whether this provider is the default external search provider.
        /// </summary>
        public bool IsDefault { get; set; } = true;

        /// <summary>
        /// HTTP timeout in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;
    }
}
