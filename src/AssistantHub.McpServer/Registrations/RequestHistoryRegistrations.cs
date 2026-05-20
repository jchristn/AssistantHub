namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for request history operations.
    /// </summary>
    public static class RequestHistoryRegistrations
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
                    Name = "requesthistory/list",
                    Description = "List request history using a RequestHistorySearchFilter serialized as query parameters.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            filterJson = new { type = "string", description = "Optional RequestHistorySearchFilter serialized as JSON string." }
                        },
                        required = System.Array.Empty<string>()
                    },
                    Handler = args =>
                    {
                        RequestHistorySearchFilter? filter = AssistantHubMcpServerHelpers.DeserializeOptional<RequestHistorySearchFilter>(args, "filterJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ListRequestHistoryAsync(filter).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "requesthistory/summary",
                    Description = "Summarize request history using a RequestHistorySearchFilter serialized as query parameters.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            filterJson = new { type = "string", description = "Optional RequestHistorySearchFilter serialized as JSON string." }
                        },
                        required = System.Array.Empty<string>()
                    },
                    Handler = args =>
                    {
                        RequestHistorySearchFilter? filter = AssistantHubMcpServerHelpers.DeserializeOptional<RequestHistorySearchFilter>(args, "filterJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetRequestHistorySummaryAsync(filter).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "requesthistory/get",
                    Description = "Get a request history entry.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            requestId = new { type = "string", description = "Request history identifier." }
                        },
                        required = new[] { "requestId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetRequestHistoryAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "requestId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "requesthistory/detail",
                    Description = "Get a request history entry detail payload.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            requestId = new { type = "string", description = "Request history identifier." }
                        },
                        required = new[] { "requestId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetRequestHistoryDetailAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "requestId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "requesthistory/delete",
                    Description = "Delete a single request history entry.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            requestId = new { type = "string", description = "Request history identifier." }
                        },
                        required = new[] { "requestId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteRequestHistoryAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "requestId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "requesthistory/bulk-delete",
                    Description = "Delete request history entries matching a RequestHistorySearchFilter.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            filterJson = new { type = "string", description = "Optional RequestHistorySearchFilter serialized as JSON string." }
                        },
                        required = System.Array.Empty<string>()
                    },
                    Handler = args =>
                    {
                        RequestHistorySearchFilter? filter = AssistantHubMcpServerHelpers.DeserializeOptional<RequestHistorySearchFilter>(args, "filterJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.DeleteRequestHistoryBulkAsync(filter).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                }
            };
        }
    }
}
