namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible chat completion response.
    /// </summary>
    public class ChatCompletionResponse
    {
        /// <summary>
        /// Unique identifier for the completion.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Object type: "chat.completion" or "chat.completion.chunk".
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; }

        /// <summary>
        /// Unix timestamp of when the completion was created.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; }

        /// <summary>
        /// Model used for the completion.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// List of completion choices.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice> Choices { get; set; }

        /// <summary>
        /// Token usage information (non-streaming only).
        /// </summary>
        [JsonPropertyName("usage")]
        public ChatCompletionUsage Usage { get; set; }

        /// <summary>
        /// Optional status message.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Retrieval telemetry (extension for RAG diagnostics).
        /// </summary>
        [JsonPropertyName("retrieval")]
        public ChatCompletionRetrieval Retrieval { get; set; }

        /// <summary>
        /// Citation metadata linking response claims to source documents.
        /// </summary>
        [JsonPropertyName("citations")]
        public ChatCompletionCitations Citations { get; set; }
    }

    /// <summary>
    /// Retrieval telemetry included in chat completion responses.
    /// </summary>
    public class ChatCompletionRetrieval
    {
        /// <summary>
        /// The collection that was searched.
        /// </summary>
        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; }

        /// <summary>
        /// Duration of the retrieval operation in milliseconds.
        /// </summary>
        [JsonPropertyName("duration_ms")]
        public double DurationMs { get; set; }

        /// <summary>
        /// Number of context chunks retrieved.
        /// </summary>
        [JsonPropertyName("chunks_returned")]
        public int ChunksReturned { get; set; }

        /// <summary>
        /// Duration of the re-ranking LLM call in milliseconds.
        /// </summary>
        [JsonPropertyName("rerank_duration_ms")]
        public double RerankDurationMs { get; set; }

        /// <summary>
        /// Number of chunks sent to the re-ranker.
        /// </summary>
        [JsonPropertyName("rerank_input_count")]
        public int RerankInputCount { get; set; }

        /// <summary>
        /// Number of chunks that survived re-ranking.
        /// </summary>
        [JsonPropertyName("rerank_output_count")]
        public int RerankOutputCount { get; set; }

        /// <summary>
        /// The retrieved context chunks.
        /// </summary>
        [JsonPropertyName("chunks")]
        public List<RetrievalChunk> Chunks { get; set; }
    }

    /// <summary>
    /// Citation metadata included in chat completion responses.
    /// </summary>
    public class ChatCompletionCitations
    {
        /// <summary>
        /// Source documents provided as context to the model.
        /// </summary>
        [JsonPropertyName("sources")]
        public List<CitationSource> Sources { get; set; }

        /// <summary>
        /// Indices from Sources that the model actually cited.
        /// </summary>
        [JsonPropertyName("referenced_indices")]
        public List<int> ReferencedIndices { get; set; }

        /// <summary>
        /// True when the system automatically populated ReferencedIndices.
        /// </summary>
        [JsonPropertyName("auto_populated")]
        public bool AutoPopulated { get; set; }
    }

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
        /// The document identifier.
        /// </summary>
        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; }

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
