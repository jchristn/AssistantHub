namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Citation metadata included in chat completion responses.
    /// </summary>
    public class ChatCompletionCitations
    {
        /// <summary>
        /// Source documents provided as context to the model.
        /// </summary>
        [JsonPropertyName("sources")]
        public List<CitationSource> Sources { get; set; }

        /// <summary>
        /// Indices from Sources that the model actually cited.
        /// </summary>
        [JsonPropertyName("referenced_indices")]
        public List<int> ReferencedIndices { get; set; }

        /// <summary>
        /// True when the system automatically populated ReferencedIndices.
        /// </summary>
        [JsonPropertyName("auto_populated")]
        public bool AutoPopulated { get; set; }
    }
}
