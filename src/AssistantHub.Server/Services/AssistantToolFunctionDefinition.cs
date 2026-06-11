namespace AssistantHub.Server.Services
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Model-facing function definition for one assistant tool.
    /// </summary>
    public class AssistantToolFunctionDefinition
    {
        /// <summary>
        /// Stable model-facing function name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// Tool description shown to the model.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = null;

        /// <summary>
        /// JSON Schema parameters.
        /// </summary>
        [JsonPropertyName("parameters")]
        public AssistantToolJsonSchema Parameters { get; set; } = null;
    }
}
