namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for assistant operations.
    /// </summary>
    public static class AssistantRegistrations
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
                    Name = "assistant/list",
                    Description = "List assistants using an optional EnumerationQuery payload.",
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
                            ? context.Sdk.ListAssistantsAsync(query).GetAwaiter().GetResult()
                            : context.Sdk.ListAssistantsAsync().GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "assistant/get",
                    Description = "Get an assistant by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetAssistantAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "assistant/create",
                    Description = "Create an assistant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantJson = new { type = "string", description = "Assistant serialized as JSON string." }
                        },
                        required = new[] { "assistantJson" }
                    },
                    Handler = args =>
                    {
                        Assistant assistant = AssistantHubMcpServerHelpers.DeserializeRequired<Assistant>(args, "assistantJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateAssistantAsync(assistant).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "assistant/update",
                    Description = "Update an assistant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." },
                            assistantJson = new { type = "string", description = "Assistant serialized as JSON string." }
                        },
                        required = new[] { "assistantId", "assistantJson" }
                    },
                    Handler = args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        Assistant assistant = AssistantHubMcpServerHelpers.DeserializeRequired<Assistant>(args, "assistantJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateAssistantAsync(assistantId, assistant).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "assistant/delete",
                    Description = "Delete an assistant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteAssistantAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "assistant/exists",
                    Description = "Check whether an assistant exists.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args => context.Sdk.AssistantExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId")).GetAwaiter().GetResult()
                },
                new()
                {
                    Name = "assistant/public/get",
                    Description = "Get the public information for an assistant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetAssistantPublicAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "assistant/documents/list",
                    Description = "List safe public document metadata selectable in assistant chat.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." },
                            queryJson = new { type = "string", description = "Optional EnumerationQuery serialized as JSON string." },
                            query = new { type = "string", description = "Optional text filter for document name, original filename, content type, or permitted source URL." },
                            contentType = new { type = "string", description = "Optional MIME type filter, such as application/pdf or text/*." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        EnumerationQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<EnumerationQuery>(args, "queryJson");
                        string? searchQuery = AssistantHubMcpServerHelpers.GetStringOptional(args, "query");
                        string? contentType = AssistantHubMcpServerHelpers.GetStringOptional(args, "contentType");

                        return AssistantHubMcpServerHelpers.Serialize(
                            context,
                            context.Sdk.ListAssistantDocumentsAsync(assistantId, query, searchQuery, contentType).GetAwaiter().GetResult());
                    }
                },
                new()
                {
                    Name = "assistant/labels/distinct",
                    Description = "List distinct labels for an assistant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetAssistantDistinctLabelsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "assistant/tags/distinct",
                    Description = "List distinct tags for an assistant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetAssistantDistinctTagsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId")).GetAwaiter().GetResult(), includeSecrets: true)
                }
            };
        }
    }
}
