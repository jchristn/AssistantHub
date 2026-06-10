namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Result of an administrator dry-run diagnostic for assistant tool policy.
    /// </summary>
    public class AssistantToolPolicyTestResult
    {
        /// <summary>
        /// Whether the dry-run diagnostics found no blocking issues.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Human-readable diagnostic summary.
        /// </summary>
        public string Message { get; set; } = null;

        /// <summary>
        /// Assistant identifier.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Selected completion endpoint identifier.
        /// </summary>
        public string InferenceEndpointId { get; set; } = null;

        /// <summary>
        /// Whether the selected completion endpoint was resolved.
        /// </summary>
        public bool EndpointResolved { get; set; } = false;

        /// <summary>
        /// Endpoint model name, if resolved.
        /// </summary>
        public string EndpointModel { get; set; } = null;

        /// <summary>
        /// Endpoint API format, if resolved.
        /// </summary>
        public string EndpointApiFormat { get; set; } = null;

        /// <summary>
        /// Whether the endpoint is active, if resolved.
        /// </summary>
        public bool EndpointActive { get; set; } = false;

        /// <summary>
        /// Whether the endpoint explicitly supports model tool calls.
        /// </summary>
        public bool EndpointSupportsToolCalling { get; set; } = false;

        /// <summary>
        /// Endpoint tool-call wire format, if configured.
        /// </summary>
        public string EndpointToolCallingApiFormat { get; set; } = null;

        /// <summary>
        /// Whether the endpoint supports multiple tool calls in one model response.
        /// </summary>
        public bool EndpointSupportsParallelToolCalls { get; set; } = false;

        /// <summary>
        /// Whether the endpoint supports tool calls in streaming responses.
        /// </summary>
        public bool EndpointSupportsStreamingToolCalls { get; set; } = false;

        /// <summary>
        /// Validation result for the supplied draft policy.
        /// </summary>
        public AssistantToolPolicyValidationResult Validation { get; set; } = null;

        /// <summary>
        /// Effective tool descriptors for the supplied draft policy.
        /// </summary>
        public List<AssistantToolDescriptor> Tools { get; set; } = new List<AssistantToolDescriptor>();

        /// <summary>
        /// Non-blocking diagnostic warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Blocking diagnostic errors.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Stable machine-readable diagnostic error codes.
        /// </summary>
        public List<string> ErrorCodes { get; set; } = new List<string>();
    }
}
