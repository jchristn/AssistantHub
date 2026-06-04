namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Typed Gemini response payload.
    /// </summary>
    public class GeminiResponse
    {
        /// <summary>
        /// Response candidates.
        /// </summary>
        public List<GeminiCandidate> Candidates { get; set; } = new List<GeminiCandidate>();

        /// <summary>
        /// Optional token usage metadata returned by Gemini.
        /// </summary>
        public GeminiUsageMetadata UsageMetadata { get; set; } = null;
    }
}
