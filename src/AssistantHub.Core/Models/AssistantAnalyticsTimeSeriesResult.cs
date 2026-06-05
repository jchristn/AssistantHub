namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Time-series analytics result.
    /// </summary>
    public class AssistantAnalyticsTimeSeriesResult
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
        /// Chart-ready series.
        /// </summary>
        public List<AssistantAnalyticsSeries> Series { get; set; } = new List<AssistantAnalyticsSeries>();
    }
}
