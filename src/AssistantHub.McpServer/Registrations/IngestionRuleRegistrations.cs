namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for ingestion rule operations.
    /// </summary>
    public static class IngestionRuleRegistrations
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
                    Name = "ingestionrule/list",
                    Description = "List ingestion rules using an optional EnumerationQuery payload.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            queryJson = new { type = "string", description = "Optional EnumerationQuery serialized as JSON string." }
                        },
                        required = System.Array.Empty<string>()
                    },
                    Handler = args =>
                    {
                        EnumerationQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<EnumerationQuery>(args, "queryJson");
                        object result = query != null
                            ? context.Sdk.ListIngestionRulesAsync(query).GetAwaiter().GetResult()
                            : context.Sdk.ListIngestionRulesAsync().GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "ingestionrule/get",
                    Description = "Get an ingestion rule by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            ruleId = new { type = "string", description = "Ingestion rule identifier." }
                        },
                        required = new[] { "ruleId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetIngestionRuleAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "ruleId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "ingestionrule/create",
                    Description = "Create an ingestion rule.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            ruleJson = new { type = "string", description = "IngestionRule serialized as JSON string." }
                        },
                        required = new[] { "ruleJson" }
                    },
                    Handler = args =>
                    {
                        IngestionRule rule = AssistantHubMcpServerHelpers.DeserializeRequired<IngestionRule>(args, "ruleJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateIngestionRuleAsync(rule).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "ingestionrule/update",
                    Description = "Update an ingestion rule.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            ruleId = new { type = "string", description = "Ingestion rule identifier." },
                            ruleJson = new { type = "string", description = "IngestionRule serialized as JSON string." }
                        },
                        required = new[] { "ruleId", "ruleJson" }
                    },
                    Handler = args =>
                    {
                        string ruleId = AssistantHubMcpServerHelpers.GetStringRequired(args, "ruleId");
                        IngestionRule rule = AssistantHubMcpServerHelpers.DeserializeRequired<IngestionRule>(args, "ruleJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateIngestionRuleAsync(ruleId, rule).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "ingestionrule/delete",
                    Description = "Delete an ingestion rule.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            ruleId = new { type = "string", description = "Ingestion rule identifier." }
                        },
                        required = new[] { "ruleId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteIngestionRuleAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "ruleId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "ingestionrule/exists",
                    Description = "Check whether an ingestion rule exists.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            ruleId = new { type = "string", description = "Ingestion rule identifier." }
                        },
                        required = new[] { "ruleId" }
                    },
                    Handler = args => context.Sdk.IngestionRuleExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "ruleId")).GetAwaiter().GetResult()
                }
            };
        }
    }
}
