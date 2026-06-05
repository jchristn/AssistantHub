namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

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
        /// Duration of the re-ranking LLM call in milliseconds.
        /// </summary>
        [JsonPropertyName("rerank_duration_ms")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double RerankDurationMs { get; set; } = 0;

        /// <summary>
        /// Number of chunks sent to the re-ranker.
        /// </summary>
        [JsonPropertyName("rerank_input_count")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int RerankInputCount { get; set; } = 0;

        /// <summary>
        /// Number of chunks that survived re-ranking.
        /// </summary>
        [JsonPropertyName("rerank_output_count")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int RerankOutputCount { get; set; } = 0;

        /// <summary>
        /// The retrieved context chunks with source identification.
        /// </summary>
        [JsonPropertyName("chunks")]
        public List<RetrievalChunk> Chunks { get; set; } = null;
    }
}
