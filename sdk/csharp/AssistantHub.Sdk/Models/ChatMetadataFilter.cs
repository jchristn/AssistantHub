namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Metadata filter for restricting retrieval by labels and tags.
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
    }
}
