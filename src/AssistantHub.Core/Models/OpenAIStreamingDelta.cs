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
    }
}
