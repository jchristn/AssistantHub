namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A single time-series data point.
    /// </summary>
    public class AssistantAnalyticsPoint
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
        /// Aggregated value for the bucket.
        /// </summary>
        public double? Value { get; set; }

        /// <summary>
        /// Number of non-null samples contributing to the value.
        /// </summary>
        public int SampleCount { get; set; }

        /// <summary>
        /// Number of null or unavailable samples in the bucket.
        /// </summary>
        public int NullCount { get; set; }
    }
}
