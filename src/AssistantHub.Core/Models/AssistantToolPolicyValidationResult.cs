namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Result of validating an assistant tool policy.
    /// </summary>
    public class AssistantToolPolicyValidationResult
    {
        /// <summary>
        /// Whether validation succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Human-readable validation message.
        /// </summary>
        public string Message { get; set; } = null;

        /// <summary>
        /// Normalized JSON-serialized AssistantToolPolicy.
        /// </summary>
        public string ToolPolicyJson { get; set; } = null;

        /// <summary>
        /// Normalized parsed AssistantToolPolicy.
        /// </summary>
        public AssistantToolPolicy ToolPolicy { get; set; } = null;

        /// <summary>
        /// Effective tools if the policy were applied.
        /// </summary>
        public List<AssistantToolDescriptor> Tools { get; set; } = new List<AssistantToolDescriptor>();

        /// <summary>
        /// Validation errors.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Stable machine-readable validation error codes.
        /// </summary>
        public List<string> ErrorCodes { get; set; } = new List<string>();
    }
}
