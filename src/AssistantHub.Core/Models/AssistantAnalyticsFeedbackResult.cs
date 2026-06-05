namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Feedback analytics result.
    /// </summary>
    public class AssistantAnalyticsFeedbackResult
    {
        /// <summary>
        /// Assistant identifier.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Resolved range.
        /// </summary>
        public AssistantAnalyticsRange Range { get; set; } = null;

        /// <summary>
        /// Timestamp when generated.
        /// </summary>
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total feedback count.
        /// </summary>
        public int TotalCount { get; set; } = 0;

        /// <summary>
        /// Positive feedback count.
        /// </summary>
        public int ThumbsUpCount { get; set; } = 0;

        /// <summary>
        /// Negative feedback count.
        /// </summary>
        public int ThumbsDownCount { get; set; } = 0;

        /// <summary>
        /// Negative feedback rate.
        /// </summary>
        public double? NegativeRate { get; set; } = null;

        /// <summary>
        /// Feedback buckets.
        /// </summary>
        public List<AssistantAnalyticsFeedbackBucket> Buckets { get; set; } = new List<AssistantAnalyticsFeedbackBucket>();
    }
}
