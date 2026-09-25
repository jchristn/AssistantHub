namespace Test.Benchmark.Runners
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Benchmark.Datasets;
    using Test.Benchmark.Metrics;
    using Test.Benchmark.Stub;

    /// <summary>
    /// Closed-loop load test: at each concurrency level, N workers issue the dataset's queries back to back for a fixed
    /// duration and the runner reports throughput, error rate and latency percentiles per operation, with a server
    /// stage breakdown. With <c>--stub</c>, embeddings and completions come from an in-process stub model server, so
    /// the result measures AssistantHub, Partio and RecallDB rather than the model.
    /// </summary>
    public class LoadRunner
    {
        #region Private-Members

        private readonly BenchmarkContext _Context;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="context">Benchmark context.</param>
        public LoadRunner(BenchmarkContext context)
        {
            _Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Run the load test.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Report.</returns>
        public async Task<LoadReport> RunAsync(CancellationToken token)
        {
            BenchmarkArguments args = _Context.Arguments;
            BenchmarkDataset dataset = DatasetStore.Load(args.Get("dataset", string.Empty));
            string scenario = args.Get("scenario", "retrieve").ToLowerInvariant();
            if (scenario != "retrieve" && scenario != "chat" && scenario != "mixed") throw new ArgumentException("--scenario must be retrieve, chat or mixed.");
            List<int> levels = args.GetList("concurrency", "1,4,16").Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToList();
            int duration = args.GetInt("duration", 30);
            int warmup = args.GetInt("warmup", 5);
            bool stub = args.GetFlag("stub");

            LoadReport report = new LoadReport { Scenario = scenario, Dataset = dataset.Name, Environment = _Context.Environment };
            report.Environment.DatasetHash = dataset.FileHash;
            report.Config["levels"] = string.Join(",", levels);
            report.Config["durationSeconds"] = duration.ToString(CultureInfo.InvariantCulture);
            report.Config["warmupSeconds"] = warmup.ToString(CultureInfo.InvariantCulture);
            report.Config["models"] = stub ? "stub (" + args.GetInt("stub-latency-ms", 5) + " ms per call)" : "real endpoints";

            StubModelServer? server = null;
            try
            {
                Provisioner provisioner = new Provisioner(_Context);
                AssistantVariant variant = AssistantVariant.From(args);
                if (stub)
                {
                    int port = args.GetInt("stub-port", StubModelServer.DefaultPort);
                    server = new StubModelServer(port, provisioner.Variant.Dimensions, args.GetInt("stub-latency-ms", 5));
                    await server.StartAsync(token).ConfigureAwait(false);
                    string upstream = args.Get("stub-host", "http://host.docker.internal:" + port);
                    string embeddingId = await EnsureEndpointAsync("embedding", "bench-stub-embed", StubModelServer.EmbeddingModel, upstream, token).ConfigureAwait(false);
                    string completionId = await EnsureEndpointAsync("completion", "bench-stub-chat", StubModelServer.CompletionModel, upstream, token).ConfigureAwait(false);
                    provisioner.Variant.EmbeddingEndpointId = embeddingId;
                    provisioner.Variant.Suffix = "stub";
                    variant.EmbeddingEndpointId = embeddingId;
                    variant.InferenceEndpointId = completionId;
                    report.Config["stubEndpoints"] = embeddingId + ", " + completionId;
                }

                foreach (KeyValuePair<string, string> item in provisioner.Variant.Describe()) report.Config[item.Key] = item.Value;
                foreach (KeyValuePair<string, string> item in variant.Describe()) report.Config["assistant." + item.Key] = item.Value;

                List<ProvisionedCollection> collections = await provisioner.ProvisionAsync(dataset, report.Ingest, token).ConfigureAwait(false);
                List<(string AssistantId, ProvisionedCollection Collection, BenchmarkQuery Query)> workload = new List<(string, ProvisionedCollection, BenchmarkQuery)>();
                foreach (ProvisionedCollection collection in collections)
                {
                    string assistantId = await provisioner.EnsureAssistantAsync(collection, variant, token).ConfigureAwait(false);
                    workload.AddRange(collection.Corpus.Queries.Select(q => (assistantId, collection, q)));
                }

                if (workload.Count == 0) throw new InvalidDataException("The dataset has no queries to drive the load test.");

                foreach (int concurrency in levels)
                {
                    LoadLevel level = await RunLevelAsync(workload, scenario, concurrency, warmup, duration, token).ConfigureAwait(false);
                    report.Levels.Add(level);
                    Console.WriteLine("[load] c=" + concurrency.ToString().PadRight(3) + " " + level.Throughput.ToString("F1") + " ops/s, errors " + (level.ErrorRate * 100).ToString("F2") + "%, "
                        + string.Join(", ", level.Operations.Select(o => o.Key + " p50 " + o.Value.Latency.P50 + "ms p95 " + o.Value.Latency.P95 + "ms")));
                }
            }
            finally
            {
                if (server != null) await server.DisposeAsync().ConfigureAwait(false);
            }

            return report;
        }

        #endregion

        #region Private-Methods

        private async Task<LoadLevel> RunLevelAsync(List<(string AssistantId, ProvisionedCollection Collection, BenchmarkQuery Query)> workload, string scenario, int concurrency, int warmup, int duration, CancellationToken token)
        {
            ConcurrentBag<(string Op, bool Ok, double Ms, string? Error)> samples = new ConcurrentBag<(string, bool, double, string?)>();
            DateTime measureFrom = DateTime.UtcNow.AddSeconds(warmup);
            DateTime stopAt = measureFrom.AddSeconds(duration);
            PrometheusSnapshot? before = null;

            List<Task> workers = new List<Task>();
            for (int w = 0; w < concurrency; w++)
            {
                int worker = w;
                workers.Add(Task.Run(async () =>
                {
                    Random random = new Random(1000 + worker);
                    int index = worker * 7919 % workload.Count;
                    while (DateTime.UtcNow < stopAt && !token.IsCancellationRequested)
                    {
                        (string assistantId, ProvisionedCollection collection, BenchmarkQuery query) = workload[index];
                        index = (index + 1) % workload.Count;
                        string op = scenario == "mixed" ? (random.NextDouble() < 0.9 ? "retrieve" : "chat") : scenario;
                        JsonObject body = new JsonObject { ["messages"] = QueryRequestBuilder.Messages(query) };
                        QueryRequestBuilder.AddScope(body, query, collection);
                        TimedCall call;
                        if (op == "retrieve")
                        {
                            body["include_stages"] = false;
                            call = (await _Context.Client.RetrieveAsync(assistantId, body, token).ConfigureAwait(false)).Call;
                        }
                        else
                        {
                            body["stream"] = false;
                            call = (await _Context.Client.ChatAsync(assistantId, body, token).ConfigureAwait(false)).Call;
                        }

                        if (DateTime.UtcNow >= measureFrom) samples.Add((op, call.IsSuccess, call.ElapsedMs, call.IsSuccess ? null : call.Describe()));
                    }
                }, token));
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, warmup)), token).ConfigureAwait(false);
            before = await PrometheusSnapshot.CaptureAsync(_Context.Http, _Context.MetricsUrl, token).ConfigureAwait(false);
            await Task.WhenAll(workers).ConfigureAwait(false);
            await Task.Delay(_Context.MetricsSettleMs, token).ConfigureAwait(false);
            PrometheusSnapshot after = await PrometheusSnapshot.CaptureAsync(_Context.Http, _Context.MetricsUrl, token).ConfigureAwait(false);

            List<(string Op, bool Ok, double Ms, string? Error)> all = samples.ToList();
            LoadLevel level = new LoadLevel
            {
                Concurrency = concurrency,
                Seconds = duration,
                Throughput = Math.Round(all.Count / (double)duration, 2),
                ErrorRate = all.Count > 0 ? Math.Round(all.Count(s => !s.Ok) / (double)all.Count, 4) : 0,
                Stages = after.Since(before)
            };

            foreach (IGrouping<string, (string Op, bool Ok, double Ms, string? Error)> group in all.GroupBy(s => s.Op).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                level.Operations[group.Key] = new LoadOpStats
                {
                    Count = group.Count(),
                    Errors = group.Count(s => !s.Ok),
                    Throughput = Math.Round(group.Count() / (double)duration, 2),
                    Latency = LatencyStats.From(group.Where(s => s.Ok).Select(s => s.Ms))
                };
            }

            level.SampleErrors = all.Where(s => s.Error != null).Select(s => s.Op + ": " + s.Error!).Distinct().Take(5).ToList();
            return level;
        }

        private async Task<string> EnsureEndpointAsync(string kind, string name, string model, string upstream, CancellationToken token)
        {
            JsonNode list = await _Context.Client.SendJsonAsync(HttpMethod.Post, "/v1.0/endpoints/" + kind + "/enumerate", new JsonObject { ["MaxResults"] = 1000 }, token).ConfigureAwait(false);
            string? existing = (list["Objects"] as JsonArray)?.FirstOrDefault(o => o?["Name"]?.GetValue<string>() == name)?["Id"]?.GetValue<string>();
            JsonObject body = new JsonObject
            {
                ["Name"] = name,
                ["Model"] = model,
                ["Endpoint"] = upstream,
                ["ApiFormat"] = "Ollama",
                ["Active"] = true,
                ["MaxConcurrentRequests"] = 64,
                ["MaxQueueDepth"] = 10000,
                ["HealthCheckEnabled"] = false
            };

            if (existing != null)
            {
                await _Context.Client.SendJsonAsync(HttpMethod.Put, "/v1.0/endpoints/" + kind + "/" + existing, body, token).ConfigureAwait(false);
                return existing;
            }

            JsonNode created = await _Context.Client.SendJsonAsync(HttpMethod.Put, "/v1.0/endpoints/" + kind, body, token).ConfigureAwait(false);
            return created["Id"]?.GetValue<string>() ?? throw new InvalidOperationException("Endpoint create returned no Id.");
        }

        #endregion
    }
}
