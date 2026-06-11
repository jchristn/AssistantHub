namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Embedding endpoint configuration (Partio endpoint with embedding type).
    /// </summary>
    public class EmbeddingEndpoint
    {
        /// <summary>
        /// Endpoint identifier.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Endpoint display name.
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Model name.
        /// </summary>
        [JsonPropertyName("Model")]
        public string Model { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Upstream endpoint URL.
        /// </summary>
        [JsonPropertyName("Endpoint")]
        public string Endpoint { get; set; }

        /// <summary>
        /// Upstream API format.
        /// </summary>
        [JsonPropertyName("ApiFormat")]
        public string ApiFormat { get; set; }

        /// <summary>
        /// Upstream API key.
        /// </summary>
        [JsonPropertyName("ApiKey")]
        public string ApiKey { get; set; }

        /// <summary>
        /// Whether the endpoint is active.
        /// </summary>
        [JsonPropertyName("Active")]
        public bool Active { get; set; }

        /// <summary>
        /// Maximum concurrent requests allowed for this endpoint.
        /// </summary>
        [JsonPropertyName("MaxConcurrentRequests")]
        public int MaxConcurrentRequests { get; set; }

        /// <summary>
        /// Whether this endpoint explicitly supports model tool calls.
        /// </summary>
        [JsonPropertyName("SupportsToolCalling")]
        public bool SupportsToolCalling { get; set; }

        /// <summary>
        /// Tool-calling wire format, for example OpenAIChatCompletions.
        /// </summary>
        [JsonPropertyName("ToolCallingApiFormat")]
        public string ToolCallingApiFormat { get; set; }

        /// <summary>
        /// Whether this endpoint supports multiple tool calls in one assistant turn.
        /// </summary>
        [JsonPropertyName("SupportsParallelToolCalls")]
        public bool SupportsParallelToolCalls { get; set; }

        /// <summary>
        /// Whether this endpoint supports tool calls while streaming responses.
        /// </summary>
        [JsonPropertyName("SupportsStreamingToolCalls")]
        public bool SupportsStreamingToolCalls { get; set; }

        /// <summary>
        /// Whether health checks are enabled.
        /// </summary>
        [JsonPropertyName("HealthCheckEnabled")]
        public bool HealthCheckEnabled { get; set; }

        /// <summary>
        /// Health check URL.
        /// </summary>
        [JsonPropertyName("HealthCheckUrl")]
        public string HealthCheckUrl { get; set; }

        /// <summary>
        /// Health check method.
        /// </summary>
        [JsonPropertyName("HealthCheckMethod")]
        public string HealthCheckMethod { get; set; }

        /// <summary>
        /// Health check interval in milliseconds.
        /// </summary>
        [JsonPropertyName("HealthCheckIntervalMs")]
        public int HealthCheckIntervalMs { get; set; }

        /// <summary>
        /// Health check timeout in milliseconds.
        /// </summary>
        [JsonPropertyName("HealthCheckTimeoutMs")]
        public int HealthCheckTimeoutMs { get; set; }

        /// <summary>
        /// Expected health check status code.
        /// </summary>
        [JsonPropertyName("HealthCheckExpectedStatusCode")]
        public int HealthCheckExpectedStatusCode { get; set; }

        /// <summary>
        /// Consecutive successes required for healthy state.
        /// </summary>
        [JsonPropertyName("HealthyThreshold")]
        public int HealthyThreshold { get; set; }

        /// <summary>
        /// Consecutive failures required for unhealthy state.
        /// </summary>
        [JsonPropertyName("UnhealthyThreshold")]
        public int UnhealthyThreshold { get; set; }

        /// <summary>
        /// Whether auth is used for health checks.
        /// </summary>
        [JsonPropertyName("HealthCheckUseAuth")]
        public bool HealthCheckUseAuth { get; set; }
    }
}
