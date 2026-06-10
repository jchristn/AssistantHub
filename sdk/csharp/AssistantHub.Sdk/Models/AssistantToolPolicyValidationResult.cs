namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Result of validating an assistant tool policy.
    /// </summary>
    public class AssistantToolPolicyValidationResult
    {
        /// <summary>
        /// Whether validation succeeded.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable validation message.
        /// </summary>
        [JsonPropertyName("Message")]
        public string Message { get; set; }

        /// <summary>
        /// Normalized JSON-serialized AssistantToolPolicy.
        /// </summary>
        [JsonPropertyName("ToolPolicyJson")]
        public string ToolPolicyJson { get; set; }

        /// <summary>
        /// Normalized parsed AssistantToolPolicy.
        /// </summary>
        [JsonPropertyName("ToolPolicy")]
        public AssistantToolPolicy ToolPolicy { get; set; }

        /// <summary>
        /// Effective tools if the policy were applied.
        /// </summary>
        [JsonPropertyName("Tools")]
        public List<AssistantToolDescriptor> Tools { get; set; } = new List<AssistantToolDescriptor>();

        /// <summary>
        /// Validation errors.
        /// </summary>
        [JsonPropertyName("Errors")]
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Stable machine-readable validation error codes.
        /// </summary>
        [JsonPropertyName("ErrorCodes")]
        public List<string> ErrorCodes { get; set; } = new List<string>();
    }
}
