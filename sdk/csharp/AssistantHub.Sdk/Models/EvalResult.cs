namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Evaluation result record.
    /// </summary>
    public class EvalResult
    {
        /// <summary>
        /// Unique identifier with prefix eres_.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Run identifier.
        /// </summary>
        [JsonPropertyName("RunId")]
        public string RunId { get; set; }

        /// <summary>
        /// Fact identifier.
        /// </summary>
        [JsonPropertyName("FactId")]
        public string FactId { get; set; }

        /// <summary>
        /// Question asked.
        /// </summary>
        [JsonPropertyName("Question")]
        public string Question { get; set; }

        /// <summary>
        /// Expected facts (JSON array string).
        /// </summary>
        [JsonPropertyName("ExpectedFacts")]
        public string ExpectedFacts { get; set; }

        /// <summary>
        /// LLM response text.
        /// </summary>
        [JsonPropertyName("LlmResponse")]
        public string LlmResponse { get; set; }

        /// <summary>
        /// Individual fact verdicts (JSON array string).
        /// </summary>
        [JsonPropertyName("FactVerdicts")]
        public string FactVerdicts { get; set; }

        /// <summary>
        /// Whether the overall evaluation passed.
        /// </summary>
        [JsonPropertyName("OverallPass")]
        public bool OverallPass { get; set; }

        /// <summary>
        /// Duration in milliseconds.
        /// </summary>
        [JsonPropertyName("DurationMs")]
        public double DurationMs { get; set; }

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        [JsonPropertyName("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }
    }
}
