namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

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

        /// <summary>
        /// Total request duration reported by Ollama in nanoseconds.
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long? TotalDuration { get; set; } = null;

        /// <summary>
        /// Model load duration reported by Ollama in nanoseconds.
        /// </summary>
        [JsonPropertyName("load_duration")]
        public long? LoadDuration { get; set; } = null;

        /// <summary>
        /// Prompt evaluation token count.
        /// </summary>
        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; } = null;

        /// <summary>
        /// Prompt evaluation duration reported by Ollama in nanoseconds.
        /// </summary>
        [JsonPropertyName("prompt_eval_duration")]
        public long? PromptEvalDuration { get; set; } = null;

        /// <summary>
        /// Generation token count.
        /// </summary>
        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; } = null;

        /// <summary>
        /// Generation duration reported by Ollama in nanoseconds.
        /// </summary>
        [JsonPropertyName("eval_duration")]
        public long? EvalDuration { get; set; } = null;
    }
}
