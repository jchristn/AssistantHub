namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Feedback analytics bucket.
    /// </summary>
    public class AssistantAnalyticsFeedbackBucket
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
        /// Positive feedback count.
        /// </summary>
        public int ThumbsUpCount { get; set; } = 0;

        /// <summary>
        /// Negative feedback count.
        /// </summary>
        public int ThumbsDownCount { get; set; } = 0;

        /// <summary>
        /// Unknown feedback count.
        /// </summary>
        public int UnknownCount { get; set; } = 0;

        /// <summary>
        /// Total feedback count.
        /// </summary>
        public int TotalCount { get; set; } = 0;

        /// <summary>
        /// Negative feedback rate.
        /// </summary>
        public double? NegativeRate { get; set; } = null;
    }
}
