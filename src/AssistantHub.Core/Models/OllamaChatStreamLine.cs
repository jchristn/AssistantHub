namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Single JSON line from an Ollama streaming chat response.
    /// </summary>
    public class OllamaChatStreamLine
    {
        /// <summary>
        /// Whether generation is complete.
        /// </summary>
        public bool Done { get; set; } = false;

        /// <summary>
        /// Message payload.
        /// </summary>
        public OllamaStreamMessage Message { get; set; } = null;
    }
}
