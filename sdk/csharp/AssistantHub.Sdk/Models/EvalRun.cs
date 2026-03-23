namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Evaluation run record.
    /// </summary>
    public class EvalRun
    {
        /// <summary>
        /// Unique identifier with prefix erun_.
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
        /// Run status.
        /// </summary>
        [JsonPropertyName("Status")]
        public EvalStatusEnum Status { get; set; }

        /// <summary>
        /// Total facts in the run.
        /// </summary>
        [JsonPropertyName("TotalFacts")]
        public int TotalFacts { get; set; }

        /// <summary>
        /// Number of facts evaluated.
        /// </summary>
        [JsonPropertyName("FactsEvaluated")]
        public int FactsEvaluated { get; set; }

        /// <summary>
        /// Number of facts that passed.
        /// </summary>
        [JsonPropertyName("FactsPassed")]
        public int FactsPassed { get; set; }

        /// <summary>
        /// Number of facts that failed.
        /// </summary>
        [JsonPropertyName("FactsFailed")]
        public int FactsFailed { get; set; }

        /// <summary>
        /// Pass rate (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("PassRate")]
        public double PassRate { get; set; }

        /// <summary>
        /// Judge prompt used for evaluation.
        /// </summary>
        [JsonPropertyName("JudgePrompt")]
        public string JudgePrompt { get; set; }

        /// <summary>
        /// Timestamp when the run started in UTC.
        /// </summary>
        [JsonPropertyName("StartedUtc")]
        public DateTime? StartedUtc { get; set; }

        /// <summary>
        /// Timestamp when the run completed in UTC.
        /// </summary>
        [JsonPropertyName("CompletedUtc")]
        public DateTime? CompletedUtc { get; set; }

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        [JsonPropertyName("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }
    }
}
