namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using AssistantHub.Core.Models;

    /// <summary>
    /// One cell to send to Partio in Structured mode.
    /// </summary>
    public class StructuredCell
    {
        /// <summary>
        /// Partio cell type: "Text", "Table" or "List".
        /// </summary>
        public string Type { get; set; } = "Text";

        /// <summary>
        /// Text for Text cells.
        /// </summary>
        public string Text { get; set; } = null;

        /// <summary>
        /// Rows for Table cells, header row first.
        /// </summary>
        public List<List<string>> Table { get; set; } = null;

        /// <summary>
        /// Items for List cells.
        /// </summary>
        public List<string> Items { get; set; } = null;

        /// <summary>
        /// Whether a List cell is ordered.
        /// </summary>
        public bool Ordered { get; set; } = false;

        /// <summary>
        /// Chunking strategy for this cell.
        /// </summary>
        public string Strategy { get; set; } = null;

        /// <summary>
        /// Text embedded ahead of every chunk of this cell (task prefix and context header); not stored.
        /// </summary>
        public string ContextPrefix { get; set; } = null;

        /// <summary>
        /// First page of the cell's content, when known.
        /// </summary>
        public int? PageStart { get; set; } = null;

        /// <summary>
        /// Last page of the cell's content, when known.
        /// </summary>
        public int? PageEnd { get; set; } = null;

        /// <summary>
        /// Sheet name, when known.
        /// </summary>
        public string Sheet { get; set; } = null;

        /// <summary>
        /// Heading path of the section, for example "Setup > Networking".
        /// </summary>
        public string Section { get; set; } = null;

        /// <summary>
        /// Provenance tags for this cell's chunks.
        /// </summary>
        /// <returns>Tag dictionary (possibly empty).</returns>
        public Dictionary<string, string> ProvenanceTagValues()
        {
            Dictionary<string, string> tags = new Dictionary<string, string>();
            if (PageStart.HasValue) tags[ProvenanceTags.PageStart] = ProvenanceTags.FormatPage(PageStart.Value);
            if (PageEnd.HasValue) tags[ProvenanceTags.PageEnd] = ProvenanceTags.FormatPage(PageEnd.Value);
            if (!String.IsNullOrWhiteSpace(Sheet)) tags[ProvenanceTags.Sheet] = Sheet;
            if (!String.IsNullOrWhiteSpace(Section)) tags[ProvenanceTags.Section] = Section.Length > 500 ? Section.Substring(0, 500) : Section;
            return tags;
        }
    }

    /// <summary>
    /// Groups a document's extracted blocks into Partio cells for Structured mode: each run of text under one heading
    /// path becomes a Text cell, and each table and list its own cell. Every cell carries its heading path and the
    /// pages or sheet it covers.
    /// </summary>
    public static class StructuredCellBuilder
    {
        #region Public-Methods

        /// <summary>
        /// Build the cells for a document.
        /// </summary>
        /// <param name="blocks">Extracted blocks in reading order.</param>
        /// <param name="title">Document title (for context headers).</param>
        /// <param name="chunking">Chunking configuration (strategies, ContextHeader, FixedTokenCount).</param>
        /// <param name="documentPrefix">Embedding task prefix for documents (may be empty).</param>
        /// <returns>Cells in reading order.</returns>
        public static List<StructuredCell> Build(List<ExtractedBlock> blocks, string title, IngestionChunkingConfig chunking, string documentPrefix)
        {
            chunking ??= new IngestionChunkingConfig();
            List<StructuredCell> cells = new List<StructuredCell>();
            if (blocks == null || blocks.Count == 0) return cells;

            List<(int Level, string Text)> headings = new List<(int Level, string Text)>();
            List<ExtractedBlock> textRun = new List<ExtractedBlock>();

            void FlushText()
            {
                if (textRun.Count == 0) return;
                string text = String.Join("\n\n", textRun.Select(b => b.Kind == "Heading"
                    ? new string('#', Math.Max(1, b.HeaderLevel ?? 1)) + " " + b.Text
                    : b.Text).Where(t => !String.IsNullOrWhiteSpace(t)));
                if (!String.IsNullOrWhiteSpace(text) && textRun.Any(b => b.Kind != "Heading"))
                {
                    StructuredCell cell = NewCell("Text", textRun, headings, title, chunking, documentPrefix);
                    cell.Text = text;
                    cell.Strategy = String.Equals(chunking.Strategy, "None", StringComparison.OrdinalIgnoreCase) ? "FixedTokenCount" : chunking.Strategy;
                    cells.Add(cell);
                    textRun.Clear();
                }
            }

            foreach (ExtractedBlock block in blocks)
            {
                if (block == null) continue;
                switch (block.Kind)
                {
                    case "Heading":
                        // A heading closes the current section. Keep a heading that has no text yet (for example a
                        // title followed by a subtitle) in the run so it is stored with the section it introduces.
                        if (textRun.Any(b => b.Kind != "Heading")) FlushText();
                        int level = Math.Max(1, block.HeaderLevel ?? 1);
                        headings.RemoveAll(h => h.Level >= level);
                        headings.Add((level, block.Text?.Trim() ?? ""));
                        textRun.Add(block);
                        break;

                    case "Table":
                        FlushText();
                        if (block.TableRows != null && block.TableRows.Count >= 2)
                        {
                            StructuredCell table = NewCell("Table", new List<ExtractedBlock> { block }, headings, title, chunking, documentPrefix);
                            table.Table = block.TableRows;
                            table.Strategy = chunking.TableStrategy;
                            cells.Add(table);
                        }
                        else if (!String.IsNullOrWhiteSpace(block.Text))
                        {
                            textRun.Add(new ExtractedBlock { Kind = "Text", Text = block.Text, PageNumber = block.PageNumber, SheetName = block.SheetName });
                        }
                        break;

                    case "List":
                        FlushText();
                        if (block.ListItems != null && block.ListItems.Count > 0)
                        {
                            StructuredCell list = NewCell("List", new List<ExtractedBlock> { block }, headings, title, chunking, documentPrefix);
                            list.Items = block.ListItems;
                            list.Ordered = block.Ordered;
                            list.Strategy = chunking.ListStrategy;
                            cells.Add(list);
                        }
                        break;

                    default:
                        if (!String.IsNullOrWhiteSpace(block.Text)) textRun.Add(block);
                        break;
                }
            }

            FlushText();
            if (textRun.Count > 0)
            {
                // Only headings remained; store them as text so no extracted content is lost.
                StructuredCell cell = NewCell("Text", textRun, headings, title, chunking, documentPrefix);
                cell.Text = String.Join("\n\n", textRun.Select(b => b.Text));
                cell.Strategy = chunking.Strategy;
                cells.Add(cell);
            }

            return cells;
        }

        /// <summary>
        /// Build the context header for a section.
        /// </summary>
        /// <param name="contextHeader">None, Title or TitleAndHeadings.</param>
        /// <param name="title">Document title.</param>
        /// <param name="section">Section heading path.</param>
        /// <param name="maxCharacters">Maximum header length; longer headers are cut at a word boundary.</param>
        /// <returns>Header line ending in a newline, or an empty string.</returns>
        public static string BuildHeader(string contextHeader, string title, string section, int maxCharacters)
        {
            if (String.Equals(contextHeader, "None", StringComparison.OrdinalIgnoreCase) || String.IsNullOrWhiteSpace(contextHeader)) return "";

            List<string> parts = new List<string>();
            if (!String.IsNullOrWhiteSpace(title)) parts.Add(title.Trim());
            if (String.Equals(contextHeader, "TitleAndHeadings", StringComparison.OrdinalIgnoreCase) && !String.IsNullOrWhiteSpace(section))
                parts.Add(section.Trim());
            if (parts.Count == 0) return "";

            string header = String.Join(" > ", parts);
            if (maxCharacters > 0 && header.Length > maxCharacters)
            {
                int cut = header.LastIndexOf(' ', Math.Max(0, maxCharacters - 1));
                header = header.Substring(0, cut > 0 ? cut : maxCharacters).TrimEnd();
            }

            return header + "\n";
        }

        #endregion

        #region Private-Methods

        private static StructuredCell NewCell(
            string type,
            List<ExtractedBlock> sourceBlocks,
            List<(int Level, string Text)> headings,
            string title,
            IngestionChunkingConfig chunking,
            string documentPrefix)
        {
            List<int> pages = sourceBlocks.Where(b => b.PageNumber.HasValue && b.PageNumber.Value > 0).Select(b => b.PageNumber.Value).ToList();
            string sheet = sourceBlocks.Select(b => b.SheetName).FirstOrDefault(s => !String.IsNullOrWhiteSpace(s));
            string section = headings.Count > 0 ? String.Join(" > ", headings.Select(h => h.Text).Where(t => t.Length > 0)) : null;

            // The header may use at most a quarter of the chunk budget (about four characters per token).
            string header = BuildHeader(chunking.ContextHeader, title, section, Math.Max(40, chunking.FixedTokenCount));
            return new StructuredCell
            {
                Type = type,
                ContextPrefix = (documentPrefix ?? "") + header,
                PageStart = pages.Count > 0 ? pages.Min() : null,
                PageEnd = pages.Count > 0 ? pages.Max() : null,
                Sheet = sheet,
                Section = String.IsNullOrWhiteSpace(section) ? null : section
            };
        }

        #endregion
    }
}
