namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Outcome of the shared retrieval stages of the chat rail (gate, rewrite, search, fusion, attachment filter,
    /// rerank). Chat and the retrieval-only route both consume it, so the benchmark measures what users get.
    /// </summary>
    public class AssistantRetrievalStagesResult
    {
        /// <summary>
        /// Whether retrieval should run for this turn (false when the gate returned SKIP).
        /// </summary>
        public bool ShouldRetrieve { get; set; } = true;

        /// <summary>
        /// Retrieval gate decision, null when the gate did not run.
        /// </summary>
        public string GateDecision { get; set; } = null;

        /// <summary>
        /// Retrieval gate duration in milliseconds.
        /// </summary>
        public double GateDurationMs { get; set; } = 0;

        /// <summary>
        /// Retrieval gate model telemetry.
        /// </summary>
        public AssistantPerformanceStage GateTelemetry { get; set; } = null;

        /// <summary>
        /// Raw query rewrite output, null when rewrite did not run or failed.
        /// </summary>
        public string QueryRewriteResult { get; set; } = null;

        /// <summary>
        /// Query rewrite duration in milliseconds.
        /// </summary>
        public double QueryRewriteDurationMs { get; set; } = 0;

        /// <summary>
        /// Query rewrite model telemetry.
        /// </summary>
        public AssistantPerformanceStage QueryRewriteTelemetry { get; set; } = null;

        /// <summary>
        /// Queries issued to the store.
        /// </summary>
        public List<string> Queries { get; set; } = new List<string>();

        /// <summary>
        /// Final ranked chunks after every stage.
        /// </summary>
        public List<RetrievalChunk> Chunks { get; set; } = new List<RetrievalChunk>();

        /// <summary>
        /// When retrieval started, null when it did not run.
        /// </summary>
        public DateTime? RetrievalStartUtc { get; set; } = null;

        /// <summary>
        /// Search, fusion and attachment-filter duration in milliseconds.
        /// </summary>
        public double RetrievalDurationMs { get; set; } = 0;

        /// <summary>
        /// Whether any hybrid search fell back to vector-only.
        /// </summary>
        public bool HybridFallbackRan { get; set; } = false;

        /// <summary>
        /// Whether any query embedding failed after retries, so vector or hybrid search returned nothing.
        /// </summary>
        public bool EmbeddingFailed { get; set; } = false;

        /// <summary>
        /// Candidates dropped per stage.
        /// </summary>
        public List<RetrievalCandidateDropSummary> DroppedCandidates { get; set; } = new List<RetrievalCandidateDropSummary>();

        /// <summary>
        /// Re-rank duration in milliseconds.
        /// </summary>
        public double RerankDurationMs { get; set; } = 0;

        /// <summary>
        /// Chunks sent to the re-ranker.
        /// </summary>
        public int RerankInputCount { get; set; } = 0;

        /// <summary>
        /// Chunks kept by the re-ranker.
        /// </summary>
        public int RerankOutputCount { get; set; } = 0;

        /// <summary>
        /// Re-rank model telemetry.
        /// </summary>
        public AssistantPerformanceStage RerankTelemetry { get; set; } = null;

        /// <summary>
        /// Whether the re-ranker ran but its reply could not be parsed.
        /// </summary>
        public bool RerankParseFailed { get; set; } = false;

        /// <summary>
        /// Per-stage ranked lists, populated only when capture was requested.
        /// </summary>
        public List<RetrievalStageSnapshot> Stages { get; set; } = null;
    }
}
