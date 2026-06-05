namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Named metric value.
    /// </summary>
    public class AssistantAnalyticsMetricValue
    {
        /// <summary>
        /// Metric name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Numeric value, or null when unavailable.
        /// </summary>
        public double? Value { get; set; } = null;

        /// <summary>
        /// Metric unit.
        /// </summary>
        public string Unit { get; set; } = null;

        /// <summary>
        /// Number of samples used.
        /// </summary>
        public int SampleCount { get; set; } = 0;

        /// <summary>
        /// Number of unavailable samples.
        /// </summary>
        public int NullCount { get; set; } = 0;
    }
}
