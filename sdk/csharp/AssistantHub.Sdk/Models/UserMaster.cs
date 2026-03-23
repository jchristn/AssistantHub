namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// User record.
    /// </summary>
    public class UserMaster
    {
        /// <summary>
        /// Unique identifier with prefix usr_.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Email address.
        /// </summary>
        [JsonPropertyName("Email")]
        public string Email { get; set; }

        /// <summary>
        /// First name.
        /// </summary>
        [JsonPropertyName("FirstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// Last name.
        /// </summary>
        [JsonPropertyName("LastName")]
        public string LastName { get; set; }

        /// <summary>
        /// Whether the user is a global admin.
        /// </summary>
        [JsonPropertyName("IsAdmin")]
        public bool IsAdmin { get; set; }

        /// <summary>
        /// Whether the user is a tenant admin.
        /// </summary>
        [JsonPropertyName("IsTenantAdmin")]
        public bool IsTenantAdmin { get; set; }

        /// <summary>
        /// Whether the user is active.
        /// </summary>
        [JsonPropertyName("Active")]
        public bool Active { get; set; }

        /// <summary>
        /// Whether the user is protected from deletion.
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
