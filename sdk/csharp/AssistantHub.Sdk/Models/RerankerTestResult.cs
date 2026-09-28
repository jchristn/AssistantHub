namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Result of testing a configured reranker.
    /// </summary>
    public class RerankerTestResult
    {
        /// <summary>
        /// Reranker identifier.
        /// </summary>
        [JsonPropertyName("RerankerId")]
        public string RerankerId { get; set; }

        /// <summary>
        /// Whether the reranker call succeeded.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Relevance scores, one per document in request order.
        /// </summary>
        [JsonPropertyName("Scores")]
        public List<double> Scores { get; set; }

        /// <summary>
        /// Error message when the call failed.
        /// </summary>
        [JsonPropertyName("ErrorMessage")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Duration of the reranker call in milliseconds.
        /// </summary>
        [JsonPropertyName("DurationMs")]
        public double DurationMs { get; set; }
    }
}
