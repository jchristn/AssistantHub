namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible chat completion message.
    /// </summary>
    public class ChatCompletionMessage
    {
        /// <summary>
        /// Message role: "system", "user", or "assistant".
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = null;

        /// <summary>
        /// Message content.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = null;
    }
}
