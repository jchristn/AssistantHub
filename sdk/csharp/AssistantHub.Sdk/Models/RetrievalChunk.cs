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
        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; }

        /// <summary>
        /// Retrieval relevance score.
        /// </summary>
        [JsonPropertyName("score")]
        public double Score { get; set; }

        /// <summary>
        /// LLM-assigned re-rank relevance score.
        /// </summary>
        [JsonPropertyName("rerank_score")]
        public double? RerankScore { get; set; }

        /// <summary>
        /// Reciprocal Rank Fusion score.
        /// </summary>
        [JsonPropertyName("fusion_score")]
        public double? FusionScore { get; set; }

        /// <summary>
        /// Full-text search score.
        /// </summary>
        [JsonPropertyName("text_score")]
        public double? TextScore { get; set; }

        /// <summary>
        /// Chunk text content.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; }

        /// <summary>
        /// Position of this chunk within the document.
        /// </summary>
        [JsonPropertyName("position")]
        public int? Position { get; set; }

        /// <summary>
        /// Neighboring chunks included by IncludeNeighbors setting.
        /// </summary>
        [JsonPropertyName("neighbors")]
        public List<RetrievalChunk> Neighbors { get; set; }

        /// <summary>
        /// First page of the chunk, when known.
        /// </summary>
        [JsonPropertyName("page_start")]
        public int? PageStart { get; set; }

        /// <summary>
        /// Last page of the chunk, when known.
        /// </summary>
        [JsonPropertyName("page_end")]
        public int? PageEnd { get; set; }

        /// <summary>
        /// Spreadsheet sheet name of the chunk, when known.
        /// </summary>
        [JsonPropertyName("sheet")]
        public string Sheet { get; set; }

        /// <summary>
        /// Section heading of the chunk, when known.
        /// </summary>
        [JsonPropertyName("section")]
        public string Section { get; set; }

        /// <summary>
        /// Identifier of the document that supersedes this chunk's document, when applicable.
        /// </summary>
        [JsonPropertyName("superseded_by")]
        public string SupersededBy { get; set; }
    }
}
