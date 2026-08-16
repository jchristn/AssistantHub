namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for collection record operations.
    /// </summary>
    public static class CollectionRecordRegistrations
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
                    Name = "collection/record/list",
                    Description = "List records in a collection using an optional EnumerationQuery payload.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionId = new { type = "string", description = "Collection identifier." },
                            queryJson = new { type = "string", description = "Optional EnumerationQuery serialized as JSON string." }
                        },
                        required = new[] { "collectionId" }
                    },
                    Handler = args =>
                    {
                        string collectionId = AssistantHubMcpServerHelpers.GetStringRequired(args, "collectionId");
                        EnumerationQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<EnumerationQuery>(args, "queryJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ListCollectionRecordsAsync(collectionId, query).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "collection/record/get",
                    Description = "Get a single collection record.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionId = new { type = "string", description = "Collection identifier." },
                            recordId = new { type = "string", description = "Record identifier." }
                        },
                        required = new[] { "collectionId", "recordId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetCollectionRecordAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "collectionId"), AssistantHubMcpServerHelpers.GetStringRequired(args, "recordId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "collection/record/create",
                    Description = "Create a record in a collection.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionId = new { type = "string", description = "Collection identifier." },
                            recordJson = new { type = "string", description = "CollectionRecord serialized as JSON string." }
                        },
                        required = new[] { "collectionId", "recordJson" }
                    },
                    Handler = args =>
                    {
                        string collectionId = AssistantHubMcpServerHelpers.GetStringRequired(args, "collectionId");
                        CollectionRecord record = AssistantHubMcpServerHelpers.DeserializeRequired<CollectionRecord>(args, "recordJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateCollectionRecordAsync(collectionId, record).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "collection/record/delete",
                    Description = "Delete a single collection record.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionId = new { type = "string", description = "Collection identifier." },
                            recordId = new { type = "string", description = "Record identifier." }
                        },
                        required = new[] { "collectionId", "recordId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteCollectionRecordAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "collectionId"), AssistantHubMcpServerHelpers.GetStringRequired(args, "recordId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "collection/record/batch-delete",
                    Description = "Delete multiple collection records.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            collectionId = new { type = "string", description = "Collection identifier." },
                            recordIdsJson = new { type = "string", description = "Array of record identifiers serialized as JSON string." }
                        },
                        required = new[] { "collectionId", "recordIdsJson" }
                    },
                    Handler = args =>
                    {
                        string collectionId = AssistantHubMcpServerHelpers.GetStringRequired(args, "collectionId");
                        List<string> recordIds = AssistantHubMcpServerHelpers.DeserializeRequired<List<string>>(args, "recordIdsJson");
                        context.Sdk.BatchDeleteCollectionRecordsAsync(collectionId, recordIds).GetAwaiter().GetResult();
                        return true;
                    }
                }
            };
        }
    }
}
