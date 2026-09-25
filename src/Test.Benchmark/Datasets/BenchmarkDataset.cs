namespace Test.Benchmark.Datasets
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A benchmark dataset: one or more corpora, each ingested into its own AssistantHub collection. The format is
    /// described in benchmarks/datasets/SCHEMA.md.
    /// </summary>
    public class BenchmarkDataset
    {
        #region Public-Members

        /// <summary>
        /// Dataset name (used in collection and report names).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Corpora.
        /// </summary>
        public List<BenchmarkCorpus> Corpora { get; set; } = new List<BenchmarkCorpus>();

        /// <summary>
        /// Directory of the dataset file, used to resolve document <c>file</c> paths. Set by the loader.
        /// </summary>
        [JsonIgnore]
        public string BaseDirectory { get; set; } = string.Empty;

        /// <summary>
        /// SHA-256 of the dataset file, recorded in reports. Set by the loader.
        /// </summary>
        [JsonIgnore]
        public string FileHash { get; set; } = string.Empty;

        #endregion
    }
}
