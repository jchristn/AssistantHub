namespace AssistantHub.Core.Models
{
    using System;

    /// <summary>
    /// Result of executing a server-side assistant tool.
    /// </summary>
    public class AssistantToolExecutionResult
    {
        /// <summary>
        /// Stable model-facing tool name.
        /// </summary>
        public string ToolName { get; set; } = null;

        /// <summary>
        /// Whether the tool call succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Whether execution was denied before calling an external service.
        /// </summary>
        public bool Denied { get; set; } = false;

        /// <summary>
        /// Whether the serialized output was truncated.
        /// </summary>
        public bool Truncated { get; set; } = false;

        /// <summary>
        /// Output character count before truncation wrapping.
        /// </summary>
        public int OutputCharacters { get; set; } = 0;

        /// <summary>
        /// Tool execution duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        /// <summary>
        /// JSON-serialized output for the model.
        /// </summary>
        public string OutputJson { get; set; } = null;

        /// <summary>
        /// Safe human-readable error message.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// Stable machine-readable error code.
        /// </summary>
        public string ErrorCode { get; set; } = null;

        /// <summary>
        /// Provider usage credits when available.
        /// </summary>
        public int? CreditsUsed { get; set; } = null;

        /// <summary>
        /// Provider-reported latency in milliseconds when available.
        /// </summary>
        public double? ProviderLatencyMs { get; set; } = null;

        /// <summary>
        /// Object bytes returned by storage-backed tools when available.
        /// </summary>
        public int? ObjectBytesReturned { get; set; } = null;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
