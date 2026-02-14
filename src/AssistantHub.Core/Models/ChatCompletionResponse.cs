namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible chat completion response.
    /// </summary>
    public class ChatCompletionResponse
    {
        /// <summary>
        /// Unique identifier for the completion.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null;

        /// <summary>
        /// Object type: "chat.completion" or "chat.completion.chunk".
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "chat.completion";

        /// <summary>
        /// Unix timestamp of when the completion was created.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; } = 0;

        /// <summary>
        /// Model used for the completion.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// List of completion choices.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice> Choices { get; set; } = new List<ChatCompletionChoice>();

        /// <summary>
        /// Token usage information (non-streaming only).
        /// </summary>
        [JsonPropertyName("usage")]
        public ChatCompletionUsage Usage { get; set; } = null;

        /// <summary>
        /// Optional status message (extension for compaction notifications).
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = null;
    }
}
