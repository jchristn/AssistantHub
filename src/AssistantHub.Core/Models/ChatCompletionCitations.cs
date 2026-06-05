namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Citation metadata included in chat completion responses.
    /// Contains a manifest of all source documents provided as context
    /// and the indices the model actually referenced in its answer.
    /// </summary>
    public class ChatCompletionCitations
    {
        /// <summary>
        /// Source documents provided as context to the model, indexed starting at 1.
        /// </summary>
        [JsonPropertyName("sources")]
        public List<CitationSource> Sources { get; set; } = new List<CitationSource>();

        /// <summary>
        /// Indices from Sources that the model actually cited in its response.
        /// Validated against the source manifest (invalid indices are excluded).
        /// When the model does not cite any sources, all source indices are
        /// populated as a fallback and AutoPopulated is set to true.
        /// </summary>
        [JsonPropertyName("referenced_indices")]
        public List<int> ReferencedIndices { get; set; } = new List<int>();

        /// <summary>
        /// True when the model did not produce inline citation markers and the
        /// system automatically populated ReferencedIndices with all source indices.
        /// Useful for diagnosing models that ignore citation instructions.
        /// </summary>
        [JsonPropertyName("auto_populated")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool AutoPopulated { get; set; } = false;
    }
}
