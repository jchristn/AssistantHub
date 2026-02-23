namespace AssistantHub.Core.Models
{
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
        /// Weight of full-text score in hybrid mode (0.0 to 1.0).
        /// </summary>
        public double TextWeight { get; set; } = 0.3;

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
    }
}
