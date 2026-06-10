namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Request to execute a server-side assistant tool.
    /// </summary>
    public class AssistantToolExecutionRequest
    {
        /// <summary>
        /// Stable model-facing tool name.
        /// </summary>
        public string ToolName { get; set; } = null;

        /// <summary>
        /// JSON-serialized tool arguments.
        /// </summary>
        public string ArgumentsJson { get; set; } = null;
    }
}
