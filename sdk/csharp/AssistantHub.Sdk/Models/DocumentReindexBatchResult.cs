namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Result for a batch document Verbex reindex operation.
    /// </summary>
    public class DocumentReindexBatchResult
    {
        /// <summary>
        /// Number of documents considered by this request.
        /// </summary>
        [JsonPropertyName("Requested")]
        public int Requested { get; set; }

        /// <summary>
        /// Number of documents eligible for reindexing.
        /// </summary>
        [JsonPropertyName("Eligible")]
        public int Eligible { get; set; }

        /// <summary>
        /// Number of documents reindexed successfully.
        /// </summary>
        [JsonPropertyName("Reindexed")]
        public int Reindexed { get; set; }

        /// <summary>
        /// Number of documents skipped.
        /// </summary>
        [JsonPropertyName("Skipped")]
        public int Skipped { get; set; }

        /// <summary>
        /// Number of documents that failed reindexing.
        /// </summary>
        [JsonPropertyName("Failed")]
        public int Failed { get; set; }

        /// <summary>
        /// Continuation token for the next enumerated batch.
        /// </summary>
        [JsonPropertyName("ContinuationToken")]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Indicates whether the end of enumerated documents has been reached.
        /// </summary>
        [JsonPropertyName("EndOfResults")]
        public bool EndOfResults { get; set; }

        /// <summary>
        /// Per-document results.
        /// </summary>
        [JsonPropertyName("Results")]
        public List<DocumentReindexResult> Results { get; set; }

        /// <summary>
        /// Total milliseconds elapsed.
        /// </summary>
        [JsonPropertyName("TotalMs")]
        public double TotalMs { get; set; }
    }
}
