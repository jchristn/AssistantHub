namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for assistant settings operations.
    /// </summary>
    public static class AssistantSettingsRegistrations
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
                    Name = "assistant/settings/get",
                    Description = "Get assistant settings.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." },
                            includeSecrets = new { type = "boolean", description = "If true, include secret-bearing fields." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args =>
                    {
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetAssistantSettingsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId")).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "assistant/settings/update",
                    Description = "Create or update assistant settings.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." },
                            settingsJson = new { type = "string", description = "AssistantSettings serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include secret-bearing fields." }
                        },
                        required = new[] { "assistantId", "settingsJson" }
                    },
                    Handler = args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        AssistantSettings settings = AssistantHubMcpServerHelpers.DeserializeRequired<AssistantSettings>(args, "settingsJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateAssistantSettingsAsync(assistantId, settings).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "assistant/settings/tools/list",
                    Description = "Get effective server-side tool availability for an assistant.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(
                        context,
                        context.Sdk.GetAssistantToolsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId")).GetAwaiter().GetResult())
                },
                new()
                {
                    Name = "assistant/settings/tools/validate",
                    Description = "Validate a draft assistant tool policy without persisting it.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." },
                            requestJson = new { type = "string", description = "Optional AssistantToolPolicyValidationRequest serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include secret-bearing fields." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        AssistantToolPolicyValidationRequest request = AssistantHubMcpServerHelpers.DeserializeOptional<AssistantToolPolicyValidationRequest>(args, "requestJson")
                            ?? new AssistantToolPolicyValidationRequest();
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.ValidateAssistantToolPolicyAsync(assistantId, request).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "assistant/settings/tools/test",
                    Description = "Run administrator dry-run diagnostics for an assistant tool policy without executing tools.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." },
                            requestJson = new { type = "string", description = "Optional AssistantToolPolicyValidationRequest serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include secret-bearing fields." }
                        },
                        required = new[] { "assistantId" }
                    },
                    Handler = args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        AssistantToolPolicyValidationRequest request = AssistantHubMcpServerHelpers.DeserializeOptional<AssistantToolPolicyValidationRequest>(args, "requestJson")
                            ?? new AssistantToolPolicyValidationRequest();
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.TestAssistantToolPolicyAsync(assistantId, request).GetAwaiter().GetResult(), includeSecrets);
                    }
                },
                new()
                {
                    Name = "assistant/settings/slack/verify",
                    Description = "Verify Slack settings for an assistant without persisting them.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            assistantId = new { type = "string", description = "Assistant identifier." },
                            requestJson = new { type = "string", description = "SlackVerificationRequest serialized as JSON string." },
                            includeSecrets = new { type = "boolean", description = "If true, include secret-bearing fields." }
                        },
                        required = new[] { "assistantId", "requestJson" }
                    },
                    Handler = args =>
                    {
                        string assistantId = AssistantHubMcpServerHelpers.GetStringRequired(args, "assistantId");
                        SlackVerificationRequest request = AssistantHubMcpServerHelpers.DeserializeRequired<SlackVerificationRequest>(args, "requestJson");
                        bool includeSecrets = AssistantHubMcpServerHelpers.GetBoolOrDefault(args, "includeSecrets", false);
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.VerifySlackAsync(assistantId, request).GetAwaiter().GetResult(), includeSecrets);
                    }
                }
            };
        }
    }
}
