namespace AssistantHub.Sdk.Exceptions
{
    using System;

    /// <summary>
    /// Exception thrown when a requested resource is not found (HTTP 404).
    /// </summary>
    public class NotFoundException : AssistantHubException
    {
        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="message">Error message.</param>
        public NotFoundException(string message) : base(message, 404)
        {
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        public NotFoundException(string message, Exception innerException) : base(message, 404, innerException)
        {
        }
    }
}
