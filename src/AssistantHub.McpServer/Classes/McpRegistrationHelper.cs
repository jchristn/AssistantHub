namespace AssistantHub.McpServer.Classes
{
    using System.Collections.Generic;
    using Voltaic;

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
                    args => definition.Handler(args));
            }
        }

        /// <summary>
        /// Register TCP methods.
        /// </summary>
        public static void RegisterTcpMethods(McpTcpServer server, IEnumerable<McpMethodDefinition> definitions)
        {
            foreach (McpMethodDefinition definition in definitions)
            {
                server.RegisterMethod(definition.Name, args => definition.Handler(args));
            }
        }

        /// <summary>
        /// Register WebSocket methods.
        /// </summary>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, IEnumerable<McpMethodDefinition> definitions)
        {
            foreach (McpMethodDefinition definition in definitions)
            {
                server.RegisterMethod(definition.Name, args => definition.Handler(args));
            }
        }
    }
}
