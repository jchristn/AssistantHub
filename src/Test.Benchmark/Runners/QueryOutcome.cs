namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;

    /// <summary>
    /// The result of one query against one assistant configuration.
    /// </summary>
    public class QueryOutcome
    {
        #region Public-Members

        /// <summary>Corpus id.</summary>
        public string Corpus { get; set; } = string.Empty;

        /// <summary>Query id.</summary>
        public string QueryId { get; set; } = string.Empty;

        /// <summary>Query type.</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Query category.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Configuration label (search mode or sweep value).</summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>HTTP status.</summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>Error description on failure.</summary>
        public string? Error { get; set; } = null;

        /// <summary>Client latency.</summary>
        public double LatencyMs { get; set; } = 0;

        /// <summary>Server-reported stage durations (gate, rewrite, retrieval, rerank, answerability, total).</summary>
        public Dictionary<string, double> ServerMs { get; set; } = new Dictionary<string, double>();

        /// <summary>Ranked dataset document ids of the final chunks (first occurrence).</summary>
        public List<string> Ranked { get; set; } = new List<string>();

        /// <summary>Relevant dataset document ids.</summary>
        public List<string> Relevant { get; set; } = new List<string>();

        /// <summary>Final chunks returned.</summary>
        public int ChunksReturned { get; set; } = 0;

        /// <summary>Metric name to value (answerable queries only).</summary>
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

        /// <summary>Per-stage metrics: stage name to (metric to value).</summary>
        public Dictionary<string, Dictionary<string, double>> StageMetrics { get; set; } = new Dictionary<string, Dictionary<string, double>>();

        /// <summary>Top final score (0 with no chunks).</summary>
        public double TopScore { get; set; } = 0;

        /// <summary>Highest raw vector similarity among final chunks.</summary>
        public double TopVectorScore { get; set; } = 0;

        /// <summary>Highest rerank score among rerank inputs, when rerank ran.</summary>
        public double? TopRerankScore { get; set; } = null;

        /// <summary>Share of returned chunks from documents that satisfy the query's filter (filter queries only).</summary>
        public double? FilterPrecision { get; set; } = null;

        /// <summary>Gate decision.</summary>
        public string? GateDecision { get; set; } = null;

        /// <summary>Answerability decision.</summary>
        public string? AnswerabilityDecision { get; set; } = null;

        /// <summary>Queries issued.</summary>
        public int QueryCount { get; set; } = 0;

        /// <summary>Flags: hybridFallback, rerankParseFailed, answerabilityParseFailed.</summary>
        public List<string> Flags { get; set; } = new List<string>();

        #endregion
    }
}
