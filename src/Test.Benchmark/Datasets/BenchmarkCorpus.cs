namespace Test.Benchmark.Datasets
{
    using System.Collections.Generic;

    /// <summary>
    /// A corpus of documents and the labelled queries asked against it.
    /// </summary>
    public class BenchmarkCorpus
    {
        #region Public-Members

        /// <summary>
        /// Corpus id, unique within the dataset.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Documents.
        /// </summary>
        public List<BenchmarkDocument> Documents { get; set; } = new List<BenchmarkDocument>();

        /// <summary>
        /// Queries.
        /// </summary>
        public List<BenchmarkQuery> Queries { get; set; } = new List<BenchmarkQuery>();

        #endregion
    }
}
