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
