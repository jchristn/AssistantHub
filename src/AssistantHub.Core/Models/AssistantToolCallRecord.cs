namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Redacted persistent trace for one model-directed assistant tool call.
    /// </summary>
    public class AssistantToolCallRecord
    {
        /// <summary>
        /// Record identifier.
        /// </summary>
        public string Id { get; set; } = IdGenerator.NewAssistantToolCallRecordId();

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = Constants.DefaultTenantId;

        /// <summary>
        /// Assistant identifier.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Chat history identifier, populated after chat history is persisted when available.
        /// </summary>
        public string ChatHistoryId { get; set; } = null;

        /// <summary>
        /// Request history identifier.
        /// </summary>
        public string RequestHistoryId { get; set; } = null;

        /// <summary>
        /// Trace identifier.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// Thread identifier.
        /// </summary>
        public string ThreadId { get; set; } = null;

        /// <summary>
        /// Request origin, such as web or slack.
        /// </summary>
        public string Origin { get; set; } = null;

        /// <summary>
        /// Zero-based chat turn index when available.
        /// </summary>
        public int TurnIndex { get; set; } = 0;

        /// <summary>
        /// One-based tool-loop iteration.
        /// </summary>
        public int Iteration { get; set; } = 0;

        /// <summary>
        /// One-based sequence number within the chat turn.
        /// </summary>
        public int SequenceNumber { get; set; } = 0;

        /// <summary>
        /// Provider-supplied tool call identifier.
        /// </summary>
        public string ProviderToolCallId { get; set; } = null;

        /// <summary>
        /// Tool name.
        /// </summary>
        public string ToolName { get; set; } = null;

        /// <summary>
        /// Redacted JSON arguments.
        /// </summary>
        public string ArgumentsJson { get; set; } = null;

        /// <summary>
        /// Redacted JSON output summary or truncated model-visible output.
        /// </summary>
        public string OutputJson { get; set; } = null;

        /// <summary>
        /// Redacted compact result summary JSON.
        /// </summary>
        public string ResultSummaryJson { get; set; } = null;

        /// <summary>
        /// Whether the tool succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Whether the tool was denied before execution.
        /// </summary>
        public bool Denied { get; set; } = false;

        /// <summary>
        /// Whether the tool output was truncated.
        /// </summary>
        public bool Truncated { get; set; } = false;

        /// <summary>
        /// Tool output characters before truncation wrapping.
        /// </summary>
        public int OutputCharacters { get; set; } = 0;

        /// <summary>
        /// Redacted input byte count.
        /// </summary>
        public int InputBytes { get; set; } = 0;

        /// <summary>
        /// Redacted output byte count.
        /// </summary>
        public int OutputBytes { get; set; } = 0;

        /// <summary>
        /// Duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        /// <summary>
        /// Stable error type when the tool failed or was denied.
        /// </summary>
        public string ErrorType { get; set; } = null;

        /// <summary>
        /// Safe error message.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// Provider used for the chat turn.
        /// </summary>
        public string Provider { get; set; } = null;

        /// <summary>
        /// Model used for the chat turn.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Whether the trace record is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// UTC timestamp when the tool call started.
        /// </summary>
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when the tool call finished.
        /// </summary>
        public DateTime FinishedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when the row was created.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when the row was last updated.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Build a record from a data row.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>Record.</returns>
        public static AssistantToolCallRecord FromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new AssistantToolCallRecord
            {
                Id = DataTableHelper.GetStringValue(row, "id"),
                TenantId = DataTableHelper.GetStringValue(row, "tenant_id"),
                AssistantId = DataTableHelper.GetStringValue(row, "assistant_id"),
                ChatHistoryId = DataTableHelper.GetStringValue(row, "chat_history_id"),
                RequestHistoryId = DataTableHelper.GetStringValue(row, "request_history_id"),
                TraceId = DataTableHelper.GetStringValue(row, "trace_id"),
                ThreadId = DataTableHelper.GetStringValue(row, "thread_id"),
                Origin = DataTableHelper.GetStringValue(row, "origin"),
                TurnIndex = DataTableHelper.GetIntValue(row, "turn_index"),
                Iteration = DataTableHelper.GetIntValue(row, "iteration"),
                SequenceNumber = DataTableHelper.GetIntValue(row, "sequence_number"),
                ProviderToolCallId = DataTableHelper.GetStringValue(row, "provider_tool_call_id"),
                ToolName = DataTableHelper.GetStringValue(row, "tool_name"),
                ArgumentsJson = DataTableHelper.GetStringValue(row, "arguments_json"),
                OutputJson = DataTableHelper.GetStringValue(row, "output_json"),
                ResultSummaryJson = DataTableHelper.GetStringValue(row, "result_summary_json"),
                Success = DataTableHelper.GetBooleanValue(row, "success"),
                Denied = DataTableHelper.GetBooleanValue(row, "denied"),
                Truncated = DataTableHelper.GetBooleanValue(row, "truncated"),
                OutputCharacters = DataTableHelper.GetIntValue(row, "output_characters"),
                InputBytes = DataTableHelper.GetIntValue(row, "input_bytes"),
                OutputBytes = DataTableHelper.GetIntValue(row, "output_bytes"),
                DurationMs = DataTableHelper.GetDoubleValue(row, "duration_ms"),
                ErrorType = DataTableHelper.GetStringValue(row, "error_type"),
                ErrorMessage = DataTableHelper.GetStringValue(row, "error_message"),
                Provider = DataTableHelper.GetStringValue(row, "provider"),
                Model = DataTableHelper.GetStringValue(row, "model"),
                Active = DataTableHelper.GetBooleanValue(row, "active", true),
                StartedUtc = DataTableHelper.GetDateTimeValue(row, "started_utc"),
                FinishedUtc = DataTableHelper.GetDateTimeValue(row, "finished_utc"),
                CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc"),
                LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc")
            };
        }
    }
}
