namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for feedback operations.
    /// </summary>
    public static class FeedbackRegistrations
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
                    Name = "feedback/list",
                    Description = "List feedback records using an optional EnumerationQuery payload.",
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
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ListFeedbackAsync(query).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "feedback/get",
                    Description = "Get a feedback record by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            feedbackId = new { type = "string", description = "Feedback identifier." }
                        },
                        required = new[] { "feedbackId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetFeedbackAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "feedbackId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "feedback/delete",
                    Description = "Delete a feedback record.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            feedbackId = new { type = "string", description = "Feedback identifier." }
                        },
                        required = new[] { "feedbackId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteFeedbackAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "feedbackId")).GetAwaiter().GetResult();
                        return true;
                    }
                }
            };
        }
    }
}
