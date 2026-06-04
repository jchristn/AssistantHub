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

    /// <summary>
    /// Normalized token counters.
    /// </summary>
    public class AssistantTokenUsageTelemetry
    {
        /// <summary>
        /// Input token count, when reported or estimated.
        /// </summary>
        [JsonPropertyName("Input")]
        public int? Input { get; set; }

        /// <summary>
        /// Output token count, when reported or estimated.
        /// </summary>
        [JsonPropertyName("Output")]
        public int? Output { get; set; }

        /// <summary>
        /// Total token count, when reported or estimated.
        /// </summary>
        [JsonPropertyName("Total")]
        public int? Total { get; set; }

        /// <summary>
        /// Provider prompt-evaluation token count, when reported.
        /// </summary>
        [JsonPropertyName("PromptEvalCount")]
        public int? PromptEvalCount { get; set; }

        /// <summary>
        /// Provider generation token count, when reported.
        /// </summary>
        [JsonPropertyName("EvalCount")]
        public int? EvalCount { get; set; }
    }

    /// <summary>
    /// Provider-native metrics normalized into common fields.
    /// </summary>
    public class AssistantProviderMetrics
    {
        /// <summary>
        /// Provider queue duration in milliseconds, when reported.
        /// </summary>
        [JsonPropertyName("QueueMs")]
        public double? QueueMs { get; set; }

        /// <summary>
        /// Provider model-load duration in milliseconds, when reported.
        /// </summary>
        [JsonPropertyName("LoadMs")]
        public double? LoadMs { get; set; }

        /// <summary>
        /// Provider prompt-evaluation duration in milliseconds, when reported.
        /// </summary>
        [JsonPropertyName("PromptEvalMs")]
        public double? PromptEvalMs { get; set; }

        /// <summary>
        /// Provider generation duration in milliseconds, when reported.
        /// </summary>
        [JsonPropertyName("GenerationMs")]
        public double? GenerationMs { get; set; }

        /// <summary>
        /// Provider-reported total duration in milliseconds, when reported.
        /// </summary>
        [JsonPropertyName("TotalMs")]
        public double? TotalMs { get; set; }

        /// <summary>
        /// Provider generation throughput in tokens per second, when derivable.
        /// </summary>
        [JsonPropertyName("TokensPerSecond")]
        public double? TokensPerSecond { get; set; }

        /// <summary>
        /// Provider request identifier, when reported.
        /// </summary>
        [JsonPropertyName("RequestId")]
        public string RequestId { get; set; }
    }
}
