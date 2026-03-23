namespace AssistantHub.Sdk.Exceptions
{
    using System;

    /// <summary>
    /// Base exception for AssistantHub SDK errors.
    /// </summary>
    public class AssistantHubException : Exception
    {
        /// <summary>
        /// HTTP status code returned by the server.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="statusCode">HTTP status code.</param>
        public AssistantHubException(string message, int statusCode = 0) : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="statusCode">HTTP status code.</param>
        /// <param name="innerException">Inner exception.</param>
        public AssistantHubException(string message, int statusCode, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
