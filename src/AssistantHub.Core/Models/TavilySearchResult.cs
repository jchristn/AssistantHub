namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Normalized Tavily search result item.
    /// </summary>
    public class TavilySearchResult
    {
        /// <summary>
        /// Result title.
        /// </summary>
        public string Title { get; set; } = null;

        /// <summary>
        /// Result URL.
        /// </summary>
        public string Url { get; set; } = null;

        /// <summary>
        /// Result snippet or summary.
        /// </summary>
        public string Content { get; set; } = null;

        /// <summary>
        /// Provider score.
        /// </summary>
        public double? Score { get; set; } = null;

        /// <summary>
        /// Raw page content when requested and permitted.
        /// </summary>
        public string RawContent { get; set; } = null;

        /// <summary>
        /// Favicon URL.
        /// </summary>
        public string FaviconUrl { get; set; } = null;

        /// <summary>
        /// Published timestamp.
        /// </summary>
        public DateTimeOffset? PublishedAt { get; set; } = null;

        /// <summary>
        /// Result images.
        /// </summary>
        public List<TavilySearchImage> Images { get; set; } = new List<TavilySearchImage>();
    }
}
