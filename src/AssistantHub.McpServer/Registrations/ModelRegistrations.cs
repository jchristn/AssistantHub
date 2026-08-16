namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using System.Net.Http;
    using AssistantHub.McpServer.Classes;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for model management operations.
    /// </summary>
    public static class ModelRegistrations
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
                    Name = "model/list",
                    Description = "List available inference models, optionally scoped to an assistant endpoint.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Optional assistant identifier." }
                        },
                        required = System.Array.Empty<string>()
                    },
                    Handler = args =>
                    {
                        string? assistantId = AssistantHubMcpServerHelpers.GetStringOptional(args, "assistantId");
                        object result = string.IsNullOrWhiteSpace(assistantId)
                            ? context.Sdk.ListModelsAsync().GetAwaiter().GetResult()
                            : context.Sdk.ListModelsAsync(assistantId).GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "model/pull",
                    Description = "Start pulling a model in the background.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            modelName = new { type = "string", description = "Model name to pull." }
                        },
                        required = new[] { "modelName" }
                    },
                    Handler = args =>
                    {
                        string modelName = AssistantHubMcpServerHelpers.GetStringRequired(args, "modelName");
                        string payload = AssistantHub.Core.Helpers.Serializer.SerializeJson(new { Name = modelName }, true)!;
                        return AssistantHubMcpRestProxy.SendJson(context, HttpMethod.Post, "/v1.0/models/pull", payload);
                    }
                },
                new()
                {
                    Name = "model/pull/status",
                    Description = "Get the current model pull status.",
                    InputSchema = McpRegistrationHelper.EmptySchema,
                    Handler = _ => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetPullStatusAsync().GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "model/delete",
                    Description = "Delete a model from the provider.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            modelName = new { type = "string", description = "Model name to delete." }
                        },
                        required = new[] { "modelName" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteModelAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "modelName")).GetAwaiter().GetResult();
                        return true;
                    }
                }
            };
        }
    }
}
