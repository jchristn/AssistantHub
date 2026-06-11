namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Provider-neutral representation of a model-requested tool call.
    /// </summary>
    public class AssistantModelToolCall
    {
        /// <summary>
        /// Provider-supplied tool call identifier.
        /// </summary>
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Id { get; set; } = null;

        /// <summary>
        /// Tool call type. OpenAI-compatible providers use "function".
        /// </summary>
        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Type { get; set; } = "function";

        /// <summary>
        /// Function payload.
        /// </summary>
        [JsonPropertyName("function")]
        public AssistantModelToolFunctionCall Function { get; set; } = null;
    }
}
