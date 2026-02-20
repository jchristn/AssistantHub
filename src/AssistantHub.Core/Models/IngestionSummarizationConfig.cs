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

        /// <summary>
        /// Maximum number of tokens for each summary.
        /// </summary>
        public int MaxSummaryTokens { get; set; } = 1024;

        /// <summary>
        /// Minimum cell content length (in characters) to trigger summarization.
        /// </summary>
        public int MinCellLength { get; set; } = 128;

        /// <summary>
        /// Maximum number of parallel summarization tasks.
        /// </summary>
        public int MaxParallelTasks { get; set; } = 4;

        /// <summary>
        /// Maximum retry attempts for an individual cell.
        /// </summary>
        public int MaxRetriesPerSummary { get; set; } = 3;

        /// <summary>
        /// Global failure limit across all cells (circuit breaker).
        /// </summary>
        public int MaxRetries { get; set; } = 9;

        /// <summary>
        /// Timeout in milliseconds for each summarization request.
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;

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

            if (config.MaxSummaryTokens < 128)
                errors.Add("MaxSummaryTokens must be >= 128.");

            if (config.MinCellLength < 0)
                errors.Add("MinCellLength must be >= 0.");

            if (config.MaxParallelTasks < 1)
                errors.Add("MaxParallelTasks must be >= 1.");

            if (config.MaxRetriesPerSummary < 0)
                errors.Add("MaxRetriesPerSummary must be >= 0.");

            if (config.MaxRetries < 0)
                errors.Add("MaxRetries must be >= 0.");

            if (config.TimeoutMs < 100)
                errors.Add("TimeoutMs must be >= 100.");

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
