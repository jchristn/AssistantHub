namespace AssistantHub.McpServer.Classes
{
    using System;
    using System.Text.Json;

    /// <summary>
    /// MCP method definition.
    /// </summary>
    public class McpMethodDefinition
    {
        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Description.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Input schema.
        /// </summary>
        public object InputSchema { get; init; } = new { type = "object", properties = new { }, required = Array.Empty<string>() };

        /// <summary>
        /// Handler.
        /// </summary>
        public Func<JsonElement?, object?> Handler { get; init; } = _ => null;
    }
}
