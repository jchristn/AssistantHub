namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Authentication request.
    /// </summary>
    public class AuthenticateRequest
    {
        #region Public-Members

        /// <summary>
        /// Email address.
        /// </summary>
        public string Email { get; set; } = null;

        /// <summary>
        /// Password.
        /// </summary>
        public string Password { get; set; } = null;

        /// <summary>
        /// Bearer token.
        /// </summary>
        public string BearerToken { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthenticateRequest()
        {
        }

        #endregion
    }
}
