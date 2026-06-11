namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Effective server-side tool availability for an assistant.
    /// </summary>
    public class AssistantToolDescriptor
    {
        /// <summary>
        /// Stable model-facing tool name.
        /// </summary>
        public string ToolName { get; set; } = null;

        /// <summary>
        /// Human-readable display name.
        /// </summary>
        public string DisplayName { get; set; } = null;

        /// <summary>
        /// Tool category.
        /// </summary>
        public string Category { get; set; } = null;

        /// <summary>
        /// Whether the assistant policy enables this tool.
        /// </summary>
        public bool EnabledByPolicy { get; set; } = false;

        /// <summary>
        /// Whether the tool is available after server prerequisites are checked.
        /// </summary>
        public bool Available { get; set; } = false;

        /// <summary>
        /// Non-secret reason why the tool is unavailable.
        /// </summary>
        public string UnavailableReason { get; set; } = null;
    }
}
