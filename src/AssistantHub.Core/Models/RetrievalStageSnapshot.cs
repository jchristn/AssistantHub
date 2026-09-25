namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// The ranked chunk list as it stood after one stage of the retrieval pipeline.
    /// </summary>
    public class RetrievalStageSnapshot
    {
        /// <summary>
        /// Stage name: <c>search</c> (one per issued query), <c>fused</c> (after multi-query fusion or merge),
        /// <c>attachment_filter</c>, <c>rerank</c>.
        /// </summary>
        [JsonPropertyName("stage")]
        public string Stage { get; set; } = null;

        /// <summary>
        /// The query issued, for <c>search</c> stages.
        /// </summary>
        [JsonPropertyName("query")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Query { get; set; } = null;

        /// <summary>
        /// Ranked chunks, best first.
        /// </summary>
        [JsonPropertyName("chunks")]
        public List<RetrievalChunk> Chunks { get; set; } = new List<RetrievalChunk>();
    }
}
