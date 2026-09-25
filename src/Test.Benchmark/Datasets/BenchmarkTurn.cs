namespace Test.Benchmark.Datasets
{
    /// <summary>
    /// A prior conversation turn.
    /// </summary>
    public class BenchmarkTurn
    {
        #region Public-Members

        /// <summary>
        /// Role: user or assistant.
        /// </summary>
        public string Role { get; set; } = "user";

        /// <summary>
        /// Content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        #endregion
    }
}
