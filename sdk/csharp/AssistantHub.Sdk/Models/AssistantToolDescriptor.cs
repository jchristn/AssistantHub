namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Effective server-side tool availability for an assistant.
    /// </summary>
    public class AssistantToolDescriptor
    {
        /// <summary>
        /// Stable model-facing tool name.
        /// </summary>
        [JsonPropertyName("ToolName")]
        public string ToolName { get; set; }

        /// <summary>
        /// Human-readable display name.
        /// </summary>
        [JsonPropertyName("DisplayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Tool category.
        /// </summary>
        [JsonPropertyName("Category")]
        public string Category { get; set; }

        /// <summary>
        /// Whether the assistant policy enables this tool.
        /// </summary>
        [JsonPropertyName("EnabledByPolicy")]
        public bool EnabledByPolicy { get; set; }

        /// <summary>
        /// Whether the tool is available after server prerequisites are checked.
        /// </summary>
        [JsonPropertyName("Available")]
        public bool Available { get; set; }

        /// <summary>
        /// Non-secret reason why the tool is unavailable.
        /// </summary>
        [JsonPropertyName("UnavailableReason")]
        public string UnavailableReason { get; set; }
    }
}
