namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Feedback submission request.
    /// </summary>
    public class FeedbackRequest
    {
        /// <summary>
        /// Assistant identifier.
        /// </summary>
        [JsonPropertyName("AssistantId")]
        public string AssistantId { get; set; }

        /// <summary>
        /// The user's message.
        /// </summary>
        [JsonPropertyName("UserMessage")]
        public string UserMessage { get; set; }

        /// <summary>
        /// The assistant's response.
        /// </summary>
        [JsonPropertyName("AssistantResponse")]
        public string AssistantResponse { get; set; }

        /// <summary>
        /// Rating (ThumbsUp or ThumbsDown).
        /// </summary>
        [JsonPropertyName("Rating")]
        public FeedbackRatingEnum Rating { get; set; }

        /// <summary>
        /// Optional feedback text.
        /// </summary>
        [JsonPropertyName("FeedbackText")]
        public string FeedbackText { get; set; }

        /// <summary>
        /// Message history (JSON string).
        /// </summary>
        [JsonPropertyName("MessageHistory")]
        public string MessageHistory { get; set; }
    }
}
