namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint analytics result.
    /// </summary>
    public class AssistantAnalyticsEndpointResult
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
        /// Endpoint summaries.
        /// </summary>
        public List<AssistantAnalyticsEndpointSummary> Endpoints { get; set; } = new List<AssistantAnalyticsEndpointSummary>();
    }
}
