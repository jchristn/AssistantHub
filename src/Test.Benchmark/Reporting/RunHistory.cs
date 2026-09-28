namespace Test.Benchmark.Reporting
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using Test.Benchmark.Datasets;

    /// <summary>
    /// The run ledger: one JSON line per configuration of every benchmark run, appended to
    /// <c>benchmarks/history/runs.jsonl</c>. Full reports stay in the git-ignored <c>benchmarks/results/</c>; the ledger
    /// is small, committed, and carries what is needed to compare runs over time: when, which commit, which dataset
    /// version, a fingerprint of the configuration, the headline metrics, and the report file for a paired
    /// <c>compare</c>. Runs with the same fingerprint measured the same thing and can be compared directly.
    /// </summary>
    public static class RunHistory
    {
        #region Public-Members

        /// <summary>
        /// Metrics recorded for retrieval configurations.
        /// </summary>
        public static readonly string[] RetrievalMetrics = new string[]
        {
            "ndcg@10", "recall@10", "hit@1", "mrr@10", "evidence@10", "context_evidence", "context_precision"
        };

        /// <summary>
        /// Metrics recorded for chat runs.
        /// </summary>
        public static readonly string[] ChatMetrics = new string[]
        {
            "accuracy", "abstentionAccuracy", "overallAccuracy", "faithfulness", "evidenceRetrievedRate", "accuracyWhenEvidenceRetrieved",
            "citationPrecision", "citationRecall", "errors", "questions"
        };

        #endregion

        #region Public-Methods

        /// <summary>
        /// Ledger path for a results directory (its sibling <c>history/runs.jsonl</c>).
        /// </summary>
        /// <param name="resultsDirectory">Results directory.</param>
        /// <returns>Ledger path.</returns>
        public static string LedgerPath(string resultsDirectory)
        {
            string root = Path.GetDirectoryName(Path.GetFullPath(resultsDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? ".";
            return Path.Combine(root, "history", "runs.jsonl");
        }

        /// <summary>
        /// Append a written report to the ledger, replacing any earlier lines for the same report.
        /// </summary>
        /// <param name="reportPath">Report JSON path.</param>
        /// <param name="ledgerPath">Ledger path.</param>
        /// <returns>Entries appended.</returns>
        public static int Record(string reportPath, string ledgerPath)
        {
            List<JsonObject> entries = Summarize(reportPath, Path.GetDirectoryName(ledgerPath) ?? ".");
            if (entries.Count == 0) return 0;
            string runId = entries[0]["runId"]!.GetValue<string>();
            List<string> lines = File.Exists(ledgerPath)
                ? File.ReadAllLines(ledgerPath).Where(l => l.Trim().Length > 0 && !l.Contains("\"runId\":\"" + runId + "\"", StringComparison.Ordinal)).ToList()
                : new List<string>();
            lines.AddRange(entries.Select(e => e.ToJsonString()));
            Directory.CreateDirectory(Path.GetDirectoryName(ledgerPath) ?? ".");
            File.WriteAllText(ledgerPath, string.Join("\n", lines) + "\n", new UTF8Encoding(false));
            return entries.Count;
        }

        /// <summary>
        /// Rebuild the ledger from every report in a results directory (backfill).
        /// </summary>
        /// <param name="resultsDirectory">Results directory.</param>
        /// <param name="ledgerPath">Ledger path.</param>
        /// <returns>Entries written.</returns>
        public static int Rebuild(string resultsDirectory, string ledgerPath)
        {
            List<string> lines = new List<string>();
            foreach (string path in Directory.GetFiles(resultsDirectory, "*.json").OrderBy(p => Path.GetFileName(p), StringComparer.Ordinal))
            {
                try
                {
                    lines.AddRange(Summarize(path, Path.GetDirectoryName(ledgerPath) ?? ".").Select(e => e.ToJsonString()));
                }
                catch (Exception e) when (e is JsonException || e is InvalidOperationException || e is IOException)
                {
                    Console.Error.WriteLine("skipped " + Path.GetFileName(path) + ": " + e.Message);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ledgerPath) ?? ".");
            File.WriteAllText(ledgerPath, string.Join("\n", lines) + (lines.Count > 0 ? "\n" : string.Empty), new UTF8Encoding(false));
            return lines.Count;
        }

        /// <summary>
        /// Read the ledger.
        /// </summary>
        /// <param name="ledgerPath">Ledger path.</param>
        /// <returns>Entries in file order.</returns>
        public static List<JsonObject> Read(string ledgerPath)
        {
            List<JsonObject> entries = new List<JsonObject>();
            if (!File.Exists(ledgerPath)) return entries;
            foreach (string line in File.ReadAllLines(ledgerPath))
            {
                if (line.Trim().Length == 0) continue;
                if (JsonNode.Parse(line) is JsonObject entry) entries.Add(entry);
            }

            return entries;
        }

        /// <summary>
        /// The <c>history</c> command: list runs and show each metric's change against the previous run with the same
        /// fingerprint.
        /// </summary>
        /// <param name="args">Arguments (--dataset, --kind, --configuration, --label, --metric, --last, --rebuild, --results-dir).</param>
        /// <param name="resultsDirectory">Default results directory.</param>
        /// <returns>Exit code.</returns>
        public static int Show(BenchmarkArguments args, string resultsDirectory)
        {
            string results = args.Get("results-dir", resultsDirectory);
            string ledger = LedgerPath(results);
            if (args.GetFlag("rebuild"))
            {
                Console.WriteLine("Rebuilt " + ledger + ": " + Rebuild(results, ledger) + " entries");
                return 0;
            }

            List<JsonObject> entries = Read(ledger);
            string? dataset = args.GetOptional("dataset");
            string? kind = args.GetOptional("kind");
            string? configuration = args.GetOptional("configuration");
            string? label = args.GetOptional("label");
            string metric = args.Get("metric", kind == "chat" ? "accuracy" : "ndcg@10");
            int last = args.GetInt("last", 40);

            List<JsonObject> filtered = entries.Where(e =>
                (dataset == null || string.Equals(Str(e, "dataset"), dataset, StringComparison.OrdinalIgnoreCase))
                && (kind == null || string.Equals(Str(e, "kind"), kind, StringComparison.OrdinalIgnoreCase))
                && (configuration == null || Str(e, "configuration").Contains(configuration, StringComparison.OrdinalIgnoreCase))
                && (label == null || Str(e, "label").Contains(label, StringComparison.OrdinalIgnoreCase))).ToList();

            Dictionary<string, double> previous = new Dictionary<string, double>(StringComparer.Ordinal);
            List<string> rows = new List<string>();
            foreach (JsonObject entry in filtered)
            {
                double? value = entry["metrics"]?[metric]?.GetValue<double>();
                string fingerprint = Str(entry, "fingerprint");
                string delta = string.Empty;
                if (value.HasValue && previous.TryGetValue(fingerprint, out double before)) delta = (value.Value - before).ToString("+0.000;-0.000;0.000", CultureInfo.InvariantCulture);
                if (value.HasValue) previous[fingerprint] = value.Value;
                rows.Add(string.Format(CultureInfo.InvariantCulture, "{0,-16} {1,-10} {2,-9} {3,-18} {4,-22} {5,-26} {6,8} {7,8}  {8}",
                    Str(entry, "startedUtc").Replace("T", " ").Substring(0, Math.Min(16, Str(entry, "startedUtc").Length)),
                    Truncate(Str(entry, "commit"), 10), Str(entry, "kind"), Truncate(Str(entry, "dataset"), 18), Truncate(Str(entry, "label"), 22),
                    Truncate(Str(entry, "configuration"), 26), value.HasValue ? value.Value.ToString("F3", CultureInfo.InvariantCulture) : "-", delta, Str(entry, "fingerprint")));
            }

            Console.WriteLine(string.Format("{0,-16} {1,-10} {2,-9} {3,-18} {4,-22} {5,-26} {6,8} {7,8}  {8}", "started (UTC)", "commit", "kind", "dataset", "label", "configuration", metric, "Δ prev", "fingerprint"));
            foreach (string row in rows.Skip(Math.Max(0, rows.Count - last))) Console.WriteLine(row);
            Console.WriteLine();
            Console.WriteLine(filtered.Count + " of " + entries.Count + " ledger entries (" + ledger + "). Δ prev compares with the previous run of the same fingerprint.");
            return 0;
        }

        /// <summary>
        /// Find the report of the most recent earlier run that shares a fingerprint with a report (for
        /// <c>compare --baseline previous</c>).
        /// </summary>
        /// <param name="candidatePath">Candidate report.</param>
        /// <param name="ledgerPath">Ledger path.</param>
        /// <returns>Baseline report path, or null.</returns>
        public static string? PreviousReport(string candidatePath, string ledgerPath)
        {
            List<JsonObject> candidate = Summarize(candidatePath, Path.GetDirectoryName(ledgerPath) ?? ".");
            if (candidate.Count == 0) return null;
            HashSet<string> fingerprints = new HashSet<string>(candidate.Select(e => Str(e, "fingerprint")), StringComparer.Ordinal);
            string runId = Str(candidate[0], "runId");
            string resultsDirectory = Path.GetDirectoryName(Path.GetFullPath(candidatePath)) ?? ".";
            foreach (JsonObject entry in Read(ledgerPath).AsEnumerable().Reverse())
            {
                if (Str(entry, "runId") == runId || string.CompareOrdinal(Str(entry, "runId"), runId) > 0) continue;
                if (!fingerprints.Contains(Str(entry, "fingerprint"))) continue;
                string path = Path.Combine(resultsDirectory, Str(entry, "runId") + ".json");
                if (File.Exists(path)) return path;
            }

            return null;
        }

        #endregion

        #region Private-Methods

        private static List<JsonObject> Summarize(string reportPath, string ledgerDirectory)
        {
            JsonObject report = JsonNode.Parse(File.ReadAllText(reportPath)) as JsonObject ?? throw new InvalidDataException("Not a report: " + reportPath);
            string kind = report["kind"]?.GetValue<string>() ?? string.Empty;
            string runId = Path.GetFileNameWithoutExtension(reportPath);
            string dataset = report["dataset"]?.GetValue<string>() ?? string.Empty;
            string label = LabelOf(runId, kind, dataset, report["scenario"]?.GetValue<string>());
            JsonNode? environment = report["environment"];
            JsonObject config = report["config"] as JsonObject ?? new JsonObject();

            JsonObject Base(string configuration, JsonObject settings, JsonObject metrics)
            {
                string fingerprint = DatasetStore.Sha256(kind + "|" + dataset + "|" + configuration.Split(' ')[0] + "|" + settings.ToJsonString()).Substring(0, 12);
                return new JsonObject
                {
                    ["runId"] = runId,
                    ["kind"] = kind,
                    ["dataset"] = dataset,
                    ["label"] = label,
                    ["configuration"] = configuration,
                    ["startedUtc"] = environment?["startedUtc"]?.GetValue<string>() ?? string.Empty,
                    ["commit"] = environment?["gitCommit"]?.GetValue<string>() ?? string.Empty,
                    ["datasetHash"] = environment?["datasetHash"]?.GetValue<string>(),
                    ["pipeline"] = config["pipeline"]?.GetValue<string>() ?? "1",
                    ["fingerprint"] = fingerprint,
                    ["settings"] = settings,
                    ["metrics"] = metrics,
                    ["report"] = Path.GetRelativePath(ledgerDirectory, reportPath).Replace('\\', '/')
                };
            }

            JsonObject Ingestion()
            {
                JsonObject settings = new JsonObject();
                foreach (string key in new string[] { "chunking", "embeddingEndpoint", "summarization", "contextPrefix", "ingestionHash", "limit", "repeats" })
                {
                    if (config[key] != null) settings[key] = config[key]!.GetValue<string>();
                }

                return settings;
            }

            List<JsonObject> entries = new List<JsonObject>();
            switch (kind)
            {
                case "retrieval":
                    double? reach = report["reachability"]?["rate"]?.GetValue<double>();
                    foreach (JsonNode? mode in report["modes"] as JsonArray ?? new JsonArray())
                    {
                        if (mode == null) continue;
                        JsonObject settings = Ingestion();
                        foreach (KeyValuePair<string, JsonNode?> item in mode["assistant"] as JsonObject ?? new JsonObject()) settings[item.Key] = item.Value?.DeepClone();
                        JsonObject metrics = new JsonObject();
                        foreach (string name in RetrievalMetrics)
                        {
                            JsonNode? value = mode["metrics"]?[name]?["mean"];
                            if (value != null) metrics[name] = value.GetValue<double>();
                        }

                        Copy(metrics, "scoreAuroc", mode["separation"]?["scoreAuroc"]);
                        Copy(metrics, "vectorScoreAuroc", mode["separation"]?["vectorScoreAuroc"]);
                        Copy(metrics, "answerableEmptyRate", mode["separation"]?["answerableEmptyRate"]);
                        Copy(metrics, "embeddingFailedRate", mode["flagRates"]?["embeddingFailed"]);
                        Copy(metrics, "rerankUnusableRate", mode["flagRates"]?["rerankParseFailed"]);
                        Copy(metrics, "filterPrecision", mode["filterPrecision"]);
                        Copy(metrics, "latencyP50", mode["latency"]?["p50"]);
                        Copy(metrics, "latencyP95", mode["latency"]?["p95"]);
                        Copy(metrics, "queries", mode["queries"]);
                        Copy(metrics, "errors", mode["errors"]);
                        if (reach.HasValue) metrics["reachability"] = reach.Value;
                        Copy(metrics, "ingestFailures", report["ingest"]?["failures"]);
                        entries.Add(Base(mode["mode"]?.GetValue<string>() ?? "?", settings, metrics));
                    }

                    break;

                case "chat":
                {
                    JsonObject settings = Ingestion();
                    foreach (KeyValuePair<string, JsonNode?> item in report["assistant"] as JsonObject ?? new JsonObject()) settings[item.Key] = item.Value?.DeepClone();
                    if (config["judge"] != null) settings["judge"] = config["judge"]!.GetValue<string>();
                    JsonObject metrics = new JsonObject();
                    foreach (string name in ChatMetrics) Copy(metrics, name, report["summary"]?[name]);
                    Copy(metrics, "latencyP50", report["latency"]?["p50"]);
                    Copy(metrics, "latencyP95", report["latency"]?["p95"]);
                    string mode = report["assistant"]?["searchMode"]?.GetValue<string>() ?? "existing";
                    entries.Add(Base(mode, settings, metrics));
                    break;
                }

                case "load":
                    foreach (JsonNode? level in report["levels"] as JsonArray ?? new JsonArray())
                    {
                        if (level == null) continue;
                        JsonObject settings = Ingestion();
                        settings["models"] = config["models"]?.GetValue<string>();
                        settings["scenario"] = report["scenario"]?.GetValue<string>();
                        JsonObject metrics = new JsonObject();
                        Copy(metrics, "throughput", level["throughput"]);
                        Copy(metrics, "errorRate", level["errorRate"]);
                        JsonObject? firstOp = (level["operations"] as JsonObject)?.Select(kv => kv.Value as JsonObject).FirstOrDefault();
                        Copy(metrics, "latencyP50", firstOp?["latency"]?["p50"]);
                        Copy(metrics, "latencyP95", firstOp?["latency"]?["p95"]);
                        entries.Add(Base("c=" + level["concurrency"], settings, metrics));
                    }

                    break;

                case "ingest":
                {
                    JsonObject metrics = new JsonObject();
                    Copy(metrics, "documents", report["ingest"]?["documents"]);
                    Copy(metrics, "failures", report["ingest"]?["failures"]);
                    Copy(metrics, "documentsPerSecond", report["ingest"]?["documentsPerSecond"]);
                    Copy(metrics, "reachability", report["reachability"]?["rate"]);
                    entries.Add(Base("ingest", Ingestion(), metrics));
                    break;
                }

                case "eval":
                {
                    JsonObject metrics = new JsonObject();
                    foreach (KeyValuePair<string, JsonNode?> item in report["summary"] as JsonObject ?? new JsonObject()) metrics[item.Key] = item.Value?.DeepClone();
                    JsonObject settings = new JsonObject { ["judge"] = config["judge"]?.GetValue<string>() };
                    entries.Add(Base("eval", settings, metrics));
                    break;
                }
            }

            return entries;
        }

        private static void Copy(JsonObject target, string name, JsonNode? value)
        {
            if (value == null) return;
            try
            {
                target[name] = value.GetValue<double>();
            }
            catch (Exception e) when (e is InvalidOperationException || e is FormatException)
            {
            }
        }

        private static string LabelOf(string runId, string kind, string dataset, string? scenario)
        {
            // Report names are <yyyyMMdd-HHmmss>-<kind>-<name>[-<label>]; the label is whatever follows the name.
            string name = kind == "load" ? (scenario ?? string.Empty) + "-" + dataset : dataset;
            string prefix = runId.Length > 16 ? runId.Substring(16) : runId;
            string head = kind + "-" + name;
            if (prefix.StartsWith(head, StringComparison.Ordinal)) return prefix.Substring(head.Length).TrimStart('-');
            return string.Empty;
        }

        private static string Str(JsonObject entry, string name)
        {
            return entry[name]?.GetValue<string>() ?? string.Empty;
        }

        private static string Truncate(string value, int max)
        {
            return value.Length <= max ? value : value.Substring(0, max);
        }

        #endregion
    }
}
