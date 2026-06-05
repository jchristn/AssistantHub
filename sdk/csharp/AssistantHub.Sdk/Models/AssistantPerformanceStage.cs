namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A measured stage in the assistant pipeline.
    /// </summary>
    public class AssistantPerformanceStage
    {
        /// <summary>
        /// Display name for the measured stage.
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Stage kind, such as operation, inference, retrieval, or persistence.
        /// </summary>
        [JsonPropertyName("Kind")]
        public string Kind { get; set; }

        /// <summary>
        /// Ordering value within the telemetry payload.
        /// </summary>
        [JsonPropertyName("Sequence")]
        public int Sequence { get; set; }

        /// <summary>
        /// Endpoint identifier used by the stage, when applicable.
        /// </summary>
        [JsonPropertyName("EndpointId")]
        public string EndpointId { get; set; }

        /// <summary>
        /// Endpoint display name used by the stage, when applicable.
        /// </summary>
        [JsonPropertyName("EndpointName")]
        public string EndpointName { get; set; }

        /// <summary>
        /// Endpoint type used by the stage, when applicable.
        /// </summary>
        [JsonPropertyName("EndpointType")]
        public string EndpointType { get; set; }

        /// <summary>
        /// Provider name, such as Ollama or OpenAI-compatible.
        /// </summary>
        [JsonPropertyName("Provider")]
        public string Provider { get; set; }

        /// <summary>
        /// API format used for the provider call.
        /// </summary>
        [JsonPropertyName("ApiFormat")]
        public string ApiFormat { get; set; }

        /// <summary>
        /// Model name used for the provider call.
        /// </summary>
        [JsonPropertyName("Model")]
        public string Model { get; set; }

        /// <summary>
        /// UTC timestamp when the stage started.
        /// </summary>
        [JsonPropertyName("StartedUtc")]
        public DateTime? StartedUtc { get; set; }

        /// <summary>
        /// UTC timestamp when the stage finished.
        /// </summary>
        [JsonPropertyName("FinishedUtc")]
        public DateTime? FinishedUtc { get; set; }

        /// <summary>
        /// Stage duration in milliseconds.
        /// </summary>
        [JsonPropertyName("DurationMs")]
        public double DurationMs { get; set; }

        /// <summary>
        /// Indicates whether the stage completed successfully.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// HTTP status code returned by the provider or service, when available.
        /// </summary>
        [JsonPropertyName("HttpStatusCode")]
        public int? HttpStatusCode { get; set; }

        /// <summary>
        /// Machine-readable error type, when the stage failed.
        /// </summary>
        [JsonPropertyName("ErrorType")]
        public string ErrorType { get; set; }

        /// <summary>
        /// Error message, when the stage failed.
        /// </summary>
        [JsonPropertyName("ErrorMessage")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Client-observed timing breakdown for provider calls.
        /// </summary>
        [JsonPropertyName("ClientTimings")]
        public AssistantPerformanceClientTimings ClientTimings { get; set; }

        /// <summary>
        /// Normalized token counters for the stage.
        /// </summary>
        [JsonPropertyName("Tokens")]
        public AssistantTokenUsageTelemetry Tokens { get; set; }

        /// <summary>
        /// Normalized provider-native metrics.
        /// </summary>
        [JsonPropertyName("ProviderMetrics")]
        public AssistantProviderMetrics ProviderMetrics { get; set; }

        /// <summary>
        /// Additional provider-agnostic metadata.
        /// </summary>
        [JsonPropertyName("Metadata")]
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Provider-specific raw metrics retained for troubleshooting.
        /// </summary>
        [JsonPropertyName("ProviderRaw")]
        public Dictionary<string, object> ProviderRaw { get; set; }
    }
}
