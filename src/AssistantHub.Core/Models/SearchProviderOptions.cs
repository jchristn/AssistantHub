namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Provider-neutral search provider options.
    /// </summary>
    public class SearchProviderOptions
    {
        /// <summary>
        /// Provider display name.
        /// </summary>
        public string Name { get; set; } = "default";

        /// <summary>
        /// Provider implementation type.
        /// </summary>
        public string ProviderType { get; set; } = "Tavily";

        /// <summary>
        /// Provider endpoint.
        /// </summary>
        public string Endpoint { get; set; } = null;

        /// <summary>
        /// Provider API key or environment-variable placeholder.
        /// </summary>
        public string ApiKey { get; set; } = null;

        /// <summary>
        /// Whether provider is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Whether provider is the default.
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Timeout in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;
    }
}
