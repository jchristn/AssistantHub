namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic;

    /// <summary>
    /// Registration methods for credential operations.
    /// </summary>
    public static class CredentialRegistrations
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
                    Name = "credential/list",
                    Description = "List credentials for a tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            queryJson = new { type = "string", description = "Optional EnumerationQuery serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, do not redact bearer tokens." }
                        },
                        required = new[] { "tenantId" }
                    },
                    Handler = args =>
                    {
                        string tenantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId");
                        EnumerationQuery? query = AssistantHubMcpServerHelpers.DeserializeOptional<EnumerationQuery>(args, "queryJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        object result = query != null
                            ? context.Sdk.ListCredentialsAsync(tenantId, query).GetAwaiter().GetResult()
                            : context.Sdk.ListCredentialsAsync(tenantId).GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets);
                    }
                },
                new()
                {
                    Name = "credential/get",
                    Description = "Get a credential by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            credentialId = new { type = "string", description = "Credential identifier." },
                            includeSecrets = new { type = "boolean", description = "If true, do not redact the bearer token." }
                        },
                        required = new[] { "tenantId", "credentialId" }
                    },
                    Handler = args =>
                    {
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(
                            context,
                            context.Sdk.GetCredentialAsync(
                                AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId"),
                                AssistantHubMcpServerHelpers.GetStringRequired(args, "credentialId")).GetAwaiter().GetResult(),
                            includeSecrets);
                    }
                },
                new()
                {
                    Name = "credential/create",
                    Description = "Create a credential under a tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            credentialJson = new { type = "string", description = "Credential serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include the bearer token in the response." }
                        },
                        required = new[] { "tenantId", "credentialJson" }
                    },
                    Handler = args =>
                    {
                        string tenantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId");
                        Credential credential = AssistantHubMcpServerHelpers.DeserializeRequired<Credential>(args, "credentialJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateCredentialAsync(tenantId, credential).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "credential/update",
                    Description = "Update a credential under a tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            credentialId = new { type = "string", description = "Credential identifier." },
                            credentialJson = new { type = "string", description = "Credential serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include the bearer token in the response." }
                        },
                        required = new[] { "tenantId", "credentialId", "credentialJson" }
                    },
                    Handler = args =>
                    {
                        string tenantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId");
                        string credentialId = AssistantHubMcpServerHelpers.GetStringRequired(args, "credentialId");
                        Credential credential = AssistantHubMcpServerHelpers.DeserializeRequired<Credential>(args, "credentialJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateCredentialAsync(tenantId, credentialId, credential).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "credential/delete",
                    Description = "Delete a credential under a tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            credentialId = new { type = "string", description = "Credential identifier." }
                        },
                        required = new[] { "tenantId", "credentialId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteCredentialAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId"), AssistantHubMcpServerHelpers.GetStringRequired(args, "credentialId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "credential/exists",
                    Description = "Check whether a credential exists under a tenant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tenantId = new { type = "string", description = "Tenant identifier." },
                            credentialId = new { type = "string", description = "Credential identifier." }
                        },
                        required = new[] { "tenantId", "credentialId" }
                    },
                    Handler = args => context.Sdk.CredentialExistsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "tenantId"), AssistantHubMcpServerHelpers.GetStringRequired(args, "credentialId")).GetAwaiter().GetResult()
                }
            };
        }
    }
}
