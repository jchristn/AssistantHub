namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Authentication result.
    /// </summary>
    public class AuthenticateResult
    {
        #region Public-Members

        /// <summary>
        /// Indicates whether or not the authentication was successful.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// User.
        /// </summary>
        public UserMaster User { get; set; } = null;

        /// <summary>
        /// Credential.
        /// </summary>
        public Credential Credential { get; set; } = null;

        /// <summary>
        /// Error message.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Tenant name.
        /// </summary>
        public string TenantName { get; set; } = null;

        /// <summary>
        /// Whether the user is a global admin.
        /// </summary>
        public bool IsGlobalAdmin { get; set; } = false;

        /// <summary>
        /// Whether the user is a tenant admin.
        /// </summary>
        public bool IsTenantAdmin { get; set; } = false;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthenticateResult()
        {
        }

        #endregion
    }
}
