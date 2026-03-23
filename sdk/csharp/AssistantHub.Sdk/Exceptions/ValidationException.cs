namespace AssistantHub.Sdk.Exceptions
{
    using System;

    /// <summary>
    /// Exception thrown when the server rejects a request due to validation errors (HTTP 400).
    /// </summary>
    public class ValidationException : AssistantHubException
    {
        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="message">Error message.</param>
        public ValidationException(string message) : base(message, 400)
        {
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        public ValidationException(string message, Exception innerException) : base(message, 400, innerException)
        {
        }
    }
}
