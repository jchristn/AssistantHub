namespace AssistantHub.Core.Models
{
    using System;

    /// <summary>
    /// Chunking configuration for an ingestion rule.
    /// </summary>
    public class IngestionChunkingConfig
    {
        #region Public-Members

        /// <summary>
        /// Chunking strategy (e.g. FixedTokenCount, SentenceBased, ParagraphBased, RegexBased, etc.).
        /// </summary>
        public string Strategy { get; set; } = "FixedTokenCount";

        /// <summary>
        /// Fixed token count per chunk.
        /// </summary>
        public int FixedTokenCount { get; set; } = 256;

        /// <summary>
        /// Number of overlapping tokens between chunks.
        /// </summary>
        public int OverlapCount { get; set; } = 0;

        /// <summary>
        /// Overlap percentage between chunks (0-1).
        /// </summary>
        public double? OverlapPercentage { get; set; } = null;

        /// <summary>
        /// Overlap strategy (e.g. SlidingWindow, SentenceBoundaryAware, SemanticBoundaryAware).
        /// </summary>
        public string OverlapStrategy { get; set; } = null;

        /// <summary>
        /// Number of rows per group for row-based chunking.
        /// </summary>
        public int RowGroupSize { get; set; } = 5;

        /// <summary>
        /// Context prefix to prepend to each chunk.
        /// </summary>
        public string ContextPrefix { get; set; } = null;

        /// <summary>
        /// Regex pattern for regex-based chunking.
        /// </summary>
        public string RegexPattern { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public IngestionChunkingConfig()
        {
        }

        #endregion
    }
}
