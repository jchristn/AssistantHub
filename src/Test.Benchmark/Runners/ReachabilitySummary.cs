namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;

    /// <summary>
    /// Evidence reachability: the share of gold evidence passages present in any stored chunk. It is the ceiling
    /// extraction and chunking set on retrieval, since a passage lost here cannot be recovered by any ranking change.
    /// </summary>
    public class ReachabilitySummary
    {
        #region Public-Members

        /// <summary>Evidence passages checked.</summary>
        public int Passages { get; set; } = 0;

        /// <summary>Passages found in a chunk of one of their relevant documents.</summary>
        public int Reachable { get; set; } = 0;

        /// <summary>Reachable share.</summary>
        public double Rate { get; set; } = 0;

        /// <summary>Reachable share per content type of the relevant document.</summary>
        public Dictionary<string, double> ByContentType { get; set; } = new Dictionary<string, double>();

        /// <summary>Passages per content type.</summary>
        public Dictionary<string, int> PassagesByContentType { get; set; } = new Dictionary<string, int>();

        /// <summary>Reachable share per query type.</summary>
        public Dictionary<string, double> ByQueryType { get; set; } = new Dictionary<string, double>();

        /// <summary>Stored chunks.</summary>
        public int Chunks { get; set; } = 0;

        /// <summary>Mean chunk length in characters.</summary>
        public double MeanChunkChars { get; set; } = 0;

        /// <summary>Chunks under 40 characters.</summary>
        public int TinyChunks { get; set; } = 0;

        /// <summary>Chunks whose normalized text duplicates another chunk of the same document.</summary>
        public int DuplicateChunks { get; set; } = 0;

        /// <summary>Documents with no stored chunks.</summary>
        public int DocumentsWithoutChunks { get; set; } = 0;

        /// <summary>Up to 25 unreachable passages (query, document, passage).</summary>
        public List<string> SampleUnreachable { get; set; } = new List<string>();

        #endregion
    }
}
