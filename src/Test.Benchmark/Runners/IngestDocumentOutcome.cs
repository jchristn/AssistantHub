namespace Test.Benchmark.Runners
{
    /// <summary>
    /// The ingest outcome of one document.
    /// </summary>
    public class IngestDocumentOutcome
    {
        #region Public-Members

        /// <summary>Corpus id.</summary>
        public string Corpus { get; set; } = string.Empty;

        /// <summary>Dataset document id.</summary>
        public string DocumentId { get; set; } = string.Empty;

        /// <summary>Uploaded content type.</summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>Uploaded bytes.</summary>
        public long Bytes { get; set; } = 0;

        /// <summary>Final status.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Final status message.</summary>
        public string? Message { get; set; } = null;

        /// <summary>Upload-to-terminal milliseconds.</summary>
        public double ElapsedMs { get; set; } = 0;

        /// <summary>Chunks stored.</summary>
        public int Chunks { get; set; } = 0;

        #endregion
    }
}
