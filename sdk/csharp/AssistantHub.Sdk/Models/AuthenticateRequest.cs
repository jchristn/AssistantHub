namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Authentication request.
    /// </summary>
    public class AuthenticateRequest
    {
        /// <summary>
        /// Email address.
        /// </summary>
        [JsonPropertyName("Email")]
        public string Email { get; set; }

        /// <summary>
        /// Password.
        /// </summary>
        [JsonPropertyName("Password")]
        public string Password { get; set; }

        /// <summary>
        /// Bearer token (alternative to email/password).
        /// </summary>
        [JsonPropertyName("BearerToken")]
        public string BearerToken { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }
    }
}
