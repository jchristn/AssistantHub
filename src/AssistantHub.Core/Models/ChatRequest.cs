namespace AssistantHub.Core.Models
{
    using System;

    /// <summary>
    /// Chat request.
    /// </summary>
    public class ChatRequest
    {
        #region Public-Members

        /// <summary>
        /// Assistant identifier.
        /// </summary>
        public string AssistantId
        {
            get => _AssistantId;
            set => _AssistantId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(AssistantId));
        }

        /// <summary>
        /// Message.
        /// </summary>
        public string Message
        {
            get => _Message;
            set => _Message = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Message));
        }

        #endregion

        #region Private-Members

        private string _AssistantId = null;
        private string _Message = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatRequest()
        {
        }

        #endregion
    }
}
