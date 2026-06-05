namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint analytics response.
    /// </summary>
    public class AssistantAnalyticsEndpointResult
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
        /// Endpoint summaries.
        /// </summary>
        public List<AssistantAnalyticsEndpointSummary> Endpoints { get; set; } = new List<AssistantAnalyticsEndpointSummary>();
    }
}
