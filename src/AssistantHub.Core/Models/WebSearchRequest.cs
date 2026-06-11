namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provider-agnostic web search request.
    /// </summary>
    public class WebSearchRequest
    {
        /// <summary>
        /// Query text.
        /// </summary>
        public string Query { get; set; } = null;

        /// <summary>
        /// Maximum result count.
        /// </summary>
        public int MaxResults { get; set; } = 5;

        /// <summary>
        /// Search depth, such as basic or advanced.
        /// </summary>
        public string SearchDepth { get; set; } = "basic";

        /// <summary>
        /// Search topic, such as general or news.
        /// </summary>
        public string Topic { get; set; } = "general";

        /// <summary>
        /// Optional relative time range.
        /// </summary>
        public string TimeRange { get; set; } = null;

        /// <summary>
        /// Optional start date formatted yyyy-MM-dd.
        /// </summary>
        public string StartDate { get; set; } = null;

        /// <summary>
        /// Optional end date formatted yyyy-MM-dd.
        /// </summary>
        public string EndDate { get; set; } = null;

        /// <summary>
        /// Include-answer mode.
        /// </summary>
        public string IncludeAnswerMode { get; set; } = "basic";

        /// <summary>
        /// Include-raw-content mode.
        /// </summary>
        public string IncludeRawContentMode { get; set; } = null;

        /// <summary>
        /// Whether to request images.
        /// </summary>
        public bool IncludeImages { get; set; } = false;

        /// <summary>
        /// Whether to request image descriptions.
        /// </summary>
        public bool IncludeImageDescriptions { get; set; } = false;

        /// <summary>
        /// Optional country hint.
        /// </summary>
        public string Country { get; set; } = null;

        /// <summary>
        /// Whether to request safe-search filtering.
        /// </summary>
        public bool SafeSearch { get; set; } = true;

        /// <summary>
        /// Domains to include.
        /// </summary>
        public List<string> IncludeDomains { get; set; } = new List<string>();

        /// <summary>
        /// Domains to exclude.
        /// </summary>
        public List<string> ExcludeDomains { get; set; } = new List<string>();

        /// <summary>
        /// Validate and normalize request fields.
        /// </summary>
        public void Normalize()
        {
            Query = String.IsNullOrWhiteSpace(Query) ? null : Query.Trim();
            SearchDepth = String.IsNullOrWhiteSpace(SearchDepth) ? "basic" : SearchDepth.Trim();
            Topic = String.IsNullOrWhiteSpace(Topic) ? "general" : Topic.Trim();
            MaxResults = Math.Clamp(MaxResults, 1, 20);
            IncludeDomains = NormalizeStringList(IncludeDomains);
            ExcludeDomains = NormalizeStringList(ExcludeDomains);
        }

        private static List<string> NormalizeStringList(IEnumerable<string> values)
        {
            if (values == null) return new List<string>();

            return values
                .Where(value => !String.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
