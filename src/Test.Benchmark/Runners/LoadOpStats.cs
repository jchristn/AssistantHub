namespace Test.Benchmark.Runners
{
    using Test.Benchmark.Metrics;

    /// <summary>
    /// Statistics for one operation type at one concurrency level.
    /// </summary>
    public class LoadOpStats
    {
        #region Public-Members

        /// <summary>Operations completed.</summary>
        public int Count { get; set; } = 0;

        /// <summary>Failed operations.</summary>
        public int Errors { get; set; } = 0;

        /// <summary>Operations per second.</summary>
        public double Throughput { get; set; } = 0;

        /// <summary>Latency of successful operations.</summary>
        public LatencyStats Latency { get; set; } = new LatencyStats();

        #endregion
    }
}
