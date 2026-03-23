namespace AssistantHub.Sdk.Exceptions
{
    using System;

    /// <summary>
    /// Exception thrown when authentication fails (HTTP 401).
    /// </summary>
    public class AuthenticationException : AssistantHubException
    {
        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="message">Error message.</param>
        public AuthenticationException(string message) : base(message, 401)
        {
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        public AuthenticationException(string message, Exception innerException) : base(message, 401, innerException)
        {
        }
    }
}
