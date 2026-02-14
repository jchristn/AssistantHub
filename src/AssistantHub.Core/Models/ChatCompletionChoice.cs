namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible chat completion choice.
    /// </summary>
    public class ChatCompletionChoice
    {
        /// <summary>
        /// Choice index.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        /// <summary>
        /// Full message (non-streaming responses).
        /// </summary>
        [JsonPropertyName("message")]
        public ChatCompletionMessage Message { get; set; } = null;

        /// <summary>
        /// Delta message (streaming responses).
        /// </summary>
        [JsonPropertyName("delta")]
        public ChatCompletionMessage Delta { get; set; } = null;

        /// <summary>
        /// Finish reason: "stop", "length", etc.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; } = null;
    }
}
