namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Stage analytics result.
    /// </summary>
    public class AssistantAnalyticsStageResult
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
        /// Stage buckets.
        /// </summary>
        public List<AssistantAnalyticsStageBucket> Buckets { get; set; } = new List<AssistantAnalyticsStageBucket>();
    }
}
