namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Time-series definition.
    /// </summary>
    public class AssistantAnalyticsSeries
    {
        /// <summary>
        /// Metric machine name.
        /// </summary>
        public string Metric { get; set; } = null;

        /// <summary>
        /// Display label.
        /// </summary>
        public string Label { get; set; } = null;

        /// <summary>
        /// Unit for values.
        /// </summary>
        public string Unit { get; set; } = null;

        /// <summary>
        /// Series points.
        /// </summary>
        public List<AssistantAnalyticsPoint> Points { get; set; } = new List<AssistantAnalyticsPoint>();
    }
}
