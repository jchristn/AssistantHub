namespace AssistantHub.Server.Services
{
    using AssistantHub.Core.Models;

    /// <summary>
    /// Result of a retrieval-only execution of the chat rail.
    /// </summary>
    public class AssistantRetrievalExecutionResult
    {
        /// <summary>
        /// Whether the execution succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// HTTP status code to return on failure.
        /// </summary>
        public int StatusCode { get; set; } = 400;

        /// <summary>
        /// Failure description.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// Retrieval response on success.
        /// </summary>
        public AssistantRetrieveResponse Response { get; set; } = null;
    }
}
