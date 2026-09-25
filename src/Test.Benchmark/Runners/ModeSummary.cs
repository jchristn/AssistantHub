namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;
    using Test.Benchmark.Metrics;

    /// <summary>
    /// Aggregate retrieval results for one assistant configuration.
    /// </summary>
    public class ModeSummary
    {
        #region Public-Members

        /// <summary>Configuration label.</summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>Full assistant configuration.</summary>
        public SortedDictionary<string, string> Assistant { get; set; } = new SortedDictionary<string, string>();

        /// <summary>Answerable queries scored.</summary>
        public int Queries { get; set; } = 0;

        /// <summary>Unanswerable queries run.</summary>
        public int NegativeQueries { get; set; } = 0;

        /// <summary>Failed requests.</summary>
        public int Errors { get; set; } = 0;

        /// <summary>Mean metric values with 95% bootstrap intervals.</summary>
        public Dictionary<string, ConfidenceInterval> Metrics { get; set; } = new Dictionary<string, ConfidenceInterval>();

        /// <summary>Mean metrics per query type (plus "count").</summary>
        public Dictionary<string, Dictionary<string, double>> ByType { get; set; } = new Dictionary<string, Dictionary<string, double>>();

        /// <summary>Mean metrics per category (plus "count").</summary>
        public Dictionary<string, Dictionary<string, double>> ByCategory { get; set; } = new Dictionary<string, Dictionary<string, double>>();

        /// <summary>Mean metrics per pipeline stage (plus "count").</summary>
        public Dictionary<string, Dictionary<string, double>> ByStage { get; set; } = new Dictionary<string, Dictionary<string, double>>();

        /// <summary>Score-separation statistics (AUROCs, means, empty-result rates).</summary>
        public Dictionary<string, double?> Separation { get; set; } = new Dictionary<string, double?>();

        /// <summary>Answerability classifier statistics, when the check ran.</summary>
        public Dictionary<string, double> Answerability { get; set; } = new Dictionary<string, double>();

        /// <summary>Rates of pipeline flags (fallbacks and parse failures) over all queries.</summary>
        public Dictionary<string, double> FlagRates { get; set; } = new Dictionary<string, double>();

        /// <summary>Mean filter precision over filter queries.</summary>
        public double? FilterPrecision { get; set; } = null;

        /// <summary>Client latency.</summary>
        public LatencyStats Latency { get; set; } = new LatencyStats();

        /// <summary>Mean server-reported stage durations.</summary>
        public Dictionary<string, double> ServerMs { get; set; } = new Dictionary<string, double>();

        /// <summary>Server stage breakdown from the metrics endpoint.</summary>
        public Dictionary<string, StageBreakdown> Stages { get; set; } = new Dictionary<string, StageBreakdown>();

        #endregion
    }
}
