namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provider-agnostic web search result item.
    /// </summary>
    public class WebSearchResultItem
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
        /// Snippet or result content.
        /// </summary>
        public string Content { get; set; } = null;

        /// <summary>
        /// Provider score.
        /// </summary>
        public double? Score { get; set; } = null;

        /// <summary>
        /// Optional raw content.
        /// </summary>
        public string RawContent { get; set; } = null;

        /// <summary>
        /// Optional favicon URL.
        /// </summary>
        public string FaviconUrl { get; set; } = null;

        /// <summary>
        /// Optional publication timestamp.
        /// </summary>
        public DateTimeOffset? PublishedAt { get; set; } = null;

        /// <summary>
        /// Result images.
        /// </summary>
        public List<TavilySearchImage> Images { get; set; } = new List<TavilySearchImage>();
    }
}
