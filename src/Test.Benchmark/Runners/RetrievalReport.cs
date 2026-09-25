namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;

    /// <summary>
    /// A retrieval benchmark report.
    /// </summary>
    public class RetrievalReport
    {
        #region Public-Members

        /// <summary>Report kind.</summary>
        public string Kind { get; set; } = "retrieval";

        /// <summary>Dataset name.</summary>
        public string Dataset { get; set; } = string.Empty;

        /// <summary>Dataset description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Environment.</summary>
        public BenchmarkEnvironment Environment { get; set; } = new BenchmarkEnvironment();

        /// <summary>Run configuration.</summary>
        public Dictionary<string, string> Config { get; set; } = new Dictionary<string, string>();

        /// <summary>Ingest statistics.</summary>
        public IngestSummary Ingest { get; set; } = new IngestSummary();

        /// <summary>Evidence reachability, when measured.</summary>
        public ReachabilitySummary? Reachability { get; set; } = null;

        /// <summary>Per-configuration summaries.</summary>
        public List<ModeSummary> Modes { get; set; } = new List<ModeSummary>();

        /// <summary>Per-query outcomes.</summary>
        public List<QueryOutcome> Outcomes { get; set; } = new List<QueryOutcome>();

        #endregion
    }
}
