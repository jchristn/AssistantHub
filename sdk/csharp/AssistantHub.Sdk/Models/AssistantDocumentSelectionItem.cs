namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Safe public metadata for documents selectable in assistant chat.
    /// </summary>
    public class AssistantDocumentSelectionItem
    {
        /// <summary>
        /// Assistant document identifier.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Display name.
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Original filename.
        /// </summary>
        [JsonPropertyName("OriginalFilename")]
        public string OriginalFilename { get; set; }

        /// <summary>
        /// MIME content type.
        /// </summary>
        [JsonPropertyName("ContentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Size in bytes.
        /// </summary>
        [JsonPropertyName("SizeBytes")]
        public long SizeBytes { get; set; }

        /// <summary>
        /// Source URL when exposed by assistant settings.
        /// </summary>
        [JsonPropertyName("SourceUrl")]
        public string SourceUrl { get; set; }

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        [JsonPropertyName("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Last update timestamp.
        /// </summary>
        [JsonPropertyName("LastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; }
    }
}
