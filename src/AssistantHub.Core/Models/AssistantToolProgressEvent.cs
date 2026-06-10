namespace AssistantHub.Core.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Safe user-visible lifecycle event for a model-directed assistant tool call.
    /// </summary>
    public class AssistantToolProgressEvent
    {
        /// <summary>
        /// Stable event type, such as assistant.tool_call.started.
        /// </summary>
        [JsonPropertyName("event_type")]
        public string EventType { get; set; } = null;

        /// <summary>
        /// Provider or server-assigned tool-call identifier.
        /// </summary>
        [JsonPropertyName("tool_call_id")]
        public string ToolCallId { get; set; } = null;

        /// <summary>
        /// Stable model-facing tool name.
        /// </summary>
        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; } = null;

        /// <summary>
        /// Short public display label for the tool.
        /// </summary>
        [JsonPropertyName("display_label")]
        public string DisplayLabel { get; set; } = null;

        /// <summary>
        /// Stable status code for clients that localize labels.
        /// </summary>
        [JsonPropertyName("status_code")]
        public string StatusCode { get; set; } = null;

        /// <summary>
        /// One-based model/tool loop iteration.
        /// </summary>
        [JsonPropertyName("iteration")]
        public int Iteration { get; set; } = 0;

        /// <summary>
        /// One-based tool call sequence within the chat turn.
        /// </summary>
        [JsonPropertyName("sequence_number")]
        public int SequenceNumber { get; set; } = 0;

        /// <summary>
        /// UTC timestamp when the tool call started.
        /// </summary>
        [JsonPropertyName("started_utc")]
        public DateTime? StartedUtc { get; set; } = null;

        /// <summary>
        /// UTC timestamp when the tool call finished.
        /// </summary>
        [JsonPropertyName("finished_utc")]
        public DateTime? FinishedUtc { get; set; } = null;

        /// <summary>
        /// Duration in milliseconds when known.
        /// </summary>
        [JsonPropertyName("duration_ms")]
        public double? DurationMs { get; set; } = null;

        /// <summary>
        /// Result count when the tool reports one.
        /// </summary>
        [JsonPropertyName("result_count")]
        public int? ResultCount { get; set; } = null;

        /// <summary>
        /// Whether the output was truncated.
        /// </summary>
        [JsonPropertyName("truncated")]
        public bool? Truncated { get; set; } = null;

        /// <summary>
        /// Whether the tool was denied by policy.
        /// </summary>
        [JsonPropertyName("denied")]
        public bool? Denied { get; set; } = null;

        /// <summary>
        /// Whether the tool succeeded.
        /// </summary>
        [JsonPropertyName("success")]
        public bool? Success { get; set; } = null;

        /// <summary>
        /// Short safe status summary.
        /// </summary>
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = null;
    }
}
