namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for history and thread operations.
    /// </summary>
    public static class HistoryRegistrations
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
                    Name = "history/list",
                    Description = "List chat history using an optional EnumerationQuery payload.",
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
                            ? context.Sdk.ListHistoryAsync(query).GetAwaiter().GetResult()
                            : context.Sdk.ListHistoryAsync().GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "history/get",
                    Description = "Get a chat history entry by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            historyId = new { type = "string", description = "Chat history identifier." }
                        },
                        required = new[] { "historyId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetHistoryAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "historyId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "history/delete",
                    Description = "Delete a chat history entry.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            historyId = new { type = "string", description = "Chat history identifier." }
                        },
                        required = new[] { "historyId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteHistoryAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "historyId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "thread/list",
                    Description = "List thread summaries using an optional EnumerationQuery payload.",
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
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ListThreadSummariesAsync(query).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "thread/get",
                    Description = "Get full thread history for an assistant thread.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." },
                            threadId = new { type = "string", description = "Thread identifier." }
                        },
                        required = new[] { "assistantId", "threadId" }
                    },
                    Handler = args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        string threadId = AssistantHubMcpServerHelpers.GetStringRequired(args, "threadId");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetThreadHistoryAsync(assistantId, threadId).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "thread/create",
                    Description = "Create a new thread for an assistant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, new
                    {
                        ThreadId = context.Sdk.CreateThreadAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId")).GetAwaiter().GetResult()
                    }, includeSecrets: true)
                },
                new()
                {
                    Name = "thread/delete",
                    Description = "Delete a thread.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            threadId = new { type = "string", description = "Thread identifier." }
                        },
                        required = new[] { "threadId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteThreadAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "threadId")).GetAwaiter().GetResult();
                        return true;
                    }
                }
            };
        }
    }
}
