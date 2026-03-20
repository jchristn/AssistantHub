namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Content payload in a Gemini response candidate.
    /// </summary>
    public class GeminiContent
    {
        /// <summary>
        /// Content parts.
        /// </summary>
        public List<GeminiContentPart> Parts { get; set; } = new List<GeminiContentPart>();
    }
}
