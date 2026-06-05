namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Endpoint health status.
    /// </summary>
    public class EndpointHealthStatus
    {
        /// <summary>
        /// Endpoint identifier.
        /// </summary>
        [JsonPropertyName("EndpointId")]
        public string EndpointId { get; set; }

        /// <summary>
        /// Endpoint name.
        /// </summary>
        [JsonPropertyName("EndpointName")]
        public string EndpointName { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Whether the endpoint is currently healthy.
        /// </summary>
        [JsonPropertyName("IsHealthy")]
        public bool IsHealthy { get; set; }

        /// <summary>
        /// First health check timestamp.
        /// </summary>
        [JsonPropertyName("FirstCheckUtc")]
        public DateTime? FirstCheckUtc { get; set; }

        /// <summary>
        /// Last health check timestamp.
        /// </summary>
        [JsonPropertyName("LastCheckUtc")]
        public DateTime? LastCheckUtc { get; set; }

        /// <summary>
        /// Last healthy timestamp.
        /// </summary>
        [JsonPropertyName("LastHealthyUtc")]
        public DateTime? LastHealthyUtc { get; set; }

        /// <summary>
        /// Last unhealthy timestamp.
        /// </summary>
        [JsonPropertyName("LastUnhealthyUtc")]
        public DateTime? LastUnhealthyUtc { get; set; }

        /// <summary>
        /// Last state change timestamp.
        /// </summary>
        [JsonPropertyName("LastStateChangeUtc")]
        public DateTime? LastStateChangeUtc { get; set; }

        /// <summary>
        /// Total uptime in milliseconds.
        /// </summary>
        [JsonPropertyName("TotalUptimeMs")]
        public double TotalUptimeMs { get; set; }

        /// <summary>
        /// Total downtime in milliseconds.
        /// </summary>
        [JsonPropertyName("TotalDowntimeMs")]
        public double TotalDowntimeMs { get; set; }

        /// <summary>
        /// Uptime percentage (0.0 to 100.0).
        /// </summary>
        [JsonPropertyName("UptimePercentage")]
        public double UptimePercentage { get; set; }

        /// <summary>
        /// Consecutive successful checks.
        /// </summary>
        [JsonPropertyName("ConsecutiveSuccesses")]
        public int ConsecutiveSuccesses { get; set; }

        /// <summary>
        /// Consecutive failed checks.
        /// </summary>
        [JsonPropertyName("ConsecutiveFailures")]
        public int ConsecutiveFailures { get; set; }

        /// <summary>
        /// Last error message.
        /// </summary>
        [JsonPropertyName("LastError")]
        public string LastError { get; set; }

        /// <summary>
        /// Recent health check history.
        /// </summary>
        [JsonPropertyName("History")]
        public List<HealthCheckRecord> History { get; set; }
    }
}
