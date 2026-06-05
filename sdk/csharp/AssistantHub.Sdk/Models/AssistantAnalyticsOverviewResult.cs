namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// High-level assistant analytics summary.
    /// </summary>
    public class AssistantAnalyticsOverviewResult
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
        /// Total request count.
        /// </summary>
        public int RequestCount { get; set; }

        /// <summary>
        /// Successful request count.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Failed request count.
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// Successful request ratio.
        /// </summary>
        public double? SuccessRate { get; set; }

        /// <summary>
        /// Failed request ratio.
        /// </summary>
        public double? FailureRate { get; set; }

        /// <summary>
        /// Average request duration in milliseconds.
        /// </summary>
        public double? AverageDurationMs { get; set; }

        /// <summary>
        /// P50 request duration in milliseconds.
        /// </summary>
        public double? P50DurationMs { get; set; }

        /// <summary>
        /// P90 request duration in milliseconds.
        /// </summary>
        public double? P90DurationMs { get; set; }

        /// <summary>
        /// P95 request duration in milliseconds.
        /// </summary>
        public double? P95DurationMs { get; set; }

        /// <summary>
        /// P99 request duration in milliseconds.
        /// </summary>
        public double? P99DurationMs { get; set; }

        /// <summary>
        /// Maximum request duration in milliseconds.
        /// </summary>
        public double? MaxDurationMs { get; set; }

        /// <summary>
        /// Number of linked performance telemetry events.
        /// </summary>
        public int TelemetryEventCount { get; set; }

        /// <summary>
        /// Number of requests with linked telemetry.
        /// </summary>
        public int RequestsWithTelemetry { get; set; }

        /// <summary>
        /// Ratio of requests with linked telemetry.
        /// </summary>
        public double? TelemetryCoverageRate { get; set; }

        /// <summary>
        /// Highest-total-duration performance stage.
        /// </summary>
        public string DominantStage { get; set; }

        /// <summary>
        /// Average duration for the dominant stage.
        /// </summary>
        public double? DominantStageAverageMs { get; set; }

        /// <summary>
        /// Most frequently used endpoint identifier.
        /// </summary>
        public string TopEndpointId { get; set; }

        /// <summary>
        /// Most frequently used endpoint name.
        /// </summary>
        public string TopEndpointName { get; set; }

        /// <summary>
        /// Provider for the most frequently used endpoint.
        /// </summary>
        public string TopEndpointProvider { get; set; }

        /// <summary>
        /// Model for the most frequently used endpoint.
        /// </summary>
        public string TopEndpointModel { get; set; }

        /// <summary>
        /// Feedback item count.
        /// </summary>
        public int FeedbackCount { get; set; }

        /// <summary>
        /// Positive feedback count.
        /// </summary>
        public int ThumbsUpCount { get; set; }

        /// <summary>
        /// Negative feedback count.
        /// </summary>
        public int ThumbsDownCount { get; set; }

        /// <summary>
        /// Ratio of negative feedback to total feedback.
        /// </summary>
        public double? NegativeFeedbackRate { get; set; }
    }
}
