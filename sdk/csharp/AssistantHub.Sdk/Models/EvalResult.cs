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
        /// Chat history row created by ChatRail eval execution.
        /// </summary>
        [JsonPropertyName("ChatHistoryId")]
        public string ChatHistoryId { get; set; }

        /// <summary>
        /// Trace identifier used for ChatRail eval execution.
        /// </summary>
        [JsonPropertyName("TraceId")]
        public string TraceId { get; set; }

        /// <summary>
        /// Serialized retrieval telemetry captured from ChatRail execution.
        /// </summary>
        [JsonPropertyName("RetrievalJson")]
        public string RetrievalJson { get; set; }

        /// <summary>
        /// Serialized citation metadata captured from ChatRail execution.
        /// </summary>
        [JsonPropertyName("CitationsJson")]
        public string CitationsJson { get; set; }

        /// <summary>
        /// Serialized safe tool-call metadata captured from ChatRail execution.
        /// </summary>
        [JsonPropertyName("ToolCallsJson")]
        public string ToolCallsJson { get; set; }

        /// <summary>
        /// Query class assigned by the answerability classifier.
        /// </summary>
        [JsonPropertyName("QueryClass")]
        public string QueryClass { get; set; }

        /// <summary>
        /// Answerability decision recorded for the evaluated chat turn.
        /// </summary>
        [JsonPropertyName("AnswerabilityDecision")]
        public string AnswerabilityDecision { get; set; }

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
