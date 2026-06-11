namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// User-supplied file attachment for a single chat turn.
    /// </summary>
    public class ChatLocalAttachment
    {
        /// <summary>
        /// Display filename.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// MIME content type, when known.
        /// </summary>
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = null;

        /// <summary>
        /// Base64-encoded file bytes. May be a raw base64 string or data URL.
        /// </summary>
        [JsonPropertyName("base64_content")]
        public string Base64Content { get; set; } = null;

        /// <summary>
        /// Plain text content supplied directly by an SDK client.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = null;
    }
}
