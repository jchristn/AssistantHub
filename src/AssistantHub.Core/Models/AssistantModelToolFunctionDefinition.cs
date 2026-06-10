namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Function definition exposed to a tool-capable model.
    /// </summary>
    public class AssistantModelToolFunctionDefinition
    {
        /// <summary>
        /// Function/tool name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// Human-readable function/tool description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = null;

        /// <summary>
        /// JSON Schema object describing function/tool parameters.
        /// </summary>
        [JsonPropertyName("parameters")]
        public object Parameters { get; set; } = null;
    }
}
