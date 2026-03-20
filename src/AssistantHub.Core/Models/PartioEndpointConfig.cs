namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;
    using AssistantHub.Core.Serialization;

    /// <summary>
    /// Typed representation of a Partio embedding or completion endpoint.
    /// </summary>
    public class PartioEndpointConfig
    {
        /// <summary>
        /// Endpoint identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Optional endpoint display name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Endpoint model name.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// Upstream endpoint URL.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Upstream API format.
        /// </summary>
        public string ApiFormat { get; set; } = string.Empty;

        /// <summary>
        /// Upstream API key.
        /// </summary>
        public string ApiKey { get; set; } = null;

        /// <summary>
        /// Whether the endpoint is active.
        /// </summary>
        public bool Active { get; set; } = false;

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
        [JsonConverter(typeof(PartioHealthCheckMethodConverter))]
        public string HealthCheckMethod { get; set; } = "GET";

        /// <summary>
        /// Health check interval in milliseconds.
        /// </summary>
        public int HealthCheckIntervalMs { get; set; } = 30000;

        /// <summary>
        /// Health check timeout in milliseconds.
        /// </summary>
        public int HealthCheckTimeoutMs { get; set; } = 10000;

        /// <summary>
        /// Expected health check status code.
        /// </summary>
        public int HealthCheckExpectedStatusCode { get; set; } = 200;

        /// <summary>
        /// Consecutive successes required for healthy state.
        /// </summary>
        public int HealthyThreshold { get; set; } = 2;

        /// <summary>
        /// Consecutive failures required for unhealthy state.
        /// </summary>
        public int UnhealthyThreshold { get; set; } = 2;

        /// <summary>
        /// Whether auth is used for health checks.
        /// </summary>
        public bool HealthCheckUseAuth { get; set; } = false;
    }
}
