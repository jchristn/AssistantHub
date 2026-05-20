namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for crawl operation operations.
    /// </summary>
    public static class CrawlOperationRegistrations
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
                    Name = "crawloperation/list",
                    Description = "List crawl operations for a plan using an optional EnumerationQuery payload.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." },
                            queryJson = new { type = "string", description = "Optional EnumerationQuery serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include authentication secrets in repository settings." }
                        },
                        required = new[] { "planId" }
                    },
                    Handler = args =>
                    {
                        string planId = AssistantHubMcpServerHelpers.GetStringRequired(args, "planId");
                        EnumerationQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<EnumerationQuery>(args, "queryJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        object result = query != null
                            ? context.Sdk.ListCrawlOperationsAsync(planId, query).GetAwaiter().GetResult()
                            : context.Sdk.ListCrawlOperationsAsync(planId).GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets);
                    }
                },
                new()
                {
                    Name = "crawloperation/get",
                    Description = "Get a crawl operation by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." },
                            operationId = new { type = "string", description = "Crawl operation identifier." },
                            includeSecrets = new { type = "boolean", description = "If true, include authentication secrets in repository settings." }
                        },
                        required = new[] { "planId", "operationId" }
                    },
                    Handler = args =>
                    {
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(
                            context,
                            context.Sdk.GetCrawlOperationAsync(
                                AssistantHubMcpServerHelpers.GetStringRequired(args, "planId"),
                                AssistantHubMcpServerHelpers.GetStringRequired(args, "operationId")).GetAwaiter().GetResult(),
                            includeSecrets);
                    }
                },
                new()
                {
                    Name = "crawloperation/delete",
                    Description = "Delete a crawl operation.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." },
                            operationId = new { type = "string", description = "Crawl operation identifier." }
                        },
                        required = new[] { "planId", "operationId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteCrawlOperationAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "planId"), AssistantHubMcpServerHelpers.GetStringRequired(args, "operationId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "crawloperation/statistics",
                    Description = "Get aggregate crawl operation statistics for a plan or a specific operation.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." },
                            operationId = new { type = "string", description = "Optional crawl operation identifier." }
                        },
                        required = new[] { "planId" }
                    },
                    Handler = args =>
                    {
                        string planId = AssistantHubMcpServerHelpers.GetStringRequired(args, "planId");
                        string? operationId = AssistantHubMcpServerHelpers.GetStringOptional(args, "operationId");
                        object result = string.IsNullOrWhiteSpace(operationId)
                            ? context.Sdk.GetCrawlStatisticsAsync(planId).GetAwaiter().GetResult()
                            : context.Sdk.GetCrawlOperationStatisticsAsync(planId, operationId).GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "crawloperation/enumeration",
                    Description = "Get the saved enumeration payload for a crawl operation.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." },
                            operationId = new { type = "string", description = "Crawl operation identifier." }
                        },
                        required = new[] { "planId", "operationId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(
                        context,
                        context.Sdk.GetCrawlOperationEnumerationAsync(
                            AssistantHubMcpServerHelpers.GetStringRequired(args, "planId"),
                            AssistantHubMcpServerHelpers.GetStringRequired(args, "operationId")).GetAwaiter().GetResult(),
                        includeSecrets: true)
                }
            };
        }
    }
}
