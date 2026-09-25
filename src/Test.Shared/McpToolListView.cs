namespace Test.Shared
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Client-side typed view of an MCP tools/list result.
    /// </summary>
    public sealed class McpToolListView
    {
        /// <summary>
        /// Tools on this page.
        /// </summary>
        [JsonPropertyName("tools")]
        public List<McpToolNameView> Tools { get; set; } = new List<McpToolNameView>();

        /// <summary>
        /// Cursor for the next page, or null when this is the last page.
        /// </summary>
        [JsonPropertyName("nextCursor")]
        public string? NextCursor { get; set; }
    }
}
