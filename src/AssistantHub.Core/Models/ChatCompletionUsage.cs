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

        /// <summary>
        /// Total context window size in tokens.
        /// </summary>
        [JsonPropertyName("context_window")]
        public int ContextWindow { get; set; } = 0;

        /// <summary>
        /// Reasoning tokens reported directly by the provider, when available.
        /// </summary>
        [JsonPropertyName("reasoning_tokens")]
        public int ReasoningTokens { get; set; } = 0;

        /// <summary>
        /// Tokens attributed to tool definitions, when reported by a compatible provider.
        /// </summary>
        [JsonPropertyName("tool_definition_tokens")]
        public int ToolDefinitionTokens { get; set; } = 0;

        /// <summary>
        /// Alternate provider field for tokens attributed to tool definitions.
        /// </summary>
        [JsonPropertyName("tool_tokens")]
        public int ToolTokens { get; set; } = 0;

        /// <summary>
        /// Provider-specific prompt token details.
        /// </summary>
        [JsonPropertyName("prompt_tokens_details")]
        public ChatCompletionPromptTokensDetails PromptTokensDetails { get; set; } = null;

        /// <summary>
        /// Provider-specific completion token details.
        /// </summary>
        [JsonPropertyName("completion_tokens_details")]
        public ChatCompletionCompletionTokensDetails CompletionTokensDetails { get; set; } = null;
    }

    /// <summary>
    /// Provider-specific prompt token details.
    /// </summary>
    public class ChatCompletionPromptTokensDetails
    {
        /// <summary>
        /// Cached prompt tokens.
        /// </summary>
        [JsonPropertyName("cached_tokens")]
        public int CachedTokens { get; set; } = 0;

        /// <summary>
        /// Audio prompt tokens.
        /// </summary>
        [JsonPropertyName("audio_tokens")]
        public int AudioTokens { get; set; } = 0;

        /// <summary>
        /// Tokens attributed to tool definitions, when reported in prompt details.
        /// </summary>
        [JsonPropertyName("tool_definition_tokens")]
        public int ToolDefinitionTokens { get; set; } = 0;

        /// <summary>
        /// Alternate provider field for tokens attributed to tool definitions.
        /// </summary>
        [JsonPropertyName("tool_tokens")]
        public int ToolTokens { get; set; } = 0;
    }

    /// <summary>
    /// Provider-specific completion token details.
    /// </summary>
    public class ChatCompletionCompletionTokensDetails
    {
        /// <summary>
        /// Reasoning tokens.
        /// </summary>
        [JsonPropertyName("reasoning_tokens")]
        public int ReasoningTokens { get; set; } = 0;

        /// <summary>
        /// Audio completion tokens.
        /// </summary>
        [JsonPropertyName("audio_tokens")]
        public int AudioTokens { get; set; } = 0;

        /// <summary>
        /// Accepted prediction tokens.
        /// </summary>
        [JsonPropertyName("accepted_prediction_tokens")]
        public int AcceptedPredictionTokens { get; set; } = 0;

        /// <summary>
        /// Rejected prediction tokens.
        /// </summary>
        [JsonPropertyName("rejected_prediction_tokens")]
        public int RejectedPredictionTokens { get; set; } = 0;
    }
}
