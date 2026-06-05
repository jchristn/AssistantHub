namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Concrete analytics range resolved by the server.
    /// </summary>
    public class AssistantAnalyticsRange
    {
        /// <summary>
        /// Resolved range identifier.
        /// </summary>
        public string RangeId { get; set; }

        /// <summary>
        /// UTC range start.
        /// </summary>
        public DateTime StartUtc { get; set; }

        /// <summary>
        /// UTC range end.
        /// </summary>
        public DateTime EndUtc { get; set; }

        /// <summary>
        /// Bucket size in seconds.
        /// </summary>
        public int BucketSeconds { get; set; }

        /// <summary>
        /// Number of buckets in the response range.
        /// </summary>
        public int BucketCount { get; set; }
    }
}
