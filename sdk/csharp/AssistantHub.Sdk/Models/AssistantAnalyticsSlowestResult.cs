namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Slowest-request analytics response.
    /// </summary>
    public class AssistantAnalyticsSlowestResult
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
        /// Slow request rows.
        /// </summary>
        public List<AssistantAnalyticsSlowRequest> Requests { get; set; } = new List<AssistantAnalyticsSlowRequest>();
    }
}
