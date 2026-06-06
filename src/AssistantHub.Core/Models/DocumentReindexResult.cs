namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Result for a single document Verbex reindex operation.
    /// </summary>
    public class DocumentReindexResult
    {
        #region Public-Members

        /// <summary>
        /// AssistantHub document identifier.
        /// </summary>
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// Whether the reindex operation succeeded or was intentionally skipped.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Operation status: Reindexed, Skipped, Failed, or NotFound.
        /// </summary>
        public string Status { get; set; } = null;

        /// <summary>
        /// Human-readable operation result.
        /// </summary>
        public string Message { get; set; } = null;

        /// <summary>
        /// Verbex tenant identifier used for indexing.
        /// </summary>
        public string VerbexTenantId { get; set; } = null;

        /// <summary>
        /// Verbex index identifier used for indexing.
        /// </summary>
        public string VerbexIndexId { get; set; } = null;

        /// <summary>
        /// Verbex record identifier used for indexing.
        /// </summary>
        public string VerbexRecordId { get; set; } = null;

        /// <summary>
        /// Total milliseconds elapsed.
        /// </summary>
        public double TotalMs { get; set; } = 0;

        #endregion
    }
}
