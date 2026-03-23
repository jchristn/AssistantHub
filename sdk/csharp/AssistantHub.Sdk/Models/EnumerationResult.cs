namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Paginated enumeration result.
    /// </summary>
    /// <typeparam name="T">Type of objects in the result.</typeparam>
    public class EnumerationResult<T>
    {
        /// <summary>
        /// Whether the query was successful.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Maximum results requested.
        /// </summary>
        [JsonPropertyName("MaxResults")]
        public int MaxResults { get; set; }

        /// <summary>
        /// Total number of records available.
        /// </summary>
        [JsonPropertyName("TotalRecords")]
        public int TotalRecords { get; set; }

        /// <summary>
        /// Number of records remaining.
        /// </summary>
        [JsonPropertyName("RecordsRemaining")]
        public int RecordsRemaining { get; set; }

        /// <summary>
        /// Continuation token for the next page.
        /// </summary>
        [JsonPropertyName("ContinuationToken")]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Whether all records have been returned.
        /// </summary>
        [JsonPropertyName("EndOfResults")]
        public bool EndOfResults { get; set; }

        /// <summary>
        /// Result objects.
        /// </summary>
        [JsonPropertyName("Objects")]
        public List<T> Objects { get; set; }

        /// <summary>
        /// Total query time in milliseconds.
        /// </summary>
        [JsonPropertyName("TotalMs")]
        public double TotalMs { get; set; }
    }
}
