namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request to score passages against a query with a configured reranker.
    /// </summary>
    public class RerankerTestRequest
    {
        /// <summary>
        /// Query to score the documents against.
        /// </summary>
        [JsonPropertyName("Query")]
        public string Query { get; set; }

        /// <summary>
        /// Passages to score (at most 100).
        /// </summary>
        [JsonPropertyName("Documents")]
        public List<string> Documents { get; set; }
    }
}
