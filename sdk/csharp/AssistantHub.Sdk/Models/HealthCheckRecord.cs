namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A single health check result.
    /// </summary>
    public class HealthCheckRecord
    {
        /// <summary>
        /// Timestamp of the check.
        /// </summary>
        [JsonPropertyName("TimestampUtc")]
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        /// Whether the check was successful.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }
    }
}
