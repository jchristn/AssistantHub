namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A measured stage or phase in the assistant pipeline.
    /// </summary>
    public class AssistantPerformanceStage
    {
        /// <summary>
        /// Display name for the measured stage.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Stage kind, such as operation, inference, retrieval, or persistence.
        /// </summary>
        public string Kind { get; set; } = "operation";

        /// <summary>
        /// Ordering value within the telemetry payload.
        /// </summary>
        public int Sequence { get; set; } = 0;

        /// <summary>
        /// Endpoint identifier used by the stage, when applicable.
        /// </summary>
        public string EndpointId { get; set; } = null;

        /// <summary>
        /// Endpoint display name used by the stage, when applicable.
        /// </summary>
        public string EndpointName { get; set; } = null;

        /// <summary>
        /// Endpoint type used by the stage, when applicable.
        /// </summary>
        public string EndpointType { get; set; } = null;

        /// <summary>
        /// Provider name, such as Ollama or OpenAI-compatible.
        /// </summary>
        public string Provider { get; set; } = null;

        /// <summary>
        /// API format used for the provider call.
        /// </summary>
        public string ApiFormat { get; set; } = null;

        /// <summary>
        /// Model name used for the provider call.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// UTC timestamp when the stage started.
        /// </summary>
        public DateTime? StartedUtc { get; set; } = null;

        /// <summary>
        /// UTC timestamp when the stage finished.
        /// </summary>
        public DateTime? FinishedUtc { get; set; } = null;

        /// <summary>
        /// Stage duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        /// <summary>
        /// Indicates whether the stage completed successfully.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// HTTP status code returned by the provider or service, when available.
        /// </summary>
        public int? HttpStatusCode { get; set; } = null;

        /// <summary>
        /// Machine-readable error type, when the stage failed.
        /// </summary>
        public string ErrorType { get; set; } = null;

        /// <summary>
        /// Error message, when the stage failed.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// Client-observed timing breakdown for provider calls.
        /// </summary>
        public AssistantPerformanceClientTimings ClientTimings { get; set; } = null;

        /// <summary>
        /// Normalized token counters for the stage.
        /// </summary>
        public AssistantTokenUsageTelemetry Tokens { get; set; } = null;

        /// <summary>
        /// Normalized provider-native metrics.
        /// </summary>
        public AssistantProviderMetrics ProviderMetrics { get; set; } = null;

        /// <summary>
        /// Additional provider-agnostic metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = null;

        /// <summary>
        /// Provider-specific raw metrics retained for troubleshooting.
        /// </summary>
        public Dictionary<string, object> ProviderRaw { get; set; } = null;
    }
}
