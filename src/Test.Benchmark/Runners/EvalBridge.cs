namespace Test.Benchmark.Runners
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Benchmark.Datasets;
    using Test.Benchmark.Metrics;

    /// <summary>
    /// Bridges benchmark datasets and AssistantHub's in-product Eval. <c>eval-export</c> turns a dataset's questions
    /// into EvalFacts on a benchmark assistant, so the same question set can be run from the dashboard.
    /// <c>eval-run</c> also starts an Eval run, waits for it, and grades every Eval answer again with the
    /// independent judge, reporting agreement and Cohen's kappa. In-product Eval judges with the assistant's own
    /// completion endpoint, so low agreement means its pass rate should not be trusted.
    /// </summary>
    public class EvalBridge
    {
        #region Public-Members

        /// <summary>
        /// Expected fact used for unanswerable questions.
        /// </summary>
        public const string DeclineFact = "The response states that the information is not available in the provided documents, or that it does not know.";

        #endregion

        #region Private-Members

        private readonly BenchmarkContext _Context;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="context">Benchmark context.</param>
        public EvalBridge(BenchmarkContext context)
        {
            _Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Create EvalFacts for every question with a gold answer, replacing the assistant's existing facts.
        /// </summary>
        /// <param name="dataset">Dataset (single corpus).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assistant id and the facts created, keyed by question text.</returns>
        public async Task<(string AssistantId, Dictionary<string, BenchmarkQuery> Facts)> ExportAsync(BenchmarkDataset dataset, CancellationToken token)
        {
            if (dataset.Corpora.Count != 1) throw new ArgumentException("eval-export and eval-run take a single-corpus dataset.");
            Provisioner provisioner = new Provisioner(_Context);
            IngestSummary ingest = new IngestSummary();
            ProvisionedCollection collection = (await provisioner.ProvisionAsync(dataset, ingest, token).ConfigureAwait(false))[0];
            AssistantVariant variant = AssistantVariant.From(_Context.Arguments);
            string assistantId = await provisioner.EnsureAssistantAsync(collection, variant, token).ConfigureAwait(false);

            foreach (JsonNode fact in await _Context.Client.ListAllAsync("/v1.0/eval/facts?assistantId=" + assistantId, token).ConfigureAwait(false))
            {
                string? id = fact["Id"]?.GetValue<string>();
                if (id != null) await _Context.Client.SendAsync(HttpMethod.Delete, "/v1.0/eval/facts/" + id, null, token).ConfigureAwait(false);
            }

            int limit = _Context.Arguments.GetInt("limit", 0);
            List<BenchmarkQuery> queries = collection.Corpus.Queries
                .Where(q => !string.IsNullOrEmpty(q.Answer) && (q.Conversation == null || q.Conversation.Count == 0) && q.MetadataFilter == null && q.AttachedDocuments == null)
                .ToList();
            if (limit > 0 && queries.Count > limit)
                queries = RetrievalRunner.StratifiedSample(queries.Select(q => (collection, q)).ToList(), limit).Select(w => w.Item2).ToList();

            Dictionary<string, BenchmarkQuery> facts = new Dictionary<string, BenchmarkQuery>(StringComparer.Ordinal);
            foreach (BenchmarkQuery query in queries)
            {
                if (facts.ContainsKey(query.Text)) continue;
                string expected = query.Answerable ? query.Answer! : DeclineFact;
                await _Context.Client.SendJsonAsync(HttpMethod.Put, "/v1.0/eval/facts", new JsonObject
                {
                    ["AssistantId"] = assistantId,
                    ["Category"] = query.Answerable ? query.Category : "unanswerable",
                    ["Question"] = query.Text,
                    ["ExpectedFacts"] = new JsonArray { expected }.ToJsonString()
                }, token).ConfigureAwait(false);
                facts[query.Text] = query;
            }

            Console.WriteLine("[eval] " + facts.Count + " EvalFacts on assistant " + assistantId + " (questions with conversations, filters or attachments are skipped: Eval asks single questions)");
            return (assistantId, facts);
        }

        /// <summary>
        /// Turn an assistant's in-product EvalFacts into a harness dataset (questions and gold answers, no relevance
        /// labels), so a team's existing Eval set can be run with <c>chat --assistant-id</c> and the independent judge.
        /// </summary>
        /// <param name="assistantId">Assistant whose facts to read.</param>
        /// <param name="output">Output dataset path.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Questions written.</returns>
        public async Task<int> ImportAsync(string assistantId, string output, CancellationToken token)
        {
            BenchmarkCorpus corpus = new BenchmarkCorpus { Id = "eval" };
            foreach (JsonNode fact in await _Context.Client.ListAllAsync("/v1.0/eval/facts?assistantId=" + Uri.EscapeDataString(assistantId), token).ConfigureAwait(false))
            {
                string question = fact["Question"]?.GetValue<string>() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(question)) continue;
                string category = fact["Category"]?.GetValue<string>() ?? "factual_lookup";
                string expected = fact["ExpectedFacts"]?.GetValue<string>() ?? string.Empty;
                try
                {
                    if (JsonNode.Parse(expected) is JsonArray facts)
                        expected = string.Join("; ", facts.Select(f => f?.GetValue<string>() ?? string.Empty).Where(f => f.Length > 0));
                }
                catch (System.Text.Json.JsonException)
                {
                }

                bool unanswerable = string.Equals(category, "unanswerable", StringComparison.OrdinalIgnoreCase);
                corpus.Queries.Add(new BenchmarkQuery
                {
                    Id = fact["Id"]?.GetValue<string>() ?? ("fact-" + corpus.Queries.Count),
                    Text = question,
                    Type = unanswerable ? "negative" : "eval",
                    Category = category,
                    Answer = unanswerable ? BenchmarkQuery.NotInCorpus : expected
                });
            }

            BenchmarkDataset dataset = new BenchmarkDataset
            {
                Name = "eval-" + assistantId,
                Description = "EvalFacts imported from assistant " + assistantId + ": questions and expected facts only (no documents or relevance labels). Run with chat --assistant-id " + assistantId + "."
            };
            dataset.Corpora.Add(corpus);
            DatasetStore.Save(dataset, output);
            Console.WriteLine("[eval] wrote " + corpus.Queries.Count + " questions to " + output);
            return corpus.Queries.Count;
        }

        /// <summary>
        /// Export, run in-product Eval, and measure its agreement with the independent judge.
        /// </summary>
        /// <param name="dataset">Dataset.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Report.</returns>
        public async Task<EvalAgreementReport> RunAsync(BenchmarkDataset dataset, CancellationToken token)
        {
            JudgeClient judge = JudgeClient.From(_Context.Http, _Context.Arguments, _Context.OllamaUrl) ?? throw new ArgumentException("eval-run needs a judge.");
            (string assistantId, Dictionary<string, BenchmarkQuery> facts) = await ExportAsync(dataset, token).ConfigureAwait(false);

            JsonNode run = await _Context.Client.SendJsonAsync(HttpMethod.Post, "/v1.0/eval/runs", new JsonObject
            {
                ["AssistantId"] = assistantId,
                ["ExecutionMode"] = "ChatRail"
            }, token).ConfigureAwait(false);
            string runId = run["Id"]?.GetValue<string>() ?? throw new InvalidOperationException("Eval run returned no Id.");
            Console.WriteLine("[eval] run " + runId + " started");

            string status = string.Empty;
            for (int i = 0; i < 7200; i++)
            {
                await Task.Delay(2000, token).ConfigureAwait(false);
                JsonNode state = await _Context.Client.SendJsonAsync(HttpMethod.Get, "/v1.0/eval/runs/" + runId, null, token).ConfigureAwait(false);
                status = state["Status"]?.GetValue<string>() ?? string.Empty;
                if (i % 15 == 0) Console.WriteLine("  " + status + " " + state["FactsEvaluated"] + "/" + state["TotalFacts"]);
                if (status == "Completed" || status == "Failed" || status == "Cancelled") break;
            }

            JsonNode results = await _Context.Client.SendJsonAsync(HttpMethod.Get, "/v1.0/eval/runs/" + runId + "/results", null, token).ConfigureAwait(false);
            EvalAgreementReport report = new EvalAgreementReport { Dataset = dataset.Name, Environment = _Context.Environment, RunId = runId };
            report.Config["judge"] = judge.Description;
            report.Config["runStatus"] = status;
            report.Config["assistantId"] = assistantId;

            List<(bool A, bool B)> pairs = new List<(bool, bool)>();
            foreach (JsonNode? result in results as JsonArray ?? new JsonArray())
            {
                string question = result?["Question"]?.GetValue<string>() ?? string.Empty;
                if (!facts.TryGetValue(question, out BenchmarkQuery? query)) continue;
                bool evalPass = result?["OverallPass"]?.GetValue<bool>() ?? false;
                string answer = JudgeClient.StripThinking(result?["LlmResponse"]?.GetValue<string>() ?? string.Empty).Trim();
                bool? judged = await judge.GradeAsync(query.Text, query.Answer ?? string.Empty, answer, !query.Answerable, token).ConfigureAwait(false);
                report.Items.Add(new Dictionary<string, string>
                {
                    ["queryId"] = query.Id,
                    ["type"] = query.Type,
                    ["evalPass"] = evalPass.ToString(),
                    ["judge"] = judged.HasValue ? judged.Value.ToString() : "unparsed",
                    ["answer"] = answer.Length > 300 ? answer.Substring(0, 300) + "…" : answer
                });
                if (judged.HasValue) pairs.Add((evalPass, judged.Value));
            }

            report.Summary["facts"] = facts.Count;
            report.Summary["results"] = report.Items.Count;
            report.Summary["paired"] = pairs.Count;
            if (pairs.Count > 0)
            {
                report.Summary["evalPassRate"] = Math.Round(pairs.Count(p => p.A) / (double)pairs.Count, 4);
                report.Summary["judgePassRate"] = Math.Round(pairs.Count(p => p.B) / (double)pairs.Count, 4);
                report.Summary["agreement"] = Math.Round(pairs.Count(p => p.A == p.B) / (double)pairs.Count, 4);
                report.Summary["kappa"] = Statistics.CohensKappa(pairs) ?? 0;
                report.Summary["evalPassJudgeFail"] = pairs.Count(p => p.A && !p.B);
                report.Summary["evalFailJudgePass"] = pairs.Count(p => !p.A && p.B);
            }

            Console.WriteLine("[eval] in-product pass rate " + report.Summary.GetValueOrDefault("evalPassRate").ToString("F3") + ", independent judge "
                + report.Summary.GetValueOrDefault("judgePassRate").ToString("F3") + ", agreement " + report.Summary.GetValueOrDefault("agreement").ToString("F3")
                + ", kappa " + report.Summary.GetValueOrDefault("kappa").ToString("F3"));
            return report;
        }

        #endregion
    }
}
