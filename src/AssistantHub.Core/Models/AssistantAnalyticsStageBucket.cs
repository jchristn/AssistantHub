namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Stage summary bucket.
    /// </summary>
    public class AssistantAnalyticsStageBucket
    {
        /// <summary>
        /// Bucket start time in UTC.
        /// </summary>
        public DateTime BucketStartUtc { get; set; }

        /// <summary>
        /// Bucket end time in UTC.
        /// </summary>
        public DateTime BucketEndUtc { get; set; }

        /// <summary>
        /// Stage name.
        /// </summary>
        public string Stage { get; set; } = null;

        /// <summary>
        /// Stage kind.
        /// </summary>
        public string Kind { get; set; } = null;

        /// <summary>
        /// Stage call count.
        /// </summary>
        public int Calls { get; set; } = 0;

        /// <summary>
        /// Failed call count.
        /// </summary>
        public int Failures { get; set; } = 0;

        /// <summary>
        /// Zero-duration/noop count.
        /// </summary>
        public int SkippedCount { get; set; } = 0;

        /// <summary>
        /// Average duration.
        /// </summary>
        public double? AverageDurationMs { get; set; } = null;

        /// <summary>
        /// 95th percentile duration.
        /// </summary>
        public double? P95DurationMs { get; set; } = null;

        /// <summary>
        /// Maximum duration.
        /// </summary>
        public double? MaxDurationMs { get; set; } = null;
    }
}
