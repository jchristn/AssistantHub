namespace Test.Benchmark.Datasets
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Converts Qasper (Dasigi et al., 2021: question answering over full NLP papers) into the harness format. A
    /// seeded sample of papers becomes one corpus of long Markdown documents; every question is scoped to its own
    /// paper with <c>attached_document_ids</c> (the questions say "this paper"), so the benchmark measures which
    /// chunks of a long document retrieval finds, plus the attachment filter. Answer evidence paragraphs become
    /// evidence passages; unanswerable questions become negatives. The first annotator's answer is the gold answer.
    /// </summary>
    public static class QasperConverter
    {
        #region Public-Methods

        /// <summary>
        /// Convert.
        /// </summary>
        /// <param name="path">qasper-dev-v0.3.json (or train).</param>
        /// <param name="papers">Papers to sample.</param>
        /// <param name="seed">Seed.</param>
        /// <returns>Dataset.</returns>
        public static BenchmarkDataset Convert(string path, int papers, int seed)
        {
            JsonObject root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new InvalidDataException("Qasper file is not an object.");
            Random random = new Random(seed);
            List<string> ids = root.Select(kv => kv.Key).OrderBy(k => k, StringComparer.Ordinal).OrderBy(_ => random.Next()).Take(papers <= 0 ? int.MaxValue : papers).ToList();

            BenchmarkCorpus corpus = new BenchmarkCorpus { Id = "papers" };
            foreach (string paperId in ids.OrderBy(i => i, StringComparer.Ordinal))
            {
                JsonNode paper = root[paperId]!;
                string documentId = "qasper-" + paperId;
                StringBuilder body = new StringBuilder();
                body.Append("## Abstract\n\n").Append(paper["abstract"]?.GetValue<string>() ?? string.Empty).Append("\n\n");
                foreach (JsonNode? section in paper["full_text"] as JsonArray ?? new JsonArray())
                {
                    string name = section?["section_name"]?.GetValue<string>() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name)) body.Append("## ").Append(name.Replace(" ::: ", " / ")).Append("\n\n");
                    foreach (JsonNode? paragraph in section?["paragraphs"] as JsonArray ?? new JsonArray())
                    {
                        string text = paragraph?.GetValue<string>() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(text)) body.Append(text).Append("\n\n");
                    }
                }

                corpus.Documents.Add(new BenchmarkDocument
                {
                    Id = documentId,
                    Title = paper["title"]?.GetValue<string>() ?? paperId,
                    Body = body.ToString(),
                    ContentType = "text/markdown",
                    Labels = new List<string> { "paper" }
                });

                foreach (JsonNode? qa in paper["qas"] as JsonArray ?? new JsonArray())
                {
                    JsonNode? answer = (qa?["answers"] as JsonArray)?.FirstOrDefault()?["answer"];
                    if (qa == null || answer == null) continue;
                    BenchmarkQuery query = new BenchmarkQuery
                    {
                        Id = "qasper-" + (qa["question_id"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N")),
                        Text = qa["question"]?.GetValue<string>() ?? string.Empty,
                        AttachedDocuments = new List<string> { documentId }
                    };

                    if (answer["unanswerable"]?.GetValue<bool>() == true)
                    {
                        query.Type = "negative";
                        query.Category = "unanswerable";
                        query.Answer = BenchmarkQuery.NotInCorpus;
                    }
                    else
                    {
                        List<string> evidence = (answer["evidence"] as JsonArray ?? new JsonArray())
                            .Select(e => e?.GetValue<string>() ?? string.Empty)
                            .Where(e => e.Length > 0 && !e.StartsWith("FLOAT SELECTED", StringComparison.Ordinal))
                            .Distinct()
                            .ToList();
                        if (evidence.Count == 0) continue;

                        query.Relevant.Add(documentId);
                        query.Evidence = evidence;
                        query.Type = "detail";
                        query.Category = "factual_lookup";
                        JsonNode? yesNo = answer["yes_no"];
                        List<string> spans = (answer["extractive_spans"] as JsonArray ?? new JsonArray()).Select(s => s?.GetValue<string>() ?? string.Empty).Where(s => s.Length > 0).ToList();
                        string freeForm = answer["free_form_answer"]?.GetValue<string>() ?? string.Empty;
                        if (yesNo != null && yesNo.GetValueKind() == System.Text.Json.JsonValueKind.True) query.Answer = "Yes";
                        else if (yesNo != null && yesNo.GetValueKind() == System.Text.Json.JsonValueKind.False) query.Answer = "No";
                        else if (spans.Count > 0) query.Answer = string.Join("; ", spans);
                        else query.Answer = freeForm;
                        if (string.IsNullOrWhiteSpace(query.Answer)) continue;
                    }

                    corpus.Queries.Add(query);
                }
            }

            BenchmarkDataset dataset = new BenchmarkDataset
            {
                Name = "qasper",
                Description = "Qasper (Dasigi et al., 2021): " + corpus.Documents.Count + " NLP papers (seed " + seed + "), " + corpus.Queries.Count
                    + " questions, each scoped to its paper with attached_document_ids. Evidence passages are the annotators' evidence paragraphs."
            };
            dataset.Corpora.Add(corpus);
            return dataset;
        }

        #endregion
    }
}
