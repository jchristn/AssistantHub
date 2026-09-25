namespace Test.Shared
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Client-side typed view of an MCP content block.
    /// </summary>
    public sealed class McpTextContentBlock
    {
        /// <summary>
        /// Content type, for example "text".
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Text payload.
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
