namespace AssistantHub.Core.Models
{
    using System;

    /// <summary>
    /// Request-history summary bucket.
    /// </summary>
    public class RequestHistorySummaryBucket
    {
        /// <summary>
        /// Bucket start time in UTC.
        /// </summary>
        public DateTime BucketStartUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Bucket end time in UTC.
        /// </summary>
        public DateTime BucketEndUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total request count.
        /// </summary>
        public int RequestCount { get; set; } = 0;

        /// <summary>
        /// Successful request count.
        /// </summary>
        public int SuccessCount { get; set; } = 0;

        /// <summary>
        /// Failed request count.
        /// </summary>
        public int FailureCount { get; set; } = 0;

        /// <summary>
        /// Average request duration.
        /// </summary>
        public double AverageDurationMs { get; set; } = 0;
    }
}
