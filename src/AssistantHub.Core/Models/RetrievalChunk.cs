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
        /// Full-text relevance score component (null in vector-only mode).
        /// </summary>
        [JsonPropertyName("text_score")]
        public double? TextScore { get; set; }

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
                foreach (var chunk in all)
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append(chunk.Content);
                }

                return sb.ToString();
            }
        }
    }
}
