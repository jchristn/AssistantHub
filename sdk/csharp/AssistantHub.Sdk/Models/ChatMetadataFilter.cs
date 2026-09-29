namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Metadata filter for restricting retrieval by labels, tags, and page range.
    /// </summary>
    public class ChatMetadataFilter
    {
        /// <summary>
        /// Labels that must be present.
        /// </summary>
        [JsonPropertyName("required_labels")]
        public List<string> RequiredLabels { get; set; }

        /// <summary>
        /// Labels that must not be present.
        /// </summary>
        [JsonPropertyName("excluded_labels")]
        public List<string> ExcludedLabels { get; set; }

        /// <summary>
        /// Tag conditions that must be satisfied.
        /// </summary>
        [JsonPropertyName("required_tags")]
        public List<ChatTagCondition> RequiredTags { get; set; }

        /// <summary>
        /// Tag conditions that must not be satisfied.
        /// </summary>
        [JsonPropertyName("excluded_tags")]
        public List<ChatTagCondition> ExcludedTags { get; set; }

        /// <summary>
        /// First page of a page range (1 to 99999). Only chunks with page provenance that overlap the range are
        /// retrieved; chunks without page numbers are excluded when a range is set. Omit for an open start.
        /// </summary>
        [JsonPropertyName("page_start")]
        public int? PageStart { get; set; }

        /// <summary>
        /// Last page of a page range (1 to 99999, at least <see cref="PageStart"/>). Omit for an open end.
        /// </summary>
        [JsonPropertyName("page_end")]
        public int? PageEnd { get; set; }
    }
}
