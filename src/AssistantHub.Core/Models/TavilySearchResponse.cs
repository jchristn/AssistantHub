namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Normalized Tavily search response.
    /// </summary>
    public class TavilySearchResponse
    {
        /// <summary>
        /// Provider name.
        /// </summary>
        public string ProviderName { get; set; } = "Tavily";

        /// <summary>
        /// Query echoed by the provider.
        /// </summary>
        public string Query { get; set; } = null;

        /// <summary>
        /// Optional provider-generated answer.
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
        /// Search results.
        /// </summary>
        public List<TavilySearchResult> Results { get; set; } = new List<TavilySearchResult>();

        /// <summary>
        /// Top-level images.
        /// </summary>
        public List<TavilySearchImage> Images { get; set; } = new List<TavilySearchImage>();

        /// <summary>
        /// Provider-selected parameters.
        /// </summary>
        public TavilyAutoParameters AutoParameters { get; set; } = null;

        /// <summary>
        /// Usage metadata.
        /// </summary>
        public TavilyUsage Usage { get; set; } = null;
    }
}
