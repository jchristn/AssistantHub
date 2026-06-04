namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Token usage metadata returned by Gemini.
    /// </summary>
    public class GeminiUsageMetadata
    {
        /// <summary>
        /// Prompt token count.
        /// </summary>
        public int? PromptTokenCount { get; set; } = null;

        /// <summary>
        /// Candidate token count.
        /// </summary>
        public int? CandidatesTokenCount { get; set; } = null;

        /// <summary>
        /// Total token count.
        /// </summary>
        public int? TotalTokenCount { get; set; } = null;
    }
}
