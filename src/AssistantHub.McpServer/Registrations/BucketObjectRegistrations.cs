namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for bucket object operations.
    /// </summary>
    public static class BucketObjectRegistrations
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
                    Name = "bucket/object/put",
                    Description = "Create an empty object marker in a bucket.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bucketName = new { type = "string", description = "Bucket name." },
                            key = new { type = "string", description = "Object key." }
                        },
                        required = new[] { "bucketName", "key" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.PutBucketObjectAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "bucketName"), AssistantHubMcpServerHelpers.GetStringRequired(args, "key")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "bucket/object/list",
                    Description = "List objects in a bucket.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bucketName = new { type = "string", description = "Bucket name." },
                            prefix = new { type = "string", description = "Optional key prefix filter." },
                            delimiter = new { type = "string", description = "Optional delimiter. Defaults to '/'." }
                        },
                        required = new[] { "bucketName" }
                    },
                    Handler = args =>
                    {
                        string bucketName = AssistantHubMcpServerHelpers.GetStringRequired(args, "bucketName");
                        string? prefix = AssistantHubMcpServerHelpers.GetStringOptional(args, "prefix");
                        string? delimiter = AssistantHubMcpServerHelpers.GetStringOptional(args, "delimiter");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ListBucketObjectsAsync(bucketName, prefix, delimiter ?? "/").GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "bucket/object/metadata",
                    Description = "Get metadata for a bucket object.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bucketName = new { type = "string", description = "Bucket name." },
                            key = new { type = "string", description = "Object key." }
                        },
                        required = new[] { "bucketName", "key" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetBucketObjectMetadataAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "bucketName"), AssistantHubMcpServerHelpers.GetStringRequired(args, "key")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "bucket/object/delete",
                    Description = "Delete an object from a bucket.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bucketName = new { type = "string", description = "Bucket name." },
                            key = new { type = "string", description = "Object key." }
                        },
                        required = new[] { "bucketName", "key" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteBucketObjectAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "bucketName"), AssistantHubMcpServerHelpers.GetStringRequired(args, "key")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "bucket/object/download",
                    Description = "Download a bucket object and return it inline as base64.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bucketName = new { type = "string", description = "Bucket name." },
                            key = new { type = "string", description = "Object key." }
                        },
                        required = new[] { "bucketName", "key" }
                    },
                    Handler = args =>
                    {
                        string bucketName = AssistantHubMcpServerHelpers.GetStringRequired(args, "bucketName");
                        string key = AssistantHubMcpServerHelpers.GetStringRequired(args, "key");
                        BinaryResponse response = AssistantHubMcpRestProxy.Download(
                            context,
                            "/v1.0/buckets/" + AssistantHubMcpRestProxy.Escape(bucketName) + "/objects/download?key=" + AssistantHubMcpRestProxy.Escape(key));
                        AssistantHubMcpServerHelpers.EnsureBinaryWithinLimit(response.Bytes.LongLength, context.Settings.Storage.MaxInlineBinaryBytes, "bucket/object/download");
                        return AssistantHubMcpServerHelpers.SerializeBinaryEnvelope(response, "bucket/" + bucketName + "/" + key);
                    }
                },
                new()
                {
                    Name = "bucket/object/upload",
                    Description = "Upload binary content to a bucket object.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bucketName = new { type = "string", description = "Bucket name." },
                            key = new { type = "string", description = "Object key." },
                            contentBase64 = new { type = "string", description = "Object content serialized as base64." },
                            contentType = new { type = "string", description = "Optional content type. Defaults to application/octet-stream." }
                        },
                        required = new[] { "bucketName", "key", "contentBase64" }
                    },
                    Handler = args =>
                    {
                        string bucketName = AssistantHubMcpServerHelpers.GetStringRequired(args, "bucketName");
                        string key = AssistantHubMcpServerHelpers.GetStringRequired(args, "key");
                        byte[] data = AssistantHubMcpServerHelpers.GetBase64BytesRequired(args, "contentBase64", context.Settings.Storage.MaxInlineBinaryBytes);
                        string? contentType = AssistantHubMcpServerHelpers.GetStringOptional(args, "contentType");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UploadBucketObjectAsync(bucketName, key, data, contentType ?? "application/octet-stream").GetAwaiter().GetResult(), includeSecrets: true);
                    }
                }
            };
        }
    }
}
