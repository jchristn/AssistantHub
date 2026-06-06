namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request body for document Verbex reindex operations.
    /// </summary>
    public class DocumentReindexRequest
    {
        /// <summary>
        /// Optional explicit document identifiers to reindex.
        /// </summary>
        [JsonPropertyName("DocumentIds")]
        public List<string> DocumentIds { get; set; }

        /// <summary>
        /// Reindex documents even when Verbex metadata is already present.
        /// </summary>
        [JsonPropertyName("IncludeAlreadyIndexed")]
        public bool IncludeAlreadyIndexed { get; set; }
    }
}
