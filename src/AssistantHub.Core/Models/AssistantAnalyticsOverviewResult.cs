namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Assistant analytics overview.
    /// </summary>
    public class AssistantAnalyticsOverviewResult
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
        /// Total requests.
        /// </summary>
        public int RequestCount { get; set; } = 0;

        /// <summary>
        /// Successful requests.
        /// </summary>
        public int SuccessCount { get; set; } = 0;

        /// <summary>
        /// Failed requests.
        /// </summary>
        public int FailureCount { get; set; } = 0;

        /// <summary>
        /// Success rate.
        /// </summary>
        public double? SuccessRate { get; set; } = null;

        /// <summary>
        /// Failure rate.
        /// </summary>
        public double? FailureRate { get; set; } = null;

        /// <summary>
        /// Average request duration.
        /// </summary>
        public double? AverageDurationMs { get; set; } = null;

        /// <summary>
        /// Median request duration.
        /// </summary>
        public double? P50DurationMs { get; set; } = null;

        /// <summary>
        /// 90th percentile request duration.
        /// </summary>
        public double? P90DurationMs { get; set; } = null;

        /// <summary>
        /// 95th percentile request duration.
        /// </summary>
        public double? P95DurationMs { get; set; } = null;

        /// <summary>
        /// 99th percentile request duration.
        /// </summary>
        public double? P99DurationMs { get; set; } = null;

        /// <summary>
        /// Maximum request duration.
        /// </summary>
        public double? MaxDurationMs { get; set; } = null;

        /// <summary>
        /// Number of performance-event rows.
        /// </summary>
        public int TelemetryEventCount { get; set; } = 0;

        /// <summary>
        /// Number of requests with linked performance events.
        /// </summary>
        public int RequestsWithTelemetry { get; set; } = 0;

        /// <summary>
        /// Telemetry coverage rate.
        /// </summary>
        public double? TelemetryCoverageRate { get; set; } = null;

        /// <summary>
        /// Highest aggregate-duration stage.
        /// </summary>
        public string DominantStage { get; set; } = null;

        /// <summary>
        /// Average duration of the dominant stage.
        /// </summary>
        public double? DominantStageAverageMs { get; set; } = null;

        /// <summary>
        /// Top endpoint identifier.
        /// </summary>
        public string TopEndpointId { get; set; } = null;

        /// <summary>
        /// Top endpoint display name.
        /// </summary>
        public string TopEndpointName { get; set; } = null;

        /// <summary>
        /// Top endpoint provider.
        /// </summary>
        public string TopEndpointProvider { get; set; } = null;

        /// <summary>
        /// Top endpoint model.
        /// </summary>
        public string TopEndpointModel { get; set; } = null;

        /// <summary>
        /// Total feedback rows.
        /// </summary>
        public int FeedbackCount { get; set; } = 0;

        /// <summary>
        /// Positive feedback rows.
        /// </summary>
        public int ThumbsUpCount { get; set; } = 0;

        /// <summary>
        /// Negative feedback rows.
        /// </summary>
        public int ThumbsDownCount { get; set; } = 0;

        /// <summary>
        /// Negative feedback rate.
        /// </summary>
        public double? NegativeFeedbackRate { get; set; } = null;
    }
}
