namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request-history summary bucket.
    /// </summary>
    public class RequestHistorySummaryBucket
    {
        /// <summary>
        /// Bucket start time in UTC.
        /// </summary>
        [JsonPropertyName("BucketStartUtc")]
        public DateTime BucketStartUtc { get; set; }

        /// <summary>
        /// Bucket end time in UTC.
        /// </summary>
        [JsonPropertyName("BucketEndUtc")]
        public DateTime BucketEndUtc { get; set; }

        /// <summary>
        /// Total request count.
        /// </summary>
        [JsonPropertyName("RequestCount")]
        public int RequestCount { get; set; }

        /// <summary>
        /// Successful request count.
        /// </summary>
        [JsonPropertyName("SuccessCount")]
        public int SuccessCount { get; set; }

        /// <summary>
        /// Failed request count.
        /// </summary>
        [JsonPropertyName("FailureCount")]
        public int FailureCount { get; set; }

        /// <summary>
        /// Average request duration.
        /// </summary>
        [JsonPropertyName("AverageDurationMs")]
        public double AverageDurationMs { get; set; }
    }
}
