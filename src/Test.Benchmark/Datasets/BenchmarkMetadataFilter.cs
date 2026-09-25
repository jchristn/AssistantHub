namespace Test.Benchmark.Datasets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A metadata filter in AssistantHub's <c>metadata_filter</c> shape. The harness also evaluates it against dataset
    /// documents to score filter precision.
    /// </summary>
    public class BenchmarkMetadataFilter
    {
        #region Public-Members

        /// <summary>
        /// Labels every matching document must carry.
        /// </summary>
        [JsonPropertyName("required_labels")]
        public List<string>? RequiredLabels { get; set; } = null;

        /// <summary>
        /// Labels no matching document may carry.
        /// </summary>
        [JsonPropertyName("excluded_labels")]
        public List<string>? ExcludedLabels { get; set; } = null;

        /// <summary>
        /// Tag conditions every matching document must satisfy.
        /// </summary>
        [JsonPropertyName("required_tags")]
        public List<BenchmarkTagCondition>? RequiredTags { get; set; } = null;

        /// <summary>
        /// Tag conditions no matching document may satisfy.
        /// </summary>
        [JsonPropertyName("excluded_tags")]
        public List<BenchmarkTagCondition>? ExcludedTags { get; set; } = null;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Whether a document satisfies the filter.
        /// </summary>
        /// <param name="document">Dataset document.</param>
        /// <returns>True when it matches.</returns>
        public bool Matches(BenchmarkDocument document)
        {
            HashSet<string> labels = new HashSet<string>(document.Labels ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> tags = document.Tags ?? new Dictionary<string, string>();

            if (RequiredLabels != null && RequiredLabels.Any(l => !labels.Contains(l))) return false;
            if (ExcludedLabels != null && ExcludedLabels.Any(l => labels.Contains(l))) return false;
            if (RequiredTags != null && RequiredTags.Any(c => !c.Matches(tags))) return false;
            if (ExcludedTags != null && ExcludedTags.Any(c => c.Matches(tags))) return false;
            return true;
        }

        #endregion
    }
}
