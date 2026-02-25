namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Authentication context propagated through request handlers.
    /// </summary>
    public class AuthContext
    {
        /// <summary>
        /// Indicates whether the request is authenticated.
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Indicates whether the caller is a global admin (authenticated via admin API key, or a user with IsAdmin=true).
        /// </summary>
        public bool IsGlobalAdmin { get; set; }

        /// <summary>
        /// Indicates whether the caller is a tenant admin.
        /// </summary>
        public bool IsTenantAdmin { get; set; }

        /// <summary>
        /// Tenant identifier for the authenticated user. Null for API-key-based global admins.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// User identifier for the authenticated user. Null for API-key-based global admins.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Credential identifier used for authentication. Null for API-key-based global admins.
        /// </summary>
        public string CredentialId { get; set; }

        /// <summary>
        /// Email address of the authenticated user. Null for API-key-based global admins.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Tenant metadata for the authenticated user. Null for API-key-based global admins.
        /// </summary>
        public TenantMetadata Tenant { get; set; }

        /// <summary>
        /// User record for the authenticated user. Null for API-key-based global admins.
        /// </summary>
        public UserMaster User { get; set; }
    }
}
