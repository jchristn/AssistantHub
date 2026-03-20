namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Typed response payload from Partio's embedding endpoint.
    /// </summary>
    public class PartioEmbedResponse
    {
        /// <summary>
        /// Returned embedding vector.
        /// </summary>
        public List<float> Embeddings { get; set; } = new List<float>();
    }
}
