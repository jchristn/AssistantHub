namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Delta payload for an OpenAI streaming chat chunk.
    /// </summary>
    public class OpenAIStreamingDelta
    {
        /// <summary>
        /// Delta content text.
        /// </summary>
        public string Content { get; set; } = null;

        /// <summary>
        /// Delta provider thinking/reasoning text when returned separately from content.
        /// </summary>
        public string Thinking { get; set; } = null;

        /// <summary>
        /// OpenAI-compatible reasoning delta used by some models.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("reasoning_content")]
        public string ReasoningContent { get; set; } = null;
    }
}
