namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for embedding endpoint operations.
    /// </summary>
    public static class EmbeddingEndpointRegistrations
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
                    Name = "embeddingendpoint/list",
                    Description = "List embedding endpoints using an optional EnumerationQuery payload.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            queryJson = new { type = "string", description = "Optional EnumerationQuery serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include API keys in the response." }
                        },
                        required = System.Array.Empty<string>()
                    },
                    Handler = args =>
                    {
                        EnumerationQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<EnumerationQuery>(args, "queryJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ListEmbeddingEndpointsAsync(query).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "embeddingendpoint/get",
                    Description = "Get an embedding endpoint by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointId = new { type = "string", description = "Embedding endpoint identifier." },
                            includeSecrets = new { type = "boolean", description = "If true, include API keys in the response." }
                        },
                        required = new[] { "endpointId" }
                    },
                    Handler = args =>
                    {
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetEmbeddingEndpointAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "endpointId")).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "embeddingendpoint/create",
                    Description = "Create an embedding endpoint.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointJson = new { type = "string", description = "EmbeddingEndpoint serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include API keys in the response." }
                        },
                        required = new[] { "endpointJson" }
                    },
                    Handler = args =>
                    {
                        EmbeddingEndpoint endpoint = AssistantHubMcpServerHelpers.DeserializeRequired<EmbeddingEndpoint>(args, "endpointJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateEmbeddingEndpointAsync(endpoint).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "embeddingendpoint/update",
                    Description = "Update an embedding endpoint.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointId = new { type = "string", description = "Embedding endpoint identifier." },
                            endpointJson = new { type = "string", description = "EmbeddingEndpoint serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include API keys in the response." }
                        },
                        required = new[] { "endpointId", "endpointJson" }
                    },
                    Handler = args =>
                    {
                        string endpointId = AssistantHubMcpServerHelpers.GetStringRequired(args, "endpointId");
                        EmbeddingEndpoint endpoint = AssistantHubMcpServerHelpers.DeserializeRequired<EmbeddingEndpoint>(args, "endpointJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateEmbeddingEndpointAsync(endpointId, endpoint).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "embeddingendpoint/delete",
                    Description = "Delete an embedding endpoint.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointId = new { type = "string", description = "Embedding endpoint identifier." }
                        },
                        required = new[] { "endpointId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteEmbeddingEndpointAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "endpointId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "embeddingendpoint/exists",
                    Description = "Check whether an embedding endpoint exists.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointId = new { type = "string", description = "Embedding endpoint identifier." }
                        },
                        required = new[] { "endpointId" }
                    },
                    Handler = args => context.Sdk.EmbeddingEndpointExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "endpointId")).GetAwaiter().GetResult()
                },
                new()
                {
                    Name = "embeddingendpoint/health",
                    Description = "Get health status for all embedding endpoints or a specific endpoint.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointId = new { type = "string", description = "Optional embedding endpoint identifier." }
                        },
                        required = System.Array.Empty<string>()
                    },
                    Handler = args =>
                    {
                        string? endpointId = AssistantHubMcpServerHelpers.GetStringOptional(args, "endpointId");
                        object result = string.IsNullOrWhiteSpace(endpointId)
                            ? context.Sdk.CheckEmbeddingHealthAsync().GetAwaiter().GetResult()
                            : context.Sdk.CheckEmbeddingEndpointHealthAsync(endpointId).GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "embeddingendpoint/test",
                    Description = "Test an embedding endpoint.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointId = new { type = "string", description = "Embedding endpoint identifier." },
                            requestJson = new { type = "string", description = "EndpointExplorerEmbeddingRequest serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include API keys in nested payloads." }
                        },
                        required = new[] { "endpointId", "requestJson" }
                    },
                    Handler = args =>
                    {
                        string endpointId = AssistantHubMcpServerHelpers.GetStringRequired(args, "endpointId");
                        EndpointExplorerEmbeddingRequest request = AssistantHubMcpServerHelpers.DeserializeRequired<EndpointExplorerEmbeddingRequest>(args, "requestJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.TestEmbeddingEndpointAsync(endpointId, request).GetAwaiter().GetResult(), includeSecrets);
                    }
                }
            };
        }
    }
}
