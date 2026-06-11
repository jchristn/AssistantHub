namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Typed request payload for creating or updating a Partio-managed endpoint.
    /// </summary>
    public class PartioEndpointRequest
    {
        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Optional endpoint name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Model name.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Endpoint URL.
        /// </summary>
        public string Endpoint { get; set; } = null;

        /// <summary>
        /// API format.
        /// </summary>
        public string ApiFormat { get; set; } = null;

        /// <summary>
        /// API key.
        /// </summary>
        public string ApiKey { get; set; } = null;

        /// <summary>
        /// Whether the endpoint is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Maximum concurrent requests allowed for this endpoint.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxConcurrentRequests { get; set; } = null;

        /// <summary>
        /// Whether this endpoint explicitly supports model tool calls.
        /// </summary>
        public bool SupportsToolCalling { get; set; } = false;

        /// <summary>
        /// Tool-calling wire format, for example OpenAIChatCompletions.
        /// </summary>
        public string ToolCallingApiFormat { get; set; } = null;

        /// <summary>
        /// Whether this endpoint supports multiple tool calls in one assistant turn.
        /// </summary>
        public bool SupportsParallelToolCalls { get; set; } = false;

        /// <summary>
        /// Whether this endpoint supports tool calls while streaming responses.
        /// </summary>
        public bool SupportsStreamingToolCalls { get; set; } = false;

        /// <summary>
        /// Whether request history is enabled.
        /// </summary>
        public bool EnableRequestHistory { get; set; } = false;

        /// <summary>
        /// Labels.
        /// </summary>
        public List<string> Labels { get; set; } = null;

        /// <summary>
        /// Tags.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = null;

        /// <summary>
        /// Whether health checks are enabled.
        /// </summary>
        public bool HealthCheckEnabled { get; set; } = false;

        /// <summary>
        /// Health check URL.
        /// </summary>
        public string HealthCheckUrl { get; set; } = null;

        /// <summary>
        /// Health check method.
        /// </summary>
        public string HealthCheckMethod { get; set; } = null;

        /// <summary>
        /// Health check interval in milliseconds.
        /// </summary>
        public int HealthCheckIntervalMs { get; set; } = 0;

        /// <summary>
        /// Health check timeout in milliseconds.
        /// </summary>
        public int HealthCheckTimeoutMs { get; set; } = 0;

        /// <summary>
        /// Expected health check status code.
        /// </summary>
        public int HealthCheckExpectedStatusCode { get; set; } = 0;

        /// <summary>
        /// Healthy threshold.
        /// </summary>
        public int HealthyThreshold { get; set; } = 0;

        /// <summary>
        /// Unhealthy threshold.
        /// </summary>
        public int UnhealthyThreshold { get; set; } = 0;

        /// <summary>
        /// Whether auth is used on health checks.
        /// </summary>
        public bool HealthCheckUseAuth { get; set; } = false;
    }
}
