namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Response body for <c>POST /v1.0/assistants/{assistantId}/retrieve</c>.
    /// </summary>
    public class AssistantRetrieveResponse
    {
        /// <summary>
        /// Assistant identifier.
        /// </summary>
        [JsonPropertyName("assistant_id")]
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Collection searched.
        /// </summary>
        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Search mode from the assistant settings.
        /// </summary>
        [JsonPropertyName("search_mode")]
        public string SearchMode { get; set; } = null;

        /// <summary>
        /// Whether retrieval ran. False when RAG is disabled, no collection is configured, or the gate skipped it.
        /// </summary>
        [JsonPropertyName("retrieved")]
        public bool Retrieved { get; set; } = false;

        /// <summary>
        /// Retrieval gate decision (<c>RETRIEVE</c> or <c>SKIP</c>), null when the gate did not run.
        /// </summary>
        [JsonPropertyName("gate_decision")]
        public string GateDecision { get; set; } = null;

        /// <summary>
        /// Retrieval gate duration in milliseconds.
        /// </summary>
        [JsonPropertyName("gate_duration_ms")]
        public double GateDurationMs { get; set; } = 0;

        /// <summary>
        /// Queries issued to the store (the user message, or the rewritten alternates).
        /// </summary>
        [JsonPropertyName("queries")]
        public List<string> Queries { get; set; } = new List<string>();

        /// <summary>
        /// Query rewrite duration in milliseconds.
        /// </summary>
        [JsonPropertyName("query_rewrite_duration_ms")]
        public double QueryRewriteDurationMs { get; set; } = 0;

        /// <summary>
        /// Search and fusion duration in milliseconds.
        /// </summary>
        [JsonPropertyName("retrieval_duration_ms")]
        public double RetrievalDurationMs { get; set; } = 0;

        /// <summary>
        /// Whether a hybrid search returned nothing and was retried as vector-only.
        /// </summary>
        [JsonPropertyName("hybrid_fallback_ran")]
        public bool HybridFallbackRan { get; set; } = false;

        /// <summary>
        /// Whether the query embedding failed after retries. Vector search then returns nothing; hybrid search falls
        /// back to its full-text leg.
        /// </summary>
        [JsonPropertyName("embedding_failed")]
        public bool EmbeddingFailed { get; set; } = false;

        /// <summary>
        /// Whether a hybrid search ran its full-text leg alone because the query embedding failed.
        /// </summary>
        [JsonPropertyName("keyword_fallback_ran")]
        public bool KeywordFallbackRan { get; set; } = false;

        /// <summary>
        /// Standalone rewrite of a follow-up question, searched alongside the original message, when the conversation
        /// rewrite ran.
        /// </summary>
        [JsonPropertyName("conversation_rewrite")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ConversationRewrite { get; set; } = null;

        /// <summary>
        /// Reranker that ran: "llm" or "cross_encoder"; null when reranking is off.
        /// </summary>
        [JsonPropertyName("reranker")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Reranker { get; set; } = null;

        /// <summary>
        /// Whether reranking was skipped because its circuit breaker was open or no cross-encoder is configured.
        /// </summary>
        [JsonPropertyName("rerank_skipped")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool RerankSkipped { get; set; } = false;

        /// <summary>
        /// Whether the cross-encoder scored every candidate below RerankMinScore, so no context was injected.
        /// </summary>
        [JsonPropertyName("no_relevant_context")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool NoRelevantContext { get; set; } = false;

        /// <summary>
        /// Number of retrieved chunks that came from superseded documents.
        /// </summary>
        [JsonPropertyName("superseded_chunks")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int SupersededChunks { get; set; } = 0;

        /// <summary>
        /// Re-rank duration in milliseconds.
        /// </summary>
        [JsonPropertyName("rerank_duration_ms")]
        public double RerankDurationMs { get; set; } = 0;

        /// <summary>
        /// Chunks sent to the re-ranker.
        /// </summary>
        [JsonPropertyName("rerank_input_count")]
        public int RerankInputCount { get; set; } = 0;

        /// <summary>
        /// Chunks kept by the re-ranker.
        /// </summary>
        [JsonPropertyName("rerank_output_count")]
        public int RerankOutputCount { get; set; } = 0;

        /// <summary>
        /// Whether the re-ranker ran but its reply could not be parsed.
        /// </summary>
        [JsonPropertyName("rerank_parse_failed")]
        public bool RerankParseFailed { get; set; } = false;

        /// <summary>
        /// Answerability decision, or <c>not_checked</c>.
        /// </summary>
        [JsonPropertyName("answerability_decision")]
        public string AnswerabilityDecision { get; set; } = "not_checked";

        /// <summary>
        /// Query class from the answerability check.
        /// </summary>
        [JsonPropertyName("query_class")]
        public string QueryClass { get; set; } = null;

        /// <summary>
        /// Answerability reason.
        /// </summary>
        [JsonPropertyName("answerability_reason")]
        public string AnswerabilityReason { get; set; } = null;

        /// <summary>
        /// Whether the answerability check ran but its reply could not be parsed.
        /// </summary>
        [JsonPropertyName("answerability_parse_failed")]
        public bool AnswerabilityParseFailed { get; set; } = false;

        /// <summary>
        /// Answerability check duration in milliseconds.
        /// </summary>
        [JsonPropertyName("answerability_duration_ms")]
        public double AnswerabilityDurationMs { get; set; } = 0;

        /// <summary>
        /// Candidates dropped per stage.
        /// </summary>
        [JsonPropertyName("dropped_candidates")]
        public List<RetrievalCandidateDropSummary> DroppedCandidates { get; set; } = new List<RetrievalCandidateDropSummary>();

        /// <summary>
        /// Final ranked chunks, as chat would inject them before prompt-budget trimming.
        /// </summary>
        [JsonPropertyName("chunks")]
        public List<RetrievalChunk> Chunks { get; set; } = new List<RetrievalChunk>();

        /// <summary>
        /// Per-stage ranked lists, when requested.
        /// </summary>
        [JsonPropertyName("stages")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RetrievalStageSnapshot> Stages { get; set; } = null;

        /// <summary>
        /// Total server-side duration in milliseconds.
        /// </summary>
        [JsonPropertyName("total_duration_ms")]
        public double TotalDurationMs { get; set; } = 0;
    }
}
