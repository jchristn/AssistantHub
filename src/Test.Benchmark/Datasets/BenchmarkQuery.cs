namespace Test.Benchmark.Datasets
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A labelled query. An empty <see cref="Relevant"/> list marks a question the corpus cannot answer.
    /// </summary>
    public class BenchmarkQuery
    {
        #region Public-Members

        /// <summary>
        /// Gold answer marking an unanswerable question. <c>NOT_IN_MEMORY</c> is also accepted.
        /// </summary>
        public const string NotInCorpus = "NOT_IN_CORPUS";

        /// <summary>
        /// Query id, unique within the corpus.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Query text (the latest user turn).
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Retrieval difficulty type.
        /// </summary>
        public string Type { get; set; } = "default";

        /// <summary>
        /// Question category (EvalFact.RecommendedCategories).
        /// </summary>
        public string Category { get; set; } = "factual_lookup";

        /// <summary>
        /// Relevant document ids, primary first.
        /// </summary>
        public List<string> Relevant { get; set; } = new List<string>();

        /// <summary>
        /// Optional graded relevance (document id to gain). Gain 0 marks a known non-answer (e.g. a superseded version).
        /// </summary>
        public Dictionary<string, int>? Grades { get; set; } = null;

        /// <summary>
        /// Verbatim evidence passages.
        /// </summary>
        public List<string>? Evidence { get; set; } = null;

        /// <summary>
        /// Gold answer.
        /// </summary>
        public string? Answer { get; set; } = null;

        /// <summary>
        /// Metadata filter, sent as the chat/retrieve <c>metadata_filter</c>.
        /// </summary>
        public BenchmarkMetadataFilter? MetadataFilter { get; set; } = null;

        /// <summary>
        /// Attached document ids (dataset ids), sent as <c>attached_document_ids</c> after mapping.
        /// </summary>
        public List<string>? AttachedDocuments { get; set; } = null;

        /// <summary>
        /// Prior conversation turns.
        /// </summary>
        public List<BenchmarkTurn>? Conversation { get; set; } = null;

        /// <summary>
        /// Date the question is asked on.
        /// </summary>
        public string? Date { get; set; } = null;

        /// <summary>
        /// True when the corpus contains the answer.
        /// </summary>
        [JsonIgnore]
        public bool Answerable
        {
            get
            {
                return Relevant != null && Relevant.Count > 0;
            }
        }

        /// <summary>
        /// True when the gold answer marks the question unanswerable.
        /// </summary>
        [JsonIgnore]
        public bool GoldIsNotInCorpus
        {
            get
            {
                return string.Equals(Answer, NotInCorpus, StringComparison.Ordinal)
                    || string.Equals(Answer, "NOT_IN_MEMORY", StringComparison.Ordinal);
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Gain of a document for nDCG: the graded value when present, 1 for a relevant document, otherwise 0.
        /// </summary>
        /// <param name="documentId">Dataset document id.</param>
        /// <returns>Gain.</returns>
        public int GainOf(string documentId)
        {
            if (Grades != null && Grades.TryGetValue(documentId, out int grade)) return Math.Max(0, grade);
            return Relevant.Contains(documentId) ? 1 : 0;
        }

        #endregion
    }
}
