namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Tavily search query.
    /// </summary>
    public class TavilySearchQuery
    {
        private string _Query = null;
        private string _SearchDepth = "basic";
        private string _Topic = "general";
        private int _MaxResults = 5;
        private int _ChunksPerSource = 3;

        /// <summary>
        /// Query text.
        /// </summary>
        public string Query
        {
            get => _Query;
            set => _Query = !String.IsNullOrWhiteSpace(value) ? value.Trim() : throw new ArgumentNullException(nameof(Query));
        }

        /// <summary>
        /// Tavily search depth, such as basic or advanced.
        /// </summary>
        public string SearchDepth
        {
            get => _SearchDepth;
            set => _SearchDepth = String.IsNullOrWhiteSpace(value) ? "basic" : value.Trim();
        }

        /// <summary>
        /// Tavily topic, such as general or news.
        /// </summary>
        public string Topic
        {
            get => _Topic;
            set => _Topic = String.IsNullOrWhiteSpace(value) ? "general" : value.Trim();
        }

        /// <summary>
        /// Maximum result count.
        /// </summary>
        public int MaxResults
        {
            get => _MaxResults;
            set => _MaxResults = Math.Clamp(value, 1, 20);
        }

        /// <summary>
        /// Chunks per source.
        /// </summary>
        public int ChunksPerSource
        {
            get => _ChunksPerSource;
            set => _ChunksPerSource = Math.Clamp(value, 1, 3);
        }

        /// <summary>
        /// Optional relative time range.
        /// </summary>
        public string TimeRange { get; set; } = null;

        /// <summary>
        /// Optional start date formatted as yyyy-MM-dd.
        /// </summary>
        public string StartDate { get; set; } = null;

        /// <summary>
        /// Optional end date formatted as yyyy-MM-dd.
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
        /// Whether to request favicons.
        /// </summary>
        public bool IncludeFavicon { get; set; } = true;

        /// <summary>
        /// Optional country hint.
        /// </summary>
        public string Country { get; set; } = null;

        /// <summary>
        /// Whether to allow Tavily auto-parameter selection.
        /// </summary>
        public bool AutoParameters { get; set; } = false;

        /// <summary>
        /// Whether to require exact matching where supported.
        /// </summary>
        public bool ExactMatch { get; set; } = false;

        /// <summary>
        /// Whether to request usage metadata.
        /// </summary>
        public bool IncludeUsage { get; set; } = true;

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
        /// Validate and normalize query fields.
        /// </summary>
        public void Validate()
        {
            if (String.IsNullOrWhiteSpace(Query))
                throw new ArgumentException("A Tavily query is required.", nameof(Query));

            IncludeDomains = NormalizeStringList(IncludeDomains);
            ExcludeDomains = NormalizeStringList(ExcludeDomains);

            if (!String.IsNullOrWhiteSpace(StartDate) && !DateOnly.TryParse(StartDate, out _))
                throw new ArgumentException("StartDate must be yyyy-MM-dd.", nameof(StartDate));

            if (!String.IsNullOrWhiteSpace(EndDate) && !DateOnly.TryParse(EndDate, out _))
                throw new ArgumentException("EndDate must be yyyy-MM-dd.", nameof(EndDate));
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
