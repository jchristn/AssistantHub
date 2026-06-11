namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request to validate an assistant tool policy without persisting it.
    /// </summary>
    public class AssistantToolPolicyValidationRequest
    {
        /// <summary>
        /// JSON-serialized AssistantToolPolicy to validate.
        /// </summary>
        [JsonPropertyName("ToolPolicyJson")]
        public string ToolPolicyJson { get; set; }

        /// <summary>
        /// Parsed policy to validate.
        /// </summary>
        [JsonPropertyName("ToolPolicy")]
        public AssistantToolPolicy ToolPolicy { get; set; }
    }
}
