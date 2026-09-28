namespace Test.Benchmark.Runners
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Benchmark.Datasets;
    using Test.Benchmark.Metrics;

    /// <summary>
    /// Retrieval benchmark: ingest a labelled dataset, then run every query through each assistant configuration's
    /// retrieval pipeline (<c>POST /v1.0/assistants/{id}/retrieve</c>, which shares its code with chat) and score
    /// the rankings at document level (Hit@1, Recall@k, All@k, MRR@10, nDCG@10), at chunk level (evidence recall)
    /// and per pipeline stage, with bootstrap confidence intervals, score separation on unanswerable questions,
    /// filter precision, pipeline flag rates, latency and a server-side stage breakdown.
    /// </summary>
    public class RetrievalRunner
    {
        #region Public-Members

        /// <summary>
        /// Metric names, in display order.
        /// </summary>
        public static readonly string[] MetricNames = new string[]
        {
            "hit@1", "recall@1", "recall@5", "recall@10", "all@5", "all@10", "mrr@10", "ndcg@10", "evidence@5", "evidence@10", "context_evidence", "context_precision"
        };

        /// <summary>
        /// Metrics reported per stage.
        /// </summary>
        public static readonly string[] StageMetricNames = new string[] { "ndcg@10", "recall@10", "evidence@10", "evidence@all" };

        /// <summary>
        /// Parameters a sweep can vary.
        /// </summary>
        public static readonly string[] SweepParameters = new string[] { "threshold", "k", "text-weight", "neighbors", "rerank-k", "rerank-threshold", "fulltext-type", "rrf-k", "candidate-pool", "recency-weight", "fusion" };

        #endregion

        #region Private-Members

        private readonly BenchmarkContext _Context;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="context">Benchmark context.</param>
        public RetrievalRunner(BenchmarkContext context)
        {
            _Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Run the benchmark.
        /// </summary>
        /// <param name="dataset">Dataset.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Report.</returns>
        public async Task<RetrievalReport> RunAsync(BenchmarkDataset dataset, CancellationToken token)
        {
            BenchmarkArguments args = _Context.Arguments;
            int concurrency = Math.Max(1, args.GetInt("concurrency", 4));
            List<(string Label, AssistantVariant Variant)> configurations = BuildConfigurations(args);

            RetrievalReport report = new RetrievalReport
            {
                Dataset = dataset.Name,
                Description = dataset.Description,
                Environment = _Context.Environment
            };
            report.Environment.DatasetHash = dataset.FileHash;
            report.Config["concurrency"] = concurrency.ToString(CultureInfo.InvariantCulture);
            report.Config["configurations"] = string.Join("; ", configurations.Select(c => c.Label));
            if (args.GetOptional("limit") != null) report.Config["limit"] = args.Get("limit", "");

            Provisioner provisioner = new Provisioner(_Context);
            foreach (KeyValuePair<string, string> item in provisioner.Variant.Describe()) report.Config[item.Key] = item.Value;

            Console.WriteLine("[retrieval] " + dataset.Name + ": " + dataset.Corpora.Count + " corpora, " + dataset.Corpora.Sum(c => c.Documents.Count)
                + " documents, " + dataset.Corpora.Sum(c => c.Queries.Count) + " queries, " + configurations.Count + " configuration(s)");
            List<ProvisionedCollection> collections = await provisioner.ProvisionAsync(dataset, report.Ingest, token).ConfigureAwait(false);

            if (args.GetFlag("reachability"))
                report.Reachability = await ReachabilityAnalyzer.AnalyzeAsync(provisioner, collections, token).ConfigureAwait(false);

            int limit = args.GetInt("limit", 0);
            foreach ((string label, AssistantVariant variant) in configurations)
            {
                Dictionary<string, string> assistants = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (ProvisionedCollection collection in collections)
                {
                    assistants[collection.Corpus.Id] = await provisioner.EnsureAssistantAsync(collection, variant, token).ConfigureAwait(false);
                }

                PrometheusSnapshot before = await PrometheusSnapshot.CaptureAsync(_Context.Http, _Context.MetricsUrl, token).ConfigureAwait(false);
                List<QueryOutcome> outcomes = await RunConfigurationAsync(collections, assistants, label, concurrency, limit, token).ConfigureAwait(false);
                await Task.Delay(_Context.MetricsSettleMs, token).ConfigureAwait(false);
                PrometheusSnapshot after = await PrometheusSnapshot.CaptureAsync(_Context.Http, _Context.MetricsUrl, token).ConfigureAwait(false);

                ModeSummary summary = Summarize(label, outcomes);
                summary.Assistant = variant.Describe();
                summary.Stages = after.Since(before);
                report.Modes.Add(summary);
                report.Outcomes.AddRange(outcomes);
                Console.WriteLine("[retrieval] " + label.PadRight(24) + " ndcg@10 " + Format(summary, "ndcg@10") + "  recall@10 " + Format(summary, "recall@10")
                    + "  evidence@10 " + Format(summary, "evidence@10") + "  p50 " + summary.Latency.P50 + "ms  errors " + summary.Errors);
            }

            return report;
        }

        #endregion

        #region Private-Methods

        private static List<(string Label, AssistantVariant Variant)> BuildConfigurations(BenchmarkArguments args)
        {
            List<(string, AssistantVariant)> configurations = new List<(string, AssistantVariant)>();
            List<string> modes = args.GetList("modes", "Vector,FullText,Hybrid");
            string? sweep = args.GetOptional("sweep");

            foreach (string mode in modes)
            {
                AssistantVariant baseline = AssistantVariant.From(args, mode);
                if (string.IsNullOrEmpty(sweep))
                {
                    configurations.Add((mode, baseline));
                    continue;
                }

                int eq = sweep.IndexOf('=');
                if (eq <= 0) throw new ArgumentException("--sweep takes name=v1,v2,... (one of " + string.Join(", ", SweepParameters) + ").");
                string parameter = sweep.Substring(0, eq).Trim().ToLowerInvariant();
                if (!SweepParameters.Contains(parameter)) throw new ArgumentException("Unknown sweep parameter '" + parameter + "'.");
                foreach (string value in sweep.Substring(eq + 1).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    AssistantVariant variant = baseline.WithMode(mode);
                    switch (parameter)
                    {
                        case "threshold": variant.ScoreThreshold = double.Parse(value, CultureInfo.InvariantCulture); break;
                        case "k": variant.TopK = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "text-weight": variant.TextWeight = double.Parse(value, CultureInfo.InvariantCulture); break;
                        case "neighbors": variant.IncludeNeighbors = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "rerank-k": variant.RerankTopK = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "rerank-threshold": variant.RerankThreshold = double.Parse(value, CultureInfo.InvariantCulture); break;
                        case "fulltext-type": variant.FullTextSearchType = value; break;
                        case "rrf-k": variant.RrfK = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "candidate-pool": variant.CandidatePool = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "recency-weight": variant.RecencyWeight = double.Parse(value, CultureInfo.InvariantCulture); break;
                        case "fusion": variant.FusionStrategy = value; break;
                    }

                    configurations.Add((mode + " " + parameter + "=" + value, variant));
                }
            }

            return configurations;
        }

        private async Task<List<QueryOutcome>> RunConfigurationAsync(List<ProvisionedCollection> collections, Dictionary<string, string> assistants, string label, int concurrency, int limit, CancellationToken token)
        {
            List<(ProvisionedCollection Collection, BenchmarkQuery Query)> work = collections
                .SelectMany(c => c.Corpus.Queries.Select(q => (c, q)))
                .ToList();
            if (limit > 0 && work.Count > limit) work = StratifiedSample(work, limit);

            ConcurrentBag<QueryOutcome> outcomes = new ConcurrentBag<QueryOutcome>();
            using SemaphoreSlim gate = new SemaphoreSlim(concurrency);
            List<Task> tasks = new List<Task>();
            int done = 0;
            foreach ((ProvisionedCollection collection, BenchmarkQuery query) in work)
            {
                await gate.WaitAsync(token).ConfigureAwait(false);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        outcomes.Add(await RunQueryAsync(collection, assistants[collection.Corpus.Id], query, label, token).ConfigureAwait(false));
                        int n = Interlocked.Increment(ref done);
                        if (n % 100 == 0) Console.WriteLine("  " + label + ": " + n + "/" + work.Count);
                    }
                    finally
                    {
                        gate.Release();
                    }
                }, token));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return outcomes.OrderBy(o => o.Corpus, StringComparer.Ordinal).ThenBy(o => o.QueryId, StringComparer.Ordinal).ToList();
        }

        private async Task<QueryOutcome> RunQueryAsync(ProvisionedCollection collection, string assistantId, BenchmarkQuery query, string label, CancellationToken token)
        {
            JsonObject body = new JsonObject
            {
                ["messages"] = QueryRequestBuilder.Messages(query),
                ["include_stages"] = true,
                ["include_answerability"] = true
            };
            QueryRequestBuilder.AddScope(body, query, collection);

            (TimedCall call, RetrieveResult? result) = await _Context.Client.RetrieveAsync(assistantId, body, token).ConfigureAwait(false);
            QueryOutcome outcome = new QueryOutcome
            {
                Corpus = collection.Corpus.Id,
                QueryId = query.Id,
                Type = query.Type,
                Category = query.Category,
                Mode = label,
                StatusCode = call.StatusCode,
                LatencyMs = Math.Round(call.ElapsedMs, 2),
                Relevant = new List<string>(query.Relevant)
            };

            if (result == null)
            {
                outcome.Error = call.Describe();
                return outcome;
            }

            outcome.ServerMs["gate"] = result.GateDurationMs;
            outcome.ServerMs["rewrite"] = result.QueryRewriteDurationMs;
            outcome.ServerMs["retrieval"] = result.RetrievalDurationMs;
            outcome.ServerMs["rerank"] = result.RerankDurationMs;
            outcome.ServerMs["answerability"] = result.AnswerabilityDurationMs;
            outcome.ServerMs["total"] = result.TotalDurationMs;
            outcome.GateDecision = result.GateDecision;
            outcome.AnswerabilityDecision = result.AnswerabilityDecision;
            outcome.QueryCount = result.Queries.Count;
            if (result.HybridFallbackRan) outcome.Flags.Add("hybridFallback");
            if (result.EmbeddingFailed) outcome.Flags.Add("embeddingFailed");
            if (result.RerankParseFailed) outcome.Flags.Add("rerankParseFailed");
            if (result.AnswerabilityParseFailed) outcome.Flags.Add("answerabilityParseFailed");

            List<RetrievedChunk> chunks = result.Chunks;
            outcome.ChunksReturned = chunks.Count;
            outcome.Ranked = RetrievalMetrics.DocumentRanking(chunks.Select(c => collection.ToDatasetId(c.DocumentId)));
            outcome.TopScore = chunks.Count > 0 ? chunks.Max(c => c.Score) : 0.0;
            outcome.TopVectorScore = chunks.Count > 0 ? chunks.Max(c => c.VectorScore ?? 0.0) : 0.0;
            RetrieveStage? scored = result.Stages?.FirstOrDefault(s => s.Stage == "rerank_scored");
            if (scored != null && scored.Chunks.Count > 0) outcome.TopRerankScore = scored.Chunks.Max(c => c.RerankScore ?? 0.0);

            if (QueryRequestBuilder.HasScope(query) && chunks.Count > 0)
            {
                Dictionary<string, BenchmarkDocument> documents = collection.Corpus.Documents.ToDictionary(d => d.Id, StringComparer.Ordinal);
                int inScope = chunks.Count(c => QueryRequestBuilder.InScope(query, documents.TryGetValue(collection.ToDatasetId(c.DocumentId), out BenchmarkDocument? d) ? d : null));
                outcome.FilterPrecision = Math.Round((double)inScope / chunks.Count, 4);
            }

            if (!query.Answerable) return outcome;

            Score(outcome.Metrics, query, outcome.Ranked, chunks);
            outcome.Metrics["context_evidence"] = EvidenceMatcher.Recall(query.Evidence, chunks.Select(c => c.MergedContent)) ?? 0.0;
            if (chunks.Count > 0)
                outcome.Metrics["context_precision"] = Math.Round(chunks.Count(c => query.GainOf(collection.ToDatasetId(c.DocumentId)) > 0) / (double)chunks.Count, 4);
            if (query.Evidence == null || query.Evidence.Count == 0)
            {
                outcome.Metrics.Remove("evidence@5");
                outcome.Metrics.Remove("evidence@10");
                outcome.Metrics.Remove("context_evidence");
            }

            if (result.Stages != null)
            {
                RetrieveStage? firstSearch = result.Stages.FirstOrDefault(s => s.Stage == "search");
                if (firstSearch != null) outcome.StageMetrics["1-search"] = StageScore(query, collection, firstSearch.Chunks);
                RetrieveStage? fused = result.Stages.FirstOrDefault(s => s.Stage == "fused");
                if (fused != null) outcome.StageMetrics["2-fused"] = StageScore(query, collection, fused.Chunks);
                RetrieveStage? reranked = result.Stages.FirstOrDefault(s => s.Stage == "rerank");
                if (reranked != null) outcome.StageMetrics["3-rerank"] = StageScore(query, collection, reranked.Chunks);
            }

            return outcome;
        }

        private static void Score(Dictionary<string, double> metrics, BenchmarkQuery query, List<string> ranked, List<RetrievedChunk> chunks)
        {
            HashSet<string> relevant = new HashSet<string>(query.Relevant, StringComparer.Ordinal);
            metrics["hit@1"] = ranked.Count > 0 && relevant.Contains(ranked[0]) ? 1.0 : 0.0;
            metrics["recall@1"] = RetrievalMetrics.RecallAtK(ranked, relevant, 1);
            metrics["recall@5"] = RetrievalMetrics.RecallAtK(ranked, relevant, 5);
            metrics["recall@10"] = RetrievalMetrics.RecallAtK(ranked, relevant, 10);
            metrics["all@5"] = RetrievalMetrics.AllAtK(ranked, relevant, 5);
            metrics["all@10"] = RetrievalMetrics.AllAtK(ranked, relevant, 10);
            metrics["mrr@10"] = RetrievalMetrics.ReciprocalRank(ranked, relevant, 10);
            metrics["ndcg@10"] = RetrievalMetrics.NdcgAtK(ranked, query.GainOf, IdealGains(query), 10);
            metrics["evidence@5"] = EvidenceMatcher.Recall(query.Evidence, chunks.Take(5).Select(c => c.Content ?? string.Empty)) ?? 0.0;
            metrics["evidence@10"] = EvidenceMatcher.Recall(query.Evidence, chunks.Take(10).Select(c => c.Content ?? string.Empty)) ?? 0.0;
        }

        private static Dictionary<string, double> StageScore(BenchmarkQuery query, ProvisionedCollection collection, List<RetrievedChunk> chunks)
        {
            List<string> ranked = RetrievalMetrics.DocumentRanking(chunks.Select(c => collection.ToDatasetId(c.DocumentId)));
            HashSet<string> relevant = new HashSet<string>(query.Relevant, StringComparer.Ordinal);
            Dictionary<string, double> metrics = new Dictionary<string, double>
            {
                ["ndcg@10"] = RetrievalMetrics.NdcgAtK(ranked, query.GainOf, IdealGains(query), 10),
                ["recall@10"] = RetrievalMetrics.RecallAtK(ranked, relevant, 10)
            };
            double? at10 = EvidenceMatcher.Recall(query.Evidence, chunks.Take(10).Select(c => c.Content ?? string.Empty));
            double? all = EvidenceMatcher.Recall(query.Evidence, chunks.Select(c => c.Content ?? string.Empty));
            if (at10.HasValue) metrics["evidence@10"] = at10.Value;
            if (all.HasValue) metrics["evidence@all"] = all.Value;
            return metrics;
        }

        private static IEnumerable<int> IdealGains(BenchmarkQuery query)
        {
            HashSet<string> ids = new HashSet<string>(query.Relevant, StringComparer.Ordinal);
            if (query.Grades != null) foreach (string id in query.Grades.Keys) ids.Add(id);
            return ids.Select(query.GainOf);
        }

        private static ModeSummary Summarize(string label, List<QueryOutcome> outcomes)
        {
            List<QueryOutcome> ok = outcomes.Where(o => o.Error == null).ToList();
            List<QueryOutcome> scored = ok.Where(o => o.Relevant.Count > 0).ToList();
            List<QueryOutcome> negatives = ok.Where(o => o.Relevant.Count == 0).ToList();

            ModeSummary summary = new ModeSummary
            {
                Mode = label,
                Queries = scored.Count,
                NegativeQueries = negatives.Count,
                Errors = outcomes.Count - ok.Count,
                Latency = LatencyStats.From(ok.Select(o => o.LatencyMs))
            };

            foreach (string name in MetricNames)
            {
                List<double> values = scored.Where(o => o.Metrics.ContainsKey(name)).Select(o => o.Metrics[name]).ToList();
                ConfidenceInterval? interval = Statistics.BootstrapMean(values);
                if (interval != null) summary.Metrics[name] = interval;
            }

            foreach (IGrouping<string, QueryOutcome> group in scored.GroupBy(o => o.Type).OrderBy(g => g.Key, StringComparer.Ordinal))
                summary.ByType[group.Key] = Means(group.ToList(), MetricNames);
            foreach (IGrouping<string, QueryOutcome> group in scored.GroupBy(o => o.Category).OrderBy(g => g.Key, StringComparer.Ordinal))
                summary.ByCategory[group.Key] = Means(group.ToList(), MetricNames);

            foreach (string stage in scored.SelectMany(o => o.StageMetrics.Keys).Distinct().OrderBy(s => s, StringComparer.Ordinal))
            {
                List<QueryOutcome> withStage = scored.Where(o => o.StageMetrics.ContainsKey(stage)).ToList();
                Dictionary<string, double> means = new Dictionary<string, double> { ["count"] = withStage.Count };
                foreach (string name in StageMetricNames)
                {
                    List<double> values = withStage.Where(o => o.StageMetrics[stage].ContainsKey(name)).Select(o => o.StageMetrics[stage][name]).ToList();
                    if (values.Count > 0) means[name] = Math.Round(values.Average(), 4);
                }

                summary.ByStage[stage] = means;
            }

            summary.Separation["meanTopScoreAnswerable"] = scored.Count > 0 ? Math.Round(scored.Average(o => o.TopScore), 4) : null;
            summary.Separation["meanTopScoreNegative"] = negatives.Count > 0 ? Math.Round(negatives.Average(o => o.TopScore), 4) : null;
            summary.Separation["scoreAuroc"] = Statistics.Auroc(scored.Select(o => o.TopScore).ToList(), negatives.Select(o => o.TopScore).ToList());
            summary.Separation["vectorScoreAuroc"] = scored.Concat(negatives).Any(o => o.TopVectorScore > 0)
                ? Statistics.Auroc(scored.Select(o => o.TopVectorScore).ToList(), negatives.Select(o => o.TopVectorScore).ToList())
                : null;
            if (scored.Concat(negatives).Any(o => o.TopRerankScore.HasValue))
            {
                summary.Separation["rerankScoreAuroc"] = Statistics.Auroc(
                    scored.Select(o => o.TopRerankScore ?? 0.0).ToList(),
                    negatives.Select(o => o.TopRerankScore ?? 0.0).ToList());
            }

            summary.Separation["answerableEmptyRate"] = scored.Count > 0 ? Math.Round(scored.Count(o => o.ChunksReturned == 0) / (double)scored.Count, 4) : null;
            summary.Separation["negativeEmptyRate"] = negatives.Count > 0 ? Math.Round(negatives.Count(o => o.ChunksReturned == 0) / (double)negatives.Count, 4) : null;

            List<QueryOutcome> checkedAnswerability = ok.Where(o => !string.IsNullOrEmpty(o.AnswerabilityDecision) && o.AnswerabilityDecision != "not_checked").ToList();
            if (checkedAnswerability.Count > 0)
            {
                int truePositive = checkedAnswerability.Count(o => o.Relevant.Count == 0 && o.AnswerabilityDecision == "unsupported");
                int predicted = checkedAnswerability.Count(o => o.AnswerabilityDecision == "unsupported");
                int actual = checkedAnswerability.Count(o => o.Relevant.Count == 0);
                summary.Answerability["checked"] = checkedAnswerability.Count;
                summary.Answerability["unsupportedPrecision"] = predicted > 0 ? Math.Round((double)truePositive / predicted, 4) : 0;
                summary.Answerability["unsupportedRecall"] = actual > 0 ? Math.Round((double)truePositive / actual, 4) : 0;
                summary.Answerability["answerableMarkedUnsupported"] = Math.Round(checkedAnswerability.Count(o => o.Relevant.Count > 0 && o.AnswerabilityDecision == "unsupported")
                    / Math.Max(1.0, checkedAnswerability.Count(o => o.Relevant.Count > 0)), 4);
            }

            foreach (string flag in new string[] { "embeddingFailed", "hybridFallback", "rerankParseFailed", "answerabilityParseFailed" })
                summary.FlagRates[flag] = ok.Count > 0 ? Math.Round(ok.Count(o => o.Flags.Contains(flag)) / (double)ok.Count, 4) : 0;

            List<double> precision = ok.Where(o => o.FilterPrecision.HasValue).Select(o => o.FilterPrecision!.Value).ToList();
            if (precision.Count > 0) summary.FilterPrecision = Math.Round(precision.Average(), 4);

            foreach (string stage in new string[] { "gate", "rewrite", "retrieval", "rerank", "answerability", "total" })
            {
                List<double> values = ok.Where(o => o.ServerMs.ContainsKey(stage)).Select(o => o.ServerMs[stage]).ToList();
                if (values.Count > 0) summary.ServerMs[stage] = Math.Round(values.Average(), 1);
            }

            return summary;
        }

        private static Dictionary<string, double> Means(List<QueryOutcome> outcomes, string[] names)
        {
            Dictionary<string, double> means = new Dictionary<string, double> { ["count"] = outcomes.Count };
            foreach (string name in names)
            {
                List<double> values = outcomes.Where(o => o.Metrics.ContainsKey(name)).Select(o => o.Metrics[name]).ToList();
                if (values.Count > 0) means[name] = Math.Round(values.Average(), 4);
            }

            return means;
        }

        /// <summary>
        /// Round-robin across query types so a limited run keeps the type mix.
        /// </summary>
        /// <param name="work">Work items with their query.</param>
        /// <param name="limit">Maximum items.</param>
        /// <returns>Sample.</returns>
        internal static List<(ProvisionedCollection, BenchmarkQuery)> StratifiedSample(List<(ProvisionedCollection Collection, BenchmarkQuery Query)> work, int limit)
        {
            List<Queue<(ProvisionedCollection, BenchmarkQuery)>> queues = work
                .GroupBy(w => w.Query.Type)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new Queue<(ProvisionedCollection, BenchmarkQuery)>(g.Select(x => (x.Collection, x.Query))))
                .ToList();
            List<(ProvisionedCollection, BenchmarkQuery)> picked = new List<(ProvisionedCollection, BenchmarkQuery)>();
            while (picked.Count < limit && queues.Any(q => q.Count > 0))
            {
                foreach (Queue<(ProvisionedCollection, BenchmarkQuery)> queue in queues)
                {
                    if (picked.Count >= limit) break;
                    if (queue.Count > 0) picked.Add(queue.Dequeue());
                }
            }

            return picked;
        }

        private static string Format(ModeSummary summary, string name)
        {
            return summary.Metrics.TryGetValue(name, out ConfidenceInterval? value) ? value.Mean.ToString("F3") : "n/a";
        }

        #endregion
    }
}
