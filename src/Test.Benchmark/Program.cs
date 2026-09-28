namespace Test.Benchmark
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Benchmark.Datasets;
    using Test.Benchmark.Reporting;
    using Test.Benchmark.Runners;
    using Test.Benchmark.Stub;

    /// <summary>
    /// AssistantHub benchmark harness. See benchmarks/README.md for the full workflow.
    /// </summary>
    public static class Program
    {
        #region Public-Methods

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Process exit code.</returns>
        public static async Task<int> Main(string[] args)
        {
            BenchmarkArguments arguments = BenchmarkArguments.Parse(args);
            using CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                switch (arguments.Command)
                {
                    case "prepare":
                        return Converters.Prepare(arguments);
                    case "validate":
                        return Validate(arguments);
                    case "ingest":
                        return await IngestAsync(arguments, cts.Token).ConfigureAwait(false);
                    case "retrieval":
                        return await RetrievalAsync(arguments, cts.Token).ConfigureAwait(false);
                    case "chat":
                        return await ChatAsync(arguments, cts.Token).ConfigureAwait(false);
                    case "load":
                        return await LoadAsync(arguments, cts.Token).ConfigureAwait(false);
                    case "stub":
                        return await StubAsync(arguments, cts.Token).ConfigureAwait(false);
                    case "compare":
                        return ResultComparer.Compare(arguments);
                    case "history":
                        return RunHistory.Show(arguments, new BenchmarkContext(arguments).ResultsDirectory);
                    case "eval-import":
                        return await EvalImportAsync(arguments, cts.Token).ConfigureAwait(false);
                    case "eval-export":
                    case "eval-run":
                        return await EvalAsync(arguments, cts.Token).ConfigureAwait(false);
                    case "label-sample":
                        return JudgeAgreement.WriteLabelSample(arguments);
                    default:
                        PrintUsage();
                        return string.IsNullOrEmpty(arguments.Command) || arguments.Command == "help" ? 0 : 2;
                }
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("Cancelled.");
                return 130;
            }
            catch (Exception e) when (e is InvalidOperationException || e is InvalidDataException || e is FileNotFoundException || e is ArgumentException || e is FormatException)
            {
                Console.Error.WriteLine("Error: " + e.Message);
                return 1;
            }
        }

        #endregion

        #region Private-Methods

        private static int Validate(BenchmarkArguments arguments)
        {
            BenchmarkDataset dataset = DatasetStore.Load(arguments.Get("dataset", string.Empty));
            List<string> problems = DatasetValidator.Validate(dataset);
            int queries = dataset.Corpora.Sum(c => c.Queries.Count);
            Console.WriteLine(dataset.Name + ": " + dataset.Corpora.Count + " corpora, " + dataset.Corpora.Sum(c => c.Documents.Count) + " documents, " + queries + " queries");
            foreach (IGrouping<string, BenchmarkQuery> group in dataset.Corpora.SelectMany(c => c.Queries).GroupBy(q => q.Type).OrderBy(g => g.Key, StringComparer.Ordinal))
                Console.WriteLine("  " + group.Key.PadRight(12) + group.Count());
            foreach (string problem in problems) Console.WriteLine((problem.StartsWith("warning:", StringComparison.Ordinal) ? "  " : "  ERROR ") + problem);
            int errors = problems.Count(p => !p.StartsWith("warning:", StringComparison.Ordinal));
            Console.WriteLine(errors == 0 ? "OK" : errors + " error(s)");
            return errors == 0 ? 0 : 1;
        }

        private static async Task<int> IngestAsync(BenchmarkArguments arguments, CancellationToken token)
        {
            BenchmarkDataset dataset = DatasetStore.Load(arguments.Get("dataset", string.Empty));
            using BenchmarkContext context = new BenchmarkContext(arguments);
            await context.InitializeAsync(token).ConfigureAwait(false);

            Provisioner provisioner = new Provisioner(context);
            IngestReport report = new IngestReport { Dataset = dataset.Name, Environment = context.Environment };
            report.Environment.DatasetHash = dataset.FileHash;
            foreach (KeyValuePair<string, string> item in provisioner.Variant.Describe()) report.Config[item.Key] = item.Value;
            List<ProvisionedCollection> collections = await provisioner.ProvisionAsync(dataset, report.Ingest, token).ConfigureAwait(false);

            foreach (IGrouping<string, IngestDocumentOutcome> group in report.Ingest.Outcomes.GroupBy(o => o.ContentType).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                List<IngestDocumentOutcome> completed = group.Where(o => o.Status == "Completed").ToList();
                report.ByContentType[group.Key] = new Dictionary<string, double>
                {
                    ["documents"] = group.Count(),
                    ["failures"] = group.Count() - completed.Count,
                    ["meanChunks"] = completed.Count > 0 ? Math.Round(completed.Average(o => o.Chunks), 2) : 0,
                    ["meanMs"] = completed.Count > 0 ? Math.Round(completed.Average(o => o.ElapsedMs), 1) : 0,
                    ["meanKB"] = Math.Round(group.Average(o => o.Bytes) / 1024.0, 1)
                };
            }

            if (!arguments.GetFlag("no-reachability"))
                report.Reachability = await ReachabilityAnalyzer.AnalyzeAsync(provisioner, collections, token).ConfigureAwait(false);

            string basePath = context.ReportBasePath("ingest", dataset.Name);
            RecordHistory(context, ReportWriter.WriteJson(report, basePath));
            Console.WriteLine("Report: " + ReportWriter.WriteMarkdown(ReportWriter.RenderIngest(report), basePath));
            return report.Ingest.Failures == 0 ? 0 : 3;
        }

        private static async Task<int> RetrievalAsync(BenchmarkArguments arguments, CancellationToken token)
        {
            BenchmarkDataset dataset = DatasetStore.Load(arguments.Get("dataset", string.Empty));
            using BenchmarkContext context = new BenchmarkContext(arguments);
            await context.InitializeAsync(token).ConfigureAwait(false);

            RetrievalReport report = await new RetrievalRunner(context).RunAsync(dataset, token).ConfigureAwait(false);
            string basePath = context.ReportBasePath("retrieval", dataset.Name);
            RecordHistory(context, ReportWriter.WriteJson(report, basePath));
            Console.WriteLine("Report: " + ReportWriter.WriteMarkdown(ReportWriter.RenderRetrieval(report), basePath));
            return 0;
        }

        private static async Task<int> ChatAsync(BenchmarkArguments arguments, CancellationToken token)
        {
            BenchmarkDataset dataset = DatasetStore.Load(arguments.Get("dataset", string.Empty));
            using BenchmarkContext context = new BenchmarkContext(arguments);
            await context.InitializeAsync(token).ConfigureAwait(false);

            ChatReport report = await new ChatRunner(context).RunAsync(dataset, token).ConfigureAwait(false);
            string basePath = context.ReportBasePath("chat", dataset.Name);
            RecordHistory(context, ReportWriter.WriteJson(report, basePath));
            Console.WriteLine("Report: " + ReportWriter.WriteMarkdown(ReportWriter.RenderChat(report), basePath));
            return 0;
        }

        private static async Task<int> LoadAsync(BenchmarkArguments arguments, CancellationToken token)
        {
            using BenchmarkContext context = new BenchmarkContext(arguments);
            await context.InitializeAsync(token).ConfigureAwait(false);

            LoadReport report = await new LoadRunner(context).RunAsync(token).ConfigureAwait(false);
            string basePath = context.ReportBasePath("load", report.Scenario + "-" + report.Dataset);
            RecordHistory(context, ReportWriter.WriteJson(report, basePath));
            Console.WriteLine("Report: " + ReportWriter.WriteMarkdown(ReportWriter.RenderLoad(report), basePath));
            return 0;
        }

        private static async Task<int> StubAsync(BenchmarkArguments arguments, CancellationToken token)
        {
            await using StubModelServer stub = new StubModelServer(
                arguments.GetInt("stub-port", StubModelServer.DefaultPort),
                arguments.GetInt("dimensions", 384),
                arguments.GetInt("stub-latency-ms", 5));
            await stub.StartAsync(token).ConfigureAwait(false);
            Console.WriteLine("Stub model server on " + stub.Url + " (Ollama-compatible /api/embed, /api/chat, /api/tags). Ctrl+C to stop.");
            try
            {
                await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }

            return 0;
        }

        private static async Task<int> EvalAsync(BenchmarkArguments arguments, CancellationToken token)
        {
            BenchmarkDataset dataset = DatasetStore.Load(arguments.Get("dataset", string.Empty));
            using BenchmarkContext context = new BenchmarkContext(arguments);
            await context.InitializeAsync(token).ConfigureAwait(false);
            EvalBridge bridge = new EvalBridge(context);
            if (arguments.Command == "eval-export")
            {
                await bridge.ExportAsync(dataset, token).ConfigureAwait(false);
                return 0;
            }

            EvalAgreementReport report = await bridge.RunAsync(dataset, token).ConfigureAwait(false);
            string basePath = context.ReportBasePath("eval", dataset.Name);
            RecordHistory(context, ReportWriter.WriteJson(report, basePath));
            Console.WriteLine("Report: " + ReportWriter.WriteMarkdown(ReportWriter.RenderEval(report), basePath));
            return 0;
        }

        private static async Task<int> EvalImportAsync(BenchmarkArguments arguments, CancellationToken token)
        {
            string assistantId = arguments.Get("assistant-id", string.Empty);
            string output = arguments.Get("output", string.Empty);
            if (assistantId.Length == 0 || output.Length == 0) throw new ArgumentException("eval-import needs --assistant-id <id> --output <file.json>.");
            using BenchmarkContext context = new BenchmarkContext(arguments);
            await new EvalBridge(context).ImportAsync(assistantId, output, token).ConfigureAwait(false);
            return 0;
        }

        private static void RecordHistory(BenchmarkContext context, string reportPath)
        {
            if (context.Arguments.GetFlag("no-history")) return;
            string ledger = RunHistory.LedgerPath(context.ResultsDirectory);
            int entries = RunHistory.Record(reportPath, ledger);
            Console.WriteLine("History: " + entries + " entr" + (entries == 1 ? "y" : "ies") + " recorded in " + ledger);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("AssistantHub benchmark harness (see benchmarks/README.md)");
            Console.WriteLine();
            Console.WriteLine("  prepare      --format beir|multihop-rag|qasper --input <path> --output <file.json> [--name n] [--limit n] [--seed n]");
            Console.WriteLine("  validate     --dataset <file.json>");
            Console.WriteLine("  ingest       --dataset <file.json> [ingestion options] [--no-reachability]");
            Console.WriteLine("  retrieval    --dataset <file.json> [--modes Vector,FullText,Hybrid] [--sweep param=v1,v2] [--reachability] [assistant options]");
            Console.WriteLine("  chat         --dataset <file.json> [--judge-model m] [--repeats n] [--limit n] [--hand-labels file] [--assistant-id id] [assistant options]");
            Console.WriteLine("  load         --dataset <file.json> [--scenario retrieve|chat|mixed] [--concurrency 1,4,16] [--duration 30] [--stub]");
            Console.WriteLine("  compare      --baseline <report.json>|previous --candidate <report.json> [--tolerance 0.01] [--alpha 0.05] [--latency-tolerance 0.25]");
            Console.WriteLine("  history      [--dataset d] [--kind retrieval|chat|load|ingest|eval] [--configuration c] [--label l] [--metric m] [--last n] [--rebuild]");
            Console.WriteLine("  eval-export  --dataset <file.json>        create in-product EvalFacts on a benchmark assistant");
            Console.WriteLine("  eval-run     --dataset <file.json>        run in-product Eval and measure its agreement with the independent judge");
            Console.WriteLine("  eval-import  --assistant-id <id> --output <file.json>   turn an assistant's EvalFacts into a dataset (run with chat --assistant-id)");
            Console.WriteLine("  label-sample --report <chat.json> --count 50 --output <labels.json>");
            Console.WriteLine("  stub         [--stub-port 38950] [--dimensions 384] [--stub-latency-ms 5]");
            Console.WriteLine();
            Console.WriteLine("Common:     --url http://127.0.0.1:38800 --token default --metrics-url http://127.0.0.1:38889/metrics|none --label <text> --output-dir <dir> --no-history");
            Console.WriteLine("Ingestion:  --chunk-strategy FixedTokenCount --chunk-tokens 256 --chunk-overlap 0 --context-prefix <text> --summarize-endpoint <id>");
            Console.WriteLine("            --embedding-endpoint default --dimensions 384 --l2-normalize --scope-suffix <text> --reingest --ingest-concurrency 4");
            Console.WriteLine("Assistant:  --k 10 --threshold 0.3 --text-weight 0.3 --fulltext-type TsRank --neighbors 0 --rewrite --rerank --rerank-k 5");
            Console.WriteLine("            --rerank-threshold 3 --gate --answerability --answerability-mode LogOnly --citations --inference-endpoint default");
            Console.WriteLine("            --utility-endpoint <id> --temperature 0 --concurrency 4 --limit n");
        }

        #endregion
    }
}
