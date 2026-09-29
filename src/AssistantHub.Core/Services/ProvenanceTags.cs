namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Chunk tags AssistantHub writes at ingestion to record where a chunk came from. Page numbers are zero-padded so
    /// RecallDB's string comparisons (GreaterThan, LessThan) order them correctly in metadata filters.
    /// </summary>
    public static class ProvenanceTags
    {
        #region Public-Members

        /// <summary>
        /// First page or slide of the chunk.
        /// </summary>
        public const string PageStart = "ah_page_start";

        /// <summary>
        /// Last page or slide of the chunk.
        /// </summary>
        public const string PageEnd = "ah_page_end";

        /// <summary>
        /// Spreadsheet sheet name.
        /// </summary>
        public const string Sheet = "ah_sheet";

        /// <summary>
        /// Heading path of the chunk's section, for example "Setup > Networking".
        /// </summary>
        public const string Section = "ah_section";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Format a page number for a tag value (five digits, zero-padded).
        /// </summary>
        /// <param name="page">Page number.</param>
        /// <returns>Tag value.</returns>
        public static string FormatPage(int page)
        {
            return Math.Max(0, page).ToString("D5", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Work out page and sheet provenance for chunks cut from a document's flat text (Flat cell mode), where
        /// Partio chunks the whole text at once. Each chunk is located in the text by its opening words, and its span is
        /// mapped to the pages and sheet of the extracted blocks it covers. Whitespace is normalized on both sides, and
        /// a chunk that cannot be located gets no tags.
        /// </summary>
        /// <param name="content">The extracted text that was chunked.</param>
        /// <param name="blocks">Extracted blocks, in reading order.</param>
        /// <param name="chunkTexts">Chunk texts, in chunk order.</param>
        /// <returns>One tag dictionary per chunk (possibly empty).</returns>
        public static List<Dictionary<string, string>> MapFlatChunks(string content, IList<ExtractedBlock> blocks, IList<string> chunkTexts)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
            int count = chunkTexts?.Count ?? 0;
            for (int i = 0; i < count; i++) result.Add(new Dictionary<string, string>());
            if (String.IsNullOrEmpty(content) || blocks == null || blocks.Count == 0 || count == 0) return result;
            if (!blocks.Any(b => b != null && ((b.PageNumber.HasValue && b.PageNumber.Value > 0) || !String.IsNullOrWhiteSpace(b.SheetName)))) return result;

            string text = Normalize(content);

            // Block start offsets in the normalized text.
            List<(int Offset, int? Page, string Sheet)> spans = new List<(int Offset, int? Page, string Sheet)>();
            int cursor = 0;
            foreach (ExtractedBlock block in blocks)
            {
                if (block == null) continue;
                string anchor = Anchor(Normalize(block.Text));
                if (anchor.Length == 0) continue;
                int at = text.IndexOf(anchor, cursor, StringComparison.Ordinal);
                if (at < 0) continue;
                int? page = block.PageNumber.HasValue && block.PageNumber.Value > 0 ? block.PageNumber : null;
                spans.Add((at, page, String.IsNullOrWhiteSpace(block.SheetName) ? null : block.SheetName));
                cursor = at + 1;
            }

            if (spans.Count == 0) return result;

            int searchFrom = 0;
            for (int i = 0; i < count; i++)
            {
                string chunk = Normalize(chunkTexts[i]);
                string anchor = Anchor(chunk);
                if (anchor.Length == 0) continue;

                // Chunks come in order but may overlap, so search from a little before the previous chunk.
                int start = text.IndexOf(anchor, Math.Max(0, searchFrom), StringComparison.Ordinal);
                if (start < 0) start = text.IndexOf(anchor, StringComparison.Ordinal);
                if (start < 0) continue;
                int length = chunk.TrimStart('#', ' ').Length;
                int end = Math.Min(text.Length, start + Math.Max(1, length)) - 1;
                searchFrom = start;

                List<(int Offset, int? Page, string Sheet)> covered = spans.Where(s => s.Offset <= end).ToList();
                int firstIndex = covered.FindLastIndex(s => s.Offset <= start);
                if (firstIndex < 0) firstIndex = 0;
                covered = covered.Skip(firstIndex).ToList();

                List<int> pages = covered.Where(s => s.Page.HasValue).Select(s => s.Page.Value).ToList();
                if (pages.Count > 0)
                {
                    result[i][PageStart] = FormatPage(pages.Min());
                    result[i][PageEnd] = FormatPage(pages.Max());
                }

                string sheet = covered.Select(s => s.Sheet).FirstOrDefault(s => s != null);
                if (sheet != null) result[i][Sheet] = sheet;
            }

            return result;
        }

        /// <summary>
        /// Copy provenance from a chunk's tags onto a retrieval chunk.
        /// </summary>
        /// <param name="chunk">Retrieval chunk.</param>
        /// <param name="tags">Tags returned with the chunk, or null.</param>
        public static void Apply(RetrievalChunk chunk, IDictionary<string, string> tags)
        {
            if (chunk == null || tags == null) return;
            chunk.PageStart = ParsePage(tags, PageStart);
            chunk.PageEnd = ParsePage(tags, PageEnd) ?? chunk.PageStart;
            if (tags.TryGetValue(Sheet, out string sheet) && !String.IsNullOrWhiteSpace(sheet)) chunk.Sheet = sheet;
            if (tags.TryGetValue(Section, out string section) && !String.IsNullOrWhiteSpace(section)) chunk.Section = section;
        }

        #endregion

        #region Private-Methods

        private static readonly Regex _Whitespace = new Regex("\\s+", RegexOptions.Compiled);

        private static string Normalize(string value)
        {
            return String.IsNullOrEmpty(value) ? "" : _Whitespace.Replace(value, " ").Trim();
        }

        private static string Anchor(string normalized)
        {
            // The opening words, stripped of markdown heading marks the chunker may keep or drop.
            string trimmed = normalized.TrimStart('#', ' ');
            return trimmed.Length > 60 ? trimmed.Substring(0, 60) : trimmed;
        }

        private static int? ParsePage(IDictionary<string, string> tags, string key)
        {
            if (!tags.TryGetValue(key, out string value) || String.IsNullOrWhiteSpace(value)) return null;
            return Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int page) && page > 0 ? page : null;
        }

        #endregion
    }
}
