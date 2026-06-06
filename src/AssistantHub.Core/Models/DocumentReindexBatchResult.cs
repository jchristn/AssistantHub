namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Result for a batch document Verbex reindex operation.
    /// </summary>
    public class DocumentReindexBatchResult
    {
        #region Public-Members

        /// <summary>
        /// Number of documents considered by this request.
        /// </summary>
        public int Requested { get; set; } = 0;

        /// <summary>
        /// Number of documents eligible for reindexing.
        /// </summary>
        public int Eligible { get; set; } = 0;

        /// <summary>
        /// Number of documents reindexed successfully.
        /// </summary>
        public int Reindexed { get; set; } = 0;

        /// <summary>
        /// Number of documents skipped.
        /// </summary>
        public int Skipped { get; set; } = 0;

        /// <summary>
        /// Number of documents that failed reindexing.
        /// </summary>
        public int Failed { get; set; } = 0;

        /// <summary>
        /// Continuation token for the next enumerated batch when document IDs are not supplied explicitly.
        /// </summary>
        public string ContinuationToken { get; set; } = null;

        /// <summary>
        /// Indicates whether the end of enumerated documents has been reached.
        /// </summary>
        public bool EndOfResults { get; set; } = true;

        /// <summary>
        /// Per-document results.
        /// </summary>
        public List<DocumentReindexResult> Results { get; set; } = new List<DocumentReindexResult>();

        /// <summary>
        /// Total milliseconds elapsed.
        /// </summary>
        public double TotalMs { get; set; } = 0;

        #endregion
    }
}
