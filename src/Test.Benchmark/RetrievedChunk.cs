namespace Test.Benchmark
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A retrieved chunk as AssistantHub reports it (retrieve route and chat <c>retrieval.chunks</c>).
    /// </summary>
    public class RetrievedChunk
    {
        #region Public-Members

        /// <summary>AssistantHub document id.</summary>
        [JsonPropertyName("document_id")]
        public string? DocumentId { get; set; } = null;

        /// <summary>Score used for ranking (fused in hybrid mode).</summary>
        [JsonPropertyName("score")]
        public double Score { get; set; } = 0;

        /// <summary>Raw vector similarity.</summary>
        [JsonPropertyName("vector_score")]
        public double? VectorScore { get; set; } = null;

        /// <summary>Raw text score.</summary>
        [JsonPropertyName("text_score")]
        public double? TextScore { get; set; } = null;

        /// <summary>LLM rerank score.</summary>
        [JsonPropertyName("rerank_score")]
        public double? RerankScore { get; set; } = null;

        /// <summary>Multi-query fusion score.</summary>
        [JsonPropertyName("fusion_score")]
        public double? FusionScore { get; set; } = null;

        /// <summary>Chunk text.</summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; } = null;

        /// <summary>Chunk position in its document.</summary>
        [JsonPropertyName("position")]
        public int? Position { get; set; } = null;

        /// <summary>Neighbor chunks, when neighbor expansion is on.</summary>
        [JsonPropertyName("neighbors")]
        public List<RetrievedChunk>? Neighbors { get; set; } = null;

        /// <summary>
        /// The text chat would inject: the chunk merged with its neighbors in position order.
        /// </summary>
        [JsonIgnore]
        public string MergedContent
        {
            get
            {
                if (Neighbors == null || Neighbors.Count == 0) return Content ?? string.Empty;
                List<RetrievedChunk> all = Neighbors.Where(n => n.Content != null).ToList();
                all.Add(new RetrievedChunk { Content = Content, Position = Position });
                StringBuilder sb = new StringBuilder();
                foreach (RetrievedChunk chunk in all.OrderBy(c => c.Position ?? 0))
                {
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(chunk.Content);
                }

                return sb.ToString();
            }
        }

        #endregion
    }
}
