namespace AssistantHub.McpServer.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using AssistantHub.Core.Telemetry;
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
                McpMethodDefinition instrumented = Instrument(definition, "http");
                server.RegisterTool(
                    instrumented.Name,
                    instrumented.Description,
                    instrumented.InputSchema,
                    parameters => instrumented.Handler(ToJsonElement(parameters)));
            }
        }

        /// <summary>
        /// Register TCP methods.
        /// </summary>
        public static void RegisterTcpMethods(McpTcpServer server, IEnumerable<McpMethodDefinition> definitions)
        {
            foreach (McpMethodDefinition definition in definitions)
            {
                McpMethodDefinition instrumented = Instrument(definition, "tcp");
                server.RegisterMethod(instrumented.Name, parameters => ToObjectResult(instrumented.Handler(ToJsonElement(parameters))));
            }
        }

        /// <summary>
        /// Register WebSocket methods.
        /// </summary>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, IEnumerable<McpMethodDefinition> definitions)
        {
            foreach (McpMethodDefinition definition in definitions)
            {
                McpMethodDefinition instrumented = Instrument(definition, "ws");
                server.RegisterMethod(instrumented.Name, parameters => ToObjectResult(instrumented.Handler(ToJsonElement(parameters))));
            }
        }

        /// <summary>
        /// Wrap a tool definition's handler so every invocation is timed and traced, tagged by tool name and
        /// transport. Metrics and spans ride the AssistantHub meter/activity source and stay a no-op until a
        /// telemetry host subscribes.
        /// </summary>
        private static McpMethodDefinition Instrument(McpMethodDefinition definition, string transport)
        {
            Func<JsonElement?, object?> inner = definition.Handler;
            string toolName = definition.Name;

            return new McpMethodDefinition
            {
                Name = definition.Name,
                Description = definition.Description,
                InputSchema = definition.InputSchema,
                Handler = input =>
                {
                    using (McpToolScope scope = AssistantHubTelemetry.StartMcpTool(toolName, transport))
                    {
                        try
                        {
                            return inner(input);
                        }
                        catch (Exception e)
                        {
                            scope.Fail(e);
                            throw;
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Shape a handler's output as a JSON-RPC method result for the TCP and WebSocket transports, where every
        /// result must be a JSON object. Handlers return JSON text: an object is returned as that object, and any
        /// other value (an array, string, number, boolean or null) is wrapped as <c>{"result": value}</c>.
        /// </summary>
        private static object ToObjectResult(object? value)
        {
            JsonElement element;
            if (value is string text)
            {
                try
                {
                    using JsonDocument document = JsonDocument.Parse(text);
                    element = document.RootElement.Clone();
                }
                catch (JsonException)
                {
                    element = JsonSerializer.SerializeToElement(text);
                }
            }
            else
            {
                element = JsonSerializer.SerializeToElement(value);
            }

            if (element.ValueKind == JsonValueKind.Object) return element;
            return new Dictionary<string, JsonElement> { ["result"] = element };
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
