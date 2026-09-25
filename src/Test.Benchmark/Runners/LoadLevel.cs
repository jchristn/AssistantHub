namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;
    using Test.Benchmark.Metrics;

    /// <summary>
    /// Results at one concurrency level.
    /// </summary>
    public class LoadLevel
    {
        #region Public-Members

        /// <summary>Closed-loop workers.</summary>
        public int Concurrency { get; set; } = 0;

        /// <summary>Measured seconds.</summary>
        public double Seconds { get; set; } = 0;

        /// <summary>Operations per second, all operations.</summary>
        public double Throughput { get; set; } = 0;

        /// <summary>Failed operations share.</summary>
        public double ErrorRate { get; set; } = 0;

        /// <summary>Per-operation statistics.</summary>
        public Dictionary<string, LoadOpStats> Operations { get; set; } = new Dictionary<string, LoadOpStats>();

        /// <summary>Server stage breakdown.</summary>
        public Dictionary<string, StageBreakdown> Stages { get; set; } = new Dictionary<string, StageBreakdown>();

        /// <summary>Up to 5 errors.</summary>
        public List<string> SampleErrors { get; set; } = new List<string>();

        #endregion
    }
}
