namespace Test.Shared
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Client-side typed view of a tool entry in an MCP tools/list result.
    /// </summary>
    public sealed class McpToolNameView
    {
        /// <summary>
        /// Tool name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
