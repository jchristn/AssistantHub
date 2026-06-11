namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for assistant tool-call trace operations.
    /// </summary>
    public static class AssistantToolCallRegistrations
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
                    Name = "assistant/tool-calls/list",
                    Description = "List redacted model-directed tool-call traces for an assistant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." },
                            queryJson = new { type = "string", description = "Optional EnumerationQuery serialized as JSON string. Supports trace, tool name, success, denied, chat-history, request-history, and time filters." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        EnumerationQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<EnumerationQuery>(args, "queryJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ListAssistantToolCallsAsync(assistantId, query).GetAwaiter().GetResult());
                    }
                },
                new()
                {
                    Name = "assistant/tool-calls/get",
                    Description = "Get one redacted model-directed tool-call trace for an assistant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." },
                            toolCallRecordId = new { type = "string", description = "Assistant tool-call trace record identifier." }
                        },
                        required = new[] { "assistantId", "toolCallRecordId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(
                        context,
                        context.Sdk.GetAssistantToolCallAsync(
                            AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId"),
                            AssistantHubMcpServerHelpers.GetStringRequired(args, "toolCallRecordId")).GetAwaiter().GetResult())
                },
                new()
                {
                    Name = "assistant/tool-calls/delete",
                    Description = "Delete one assistant tool-call trace record.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." },
                            toolCallRecordId = new { type = "string", description = "Assistant tool-call trace record identifier." }
                        },
                        required = new[] { "assistantId", "toolCallRecordId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteAssistantToolCallAsync(
                            AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId"),
                            AssistantHubMcpServerHelpers.GetStringRequired(args, "toolCallRecordId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "assistant/tool-calls/delete-bulk",
                    Description = "Delete assistant tool-call trace records matching the supplied filters.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." },
                            queryJson = new { type = "string", description = "Optional EnumerationQuery serialized as JSON string. Filters constrain which trace records are deleted." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        EnumerationQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<EnumerationQuery>(args, "queryJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.DeleteAssistantToolCallsAsync(assistantId, query).GetAwaiter().GetResult());
                    }
                }
            };
        }
    }
}
