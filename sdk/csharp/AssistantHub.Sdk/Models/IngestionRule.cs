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
}
