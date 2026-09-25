namespace Test.Benchmark
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// One stage snapshot from the retrieve route.
    /// </summary>
    public class RetrieveStage
    {
        #region Public-Members

        /// <summary>Stage name.</summary>
        [JsonPropertyName("stage")]
        public string Stage { get; set; } = string.Empty;

        /// <summary>Query, for search stages.</summary>
        [JsonPropertyName("query")]
        public string? Query { get; set; } = null;

        /// <summary>Ranked chunks.</summary>
        [JsonPropertyName("chunks")]
        public List<RetrievedChunk> Chunks { get; set; } = new List<RetrievedChunk>();

        #endregion
    }
}
