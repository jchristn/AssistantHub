namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Aggregated count of retrieval candidates dropped at a pipeline stage.
    /// </summary>
    public class RetrievalCandidateDropSummary
    {
        /// <summary>
        /// Pipeline stage that dropped the candidates.
        /// </summary>
        [JsonPropertyName("stage")]
        public string Stage { get; set; }

        /// <summary>
        /// Reason the candidates were dropped.
        /// </summary>
        [JsonPropertyName("reason")]
        public string Reason { get; set; }

        /// <summary>
        /// Number of candidates dropped for this stage and reason.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
