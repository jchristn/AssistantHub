namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Ingestion rule record.
    /// </summary>
    public class IngestionRule
    {
        /// <summary>
        /// Unique identifier with prefix irule_.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Display name.
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Description.
        /// </summary>
        [JsonPropertyName("Description")]
        public string Description { get; set; }

        /// <summary>
        /// S3 bucket name.
        /// </summary>
        [JsonPropertyName("Bucket")]
        public string Bucket { get; set; }

        /// <summary>
        /// Collection name.
        /// </summary>
        [JsonPropertyName("CollectionName")]
        public string CollectionName { get; set; }

        /// <summary>
        /// Collection identifier.
        /// </summary>
        [JsonPropertyName("CollectionId")]
        public string CollectionId { get; set; }

        /// <summary>
        /// Labels.
        /// </summary>
        [JsonPropertyName("Labels")]
        public List<string> Labels { get; set; }

        /// <summary>
        /// Tags.
        /// </summary>
        [JsonPropertyName("Tags")]
        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Atomization setting.
        /// </summary>
        [JsonPropertyName("Atomization")]
        public string Atomization { get; set; }

        /// <summary>
        /// Summarization configuration.
        /// </summary>
        [JsonPropertyName("Summarization")]
        public IngestionSummarizationConfig Summarization { get; set; }

        /// <summary>
        /// Chunking configuration.
        /// </summary>
        [JsonPropertyName("Chunking")]
        public IngestionChunkingConfig Chunking { get; set; }

        /// <summary>
        /// Embedding configuration.
        /// </summary>
        [JsonPropertyName("Embedding")]
        public IngestionEmbeddingConfig Embedding { get; set; }

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

    /// <summary>
    /// Ingestion chunking configuration.
    /// </summary>
    public class IngestionChunkingConfig
    {
        /// <summary>
        /// Chunking strategy.
        /// </summary>
        [JsonPropertyName("Strategy")]
        public string Strategy { get; set; }

        /// <summary>
        /// Fixed token count per chunk.
        /// </summary>
        [JsonPropertyName("FixedTokenCount")]
        public int FixedTokenCount { get; set; }

        /// <summary>
        /// Overlap count.
        /// </summary>
        [JsonPropertyName("OverlapCount")]
        public int OverlapCount { get; set; }

        /// <summary>
        /// Overlap percentage (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("OverlapPercentage")]
        public double OverlapPercentage { get; set; }

        /// <summary>
        /// Overlap strategy.
        /// </summary>
        [JsonPropertyName("OverlapStrategy")]
        public string OverlapStrategy { get; set; }

        /// <summary>
        /// Row group size.
        /// </summary>
        [JsonPropertyName("RowGroupSize")]
        public int RowGroupSize { get; set; }

        /// <summary>
        /// Context prefix.
        /// </summary>
        [JsonPropertyName("ContextPrefix")]
        public string ContextPrefix { get; set; }

        /// <summary>
        /// Regex pattern.
        /// </summary>
        [JsonPropertyName("RegexPattern")]
        public string RegexPattern { get; set; }
    }

    /// <summary>
    /// Ingestion embedding configuration.
    /// </summary>
    public class IngestionEmbeddingConfig
    {
        /// <summary>
        /// Embedding endpoint identifier.
        /// </summary>
        [JsonPropertyName("EmbeddingEndpointId")]
        public string EmbeddingEndpointId { get; set; }

        /// <summary>
        /// Whether to apply L2 normalization.
        /// </summary>
        [JsonPropertyName("L2Normalization")]
        public bool L2Normalization { get; set; }
    }
}
