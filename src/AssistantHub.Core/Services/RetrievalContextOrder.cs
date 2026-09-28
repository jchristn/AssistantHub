namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Arranges retrieved chunks for the prompt. Ranking decides which chunks are used; this decides the order the
    /// answer model reads them in.
    /// </summary>
    public static class RetrievalContextOrder
    {
        #region Public-Members

        /// <summary>
        /// Most relevant chunk first (the ranking order).
        /// </summary>
        public const string Score = "Score";

        /// <summary>
        /// Chunks grouped by document, documents ordered by their best-ranked chunk, chunks in document order, and
        /// chunks that touch or overlap merged into one passage.
        /// </summary>
        public const string ReadingOrder = "ReadingOrder";

        #endregion

        #region Private-Members

        private const int _MinimumOverlapCharacters = 16;
        private const int _MaximumOverlapCharacters = 2000;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Arrange ranked chunks for the prompt.
        /// </summary>
        /// <param name="rankedChunks">Chunks in ranking order (most relevant first).</param>
        /// <param name="contextOrder">Score or ReadingOrder.</param>
        /// <returns>The chunks to inject, in prompt order. Score returns the input list unchanged.</returns>
        public static List<RetrievalChunk> Apply(List<RetrievalChunk> rankedChunks, string contextOrder)
        {
            if (rankedChunks == null || rankedChunks.Count < 2) return rankedChunks;
            if (!String.Equals(contextOrder, ReadingOrder, StringComparison.OrdinalIgnoreCase)) return rankedChunks;

            // Group by document in order of each document's best-ranked chunk; chunks without a document keep their
            // own group so they are never merged with anything.
            List<List<(RetrievalChunk Chunk, int Rank)>> groups = new List<List<(RetrievalChunk Chunk, int Rank)>>();
            Dictionary<string, List<(RetrievalChunk Chunk, int Rank)>> byDocument = new Dictionary<string, List<(RetrievalChunk Chunk, int Rank)>>(StringComparer.Ordinal);

            for (int rank = 0; rank < rankedChunks.Count; rank++)
            {
                RetrievalChunk chunk = rankedChunks[rank];
                if (chunk == null) continue;

                if (String.IsNullOrEmpty(chunk.DocumentId))
                {
                    groups.Add(new List<(RetrievalChunk Chunk, int Rank)> { (chunk, rank) });
                    continue;
                }

                if (!byDocument.TryGetValue(chunk.DocumentId, out List<(RetrievalChunk Chunk, int Rank)> group))
                {
                    group = new List<(RetrievalChunk Chunk, int Rank)>();
                    byDocument[chunk.DocumentId] = group;
                    groups.Add(group);
                }

                group.Add((chunk, rank));
            }

            List<RetrievalChunk> ordered = new List<RetrievalChunk>();
            foreach (List<(RetrievalChunk Chunk, int Rank)> group in groups)
            {
                List<(RetrievalChunk Chunk, int Rank)> positioned = group
                    .OrderBy(item => item.Chunk.Position.HasValue ? 0 : 1)
                    .ThenBy(item => item.Chunk.Position ?? 0)
                    .ThenBy(item => item.Rank)
                    .ToList();

                ordered.AddRange(MergeAdjacent(positioned.Select(item => item.Chunk).ToList()));
            }

            return ordered;
        }

        /// <summary>
        /// Join two passages, dropping text the second repeats from the end of the first (chunk overlap).
        /// </summary>
        /// <param name="first">Earlier passage.</param>
        /// <param name="second">Later passage.</param>
        /// <returns>Joined text.</returns>
        public static string JoinWithoutOverlap(string first, string second)
        {
            if (String.IsNullOrEmpty(first)) return second ?? "";
            if (String.IsNullOrEmpty(second)) return first;

            int maxOverlap = Math.Min(Math.Min(first.Length, second.Length), _MaximumOverlapCharacters);
            for (int length = maxOverlap; length >= _MinimumOverlapCharacters; length--)
            {
                if (String.CompareOrdinal(first, first.Length - length, second, 0, length) == 0)
                    return first + second.Substring(length);
            }

            return first + "\n" + second;
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Merge chunks of one document whose covered positions (the chunk plus its neighbors) touch or overlap.
        /// </summary>
        private static List<RetrievalChunk> MergeAdjacent(List<RetrievalChunk> chunks)
        {
            List<RetrievalChunk> result = new List<RetrievalChunk>();
            List<RetrievalChunk> run = new List<RetrievalChunk>();
            int runEnd = Int32.MinValue;

            foreach (RetrievalChunk chunk in chunks)
            {
                (int start, int end)? span = CoveredSpan(chunk);
                if (span == null)
                {
                    Flush(run, result);
                    result.Add(chunk);
                    runEnd = Int32.MinValue;
                    continue;
                }

                if (run.Count > 0 && span.Value.start > runEnd + 1)
                {
                    Flush(run, result);
                    runEnd = Int32.MinValue;
                }

                run.Add(chunk);
                runEnd = Math.Max(runEnd, span.Value.end);
            }

            Flush(run, result);
            return result;
        }

        private static void Flush(List<RetrievalChunk> run, List<RetrievalChunk> result)
        {
            if (run.Count == 0) return;
            result.Add(run.Count == 1 ? run[0] : MergeRun(run));
            run.Clear();
        }

        private static (int start, int end)? CoveredSpan(RetrievalChunk chunk)
        {
            if (chunk?.Position == null) return null;

            int start = chunk.Position.Value;
            int end = chunk.Position.Value;
            if (chunk.Neighbors != null)
            {
                foreach (RetrievalChunk neighbor in chunk.Neighbors)
                {
                    if (neighbor?.Position == null) continue;
                    start = Math.Min(start, neighbor.Position.Value);
                    end = Math.Max(end, neighbor.Position.Value);
                }
            }

            return (start, end);
        }

        /// <summary>
        /// Merge a run of touching chunks into one passage: every covered position once, in document order, with
        /// repeated overlap text removed. Scores keep the best value in the run so ranking evidence is not lost.
        /// </summary>
        private static RetrievalChunk MergeRun(List<RetrievalChunk> run)
        {
            SortedDictionary<int, string> byPosition = new SortedDictionary<int, string>();
            foreach (RetrievalChunk chunk in run)
            {
                if (chunk.Neighbors != null)
                {
                    foreach (RetrievalChunk neighbor in chunk.Neighbors)
                    {
                        if (neighbor?.Position != null && neighbor.Content != null && !byPosition.ContainsKey(neighbor.Position.Value))
                            byPosition[neighbor.Position.Value] = neighbor.Content;
                    }
                }

                if (chunk.Position.HasValue && chunk.Content != null)
                    byPosition[chunk.Position.Value] = chunk.Content;
            }

            string merged = null;
            foreach (string text in byPosition.Values)
                merged = merged == null ? text : JoinWithoutOverlap(merged, text);

            RetrievalChunk best = run[0];
            return new RetrievalChunk
            {
                DocumentId = best.DocumentId,
                Position = run.Min(c => c.Position),
                Score = run.Max(c => c.Score),
                RerankScore = MaxOrNull(run.Select(c => c.RerankScore)),
                FusionScore = MaxOrNull(run.Select(c => c.FusionScore)),
                TextScore = MaxOrNull(run.Select(c => c.TextScore)),
                VectorScore = MaxOrNull(run.Select(c => c.VectorScore)),
                VectorRank = MinOrNull(run.Select(c => c.VectorRank)),
                TextRank = MinOrNull(run.Select(c => c.TextRank)),
                Content = merged ?? "",
                Neighbors = null
            };
        }

        private static double? MaxOrNull(IEnumerable<double?> values)
        {
            List<double> present = values.Where(v => v.HasValue).Select(v => v.Value).ToList();
            return present.Count > 0 ? present.Max() : null;
        }

        private static int? MinOrNull(IEnumerable<int?> values)
        {
            List<int> present = values.Where(v => v.HasValue).Select(v => v.Value).ToList();
            return present.Count > 0 ? present.Min() : null;
        }

        #endregion
    }
}
