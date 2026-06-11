namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Result of an administrator dry-run diagnostic for assistant tool policy.
    /// </summary>
    public class AssistantToolPolicyTestResult
    {
        /// <summary>
        /// Whether the dry-run diagnostics found no blocking issues.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable diagnostic summary.
        /// </summary>
        [JsonPropertyName("Message")]
        public string Message { get; set; }

        /// <summary>
        /// Assistant identifier.
        /// </summary>
        [JsonPropertyName("AssistantId")]
        public string AssistantId { get; set; }

        /// <summary>
        /// Selected completion endpoint identifier.
        /// </summary>
        [JsonPropertyName("InferenceEndpointId")]
        public string InferenceEndpointId { get; set; }

        /// <summary>
        /// Configured tool-routing completion endpoint identifier, when it differs from the response endpoint.
        /// </summary>
        [JsonPropertyName("ToolRoutingInferenceEndpointId")]
        public string ToolRoutingInferenceEndpointId { get; set; }

        /// <summary>
        /// Effective completion endpoint identifier used for tool-routing diagnostics.
        /// </summary>
        [JsonPropertyName("EffectiveToolRoutingInferenceEndpointId")]
        public string EffectiveToolRoutingInferenceEndpointId { get; set; }

        /// <summary>
        /// Whether the effective tool-routing completion endpoint was resolved.
        /// </summary>
        [JsonPropertyName("EndpointResolved")]
        public bool EndpointResolved { get; set; }

        /// <summary>
        /// Effective tool-routing endpoint model name, if resolved.
        /// </summary>
        [JsonPropertyName("EndpointModel")]
        public string EndpointModel { get; set; }

        /// <summary>
        /// Endpoint API format, if resolved.
        /// </summary>
        [JsonPropertyName("EndpointApiFormat")]
        public string EndpointApiFormat { get; set; }

        /// <summary>
        /// Whether the endpoint is active, if resolved.
        /// </summary>
        [JsonPropertyName("EndpointActive")]
        public bool EndpointActive { get; set; }

        /// <summary>
        /// Whether the endpoint explicitly supports model tool calls.
        /// </summary>
        [JsonPropertyName("EndpointSupportsToolCalling")]
        public bool EndpointSupportsToolCalling { get; set; }

        /// <summary>
        /// Endpoint tool-call wire format, if configured.
        /// </summary>
        [JsonPropertyName("EndpointToolCallingApiFormat")]
        public string EndpointToolCallingApiFormat { get; set; }

        /// <summary>
        /// Whether the endpoint supports multiple tool calls in one model response.
        /// </summary>
        [JsonPropertyName("EndpointSupportsParallelToolCalls")]
        public bool EndpointSupportsParallelToolCalls { get; set; }

        /// <summary>
        /// Whether the endpoint supports tool calls in streaming responses.
        /// </summary>
        [JsonPropertyName("EndpointSupportsStreamingToolCalls")]
        public bool EndpointSupportsStreamingToolCalls { get; set; }

        /// <summary>
        /// Validation result for the supplied draft policy.
        /// </summary>
        [JsonPropertyName("Validation")]
        public AssistantToolPolicyValidationResult Validation { get; set; }

        /// <summary>
        /// Effective tool descriptors for the supplied draft policy.
        /// </summary>
        [JsonPropertyName("Tools")]
        public List<AssistantToolDescriptor> Tools { get; set; } = new List<AssistantToolDescriptor>();

        /// <summary>
        /// Non-blocking diagnostic warnings.
        /// </summary>
        [JsonPropertyName("Warnings")]
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Blocking diagnostic errors.
        /// </summary>
        [JsonPropertyName("Errors")]
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Stable machine-readable diagnostic error codes.
        /// </summary>
        [JsonPropertyName("ErrorCodes")]
        public List<string> ErrorCodes { get; set; } = new List<string>();
    }
}
