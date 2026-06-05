namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Aggregated endpoint and model usage summary.
    /// </summary>
    public class AssistantAnalyticsEndpointSummary
    {
        /// <summary>
        /// Endpoint identifier.
        /// </summary>
        public string EndpointId { get; set; }

        /// <summary>
        /// Endpoint name.
        /// </summary>
        public string EndpointName { get; set; }

        /// <summary>
        /// Endpoint type.
        /// </summary>
        public string EndpointType { get; set; }

        /// <summary>
        /// Provider name.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Endpoint API format.
        /// </summary>
        public string ApiFormat { get; set; }

        /// <summary>
        /// Model name.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Stage where the endpoint was used.
        /// </summary>
        public string Stage { get; set; }

        /// <summary>
        /// Call count.
        /// </summary>
        public int Calls { get; set; }

        /// <summary>
        /// Failed call count.
        /// </summary>
        public int Failures { get; set; }

        /// <summary>
        /// Average call duration in milliseconds.
        /// </summary>
        public double? AverageDurationMs { get; set; }

        /// <summary>
        /// P95 call duration in milliseconds.
        /// </summary>
        public double? P95DurationMs { get; set; }

        /// <summary>
        /// Average endpoint limiter wait in milliseconds.
        /// </summary>
        public double? AverageLimiterWaitMs { get; set; }

        /// <summary>
        /// P95 endpoint limiter wait in milliseconds.
        /// </summary>
        public double? P95LimiterWaitMs { get; set; }

        /// <summary>
        /// Average request-to-headers duration in milliseconds.
        /// </summary>
        public double? AverageRequestToHeadersMs { get; set; }

        /// <summary>
        /// Average provider-reported load duration in milliseconds.
        /// </summary>
        public double? AverageProviderLoadMs { get; set; }

        /// <summary>
        /// Average provider-reported generation duration in milliseconds.
        /// </summary>
        public double? AverageProviderGenerationMs { get; set; }

        /// <summary>
        /// Average output throughput in tokens per second.
        /// </summary>
        public double? AverageTokensPerSecond { get; set; }

        /// <summary>
        /// Input token count.
        /// </summary>
        public int InputTokens { get; set; }

        /// <summary>
        /// Output token count.
        /// </summary>
        public int OutputTokens { get; set; }
    }
}
