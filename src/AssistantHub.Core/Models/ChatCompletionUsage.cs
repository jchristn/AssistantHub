namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible token usage information.
    /// </summary>
    public class ChatCompletionUsage
    {
        /// <summary>
        /// Number of tokens in the prompt.
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; } = 0;

        /// <summary>
        /// Number of tokens in the completion.
        /// </summary>
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; } = 0;

        /// <summary>
        /// Total number of tokens used.
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; } = 0;
    }
}
