namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Response payload for bulk request-history deletion.
    /// </summary>
    public class RequestHistoryDeleteResult
    {
        /// <summary>
        /// Number of deleted records.
        /// </summary>
        [JsonPropertyName("DeletedCount")]
        public int DeletedCount { get; set; }
    }
}
