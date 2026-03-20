namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Candidate payload in a Gemini response.
    /// </summary>
    public class GeminiCandidate
    {
        /// <summary>
        /// Candidate content.
        /// </summary>
        public GeminiContent Content { get; set; } = null;
    }
}
