namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Redacted persistent trace for one model-directed assistant tool call.
    /// </summary>
    public class AssistantToolCallRecord
    {
        /// <summary>Record identifier.</summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>Tenant identifier.</summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>Assistant identifier.</summary>
        [JsonPropertyName("AssistantId")]
        public string AssistantId { get; set; }

        /// <summary>Linked chat-history identifier.</summary>
        [JsonPropertyName("ChatHistoryId")]
        public string ChatHistoryId { get; set; }

        /// <summary>Linked request-history identifier.</summary>
        [JsonPropertyName("RequestHistoryId")]
        public string RequestHistoryId { get; set; }

        /// <summary>Trace identifier.</summary>
        [JsonPropertyName("TraceId")]
        public string TraceId { get; set; }

        /// <summary>Thread identifier.</summary>
        [JsonPropertyName("ThreadId")]
        public string ThreadId { get; set; }

        /// <summary>Request origin.</summary>
        [JsonPropertyName("Origin")]
        public string Origin { get; set; }

        /// <summary>Zero-based chat turn index when available.</summary>
        [JsonPropertyName("TurnIndex")]
        public int TurnIndex { get; set; }

        /// <summary>One-based tool-loop iteration.</summary>
        [JsonPropertyName("Iteration")]
        public int Iteration { get; set; }

        /// <summary>One-based sequence number within the chat turn.</summary>
        [JsonPropertyName("SequenceNumber")]
        public int SequenceNumber { get; set; }

        /// <summary>Provider-supplied tool-call identifier.</summary>
        [JsonPropertyName("ProviderToolCallId")]
        public string ProviderToolCallId { get; set; }

        /// <summary>Tool name.</summary>
        [JsonPropertyName("ToolName")]
        public string ToolName { get; set; }

        /// <summary>Redacted JSON arguments.</summary>
        [JsonPropertyName("ArgumentsJson")]
        public string ArgumentsJson { get; set; }

        /// <summary>Redacted JSON output summary.</summary>
        [JsonPropertyName("OutputJson")]
        public string OutputJson { get; set; }

        /// <summary>Redacted compact result summary JSON.</summary>
        [JsonPropertyName("ResultSummaryJson")]
        public string ResultSummaryJson { get; set; }

        /// <summary>Whether the tool call succeeded.</summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>Whether the tool call was denied before execution.</summary>
        [JsonPropertyName("Denied")]
        public bool Denied { get; set; }

        /// <summary>Whether tool output was truncated.</summary>
        [JsonPropertyName("Truncated")]
        public bool Truncated { get; set; }

        /// <summary>Tool output character count.</summary>
        [JsonPropertyName("OutputCharacters")]
        public int OutputCharacters { get; set; }

        /// <summary>Redacted input byte count.</summary>
        [JsonPropertyName("InputBytes")]
        public int InputBytes { get; set; }

        /// <summary>Redacted output byte count.</summary>
        [JsonPropertyName("OutputBytes")]
        public int OutputBytes { get; set; }

        /// <summary>Tool execution duration in milliseconds.</summary>
        [JsonPropertyName("DurationMs")]
        public double DurationMs { get; set; }

        /// <summary>Stable error type.</summary>
        [JsonPropertyName("ErrorType")]
        public string ErrorType { get; set; }

        /// <summary>Safe error message.</summary>
        [JsonPropertyName("ErrorMessage")]
        public string ErrorMessage { get; set; }

        /// <summary>Provider used for the chat turn.</summary>
        [JsonPropertyName("Provider")]
        public string Provider { get; set; }

        /// <summary>Model used for the chat turn.</summary>
        [JsonPropertyName("Model")]
        public string Model { get; set; }

        /// <summary>Whether the trace record is active.</summary>
        [JsonPropertyName("Active")]
        public bool Active { get; set; }

        /// <summary>UTC timestamp when the tool call started.</summary>
        [JsonPropertyName("StartedUtc")]
        public DateTime? StartedUtc { get; set; }

        /// <summary>UTC timestamp when the tool call finished.</summary>
        [JsonPropertyName("FinishedUtc")]
        public DateTime? FinishedUtc { get; set; }

        /// <summary>UTC timestamp when the trace record was created.</summary>
        [JsonPropertyName("CreatedUtc")]
        public DateTime? CreatedUtc { get; set; }

        /// <summary>UTC timestamp when the trace record was last updated.</summary>
        [JsonPropertyName("LastUpdateUtc")]
        public DateTime? LastUpdateUtc { get; set; }
    }
}
