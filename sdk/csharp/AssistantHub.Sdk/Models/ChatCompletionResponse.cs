namespace AssistantHub.Sdk.Models
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
        public string Id { get; set; }

        /// <summary>
        /// Object type: "chat.completion" or "chat.completion.chunk".
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; }

        /// <summary>
        /// Unix timestamp of when the completion was created.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; }

        /// <summary>
        /// Model used for the completion.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// List of completion choices.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice> Choices { get; set; }

        /// <summary>
        /// Token usage information (non-streaming only).
        /// </summary>
        [JsonPropertyName("usage")]
        public ChatCompletionUsage Usage { get; set; }

        /// <summary>
        /// Optional status message.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Retrieval telemetry (extension for RAG diagnostics).
        /// </summary>
        [JsonPropertyName("retrieval")]
        public ChatCompletionRetrieval Retrieval { get; set; }

        /// <summary>
        /// Citation metadata linking response claims to source documents.
        /// </summary>
        [JsonPropertyName("citations")]
        public ChatCompletionCitations Citations { get; set; }
    }
}
