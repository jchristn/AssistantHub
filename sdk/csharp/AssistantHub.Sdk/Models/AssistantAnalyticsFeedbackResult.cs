namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Feedback analytics response.
    /// </summary>
    public class AssistantAnalyticsFeedbackResult
    {
        /// <summary>
        /// Assistant identifier.
        /// </summary>
        public string AssistantId { get; set; }

        /// <summary>
        /// Resolved analytics range.
        /// </summary>
        public AssistantAnalyticsRange Range { get; set; }

        /// <summary>
        /// UTC timestamp when the response was generated.
        /// </summary>
        public DateTime GeneratedUtc { get; set; }

        /// <summary>
        /// Total feedback count.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Positive feedback count.
        /// </summary>
        public int ThumbsUpCount { get; set; }

        /// <summary>
        /// Negative feedback count.
        /// </summary>
        public int ThumbsDownCount { get; set; }

        /// <summary>
        /// Ratio of negative feedback to total feedback.
        /// </summary>
        public double? NegativeRate { get; set; }

        /// <summary>
        /// Feedback buckets.
        /// </summary>
        public List<AssistantAnalyticsFeedbackBucket> Buckets { get; set; } = new List<AssistantAnalyticsFeedbackBucket>();
    }
}
