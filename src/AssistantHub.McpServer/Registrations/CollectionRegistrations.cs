namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using System.Text.Json;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for collection operations.
    /// </summary>
    public static class CollectionRegistrations
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
                    Name = "collection/list",
                    Description = "List collections using an optional EnumerationQuery payload.",
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
                            ? context.Sdk.ListCollectionsAsync(query).GetAwaiter().GetResult()
                            : context.Sdk.ListCollectionsAsync().GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "collection/get",
                    Description = "Get a collection by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionId = new { type = "string", description = "Collection identifier." }
                        },
                        required = new[] { "collectionId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetCollectionAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "collectionId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "collection/create",
                    Description = "Create a collection.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionJson = new { type = "string", description = "Collection serialized as JSON string." }
                        },
                        required = new[] { "collectionJson" }
                    },
                    Handler = args =>
                    {
                        Collection collection = AssistantHubMcpServerHelpers.DeserializeRequired<Collection>(args, "collectionJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateCollectionAsync(collection).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "collection/update",
                    Description = "Update a collection.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionId = new { type = "string", description = "Collection identifier." },
                            collectionJson = new { type = "string", description = "Collection serialized as JSON string." }
                        },
                        required = new[] { "collectionId", "collectionJson" }
                    },
                    Handler = args =>
                    {
                        string collectionId = AssistantHubMcpServerHelpers.GetStringRequired(args, "collectionId");
                        Collection collection = AssistantHubMcpServerHelpers.DeserializeRequired<Collection>(args, "collectionJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateCollectionAsync(collectionId, collection).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "collection/delete",
                    Description = "Delete a collection.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionId = new { type = "string", description = "Collection identifier." }
                        },
                        required = new[] { "collectionId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteCollectionAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "collectionId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "collection/exists",
                    Description = "Check whether a collection exists.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionId = new { type = "string", description = "Collection identifier." }
                        },
                        required = new[] { "collectionId" }
                    },
                    Handler = args => context.Sdk.CollectionExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "collectionId")).GetAwaiter().GetResult()
                },
                new()
                {
                    Name = "collection/labels/distinct",
                    Description = "List distinct labels in a collection.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionId = new { type = "string", description = "Collection identifier." }
                        },
                        required = new[] { "collectionId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetCollectionDistinctLabelsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "collectionId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "collection/tags/distinct",
                    Description = "List distinct tags in a collection.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionId = new { type = "string", description = "Collection identifier." }
                        },
                        required = new[] { "collectionId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetCollectionDistinctTagsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "collectionId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "collection/search",
                    Description = "Search records in a RecallDB collection.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionId = new { type = "string", description = "Collection identifier." },
                            requestJson = new { type = "string", description = "RecallDB search request serialized as JSON string." }
                        },
                        required = new[] { "collectionId", "requestJson" }
                    },
                    Handler = args =>
                    {
                        string collectionId = AssistantHubMcpServerHelpers.GetStringRequired(args, "collectionId");
                        JsonElement request = GetJsonBody(args, "requestJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.SearchCollectionAsync(collectionId, request).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                }
            };
        }

        private static JsonElement GetJsonBody(JsonElement? args, string propertyName)
        {
            string json = AssistantHubMcpServerHelpers.GetJsonRequired(args, propertyName);
            using JsonDocument document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
    }
}
