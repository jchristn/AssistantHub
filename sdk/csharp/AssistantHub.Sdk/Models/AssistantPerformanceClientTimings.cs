namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Client-observed timings for an upstream provider call.
    /// </summary>
    public class AssistantPerformanceClientTimings
    {
        /// <summary>
        /// Time spent waiting for the endpoint concurrency limiter.
        /// </summary>
        [JsonPropertyName("EndpointLimiterWaitMs")]
        public double? EndpointLimiterWaitMs { get; set; }

        /// <summary>
        /// Time from sending the request to receiving response headers.
        /// </summary>
        [JsonPropertyName("RequestToHeadersMs")]
        public double? RequestToHeadersMs { get; set; }

        /// <summary>
        /// Time from response headers to first streamed token.
        /// </summary>
        [JsonPropertyName("HeadersToFirstTokenMs")]
        public double? HeadersToFirstTokenMs { get; set; }

        /// <summary>
        /// Time from first streamed token to the final streamed token.
        /// </summary>
        [JsonPropertyName("FirstTokenToLastTokenMs")]
        public double? FirstTokenToLastTokenMs { get; set; }

        /// <summary>
        /// Total client-observed duration.
        /// </summary>
        [JsonPropertyName("TotalMs")]
        public double? TotalMs { get; set; }
    }
}
