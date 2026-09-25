namespace Test.Benchmark.Datasets
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Test.Benchmark.Metrics;

    /// <summary>
    /// Checks a dataset before it is run: ids, relevance labels, evidence passages against the source text, negatives
    /// and filters. Evidence is matched against the raw source file (HTML tags stripped), so the check catches
    /// labelling mistakes independently of AssistantHub's extraction.
    /// </summary>
    public static class DatasetValidator
    {
        #region Public-Methods

        /// <summary>
        /// Validate a dataset.
        /// </summary>
        /// <param name="dataset">Dataset.</param>
        /// <returns>Errors and warnings (warnings are prefixed "warning:").</returns>
        public static List<string> Validate(BenchmarkDataset dataset)
        {
            List<string> problems = new List<string>();
            foreach (BenchmarkCorpus corpus in dataset.Corpora)
            {
                Dictionary<string, BenchmarkDocument> documents = corpus.Documents.ToDictionary(d => d.Id, StringComparer.Ordinal);
                Dictionary<string, string> text = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (BenchmarkDocument document in corpus.Documents)
                {
                    if (!string.IsNullOrEmpty(document.File))
                    {
                        string path = Path.Combine(dataset.BaseDirectory, document.File);
                        if (!File.Exists(path)) problems.Add(corpus.Id + "/" + document.Id + ": file not found " + document.File);
                        else if (IsText(path)) text[document.Id] = EvidenceMatcher.Normalize(File.ReadAllText(path));
                    }
                    else
                    {
                        text[document.Id] = EvidenceMatcher.Normalize((document.Title ?? string.Empty) + " " + (document.Body ?? string.Empty));
                    }

                    if (document.Supersedes != null && !documents.ContainsKey(document.Supersedes))
                        problems.Add(corpus.Id + "/" + document.Id + ": supersedes unknown document " + document.Supersedes);
                }

                HashSet<string> queryIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (BenchmarkQuery query in corpus.Queries)
                {
                    string where = corpus.Id + "/" + query.Id;
                    if (!queryIds.Add(query.Id)) problems.Add(where + ": duplicate query id");
                    if (string.IsNullOrWhiteSpace(query.Text)) problems.Add(where + ": empty text");
                    foreach (string id in query.Relevant.Concat(query.Grades?.Keys ?? Enumerable.Empty<string>()).Concat(query.AttachedDocuments ?? new List<string>()))
                    {
                        if (!documents.ContainsKey(id)) problems.Add(where + ": unknown document " + id);
                    }

                    if (!query.Answerable)
                    {
                        if (!query.GoldIsNotInCorpus && query.Answer != null) problems.Add(where + ": negative query should have answer " + BenchmarkQuery.NotInCorpus);
                        if (query.Evidence != null && query.Evidence.Count > 0) problems.Add(where + ": negative query has evidence");
                        continue;
                    }

                    if (query.Evidence == null || query.Evidence.Count == 0) problems.Add("warning: " + where + ": answerable query has no evidence");
                    foreach (string passage in query.Evidence ?? new List<string>())
                    {
                        string normalized = EvidenceMatcher.Normalize(passage);
                        bool checkable = query.Relevant.All(id => text.ContainsKey(id));
                        if (checkable && !query.Relevant.Any(id => text.TryGetValue(id, out string? t) && EvidenceMatcher.Contains(t, normalized)))
                            problems.Add(where + ": evidence not found in any relevant document: " + passage);
                    }

                    if (query.MetadataFilter != null)
                    {
                        foreach (string id in query.Relevant)
                        {
                            if (documents.TryGetValue(id, out BenchmarkDocument? d) && !query.MetadataFilter.Matches(d))
                                problems.Add(where + ": relevant document " + id + " does not pass the query's own filter");
                        }
                    }
                }
            }

            return problems;
        }

        #endregion

        #region Private-Methods

        private static bool IsText(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".md" || extension == ".txt" || extension == ".html" || extension == ".htm" || extension == ".json" || extension == ".csv";
        }

        #endregion
    }
}
