namespace Test.Benchmark.Datasets
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Converts MultiHop-RAG (Tang and Yang, 2024: corpus.json and MultiHopRAG.json) into the harness format. All 609
    /// news articles are ingested; queries are a stratified sample across the four question types (inference,
    /// comparison, temporal, null). Each query's evidence facts are verbatim sentences from its source articles, so
    /// they become evidence passages; null queries become unanswerable questions.
    /// </summary>
    public static class MultiHopRagConverter
    {
        #region Public-Methods

        /// <summary>
        /// Convert.
        /// </summary>
        /// <param name="directory">Directory with corpus.json and MultiHopRAG.json.</param>
        /// <param name="limit">Queries to sample (0 for all).</param>
        /// <param name="seed">Sampling seed.</param>
        /// <returns>Dataset.</returns>
        public static BenchmarkDataset Convert(string directory, int limit, int seed)
        {
            JsonArray articles = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "corpus.json"))) as JsonArray ?? throw new InvalidDataException("corpus.json is not an array.");
            JsonArray questions = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "MultiHopRAG.json"))) as JsonArray ?? throw new InvalidDataException("MultiHopRAG.json is not an array.");

            BenchmarkCorpus corpus = new BenchmarkCorpus { Id = "news" };
            Dictionary<string, string> idByTitle = new Dictionary<string, string>(StringComparer.Ordinal);
            int index = 0;
            foreach (JsonNode? article in articles)
            {
                if (article == null) continue;
                string title = article["title"]?.GetValue<string>() ?? string.Empty;
                string id = "mhr-" + (index++).ToString("D3");
                idByTitle[title] = id;
                string category = article["category"]?.GetValue<string>() ?? "news";
                string source = article["source"]?.GetValue<string>() ?? "unknown";
                string published = article["published_at"]?.GetValue<string>() ?? string.Empty;
                corpus.Documents.Add(new BenchmarkDocument
                {
                    Id = id,
                    Title = title,
                    Body = "Source: " + source + (published.Length >= 10 ? ". Published: " + published.Substring(0, 10) : string.Empty) + ".\n\n" + (article["body"]?.GetValue<string>() ?? string.Empty),
                    Labels = new List<string> { category },
                    Tags = new Dictionary<string, string> { ["source"] = source, ["category"] = category },
                    Date = published.Length >= 10 ? published.Substring(0, 10) : null
                });
            }

            List<BenchmarkQuery> all = new List<BenchmarkQuery>();
            int q = 0;
            foreach (JsonNode? question in questions)
            {
                if (question == null) continue;
                string type = question["question_type"]?.GetValue<string>() ?? "unknown";
                BenchmarkQuery query = new BenchmarkQuery
                {
                    Id = "mhr-q" + (q++).ToString("D4"),
                    Text = question["query"]?.GetValue<string>() ?? string.Empty,
                    Answer = question["answer"]?.GetValue<string>(),
                    Type = type switch
                    {
                        "inference_query" => "multi-inference",
                        "comparison_query" => "multi-comparison",
                        "temporal_query" => "multi-temporal",
                        "null_query" => "negative",
                        _ => type
                    },
                    Category = type switch
                    {
                        "temporal_query" => "temporal",
                        "null_query" => "unanswerable",
                        _ => "multi_hop"
                    }
                };

                if (type == "null_query")
                {
                    query.Answer = BenchmarkQuery.NotInCorpus;
                }
                else
                {
                    query.Evidence = new List<string>();
                    foreach (JsonNode? evidence in question["evidence_list"] as JsonArray ?? new JsonArray())
                    {
                        string title = evidence?["title"]?.GetValue<string>() ?? string.Empty;
                        if (idByTitle.TryGetValue(title, out string? id) && !query.Relevant.Contains(id)) query.Relevant.Add(id);
                        string? fact = evidence?["fact"]?.GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(fact)) query.Evidence.Add(fact);
                    }

                    if (query.Relevant.Count == 0) continue;
                }

                all.Add(query);
            }

            corpus.Queries = Sample(all, limit, seed);
            BenchmarkDataset dataset = new BenchmarkDataset
            {
                Name = "multihop-rag",
                Description = "MultiHop-RAG (Tang and Yang, 2024): " + corpus.Documents.Count + " news articles, " + corpus.Queries.Count + " of " + all.Count
                    + " queries (stratified by question type, seed " + seed + "). Evidence passages are the dataset's verbatim facts."
            };
            dataset.Corpora.Add(corpus);
            return dataset;
        }

        /// <summary>
        /// Stratified sample across query types (round-robin, reproducible with the seed).
        /// </summary>
        /// <param name="queries">Queries.</param>
        /// <param name="limit">Target count, 0 for all.</param>
        /// <param name="seed">Seed.</param>
        /// <returns>Sample.</returns>
        public static List<BenchmarkQuery> Sample(List<BenchmarkQuery> queries, int limit, int seed)
        {
            if (limit <= 0 || queries.Count <= limit) return queries;
            Random random = new Random(seed);
            List<Queue<BenchmarkQuery>> queues = queries
                .GroupBy(x => x.Type)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new Queue<BenchmarkQuery>(g.OrderBy(_ => random.Next())))
                .ToList();
            List<BenchmarkQuery> picked = new List<BenchmarkQuery>();
            while (picked.Count < limit && queues.Any(qu => qu.Count > 0))
            {
                foreach (Queue<BenchmarkQuery> queue in queues)
                {
                    if (picked.Count >= limit) break;
                    if (queue.Count > 0) picked.Add(queue.Dequeue());
                }
            }

            return picked.OrderBy(x => x.Id, StringComparer.Ordinal).ToList();
        }

        #endregion
    }
}
