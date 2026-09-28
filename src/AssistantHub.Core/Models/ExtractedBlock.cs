namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// One structural block of an extracted document: a heading, a run of text, a table or a list, with the page or
    /// sheet it came from.
    /// </summary>
    public class ExtractedBlock
    {
        /// <summary>
        /// Block kind: "Heading", "Text", "Table" or "List".
        /// </summary>
        public string Kind { get; set; } = "Text";

        /// <summary>
        /// Readable text of the block (markdown for tables and lists).
        /// </summary>
        public string Text { get; set; } = null;

        /// <summary>
        /// Heading level (1 = top level) for headings.
        /// </summary>
        public int? HeaderLevel { get; set; } = null;

        /// <summary>
        /// Page, slide or sheet number reported by the extractor, when known.
        /// </summary>
        public int? PageNumber { get; set; } = null;

        /// <summary>
        /// Spreadsheet sheet name, when known.
        /// </summary>
        public string SheetName { get; set; } = null;

        /// <summary>
        /// Table rows for tables, header row first.
        /// </summary>
        public List<List<string>> TableRows { get; set; } = null;

        /// <summary>
        /// List items for lists.
        /// </summary>
        public List<string> ListItems { get; set; } = null;

        /// <summary>
        /// Whether a list is ordered.
        /// </summary>
        public bool Ordered { get; set; } = false;
    }

    /// <summary>
    /// Result of extracting a document: the flat readable text and its structural blocks.
    /// </summary>
    public class AtomExtractionResult
    {
        /// <summary>
        /// Flat readable text (blocks separated by blank lines).
        /// </summary>
        public string Text { get; set; } = null;

        /// <summary>
        /// Structural blocks in reading order.
        /// </summary>
        public List<ExtractedBlock> Blocks { get; set; } = new List<ExtractedBlock>();

        /// <summary>
        /// Reason extraction failed or was refused, when it did.
        /// </summary>
        public string ErrorMessage { get; set; } = null;
    }
}
