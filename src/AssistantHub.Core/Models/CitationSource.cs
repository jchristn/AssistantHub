namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A single source document in the citation manifest.
    /// </summary>
    public class CitationSource
    {
        /// <summary>
        /// 1-based index matching the bracket notation [N] used in the response text.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        /// <summary>
        /// The document identifier (maps to AssistantDocument.Id).
        /// </summary>
        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// Display name of the source document.
        /// </summary>
        [JsonPropertyName("document_name")]
        public string DocumentName { get; set; } = null;

        /// <summary>
        /// MIME content type of the source document.
        /// </summary>
        [JsonPropertyName("content_type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ContentType { get; set; } = null;

        /// <summary>
        /// Retrieval relevance score (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("score")]
        public double Score { get; set; } = 0;

        /// <summary>
        /// Reciprocal Rank Fusion score, null when RRF is disabled.
        /// </summary>
        [JsonPropertyName("fusion_score")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? FusionScore { get; set; } = null;

        /// <summary>
        /// LLM-assigned re-rank relevance score (0–10), null when re-ranking is disabled.
        /// </summary>
        [JsonPropertyName("rerank_score")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? RerankScore { get; set; } = null;

        /// <summary>
        /// Text excerpt from the retrieved chunk.
        /// </summary>
        [JsonPropertyName("excerpt")]
        public string Excerpt { get; set; } = null;

        /// <summary>
        /// Download URL for the source document.
        /// Populated based on CitationLinkMode: null for "None",
        /// relative path for "Authenticated", unauthenticated server-proxied path for "Public".
        /// </summary>
        [JsonPropertyName("download_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string DownloadUrl { get; set; } = null;
    }
}
