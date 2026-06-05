namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Aggregated performance-stage bucket.
    /// </summary>
    public class AssistantAnalyticsStageBucket
    {
        /// <summary>
        /// UTC bucket start.
        /// </summary>
        public DateTime BucketStartUtc { get; set; }

        /// <summary>
        /// UTC bucket end.
        /// </summary>
        public DateTime BucketEndUtc { get; set; }

        /// <summary>
        /// Performance stage name.
        /// </summary>
        public string Stage { get; set; }

        /// <summary>
        /// Stage kind.
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Number of calls in the bucket.
        /// </summary>
        public int Calls { get; set; }

        /// <summary>
        /// Number of failed calls in the bucket.
        /// </summary>
        public int Failures { get; set; }

        /// <summary>
        /// Number of skipped calls in the bucket.
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// Average stage duration in milliseconds.
        /// </summary>
        public double? AverageDurationMs { get; set; }

        /// <summary>
        /// P95 stage duration in milliseconds.
        /// </summary>
        public double? P95DurationMs { get; set; }

        /// <summary>
        /// Maximum stage duration in milliseconds.
        /// </summary>
        public double? MaxDurationMs { get; set; }
    }
}
