namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Slow request result.
    /// </summary>
    public class AssistantAnalyticsSlowestResult
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
        /// Slowest requests.
        /// </summary>
        public List<AssistantAnalyticsSlowRequest> Requests { get; set; } = new List<AssistantAnalyticsSlowRequest>();
    }
}
