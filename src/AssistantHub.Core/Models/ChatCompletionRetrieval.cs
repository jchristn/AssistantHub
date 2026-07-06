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
        /// Classified user-query type for this turn, when available.
        /// </summary>
        [JsonPropertyName("query_class")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string QueryClass { get; set; } = null;

        /// <summary>
        /// Answerability decision made after retrieval and before final generation.
        /// </summary>
        [JsonPropertyName("answerability_decision")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string AnswerabilityDecision { get; set; } = null;

        /// <summary>
        /// Safe reason for the answerability decision.
        /// </summary>
        [JsonPropertyName("answerability_reason")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string AnswerabilityReason { get; set; } = null;

        /// <summary>
        /// Number of retrieval candidates dropped after initial retrieval.
        /// </summary>
        [JsonPropertyName("dropped_candidate_count")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? DroppedCandidateCount { get; set; } = null;

        /// <summary>
        /// Safe summary of retrieval candidates dropped after initial retrieval.
        /// </summary>
        [JsonPropertyName("dropped_candidates")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RetrievalCandidateDropSummary> DroppedCandidates { get; set; } = null;

        /// <summary>
        /// Number of citation references extracted from the final answer.
        /// </summary>
        [JsonPropertyName("final_citation_count")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? FinalCitationCount { get; set; } = null;

        /// <summary>
        /// Attached document identifiers used to constrain retrieval.
        /// </summary>
        [JsonPropertyName("attached_document_ids")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> AttachedDocumentIds { get; set; } = null;

        /// <summary>
        /// Safe metadata for attached documents used to constrain retrieval.
        /// </summary>
        [JsonPropertyName("attached_documents")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<AssistantDocumentSelectionItem> AttachedDocuments { get; set; } = null;

        /// <summary>
        /// Indicates whether retrieval was constrained by attached document identifiers.
        /// </summary>
        [JsonPropertyName("document_filter_applied")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DocumentFilterApplied { get; set; } = false;

        /// <summary>
        /// The retrieved context chunks with source identification.
        /// </summary>
        [JsonPropertyName("chunks")]
        public List<RetrievalChunk> Chunks { get; set; } = null;
    }
}
