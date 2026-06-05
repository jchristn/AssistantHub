namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint/model/provider summary.
    /// </summary>
    public class AssistantAnalyticsEndpointSummary
    {
        /// <summary>
        /// Endpoint identifier.
        /// </summary>
        public string EndpointId { get; set; } = null;

        /// <summary>
        /// Endpoint name.
        /// </summary>
        public string EndpointName { get; set; } = null;

        /// <summary>
        /// Endpoint type.
        /// </summary>
        public string EndpointType { get; set; } = null;

        /// <summary>
        /// Provider.
        /// </summary>
        public string Provider { get; set; } = null;

        /// <summary>
        /// API format.
        /// </summary>
        public string ApiFormat { get; set; } = null;

        /// <summary>
        /// Model name.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Stage name.
        /// </summary>
        public string Stage { get; set; } = null;

        /// <summary>
        /// Calls.
        /// </summary>
        public int Calls { get; set; } = 0;

        /// <summary>
        /// Failures.
        /// </summary>
        public int Failures { get; set; } = 0;

        /// <summary>
        /// Average duration.
        /// </summary>
        public double? AverageDurationMs { get; set; } = null;

        /// <summary>
        /// 95th percentile duration.
        /// </summary>
        public double? P95DurationMs { get; set; } = null;

        /// <summary>
        /// Average limiter wait.
        /// </summary>
        public double? AverageLimiterWaitMs { get; set; } = null;

        /// <summary>
        /// 95th percentile limiter wait.
        /// </summary>
        public double? P95LimiterWaitMs { get; set; } = null;

        /// <summary>
        /// Average request-to-headers duration.
        /// </summary>
        public double? AverageRequestToHeadersMs { get; set; } = null;

        /// <summary>
        /// Average provider load duration.
        /// </summary>
        public double? AverageProviderLoadMs { get; set; } = null;

        /// <summary>
        /// Average provider generation duration.
        /// </summary>
        public double? AverageProviderGenerationMs { get; set; } = null;

        /// <summary>
        /// Average provider tokens per second.
        /// </summary>
        public double? AverageTokensPerSecond { get; set; } = null;

        /// <summary>
        /// Total input tokens.
        /// </summary>
        public int InputTokens { get; set; } = 0;

        /// <summary>
        /// Total output tokens.
        /// </summary>
        public int OutputTokens { get; set; } = 0;
    }
}
