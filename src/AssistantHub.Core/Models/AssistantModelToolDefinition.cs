namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Provider-neutral tool definition using the OpenAI-compatible function shape.
    /// </summary>
    public class AssistantModelToolDefinition
    {
        /// <summary>
        /// Tool type. OpenAI-compatible providers use "function".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        /// <summary>
        /// Function definition.
        /// </summary>
        [JsonPropertyName("function")]
        public AssistantModelToolFunctionDefinition Function { get; set; } = null;
    }
}
