namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;

    /// <summary>
    /// A load benchmark report.
    /// </summary>
    public class LoadReport
    {
        #region Public-Members

        /// <summary>Report kind.</summary>
        public string Kind { get; set; } = "load";

        /// <summary>Scenario: retrieve, chat or mixed.</summary>
        public string Scenario { get; set; } = "retrieve";

        /// <summary>Dataset the workload queries come from.</summary>
        public string Dataset { get; set; } = string.Empty;

        /// <summary>Environment.</summary>
        public BenchmarkEnvironment Environment { get; set; } = new BenchmarkEnvironment();

        /// <summary>Run configuration.</summary>
        public Dictionary<string, string> Config { get; set; } = new Dictionary<string, string>();

        /// <summary>Preload ingest.</summary>
        public IngestSummary Ingest { get; set; } = new IngestSummary();

        /// <summary>Levels.</summary>
        public List<LoadLevel> Levels { get; set; } = new List<LoadLevel>();

        #endregion
    }
}
