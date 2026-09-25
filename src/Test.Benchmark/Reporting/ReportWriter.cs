namespace Test.Benchmark.Reporting
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using Test.Benchmark.Datasets;
    using Test.Benchmark.Metrics;
    using Test.Benchmark.Runners;

    /// <summary>
    /// Writes reports as JSON (for machines and <c>compare</c>) and Markdown (for people).
    /// </summary>
    public static partial class ReportWriter
    {
        #region Public-Methods

        /// <summary>
        /// Write a report as JSON.
        /// </summary>
        /// <param name="report">Report.</param>
        /// <param name="basePath">Path without extension.</param>
        /// <returns>Written path.</returns>
        public static string WriteJson(object report, string basePath)
        {
            string path = basePath + ".json";
            File.WriteAllText(path, JsonSerializer.Serialize(report, report.GetType(), DatasetStore.Json), new UTF8Encoding(false));
            return path;
        }

        /// <summary>
        /// Write Markdown.
        /// </summary>
        /// <param name="markdown">Markdown.</param>
        /// <param name="basePath">Path without extension.</param>
        /// <returns>Written path.</returns>
        public static string WriteMarkdown(string markdown, string basePath)
        {
            string path = basePath + ".md";
            File.WriteAllText(path, markdown, new UTF8Encoding(false));
            return path;
        }

        /// <summary>
        /// Render a retrieval report.
        /// </summary>
        /// <param name="report">Report.</param>
        /// <returns>Markdown.</returns>
        public static string RenderRetrieval(RetrievalReport report)
        {
            StringBuilder md = new StringBuilder();
            md.Append("# Retrieval benchmark: ").Append(report.Dataset).Append("\n\n");
            if (!string.IsNullOrEmpty(report.Description)) md.Append(report.Description).Append("\n\n");
            AppendEnvironment(md, report.Environment, report.Config);
            AppendIngest(md, report.Ingest);
            if (report.Reachability != null) AppendReachability(md, report.Reachability);

            md.Append("## Headline (answerable queries, mean with 95% bootstrap CI)\n\n");
            string[] headline = new string[] { "ndcg@10", "recall@10", "hit@1", "mrr@10", "evidence@10", "context_evidence", "context_precision" };
            md.Append("| Configuration | n | ").Append(string.Join(" | ", headline)).Append(" |\n|---|---|").Append(Repeat("---|", headline.Length)).Append("\n");
            foreach (ModeSummary mode in report.Modes)
            {
                md.Append("| ").Append(mode.Mode).Append(" | ").Append(mode.Queries).Append(" | ");
                md.Append(string.Join(" | ", headline.Select(n => mode.Metrics.TryGetValue(n, out ConfidenceInterval? ci) ? ci.ToString() : "-"))).Append(" |\n");
            }

            md.Append("\n## All metrics\n\n| Configuration | ").Append(string.Join(" | ", RetrievalRunner.MetricNames)).Append(" | Errors |\n|---|")
              .Append(Repeat("---|", RetrievalRunner.MetricNames.Length)).Append("---|\n");
            foreach (ModeSummary mode in report.Modes)
            {
                md.Append("| ").Append(mode.Mode).Append(" | ");
                md.Append(string.Join(" | ", RetrievalRunner.MetricNames.Select(n => mode.Metrics.TryGetValue(n, out ConfidenceInterval? ci) ? ci.Mean.ToString("F3") : "-")));
                md.Append(" | ").Append(mode.Errors).Append(" |\n");
            }

            AppendBreakdown(md, "By query type", report.Modes, m => m.ByType);
            AppendBreakdown(md, "By question category", report.Modes, m => m.ByCategory);

            if (report.Modes.Any(m => m.ByStage.Count > 0))
            {
                md.Append("\n## Pipeline stages (what each stage adds)\n\n");
                md.Append("`1-search` is the first issued query's raw search, `2-fused` the list after multi-query fusion (the re-rank input), `3-rerank` the re-ranker's output.\n\n");
                md.Append("| Configuration | Stage | n | ").Append(string.Join(" | ", RetrievalRunner.StageMetricNames)).Append(" |\n|---|---|---|").Append(Repeat("---|", RetrievalRunner.StageMetricNames.Length)).Append("\n");
                foreach (ModeSummary mode in report.Modes)
                {
                    foreach (KeyValuePair<string, Dictionary<string, double>> stage in mode.ByStage)
                    {
                        md.Append("| ").Append(mode.Mode).Append(" | ").Append(stage.Key).Append(" | ").Append(stage.Value["count"]).Append(" | ");
                        md.Append(string.Join(" | ", RetrievalRunner.StageMetricNames.Select(n => Value(stage.Value, n)))).Append(" |\n");
                    }
                }
            }

            md.Append("\n## Score separation (can a score say \"nothing relevant\"?)\n\n");
            md.Append("AUROC of the top-hit score as a classifier of answerable vs unanswerable questions: 0.5 is chance, 1.0 a perfect threshold.\n\n");
            md.Append("| Configuration | Negatives | Mean top score, answerable | Mean top score, negative | Score AUROC | Vector AUROC | Rerank AUROC | Answerable with no chunks | Negatives with no chunks |\n|---|---|---|---|---|---|---|---|---|\n");
            foreach (ModeSummary mode in report.Modes)
            {
                md.Append("| ").Append(mode.Mode).Append(" | ").Append(mode.NegativeQueries).Append(" | ").Append(Nullable(mode.Separation, "meanTopScoreAnswerable"))
                  .Append(" | ").Append(Nullable(mode.Separation, "meanTopScoreNegative")).Append(" | ").Append(Nullable(mode.Separation, "scoreAuroc"))
                  .Append(" | ").Append(Nullable(mode.Separation, "vectorScoreAuroc")).Append(" | ").Append(Nullable(mode.Separation, "rerankScoreAuroc"))
                  .Append(" | ").Append(Percent(mode.Separation, "answerableEmptyRate")).Append(" | ").Append(Percent(mode.Separation, "negativeEmptyRate")).Append(" |\n");
            }

            if (report.Modes.Any(m => m.Answerability.Count > 0))
            {
                md.Append("\n## Answerability check\n\n| Configuration | Checked | \"unsupported\" precision | \"unsupported\" recall | Answerable marked unsupported |\n|---|---|---|---|---|\n");
                foreach (ModeSummary mode in report.Modes.Where(m => m.Answerability.Count > 0))
                {
                    md.Append("| ").Append(mode.Mode).Append(" | ").Append(mode.Answerability["checked"]).Append(" | ").Append(mode.Answerability["unsupportedPrecision"].ToString("F3"))
                      .Append(" | ").Append(mode.Answerability["unsupportedRecall"].ToString("F3")).Append(" | ").Append(mode.Answerability["answerableMarkedUnsupported"].ToString("P1")).Append(" |\n");
                }
            }

            md.Append("\n## Pipeline flags and filters\n\n| Configuration | Embedding failed | Hybrid fallback | Rerank unusable | Answerability unusable | Filter precision |\n|---|---|---|---|---|---|\n");
            foreach (ModeSummary mode in report.Modes)
            {
                md.Append("| ").Append(mode.Mode).Append(" | ").Append(mode.FlagRates.GetValueOrDefault("embeddingFailed").ToString("P1"))
                  .Append(" | ").Append(mode.FlagRates.GetValueOrDefault("hybridFallback").ToString("P1"))
                  .Append(" | ").Append(mode.FlagRates.GetValueOrDefault("rerankParseFailed").ToString("P1"))
                  .Append(" | ").Append(mode.FlagRates.GetValueOrDefault("answerabilityParseFailed").ToString("P1"))
                  .Append(" | ").Append(mode.FilterPrecision.HasValue ? mode.FilterPrecision.Value.ToString("F3") : "-").Append(" |\n");
            }

            md.Append("\n## Latency (ms)\n\n| Configuration | client p50 | p90 | p95 | p99 | server gate | rewrite | retrieval | rerank | answerability | total |\n|---|---|---|---|---|---|---|---|---|---|---|\n");
            foreach (ModeSummary mode in report.Modes)
            {
                md.Append("| ").Append(mode.Mode).Append(" | ").Append(mode.Latency.P50).Append(" | ").Append(mode.Latency.P90).Append(" | ").Append(mode.Latency.P95).Append(" | ").Append(mode.Latency.P99);
                foreach (string stage in new string[] { "gate", "rewrite", "retrieval", "rerank", "answerability", "total" })
                    md.Append(" | ").Append(mode.ServerMs.TryGetValue(stage, out double ms) ? ms.ToString("F1") : "-");
                md.Append(" |\n");
            }

            AppendStageBreakdown(md, report.Modes.Select(m => (m.Mode, m.Stages)).ToList());

            ModeSummary? first = report.Modes.FirstOrDefault();
            if (first != null)
            {
                md.Append("\n## Misses in `").Append(first.Mode).Append("` (first 30 answerable queries with no relevant document in the results)\n\n");
                foreach (QueryOutcome miss in report.Outcomes.Where(o => o.Mode == first.Mode && o.Relevant.Count > 0 && o.Error == null && o.Metrics.GetValueOrDefault("recall@10") == 0).Take(30))
                {
                    md.Append("- `").Append(miss.Corpus).Append('/').Append(miss.QueryId).Append("` (").Append(miss.Type).Append(") relevant ")
                      .Append(string.Join(",", miss.Relevant)).Append(" → got ").Append(string.Join(",", miss.Ranked.Take(5))).Append('\n');
                }

                foreach (QueryOutcome error in report.Outcomes.Where(o => o.Error != null).Take(10))
                    md.Append("- error `").Append(error.Corpus).Append('/').Append(error.QueryId).Append("`: ").Append(error.Error).Append('\n');
            }

            return md.ToString();
        }

        /// <summary>
        /// Environment and configuration table.
        /// </summary>
        /// <param name="md">Builder.</param>
        /// <param name="environment">Environment.</param>
        /// <param name="config">Configuration.</param>
        public static void AppendEnvironment(StringBuilder md, BenchmarkEnvironment environment, Dictionary<string, string> config)
        {
            md.Append("| | |\n|---|---|\n");
            md.Append("| Started (UTC) | ").Append(environment.StartedUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Append(" |\n");
            md.Append("| Server | ").Append(environment.ServerUrl).Append(" |\n");
            md.Append("| Commit | ").Append(environment.GitCommit).Append(" |\n");
            md.Append("| Machine | ").Append(environment.Machine).Append(" |\n");
            md.Append("| Harness | ").Append(environment.HarnessVersion).Append(" |\n");
            if (!string.IsNullOrEmpty(environment.DatasetHash)) md.Append("| Dataset hash | ").Append(environment.DatasetHash).Append(" |\n");
            foreach (KeyValuePair<string, string> image in environment.Images) md.Append("| ").Append(image.Key).Append(" | ").Append(image.Value).Append(" |\n");
            foreach (KeyValuePair<string, string> model in environment.Models) md.Append("| model ").Append(model.Key).Append(" | ").Append(model.Value).Append(" |\n");
            foreach (KeyValuePair<string, string> item in config) md.Append("| ").Append(item.Key).Append(" | ").Append(item.Value.Replace("|", "/")).Append(" |\n");
            md.Append('\n');
        }

        #endregion

        #region Private-Methods

        private static void AppendIngest(StringBuilder md, IngestSummary ingest)
        {
            md.Append("## Ingest\n\n| Documents | Reused | Uploaded | Failures | Wall (s) | Docs/s | In flight | Upload→complete p50 | p95 |\n|---|---|---|---|---|---|---|---|---|\n");
            md.Append("| ").Append(ingest.Documents).Append(" | ").Append(ingest.Reused).Append(" | ").Append(ingest.Uploaded).Append(" | ").Append(ingest.Failures)
              .Append(" | ").Append(ingest.WallSeconds).Append(" | ").Append(ingest.DocumentsPerSecond).Append(" | ").Append(ingest.Concurrency)
              .Append(" | ").Append(ingest.Latency.P50).Append(" ms | ").Append(ingest.Latency.P95).Append(" ms |\n\n");
            foreach (string error in ingest.SampleErrors) md.Append("- ingest failure: `").Append(error.Replace("`", "'")).Append("`\n");
            if (ingest.SampleErrors.Count > 0) md.Append('\n');
        }

        private static void AppendReachability(StringBuilder md, ReachabilitySummary reach)
        {
            md.Append("## Evidence reachability (ceiling set by extraction and chunking)\n\n");
            md.Append("| Passages | Reachable | Rate | Chunks | Mean chunk chars | Tiny chunks | Duplicate chunks | Documents without chunks |\n|---|---|---|---|---|---|---|---|\n");
            md.Append("| ").Append(reach.Passages).Append(" | ").Append(reach.Reachable).Append(" | ").Append(reach.Rate.ToString("P1")).Append(" | ").Append(reach.Chunks)
              .Append(" | ").Append(reach.MeanChunkChars).Append(" | ").Append(reach.TinyChunks).Append(" | ").Append(reach.DuplicateChunks).Append(" | ").Append(reach.DocumentsWithoutChunks).Append(" |\n\n");
            md.Append("| Content type | Passages | Reachable |\n|---|---|---|\n");
            foreach (KeyValuePair<string, double> item in reach.ByContentType.OrderBy(i => i.Key, StringComparer.Ordinal))
                md.Append("| ").Append(item.Key).Append(" | ").Append(reach.PassagesByContentType[item.Key]).Append(" | ").Append(item.Value.ToString("P1")).Append(" |\n");
            md.Append('\n');
            foreach (string sample in reach.SampleUnreachable.Take(15)) md.Append("- unreachable: ").Append(sample.Replace("|", "/")).Append('\n');
            md.Append('\n');
        }

        private static void AppendBreakdown(StringBuilder md, string title, List<ModeSummary> modes, Func<ModeSummary, Dictionary<string, Dictionary<string, double>>> select)
        {
            string[] metrics = new string[] { "hit@1", "recall@10", "all@10", "ndcg@10", "evidence@10", "context_evidence" };
            HashSet<string> keys = new HashSet<string>(modes.SelectMany(m => select(m).Keys));
            if (keys.Count == 0) return;
            md.Append("\n## ").Append(title).Append("\n\n| Group | Configuration | n | ").Append(string.Join(" | ", metrics)).Append(" |\n|---|---|---|").Append(Repeat("---|", metrics.Length)).Append("\n");
            foreach (string key in keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                foreach (ModeSummary mode in modes)
                {
                    if (!select(mode).TryGetValue(key, out Dictionary<string, double>? values)) continue;
                    md.Append("| ").Append(key).Append(" | ").Append(mode.Mode).Append(" | ").Append(values["count"]).Append(values["count"] < 20 ? "*" : string.Empty).Append(" | ");
                    md.Append(string.Join(" | ", metrics.Select(n => Value(values, n)))).Append(" |\n");
                }
            }

            md.Append("\n\\* fewer than 20 queries: indicative only.\n");
        }

        private static void AppendStageBreakdown(StringBuilder md, List<(string Label, Dictionary<string, StageBreakdown> Stages)> rows)
        {
            HashSet<string> names = new HashSet<string>(rows.SelectMany(r => r.Stages.Keys));
            md.Append("\n## Server operations (mean ms per call, from the metrics endpoint)\n\n");
            if (names.Count == 0)
            {
                md.Append("_No metrics endpoint available._\n");
                return;
            }

            List<string> ordered = names.OrderBy(n => n, StringComparer.Ordinal).ToList();
            md.Append("| Configuration | ").Append(string.Join(" | ", ordered)).Append(" |\n|---|").Append(Repeat("---|", ordered.Count)).Append("\n");
            foreach ((string label, Dictionary<string, StageBreakdown> stages) in rows)
            {
                md.Append("| ").Append(label).Append(" | ");
                md.Append(string.Join(" | ", ordered.Select(s => stages.TryGetValue(s, out StageBreakdown? b) ? b.MeanMs + " (×" + b.Count + ")" : "-")));
                md.Append(" |\n");
            }
        }

        private static string Value(Dictionary<string, double> values, string name)
        {
            return values.TryGetValue(name, out double value) ? value.ToString("F3") : "-";
        }

        private static string Nullable(Dictionary<string, double?> values, string name)
        {
            return values.TryGetValue(name, out double? value) && value.HasValue ? value.Value.ToString("F3") : "-";
        }

        private static string Percent(Dictionary<string, double?> values, string name)
        {
            return values.TryGetValue(name, out double? value) && value.HasValue ? value.Value.ToString("P1") : "-";
        }

        private static string Repeat(string text, int count)
        {
            return string.Concat(Enumerable.Repeat(text, count));
        }

        #endregion
    }
}
