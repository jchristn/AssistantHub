namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Core.Settings;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for configuration operations.
    /// </summary>
    public static class ConfigurationRegistrations
    {
        public static void RegisterHttpTools(McpHttpServer server, AssistantHubMcpContext context) => McpRegistrationHelper.RegisterHttpTools(server, GetDefinitions(context));
        public static void RegisterTcpMethods(McpTcpServer server, AssistantHubMcpContext context) => McpRegistrationHelper.RegisterTcpMethods(server, GetDefinitions(context));
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, AssistantHubMcpContext context) => McpRegistrationHelper.RegisterWebSocketMethods(server, GetDefinitions(context));

        private static List<McpMethodDefinition> GetDefinitions(AssistantHubMcpContext context)
        {
            return new List<McpMethodDefinition>
            {
                new()
                {
                    Name = "configuration/get",
                    Description = "Get the current AssistantHub server configuration.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            includeSecrets = new { type = "boolean", description = "If true, include secret-bearing fields." }
                        },
                        required = System.Array.Empty<string>()
                    },
                    Handler = args =>
                    {
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetConfigAsync().GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "configuration/update",
                    Description = "Replace the current AssistantHub server configuration.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            configurationJson = new { type = "string", description = "AssistantHubSettings serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include secret-bearing fields in the response." }
                        },
                        required = new[] { "configurationJson" }
                    },
                    Handler = args =>
                    {
                        AssistantHubSettings configuration = AssistantHubMcpServerHelpers.DeserializeRequired<AssistantHubSettings>(args, "configurationJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        string payload = AssistantHub.Core.Helpers.Serializer.SerializeJson(configuration, true)!;
                        string result = AssistantHubMcpRestProxy.SendJson(context, System.Net.Http.HttpMethod.Put, "/v1.0/configuration", payload);
                        return AssistantHubMcpServerHelpers.SerializeJsonString(context, result, includeSecrets);
                    }
                }
            };
        }
    }
}
