namespace Test.Benchmark.Runners
{
    using System;
    using System.Collections.Generic;
    using Test.Benchmark.Datasets;

    /// <summary>
    /// A corpus ingested into an AssistantHub collection, with the mapping between dataset and AssistantHub
    /// document ids.
    /// </summary>
    public class ProvisionedCollection
    {
        #region Public-Members

        /// <summary>Corpus.</summary>
        public BenchmarkCorpus Corpus { get; set; } = new BenchmarkCorpus();

        /// <summary>Collection name.</summary>
        public string CollectionName { get; set; } = string.Empty;

        /// <summary>Collection id.</summary>
        public string CollectionId { get; set; } = string.Empty;

        /// <summary>Ingestion rule id.</summary>
        public string IngestionRuleId { get; set; } = string.Empty;

        /// <summary>AssistantHub document id to dataset document id.</summary>
        public Dictionary<string, string> DatasetIdByDocumentId { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Dataset document id to AssistantHub document id.</summary>
        public Dictionary<string, string> DocumentIdByDatasetId { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

        #endregion

        #region Public-Methods

        /// <summary>
        /// Map an AssistantHub document id to its dataset id.
        /// </summary>
        /// <param name="documentId">AssistantHub document id.</param>
        /// <returns>Dataset id, or the input prefixed with "?" when unknown.</returns>
        public string ToDatasetId(string? documentId)
        {
            if (string.IsNullOrEmpty(documentId)) return "?";
            return DatasetIdByDocumentId.TryGetValue(documentId, out string? id) ? id : "?" + documentId;
        }

        #endregion
    }
}
