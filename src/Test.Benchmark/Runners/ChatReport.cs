namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;
    using Test.Benchmark.Metrics;

    /// <summary>
    /// A chat (end-to-end RAG) benchmark report.
    /// </summary>
    public class ChatReport
    {
        #region Public-Members

        /// <summary>Report kind.</summary>
        public string Kind { get; set; } = "chat";

        /// <summary>Dataset name.</summary>
        public string Dataset { get; set; } = string.Empty;

        /// <summary>Environment.</summary>
        public BenchmarkEnvironment Environment { get; set; } = new BenchmarkEnvironment();

        /// <summary>Run configuration.</summary>
        public Dictionary<string, string> Config { get; set; } = new Dictionary<string, string>();

        /// <summary>Assistant configuration.</summary>
        public SortedDictionary<string, string> Assistant { get; set; } = new SortedDictionary<string, string>();

        /// <summary>Ingest statistics.</summary>
        public IngestSummary Ingest { get; set; } = new IngestSummary();

        /// <summary>Summary metrics (first repeat, or all repeats pooled where stated).</summary>
        public Dictionary<string, double> Summary { get; set; } = new Dictionary<string, double>();

        /// <summary>Confidence intervals of the headline rates.</summary>
        public Dictionary<string, ConfidenceInterval> Intervals { get; set; } = new Dictionary<string, ConfidenceInterval>();

        /// <summary>Accuracy of each repeat.</summary>
        public List<double> RepeatAccuracy { get; set; } = new List<double>();

        /// <summary>Summary per query type.</summary>
        public Dictionary<string, Dictionary<string, double>> ByType { get; set; } = new Dictionary<string, Dictionary<string, double>>();

        /// <summary>Summary per category.</summary>
        public Dictionary<string, Dictionary<string, double>> ByCategory { get; set; } = new Dictionary<string, Dictionary<string, double>>();

        /// <summary>Judge agreement with hand labels, when provided.</summary>
        public Dictionary<string, double>? JudgeAgreement { get; set; } = null;

        /// <summary>Client latency.</summary>
        public LatencyStats Latency { get; set; } = new LatencyStats();

        /// <summary>Server stage breakdown.</summary>
        public Dictionary<string, StageBreakdown> Stages { get; set; } = new Dictionary<string, StageBreakdown>();

        /// <summary>Items.</summary>
        public List<ChatItem> Items { get; set; } = new List<ChatItem>();

        #endregion
    }
}
