namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Request to validate an assistant tool policy without persisting it.
    /// </summary>
    public class AssistantToolPolicyValidationRequest
    {
        /// <summary>
        /// JSON-serialized AssistantToolPolicy to validate.
        /// </summary>
        public string ToolPolicyJson { get; set; } = null;

        /// <summary>
        /// Parsed policy to validate.
        /// </summary>
        public AssistantToolPolicy ToolPolicy { get; set; } = null;
    }
}
