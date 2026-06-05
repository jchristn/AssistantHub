namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Crawl filter settings sub-object.
    /// </summary>
    public class CrawlFilterSettings
    {
        /// <summary>
        /// Object key prefix filter.
        /// </summary>
        [JsonPropertyName("ObjectPrefix")]
        public string ObjectPrefix { get; set; }

        /// <summary>
        /// Object key suffix filter.
        /// </summary>
        [JsonPropertyName("ObjectSuffix")]
        public string ObjectSuffix { get; set; }

        /// <summary>
        /// Allowed content types filter.
        /// </summary>
        [JsonPropertyName("AllowedContentTypes")]
        public List<string> AllowedContentTypes { get; set; }

        /// <summary>
        /// Minimum object size in bytes.
        /// </summary>
        [JsonPropertyName("MinimumSize")]
        public long MinimumSize { get; set; }

        /// <summary>
        /// Maximum object size in bytes.
        /// </summary>
        [JsonPropertyName("MaximumSize")]
        public long? MaximumSize { get; set; }
    }
}
