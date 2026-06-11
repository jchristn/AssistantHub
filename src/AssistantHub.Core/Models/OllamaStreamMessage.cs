namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Message payload within an Ollama chat stream line.
    /// </summary>
    public class OllamaStreamMessage
    {
        /// <summary>
        /// Message content.
        /// </summary>
        public string Content { get; set; } = null;

        /// <summary>
        /// Provider thinking/reasoning text when returned separately from content.
        /// </summary>
        public string Thinking { get; set; } = null;
    }
}
