namespace Test.Benchmark.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Standard ranked-retrieval metrics over a ranked list of document ids and relevance labels. nDCG uses linear
    /// gain (gain = grade), matching pytrec_eval and BEIR, so document-level results are comparable to published
    /// numbers.
    /// </summary>
    public static class RetrievalMetrics
    {
        #region Public-Methods

        /// <summary>
        /// Collapse a ranked chunk list to a ranked document list by first occurrence (the MaxP convention).
        /// </summary>
        /// <param name="chunkDocumentIds">Document id of each chunk, best first.</param>
        /// <returns>Distinct document ids in rank order.</returns>
        public static List<string> DocumentRanking(IEnumerable<string> chunkDocumentIds)
        {
            List<string> ranked = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in chunkDocumentIds)
            {
                if (!string.IsNullOrEmpty(id) && seen.Add(id)) ranked.Add(id);
            }

            return ranked;
        }

        /// <summary>
        /// Fraction of the relevant documents in the top k.
        /// </summary>
        /// <param name="ranked">Ranked document ids.</param>
        /// <param name="relevant">Relevant ids.</param>
        /// <param name="k">Cutoff.</param>
        /// <returns>Recall in [0, 1].</returns>
        public static double RecallAtK(IReadOnlyList<string> ranked, IReadOnlyCollection<string> relevant, int k)
        {
            if (relevant == null || relevant.Count == 0) return 0.0;
            int found = ranked.Take(k).Distinct(StringComparer.Ordinal).Count(id => relevant.Contains(id));
            return (double)found / relevant.Count;
        }

        /// <summary>
        /// 1 when every relevant document is in the top k.
        /// </summary>
        /// <param name="ranked">Ranked document ids.</param>
        /// <param name="relevant">Relevant ids.</param>
        /// <param name="k">Cutoff.</param>
        /// <returns>1 or 0.</returns>
        public static double AllAtK(IReadOnlyList<string> ranked, IReadOnlyCollection<string> relevant, int k)
        {
            if (relevant == null || relevant.Count == 0) return 0.0;
            HashSet<string> top = new HashSet<string>(ranked.Take(k), StringComparer.Ordinal);
            return relevant.All(id => top.Contains(id)) ? 1.0 : 0.0;
        }

        /// <summary>
        /// Reciprocal rank of the first relevant document within the top k.
        /// </summary>
        /// <param name="ranked">Ranked document ids.</param>
        /// <param name="relevant">Relevant ids.</param>
        /// <param name="k">Cutoff.</param>
        /// <returns>1/rank or 0.</returns>
        public static double ReciprocalRank(IReadOnlyList<string> ranked, IReadOnlyCollection<string> relevant, int k)
        {
            if (relevant == null || relevant.Count == 0) return 0.0;
            int limit = Math.Min(k, ranked.Count);
            for (int i = 0; i < limit; i++)
            {
                if (relevant.Contains(ranked[i])) return 1.0 / (i + 1);
            }

            return 0.0;
        }

        /// <summary>
        /// Normalized discounted cumulative gain at k.
        /// </summary>
        /// <param name="ranked">Ranked document ids.</param>
        /// <param name="gain">Gain of a document id (0 when not relevant).</param>
        /// <param name="idealGains">Gains of all relevant documents (for the ideal ranking).</param>
        /// <param name="k">Cutoff.</param>
        /// <returns>nDCG in [0, 1].</returns>
        public static double NdcgAtK(IReadOnlyList<string> ranked, Func<string, int> gain, IEnumerable<int> idealGains, int k)
        {
            double dcg = 0.0;
            HashSet<string> counted = new HashSet<string>(StringComparer.Ordinal);
            int limit = Math.Min(k, ranked.Count);
            for (int i = 0; i < limit; i++)
            {
                if (!counted.Add(ranked[i])) continue;
                int g = gain(ranked[i]);
                if (g > 0) dcg += g / Math.Log2(i + 2);
            }

            double idcg = 0.0;
            List<int> ideal = idealGains.Where(g => g > 0).OrderByDescending(g => g).Take(k).ToList();
            for (int i = 0; i < ideal.Count; i++)
            {
                idcg += ideal[i] / Math.Log2(i + 2);
            }

            return idcg > 0.0 ? dcg / idcg : 0.0;
        }

        #endregion
    }
}
