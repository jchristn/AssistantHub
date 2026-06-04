namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Streaming chunk payload from an OpenAI-compatible chat completion stream.
    /// </summary>
    public class OpenAIStreamingChunk
    {
        /// <summary>
        /// Chunk choices.
        /// </summary>
        public List<OpenAIStreamingChoice> Choices { get; set; } = new List<OpenAIStreamingChoice>();

        /// <summary>
        /// Optional token usage included by OpenAI-compatible providers when requested.
        /// </summary>
        public ChatCompletionUsage Usage { get; set; } = null;
    }
}
