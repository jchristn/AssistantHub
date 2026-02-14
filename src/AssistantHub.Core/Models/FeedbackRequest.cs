namespace AssistantHub.Core.Models
{
    using System;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Feedback request.
    /// </summary>
    public class FeedbackRequest
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
        /// User message.
        /// </summary>
        public string UserMessage { get; set; } = null;

        /// <summary>
        /// Assistant response.
        /// </summary>
        public string AssistantResponse { get; set; } = null;

        /// <summary>
        /// Feedback rating.
        /// </summary>
        public FeedbackRatingEnum Rating { get; set; } = FeedbackRatingEnum.ThumbsUp;

        /// <summary>
        /// Feedback text.
        /// </summary>
        public string FeedbackText { get; set; } = null;

        /// <summary>
        /// Full message history as JSON string.
        /// </summary>
        public string MessageHistory { get; set; } = null;

        #endregion

        #region Private-Members

        private string _AssistantId = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public FeedbackRequest()
        {
        }

        #endregion
    }
}
