namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for completion endpoint operations.
    /// </summary>
    public static class CompletionEndpointRegistrations
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
                    Name = "completionendpoint/list",
                    Description = "List completion endpoints using an optional EnumerationQuery payload.",
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
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ListCompletionEndpointsAsync(query).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "completionendpoint/get",
                    Description = "Get a completion endpoint by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointId = new { type = "string", description = "Completion endpoint identifier." },
                            includeSecrets = new { type = "boolean", description = "If true, include API keys in the response." }
                        },
                        required = new[] { "endpointId" }
                    },
                    Handler = args =>
                    {
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetCompletionEndpointAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "endpointId")).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "completionendpoint/create",
                    Description = "Create a completion endpoint.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointJson = new { type = "string", description = "CompletionEndpoint serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include API keys in the response." }
                        },
                        required = new[] { "endpointJson" }
                    },
                    Handler = args =>
                    {
                        CompletionEndpoint endpoint = AssistantHubMcpServerHelpers.DeserializeRequired<CompletionEndpoint>(args, "endpointJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateCompletionEndpointAsync(endpoint).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "completionendpoint/update",
                    Description = "Update a completion endpoint.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointId = new { type = "string", description = "Completion endpoint identifier." },
                            endpointJson = new { type = "string", description = "CompletionEndpoint serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include API keys in the response." }
                        },
                        required = new[] { "endpointId", "endpointJson" }
                    },
                    Handler = args =>
                    {
                        string endpointId = AssistantHubMcpServerHelpers.GetStringRequired(args, "endpointId");
                        CompletionEndpoint endpoint = AssistantHubMcpServerHelpers.DeserializeRequired<CompletionEndpoint>(args, "endpointJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateCompletionEndpointAsync(endpointId, endpoint).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "completionendpoint/delete",
                    Description = "Delete a completion endpoint.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointId = new { type = "string", description = "Completion endpoint identifier." }
                        },
                        required = new[] { "endpointId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteCompletionEndpointAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "endpointId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "completionendpoint/exists",
                    Description = "Check whether a completion endpoint exists.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointId = new { type = "string", description = "Completion endpoint identifier." }
                        },
                        required = new[] { "endpointId" }
                    },
                    Handler = args => context.Sdk.CompletionEndpointExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "endpointId")).GetAwaiter().GetResult()
                },
                new()
                {
                    Name = "completionendpoint/health",
                    Description = "Get health status for all completion endpoints or a specific endpoint.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointId = new { type = "string", description = "Optional completion endpoint identifier." }
                        },
                        required = System.Array.Empty<string>()
                    },
                    Handler = args =>
                    {
                        string? endpointId = AssistantHubMcpServerHelpers.GetStringOptional(args, "endpointId");
                        object result = string.IsNullOrWhiteSpace(endpointId)
                            ? context.Sdk.CheckCompletionHealthAsync().GetAwaiter().GetResult()
                            : context.Sdk.CheckCompletionEndpointHealthAsync(endpointId).GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "completionendpoint/test",
                    Description = "Test a completion endpoint.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            endpointId = new { type = "string", description = "Completion endpoint identifier." },
                            requestJson = new { type = "string", description = "EndpointExplorerCompletionRequest serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include API keys in nested payloads." }
                        },
                        required = new[] { "endpointId", "requestJson" }
                    },
                    Handler = args =>
                    {
                        string endpointId = AssistantHubMcpServerHelpers.GetStringRequired(args, "endpointId");
                        EndpointExplorerCompletionRequest request = AssistantHubMcpServerHelpers.DeserializeRequired<EndpointExplorerCompletionRequest>(args, "requestJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.TestCompletionEndpointAsync(endpointId, request).GetAwaiter().GetResult(), includeSecrets);
                    }
                }
            };
        }
    }
}
