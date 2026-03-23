namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Credential record.
    /// </summary>
    public class Credential
    {
        /// <summary>
        /// Unique identifier with prefix cred_.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// User identifier.
        /// </summary>
        [JsonPropertyName("UserId")]
        public string UserId { get; set; }

        /// <summary>
        /// Credential name.
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Bearer token.
        /// </summary>
        [JsonPropertyName("BearerToken")]
        public string BearerToken { get; set; }

        /// <summary>
        /// Whether the credential is active.
        /// </summary>
        [JsonPropertyName("Active")]
        public bool Active { get; set; }

        /// <summary>
        /// Whether the credential is protected from deletion.
        /// </summary>
        [JsonPropertyName("IsProtected")]
        public bool IsProtected { get; set; }

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
