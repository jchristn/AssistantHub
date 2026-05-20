namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using System.Net.Http;
    using AssistantHub.McpServer.Classes;
    using Voltaic;

    /// <summary>
    /// Registration methods for system operations.
    /// </summary>
    public static class SystemRegistrations
    {
        /// <summary>
        /// Register HTTP tools.
        /// </summary>
        public static void RegisterHttpTools(McpHttpServer server, AssistantHubMcpContext context)
        {
            McpRegistrationHelper.RegisterHttpTools(server, GetDefinitions(context));
        }

        /// <summary>
        /// Register TCP methods.
        /// </summary>
        public static void RegisterTcpMethods(McpTcpServer server, AssistantHubMcpContext context)
        {
            McpRegistrationHelper.RegisterTcpMethods(server, GetDefinitions(context));
        }

        /// <summary>
        /// Register WebSocket methods.
        /// </summary>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, AssistantHubMcpContext context)
        {
            McpRegistrationHelper.RegisterWebSocketMethods(server, GetDefinitions(context));
        }

        private static List<McpMethodDefinition> GetDefinitions(AssistantHubMcpContext context)
        {
            return new List<McpMethodDefinition>
            {
                new McpMethodDefinition
                {
                    Name = "system/health",
                    Description = "Check AssistantHub health using the root endpoint.",
                    InputSchema = McpRegistrationHelper.EmptySchema,
                    Handler = _ => AssistantHubMcpServerHelpers.Serialize(context, new
                    {
                        Healthy = context.Sdk.HealthCheckAsync().GetAwaiter().GetResult(),
                        Metadata = context.Sdk.HealthAsync().GetAwaiter().GetResult()
                    }, includeSecrets: true)
                },
                new McpMethodDefinition
                {
                    Name = "system/whoami",
                    Description = "Return the current authenticated identity.",
                    InputSchema = McpRegistrationHelper.EmptySchema,
                    Handler = _ => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.WhoAmIAsync().GetAwaiter().GetResult(), includeSecrets: true)
                },
                new McpMethodDefinition
                {
                    Name = "system/openapi",
                    Description = "Fetch the current AssistantHub OpenAPI document.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            versioned = new { type = "boolean", description = "If true, fetch /v1.0/openapi.json instead of /openapi.json." }
                        },
                        required = System.Array.Empty<string>()
                    },
                    Handler = args =>
                    {
                        bool versioned = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "versioned", false);
                        return AssistantHubMcpRestProxy.SendJson(context, HttpMethod.Get, versioned ? "/v1.0/openapi.json" : "/openapi.json");
                    }
                }
            };
        }
    }
}
