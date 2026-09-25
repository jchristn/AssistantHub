namespace Test.Benchmark.Reporting
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using Test.Benchmark.Datasets;
    using Test.Benchmark.Metrics;
    using Test.Benchmark.Runners;

    /// <summary>
    /// Compares two retrieval or chat reports and exits non-zero on a regression, so a benchmark can gate a change in
    /// CI. A metric regresses only when its mean drops by more than <c>--tolerance</c> AND a paired bootstrap test
    /// over the queries both reports share gives p &lt; <c>--alpha</c>; improvements are reported the same way. p95
    /// latency regresses when it grows by more than <c>--latency-tolerance</c> (a fraction; 0 disables).
    /// </summary>
    public static class ResultComparer
    {
        #region Public-Methods

        /// <summary>
        /// Run the compare command.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>0 without regression, 1 with.</returns>
        public static int Compare(BenchmarkArguments args)
        {
            string baselinePath = args.Get("baseline", string.Empty);
            string candidatePath = args.Get("candidate", string.Empty);
            if (!File.Exists(baselinePath) || !File.Exists(candidatePath)) throw new FileNotFoundException("compare needs existing --baseline and --candidate report files.");
            double tolerance = args.GetDouble("tolerance", 0.01);
            double alpha = args.GetDouble("alpha", 0.05);
            double latencyTolerance = args.GetDouble("latency-tolerance", 0.0);

            string kind = JsonNode.Parse(File.ReadAllText(baselinePath))?["kind"]?.GetValue<string>() ?? string.Empty;
            string candidateKind = JsonNode.Parse(File.ReadAllText(candidatePath))?["kind"]?.GetValue<string>() ?? string.Empty;
            if (kind != candidateKind) throw new InvalidDataException("Reports are of different kinds: " + kind + " vs " + candidateKind + ".");

            Console.WriteLine("baseline : " + baselinePath);
            Console.WriteLine("candidate: " + candidatePath);
            Console.WriteLine("rule     : regression when delta < -" + tolerance + " and paired bootstrap p < " + alpha + (latencyTolerance > 0 ? "; p95 latency +" + (latencyTolerance * 100) + "%" : string.Empty));
            Console.WriteLine();

            bool regressed = kind switch
            {
                "retrieval" => CompareRetrieval(Load<RetrievalReport>(baselinePath), Load<RetrievalReport>(candidatePath), tolerance, alpha, latencyTolerance),
                "chat" => CompareChat(Load<ChatReport>(baselinePath), Load<ChatReport>(candidatePath), tolerance, alpha, latencyTolerance),
                _ => throw new InvalidDataException("compare supports retrieval and chat reports, not '" + kind + "'.")
            };

            Console.WriteLine();
            Console.WriteLine(regressed ? "RESULT: regression" : "RESULT: no regression");
            return regressed ? 1 : 0;
        }

        #endregion

        #region Private-Methods

        private static bool CompareRetrieval(RetrievalReport baseline, RetrievalReport candidate, double tolerance, double alpha, double latencyTolerance)
        {
            bool regressed = false;
            Header();
            foreach (ModeSummary mode in candidate.Modes)
            {
                ModeSummary? before = baseline.Modes.FirstOrDefault(m => string.Equals(m.Mode, mode.Mode, StringComparison.OrdinalIgnoreCase));
                if (before == null)
                {
                    Console.WriteLine(mode.Mode + ": not in baseline, skipped");
                    continue;
                }

                Dictionary<string, QueryOutcome> baseOutcomes = baseline.Outcomes.Where(o => o.Mode == before.Mode && o.Error == null).ToDictionary(o => o.Corpus + "/" + o.QueryId);
                List<QueryOutcome> candOutcomes = candidate.Outcomes.Where(o => o.Mode == mode.Mode && o.Error == null).ToList();
                foreach (string metric in RetrievalRunner.MetricNames)
                {
                    List<double> differences = new List<double>();
                    List<double> a = new List<double>();
                    List<double> b = new List<double>();
                    foreach (QueryOutcome outcome in candOutcomes)
                    {
                        if (!baseOutcomes.TryGetValue(outcome.Corpus + "/" + outcome.QueryId, out QueryOutcome? old)) continue;
                        if (!outcome.Metrics.TryGetValue(metric, out double after) || !old.Metrics.TryGetValue(metric, out double prior)) continue;
                        differences.Add(after - prior);
                        a.Add(prior);
                        b.Add(after);
                    }

                    if (differences.Count == 0) continue;
                    regressed |= Row(mode.Mode, metric, a.Average(), b.Average(), differences, tolerance, alpha);
                }

                regressed |= LatencyRow(mode.Mode, before.Latency.P95, mode.Latency.P95, latencyTolerance);
            }

            return regressed;
        }

        private static bool CompareChat(ChatReport baseline, ChatReport candidate, double tolerance, double alpha, double latencyTolerance)
        {
            bool regressed = false;
            Header();
            Dictionary<string, ChatItem> baseItems = baseline.Items.Where(i => i.Repeat == 0 && i.Error == null).ToDictionary(i => i.Corpus + "/" + i.QueryId);
            List<ChatItem> candItems = candidate.Items.Where(i => i.Repeat == 0 && i.Error == null).ToList();

            foreach ((string name, Func<ChatItem, double?> value) in new (string, Func<ChatItem, double?>)[]
            {
                ("accuracy", i => !i.Unanswerable && i.Correct.HasValue ? (i.Correct == true ? 1 : 0) : null),
                ("abstention", i => i.Unanswerable && i.Correct.HasValue ? (i.Correct == true ? 1 : 0) : null),
                ("faithfulness", i => i.Faithful.HasValue ? (i.Faithful == true ? 1 : 0) : null),
                ("evidenceInPrompt", i => i.Relevant.Count > 0 ? (i.Retrieved.Intersect(i.Relevant).Any() ? 1 : 0) : null),
                ("citationRecall", i => i.Relevant.Count > 0 ? i.Cited.Intersect(i.Relevant).Count() / (double)i.Relevant.Count : null)
            })
            {
                List<double> differences = new List<double>();
                List<double> a = new List<double>();
                List<double> b = new List<double>();
                foreach (ChatItem item in candItems)
                {
                    if (!baseItems.TryGetValue(item.Corpus + "/" + item.QueryId, out ChatItem? old)) continue;
                    double? after = value(item);
                    double? prior = value(old);
                    if (!after.HasValue || !prior.HasValue) continue;
                    differences.Add(after.Value - prior.Value);
                    a.Add(prior.Value);
                    b.Add(after.Value);
                }

                if (differences.Count > 0) regressed |= Row("chat", name, a.Average(), b.Average(), differences, tolerance, alpha);
            }

            regressed |= LatencyRow("chat", baseline.Latency.P95, candidate.Latency.P95, latencyTolerance);
            return regressed;
        }

        private static void Header()
        {
            Console.WriteLine(string.Format("{0,-28} {1,-17} {2,9} {3,9} {4,9} {5,5} {6,8}", "configuration", "metric", "baseline", "candidate", "delta", "n", "p"));
        }

        private static bool Row(string label, string metric, double before, double after, List<double> differences, double tolerance, double alpha)
        {
            double delta = after - before;
            double? p = Statistics.PairedBootstrapP(differences);
            bool significant = p.HasValue && p.Value < alpha;
            string verdict = delta < -tolerance && significant ? "  REGRESSION" : (delta > tolerance && significant ? "  improved" : string.Empty);
            Console.WriteLine(string.Format("{0,-28} {1,-17} {2,9:F3} {3,9:F3} {4,9:+0.000;-0.000;0.000} {5,5} {6,8}{7}",
                Truncate(label, 28), metric, before, after, delta, differences.Count, p.HasValue ? p.Value.ToString("F3") : "-", verdict));
            return verdict == "  REGRESSION";
        }

        private static bool LatencyRow(string label, double before, double after, double latencyTolerance)
        {
            bool slow = latencyTolerance > 0 && before > 0 && after > before * (1.0 + latencyTolerance);
            Console.WriteLine(string.Format("{0,-28} {1,-17} {2,9:F1} {3,9:F1} {4,9:+0.0;-0.0;0.0} {5,5} {6,8}{7}",
                Truncate(label, 28), "p95 ms", before, after, after - before, "", "", slow ? "  REGRESSION" : string.Empty));
            return slow;
        }

        private static T Load<T>(string path)
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), DatasetStore.Json) ?? throw new InvalidDataException("Could not read " + path);
        }

        private static string Truncate(string value, int max)
        {
            return value.Length <= max ? value : value.Substring(0, max);
        }

        #endregion
    }
}
