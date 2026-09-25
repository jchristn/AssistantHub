namespace Test.Benchmark
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Response of <c>POST /v1.0/assistants/{id}/retrieve</c> (the fields the harness uses).
    /// </summary>
    public class RetrieveResult
    {
        #region Public-Members

        /// <summary>Whether retrieval ran.</summary>
        [JsonPropertyName("retrieved")]
        public bool Retrieved { get; set; } = false;

        /// <summary>Gate decision.</summary>
        [JsonPropertyName("gate_decision")]
        public string? GateDecision { get; set; } = null;

        /// <summary>Queries issued.</summary>
        [JsonPropertyName("queries")]
        public List<string> Queries { get; set; } = new List<string>();

        /// <summary>Gate duration.</summary>
        [JsonPropertyName("gate_duration_ms")]
        public double GateDurationMs { get; set; } = 0;

        /// <summary>Rewrite duration.</summary>
        [JsonPropertyName("query_rewrite_duration_ms")]
        public double QueryRewriteDurationMs { get; set; } = 0;

        /// <summary>Search and fusion duration.</summary>
        [JsonPropertyName("retrieval_duration_ms")]
        public double RetrievalDurationMs { get; set; } = 0;

        /// <summary>Rerank duration.</summary>
        [JsonPropertyName("rerank_duration_ms")]
        public double RerankDurationMs { get; set; } = 0;

        /// <summary>Answerability duration.</summary>
        [JsonPropertyName("answerability_duration_ms")]
        public double AnswerabilityDurationMs { get; set; } = 0;

        /// <summary>Server total.</summary>
        [JsonPropertyName("total_duration_ms")]
        public double TotalDurationMs { get; set; } = 0;

        /// <summary>Hybrid fallback ran.</summary>
        [JsonPropertyName("hybrid_fallback_ran")]
        public bool HybridFallbackRan { get; set; } = false;

        /// <summary>Query embedding failed after retries.</summary>
        [JsonPropertyName("embedding_failed")]
        public bool EmbeddingFailed { get; set; } = false;

        /// <summary>Rerank inputs.</summary>
        [JsonPropertyName("rerank_input_count")]
        public int RerankInputCount { get; set; } = 0;

        /// <summary>Rerank produced no usable scores.</summary>
        [JsonPropertyName("rerank_parse_failed")]
        public bool RerankParseFailed { get; set; } = false;

        /// <summary>Answerability decision.</summary>
        [JsonPropertyName("answerability_decision")]
        public string? AnswerabilityDecision { get; set; } = null;

        /// <summary>Answerability produced no usable decision.</summary>
        [JsonPropertyName("answerability_parse_failed")]
        public bool AnswerabilityParseFailed { get; set; } = false;

        /// <summary>Final chunks.</summary>
        [JsonPropertyName("chunks")]
        public List<RetrievedChunk> Chunks { get; set; } = new List<RetrievedChunk>();

        /// <summary>Per-stage lists.</summary>
        [JsonPropertyName("stages")]
        public List<RetrieveStage>? Stages { get; set; } = null;

        #endregion
    }
}
