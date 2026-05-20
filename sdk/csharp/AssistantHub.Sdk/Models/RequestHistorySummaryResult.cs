namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request-history summary result.
    /// </summary>
    public class RequestHistorySummaryResult
    {
        /// <summary>
        /// Total request count.
        /// </summary>
        [JsonPropertyName("TotalCount")]
        public long TotalCount { get; set; }

        /// <summary>
        /// Total successful request count.
        /// </summary>
        [JsonPropertyName("TotalSuccess")]
        public long TotalSuccess { get; set; }

        /// <summary>
        /// Total failed request count.
        /// </summary>
        [JsonPropertyName("TotalFailure")]
        public long TotalFailure { get; set; }

        /// <summary>
        /// Average request duration.
        /// </summary>
        [JsonPropertyName("AverageDurationMs")]
        public double AverageDurationMs { get; set; }

        /// <summary>
        /// Time buckets.
        /// </summary>
        [JsonPropertyName("Buckets")]
        public List<RequestHistorySummaryBucket> Buckets { get; set; } = new List<RequestHistorySummaryBucket>();
    }
}
