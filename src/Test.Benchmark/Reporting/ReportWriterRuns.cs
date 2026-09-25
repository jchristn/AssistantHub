namespace Test.Benchmark.Reporting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Test.Benchmark.Metrics;
    using Test.Benchmark.Runners;

    /// <summary>
    /// Markdown renderers for ingest, chat, load and eval reports.
    /// </summary>
    public static partial class ReportWriter
    {
        #region Public-Methods

        /// <summary>
        /// Render an ingest report.
        /// </summary>
        /// <param name="report">Report.</param>
        /// <returns>Markdown.</returns>
        public static string RenderIngest(IngestReport report)
        {
            StringBuilder md = new StringBuilder();
            md.Append("# Ingest benchmark: ").Append(report.Dataset).Append("\n\n");
            AppendEnvironment(md, report.Environment, report.Config);
            AppendIngest(md, report.Ingest);

            if (report.ByContentType.Count > 0)
            {
                md.Append("## By content type (documents uploaded in this run)\n\n| Content type | Documents | Failures | Mean chunks | Mean upload→complete ms | Mean KB |\n|---|---|---|---|---|---|\n");
                foreach (KeyValuePair<string, Dictionary<string, double>> type in report.ByContentType)
                {
                    md.Append("| ").Append(type.Key).Append(" | ").Append(type.Value["documents"]).Append(" | ").Append(type.Value["failures"]).Append(" | ")
                      .Append(type.Value["meanChunks"]).Append(" | ").Append(type.Value["meanMs"]).Append(" | ").Append(type.Value["meanKB"]).Append(" |\n");
                }

                md.Append('\n');
            }

            AppendStageBreakdown(md, new List<(string, Dictionary<string, StageBreakdown>)> { ("ingest", report.Ingest.Stages) });
            md.Append('\n');
            if (report.Reachability != null) AppendReachability(md, report.Reachability);
            return md.ToString();
        }

        /// <summary>
        /// Render a chat report.
        /// </summary>
        /// <param name="report">Report.</param>
        /// <returns>Markdown.</returns>
        public static string RenderChat(ChatReport report)
        {
            StringBuilder md = new StringBuilder();
            md.Append("# Chat benchmark: ").Append(report.Dataset).Append("\n\n");
            Dictionary<string, string> config = new Dictionary<string, string>(report.Config);
            foreach (KeyValuePair<string, string> item in report.Assistant) config["assistant." + item.Key] = item.Value;
            AppendEnvironment(md, report.Environment, config);
            AppendIngest(md, report.Ingest);

            md.Append("## Headline (95% bootstrap CI)\n\n| Metric | Value |\n|---|---|\n");
            foreach (KeyValuePair<string, ConfidenceInterval> interval in report.Intervals) md.Append("| ").Append(interval.Key).Append(" | ").Append(interval.Value).Append(" (n ").Append(interval.Value.N).Append(") |\n");
            if (report.RepeatAccuracy.Count > 1)
                md.Append("| overall accuracy across ").Append(report.RepeatAccuracy.Count).Append(" repeats | ").Append(report.Summary["overallAccuracyMeanOfRepeats"].ToString("F3"))
                  .Append(" ± ").Append(report.Summary["overallAccuracySdOfRepeats"].ToString("F3")).Append(" (").Append(string.Join(", ", report.RepeatAccuracy.Select(a => a.ToString("F3")))).Append(") |\n");
            if (report.JudgeAgreement != null)
                md.Append("| judge vs hand labels | agreement ").Append(report.JudgeAgreement.GetValueOrDefault("agreement").ToString("F3")).Append(", kappa ")
                  .Append(report.JudgeAgreement.GetValueOrDefault("kappa").ToString("F3")).Append(" (n ").Append(report.JudgeAgreement.GetValueOrDefault("paired")).Append(") |\n");

            md.Append("\n## Summary\n\n| Metric | Value |\n|---|---|\n");
            foreach (KeyValuePair<string, double> item in report.Summary) md.Append("| ").Append(item.Key).Append(" | ").Append(item.Value % 1 == 0 ? item.Value.ToString("F0") : item.Value.ToString("F3")).Append(" |\n");
            md.Append("\n| Latency (client, ms) | p50 | p90 | p95 | max | mean |\n|---|---|---|---|---|---|\n");
            md.Append("| chat | ").Append(report.Latency.P50).Append(" | ").Append(report.Latency.P90).Append(" | ").Append(report.Latency.P95).Append(" | ").Append(report.Latency.Max).Append(" | ").Append(report.Latency.Mean).Append(" |\n");

            AppendChatBreakdown(md, "By query type", report.ByType);
            AppendChatBreakdown(md, "By question category", report.ByCategory);
            AppendStageBreakdown(md, new List<(string, Dictionary<string, StageBreakdown>)> { ("chat", report.Stages) });

            md.Append("\n## Wrong answers (first 25)\n\n");
            foreach (ChatItem item in report.Items.Where(i => i.Repeat == 0 && i.Correct == false).Take(25))
            {
                string answer = item.Answer.Replace("\n", " ");
                if (answer.Length > 240) answer = answer.Substring(0, 240) + "…";
                string evidence = item.Relevant.Count == 0 ? "n/a" : (item.Retrieved.Intersect(item.Relevant).Any() ? "yes" : "no");
                md.Append("- `").Append(item.Corpus).Append('/').Append(item.QueryId).Append("` (").Append(item.Type).Append("), evidence in prompt: ").Append(evidence)
                  .Append("\n  - Q: ").Append(item.Question.Replace("\n", " ")).Append("\n  - gold: ").Append(item.Gold.Replace("\n", " ")).Append("\n  - got: ").Append(answer).Append('\n');
            }

            foreach (ChatItem item in report.Items.Where(i => i.Error != null).Take(10))
                md.Append("- error `").Append(item.Corpus).Append('/').Append(item.QueryId).Append("`: ").Append(item.Error).Append('\n');
            return md.ToString();
        }

        /// <summary>
        /// Render a load report.
        /// </summary>
        /// <param name="report">Report.</param>
        /// <returns>Markdown.</returns>
        public static string RenderLoad(LoadReport report)
        {
            StringBuilder md = new StringBuilder();
            md.Append("# Load benchmark: ").Append(report.Scenario).Append(" on ").Append(report.Dataset).Append("\n\n");
            AppendEnvironment(md, report.Environment, report.Config);
            AppendIngest(md, report.Ingest);

            md.Append("## Throughput and latency by concurrency (client, ms)\n\n| Concurrency | Ops/s | Error rate | Operation | Count | Ops/s | p50 | p95 | p99 | max |\n|---|---|---|---|---|---|---|---|---|---|\n");
            foreach (LoadLevel level in report.Levels)
            {
                bool first = true;
                foreach (KeyValuePair<string, LoadOpStats> op in level.Operations)
                {
                    md.Append("| ").Append(first ? level.Concurrency.ToString() : string.Empty).Append(" | ").Append(first ? level.Throughput.ToString("F1") : string.Empty)
                      .Append(" | ").Append(first ? (level.ErrorRate * 100).ToString("F2") + "%" : string.Empty).Append(" | ").Append(op.Key).Append(" | ").Append(op.Value.Count)
                      .Append(" | ").Append(op.Value.Throughput.ToString("F1")).Append(" | ").Append(op.Value.Latency.P50).Append(" | ").Append(op.Value.Latency.P95)
                      .Append(" | ").Append(op.Value.Latency.P99).Append(" | ").Append(op.Value.Latency.Max).Append(" |\n");
                    first = false;
                }
            }

            AppendStageBreakdown(md, report.Levels.Select(l => ("c=" + l.Concurrency, l.Stages)).ToList());
            foreach (LoadLevel level in report.Levels)
            {
                foreach (string error in level.SampleErrors) md.Append("- c=").Append(level.Concurrency).Append(" error: `").Append(error.Replace("`", "'")).Append("`\n");
            }

            return md.ToString();
        }

        /// <summary>
        /// Render an eval agreement report.
        /// </summary>
        /// <param name="report">Report.</param>
        /// <returns>Markdown.</returns>
        public static string RenderEval(EvalAgreementReport report)
        {
            StringBuilder md = new StringBuilder();
            md.Append("# In-product Eval vs independent judge: ").Append(report.Dataset).Append("\n\n");
            AppendEnvironment(md, report.Environment, report.Config);
            md.Append("## Summary\n\n| Metric | Value |\n|---|---|\n");
            foreach (KeyValuePair<string, double> item in report.Summary) md.Append("| ").Append(item.Key).Append(" | ").Append(item.Value % 1 == 0 ? item.Value.ToString("F0") : item.Value.ToString("F3")).Append(" |\n");
            md.Append("\n## Disagreements\n\n| Query | Type | Eval | Judge | Answer |\n|---|---|---|---|---|\n");
            foreach (Dictionary<string, string> item in report.Items.Where(i => !string.Equals(i["evalPass"], i["judge"], StringComparison.OrdinalIgnoreCase)))
            {
                md.Append("| ").Append(item["queryId"]).Append(" | ").Append(item["type"]).Append(" | ").Append(item["evalPass"]).Append(" | ").Append(item["judge"]).Append(" | ")
                  .Append(item["answer"].Replace("|", "/").Replace("\n", " ")).Append(" |\n");
            }

            return md.ToString();
        }

        #endregion

        #region Private-Methods

        private static void AppendChatBreakdown(StringBuilder md, string title, Dictionary<string, Dictionary<string, double>> groups)
        {
            if (groups.Count == 0) return;
            string[] names = new string[] { "accuracy", "abstentionAccuracy", "faithfulness", "evidenceRetrievedRate", "accuracyWhenEvidenceRetrieved", "citationRecall" };
            md.Append("\n## ").Append(title).Append("\n\n| Group | n | ").Append(string.Join(" | ", names)).Append(" |\n|---|---|").Append(Repeat("---|", names.Length)).Append("\n");
            foreach (KeyValuePair<string, Dictionary<string, double>> group in groups)
            {
                double n = group.Value.GetValueOrDefault("questions");
                md.Append("| ").Append(group.Key).Append(" | ").Append(n).Append(n < 20 ? "*" : string.Empty).Append(" | ");
                md.Append(string.Join(" | ", names.Select(name => Value(group.Value, name)))).Append(" |\n");
            }

            md.Append("\n\\* fewer than 20 questions: indicative only.\n");
        }

        #endregion
    }
}
