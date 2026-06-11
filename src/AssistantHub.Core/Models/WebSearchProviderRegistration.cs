namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Registered web search provider.
    /// </summary>
    public class WebSearchProviderRegistration
    {
        /// <summary>
        /// Provider name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Provider type.
        /// </summary>
        public string ProviderType { get; set; } = null;

        /// <summary>
        /// Provider options.
        /// </summary>
        public SearchProviderOptions Options { get; set; } = null;
    }
}
