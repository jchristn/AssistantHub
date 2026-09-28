namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Extraction and intake settings for an ingestion rule: options passed to DocumentAtom when a document is
    /// extracted, and how duplicate uploads are handled.
    /// </summary>
    public class IngestionExtractionConfig
    {
        #region Public-Members

        /// <summary>
        /// Run OCR on images embedded in PDF, Word, PowerPoint, Excel and RTF documents. Null uses DocumentAtom's
        /// default (on).
        /// </summary>
        public bool? OcrEmbeddedImages { get; set; } = null;

        /// <summary>
        /// Treat the first row of a CSV or TSV file as its header row. Null uses DocumentAtom's default (true).
        /// </summary>
        public bool? CsvHasHeaderRow { get; set; } = null;

        /// <summary>
        /// Number of CSV or TSV rows per extracted table (0 or null keeps the whole file as one table).
        /// </summary>
        public int? CsvRowsPerAtom
        {
            get => _CsvRowsPerAtom;
            set => _CsvRowsPerAtom = (value == null || value.Value >= 0) ? value : throw new ArgumentOutOfRangeException(nameof(CsvRowsPerAtom));
        }

        /// <summary>
        /// Score a spreadsheet row must reach to be detected as a header row. Null uses DocumentAtom's default (3).
        /// </summary>
        public int? ExcelHeaderRowScoreThreshold
        {
            get => _ExcelHeaderRowScoreThreshold;
            set => _ExcelHeaderRowScoreThreshold = (value == null || value.Value >= 0) ? value : throw new ArgumentOutOfRangeException(nameof(ExcelHeaderRowScoreThreshold));
        }

        /// <summary>
        /// What to do when an upload's content exactly matches a document already in the rule's collection:
        /// "Allow" (ingest it), "Warn" (ingest it and record the match) or "Reject" (refuse the upload with 409).
        /// </summary>
        public string DuplicatePolicy
        {
            get => _DuplicatePolicy;
            set => _DuplicatePolicy = NormalizeDuplicatePolicy(value);
        }

        /// <summary>
        /// Similarity (0 to 1) at which another document in the collection is recorded as a near-duplicate after
        /// ingestion, using the new document's first chunk. 0 disables the check. Default 0.85.
        /// </summary>
        public double NearDuplicateThreshold
        {
            get => _NearDuplicateThreshold;
            set => _NearDuplicateThreshold = (value >= 0.0 && value <= 1.0) ? value : throw new ArgumentOutOfRangeException(nameof(NearDuplicateThreshold));
        }

        #endregion

        #region Private-Members

        private int? _CsvRowsPerAtom = null;
        private int? _ExcelHeaderRowScoreThreshold = null;
        private string _DuplicatePolicy = "Allow";
        private double _NearDuplicateThreshold = 0.85;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build the DocumentAtom processor settings for this configuration, or null when nothing is set.
        /// </summary>
        /// <param name="documentType">Detected document type (lowercase, e.g. "pdf", "tsv").</param>
        /// <returns>Settings object to send, or null.</returns>
        public Dictionary<string, object> ToDocumentAtomSettings(string documentType)
        {
            Dictionary<string, object> settings = new Dictionary<string, object>();
            if (OcrEmbeddedImages.HasValue) settings["ExtractAtomsFromImages"] = OcrEmbeddedImages.Value;
            if (CsvHasHeaderRow.HasValue) settings["HasHeaderRow"] = CsvHasHeaderRow.Value;
            if (CsvRowsPerAtom.HasValue && CsvRowsPerAtom.Value > 0) settings["RowsPerAtom"] = CsvRowsPerAtom.Value;
            if (ExcelHeaderRowScoreThreshold.HasValue) settings["HeaderRowScoreThreshold"] = ExcelHeaderRowScoreThreshold.Value;
            if (String.Equals(documentType, "tsv", StringComparison.OrdinalIgnoreCase)) settings["ColumnDelimiter"] = "\t";
            return settings.Count > 0 ? settings : null;
        }

        #endregion

        #region Private-Methods

        private static string NormalizeDuplicatePolicy(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "Allow";
            foreach (string policy in new[] { "Allow", "Warn", "Reject" })
            {
                if (value.Trim().Equals(policy, StringComparison.OrdinalIgnoreCase)) return policy;
            }
            throw new ArgumentOutOfRangeException(nameof(DuplicatePolicy), "DuplicatePolicy must be Allow, Warn or Reject.");
        }

        #endregion
    }
}
