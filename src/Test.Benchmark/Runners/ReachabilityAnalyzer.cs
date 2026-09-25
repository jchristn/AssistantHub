namespace Test.Benchmark.Runners
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Benchmark.Datasets;
    using Test.Benchmark.Metrics;

    /// <summary>
    /// Measures evidence reachability and chunk quality over everything a collection stores.
    /// </summary>
    public static class ReachabilityAnalyzer
    {
        #region Public-Methods

        /// <summary>
        /// Analyze provisioned collections.
        /// </summary>
        /// <param name="provisioner">Provisioner (reads stored chunks).</param>
        /// <param name="collections">Collections.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Summary.</returns>
        public static async Task<ReachabilitySummary> AnalyzeAsync(Provisioner provisioner, List<ProvisionedCollection> collections, CancellationToken token)
        {
            ReachabilitySummary summary = new ReachabilitySummary();
            Dictionary<string, (int Total, int Found)> byContentType = new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, (int Total, int Found)> byQueryType = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
            long chunkChars = 0;

            foreach (ProvisionedCollection collection in collections)
            {
                List<(string DocumentId, string Content)> chunks = await provisioner.ReadChunksAsync(collection, token).ConfigureAwait(false);
                Dictionary<string, List<string>> normalizedByDocument = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                foreach ((string documentId, string content) in chunks)
                {
                    summary.Chunks++;
                    chunkChars += content.Length;
                    if (content.Trim().Length < 40) summary.TinyChunks++;
                    string normalized = EvidenceMatcher.Normalize(content);
                    if (!normalizedByDocument.TryGetValue(documentId, out List<string>? list)) normalizedByDocument[documentId] = list = new List<string>();
                    if (list.Contains(normalized)) summary.DuplicateChunks++;
                    list.Add(normalized);
                }

                summary.DocumentsWithoutChunks += collection.Corpus.Documents.Count(d => !normalizedByDocument.ContainsKey(d.Id));
                Dictionary<string, BenchmarkDocument> documents = collection.Corpus.Documents.ToDictionary(d => d.Id, StringComparer.Ordinal);

                foreach (BenchmarkQuery query in collection.Corpus.Queries.Where(q => q.Answerable && q.Evidence != null))
                {
                    foreach (string passage in query.Evidence!)
                    {
                        string normalizedPassage = EvidenceMatcher.Normalize(passage);
                        string? foundIn = query.Relevant.FirstOrDefault(id =>
                            normalizedByDocument.TryGetValue(id, out List<string>? docChunks) && docChunks.Any(c => EvidenceMatcher.Contains(c, normalizedPassage)));
                        bool found = foundIn != null;

                        string primary = foundIn ?? query.Relevant[0];
                        string contentType = documents.TryGetValue(primary, out BenchmarkDocument? document)
                            ? document.ContentType ?? (document.File != null ? DatasetStore.ContentTypeFor(document.File) : "text/plain")
                            : "unknown";

                        summary.Passages++;
                        if (found) summary.Reachable++;
                        else if (summary.SampleUnreachable.Count < 25)
                            summary.SampleUnreachable.Add(collection.Corpus.Id + "/" + query.Id + " [" + string.Join(",", query.Relevant) + "] " + Shorten(passage));

                        Add(byContentType, ShortType(contentType), found);
                        Add(byQueryType, query.Type, found);
                    }
                }
            }

            summary.Rate = summary.Passages > 0 ? Math.Round((double)summary.Reachable / summary.Passages, 4) : 0;
            summary.MeanChunkChars = summary.Chunks > 0 ? Math.Round((double)chunkChars / summary.Chunks, 1) : 0;
            foreach (KeyValuePair<string, (int Total, int Found)> item in byContentType)
            {
                summary.ByContentType[item.Key] = Math.Round((double)item.Value.Found / item.Value.Total, 4);
                summary.PassagesByContentType[item.Key] = item.Value.Total;
            }

            foreach (KeyValuePair<string, (int Total, int Found)> item in byQueryType)
                summary.ByQueryType[item.Key] = Math.Round((double)item.Value.Found / item.Value.Total, 4);

            Console.WriteLine("[reachability] " + summary.Reachable + "/" + summary.Passages + " evidence passages reachable (" + summary.Rate.ToString("P1") + "), "
                + summary.Chunks + " chunks, " + summary.TinyChunks + " tiny, " + summary.DuplicateChunks + " duplicate, " + summary.DocumentsWithoutChunks + " documents without chunks");
            return summary;
        }

        #endregion

        #region Private-Methods

        private static void Add(Dictionary<string, (int Total, int Found)> counts, string key, bool found)
        {
            (int total, int hits) = counts.TryGetValue(key, out (int, int) existing) ? existing : (0, 0);
            counts[key] = (total + 1, hits + (found ? 1 : 0));
        }

        private static string ShortType(string contentType)
        {
            if (contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)) return "pdf";
            if (contentType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase)) return "docx";
            if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase)) return "html";
            if (contentType.Contains("markdown", StringComparison.OrdinalIgnoreCase)) return "md";
            if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) return "txt";
            return contentType;
        }

        private static string Shorten(string text)
        {
            string single = text.Replace("\n", " ");
            return single.Length > 120 ? single.Substring(0, 120) + "…" : single;
        }

        #endregion
    }
}
