namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Request-history summary result.
    /// </summary>
    public class RequestHistorySummaryResult
    {
        /// <summary>
        /// Total request count.
        /// </summary>
        public long TotalCount { get; set; } = 0;

        /// <summary>
        /// Total successful request count.
        /// </summary>
        public long TotalSuccess { get; set; } = 0;

        /// <summary>
        /// Total failed request count.
        /// </summary>
        public long TotalFailure { get; set; } = 0;

        /// <summary>
        /// Average request duration.
        /// </summary>
        public double AverageDurationMs { get; set; } = 0;

        /// <summary>
        /// Time buckets.
        /// </summary>
        public List<RequestHistorySummaryBucket> Buckets { get; set; } = new List<RequestHistorySummaryBucket>();
    }
}
