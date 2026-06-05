namespace AssistantHub.Sdk.Models
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
        [JsonPropertyName("SchemaVersion")]
        public int SchemaVersion { get; set; }

        /// <summary>
        /// Correlation identifier shared by chat history, request history, and logs.
        /// </summary>
        [JsonPropertyName("TraceId")]
        public string TraceId { get; set; }

        /// <summary>
        /// Chat history identifier when known.
        /// </summary>
        [JsonPropertyName("ChatHistoryId")]
        public string ChatHistoryId { get; set; }

        /// <summary>
        /// Request history identifier when known.
        /// </summary>
        [JsonPropertyName("RequestHistoryId")]
        public string RequestHistoryId { get; set; }

        /// <summary>
        /// Approximate wall-clock time for the assistant pipeline.
        /// </summary>
        [JsonPropertyName("WallTimeMs")]
        public double WallTimeMs { get; set; }

        /// <summary>
        /// Timestamp when telemetry was created.
        /// </summary>
        [JsonPropertyName("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Ordered stage timings.
        /// </summary>
        [JsonPropertyName("Stages")]
        public List<AssistantPerformanceStage> Stages { get; set; } = new List<AssistantPerformanceStage>();
    }
}
