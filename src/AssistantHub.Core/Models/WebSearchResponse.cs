namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Provider-agnostic web search response.
    /// </summary>
    public class WebSearchResponse
    {
        /// <summary>
        /// Provider name.
        /// </summary>
        public string ProviderName { get; set; } = null;

        /// <summary>
        /// Query echoed by the provider.
        /// </summary>
        public string Query { get; set; } = null;

        /// <summary>
        /// Optional answer summary.
        /// </summary>
        public string Answer { get; set; } = null;

        /// <summary>
        /// Provider request identifier.
        /// </summary>
        public string RequestId { get; set; } = null;

        /// <summary>
        /// Provider latency in seconds.
        /// </summary>
        public double? LatencySeconds { get; set; } = null;

        /// <summary>
        /// Result items.
        /// </summary>
        public List<WebSearchResultItem> Results { get; set; } = new List<WebSearchResultItem>();

        /// <summary>
        /// Top-level images.
        /// </summary>
        public List<TavilySearchImage> Images { get; set; } = new List<TavilySearchImage>();

        /// <summary>
        /// Provider usage credits when available.
        /// </summary>
        public int? CreditsUsed { get; set; } = null;

        /// <summary>
        /// Provider attempts.
        /// </summary>
        public List<WebSearchProviderAttempt> Attempts { get; set; } = new List<WebSearchProviderAttempt>();
    }
}
