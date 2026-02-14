namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Chat result.
    /// </summary>
    public class ChatResult
    {
        #region Public-Members

        /// <summary>
        /// Indicates whether or not the chat request was successful.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Assistant identifier.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// User message.
        /// </summary>
        public string UserMessage { get; set; } = null;

        /// <summary>
        /// Assistant response.
        /// </summary>
        public string AssistantResponse { get; set; } = null;

        /// <summary>
        /// Sources referenced in the response.
        /// </summary>
        public List<string> Sources { get; set; } = new List<string>();

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
        public ChatResult()
        {
        }

        #endregion
    }
}
