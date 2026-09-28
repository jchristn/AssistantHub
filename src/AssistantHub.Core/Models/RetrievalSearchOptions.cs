namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Search options for retrieval queries supporting vector, full-text, and hybrid modes.
    /// </summary>
    public class RetrievalSearchOptions
    {
        /// <summary>
        /// Search mode: Vector, FullText, or Hybrid.
        /// </summary>
        public string SearchMode { get; set; } = "Vector";

        /// <summary>
        /// Share of the full-text leg in hybrid mode (0.0 to 1.0).
        /// </summary>
        public double TextWeight { get; set; } = 0.3;

        /// <summary>
        /// Hybrid fusion strategy sent to RecallDB: Rrf or Linear.
        /// </summary>
        public string FusionStrategy { get; set; } = "Rrf";

        /// <summary>
        /// RRF constant k sent to RecallDB for Rrf fusion.
        /// </summary>
        public int RrfK { get; set; } = 60;

        /// <summary>
        /// Candidates each hybrid leg retrieves before fusion. Null uses RecallDB's default.
        /// </summary>
        public int? FusionCandidatePool { get; set; } = null;

        /// <summary>
        /// Weight of RecallDB's recency signal in Rrf fusion. 0 disables it.
        /// </summary>
        public double RecencyWeight { get; set; } = 0.0;

        /// <summary>
        /// Apply the score threshold to full-text scores too. Off by default, because an assistant's threshold is a
        /// vector-similarity threshold and full-text scores sit far below it; set it when a caller chose a threshold
        /// for this search explicitly (for example a tool call's score_threshold).
        /// </summary>
        public bool ApplyThresholdToFullText { get; set; } = false;

        /// <summary>
        /// Prepend the embedding model's query task prefix (for example "search_query: ") before embedding the query.
        /// </summary>
        public bool EmbeddingTaskPrefixes { get; set; } = false;

        /// <summary>
        /// Full-text ranking function: TsRank or TsRankCd.
        /// </summary>
        public string FullTextSearchType { get; set; } = "TsRank";

        /// <summary>
        /// PostgreSQL text search language configuration.
        /// </summary>
        public string FullTextLanguage { get; set; } = "english";

        /// <summary>
        /// Full-text score normalization bitmask.
        /// </summary>
        public int FullTextNormalization { get; set; } = 32;

        /// <summary>
        /// Minimum full-text score threshold. Null means no threshold.
        /// </summary>
        public double? FullTextMinimumScore { get; set; } = null;

        /// <summary>
        /// Number of neighboring chunks to include before and after each matched chunk (0-10).
        /// Passed to RecallDB as IncludeNeighbors on the search query.
        /// </summary>
        public int IncludeNeighbors { get; set; } = 0;

        /// <summary>
        /// Optional metadata filter to restrict retrieval to documents matching specified labels and/or tags.
        /// </summary>
        public ChatMetadataFilter MetadataFilter { get; set; } = null;

        /// <summary>
        /// Optional AssistantDocument.Id values used to restrict retrieval to specific documents.
        /// </summary>
        public List<string> DocumentIds { get; set; } = null;

        /// <summary>
        /// Set by RetrievalService when a hybrid search falls back to vector-only retrieval.
        /// </summary>
        public bool HybridFallbackRan { get; set; } = false;

        /// <summary>
        /// Set by the retrieval service when the query embedding could not be generated (after retries), so vector
        /// and hybrid search returned nothing. Callers surface it instead of treating the turn as "no relevant context".
        /// </summary>
        public bool EmbeddingFailed { get; set; } = false;

        /// <summary>
        /// Set by the retrieval service when a hybrid search could not embed the query and ran its full-text leg alone,
        /// so the turn still gets keyword matches instead of no context.
        /// </summary>
        public bool KeywordFallbackRan { get; set; } = false;

        /// <summary>
        /// Build search options from an assistant's retrieval settings.
        /// </summary>
        /// <param name="settings">Assistant settings.</param>
        /// <param name="metadataFilter">Optional metadata filter.</param>
        /// <param name="documentIds">Optional document scope.</param>
        /// <returns>Search options.</returns>
        public static RetrievalSearchOptions FromAssistantSettings(AssistantSettings settings, ChatMetadataFilter metadataFilter = null, List<string> documentIds = null)
        {
            if (settings == null) return new RetrievalSearchOptions { MetadataFilter = metadataFilter, DocumentIds = documentIds };

            return new RetrievalSearchOptions
            {
                SearchMode = settings.SearchMode,
                TextWeight = settings.TextWeight,
                FusionStrategy = settings.FusionStrategy,
                RrfK = settings.RrfK,
                FusionCandidatePool = settings.FusionCandidatePool,
                RecencyWeight = settings.RecencyWeight,
                EmbeddingTaskPrefixes = settings.EmbeddingTaskPrefixes,
                FullTextSearchType = settings.FullTextSearchType,
                FullTextLanguage = settings.FullTextLanguage,
                FullTextNormalization = settings.FullTextNormalization,
                FullTextMinimumScore = settings.FullTextMinimumScore,
                IncludeNeighbors = settings.RetrievalIncludeNeighbors,
                MetadataFilter = metadataFilter,
                DocumentIds = documentIds
            };
        }
    }
}
