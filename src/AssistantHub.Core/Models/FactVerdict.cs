namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Verdict for a single expected fact within an evaluation result.
    /// </summary>
    public class FactVerdict
    {
        /// <summary>
        /// The expected fact text.
        /// </summary>
        public string Fact { get; set; } = null;

        /// <summary>
        /// Whether the fact was found in the response.
        /// </summary>
        public bool Pass { get; set; } = false;

        /// <summary>
        /// LLM judge reasoning for the verdict.
        /// </summary>
        public string Reasoning { get; set; } = null;
    }
}
