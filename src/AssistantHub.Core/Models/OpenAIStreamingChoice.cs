namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Choice payload for an OpenAI streaming chat chunk.
    /// </summary>
    public class OpenAIStreamingChoice
    {
        /// <summary>
        /// Incremental delta.
        /// </summary>
        public OpenAIStreamingDelta Delta { get; set; } = null;
    }
}
