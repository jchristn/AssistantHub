namespace AssistantHub.Sdk.Models
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
        public string ToolCallId { get; set; }

        /// <summary>
        /// Stable model-facing tool name.
        /// </summary>
        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; }

        /// <summary>
        /// Short public display label for the tool.
        /// </summary>
        [JsonPropertyName("display_label")]
        public string DisplayLabel { get; set; }

        /// <summary>
        /// One-based model/tool loop iteration.
        /// </summary>
        [JsonPropertyName("iteration")]
        public int Iteration { get; set; }

        /// <summary>
        /// One-based sequence number within the chat turn.
        /// </summary>
        [JsonPropertyName("sequence_number")]
        public int SequenceNumber { get; set; }

        /// <summary>
        /// Whether the tool succeeded.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Whether the tool was denied by policy.
        /// </summary>
        [JsonPropertyName("denied")]
        public bool Denied { get; set; }

        /// <summary>
        /// Whether the output was truncated.
        /// </summary>
        [JsonPropertyName("truncated")]
        public bool Truncated { get; set; }

        /// <summary>
        /// Output character count before final wrapping.
        /// </summary>
        [JsonPropertyName("output_characters")]
        public int OutputCharacters { get; set; }

        /// <summary>
        /// Safe count of result items returned by the tool, when known.
        /// </summary>
        [JsonPropertyName("result_count")]
        public int? ResultCount { get; set; }

        /// <summary>
        /// Provider usage credits when available.
        /// </summary>
        [JsonPropertyName("credits_used")]
        public int? CreditsUsed { get; set; }

        /// <summary>
        /// Provider-reported latency in milliseconds when available.
        /// </summary>
        [JsonPropertyName("provider_latency_ms")]
        public double? ProviderLatencyMs { get; set; }

        /// <summary>
        /// Duration in milliseconds.
        /// </summary>
        [JsonPropertyName("duration_ms")]
        public double DurationMs { get; set; }

        /// <summary>
        /// Short safe status summary.
        /// </summary>
        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        /// <summary>
        /// UTC timestamp when the tool call started.
        /// </summary>
        [JsonPropertyName("started_utc")]
        public DateTime? StartedUtc { get; set; }

        /// <summary>
        /// UTC timestamp when the tool call finished.
        /// </summary>
        [JsonPropertyName("finished_utc")]
        public DateTime? FinishedUtc { get; set; }
    }
}
