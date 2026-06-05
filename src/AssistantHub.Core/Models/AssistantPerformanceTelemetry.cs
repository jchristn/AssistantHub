namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Versioned provider-agnostic performance telemetry for a chat turn.
    /// </summary>
    public class AssistantPerformanceTelemetry
    {
        /// <summary>
        /// Telemetry schema version.
        /// </summary>
        public int SchemaVersion { get; set; } = 1;

        /// <summary>
        /// Correlation identifier shared by chat history, request history, and logs.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// Chat history identifier when known.
        /// </summary>
        public string ChatHistoryId { get; set; } = null;

        /// <summary>
        /// Request history identifier when known.
        /// </summary>
        public string RequestHistoryId { get; set; } = null;

        /// <summary>
        /// Assistant identifier when known.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Approximate wall-clock time for the assistant pipeline.
        /// </summary>
        public double WallTimeMs { get; set; } = 0;

        /// <summary>
        /// Timestamp when telemetry was created.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Ordered stage timings.
        /// </summary>
        public List<AssistantPerformanceStage> Stages { get; set; } = new List<AssistantPerformanceStage>();
    }
}
