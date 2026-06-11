namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Slow assistant request with hot-path context.
    /// </summary>
    public class AssistantAnalyticsSlowRequest
    {
        /// <summary>
        /// Request-history identifier.
        /// </summary>
        public string RequestHistoryId { get; set; }

        /// <summary>
        /// Chat-history identifier.
        /// </summary>
        public string ChatHistoryId { get; set; }

        /// <summary>
        /// Trace identifier.
        /// </summary>
        public string TraceId { get; set; }

        /// <summary>
        /// UTC request creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Whether the request succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Total request duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Request path.
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        /// Highest-duration performance stage.
        /// </summary>
        public string DominantStage { get; set; }

        /// <summary>
        /// Dominant-stage duration in milliseconds.
        /// </summary>
        public double? DominantStageDurationMs { get; set; }

        /// <summary>
        /// Endpoint identifier used by the dominant stage.
        /// </summary>
        public string EndpointId { get; set; }

        /// <summary>
        /// Endpoint name used by the dominant stage.
        /// </summary>
        public string EndpointName { get; set; }

        /// <summary>
        /// Provider used by the dominant stage.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Model used by the dominant stage.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Number of tool calls in the request.
        /// </summary>
        public int ToolCallCount { get; set; }

        /// <summary>
        /// Number of failed non-denied tool calls.
        /// </summary>
        public int ToolFailureCount { get; set; }

        /// <summary>
        /// Number of policy-denied tool calls.
        /// </summary>
        public int ToolDeniedCount { get; set; }

        /// <summary>
        /// Number of truncated tool outputs.
        /// </summary>
        public int ToolTruncatedCount { get; set; }

        /// <summary>
        /// Aggregate tool duration in milliseconds.
        /// </summary>
        public double? ToolDurationMs { get; set; }

        /// <summary>
        /// Slowest tool name.
        /// </summary>
        public string SlowestToolName { get; set; }

        /// <summary>
        /// Slowest tool duration in milliseconds.
        /// </summary>
        public double? SlowestToolDurationMs { get; set; }

        /// <summary>
        /// Tool names with at least one failure or denial.
        /// </summary>
        public List<string> FailingToolNames { get; set; } = new List<string>();
    }
}
