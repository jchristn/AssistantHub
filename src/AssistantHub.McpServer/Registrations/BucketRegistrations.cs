namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for bucket operations.
    /// </summary>
    public static class BucketRegistrations
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
                    Name = "bucket/list",
                    Description = "List buckets.",
                    InputSchema = McpRegistrationHelper.EmptySchema,
                    Handler = _ => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ListBucketsAsync().GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "bucket/get",
                    Description = "Get a bucket by name.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bucketName = new { type = "string", description = "Bucket name." }
                        },
                        required = new[] { "bucketName" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetBucketAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "bucketName")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "bucket/create",
                    Description = "Create a bucket.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bucketJson = new { type = "string", description = "BucketCreateRequest serialized as JSON string." }
                        },
                        required = new[] { "bucketJson" }
                    },
                    Handler = args =>
                    {
                        BucketCreateRequest request = AssistantHubMcpServerHelpers.DeserializeRequired<BucketCreateRequest>(args, "bucketJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateBucketAsync(request).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "bucket/delete",
                    Description = "Delete a bucket.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bucketName = new { type = "string", description = "Bucket name." }
                        },
                        required = new[] { "bucketName" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteBucketAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "bucketName")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "bucket/exists",
                    Description = "Check whether a bucket exists.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bucketName = new { type = "string", description = "Bucket name." }
                        },
                        required = new[] { "bucketName" }
                    },
                    Handler = args => context.Sdk.BucketExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "bucketName")).GetAwaiter().GetResult()
                }
            };
        }
    }
}
