namespace Test.Benchmark.Runners
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Benchmark.Datasets;
    using Test.Benchmark.Metrics;

    /// <summary>
    /// Ingests benchmark corpora through AssistantHub's real document pipeline (upload, S3, DocumentAtom
    /// extraction, Partio chunking and embedding, RecallDB) and creates the assistants that query them.
    /// </summary>
    /// <remarks>
    /// Collection, ingestion-rule and assistant names are deterministic (dataset, corpus, ingestion configuration hash,
    /// suffix; assistant configuration hash), so a rerun reuses what is already ingested and only uploads documents
    /// that are missing or failed. <c>--reingest</c> deletes the corpus's documents first. Documents carry a
    /// <c>bench_doc_id</c> tag, from which the dataset-to-AssistantHub id map is rebuilt on reuse. Uploads are
    /// bounded by an in-flight window (<c>--ingest-concurrency</c>, default 4), because AssistantHub accepts an upload
    /// immediately and processes it in the background. With <c>--date-order</c>, dated corpora are ingested one
    /// document at a time in date order (AssistantHub has no recency signal today, so this is off by default).
    /// </remarks>
    public class Provisioner
    {
        #region Public-Members

        /// <summary>
        /// Bucket all benchmark documents are stored in.
        /// </summary>
        public const string Bucket = "bench";

        /// <summary>
        /// Ingestion configuration.
        /// </summary>
        public IngestionVariant Variant { get; }

        #endregion

        #region Private-Members

        private static readonly Regex _ChunksStored = new Regex("(\\d+) chunks? stored", RegexOptions.Compiled);
        private static readonly string[] _Terminal = new string[] { "Completed", "Failed", "TypeDetectionFailed" };

        private readonly BenchmarkContext _Context;
        private readonly int _Concurrency;
        private readonly bool _Reingest;
        private readonly int _TimeoutSeconds;
        private readonly bool _DateOrder;
        private readonly Dictionary<string, string> _Assistants = new Dictionary<string, string>(StringComparer.Ordinal);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="context">Benchmark context.</param>
        public Provisioner(BenchmarkContext context)
        {
            _Context = context ?? throw new ArgumentNullException(nameof(context));
            Variant = IngestionVariant.From(context.Arguments);
            _Concurrency = Math.Max(1, context.Arguments.GetInt("ingest-concurrency", 4));
            _Reingest = context.Arguments.GetFlag("reingest");
            _TimeoutSeconds = context.Arguments.GetInt("ingest-timeout-seconds", 900);
            _DateOrder = context.Arguments.GetFlag("date-order");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Provision every corpus of a dataset.
        /// </summary>
        /// <param name="dataset">Dataset.</param>
        /// <param name="summary">Ingest statistics (accumulated).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Provisioned collections, in corpus order.</returns>
        public async Task<List<ProvisionedCollection>> ProvisionAsync(BenchmarkDataset dataset, IngestSummary summary, CancellationToken token)
        {
            await EnsureBucketAsync(token).ConfigureAwait(false);
            List<ProvisionedCollection> collections = new List<ProvisionedCollection>();
            ConcurrentBag<double> latencies = new ConcurrentBag<double>();
            summary.Concurrency = _Concurrency;

            PrometheusSnapshot before = await PrometheusSnapshot.CaptureAsync(_Context.Http, _Context.MetricsUrl, token).ConfigureAwait(false);
            Stopwatch wall = Stopwatch.StartNew();

            foreach (BenchmarkCorpus corpus in dataset.Corpora)
            {
                collections.Add(await ProvisionCorpusAsync(dataset, corpus, summary, latencies, token).ConfigureAwait(false));
            }

            wall.Stop();
            if (summary.Uploaded > 0)
            {
                await Task.Delay(_Context.MetricsSettleMs, token).ConfigureAwait(false);
                PrometheusSnapshot after = await PrometheusSnapshot.CaptureAsync(_Context.Http, _Context.MetricsUrl, token).ConfigureAwait(false);
                summary.Stages = after.Since(before);
                summary.WallSeconds = Math.Round(wall.Elapsed.TotalSeconds, 1);
                summary.DocumentsPerSecond = summary.WallSeconds > 0 ? Math.Round(summary.Uploaded / summary.WallSeconds, 2) : 0;
            }

            summary.Latency = LatencyStats.From(latencies);
            Console.WriteLine("[ingest] " + summary.Documents + " documents: " + summary.Reused + " reused, " + summary.Uploaded + " uploaded in "
                + summary.WallSeconds + "s (" + summary.DocumentsPerSecond + " docs/s), " + summary.Failures + " failures");
            return collections;
        }

        /// <summary>
        /// Find or create the assistant for a collection and configuration, and write its settings.
        /// </summary>
        /// <param name="collection">Provisioned collection.</param>
        /// <param name="variant">Assistant configuration.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assistant id.</returns>
        public async Task<string> EnsureAssistantAsync(ProvisionedCollection collection, AssistantVariant variant, CancellationToken token)
        {
            string name = Truncate(collection.CollectionName, 60) + "-a" + variant.Hash();
            if (_Assistants.TryGetValue(name, out string? cached)) return cached;

            List<JsonNode> assistants = await _Context.Client.ListAllAsync("/v1.0/assistants", token).ConfigureAwait(false);
            string? assistantId = assistants.FirstOrDefault(a => a["Name"]?.GetValue<string>() == name)?["Id"]?.GetValue<string>();
            if (assistantId == null)
            {
                JsonNode created = await _Context.Client.SendJsonAsync(HttpMethod.Put, "/v1.0/assistants", new JsonObject
                {
                    ["Name"] = name,
                    ["Description"] = "Benchmark assistant (managed by src/Test.Benchmark): " + string.Join(", ", variant.Describe().Select(kv => kv.Key + "=" + kv.Value))
                }, token).ConfigureAwait(false);
                assistantId = created["Id"]?.GetValue<string>() ?? throw new InvalidOperationException("Assistant create returned no Id.");
            }

            JsonNode settings = await _Context.Client.SendJsonAsync(HttpMethod.Get, "/v1.0/assistants/" + assistantId + "/settings", null, token).ConfigureAwait(false);
            JsonObject settingsObject = settings.AsObject();
            variant.ApplyTo(settingsObject, collection.CollectionId);
            await _Context.Client.SendJsonAsync(HttpMethod.Put, "/v1.0/assistants/" + assistantId + "/settings", settingsObject, token).ConfigureAwait(false);

            _Assistants[name] = assistantId;
            return assistantId;
        }

        /// <summary>
        /// Read every stored chunk of a collection (for evidence reachability).
        /// </summary>
        /// <param name="collection">Provisioned collection.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>(dataset document id, chunk text) pairs.</returns>
        public async Task<List<(string DocumentId, string Content)>> ReadChunksAsync(ProvisionedCollection collection, CancellationToken token)
        {
            List<JsonNode> records = await _Context.Client.ListAllAsync("/v1.0/collections/" + collection.CollectionId + "/records", token).ConfigureAwait(false);
            return records
                .Select(r => (collection.ToDatasetId(r["DocumentId"]?.GetValue<string>()), r["Content"]?.GetValue<string>() ?? string.Empty))
                .ToList();
        }

        #endregion

        #region Private-Methods

        private async Task<ProvisionedCollection> ProvisionCorpusAsync(BenchmarkDataset dataset, BenchmarkCorpus corpus, IngestSummary summary, ConcurrentBag<double> latencies, CancellationToken token)
        {
            string name = "bench-" + Slug(dataset.Name, 24) + "-" + Slug(corpus.Id, 24) + "-" + Variant.Hash() + (Variant.Suffix.Length > 0 ? "-" + Slug(Variant.Suffix, 16) : string.Empty);
            ProvisionedCollection provisioned = new ProvisionedCollection { Corpus = corpus, CollectionName = name };
            summary.Documents += corpus.Documents.Count;

            List<JsonNode> collections = await _Context.Client.ListAllAsync("/v1.0/collections", token).ConfigureAwait(false);
            provisioned.CollectionId = collections.FirstOrDefault(c => c["Name"]?.GetValue<string>() == name)?["Id"]?.GetValue<string>() ?? string.Empty;
            if (provisioned.CollectionId.Length == 0)
            {
                JsonNode created = await _Context.Client.SendJsonAsync(HttpMethod.Put, "/v1.0/collections", new JsonObject
                {
                    ["Name"] = name,
                    ["Dimensionality"] = Variant.Dimensions
                }, token).ConfigureAwait(false);
                provisioned.CollectionId = created["Id"]?.GetValue<string>() ?? throw new InvalidOperationException("Collection create returned no Id.");
            }

            List<JsonNode> rules = await _Context.Client.ListAllAsync("/v1.0/ingestion-rules", token).ConfigureAwait(false);
            provisioned.IngestionRuleId = rules.FirstOrDefault(r => r["Name"]?.GetValue<string>() == name)?["Id"]?.GetValue<string>() ?? string.Empty;
            if (provisioned.IngestionRuleId.Length == 0)
            {
                JsonNode created = await _Context.Client.SendJsonAsync(HttpMethod.Put, "/v1.0/ingestion-rules", Variant.ToRuleBody(name, Bucket, name, provisioned.CollectionId), token).ConfigureAwait(false);
                provisioned.IngestionRuleId = created["Id"]?.GetValue<string>() ?? throw new InvalidOperationException("Ingestion rule create returned no Id.");
            }

            // Existing documents, keyed by the bench_doc_id tag.
            Dictionary<string, BenchmarkDocument> wanted = corpus.Documents.ToDictionary(d => d.Id, StringComparer.Ordinal);
            Dictionary<string, (string Id, string Status)> existing = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
            List<string> toDelete = new List<string>();
            foreach (JsonNode document in await _Context.Client.ListAllAsync("/v1.0/documents?collectionId=" + Uri.EscapeDataString(provisioned.CollectionId), token).ConfigureAwait(false))
            {
                string id = document["Id"]?.GetValue<string>() ?? string.Empty;
                string status = document["Status"]?.GetValue<string>() ?? string.Empty;
                string? datasetId = ReadTag(document, "bench_doc_id");
                bool keep = !_Reingest && datasetId != null && wanted.ContainsKey(datasetId) && !existing.ContainsKey(datasetId) && status == "Completed";
                if (keep) existing[datasetId!] = (id, status);
                else toDelete.Add(id);
            }

            if (toDelete.Count > 0)
            {
                Console.WriteLine("[ingest] " + name + ": deleting " + toDelete.Count + " stale, failed or duplicate documents");
                await ForEachAsync(toDelete, 8, id => _Context.Client.SendAsync(HttpMethod.Delete, "/v1.0/documents/" + id, null, token), token).ConfigureAwait(false);
            }

            foreach (KeyValuePair<string, (string Id, string Status)> pair in existing)
            {
                provisioned.DocumentIdByDatasetId[pair.Key] = pair.Value.Id;
                provisioned.DatasetIdByDocumentId[pair.Value.Id] = pair.Key;
            }

            summary.Reused += existing.Count;
            List<BenchmarkDocument> missing = corpus.Documents.Where(d => !existing.ContainsKey(d.Id)).ToList();
            if (missing.Count == 0) return provisioned;

            bool dated = _DateOrder && missing.Any(d => !string.IsNullOrEmpty(d.Date));
            if (dated) missing = missing.OrderBy(d => d.Date ?? string.Empty, StringComparer.Ordinal).ThenBy(d => d.Id, StringComparer.Ordinal).ToList();
            int concurrency = dated ? 1 : _Concurrency;
            Console.WriteLine("[ingest] " + name + ": uploading " + missing.Count + " documents (" + (dated ? "date order, one at a time" : "concurrency " + concurrency) + ")");

            object sync = new object();
            int done = 0;
            await ForEachAsync(missing, concurrency, async document =>
            {
                IngestDocumentOutcome outcome = await IngestDocumentAsync(dataset, corpus, provisioned, document, token).ConfigureAwait(false);
                lock (sync)
                {
                    summary.Uploaded++;
                    summary.Outcomes.Add(outcome);
                    if (outcome.Status == "Completed")
                    {
                        latencies.Add(outcome.ElapsedMs);
                    }
                    else
                    {
                        summary.Failures++;
                        if (summary.SampleErrors.Count < 20) summary.SampleErrors.Add(corpus.Id + "/" + document.Id + " (" + outcome.ContentType + "): " + outcome.Status + " " + outcome.Message);
                    }

                    done++;
                    if (done % 25 == 0 || done == missing.Count) Console.WriteLine("  ingested " + done + "/" + missing.Count + (summary.Failures > 0 ? " (" + summary.Failures + " failed)" : string.Empty));
                }
            }, token).ConfigureAwait(false);

            return provisioned;
        }

        private async Task<IngestDocumentOutcome> IngestDocumentAsync(BenchmarkDataset dataset, BenchmarkCorpus corpus, ProvisionedCollection provisioned, BenchmarkDocument document, CancellationToken token)
        {
            byte[] content = DatasetStore.ReadContent(dataset, document, out string contentType, out string fileName);
            IngestDocumentOutcome outcome = new IngestDocumentOutcome { Corpus = corpus.Id, DocumentId = document.Id, ContentType = contentType, Bytes = content.LongLength };

            JsonObject tags = new JsonObject();
            if (document.Tags != null)
            {
                foreach (KeyValuePair<string, string> tag in document.Tags) tags[tag.Key] = tag.Value;
            }

            tags["bench_doc_id"] = document.Id;
            JsonArray labels = new JsonArray();
            foreach (string label in document.Labels ?? new List<string>()) labels.Add(label);

            JsonObject body = new JsonObject
            {
                ["IngestionRuleId"] = provisioned.IngestionRuleId,
                ["Name"] = string.IsNullOrWhiteSpace(document.Title) ? document.Id : document.Title,
                ["OriginalFilename"] = fileName,
                ["ContentType"] = contentType,
                ["Labels"] = labels,
                ["Tags"] = tags,
                ["Base64Content"] = Convert.ToBase64String(content)
            };

            Stopwatch sw = Stopwatch.StartNew();
            TimedCall upload = null!;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                upload = await _Context.Client.SendAsync(HttpMethod.Put, "/v1.0/documents", body.ToJsonString(), token).ConfigureAwait(false);
                if (upload.IsSuccess || (upload.StatusCode >= 400 && upload.StatusCode < 500 && upload.StatusCode != 429)) break;
                await Task.Delay(1000 * (attempt + 1), token).ConfigureAwait(false);
            }

            if (!upload.IsSuccess)
            {
                outcome.Status = "UploadFailed";
                outcome.Message = upload.Describe();
                outcome.ElapsedMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
                return outcome;
            }

            string documentId = JsonNode.Parse(upload.Body)?["Id"]?.GetValue<string>() ?? string.Empty;
            lock (provisioned)
            {
                provisioned.DocumentIdByDatasetId[document.Id] = documentId;
                provisioned.DatasetIdByDocumentId[documentId] = document.Id;
            }

            int delayMs = 500;
            while (sw.Elapsed.TotalSeconds < _TimeoutSeconds)
            {
                await Task.Delay(delayMs, token).ConfigureAwait(false);
                delayMs = Math.Min(2000, delayMs + 250);
                TimedCall poll = await _Context.Client.SendAsync(HttpMethod.Get, "/v1.0/documents/" + documentId, null, token).ConfigureAwait(false);
                if (!poll.IsSuccess) continue;
                JsonNode? state = JsonNode.Parse(poll.Body);
                string status = state?["Status"]?.GetValue<string>() ?? string.Empty;
                if (!_Terminal.Contains(status)) continue;

                outcome.Status = status;
                outcome.Message = state?["StatusMessage"]?.GetValue<string>();
                outcome.Chunks = CountChunks(state);
                outcome.ElapsedMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
                return outcome;
            }

            outcome.Status = "Timeout";
            outcome.Message = "not terminal after " + _TimeoutSeconds + "s";
            outcome.ElapsedMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
            return outcome;
        }

        private async Task EnsureBucketAsync(CancellationToken token)
        {
            TimedCall call = await _Context.Client.SendAsync(HttpMethod.Put, "/v1.0/buckets", new JsonObject { ["Name"] = Bucket }.ToJsonString(), token).ConfigureAwait(false);
            if (!call.IsSuccess && call.StatusCode != 409) throw new InvalidOperationException("Could not create bucket '" + Bucket + "': " + call.Describe());
        }

        private static int CountChunks(JsonNode? document)
        {
            string? records = document?["ChunkRecordIds"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(records))
            {
                try
                {
                    if (JsonNode.Parse(records) is JsonArray array) return array.Count;
                }
                catch (JsonException)
                {
                }
            }

            Match match = _ChunksStored.Match(document?["StatusMessage"]?.GetValue<string>() ?? string.Empty);
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private static string? ReadTag(JsonNode document, string key)
        {
            JsonNode? tags = document["Tags"];
            try
            {
                if (tags is JsonValue value && value.TryGetValue(out string? text) && !string.IsNullOrEmpty(text)) tags = JsonNode.Parse(text);
                return tags?[key]?.GetValue<string>();
            }
            catch (JsonException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static async Task ForEachAsync<T>(IEnumerable<T> items, int concurrency, Func<T, Task> action, CancellationToken token)
        {
            using SemaphoreSlim gate = new SemaphoreSlim(concurrency);
            List<Task> tasks = new List<Task>();
            foreach (T item in items)
            {
                await gate.WaitAsync(token).ConfigureAwait(false);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await action(item).ConfigureAwait(false);
                    }
                    finally
                    {
                        gate.Release();
                    }
                }, token));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private static string Slug(string value, int max)
        {
            string slug = Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
            return Truncate(slug, max);
        }

        private static string Truncate(string value, int max)
        {
            return value.Length <= max ? value : value.Substring(0, max);
        }

        #endregion
    }
}
