namespace Test.Benchmark.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Matches gold evidence passages against chunk text. Both sides are normalized (HTML tags and entities removed,
    /// Markdown syntax and punctuation dropped, lowercased, whitespace collapsed), so extraction and chunking
    /// formatting differences do not count as misses. A passage split across a chunk boundary still counts when a
    /// chunk holds at least <see cref="TokenOverlapThreshold"/> of its tokens as a contiguous run.
    /// </summary>
    public static class EvidenceMatcher
    {
        #region Public-Members

        /// <summary>
        /// Fraction of a passage's tokens that must appear contiguously in one chunk for a partial match.
        /// </summary>
        public const double TokenOverlapThreshold = 0.8;

        #endregion

        #region Private-Members

        private static readonly Regex _Tags = new Regex(@"</?[a-zA-Z][a-zA-Z0-9]*(\s[^<>]*)?/?>", RegexOptions.Compiled);
        private static readonly Regex _NonWord = new Regex("[^\\p{L}\\p{N}]+", RegexOptions.Compiled);

        #endregion

        #region Public-Methods

        /// <summary>
        /// Normalize text for matching.
        /// </summary>
        /// <param name="text">Raw text.</param>
        /// <returns>Normalized text: lowercase words separated by single spaces.</returns>
        public static string Normalize(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string decoded = WebUtility.HtmlDecode(_Tags.Replace(text, " "));
            return _NonWord.Replace(decoded.ToLowerInvariant(), " ").Trim();
        }

        /// <summary>
        /// Whether a normalized chunk contains a normalized passage (exactly, or by the token-run threshold).
        /// </summary>
        /// <param name="normalizedChunk">Normalized chunk text.</param>
        /// <param name="normalizedPassage">Normalized evidence passage.</param>
        /// <returns>True on a match.</returns>
        public static bool Contains(string normalizedChunk, string normalizedPassage)
        {
            if (string.IsNullOrEmpty(normalizedPassage) || string.IsNullOrEmpty(normalizedChunk)) return false;
            if (Wrap(normalizedChunk).Contains(Wrap(normalizedPassage), StringComparison.Ordinal)) return true;

            string[] tokens = normalizedPassage.Split(' ');
            if (tokens.Length < 5) return false;
            int need = (int)Math.Ceiling(tokens.Length * TokenOverlapThreshold);
            string wrappedChunk = Wrap(normalizedChunk);

            // A contiguous run of `need` tokens from the start or the end covers the chunk-boundary case.
            string head = string.Join(" ", tokens.Take(need));
            string tail = string.Join(" ", tokens.Skip(tokens.Length - need));
            return wrappedChunk.Contains(Wrap(head), StringComparison.Ordinal) || wrappedChunk.Contains(Wrap(tail), StringComparison.Ordinal);
        }

        /// <summary>
        /// Fraction of evidence passages found in at least one of the chunks.
        /// </summary>
        /// <param name="evidence">Evidence passages (raw).</param>
        /// <param name="chunks">Chunk texts (raw).</param>
        /// <returns>Recall in [0, 1], or null when there is no evidence.</returns>
        public static double? Recall(IReadOnlyList<string>? evidence, IEnumerable<string> chunks)
        {
            if (evidence == null || evidence.Count == 0) return null;
            List<string> normalizedChunks = chunks.Select(Normalize).ToList();
            int found = 0;
            foreach (string passage in evidence)
            {
                string normalizedPassage = Normalize(passage);
                if (normalizedChunks.Any(c => Contains(c, normalizedPassage))) found++;
            }

            return (double)found / evidence.Count;
        }

        #endregion

        #region Private-Methods

        private static string Wrap(string normalized)
        {
            StringBuilder sb = new StringBuilder(normalized.Length + 2);
            return sb.Append(' ').Append(normalized).Append(' ').ToString();
        }

        #endregion
    }
}
