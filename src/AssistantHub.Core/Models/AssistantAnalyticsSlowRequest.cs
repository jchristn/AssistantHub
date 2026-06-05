namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Slow request diagnostic row.
    /// </summary>
    public class AssistantAnalyticsSlowRequest
    {
        /// <summary>
        /// Request-history identifier.
        /// </summary>
        public string RequestHistoryId { get; set; } = null;

        /// <summary>
        /// Chat-history identifier.
        /// </summary>
        public string ChatHistoryId { get; set; } = null;

        /// <summary>
        /// Trace identifier.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// Created timestamp in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        /// Success flag.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Total request duration.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        /// <summary>
        /// Request path.
        /// </summary>
        public string RequestPath { get; set; } = null;

        /// <summary>
        /// Dominant stage name.
        /// </summary>
        public string DominantStage { get; set; } = null;

        /// <summary>
        /// Dominant stage duration.
        /// </summary>
        public double? DominantStageDurationMs { get; set; } = null;

        /// <summary>
        /// Endpoint identifier.
        /// </summary>
        public string EndpointId { get; set; } = null;

        /// <summary>
        /// Endpoint name.
        /// </summary>
        public string EndpointName { get; set; } = null;

        /// <summary>
        /// Provider.
        /// </summary>
        public string Provider { get; set; } = null;

        /// <summary>
        /// Model.
        /// </summary>
        public string Model { get; set; } = null;
    }
}
