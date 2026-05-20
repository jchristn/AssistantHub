namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for user operations.
    /// </summary>
    public static class UserRegistrations
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
                    Name = "user/list",
                    Description = "List users for a tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            queryJson = new { type = "string", description = "Optional EnumerationQuery serialized as JSON string." }
                        },
                        required = new[] { "tenantId" }
                    },
                    Handler = args =>
                    {
                        string tenantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId");
                        EnumerationQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<EnumerationQuery>(args, "queryJson");
                        object result = query != null
                            ? context.Sdk.ListUsersAsync(tenantId, query).GetAwaiter().GetResult()
                            : context.Sdk.ListUsersAsync(tenantId).GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "user/get",
                    Description = "Get a user by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            userId = new { type = "string", description = "User identifier." }
                        },
                        required = new[] { "tenantId", "userId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetUserAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId"), AssistantHubMcpServerHelpers.GetStringRequired(args, "userId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "user/create",
                    Description = "Create a user under a tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            userJson = new { type = "string", description = "UserMaster serialized as JSON string." }
                        },
                        required = new[] { "tenantId", "userJson" }
                    },
                    Handler = args =>
                    {
                        string tenantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId");
                        UserMaster user = AssistantHubMcpServerHelpers.DeserializeRequired<UserMaster>(args, "userJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateUserAsync(tenantId, user).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "user/update",
                    Description = "Update a user under a tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            userId = new { type = "string", description = "User identifier." },
                            userJson = new { type = "string", description = "UserMaster serialized as JSON string." }
                        },
                        required = new[] { "tenantId", "userId", "userJson" }
                    },
                    Handler = args =>
                    {
                        string tenantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId");
                        string userId = AssistantHubMcpServerHelpers.GetStringRequired(args, "userId");
                        UserMaster user = AssistantHubMcpServerHelpers.DeserializeRequired<UserMaster>(args, "userJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateUserAsync(tenantId, userId, user).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "user/delete",
                    Description = "Delete a user under a tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            userId = new { type = "string", description = "User identifier." }
                        },
                        required = new[] { "tenantId", "userId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteUserAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId"), AssistantHubMcpServerHelpers.GetStringRequired(args, "userId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "user/exists",
                    Description = "Check whether a user exists under a tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            userId = new { type = "string", description = "User identifier." }
                        },
                        required = new[] { "tenantId", "userId" }
                    },
                    Handler = args => context.Sdk.UserExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId"), AssistantHubMcpServerHelpers.GetStringRequired(args, "userId")).GetAwaiter().GetResult()
                }
            };
        }
    }
}
