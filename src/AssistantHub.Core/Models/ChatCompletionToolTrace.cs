namespace AssistantHub.Core.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Safe chat-completion extension metadata for a model-directed assistant tool call.
    /// </summary>
    public class ChatCompletionToolTrace
    {
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
        /// Whether the tool succeeded.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = false;

        /// <summary>
        /// Whether the tool was denied by policy.
        /// </summary>
        [JsonPropertyName("denied")]
        public bool Denied { get; set; } = false;

        /// <summary>
        /// Whether output was truncated.
        /// </summary>
        [JsonPropertyName("truncated")]
        public bool Truncated { get; set; } = false;

        /// <summary>
        /// Output character count before final wrapping.
        /// </summary>
        [JsonPropertyName("output_characters")]
        public int OutputCharacters { get; set; } = 0;

        /// <summary>
        /// Safe count of result items returned by the tool, when known.
        /// </summary>
        [JsonPropertyName("result_count")]
        public int? ResultCount { get; set; } = null;

        /// <summary>
        /// Provider usage credits when available.
        /// </summary>
        [JsonPropertyName("credits_used")]
        public int? CreditsUsed { get; set; } = null;

        /// <summary>
        /// Provider-reported latency in milliseconds when available.
        /// </summary>
        [JsonPropertyName("provider_latency_ms")]
        public double? ProviderLatencyMs { get; set; } = null;

        /// <summary>
        /// Duration in milliseconds.
        /// </summary>
        [JsonPropertyName("duration_ms")]
        public double DurationMs { get; set; } = 0;

        /// <summary>
        /// Short safe status summary.
        /// </summary>
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = null;

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
    }
}
