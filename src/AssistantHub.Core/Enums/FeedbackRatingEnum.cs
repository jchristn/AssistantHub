namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Feedback rating enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FeedbackRatingEnum
    {
        /// <summary>
        /// Thumbs up.
        /// </summary>
        [EnumMember(Value = "ThumbsUp")]
        ThumbsUp,

        /// <summary>
        /// Thumbs down.
        /// </summary>
        [EnumMember(Value = "ThumbsDown")]
        ThumbsDown
    }
}
