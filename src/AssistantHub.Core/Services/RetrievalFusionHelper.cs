namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Shared retrieval result fusion helpers.
    /// </summary>
    public static class RetrievalFusionHelper
    {
        /// <summary>
        /// Fuse multiple ranked retrieval result lists using Reciprocal Rank Fusion.
        /// </summary>
        /// <param name="rankedResults">Ranked result lists in query order.</param>
        /// <param name="maxResults">Maximum fused results to return.</param>
        /// <param name="rrfK">RRF K constant. Defaults to 60.</param>
        /// <param name="weights">Optional weight per result list, in the same order (missing entries are 1.0). Lower
        /// weights let extra queries re-rank the main query's candidates without displacing them.</param>
        /// <returns>Fused, deduplicated chunks ordered by descending fusion score.</returns>
        public static List<RetrievalChunk> FuseByReciprocalRank(
            IEnumerable<IReadOnlyList<RetrievalChunk>> rankedResults,
            int maxResults,
            double rrfK = 60.0,
            IReadOnlyList<double> weights = null)
        {
            int listIndex = -1;
            Dictionary<string, double> rrfScores = new Dictionary<string, double>(StringComparer.Ordinal);
            Dictionary<string, RetrievalChunk> chunkMap = new Dictionary<string, RetrievalChunk>(StringComparer.Ordinal);

            foreach (IReadOnlyList<RetrievalChunk> results in rankedResults ?? Enumerable.Empty<IReadOnlyList<RetrievalChunk>>())
            {
                listIndex++;
                if (results == null) continue;
                double weight = weights != null && listIndex < weights.Count ? weights[listIndex] : 1.0;

                for (int rank = 0; rank < results.Count; rank++)
                {
                    RetrievalChunk chunk = results[rank];
                    if (chunk == null) continue;

                    string dedupeKey = BuildDedupeKey(chunk);
                    double rrfContribution = weight / (rrfK + rank + 1);

                    if (!rrfScores.ContainsKey(dedupeKey))
                    {
                        rrfScores[dedupeKey] = 0;
                        chunkMap[dedupeKey] = chunk;
                    }
                    else if (chunk.Score > chunkMap[dedupeKey].Score)
                    {
                        chunkMap[dedupeKey] = chunk;
                    }

                    rrfScores[dedupeKey] += rrfContribution;
                }
            }

            foreach (KeyValuePair<string, RetrievalChunk> kvp in chunkMap)
                kvp.Value.FusionScore = Math.Round(rrfScores[kvp.Key], 6);

            return chunkMap.Values
                .OrderByDescending(chunk => chunk.FusionScore)
                .Take(Math.Max(1, maxResults))
                .ToList();
        }

        private static string BuildDedupeKey(RetrievalChunk chunk)
        {
            return (chunk?.DocumentId ?? "") + ":" + (chunk?.Position.HasValue == true ? chunk.Position.Value.ToString() : "");
        }
    }
}
