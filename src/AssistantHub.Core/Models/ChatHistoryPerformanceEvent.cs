namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Queryable performance event derived from a chat history telemetry payload.
    /// </summary>
    public class ChatHistoryPerformanceEvent
    {
        /// <summary>
        /// Event identifier.
        /// </summary>
        public string Id { get; set; } = IdGenerator.NewChatHistoryPerformanceEventId();

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = Constants.DefaultTenantId;

        /// <summary>
        /// Associated assistant identifier.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Associated chat history identifier.
        /// </summary>
        public string ChatHistoryId { get; set; } = null;

        /// <summary>
        /// Associated request history identifier, when available.
        /// </summary>
        public string RequestHistoryId { get; set; } = null;

        /// <summary>
        /// Correlation identifier shared by chat history, request history, and logs.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// Ordering value within the chat turn.
        /// </summary>
        public int SequenceNumber { get; set; } = 0;

        /// <summary>
        /// Stage name.
        /// </summary>
        public string Stage { get; set; } = null;

        /// <summary>
        /// Stage phase, when a stage has finer-grained phases.
        /// </summary>
        public string Phase { get; set; } = null;

        /// <summary>
        /// Stage kind, such as operation, inference, retrieval, or persistence.
        /// </summary>
        public string Kind { get; set; } = "operation";

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
        /// Provider name, when the stage calls an upstream provider.
        /// </summary>
        public string Provider { get; set; } = null;

        /// <summary>
        /// API format used for the upstream provider.
        /// </summary>
        public string ApiFormat { get; set; } = null;

        /// <summary>
        /// Model name used for the upstream provider.
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
        /// Input token count, when reported or estimated.
        /// </summary>
        public int? InputTokens { get; set; } = null;

        /// <summary>
        /// Output token count, when reported or estimated.
        /// </summary>
        public int? OutputTokens { get; set; } = null;

        /// <summary>
        /// Total token count, when reported or estimated.
        /// </summary>
        public int? TotalTokens { get; set; } = null;

        /// <summary>
        /// Number of chunks entering the stage, when applicable.
        /// </summary>
        public int? ChunksInput { get; set; } = null;

        /// <summary>
        /// Number of chunks leaving the stage, when applicable.
        /// </summary>
        public int? ChunksOutput { get; set; } = null;

        /// <summary>
        /// Number of retrieval queries executed, when applicable.
        /// </summary>
        public int? RetrievalQueryCount { get; set; } = null;

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
        /// Time from first streamed token to final streamed token.
        /// </summary>
        public double? FirstTokenToLastTokenMs { get; set; } = null;

        /// <summary>
        /// Total client-observed duration.
        /// </summary>
        public double? ClientTotalMs { get; set; } = null;

        /// <summary>
        /// Provider queue duration in milliseconds, when reported.
        /// </summary>
        public double? ProviderQueueMs { get; set; } = null;

        /// <summary>
        /// Provider model-load duration in milliseconds, when reported.
        /// </summary>
        public double? ProviderLoadMs { get; set; } = null;

        /// <summary>
        /// Provider prompt-evaluation duration in milliseconds, when reported.
        /// </summary>
        public double? ProviderPromptEvalMs { get; set; } = null;

        /// <summary>
        /// Provider generation duration in milliseconds, when reported.
        /// </summary>
        public double? ProviderGenerationMs { get; set; } = null;

        /// <summary>
        /// Provider-reported total duration in milliseconds, when reported.
        /// </summary>
        public double? ProviderTotalMs { get; set; } = null;

        /// <summary>
        /// Provider generation throughput in tokens per second, when derivable.
        /// </summary>
        public double? ProviderTokensPerSecond { get; set; } = null;

        /// <summary>
        /// Provider request identifier, when reported.
        /// </summary>
        public string ProviderRequestId { get; set; } = null;

        /// <summary>
        /// JSON-serialized additional metadata.
        /// </summary>
        public string MetadataJson { get; set; } = null;

        /// <summary>
        /// JSON-serialized normalized provider metrics.
        /// </summary>
        public string ProviderMetricsJson { get; set; } = null;

        /// <summary>
        /// JSON-serialized provider-specific raw metrics.
        /// </summary>
        public string ProviderRawJson { get; set; } = null;

        /// <summary>
        /// UTC timestamp when the event row was created.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Build an event from a data row.
        /// </summary>
        public static ChatHistoryPerformanceEvent FromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new ChatHistoryPerformanceEvent
            {
                Id = DataTableHelper.GetStringValue(row, "id"),
                TenantId = DataTableHelper.GetStringValue(row, "tenant_id"),
                AssistantId = DataTableHelper.GetStringValue(row, "assistant_id"),
                ChatHistoryId = DataTableHelper.GetStringValue(row, "chat_history_id"),
                RequestHistoryId = DataTableHelper.GetStringValue(row, "request_history_id"),
                TraceId = DataTableHelper.GetStringValue(row, "trace_id"),
                SequenceNumber = DataTableHelper.GetIntValue(row, "sequence_number"),
                Stage = DataTableHelper.GetStringValue(row, "stage"),
                Phase = DataTableHelper.GetStringValue(row, "phase"),
                Kind = DataTableHelper.GetStringValue(row, "kind"),
                EndpointId = DataTableHelper.GetStringValue(row, "endpoint_id"),
                EndpointName = DataTableHelper.GetStringValue(row, "endpoint_name"),
                EndpointType = DataTableHelper.GetStringValue(row, "endpoint_type"),
                Provider = DataTableHelper.GetStringValue(row, "provider"),
                ApiFormat = DataTableHelper.GetStringValue(row, "api_format"),
                Model = DataTableHelper.GetStringValue(row, "model"),
                StartedUtc = DataTableHelper.GetNullableDateTimeValue(row, "started_utc"),
                FinishedUtc = DataTableHelper.GetNullableDateTimeValue(row, "finished_utc"),
                DurationMs = DataTableHelper.GetDoubleValue(row, "duration_ms"),
                Success = DataTableHelper.GetBooleanValue(row, "success", true),
                HttpStatusCode = DataTableHelper.GetNullableIntValue(row, "http_status_code"),
                ErrorType = DataTableHelper.GetStringValue(row, "error_type"),
                ErrorMessage = DataTableHelper.GetStringValue(row, "error_message"),
                InputTokens = DataTableHelper.GetNullableIntValue(row, "input_tokens"),
                OutputTokens = DataTableHelper.GetNullableIntValue(row, "output_tokens"),
                TotalTokens = DataTableHelper.GetNullableIntValue(row, "total_tokens"),
                ChunksInput = DataTableHelper.GetNullableIntValue(row, "chunks_input"),
                ChunksOutput = DataTableHelper.GetNullableIntValue(row, "chunks_output"),
                RetrievalQueryCount = DataTableHelper.GetNullableIntValue(row, "retrieval_query_count"),
                EndpointLimiterWaitMs = DataTableHelper.GetNullableDoubleValue(row, "endpoint_limiter_wait_ms"),
                RequestToHeadersMs = DataTableHelper.GetNullableDoubleValue(row, "request_to_headers_ms"),
                HeadersToFirstTokenMs = DataTableHelper.GetNullableDoubleValue(row, "headers_to_first_token_ms"),
                FirstTokenToLastTokenMs = DataTableHelper.GetNullableDoubleValue(row, "first_token_to_last_token_ms"),
                ClientTotalMs = DataTableHelper.GetNullableDoubleValue(row, "client_total_ms"),
                ProviderQueueMs = DataTableHelper.GetNullableDoubleValue(row, "provider_queue_ms"),
                ProviderLoadMs = DataTableHelper.GetNullableDoubleValue(row, "provider_load_ms"),
                ProviderPromptEvalMs = DataTableHelper.GetNullableDoubleValue(row, "provider_prompt_eval_ms"),
                ProviderGenerationMs = DataTableHelper.GetNullableDoubleValue(row, "provider_generation_ms"),
                ProviderTotalMs = DataTableHelper.GetNullableDoubleValue(row, "provider_total_ms"),
                ProviderTokensPerSecond = DataTableHelper.GetNullableDoubleValue(row, "provider_tokens_per_second"),
                ProviderRequestId = DataTableHelper.GetStringValue(row, "provider_request_id"),
                MetadataJson = DataTableHelper.GetStringValue(row, "metadata_json"),
                ProviderMetricsJson = DataTableHelper.GetStringValue(row, "provider_metrics_json"),
                ProviderRawJson = DataTableHelper.GetStringValue(row, "provider_raw_json"),
                CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc")
            };
        }
    }
}
