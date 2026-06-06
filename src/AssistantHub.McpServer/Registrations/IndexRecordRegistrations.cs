namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using System.Text.Json;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for Verbex inverted-index record operations.
    /// </summary>
    public static class IndexRecordRegistrations
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
                    Name = "index/record/list",
                    Description = "List records in a Verbex inverted index using an optional EnumerationQuery payload.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            queryJson = new { type = "string", description = "Optional EnumerationQuery serialized as JSON string." }
                        },
                        required = new[] { "indexId" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        EnumerationQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<EnumerationQuery>(args, "queryJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ListIndexRecordsAsync(indexId, query).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/record/get",
                    Description = "Get a single Verbex inverted-index record.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            recordId = new { type = "string", description = "Record identifier." }
                        },
                        required = new[] { "indexId", "recordId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetIndexRecordAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId"), AssistantHubMcpServerHelpers.GetStringRequired(args, "recordId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "index/record/create",
                    Description = "Create a record in a Verbex inverted index.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            recordJson = new { type = "string", description = "Record payload serialized as JSON string." }
                        },
                        required = new[] { "indexId", "recordJson" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        JsonElement record = GetJsonBody(args, "recordJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateIndexRecordAsync(indexId, record).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/record/create-batch",
                    Description = "Create records in a Verbex inverted index in batch.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            recordsJson = new { type = "string", description = "Batch record payload serialized as JSON string." }
                        },
                        required = new[] { "indexId", "recordsJson" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        JsonElement records = GetJsonBody(args, "recordsJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateIndexRecordsBatchAsync(indexId, records).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/record/exists",
                    Description = "Check whether a Verbex inverted-index record exists.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            recordId = new { type = "string", description = "Record identifier." }
                        },
                        required = new[] { "indexId", "recordId" }
                    },
                    Handler = args => context.Sdk.IndexRecordExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId"), AssistantHubMcpServerHelpers.GetStringRequired(args, "recordId")).GetAwaiter().GetResult()
                },
                new()
                {
                    Name = "index/record/exists-batch",
                    Description = "Check whether multiple Verbex inverted-index records exist.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            recordIdsJson = new { type = "string", description = "Batch existence payload serialized as JSON string." }
                        },
                        required = new[] { "indexId", "recordIdsJson" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        JsonElement recordIds = GetJsonBody(args, "recordIdsJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CheckIndexRecordsExistAsync(indexId, recordIds).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/record/delete",
                    Description = "Delete a single Verbex inverted-index record.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            recordId = new { type = "string", description = "Record identifier." }
                        },
                        required = new[] { "indexId", "recordId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteIndexRecordAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId"), AssistantHubMcpServerHelpers.GetStringRequired(args, "recordId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "index/record/batch-delete",
                    Description = "Delete multiple Verbex inverted-index records.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            recordIdsJson = new { type = "string", description = "Array of record identifiers serialized as JSON string." }
                        },
                        required = new[] { "indexId", "recordIdsJson" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        List<string> recordIds = AssistantHubMcpServerHelpers.DeserializeRequired<List<string>>(args, "recordIdsJson");
                        context.Sdk.DeleteIndexRecordsAsync(indexId, recordIds).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "index/record/labels/update",
                    Description = "Update labels on a Verbex inverted-index record.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            recordId = new { type = "string", description = "Record identifier." },
                            labelsJson = new { type = "string", description = "Labels payload serialized as JSON string." }
                        },
                        required = new[] { "indexId", "recordId", "labelsJson" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        string recordId = AssistantHubMcpServerHelpers.GetStringRequired(args, "recordId");
                        JsonElement labels = GetJsonBody(args, "labelsJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateIndexRecordLabelsAsync(indexId, recordId, labels).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/record/tags/update",
                    Description = "Update tags on a Verbex inverted-index record.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            recordId = new { type = "string", description = "Record identifier." },
                            tagsJson = new { type = "string", description = "Tags payload serialized as JSON string." }
                        },
                        required = new[] { "indexId", "recordId", "tagsJson" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        string recordId = AssistantHubMcpServerHelpers.GetStringRequired(args, "recordId");
                        JsonElement tags = GetJsonBody(args, "tagsJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateIndexRecordTagsAsync(indexId, recordId, tags).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/record/custom-metadata/update",
                    Description = "Update custom metadata on a Verbex inverted-index record.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            recordId = new { type = "string", description = "Record identifier." },
                            customMetadataJson = new { type = "string", description = "Custom metadata payload serialized as JSON string." }
                        },
                        required = new[] { "indexId", "recordId", "customMetadataJson" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        string recordId = AssistantHubMcpServerHelpers.GetStringRequired(args, "recordId");
                        JsonElement customMetadata = GetJsonBody(args, "customMetadataJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateIndexRecordCustomMetadataAsync(indexId, recordId, customMetadata).GetAwaiter().GetResult(), includeSecrets: true);
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
