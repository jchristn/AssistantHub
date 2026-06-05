namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Ingestion summarization configuration.
    /// </summary>
    public class IngestionSummarizationConfig
    {
        /// <summary>
        /// Completion endpoint identifier.
        /// </summary>
        [JsonPropertyName("CompletionEndpointId")]
        public string CompletionEndpointId { get; set; }

        /// <summary>
        /// Summarization order.
        /// </summary>
        [JsonPropertyName("Order")]
        public SummarizationOrderEnum Order { get; set; }

        /// <summary>
        /// Summarization prompt template.
        /// </summary>
        [JsonPropertyName("SummarizationPrompt")]
        public string SummarizationPrompt { get; set; }

        /// <summary>
        /// Maximum summary tokens.
        /// </summary>
        [JsonPropertyName("MaxSummaryTokens")]
        public int MaxSummaryTokens { get; set; }

        /// <summary>
        /// Minimum cell length.
        /// </summary>
        [JsonPropertyName("MinCellLength")]
        public int MinCellLength { get; set; }

        /// <summary>
        /// Maximum parallel tasks.
        /// </summary>
        [JsonPropertyName("MaxParallelTasks")]
        public int MaxParallelTasks { get; set; }

        /// <summary>
        /// Maximum retries per summary.
        /// </summary>
        [JsonPropertyName("MaxRetriesPerSummary")]
        public int MaxRetriesPerSummary { get; set; }

        /// <summary>
        /// Maximum retries overall.
        /// </summary>
        [JsonPropertyName("MaxRetries")]
        public int MaxRetries { get; set; }

        /// <summary>
        /// Timeout in milliseconds.
        /// </summary>
        [JsonPropertyName("TimeoutMs")]
        public int TimeoutMs { get; set; }
    }
}
