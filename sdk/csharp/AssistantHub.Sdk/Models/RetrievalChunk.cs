namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A retrieved context chunk from the vector database.
    /// </summary>
    public class RetrievalChunk
    {
        /// <summary>
        /// Document identifier.
        /// </summary>
        [JsonPropertyName("DocumentId")]
        public string DocumentId { get; set; }

        /// <summary>
        /// Retrieval relevance score.
        /// </summary>
        [JsonPropertyName("Score")]
        public double Score { get; set; }

        /// <summary>
        /// LLM-assigned re-rank relevance score.
        /// </summary>
        [JsonPropertyName("RerankScore")]
        public double? RerankScore { get; set; }

        /// <summary>
        /// Reciprocal Rank Fusion score.
        /// </summary>
        [JsonPropertyName("FusionScore")]
        public double? FusionScore { get; set; }

        /// <summary>
        /// Full-text search score.
        /// </summary>
        [JsonPropertyName("TextScore")]
        public double? TextScore { get; set; }

        /// <summary>
        /// Chunk text content.
        /// </summary>
        [JsonPropertyName("Content")]
        public string Content { get; set; }

        /// <summary>
        /// Position of this chunk within the document.
        /// </summary>
        [JsonPropertyName("Position")]
        public int Position { get; set; }

        /// <summary>
        /// Neighboring chunks included by IncludeNeighbors setting.
        /// </summary>
        [JsonPropertyName("Neighbors")]
        public List<RetrievalChunk> Neighbors { get; set; }
    }
}
