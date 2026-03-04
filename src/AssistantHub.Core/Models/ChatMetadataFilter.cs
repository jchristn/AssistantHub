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
        /// Returns true if no filters are configured.
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty =>
            (RequiredLabels == null || RequiredLabels.Count == 0) &&
            (ExcludedLabels == null || ExcludedLabels.Count == 0) &&
            (RequiredTags == null || RequiredTags.Count == 0) &&
            (ExcludedTags == null || ExcludedTags.Count == 0);

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
        }
    }
}
