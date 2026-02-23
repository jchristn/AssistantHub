namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A retrieved chunk with source identification and scoring.
    /// </summary>
    public class RetrievalChunk
    {
        /// <summary>
        /// The document identifier from RecallDB (maps to AssistantDocument.Id).
        /// </summary>
        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// Cosine similarity score (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("score")]
        public double Score { get; set; } = 0;

        /// <summary>
        /// Full-text relevance score component (null in vector-only mode).
        /// </summary>
        [JsonPropertyName("text_score")]
        public double? TextScore { get; set; }

        /// <summary>
        /// Text content of the matching chunk.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = null;
    }
}
