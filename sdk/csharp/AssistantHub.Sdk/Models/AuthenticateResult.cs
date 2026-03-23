namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Authentication result.
    /// </summary>
    public class AuthenticateResult
    {
        /// <summary>
        /// Whether authentication was successful.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Authenticated user.
        /// </summary>
        [JsonPropertyName("User")]
        public UserMaster User { get; set; }

        /// <summary>
        /// Credential used.
        /// </summary>
        [JsonPropertyName("Credential")]
        public Credential Credential { get; set; }

        /// <summary>
        /// Error message if authentication failed.
        /// </summary>
        [JsonPropertyName("ErrorMessage")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Tenant name.
        /// </summary>
        [JsonPropertyName("TenantName")]
        public string TenantName { get; set; }

        /// <summary>
        /// Whether the user is a global admin.
        /// </summary>
        [JsonPropertyName("IsGlobalAdmin")]
        public bool IsGlobalAdmin { get; set; }

        /// <summary>
        /// Whether the user is a tenant admin.
        /// </summary>
        [JsonPropertyName("IsTenantAdmin")]
        public bool IsTenantAdmin { get; set; }
    }
}
