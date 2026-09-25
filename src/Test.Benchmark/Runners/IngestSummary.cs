namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;
    using Test.Benchmark.Metrics;

    /// <summary>
    /// Ingest statistics for a provisioning pass.
    /// </summary>
    public class IngestSummary
    {
        #region Public-Members

        /// <summary>Documents in the dataset.</summary>
        public int Documents { get; set; } = 0;

        /// <summary>Documents uploaded in this run.</summary>
        public int Uploaded { get; set; } = 0;

        /// <summary>Documents already ingested and reused.</summary>
        public int Reused { get; set; } = 0;

        /// <summary>Documents that ended Failed or timed out.</summary>
        public int Failures { get; set; } = 0;

        /// <summary>Wall-clock seconds for the uploads in this run.</summary>
        public double WallSeconds { get; set; } = 0;

        /// <summary>Uploaded documents per second.</summary>
        public double DocumentsPerSecond { get; set; } = 0;

        /// <summary>In-flight document limit.</summary>
        public int Concurrency { get; set; } = 0;

        /// <summary>Upload-to-Completed latency.</summary>
        public LatencyStats Latency { get; set; } = new LatencyStats();

        /// <summary>Server stage breakdown during the uploads.</summary>
        public Dictionary<string, StageBreakdown> Stages { get; set; } = new Dictionary<string, StageBreakdown>();

        /// <summary>Per-document outcomes for this run's uploads.</summary>
        public List<IngestDocumentOutcome> Outcomes { get; set; } = new List<IngestDocumentOutcome>();

        /// <summary>Up to 20 failure descriptions.</summary>
        public List<string> SampleErrors { get; set; } = new List<string>();

        #endregion
    }
}
