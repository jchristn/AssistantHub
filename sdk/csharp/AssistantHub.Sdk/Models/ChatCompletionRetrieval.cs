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
        /// Attached document identifiers used to constrain retrieval.
        /// </summary>
        [JsonPropertyName("attached_document_ids")]
        public List<string> AttachedDocumentIds { get; set; }

        /// <summary>
        /// Safe metadata for attached documents used to constrain retrieval.
        /// </summary>
        [JsonPropertyName("attached_documents")]
        public List<AssistantDocumentSelectionItem> AttachedDocuments { get; set; }

        /// <summary>
        /// Indicates whether retrieval was constrained by attached document identifiers.
        /// </summary>
        [JsonPropertyName("document_filter_applied")]
        public bool DocumentFilterApplied { get; set; }

        /// <summary>
        /// Query class assigned by the answerability classifier.
        /// </summary>
        [JsonPropertyName("query_class")]
        public string QueryClass { get; set; }

        /// <summary>
        /// Answerability classifier decision.
        /// </summary>
        [JsonPropertyName("answerability_decision")]
        public string AnswerabilityDecision { get; set; }

        /// <summary>
        /// Brief answerability classifier rationale.
        /// </summary>
        [JsonPropertyName("answerability_reason")]
        public string AnswerabilityReason { get; set; }

        /// <summary>
        /// Total number of retrieval candidates dropped by filtering, reranking, or prompt-budget trimming.
        /// </summary>
        [JsonPropertyName("dropped_candidate_count")]
        public int DroppedCandidateCount { get; set; }

        /// <summary>
        /// Aggregated dropped candidate counts by pipeline stage and reason.
        /// </summary>
        [JsonPropertyName("dropped_candidates")]
        public List<RetrievalCandidateDropSummary> DroppedCandidates { get; set; }

        /// <summary>
        /// Number of citation sources referenced in the final response.
        /// </summary>
        [JsonPropertyName("final_citation_count")]
        public int? FinalCitationCount { get; set; }

        /// <summary>
        /// Standalone rewrite of a follow-up question, searched alongside the original message, when the conversation rewrite ran.
        /// </summary>
        [JsonPropertyName("conversation_rewrite")]
        public string ConversationRewrite { get; set; }

        /// <summary>
        /// Reranker that ran: "llm" or "cross_encoder"; null when reranking is off.
        /// </summary>
        [JsonPropertyName("reranker")]
        public string Reranker { get; set; }

        /// <summary>
        /// Indicates whether reranking was skipped because its circuit breaker was open or no cross-encoder is configured.
        /// </summary>
        [JsonPropertyName("rerank_skipped")]
        public bool RerankSkipped { get; set; }

        /// <summary>
        /// Indicates whether the cross-encoder scored every candidate below RerankMinScore, so no context was injected.
        /// </summary>
        [JsonPropertyName("no_relevant_context")]
        public bool NoRelevantContext { get; set; }

        /// <summary>
        /// Number of retrieved chunks that came from superseded documents.
        /// </summary>
        [JsonPropertyName("superseded_chunks")]
        public int SupersededChunks { get; set; }

        /// <summary>
        /// Indicates whether a degenerate cited answer was regenerated without the citation instructions.
        /// </summary>
        [JsonPropertyName("answer_regenerated")]
        public bool AnswerRegenerated { get; set; }

        /// <summary>
        /// The retrieved context chunks.
        /// </summary>
        [JsonPropertyName("chunks")]
        public List<RetrievalChunk> Chunks { get; set; }
    }
}
