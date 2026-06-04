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

    /// <summary>
    /// Normalized token counters.
    /// </summary>
    public class AssistantTokenUsageTelemetry
    {
        /// <summary>
        /// Input token count, when reported or estimated.
        /// </summary>
        public int? Input { get; set; } = null;

        /// <summary>
        /// Output token count, when reported or estimated.
        /// </summary>
        public int? Output { get; set; } = null;

        /// <summary>
        /// Total token count, when reported or estimated.
        /// </summary>
        public int? Total { get; set; } = null;

        /// <summary>
        /// Provider prompt-evaluation token count, when reported.
        /// </summary>
        public int? PromptEvalCount { get; set; } = null;

        /// <summary>
        /// Provider generation token count, when reported.
        /// </summary>
        public int? EvalCount { get; set; } = null;
    }

    /// <summary>
    /// Provider-native metrics normalized into common fields.
    /// </summary>
    public class AssistantProviderMetrics
    {
        /// <summary>
        /// Provider queue duration in milliseconds, when reported.
        /// </summary>
        public double? QueueMs { get; set; } = null;

        /// <summary>
        /// Provider model-load duration in milliseconds, when reported.
        /// </summary>
        public double? LoadMs { get; set; } = null;

        /// <summary>
        /// Provider prompt-evaluation duration in milliseconds, when reported.
        /// </summary>
        public double? PromptEvalMs { get; set; } = null;

        /// <summary>
        /// Provider generation duration in milliseconds, when reported.
        /// </summary>
        public double? GenerationMs { get; set; } = null;

        /// <summary>
        /// Provider-reported total duration in milliseconds, when reported.
        /// </summary>
        public double? TotalMs { get; set; } = null;

        /// <summary>
        /// Provider generation throughput in tokens per second, when derivable.
        /// </summary>
        public double? TokensPerSecond { get; set; } = null;

        /// <summary>
        /// Provider request identifier, when reported.
        /// </summary>
        public string RequestId { get; set; } = null;
    }
}
