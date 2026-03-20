namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

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
