namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Request-history search filter.
    /// </summary>
    public class RequestHistorySearchFilter
    {
        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        [JsonPropertyName("MaxResults")]
        public int MaxResults
        {
            get => _MaxResults;
            set => _MaxResults = Math.Clamp(value, 1, 1000);
        }

        /// <summary>
        /// Continuation token.
        /// </summary>
        [JsonPropertyName("ContinuationToken")]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Ordering.
        /// </summary>
        [JsonPropertyName("Ordering")]
        public EnumerationOrderEnum Ordering { get; set; } = EnumerationOrderEnum.CreatedDescending;

        /// <summary>
        /// Request type filter.
        /// </summary>
        [JsonPropertyName("RequestType")]
        public string RequestType { get; set; }

        /// <summary>
        /// HTTP method filter.
        /// </summary>
        [JsonPropertyName("HttpMethod")]
        public string HttpMethod { get; set; }

        /// <summary>
        /// Path substring filter.
        /// </summary>
        [JsonPropertyName("PathContains")]
        public string PathContains { get; set; }

        /// <summary>
        /// Status code filter.
        /// </summary>
        [JsonPropertyName("StatusCode")]
        public int? StatusCode { get; set; }

        /// <summary>
        /// Success filter.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool? Success { get; set; }

        /// <summary>
        /// Tenant identifier filter.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// User identifier filter.
        /// </summary>
        [JsonPropertyName("UserId")]
        public string UserId { get; set; }

        /// <summary>
        /// Credential identifier filter.
        /// </summary>
        [JsonPropertyName("CredentialId")]
        public string CredentialId { get; set; }

        /// <summary>
        /// Assistant identifier filter.
        /// </summary>
        [JsonPropertyName("AssistantId")]
        public string AssistantId { get; set; }

        /// <summary>
        /// Thread identifier filter.
        /// </summary>
        [JsonPropertyName("ThreadId")]
        public string ThreadId { get; set; }

        /// <summary>
        /// Source-type filter.
        /// </summary>
        [JsonPropertyName("SourceType")]
        public string SourceType { get; set; }

        /// <summary>
        /// Free-text search.
        /// </summary>
        [JsonPropertyName("SearchText")]
        public string SearchText { get; set; }

        /// <summary>
        /// Start time bound.
        /// </summary>
        [JsonPropertyName("StartUtc")]
        public DateTime? StartUtc { get; set; }

        /// <summary>
        /// End time bound.
        /// </summary>
        [JsonPropertyName("EndUtc")]
        public DateTime? EndUtc { get; set; }

        /// <summary>
        /// Summary bucket width in seconds.
        /// </summary>
        [JsonPropertyName("BucketSeconds")]
        public int BucketSeconds
        {
            get => _BucketSeconds;
            set => _BucketSeconds = Math.Clamp(value, 1, 86400);
        }

        /// <summary>
        /// Legacy summary bucket width in minutes.
        /// </summary>
        [JsonPropertyName("BucketMinutes")]
        public int BucketMinutes
        {
            get => Math.Max(1, (int)Math.Ceiling(_BucketSeconds / 60d));
            set => BucketSeconds = Math.Clamp(value, 1, 1440) * 60;
        }

        private int _MaxResults = 100;
        private int _BucketSeconds = 15 * 60;
    }
}
