namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Resolved analytics time range and bucket geometry.
    /// </summary>
    public class AssistantAnalyticsRange
    {
        /// <summary>
        /// Range identifier.
        /// </summary>
        public string RangeId { get; set; } = "lastDay";

        /// <summary>
        /// Inclusive UTC start time.
        /// </summary>
        public DateTime StartUtc { get; set; } = DateTime.UtcNow.AddDays(-1);

        /// <summary>
        /// Exclusive UTC end time.
        /// </summary>
        public DateTime EndUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Bucket width in seconds.
        /// </summary>
        public int BucketSeconds { get; set; } = 900;

        /// <summary>
        /// Number of buckets returned.
        /// </summary>
        public int BucketCount { get; set; } = 96;
    }
}
