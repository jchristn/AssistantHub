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

        /// <summary>
        /// Number of tool calls in the request.
        /// </summary>
        public int ToolCallCount { get; set; } = 0;

        /// <summary>
        /// Number of failed non-denied tool calls in the request.
        /// </summary>
        public int ToolFailureCount { get; set; } = 0;

        /// <summary>
        /// Number of policy-denied tool calls in the request.
        /// </summary>
        public int ToolDeniedCount { get; set; } = 0;

        /// <summary>
        /// Number of truncated tool outputs in the request.
        /// </summary>
        public int ToolTruncatedCount { get; set; } = 0;

        /// <summary>
        /// Aggregate tool duration.
        /// </summary>
        public double? ToolDurationMs { get; set; } = null;

        /// <summary>
        /// Slowest tool name, when known.
        /// </summary>
        public string SlowestToolName { get; set; } = null;

        /// <summary>
        /// Slowest tool aggregate duration, when known.
        /// </summary>
        public double? SlowestToolDurationMs { get; set; } = null;

        /// <summary>
        /// Tool names with at least one failure or denial.
        /// </summary>
        public List<string> FailingToolNames { get; set; } = new List<string>();
    }
}
