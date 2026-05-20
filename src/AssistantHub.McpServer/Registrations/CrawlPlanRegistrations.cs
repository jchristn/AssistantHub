namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using System.Net.Http;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for crawl plan operations.
    /// </summary>
    public static class CrawlPlanRegistrations
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
                    Name = "crawlplan/list",
                    Description = "List crawl plans using an optional EnumerationQuery payload.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            queryJson = new { type = "string", description = "Optional EnumerationQuery serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include authentication secrets in repository settings." }
                        },
                        required = System.Array.Empty<string>()
                    },
                    Handler = args =>
                    {
                        EnumerationQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<EnumerationQuery>(args, "queryJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        object result = query != null
                            ? context.Sdk.ListCrawlPlansAsync(query).GetAwaiter().GetResult()
                            : context.Sdk.ListCrawlPlansAsync().GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets);
                    }
                },
                new()
                {
                    Name = "crawlplan/get",
                    Description = "Get a crawl plan by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." },
                            includeSecrets = new { type = "boolean", description = "If true, include authentication secrets in repository settings." }
                        },
                        required = new[] { "planId" }
                    },
                    Handler = args =>
                    {
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetCrawlPlanAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "planId")).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "crawlplan/create",
                    Description = "Create a crawl plan.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planJson = new { type = "string", description = "CrawlPlan serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include authentication secrets in repository settings." }
                        },
                        required = new[] { "planJson" }
                    },
                    Handler = args =>
                    {
                        CrawlPlan plan = AssistantHubMcpServerHelpers.DeserializeRequired<CrawlPlan>(args, "planJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateCrawlPlanAsync(plan).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "crawlplan/update",
                    Description = "Update a crawl plan.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." },
                            planJson = new { type = "string", description = "CrawlPlan serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include authentication secrets in repository settings." }
                        },
                        required = new[] { "planId", "planJson" }
                    },
                    Handler = args =>
                    {
                        string planId = AssistantHubMcpServerHelpers.GetStringRequired(args, "planId");
                        CrawlPlan plan = AssistantHubMcpServerHelpers.DeserializeRequired<CrawlPlan>(args, "planJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateCrawlPlanAsync(planId, plan).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "crawlplan/delete",
                    Description = "Delete a crawl plan.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." }
                        },
                        required = new[] { "planId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteCrawlPlanAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "planId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "crawlplan/exists",
                    Description = "Check whether a crawl plan exists.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." }
                        },
                        required = new[] { "planId" }
                    },
                    Handler = args => context.Sdk.CrawlPlanExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "planId")).GetAwaiter().GetResult()
                },
                new()
                {
                    Name = "crawlplan/start",
                    Description = "Start a crawl plan.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." }
                        },
                        required = new[] { "planId" }
                    },
                    Handler = args => AssistantHubMcpRestProxy.SendJson(context, HttpMethod.Post, "/v1.0/crawlplans/" + AssistantHubMcpRestProxy.Escape(AssistantHubMcpServerHelpers.GetStringRequired(args, "planId")) + "/start")
                },
                new()
                {
                    Name = "crawlplan/stop",
                    Description = "Stop a crawl plan.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." }
                        },
                        required = new[] { "planId" }
                    },
                    Handler = args => AssistantHubMcpRestProxy.SendJson(context, HttpMethod.Post, "/v1.0/crawlplans/" + AssistantHubMcpRestProxy.Escape(AssistantHubMcpServerHelpers.GetStringRequired(args, "planId")) + "/stop")
                },
                new()
                {
                    Name = "crawlplan/connectivity",
                    Description = "Test crawl plan connectivity.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." }
                        },
                        required = new[] { "planId" }
                    },
                    Handler = args => AssistantHubMcpRestProxy.SendJson(context, HttpMethod.Post, "/v1.0/crawlplans/" + AssistantHubMcpRestProxy.Escape(AssistantHubMcpServerHelpers.GetStringRequired(args, "planId")) + "/connectivity")
                },
                new()
                {
                    Name = "crawlplan/enumerate",
                    Description = "Enumerate crawl plan contents.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            planId = new { type = "string", description = "Crawl plan identifier." }
                        },
                        required = new[] { "planId" }
                    },
                    Handler = args => AssistantHubMcpRestProxy.SendJson(context, HttpMethod.Get, "/v1.0/crawlplans/" + AssistantHubMcpRestProxy.Escape(AssistantHubMcpServerHelpers.GetStringRequired(args, "planId")) + "/enumerate")
                }
            };
        }
    }
}
