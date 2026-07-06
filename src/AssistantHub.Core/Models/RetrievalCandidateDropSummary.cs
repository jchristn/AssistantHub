namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Safe summary of retrieval candidates removed before final context or answer generation.
    /// </summary>
    public class RetrievalCandidateDropSummary
    {
        /// <summary>
        /// Pipeline stage that removed the candidate(s).
        /// </summary>
        [JsonPropertyName("stage")]
        public string Stage { get; set; } = null;

        /// <summary>
        /// Safe reason for removal.
        /// </summary>
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = null;

        /// <summary>
        /// Number of candidates removed for this reason.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; } = 0;
    }
}
