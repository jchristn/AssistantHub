namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for tenant operations.
    /// </summary>
    public static class TenantRegistrations
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
                    Name = "tenant/list",
                    Description = "List tenants using an optional EnumerationQuery payload.",
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
                            ? context.Sdk.ListTenantsAsync(query).GetAwaiter().GetResult()
                            : context.Sdk.ListTenantsAsync().GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "tenant/get",
                    Description = "Get a tenant by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." }
                        },
                        required = new[] { "tenantId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetTenantAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "tenant/create",
                    Description = "Create a tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantJson = new { type = "string", description = "TenantMetadata serialized as JSON string." }
                        },
                        required = new[] { "tenantJson" }
                    },
                    Handler = args =>
                    {
                        TenantMetadata tenant = AssistantHubMcpServerHelpers.DeserializeRequired<TenantMetadata>(args, "tenantJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateTenantAsync(tenant).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "tenant/update",
                    Description = "Update an existing tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            tenantJson = new { type = "string", description = "TenantMetadata serialized as JSON string." }
                        },
                        required = new[] { "tenantId", "tenantJson" }
                    },
                    Handler = args =>
                    {
                        string tenantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId");
                        TenantMetadata tenant = AssistantHubMcpServerHelpers.DeserializeRequired<TenantMetadata>(args, "tenantJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateTenantAsync(tenantId, tenant).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "tenant/delete",
                    Description = "Delete a tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." }
                        },
                        required = new[] { "tenantId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteTenantAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "tenant/exists",
                    Description = "Check whether a tenant exists.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." }
                        },
                        required = new[] { "tenantId" }
                    },
                    Handler = args => context.Sdk.TenantExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId")).GetAwaiter().GetResult()
                }
            };
        }
    }
}
