namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using System.Text.Json;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for Verbex inverted-index operations.
    /// </summary>
    public static class IndexRegistrations
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
                    Name = "index/list",
                    Description = "List Verbex inverted indices using an optional EnumerationQuery payload.",
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
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ListIndicesAsync(query).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/get",
                    Description = "Get a Verbex inverted index by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." }
                        },
                        required = new[] { "indexId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetIndexAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "index/create",
                    Description = "Create a Verbex inverted index.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexJson = new { type = "string", description = "Index creation payload serialized as JSON string." }
                        },
                        required = new[] { "indexJson" }
                    },
                    Handler = args =>
                    {
                        JsonElement index = GetJsonBody(args, "indexJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateIndexAsync(index).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/update",
                    Description = "Update a Verbex inverted index.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            indexJson = new { type = "string", description = "Index update payload serialized as JSON string." }
                        },
                        required = new[] { "indexId", "indexJson" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        JsonElement index = GetJsonBody(args, "indexJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateIndexAsync(indexId, index).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/delete",
                    Description = "Delete a Verbex inverted index.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." }
                        },
                        required = new[] { "indexId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteIndexAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "index/exists",
                    Description = "Check whether a Verbex inverted index exists.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." }
                        },
                        required = new[] { "indexId" }
                    },
                    Handler = args => context.Sdk.IndexExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId")).GetAwaiter().GetResult()
                },
                new()
                {
                    Name = "index/labels/update",
                    Description = "Update labels on a Verbex inverted index.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            labelsJson = new { type = "string", description = "Labels payload serialized as JSON string." }
                        },
                        required = new[] { "indexId", "labelsJson" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        JsonElement labels = GetJsonBody(args, "labelsJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateIndexLabelsAsync(indexId, labels).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/tags/update",
                    Description = "Update tags on a Verbex inverted index.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            tagsJson = new { type = "string", description = "Tags payload serialized as JSON string." }
                        },
                        required = new[] { "indexId", "tagsJson" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        JsonElement tags = GetJsonBody(args, "tagsJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateIndexTagsAsync(indexId, tags).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/custom-metadata/update",
                    Description = "Update custom metadata on a Verbex inverted index.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            customMetadataJson = new { type = "string", description = "Custom metadata payload serialized as JSON string." }
                        },
                        required = new[] { "indexId", "customMetadataJson" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        JsonElement customMetadata = GetJsonBody(args, "customMetadataJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateIndexCustomMetadataAsync(indexId, customMetadata).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/terms/top",
                    Description = "Get top terms from a Verbex inverted index.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            maxResults = new { type = "integer", description = "Optional maximum number of terms." }
                        },
                        required = new[] { "indexId" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        int? maxResults = AssistantHubMcpServerHelpers.GetIntOptional(args, "maxResults");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetIndexTopTermsAsync(indexId, maxResults).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "index/search",
                    Description = "Search a Verbex inverted index.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            indexId = new { type = "string", description = "Index identifier." },
                            requestJson = new { type = "string", description = "Verbex search request serialized as JSON string." }
                        },
                        required = new[] { "indexId", "requestJson" }
                    },
                    Handler = args =>
                    {
                        string indexId = AssistantHubMcpServerHelpers.GetStringRequired(args, "indexId");
                        JsonElement request = GetJsonBody(args, "requestJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.SearchIndexAsync(indexId, request).GetAwaiter().GetResult(), includeSecrets: true);
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
