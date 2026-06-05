namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Time-series analytics response.
    /// </summary>
    public class AssistantAnalyticsTimeSeriesResult
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
        /// Metric series.
        /// </summary>
        public List<AssistantAnalyticsSeries> Series { get; set; } = new List<AssistantAnalyticsSeries>();
    }
}
