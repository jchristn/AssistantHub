namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Client-observed timings for an upstream provider request.
    /// </summary>
    public class AssistantPerformanceClientTimings
    {
        /// <summary>
        /// Time spent waiting for the endpoint concurrency limiter.
        /// </summary>
        public double? EndpointLimiterWaitMs { get; set; } = null;

        /// <summary>
        /// Time from sending the request to receiving response headers.
        /// </summary>
        public double? RequestToHeadersMs { get; set; } = null;

        /// <summary>
        /// Time from response headers to first streamed token.
        /// </summary>
        public double? HeadersToFirstTokenMs { get; set; } = null;

        /// <summary>
        /// Time from first streamed token to the final streamed token.
        /// </summary>
        public double? FirstTokenToLastTokenMs { get; set; } = null;

        /// <summary>
        /// Total client-observed duration.
        /// </summary>
        public double? TotalMs { get; set; } = null;
    }
}
