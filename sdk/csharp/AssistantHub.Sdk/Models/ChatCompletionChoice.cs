namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A single choice in a chat completion response.
    /// </summary>
    public class ChatCompletionChoice
    {
        /// <summary>
        /// Choice index.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// The message for non-streaming responses.
        /// </summary>
        [JsonPropertyName("message")]
        public ChatCompletionMessage Message { get; set; }

        /// <summary>
        /// The delta for streaming responses.
        /// </summary>
        [JsonPropertyName("delta")]
        public ChatCompletionMessage Delta { get; set; }

        /// <summary>
        /// Reason the model stopped generating.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }
}
