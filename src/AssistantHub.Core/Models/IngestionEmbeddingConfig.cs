namespace AssistantHub.Core.Models
{
    using System;

    /// <summary>
    /// Embedding configuration for an ingestion rule.
    /// </summary>
    public class IngestionEmbeddingConfig
    {
        #region Public-Members

        /// <summary>
        /// ID of the embedding endpoint to use.
        /// </summary>
        public string EmbeddingEndpointId { get; set; } = null;

        /// <summary>
        /// Whether to apply L2 normalization to embeddings.
        /// </summary>
        public bool L2Normalization { get; set; } = false;

        /// <summary>
        /// Prepend the embedding model's document task prefix (for example "search_document: " for nomic-embed-text)
        /// when embedding chunks. The stored chunk text is unchanged. Assistants searching the collection should turn
        /// on EmbeddingTaskPrefixes so queries get the matching query prefix.
        /// </summary>
        public bool TaskPrefixes { get; set; } = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public IngestionEmbeddingConfig()
        {
        }

        #endregion
    }
}
