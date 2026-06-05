namespace AssistantHub.Sdk.Models
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
}
