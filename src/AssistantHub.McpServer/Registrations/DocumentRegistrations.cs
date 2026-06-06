namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for document operations.
    /// </summary>
    public static class DocumentRegistrations
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
                    Name = "document/list",
                    Description = "List documents using an optional EnumerationQuery payload.",
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
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ListDocumentsAsync(query).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "document/get",
                    Description = "Get a document by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            documentId = new { type = "string", description = "Document identifier." }
                        },
                        required = new[] { "documentId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetDocumentAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "documentId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "document/upload",
                    Description = "Upload a document for ingestion.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            ingestionRuleId = new { type = "string", description = "Ingestion rule identifier." },
                            contentBase64 = new { type = "string", description = "Document content serialized as base64." },
                            name = new { type = "string", description = "Optional display name." },
                            originalFilename = new { type = "string", description = "Optional original filename." },
                            contentType = new { type = "string", description = "Optional content type." }
                        },
                        required = new[] { "ingestionRuleId", "contentBase64" }
                    },
                    Handler = args =>
                    {
                        string ingestionRuleId = AssistantHubMcpServerHelpers.GetStringRequired(args, "ingestionRuleId");
                        byte[] data = AssistantHubMcpServerHelpers.GetBase64BytesRequired(args, "contentBase64", context.Settings.Storage.MaxInlineBinaryBytes);
                        string? name = AssistantHubMcpServerHelpers.GetStringOptional(args, "name");
                        string? originalFilename = AssistantHubMcpServerHelpers.GetStringOptional(args, "originalFilename");
                        string? contentType = AssistantHubMcpServerHelpers.GetStringOptional(args, "contentType");
                        AssistantDocument result = context.Sdk.UploadDocumentAsync(ingestionRuleId, data, name, originalFilename, contentType).GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "document/delete",
                    Description = "Delete a document.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            documentId = new { type = "string", description = "Document identifier." }
                        },
                        required = new[] { "documentId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteDocumentAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "documentId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "document/bulk-delete",
                    Description = "Delete multiple documents by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            documentIdsJson = new { type = "string", description = "Array of document identifiers serialized as JSON string." }
                        },
                        required = new[] { "documentIdsJson" }
                    },
                    Handler = args =>
                    {
                        List<string> documentIds = AssistantHubMcpServerHelpers.DeserializeRequired<List<string>>(args, "documentIdsJson");
                        context.Sdk.BulkDeleteDocumentsAsync(documentIds).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "document/exists",
                    Description = "Check whether a document exists.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            documentId = new { type = "string", description = "Document identifier." }
                        },
                        required = new[] { "documentId" }
                    },
                    Handler = args => context.Sdk.DocumentExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "documentId")).GetAwaiter().GetResult()
                },
                new()
                {
                    Name = "document/reindex",
                    Description = "Reindex a single completed document into Verbex.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            documentId = new { type = "string", description = "Document identifier." }
                        },
                        required = new[] { "documentId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ReindexDocumentAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "documentId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "document/reindex-batch",
                    Description = "Reindex completed documents into Verbex using optional DocumentReindexRequest and EnumerationQuery payloads.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            requestJson = new { type = "string", description = "Optional DocumentReindexRequest serialized as JSON string." },
                            queryJson = new { type = "string", description = "Optional EnumerationQuery serialized as JSON string for page-based backfill." }
                        },
                        required = System.Array.Empty<string>()
                    },
                    Handler = args =>
                    {
                        DocumentReindexRequest? request = AssistantHubMcpServerHelpers.DeserializeOptional<DocumentReindexRequest>(args, "requestJson");
                        EnumerationQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<EnumerationQuery>(args, "queryJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ReindexDocumentsAsync(request, query).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "document/processing-log",
                    Description = "Get a document processing log payload.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            documentId = new { type = "string", description = "Document identifier." }
                        },
                        required = new[] { "documentId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetDocumentProcessingLogAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "documentId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "document/download",
                    Description = "Download a document and return it inline as base64.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            documentId = new { type = "string", description = "Document identifier." }
                        },
                        required = new[] { "documentId" }
                    },
                    Handler = args =>
                    {
                        string documentId = AssistantHubMcpServerHelpers.GetStringRequired(args, "documentId");
                        BinaryResponse response = AssistantHubMcpRestProxy.Download(context, "/v1.0/documents/" + AssistantHubMcpRestProxy.Escape(documentId) + "/download");
                        AssistantHubMcpServerHelpers.EnsureBinaryWithinLimit(response.Bytes.LongLength, context.Settings.Storage.MaxInlineBinaryBytes, "document/download");
                        return AssistantHubMcpServerHelpers.SerializeBinaryEnvelope(response, "document/" + documentId);
                    }
                }
            };
        }
    }
}
