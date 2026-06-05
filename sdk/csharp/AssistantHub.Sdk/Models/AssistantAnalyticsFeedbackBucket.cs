namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Feedback analytics bucket.
    /// </summary>
    public class AssistantAnalyticsFeedbackBucket
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
        /// Positive feedback count.
        /// </summary>
        public int ThumbsUpCount { get; set; }

        /// <summary>
        /// Negative feedback count.
        /// </summary>
        public int ThumbsDownCount { get; set; }

        /// <summary>
        /// Feedback count that is neither positive nor negative.
        /// </summary>
        public int UnknownCount { get; set; }

        /// <summary>
        /// Total feedback count.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Ratio of negative feedback to total feedback.
        /// </summary>
        public double? NegativeRate { get; set; }
    }
}
