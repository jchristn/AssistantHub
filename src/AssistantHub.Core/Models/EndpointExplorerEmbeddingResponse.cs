namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Response payload for embedding endpoint test requests.
    /// </summary>
    public class EndpointExplorerEmbeddingResponse
    {
        /// <summary>
        /// Whether the explorer request succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Explorer result status code.
        /// </summary>
        public int StatusCode { get; set; } = 200;

        /// <summary>
        /// Error text when the request fails.
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// Target embedding endpoint ID.
        /// </summary>
        public string EndpointId { get; set; } = null;

        /// <summary>
        /// Target model name.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Input text that was embedded.
        /// </summary>
        public string Input { get; set; } = null;

        /// <summary>
        /// Returned embedding vector.
        /// </summary>
        public List<float> Embedding { get; set; } = new List<float>();

        /// <summary>
        /// Number of dimensions in the returned embedding.
        /// </summary>
        public int Dimensions { get; set; } = 0;

        /// <summary>
        /// Overall request duration in milliseconds.
        /// </summary>
        public long ResponseTimeMs { get; set; } = 0;

        /// <summary>
        /// Related request-history entry ID when persisted.
        /// </summary>
        public string RequestHistoryId { get; set; } = null;

        /// <summary>
        /// Upstream embedding calls made by Partio for this explorer request.
        /// </summary>
        public List<EmbeddingCallDetail> EmbeddingCalls { get; set; } = new List<EmbeddingCallDetail>();
    }
}
