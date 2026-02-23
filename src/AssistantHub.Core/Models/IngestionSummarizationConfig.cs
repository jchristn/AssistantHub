namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Summarization configuration for an ingestion rule.
    /// </summary>
    public class IngestionSummarizationConfig
    {
        #region Public-Members

        /// <summary>
        /// ID of a Partio completion endpoint used for summarization.
        /// </summary>
        public string CompletionEndpointId { get; set; } = null;

        /// <summary>
        /// Traversal order for summarization.
        /// </summary>
        public SummarizationOrderEnum Order { get; set; } = SummarizationOrderEnum.BottomUp;

        /// <summary>
        /// Custom prompt template for summarization. Null uses Partio default.
        /// </summary>
        public string SummarizationPrompt { get; set; } = null;

        private int _maxSummaryTokens = 1024;

        /// <summary>
        /// Maximum number of tokens for each summary.
        /// Default: 1024. Minimum: 128.
        /// Values below the minimum are clamped.
        /// </summary>
        public int MaxSummaryTokens
        {
            get => _maxSummaryTokens;
            set => _maxSummaryTokens = Math.Max(128, value);
        }

        private int _minCellLength = 128;

        /// <summary>
        /// Minimum cell content length (in characters) to trigger summarization.
        /// Default: 128. Minimum: 0.
        /// Values below the minimum are clamped.
        /// </summary>
        public int MinCellLength
        {
            get => _minCellLength;
            set => _minCellLength = Math.Max(0, value);
        }

        private int _maxParallelTasks = 1;

        /// <summary>
        /// Maximum number of parallel summarization tasks.
        /// Default: 1. Minimum: 1.
        /// Values below the minimum are clamped.
        /// </summary>
        public int MaxParallelTasks
        {
            get => _maxParallelTasks;
            set => _maxParallelTasks = Math.Max(1, value);
        }

        private int _maxRetriesPerSummary = 3;

        /// <summary>
        /// Maximum retry attempts for an individual cell.
        /// Default: 3. Minimum: 0.
        /// Values below the minimum are clamped.
        /// </summary>
        public int MaxRetriesPerSummary
        {
            get => _maxRetriesPerSummary;
            set => _maxRetriesPerSummary = Math.Max(0, value);
        }

        private int _maxRetries = 9;

        /// <summary>
        /// Global failure limit across all cells (circuit breaker).
        /// Default: 9. Minimum: 0.
        /// Values below the minimum are clamped.
        /// </summary>
        public int MaxRetries
        {
            get => _maxRetries;
            set => _maxRetries = Math.Max(0, value);
        }

        private int _timeoutMs = 300000;

        /// <summary>
        /// Timeout in milliseconds for each summarization request.
        /// Default: 300000. Minimum: 100.
        /// Values below the minimum are clamped.
        /// </summary>
        public int TimeoutMs
        {
            get => _timeoutMs;
            set => _timeoutMs = Math.Max(100, value);
        }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public IngestionSummarizationConfig()
        {
        }

        /// <summary>
        /// Validate the summarization configuration.
        /// </summary>
        /// <param name="config">Configuration to validate.</param>
        /// <returns>List of validation error messages (empty if valid).</returns>
        public static List<string> Validate(IngestionSummarizationConfig config)
        {
            List<string> errors = new List<string>();
            if (config == null) return errors;

            if (String.IsNullOrWhiteSpace(config.CompletionEndpointId))
                errors.Add("CompletionEndpointId is required when summarization is enabled.");

            if (!String.IsNullOrEmpty(config.SummarizationPrompt))
            {
                if (!config.SummarizationPrompt.Contains("{content}"))
                    errors.Add("SummarizationPrompt should contain the {content} placeholder.");
            }

            return errors;
        }

        #endregion
    }
}
