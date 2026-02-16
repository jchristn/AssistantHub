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
        /// Embedding model name.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Whether to apply L2 normalization to embeddings.
        /// </summary>
        public bool L2Normalization { get; set; } = false;

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
