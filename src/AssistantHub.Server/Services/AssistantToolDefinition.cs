namespace AssistantHub.Server.Services
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible model-facing assistant tool definition.
    /// </summary>
    public class AssistantToolDefinition
    {
        /// <summary>
        /// Tool definition type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        /// <summary>
        /// Function tool definition.
        /// </summary>
        [JsonPropertyName("function")]
        public AssistantToolFunctionDefinition Function { get; set; } = null;
    }
}
