namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A named analytics metric series.
    /// </summary>
    public class AssistantAnalyticsSeries
    {
        /// <summary>
        /// Metric identifier.
        /// </summary>
        public string Metric { get; set; }

        /// <summary>
        /// Human-readable metric label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Metric unit.
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Time-series points.
        /// </summary>
        public List<AssistantAnalyticsPoint> Points { get; set; } = new List<AssistantAnalyticsPoint>();
    }
}
