namespace Test.Benchmark.Datasets
{
    using System.Collections.Generic;

    /// <summary>
    /// A document to ingest. Either <see cref="Body"/> (text) or <see cref="File"/> (a native file) is set.
    /// </summary>
    public class BenchmarkDocument
    {
        #region Public-Members

        /// <summary>
        /// Document id, matched against query relevance labels.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Title (becomes the AssistantHub document name).
        /// </summary>
        public string? Title { get; set; } = null;

        /// <summary>
        /// Text content.
        /// </summary>
        public string? Body { get; set; } = null;

        /// <summary>
        /// Native file path, relative to the dataset file.
        /// </summary>
        public string? File { get; set; } = null;

        /// <summary>
        /// MIME type; inferred from the file extension when omitted.
        /// </summary>
        public string? ContentType { get; set; } = null;

        /// <summary>
        /// One-line summary (informational).
        /// </summary>
        public string? Summary { get; set; } = null;

        /// <summary>
        /// Labels attached to the AssistantHub document.
        /// </summary>
        public List<string>? Labels { get; set; } = null;

        /// <summary>
        /// Tags attached to the AssistantHub document.
        /// </summary>
        public Dictionary<string, string>? Tags { get; set; } = null;

        /// <summary>
        /// ISO date; dated documents are ingested in date order.
        /// </summary>
        public string? Date { get; set; } = null;

        /// <summary>
        /// Version of a versioned document.
        /// </summary>
        public string? Version { get; set; } = null;

        /// <summary>
        /// Id of the document this one replaces.
        /// </summary>
        public string? Supersedes { get; set; } = null;

        #endregion
    }
}
