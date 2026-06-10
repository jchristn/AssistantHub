namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A single source document in the citation manifest.
    /// </summary>
    public class CitationSource
    {
        /// <summary>
        /// 1-based index matching the bracket notation [N] in the response.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// Source category, for example document or web.
        /// </summary>
        [JsonPropertyName("source_type")]
        public string SourceType { get; set; }

        /// <summary>
        /// The document identifier.
        /// </summary>
        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; }

        /// <summary>
        /// Source URL for web evidence or document metadata when available.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// Display name of the source document.
        /// </summary>
        [JsonPropertyName("document_name")]
        public string DocumentName { get; set; }

        /// <summary>
        /// MIME content type of the source document.
        /// </summary>
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        /// <summary>
        /// Retrieval relevance score.
        /// </summary>
        [JsonPropertyName("score")]
        public double Score { get; set; }

        /// <summary>
        /// Reciprocal Rank Fusion score.
        /// </summary>
        [JsonPropertyName("fusion_score")]
        public double? FusionScore { get; set; }

        /// <summary>
        /// LLM-assigned re-rank relevance score.
        /// </summary>
        [JsonPropertyName("rerank_score")]
        public double? RerankScore { get; set; }

        /// <summary>
        /// Text excerpt from the retrieved chunk.
        /// </summary>
        [JsonPropertyName("excerpt")]
        public string Excerpt { get; set; }

        /// <summary>
        /// Download URL for the source document.
        /// </summary>
        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; }
    }
}
