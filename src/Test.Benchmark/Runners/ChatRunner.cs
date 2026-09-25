namespace Test.Benchmark.Runners
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Benchmark.Datasets;
    using Test.Benchmark.Metrics;

    /// <summary>
    /// End-to-end chat benchmark: ask each labelled question through <c>POST /v1.0/assistants/{id}/chat</c>
    /// (non-streaming), then grade the answer with an independent judge. Reports accuracy on answerable questions,
    /// abstention on unanswerable ones, faithfulness to the injected context, citation precision and recall, and
    /// accuracy split by whether retrieval put the evidence in the prompt, which separates retrieval misses from
    /// generation misses. Repeats measure run-to-run variance; hand labels measure judge agreement.
    /// </summary>
    public class ChatRunner
    {
        #region Private-Members

        private readonly BenchmarkContext _Context;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="context">Benchmark context.</param>
        public ChatRunner(BenchmarkContext context)
        {
            _Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Run the benchmark.
        /// </summary>
        /// <param name="dataset">Dataset with gold answers.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Report.</returns>
        public async Task<ChatReport> RunAsync(BenchmarkDataset dataset, CancellationToken token)
        {
            BenchmarkArguments args = _Context.Arguments;
            int concurrency = Math.Max(1, args.GetInt("concurrency", 1));
            int repeats = Math.Max(1, args.GetInt("repeats", 1));
            int limit = args.GetInt("limit", 0);
            bool faithfulness = !args.GetFlag("no-faithfulness");
            JudgeClient? judge = JudgeClient.From(_Context.Http, args, _Context.OllamaUrl);

            AssistantVariant variant = AssistantVariant.From(args);
            variant.Citations = !args.GetFlag("no-citations");

            ChatReport report = new ChatReport { Dataset = dataset.Name, Environment = _Context.Environment, Assistant = variant.Describe() };
            report.Environment.DatasetHash = dataset.FileHash;
            report.Config["judge"] = judge != null ? judge.Description : "none";
            report.Config["concurrency"] = concurrency.ToString(CultureInfo.InvariantCulture);
            report.Config["repeats"] = repeats.ToString(CultureInfo.InvariantCulture);
            report.Config["faithfulness"] = faithfulness ? "judged" : "off";
            if (limit > 0) report.Config["limit"] = limit.ToString(CultureInfo.InvariantCulture);

            List<ProvisionedCollection> collections;
            Dictionary<string, string> assistants = new Dictionary<string, string>(StringComparer.Ordinal);
            string? existingAssistant = args.GetOptional("assistant-id");
            if (existingAssistant != null)
            {
                // Ask an existing assistant as it is configured (for example with an eval-import question set):
                // nothing is ingested or reconfigured, and relevance-based metrics are skipped for unlabelled questions.
                report.Config["assistantId"] = existingAssistant;
                report.Assistant = new SortedDictionary<string, string> { ["assistant"] = existingAssistant + " (existing, settings unchanged)" };
                collections = dataset.Corpora.Select(c => new ProvisionedCollection { Corpus = c, CollectionName = "existing" }).ToList();
                foreach (ProvisionedCollection collection in collections) assistants[collection.Corpus.Id] = existingAssistant;
            }
            else
            {
                Provisioner provisioner = new Provisioner(_Context);
                foreach (KeyValuePair<string, string> item in provisioner.Variant.Describe()) report.Config[item.Key] = item.Value;
                collections = await provisioner.ProvisionAsync(dataset, report.Ingest, token).ConfigureAwait(false);
                foreach (ProvisionedCollection collection in collections)
                    assistants[collection.Corpus.Id] = await provisioner.EnsureAssistantAsync(collection, variant, token).ConfigureAwait(false);
            }

            List<(ProvisionedCollection Collection, BenchmarkQuery Query)> work = collections
                .SelectMany(c => c.Corpus.Queries.Where(q => !string.IsNullOrEmpty(q.Answer)).Select(q => (c, q)))
                .ToList();
            if (limit > 0 && work.Count > limit) work = RetrievalRunner.StratifiedSample(work, limit).Select(w => (w.Item1, w.Item2)).ToList();

            Console.WriteLine("[chat] " + work.Count + " questions x " + repeats + " repeat(s), judge " + report.Config["judge"]);
            PrometheusSnapshot before = await PrometheusSnapshot.CaptureAsync(_Context.Http, _Context.MetricsUrl, token).ConfigureAwait(false);

            ConcurrentBag<ChatItem> items = new ConcurrentBag<ChatItem>();
            for (int repeat = 0; repeat < repeats; repeat++)
            {
                int repeatIndex = repeat;
                int done = 0;
                using SemaphoreSlim gate = new SemaphoreSlim(concurrency);
                List<Task> tasks = new List<Task>();
                foreach ((ProvisionedCollection collection, BenchmarkQuery query) in work)
                {
                    await gate.WaitAsync(token).ConfigureAwait(false);
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            items.Add(await AskAsync(collection, assistants[collection.Corpus.Id], query, repeatIndex, judge, faithfulness, token).ConfigureAwait(false));
                            int n = Interlocked.Increment(ref done);
                            if (n % 10 == 0) Console.WriteLine("  repeat " + (repeatIndex + 1) + ": answered " + n + "/" + work.Count);
                        }
                        finally
                        {
                            gate.Release();
                        }
                    }, token));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            report.Config["retriedChatCalls"] = _Context.Client.RetriedChatCalls.ToString(CultureInfo.InvariantCulture);
            await Task.Delay(_Context.MetricsSettleMs, token).ConfigureAwait(false);
            PrometheusSnapshot after = await PrometheusSnapshot.CaptureAsync(_Context.Http, _Context.MetricsUrl, token).ConfigureAwait(false);
            report.Stages = after.Since(before);

            report.Items = items.OrderBy(i => i.Repeat).ThenBy(i => i.Corpus, StringComparer.Ordinal).ThenBy(i => i.QueryId, StringComparer.Ordinal).ToList();
            List<ChatItem> first = report.Items.Where(i => i.Repeat == 0).ToList();
            report.Latency = LatencyStats.From(report.Items.Where(i => i.Error == null).Select(i => i.LatencyMs));
            report.Summary = Summarize(first);
            report.Intervals = Intervals(first);
            foreach (IGrouping<string, ChatItem> group in first.GroupBy(i => i.Type).OrderBy(g => g.Key, StringComparer.Ordinal))
                report.ByType[group.Key] = Summarize(group.ToList());
            foreach (IGrouping<string, ChatItem> group in first.GroupBy(i => i.Category).OrderBy(g => g.Key, StringComparer.Ordinal))
                report.ByCategory[group.Key] = Summarize(group.ToList());

            for (int repeat = 0; repeat < repeats; repeat++)
            {
                List<ChatItem> judged = report.Items.Where(i => i.Repeat == repeat && i.Correct.HasValue).ToList();
                if (judged.Count > 0) report.RepeatAccuracy.Add(Math.Round(judged.Average(i => i.Correct == true ? 1.0 : 0.0), 4));
            }

            if (report.RepeatAccuracy.Count > 1)
            {
                report.Summary["overallAccuracyMeanOfRepeats"] = Math.Round(report.RepeatAccuracy.Average(), 4);
                report.Summary["overallAccuracySdOfRepeats"] = Statistics.StandardDeviation(report.RepeatAccuracy);
            }

            string? labels = args.GetOptional("hand-labels");
            if (labels != null) report.JudgeAgreement = JudgeAgreement.Compute(labels, first);

            Console.WriteLine("[chat] accuracy " + Format(report.Summary, "accuracy") + "  abstention " + Format(report.Summary, "abstentionAccuracy")
                + "  faithfulness " + Format(report.Summary, "faithfulness") + "  evidence in prompt " + Format(report.Summary, "evidenceRetrievedRate")
                + "  citation P/R " + Format(report.Summary, "citationPrecision") + "/" + Format(report.Summary, "citationRecall") + "  p50 " + report.Latency.P50 + "ms");
            return report;
        }

        #endregion

        #region Private-Methods

        private async Task<ChatItem> AskAsync(ProvisionedCollection collection, string assistantId, BenchmarkQuery query, int repeat, JudgeClient? judge, bool faithfulness, CancellationToken token)
        {
            JsonObject body = new JsonObject
            {
                ["messages"] = QueryRequestBuilder.Messages(query),
                ["stream"] = false
            };
            QueryRequestBuilder.AddScope(body, query, collection);

            (TimedCall call, ChatResult? result) = await _Context.Client.ChatAsync(assistantId, body, token).ConfigureAwait(false);
            // Imported question sets (eval-import) carry gold answers without relevance labels, so the gold answer
            // decides answerability; relevance-based metrics only use labelled questions.
            bool unanswerable = query.GoldIsNotInCorpus || (!query.Answerable && string.IsNullOrEmpty(query.Answer));
            ChatItem item = new ChatItem
            {
                Corpus = collection.Corpus.Id,
                QueryId = query.Id,
                Repeat = repeat,
                Type = query.Type,
                Category = query.Category,
                Question = query.Text,
                Gold = query.Answer ?? string.Empty,
                StatusCode = call.StatusCode,
                LatencyMs = Math.Round(call.ElapsedMs, 1),
                Relevant = new List<string>(query.Relevant),
                Unanswerable = unanswerable
            };

            if (result == null)
            {
                item.Error = call.Describe();
                return item;
            }

            item.Answer = JudgeClient.StripThinking(result.Answer).Trim();
            item.PromptTokens = result.PromptTokens;
            item.CompletionTokens = result.CompletionTokens;
            item.AnswerabilityDecision = result.AnswerabilityDecision;
            item.Retrieved = RetrievalMetrics.DocumentRanking(result.Chunks.Select(c => collection.ToDatasetId(c.DocumentId)));
            item.CitationsPresent = result.CitationsPresent;
            item.Cited = result.CitedDocumentIds.Select(collection.ToDatasetId).Distinct().ToList();
            item.ContextEvidence = EvidenceMatcher.Recall(query.Evidence, result.Chunks.Select(c => c.MergedContent));

            if (judge == null) return item;
            try
            {
                item.Correct = await judge.GradeAsync(query.Text, item.Gold, item.Answer, unanswerable, token).ConfigureAwait(false);
                if (faithfulness && !unanswerable && result.Chunks.Count > 0)
                {
                    string context = string.Join("\n\n---\n\n", result.Chunks.Select(c => c.MergedContent));
                    item.Faithful = await judge.FaithfulAsync(query.Text, context, item.Answer, token).ConfigureAwait(false);
                }
            }
            catch (InvalidOperationException e)
            {
                item.Error = "judge: " + e.Message;
            }
            catch (System.Net.Http.HttpRequestException e)
            {
                item.Error = "judge: " + e.Message;
            }

            return item;
        }

        private static Dictionary<string, double> Summarize(List<ChatItem> items)
        {
            Dictionary<string, double> summary = new Dictionary<string, double>();
            List<ChatItem> ok = items.Where(i => i.StatusCode >= 200 && i.StatusCode < 300).ToList();
            List<ChatItem> answerable = ok.Where(i => !i.Unanswerable).ToList();
            List<ChatItem> negatives = ok.Where(i => i.Unanswerable).ToList();
            List<ChatItem> labelled = answerable.Where(i => i.Relevant.Count > 0).ToList();

            summary["questions"] = items.Count;
            summary["errors"] = items.Count - ok.Count;
            summary["answerable"] = answerable.Count;
            summary["unanswerable"] = negatives.Count;

            List<ChatItem> judgedAnswerable = answerable.Where(i => i.Correct.HasValue).ToList();
            List<ChatItem> judgedNegative = negatives.Where(i => i.Correct.HasValue).ToList();
            if (judgedAnswerable.Count > 0) summary["accuracy"] = Rate(judgedAnswerable, i => i.Correct == true);
            if (judgedNegative.Count > 0) summary["abstentionAccuracy"] = Rate(judgedNegative, i => i.Correct == true);
            if (judgedAnswerable.Count + judgedNegative.Count > 0) summary["overallAccuracy"] = Rate(judgedAnswerable.Concat(judgedNegative).ToList(), i => i.Correct == true);
            summary["unjudged"] = ok.Count(i => !i.Correct.HasValue);

            List<ChatItem> faithful = answerable.Where(i => i.Faithful.HasValue).ToList();
            if (faithful.Count > 0) summary["faithfulness"] = Rate(faithful, i => i.Faithful == true);

            if (labelled.Count > 0)
            {
                summary["evidenceRetrievedRate"] = Rate(labelled, i => i.Retrieved.Intersect(i.Relevant).Any());
                summary["contextRecall"] = Math.Round(labelled.Average(i => (double)i.Retrieved.Intersect(i.Relevant).Count() / i.Relevant.Count), 4);
                List<ChatItem> withEvidence = labelled.Where(i => i.ContextEvidence.HasValue).ToList();
                if (withEvidence.Count > 0) summary["contextEvidence"] = Math.Round(withEvidence.Average(i => i.ContextEvidence!.Value), 4);

                List<ChatItem> citing = labelled.Where(i => i.Cited.Count > 0).ToList();
                summary["citationRate"] = Math.Round((double)citing.Count / labelled.Count, 4);
                if (citing.Count > 0) summary["citationPrecision"] = Math.Round(citing.Average(i => (double)i.Cited.Intersect(i.Relevant).Count() / i.Cited.Count), 4);
                summary["citationRecall"] = Math.Round(labelled.Average(i => (double)i.Cited.Intersect(i.Relevant).Count() / i.Relevant.Count), 4);

                List<ChatItem> hit = judgedAnswerable.Where(i => i.Relevant.Count > 0 && i.Retrieved.Intersect(i.Relevant).Any()).ToList();
                List<ChatItem> miss = judgedAnswerable.Where(i => i.Relevant.Count > 0 && !i.Retrieved.Intersect(i.Relevant).Any()).ToList();
                if (hit.Count > 0) summary["accuracyWhenEvidenceRetrieved"] = Rate(hit, i => i.Correct == true);
                if (miss.Count > 0) summary["accuracyWhenEvidenceMissed"] = Rate(miss, i => i.Correct == true);
                summary["evidenceMissedCount"] = miss.Count;
            }

            if (ok.Count > 0)
            {
                summary["meanPromptTokens"] = Math.Round(ok.Average(i => i.PromptTokens), 1);
                summary["meanCompletionTokens"] = Math.Round(ok.Average(i => i.CompletionTokens), 1);
            }

            return summary;
        }

        private static Dictionary<string, ConfidenceInterval> Intervals(List<ChatItem> items)
        {
            Dictionary<string, ConfidenceInterval> intervals = new Dictionary<string, ConfidenceInterval>();
            List<ChatItem> ok = items.Where(i => i.Error == null).ToList();
            Add(intervals, "accuracy", ok.Where(i => !i.Unanswerable && i.Correct.HasValue).Select(i => i.Correct == true ? 1.0 : 0.0).ToList());
            Add(intervals, "abstentionAccuracy", ok.Where(i => i.Unanswerable && i.Correct.HasValue).Select(i => i.Correct == true ? 1.0 : 0.0).ToList());
            Add(intervals, "overallAccuracy", ok.Where(i => i.Correct.HasValue).Select(i => i.Correct == true ? 1.0 : 0.0).ToList());
            Add(intervals, "faithfulness", ok.Where(i => i.Faithful.HasValue).Select(i => i.Faithful == true ? 1.0 : 0.0).ToList());
            Add(intervals, "evidenceRetrievedRate", ok.Where(i => i.Relevant.Count > 0).Select(i => i.Retrieved.Intersect(i.Relevant).Any() ? 1.0 : 0.0).ToList());
            return intervals;
        }

        private static void Add(Dictionary<string, ConfidenceInterval> intervals, string name, List<double> values)
        {
            ConfidenceInterval? interval = Statistics.BootstrapMean(values);
            if (interval != null) intervals[name] = interval;
        }

        private static double Rate(List<ChatItem> items, Func<ChatItem, bool> predicate)
        {
            return items.Count == 0 ? 0 : Math.Round(items.Count(predicate) / (double)items.Count, 4);
        }

        private static string Format(Dictionary<string, double> metrics, string name)
        {
            return metrics.TryGetValue(name, out double value) ? value.ToString("F3") : "n/a";
        }

        #endregion
    }
}
