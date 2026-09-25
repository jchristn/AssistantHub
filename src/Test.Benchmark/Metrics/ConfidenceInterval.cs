namespace Test.Benchmark.Metrics
{
    /// <summary>
    /// A mean with a 95% bootstrap confidence interval.
    /// </summary>
    public class ConfidenceInterval
    {
        #region Public-Members

        /// <summary>Mean.</summary>
        public double Mean { get; set; } = 0.0;

        /// <summary>Lower bound (2.5th percentile).</summary>
        public double Low { get; set; } = 0.0;

        /// <summary>Upper bound (97.5th percentile).</summary>
        public double High { get; set; } = 0.0;

        /// <summary>Sample size.</summary>
        public int N { get; set; } = 0;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Render as "mean [low, high]".
        /// </summary>
        /// <returns>Text.</returns>
        public override string ToString()
        {
            return Mean.ToString("F3") + " [" + Low.ToString("F3") + ", " + High.ToString("F3") + "]";
        }

        #endregion
    }
}
