namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Options for provider-agnostic web search execution.
    /// </summary>
    public class WebSearchServiceOptions
    {
        /// <summary>
        /// Registered providers.
        /// </summary>
        public List<WebSearchProviderRegistration> Providers { get; set; } = new List<WebSearchProviderRegistration>();

        /// <summary>
        /// Maximum number of provider attempts.
        /// </summary>
        public int MaxProviderAttempts { get; set; } = 1;
    }
}
