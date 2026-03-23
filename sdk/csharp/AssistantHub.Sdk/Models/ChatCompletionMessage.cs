namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A single message in a chat completion conversation.
    /// </summary>
    public class ChatCompletionMessage
    {
        /// <summary>
        /// The role of the message author (system, user, assistant).
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; }

        /// <summary>
        /// The content of the message.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}
