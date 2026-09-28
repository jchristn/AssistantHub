namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
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
        /// LLM-assigned relevance score from re-ranking (0.0–10.0), null when re-ranking is disabled.
        /// </summary>
        [JsonPropertyName("rerank_score")]
        public double? RerankScore { get; set; } = null;

        /// <summary>
        /// Reciprocal Rank Fusion score computed across multiple query result lists.
        /// Null when RRF is disabled or only a single query was issued.
        /// </summary>
        [JsonPropertyName("fusion_score")]
        public double? FusionScore { get; set; } = null;

        /// <summary>
        /// Full-text relevance score component (null in vector-only mode).
        /// </summary>
        [JsonPropertyName("text_score")]
        public double? TextScore { get; set; }

        /// <summary>
        /// Raw vector similarity reported by the store (null when the vector leg did not score this chunk).
        /// In hybrid mode <see cref="Score"/> is the fused score, so this is the only raw similarity available.
        /// </summary>
        [JsonPropertyName("vector_score")]
        public double? VectorScore { get; set; } = null;

        /// <summary>
        /// 1-based rank in the hybrid vector leg (null outside hybrid search or when absent from that leg).
        /// </summary>
        [JsonPropertyName("vector_rank")]
        public int? VectorRank { get; set; } = null;

        /// <summary>
        /// 1-based rank in the hybrid text leg (null outside hybrid search or when absent from that leg).
        /// </summary>
        [JsonPropertyName("text_rank")]
        public int? TextRank { get; set; } = null;

        /// <summary>
        /// Text content of the matching chunk.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = null;

        /// <summary>
        /// Positional index of this chunk within its source document.
        /// </summary>
        [JsonPropertyName("position")]
        public int? Position { get; set; } = null;

        /// <summary>
        /// Neighboring chunks surrounding this match in positional order.
        /// Populated when IncludeNeighbors is specified. Null when not requested.
        /// </summary>
        [JsonPropertyName("neighbors")]
        public List<RetrievalChunk> Neighbors { get; set; } = null;

        /// <summary>
        /// First page (or slide) of the source document this chunk came from, when known.
        /// </summary>
        [JsonPropertyName("page_start")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PageStart { get; set; } = null;

        /// <summary>
        /// Last page (or slide) of the source document this chunk came from, when known.
        /// </summary>
        [JsonPropertyName("page_end")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PageEnd { get; set; } = null;

        /// <summary>
        /// Spreadsheet sheet this chunk came from, when known.
        /// </summary>
        [JsonPropertyName("sheet")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Sheet { get; set; } = null;

        /// <summary>
        /// Heading path of the section this chunk came from, when known.
        /// </summary>
        [JsonPropertyName("section")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Section { get; set; } = null;

        /// <summary>
        /// Identifier of the document that supersedes this chunk's document, when the chunk is kept as outdated.
        /// </summary>
        [JsonPropertyName("superseded_by")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string SupersededBy { get; set; } = null;

        /// <summary>
        /// Build a short provenance label such as "p. 3", "pp. 3-4" or "sheet Q1", or null when none is known.
        /// </summary>
        /// <returns>Label or null.</returns>
        public string ProvenanceLabel()
        {
            List<string> parts = new List<string>();
            if (PageStart.HasValue)
            {
                if (PageEnd.HasValue && PageEnd.Value != PageStart.Value) parts.Add("pp. " + PageStart.Value + "-" + PageEnd.Value);
                else parts.Add("p. " + PageStart.Value);
            }
            if (!System.String.IsNullOrEmpty(Sheet)) parts.Add("sheet " + Sheet);
            return parts.Count > 0 ? System.String.Join(", ", parts) : null;
        }

        /// <summary>
        /// Returns the matched chunk's content with neighbor content merged in positional order.
        /// Neighbors before the match are prepended; neighbors after are appended.
        /// Falls back to Content when no neighbors are present.
        /// </summary>
        [JsonIgnore]
        public string MergedContent
        {
            get
            {
                if (Neighbors == null || Neighbors.Count == 0)
                    return Content;

                // Build a combined list of all chunks (neighbors + this match) sorted by position.
                // RecallDB returns neighbors sorted by Position ASC and excludes the matched chunk.
                List<RetrievalChunk> all = new List<RetrievalChunk>(Neighbors.Where(n => n.Content != null));

                // Insert the matched chunk at its correct position
                if (Content != null)
                {
                    all.Add(new RetrievalChunk { Content = Content, Position = Position });
                }

                if (Position.HasValue)
                {
                    all.Sort((a, b) => (a.Position ?? 0).CompareTo(b.Position ?? 0));
                }

                StringBuilder sb = new StringBuilder();
                foreach (RetrievalChunk chunk in all)
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append(chunk.Content);
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Copy the chunk's identity and scores (not its neighbors) so a pipeline stage can be snapshotted before a
        /// later stage mutates or reorders the list.
        /// </summary>
        /// <returns>A shallow copy without neighbors.</returns>
        public RetrievalChunk CloneForSnapshot()
        {
            return new RetrievalChunk
            {
                DocumentId = DocumentId,
                Score = Score,
                RerankScore = RerankScore,
                FusionScore = FusionScore,
                TextScore = TextScore,
                VectorScore = VectorScore,
                VectorRank = VectorRank,
                TextRank = TextRank,
                Content = Content,
                Position = Position,
                PageStart = PageStart,
                PageEnd = PageEnd,
                Sheet = Sheet,
                Section = Section,
                SupersededBy = SupersededBy
            };
        }
    }
}
