namespace Test.Benchmark.Metrics
{
    /// <summary>
    /// Server-side time spent in one operation during a benchmark phase.
    /// </summary>
    public class StageBreakdown
    {
        #region Public-Members

        /// <summary>Calls.</summary>
        public long Count { get; set; } = 0;

        /// <summary>Mean milliseconds per call.</summary>
        public double MeanMs { get; set; } = 0.0;

        /// <summary>Total milliseconds.</summary>
        public double TotalMs { get; set; } = 0.0;

        #endregion
    }
}
