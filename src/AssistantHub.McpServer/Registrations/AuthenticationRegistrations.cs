namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for authentication operations.
    /// </summary>
    public static class AuthenticationRegistrations
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
                    Name = "auth/authenticate",
                    Description = "Authenticate using email, password, and tenant and return the bearer token result.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            requestJson = new { type = "string", description = "AuthenticateRequest serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include the bearer token in the response." }
                        },
                        required = new[] { "requestJson" }
                    },
                    Handler = args =>
                    {
                        AuthenticateRequest request = AssistantHubMcpServerHelpers.DeserializeRequired<AuthenticateRequest>(args, "requestJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        AuthenticateResult result = context.Sdk.AuthenticateAsync(request).GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets);
                    }
                }
            };
        }
    }
}
