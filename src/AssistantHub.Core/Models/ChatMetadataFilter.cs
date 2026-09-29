namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Metadata filter for controlling which documents are included during RAG retrieval.
    /// Supports label-based and tag-based (key-value) filtering.
    /// </summary>
    public class ChatMetadataFilter
    {
        /// <summary>
        /// Labels that must be present on retrieved documents.
        /// </summary>
        [JsonPropertyName("required_labels")]
        public List<string> RequiredLabels { get; set; } = null;

        /// <summary>
        /// Labels that must NOT be present on retrieved documents.
        /// </summary>
        [JsonPropertyName("excluded_labels")]
        public List<string> ExcludedLabels { get; set; } = null;

        /// <summary>
        /// Tag conditions that must all match on retrieved documents.
        /// </summary>
        [JsonPropertyName("required_tags")]
        public List<ChatTagCondition> RequiredTags { get; set; } = null;

        /// <summary>
        /// Tag conditions that must NOT match on retrieved documents.
        /// </summary>
        [JsonPropertyName("excluded_tags")]
        public List<ChatTagCondition> ExcludedTags { get; set; } = null;

        /// <summary>
        /// First page of a page range (1 or more). Only chunks with page provenance that overlap the range are
        /// retrieved; chunks without page numbers (for example from Markdown or HTML) are excluded.
        /// </summary>
        [JsonPropertyName("page_start")]
        public int? PageStart { get; set; } = null;

        /// <summary>
        /// Last page of a page range (1 or more, at least <see cref="PageStart"/>). Omit it for an open-ended range.
        /// </summary>
        [JsonPropertyName("page_end")]
        public int? PageEnd { get; set; } = null;

        /// <summary>
        /// Returns true if no filters are configured.
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty =>
            (RequiredLabels == null || RequiredLabels.Count == 0) &&
            (ExcludedLabels == null || ExcludedLabels.Count == 0) &&
            (RequiredTags == null || RequiredTags.Count == 0) &&
            (ExcludedTags == null || ExcludedTags.Count == 0) &&
            !PageStart.HasValue &&
            !PageEnd.HasValue;

        /// <summary>
        /// Check the page range.
        /// </summary>
        /// <returns>An error message, or null when valid.</returns>
        public string Validate()
        {
            if (PageStart.HasValue && PageStart.Value < 1) return "page_start must be 1 or more.";
            if (PageEnd.HasValue && PageEnd.Value < 1) return "page_end must be 1 or more.";
            if (PageStart.HasValue && PageEnd.HasValue && PageEnd.Value < PageStart.Value) return "page_end must not be less than page_start.";
            if ((PageStart ?? 0) > 99999 || (PageEnd ?? 0) > 99999) return "Page numbers must be 99999 or less.";
            return null;
        }

        /// <summary>
        /// Tag conditions that select chunks overlapping the page range: the chunk starts on or before the last page
        /// and ends on or after the first. Page tags are zero-padded to five digits, so RecallDB's string comparison
        /// orders them numerically.
        /// </summary>
        /// <returns>Conditions (empty when no page range is set).</returns>
        public List<ChatTagCondition> PageTagConditions()
        {
            List<ChatTagCondition> conditions = new List<ChatTagCondition>();
            if (PageEnd.HasValue)
                conditions.Add(new ChatTagCondition { Key = "ah_page_start", Condition = "LessThan", Value = (PageEnd.Value + 1).ToString("D5", System.Globalization.CultureInfo.InvariantCulture) });
            if (PageStart.HasValue)
                conditions.Add(new ChatTagCondition { Key = "ah_page_end", Condition = "GreaterThan", Value = (PageStart.Value - 1).ToString("D5", System.Globalization.CultureInfo.InvariantCulture) });
            return conditions;
        }

        /// <summary>
        /// Merge another filter into this one.
        /// Required labels/tags are unioned; excluded labels/tags are unioned.
        /// </summary>
        /// <param name="other">Filter to merge.</param>
        public void Merge(ChatMetadataFilter other)
        {
            if (other == null) return;

            if (other.RequiredLabels != null && other.RequiredLabels.Count > 0)
            {
                if (RequiredLabels == null) RequiredLabels = new List<string>();
                RequiredLabels = RequiredLabels.Union(other.RequiredLabels).Distinct().ToList();
            }

            if (other.ExcludedLabels != null && other.ExcludedLabels.Count > 0)
            {
                if (ExcludedLabels == null) ExcludedLabels = new List<string>();
                ExcludedLabels = ExcludedLabels.Union(other.ExcludedLabels).Distinct().ToList();
            }

            if (other.RequiredTags != null && other.RequiredTags.Count > 0)
            {
                if (RequiredTags == null) RequiredTags = new List<ChatTagCondition>();
                RequiredTags.AddRange(other.RequiredTags);
            }

            if (other.ExcludedTags != null && other.ExcludedTags.Count > 0)
            {
                if (ExcludedTags == null) ExcludedTags = new List<ChatTagCondition>();
                ExcludedTags.AddRange(other.ExcludedTags);
            }

            // Page ranges intersect: the later start and the earlier end win.
            if (other.PageStart.HasValue) PageStart = PageStart.HasValue ? System.Math.Max(PageStart.Value, other.PageStart.Value) : other.PageStart;
            if (other.PageEnd.HasValue) PageEnd = PageEnd.HasValue ? System.Math.Min(PageEnd.Value, other.PageEnd.Value) : other.PageEnd;
        }
    }
}
