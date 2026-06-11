namespace AssistantHub.Sdk.Models
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
        public string Name { get; set; }

        /// <summary>
        /// MIME content type.
        /// </summary>
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        /// <summary>
        /// Base64-encoded file bytes.
        /// </summary>
        [JsonPropertyName("base64_content")]
        public string Base64Content { get; set; }

        /// <summary>
        /// Plain text content supplied directly by the SDK client.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
