namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Tenant metadata record.
    /// </summary>
    public class TenantMetadata
    {
        /// <summary>
        /// Unique identifier with prefix ten_.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Tenant name.
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Whether the tenant is active.
        /// </summary>
        [JsonPropertyName("Active")]
        public bool Active { get; set; }

        /// <summary>
        /// Whether the tenant is protected from deletion.
        /// </summary>
        [JsonPropertyName("IsProtected")]
        public bool IsProtected { get; set; }

        /// <summary>
        /// Labels.
        /// </summary>
        [JsonPropertyName("Labels")]
        public List<string> Labels { get; set; }

        /// <summary>
        /// Tags.
        /// </summary>
        [JsonPropertyName("Tags")]
        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        [JsonPropertyName("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated in UTC.
        /// </summary>
        [JsonPropertyName("LastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; }
    }
}
