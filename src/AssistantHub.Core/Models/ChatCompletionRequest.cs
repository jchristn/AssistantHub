namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible chat completion request.
    /// </summary>
    public class ChatCompletionRequest
    {
        /// <summary>
        /// Model name (optional, assistant settings used as default).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// List of messages in the conversation.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<ChatCompletionMessage> Messages { get; set; } = new List<ChatCompletionMessage>();

        /// <summary>
        /// Whether to stream the response (ignored; assistant setting controls this).
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Sampling temperature (optional override).
        /// </summary>
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; } = null;

        /// <summary>
        /// Top-p nucleus sampling (optional override).
        /// </summary>
        [JsonPropertyName("top_p")]
        public double? TopP { get; set; } = null;

        /// <summary>
        /// Maximum tokens to generate (optional override).
        /// </summary>
        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; } = null;
    }
}
