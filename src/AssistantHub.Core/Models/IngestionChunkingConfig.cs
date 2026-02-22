namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Chunking configuration for an ingestion rule.
    /// </summary>
    public class IngestionChunkingConfig
    {
        #region Public-Members

        /// <summary>
        /// Chunking strategy (e.g. None, FixedTokenCount, SentenceBased, ParagraphBased, RegexBased, etc.).
        /// When set to "None", chunking is skipped and the entire document content is treated as a single chunk.
        /// </summary>
        public string Strategy { get; set; } = "FixedTokenCount";

        private int _fixedTokenCount = 256;

        /// <summary>
        /// Fixed token count per chunk.
        /// Default: 256. Minimum: 1.
        /// Values below the minimum are clamped.
        /// </summary>
        public int FixedTokenCount
        {
            get => _fixedTokenCount;
            set => _fixedTokenCount = Math.Max(1, value);
        }

        private int _overlapCount = 0;

        /// <summary>
        /// Number of overlapping tokens between chunks.
        /// Default: 0. Minimum: 0.
        /// Values below the minimum are clamped.
        /// </summary>
        public int OverlapCount
        {
            get => _overlapCount;
            set => _overlapCount = Math.Max(0, value);
        }

        private double? _overlapPercentage = null;

        /// <summary>
        /// Overlap percentage between chunks.
        /// Default: null. Minimum: 0.0. Maximum: 1.0.
        /// Values outside the range are clamped.
        /// </summary>
        public double? OverlapPercentage
        {
            get => _overlapPercentage;
            set => _overlapPercentage = value.HasValue ? Math.Clamp(value.Value, 0.0, 1.0) : null;
        }

        /// <summary>
        /// Overlap strategy (e.g. SlidingWindow, SentenceBoundaryAware, SemanticBoundaryAware).
        /// </summary>
        public string OverlapStrategy { get; set; } = null;

        private int _rowGroupSize = 5;

        /// <summary>
        /// Number of rows per group for row-based chunking.
        /// Default: 5. Minimum: 1.
        /// Values below the minimum are clamped.
        /// </summary>
        public int RowGroupSize
        {
            get => _rowGroupSize;
            set => _rowGroupSize = Math.Max(1, value);
        }

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

        /// <summary>
        /// Validate the chunking configuration.
        /// </summary>
        /// <param name="config">Configuration to validate.</param>
        /// <returns>List of validation error messages (empty if valid).</returns>
        public static List<string> Validate(IngestionChunkingConfig config)
        {
            List<string> errors = new List<string>();
            if (config == null) return errors;

            if (String.IsNullOrWhiteSpace(config.Strategy))
                errors.Add("Strategy is required.");

            return errors;
        }

        #endregion
    }
}
