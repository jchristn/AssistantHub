namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Enumeration query for paginated listing.
    /// </summary>
    public class EnumerationQuery
    {
        /// <summary>
        /// Maximum results per page (1-1000).
        /// </summary>
        [JsonPropertyName("MaxResults")]
        public int MaxResults { get; set; }

        /// <summary>
        /// Continuation token for pagination.
        /// </summary>
        [JsonPropertyName("ContinuationToken")]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Result ordering.
        /// </summary>
        [JsonPropertyName("Ordering")]
        public EnumerationOrderEnum Ordering { get; set; }

        /// <summary>
        /// Filter by assistant identifier.
        /// </summary>
        [JsonPropertyName("AssistantIdFilter")]
        public string AssistantIdFilter { get; set; }

        /// <summary>
        /// Filter by bucket name.
        /// </summary>
        [JsonPropertyName("BucketNameFilter")]
        public string BucketNameFilter { get; set; }

        /// <summary>
        /// Filter by collection identifier.
        /// </summary>
        [JsonPropertyName("CollectionIdFilter")]
        public string CollectionIdFilter { get; set; }

        /// <summary>
        /// Filter by thread identifier.
        /// </summary>
        [JsonPropertyName("ThreadIdFilter")]
        public string ThreadIdFilter { get; set; }
    }
}
