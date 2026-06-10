namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Token usage information for a chat completion.
    /// </summary>
    public class ChatCompletionUsage
    {
        /// <summary>
        /// Number of tokens in the prompt.
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// Number of tokens in the completion.
        /// </summary>
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Total number of tokens.
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        /// <summary>
        /// Context window size.
        /// </summary>
        [JsonPropertyName("context_window")]
        public int ContextWindow { get; set; }

        /// <summary>
        /// Reasoning tokens reported directly by the provider.
        /// </summary>
        [JsonPropertyName("reasoning_tokens")]
        public int ReasoningTokens { get; set; }

        /// <summary>
        /// Tokens attributed to tool definitions, when reported by a compatible provider.
        /// </summary>
        [JsonPropertyName("tool_definition_tokens")]
        public int ToolDefinitionTokens { get; set; }

        /// <summary>
        /// Alternate provider field for tokens attributed to tool definitions.
        /// </summary>
        [JsonPropertyName("tool_tokens")]
        public int ToolTokens { get; set; }

        /// <summary>
        /// Provider-specific prompt token details.
        /// </summary>
        [JsonPropertyName("prompt_tokens_details")]
        public ChatCompletionPromptTokensDetails PromptTokensDetails { get; set; }

        /// <summary>
        /// Provider-specific completion token details.
        /// </summary>
        [JsonPropertyName("completion_tokens_details")]
        public ChatCompletionCompletionTokensDetails CompletionTokensDetails { get; set; }
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
        public int CachedTokens { get; set; }

        /// <summary>
        /// Audio prompt tokens.
        /// </summary>
        [JsonPropertyName("audio_tokens")]
        public int AudioTokens { get; set; }

        /// <summary>
        /// Tokens attributed to tool definitions.
        /// </summary>
        [JsonPropertyName("tool_definition_tokens")]
        public int ToolDefinitionTokens { get; set; }

        /// <summary>
        /// Alternate provider field for tokens attributed to tool definitions.
        /// </summary>
        [JsonPropertyName("tool_tokens")]
        public int ToolTokens { get; set; }
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
        public int ReasoningTokens { get; set; }

        /// <summary>
        /// Audio completion tokens.
        /// </summary>
        [JsonPropertyName("audio_tokens")]
        public int AudioTokens { get; set; }

        /// <summary>
        /// Accepted prediction tokens.
        /// </summary>
        [JsonPropertyName("accepted_prediction_tokens")]
        public int AcceptedPredictionTokens { get; set; }

        /// <summary>
        /// Rejected prediction tokens.
        /// </summary>
        [JsonPropertyName("rejected_prediction_tokens")]
        public int RejectedPredictionTokens { get; set; }
    }
}
