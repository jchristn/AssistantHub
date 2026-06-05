namespace AssistantHub.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for assistant analytics operations.
    /// </summary>
    public static class AssistantAnalyticsRegistrations
    {
        public static void RegisterHttpTools(McpHttpServer server, AssistantHubMcpContext context) => McpRegistrationHelper.RegisterHttpTools(server, GetDefinitions(context));
        public static void RegisterTcpMethods(McpTcpServer server, AssistantHubMcpContext context) => McpRegistrationHelper.RegisterTcpMethods(server, GetDefinitions(context));
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, AssistantHubMcpContext context) => McpRegistrationHelper.RegisterWebSocketMethods(server, GetDefinitions(context));

        private static List<McpMethodDefinition> GetDefinitions(AssistantHubMcpContext context)
        {
            return new List<McpMethodDefinition>
            {
                CreateDefinition(
                    "assistantanalytics/overview",
                    "Get assistant analytics overview for the selected range.",
                    args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        AssistantAnalyticsQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<AssistantAnalyticsQuery>(args, "queryJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetAssistantAnalyticsOverviewAsync(assistantId, query).GetAwaiter().GetResult(), includeSecrets: true);
                    }),
                CreateDefinition(
                    "assistantanalytics/timeseries",
                    "Get chart-ready assistant analytics time series for the selected range.",
                    args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        AssistantAnalyticsQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<AssistantAnalyticsQuery>(args, "queryJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetAssistantAnalyticsTimeSeriesAsync(assistantId, query).GetAwaiter().GetResult(), includeSecrets: true);
                    }),
                CreateDefinition(
                    "assistantanalytics/stages",
                    "Get assistant analytics stage summaries for the selected range.",
                    args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        AssistantAnalyticsQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<AssistantAnalyticsQuery>(args, "queryJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetAssistantAnalyticsStagesAsync(assistantId, query).GetAwaiter().GetResult(), includeSecrets: true);
                    }),
                CreateDefinition(
                    "assistantanalytics/endpoints",
                    "Get assistant analytics endpoint/model/provider summaries for the selected range.",
                    args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        AssistantAnalyticsQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<AssistantAnalyticsQuery>(args, "queryJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetAssistantAnalyticsEndpointsAsync(assistantId, query).GetAwaiter().GetResult(), includeSecrets: true);
                    }),
                CreateDefinition(
                    "assistantanalytics/slowest",
                    "Get slowest assistant requests for the selected range.",
                    args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        AssistantAnalyticsQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<AssistantAnalyticsQuery>(args, "queryJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetAssistantAnalyticsSlowestAsync(assistantId, query).GetAwaiter().GetResult(), includeSecrets: true);
                    }),
                CreateDefinition(
                    "assistantanalytics/feedback",
                    "Get assistant feedback analytics for the selected range.",
                    args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        AssistantAnalyticsQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<AssistantAnalyticsQuery>(args, "queryJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetAssistantAnalyticsFeedbackAsync(assistantId, query).GetAwaiter().GetResult(), includeSecrets: true);
                    })
            };
        }

        private static McpMethodDefinition CreateDefinition(string name, string description, Func<System.Text.Json.JsonElement?, object> handler)
        {
            return new McpMethodDefinition
            {
                Name = name,
                Description = description,
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        assistantId = new { type = "string", description = "Assistant identifier." },
                        queryJson = new { type = "string", description = "Optional AssistantAnalyticsQuery serialized as JSON string." }
                    },
                    required = new[] { "assistantId" }
                },
                Handler = handler
            };
        }
    }
}
