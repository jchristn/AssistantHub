namespace AssistantHub.Core.Models
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
        public string Id { get; set; } = null;

        /// <summary>
        /// Object type: "chat.completion" or "chat.completion.chunk".
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "chat.completion";

        /// <summary>
        /// Unix timestamp of when the completion was created.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; } = 0;

        /// <summary>
        /// Model used for the completion.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null;

        /// <summary>
        /// List of completion choices.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice> Choices { get; set; } = new List<ChatCompletionChoice>();

        /// <summary>
        /// Token usage information (non-streaming only).
        /// </summary>
        [JsonPropertyName("usage")]
        public ChatCompletionUsage Usage { get; set; } = null;

        /// <summary>
        /// Optional status message (extension for compaction notifications).
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = null;

        /// <summary>
        /// Retrieval telemetry (extension for RAG diagnostics).
        /// </summary>
        [JsonPropertyName("retrieval")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ChatCompletionRetrieval Retrieval { get; set; } = null;

        /// <summary>
        /// Citation metadata linking response claims to source documents.
        /// Populated only when EnableCitations is true and RAG is active.
        /// </summary>
        [JsonPropertyName("citations")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ChatCompletionCitations Citations { get; set; } = null;
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
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Duration of the retrieval operation in milliseconds.
        /// </summary>
        [JsonPropertyName("duration_ms")]
        public double DurationMs { get; set; } = 0;

        /// <summary>
        /// Number of context chunks retrieved (after score filtering).
        /// </summary>
        [JsonPropertyName("chunks_returned")]
        public int ChunksReturned { get; set; } = 0;

        /// <summary>
        /// The retrieved context chunks with source identification.
        /// </summary>
        [JsonPropertyName("chunks")]
        public List<RetrievalChunk> Chunks { get; set; } = null;
    }

    /// <summary>
    /// Citation metadata included in chat completion responses.
    /// Contains a manifest of all source documents provided as context
    /// and the indices the model actually referenced in its answer.
    /// </summary>
    public class ChatCompletionCitations
    {
        /// <summary>
        /// Source documents provided as context to the model, indexed starting at 1.
        /// </summary>
        [JsonPropertyName("sources")]
        public List<CitationSource> Sources { get; set; } = new List<CitationSource>();

        /// <summary>
        /// Indices from Sources that the model actually cited in its response.
        /// Validated against the source manifest (invalid indices are excluded).
        /// When the model does not cite any sources, all source indices are
        /// populated as a fallback and AutoPopulated is set to true.
        /// </summary>
        [JsonPropertyName("referenced_indices")]
        public List<int> ReferencedIndices { get; set; } = new List<int>();

        /// <summary>
        /// True when the model did not produce inline citation markers and the
        /// system automatically populated ReferencedIndices with all source indices.
        /// Useful for diagnosing models that ignore citation instructions.
        /// </summary>
        [JsonPropertyName("auto_populated")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool AutoPopulated { get; set; } = false;
    }

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
