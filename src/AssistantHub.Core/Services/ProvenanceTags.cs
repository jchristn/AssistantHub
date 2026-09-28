namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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

        private static int? ParsePage(IDictionary<string, string> tags, string key)
        {
            if (!tags.TryGetValue(key, out string value) || String.IsNullOrWhiteSpace(value)) return null;
            return Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int page) && page > 0 ? page : null;
        }

        #endregion
    }
}
