namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Provider-neutral non-streaming inference response that may contain tool calls.
    /// </summary>
    public class ToolCapableInferenceResponse
    {
        /// <summary>
        /// Whether inference succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Final assistant content, if returned.
        /// </summary>
        public string Content { get; set; } = null;

        /// <summary>
        /// Model-requested tool calls.
        /// </summary>
        public List<AssistantModelToolCall> ToolCalls { get; set; } = new List<AssistantModelToolCall>();

        /// <summary>
        /// Provider finish reason.
        /// </summary>
        public string FinishReason { get; set; } = null;

        /// <summary>
        /// Error message, if inference failed.
        /// </summary>
        public string ErrorMessage { get; set; } = null;
    }
}
