namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Assistant feedback record.
    /// </summary>
    public class AssistantFeedback
    {
        /// <summary>
        /// Unique identifier with prefix afb_.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

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
        /// Rating.
        /// </summary>
        [JsonPropertyName("Rating")]
        public FeedbackRatingEnum Rating { get; set; }

        /// <summary>
        /// Feedback text.
        /// </summary>
        [JsonPropertyName("FeedbackText")]
        public string FeedbackText { get; set; }

        /// <summary>
        /// Message history (JSON string).
        /// </summary>
        [JsonPropertyName("MessageHistory")]
        public string MessageHistory { get; set; }

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        [JsonPropertyName("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated in UTC.
        /// </summary>
        [JsonPropertyName("LastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; }
    }
}
