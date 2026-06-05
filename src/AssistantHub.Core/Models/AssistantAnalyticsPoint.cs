namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Time-series point.
    /// </summary>
    public class AssistantAnalyticsPoint
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
        /// Point value, or null when unavailable.
        /// </summary>
        public double? Value { get; set; } = null;

        /// <summary>
        /// Number of samples used.
        /// </summary>
        public int SampleCount { get; set; } = 0;

        /// <summary>
        /// Number of unavailable samples.
        /// </summary>
        public int NullCount { get; set; } = 0;
    }
}
