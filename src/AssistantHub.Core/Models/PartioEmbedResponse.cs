namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Typed response payload from Partio's standalone <c>POST /v1.0/embed</c> endpoint (Partio v0.4.0+).
    /// The endpoint embeds one or more input strings in a single batch, so <see cref="Embeddings"/> is a
    /// list of vectors -- one per input string, in request order.
    /// </summary>
    public class PartioEmbedResponse
    {
        /// <summary>
        /// Whether the embed request succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Result status code reported inside the response body. Partio may set this to a non-200 value
        /// (for example 429) alongside the HTTP status when a request is rejected.
        /// </summary>
        public int StatusCode { get; set; } = 200;

        /// <summary>
        /// Error text when the request fails.
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// Target embedding endpoint identifier.
        /// </summary>
        public string EndpointId { get; set; } = null;

        /// <summary>
        /// Target model name.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Returned embedding vectors, one per input string in the same order.
        /// </summary>
        public List<List<float>> Embeddings { get; set; } = new List<List<float>>();

        /// <summary>
        /// Number of embedding vectors returned.
        /// </summary>
        public int Count { get; set; } = 0;

        /// <summary>
        /// Number of dimensions in each returned embedding (0 when none).
        /// </summary>
        public int Dimensions { get; set; } = 0;

        /// <summary>
        /// Whether L2 normalization was applied to the returned vectors.
        /// </summary>
        public bool L2Normalization { get; set; } = false;

        /// <summary>
        /// Overall request duration, in milliseconds.
        /// </summary>
        public double ResponseTimeMs { get; set; } = 0;
    }
}
