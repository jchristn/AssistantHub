namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Provider-neutral representation of a server-side tool output sent back to a model.
    /// </summary>
    public class AssistantModelToolOutput
    {
        /// <summary>
        /// Tool call identifier this output answers.
        /// </summary>
        [JsonPropertyName("tool_call_id")]
        public string ToolCallId { get; set; } = null;

        /// <summary>
        /// Tool/function name.
        /// </summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Name { get; set; } = null;

        /// <summary>
        /// JSON-serialized tool output content.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = null;
    }
}
