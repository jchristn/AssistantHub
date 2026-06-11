namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible chat completion message.
    /// </summary>
    public class ChatCompletionMessage
    {
        /// <summary>
        /// Message role: "system", "user", or "assistant".
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = null;

        /// <summary>
        /// Message content.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = null;

        /// <summary>
        /// Optional provider thinking/reasoning text exposed only when the assistant permits it.
        /// </summary>
        [JsonPropertyName("thinking")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Thinking { get; set; } = null;

        /// <summary>
        /// Model-requested tool calls on an assistant message.
        /// </summary>
        [JsonPropertyName("tool_calls")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<AssistantModelToolCall> ToolCalls { get; set; } = null;

        /// <summary>
        /// Tool call identifier answered by a tool-role message.
        /// </summary>
        [JsonPropertyName("tool_call_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ToolCallId { get; set; } = null;

        /// <summary>
        /// Optional tool/function name for tool-role messages.
        /// </summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Name { get; set; } = null;
    }
}
