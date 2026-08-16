namespace AssistantHub.McpServer.Classes
{
    using System.Collections.Generic;
    using System.Text.Json;
    using Voltaic.Core;
    using Voltaic.Mcp;

    /// <summary>
    /// MCP registration helper.
    /// </summary>
    internal static class McpRegistrationHelper
    {
        /// <summary>
        /// Empty input schema.
        /// </summary>
        public static object EmptySchema =>
            new
            {
                type = "object",
                properties = new { },
                required = System.Array.Empty<string>()
            };

        /// <summary>
        /// Register HTTP tools.
        /// </summary>
        public static void RegisterHttpTools(McpHttpServer server, IEnumerable<McpMethodDefinition> definitions)
        {
            foreach (McpMethodDefinition definition in definitions)
            {
                server.RegisterTool(
                    definition.Name,
                    definition.Description,
                    definition.InputSchema,
                    parameters => definition.Handler(ToJsonElement(parameters)));
            }
        }

        /// <summary>
        /// Register TCP methods.
        /// </summary>
        public static void RegisterTcpMethods(McpTcpServer server, IEnumerable<McpMethodDefinition> definitions)
        {
            foreach (McpMethodDefinition definition in definitions)
            {
                server.RegisterMethod(definition.Name, parameters => definition.Handler(ToJsonElement(parameters)));
            }
        }

        /// <summary>
        /// Register WebSocket methods.
        /// </summary>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, IEnumerable<McpMethodDefinition> definitions)
        {
            foreach (McpMethodDefinition definition in definitions)
            {
                server.RegisterMethod(definition.Name, parameters => definition.Handler(ToJsonElement(parameters)));
            }
        }

        /// <summary>
        /// Convert Voltaic RPC parameters into a JsonElement so tool handlers can consume them.
        /// </summary>
        private static JsonElement? ToJsonElement(RpcParameters parameters)
        {
            if (parameters == null || !parameters.HasValue || string.IsNullOrEmpty(parameters.RawJson))
                return null;

            using JsonDocument document = JsonDocument.Parse(parameters.RawJson);
            return document.RootElement.Clone();
        }
    }
}
