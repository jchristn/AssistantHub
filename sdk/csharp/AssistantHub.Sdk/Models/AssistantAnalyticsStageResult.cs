namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Performance-stage analytics response.
    /// </summary>
    public class AssistantAnalyticsStageResult
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
        /// Stage buckets.
        /// </summary>
        public List<AssistantAnalyticsStageBucket> Buckets { get; set; } = new List<AssistantAnalyticsStageBucket>();
    }
}
