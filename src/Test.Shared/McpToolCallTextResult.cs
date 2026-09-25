namespace Test.Shared
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Client-side typed view of an MCP tools/call result whose content is text.
    /// </summary>
    public sealed class McpToolCallTextResult
    {
        /// <summary>
        /// Content blocks returned by the tool.
        /// </summary>
        [JsonPropertyName("content")]
        public List<McpTextContentBlock> Content { get; set; } = new List<McpTextContentBlock>();

        /// <summary>
        /// True when the tool reported an execution error.
        /// </summary>
        [JsonPropertyName("isError")]
        public bool? IsError { get; set; }
    }
}
