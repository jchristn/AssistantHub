namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;

    /// <summary>
    /// An ingest benchmark report: pipeline throughput and failures per content type, and evidence reachability.
    /// </summary>
    public class IngestReport
    {
        #region Public-Members

        /// <summary>Report kind.</summary>
        public string Kind { get; set; } = "ingest";

        /// <summary>Dataset name.</summary>
        public string Dataset { get; set; } = string.Empty;

        /// <summary>Environment.</summary>
        public BenchmarkEnvironment Environment { get; set; } = new BenchmarkEnvironment();

        /// <summary>Run configuration.</summary>
        public Dictionary<string, string> Config { get; set; } = new Dictionary<string, string>();

        /// <summary>Ingest statistics.</summary>
        public IngestSummary Ingest { get; set; } = new IngestSummary();

        /// <summary>Per content type: documents, failures, mean chunks, mean upload-to-complete ms.</summary>
        public Dictionary<string, Dictionary<string, double>> ByContentType { get; set; } = new Dictionary<string, Dictionary<string, double>>();

        /// <summary>Evidence reachability.</summary>
        public ReachabilitySummary? Reachability { get; set; } = null;

        #endregion
    }
}
