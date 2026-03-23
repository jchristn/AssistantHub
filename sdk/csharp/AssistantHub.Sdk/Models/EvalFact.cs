namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Evaluation fact record.
    /// </summary>
    public class EvalFact
    {
        /// <summary>
        /// Unique identifier with prefix ef_.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Assistant identifier.
        /// </summary>
        [JsonPropertyName("AssistantId")]
        public string AssistantId { get; set; }

        /// <summary>
        /// Fact category.
        /// </summary>
        [JsonPropertyName("Category")]
        public string Category { get; set; }

        /// <summary>
        /// Question to ask the assistant.
        /// </summary>
        [JsonPropertyName("Question")]
        public string Question { get; set; }

        /// <summary>
        /// Expected facts (JSON array string).
        /// </summary>
        [JsonPropertyName("ExpectedFacts")]
        public string ExpectedFacts { get; set; }

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        [JsonPropertyName("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated in UTC.
        /// </summary>
        [JsonPropertyName("LastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; }
    }
}
