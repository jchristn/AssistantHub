namespace AssistantHub.McpServer.Registrations
{
    using System.Collections.Generic;
    using AssistantHub.McpServer.Classes;
    using AssistantHub.Sdk.Models;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for evaluation operations.
    /// </summary>
    public static class EvalRegistrations
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
                    Name = "eval/fact/list",
                    Description = "List evaluation facts using an optional EnumerationQuery payload.",
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
                            ? context.Sdk.ListEvalFactsAsync(query).GetAwaiter().GetResult()
                            : context.Sdk.ListEvalFactsAsync().GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "eval/fact/get",
                    Description = "Get an evaluation fact by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            factId = new { type = "string", description = "Evaluation fact identifier." }
                        },
                        required = new[] { "factId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetEvalFactAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "factId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "eval/fact/create",
                    Description = "Create an evaluation fact.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            factJson = new { type = "string", description = "EvalFact serialized as JSON string." }
                        },
                        required = new[] { "factJson" }
                    },
                    Handler = args =>
                    {
                        EvalFact fact = AssistantHubMcpServerHelpers.DeserializeRequired<EvalFact>(args, "factJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.CreateEvalFactAsync(fact).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "eval/fact/update",
                    Description = "Update an evaluation fact.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            factId = new { type = "string", description = "Evaluation fact identifier." },
                            factJson = new { type = "string", description = "EvalFact serialized as JSON string." }
                        },
                        required = new[] { "factId", "factJson" }
                    },
                    Handler = args =>
                    {
                        string factId = AssistantHubMcpServerHelpers.GetStringRequired(args, "factId");
                        EvalFact fact = AssistantHubMcpServerHelpers.DeserializeRequired<EvalFact>(args, "factJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.UpdateEvalFactAsync(factId, fact).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "eval/fact/delete",
                    Description = "Delete an evaluation fact.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            factId = new { type = "string", description = "Evaluation fact identifier." }
                        },
                        required = new[] { "factId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteEvalFactAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "factId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "eval/run/create",
                    Description = "Start an evaluation run.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            requestJson = new { type = "string", description = "EvalRunRequest serialized as JSON string." }
                        },
                        required = new[] { "requestJson" }
                    },
                    Handler = args =>
                    {
                        EvalRunRequest request = AssistantHubMcpServerHelpers.DeserializeRequired<EvalRunRequest>(args, "requestJson");
                        return AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.StartEvalRunAsync(request).GetAwaiter().GetResult(), includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "eval/run/list",
                    Description = "List evaluation runs using an optional EnumerationQuery payload.",
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
                            ? context.Sdk.ListEvalRunsAsync(query).GetAwaiter().GetResult()
                            : context.Sdk.ListEvalRunsAsync().GetAwaiter().GetResult();
                        return AssistantHubMcpServerHelpers.Serialize(context, result, includeSecrets: true);
                    }
                },
                new()
                {
                    Name = "eval/run/get",
                    Description = "Get an evaluation run by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            runId = new { type = "string", description = "Evaluation run identifier." }
                        },
                        required = new[] { "runId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetEvalRunAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "runId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "eval/run/delete",
                    Description = "Delete an evaluation run and its results.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            runId = new { type = "string", description = "Evaluation run identifier." }
                        },
                        required = new[] { "runId" }
                    },
                    Handler = args =>
                    {
                        context.Sdk.DeleteEvalRunAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "runId")).GetAwaiter().GetResult();
                        return true;
                    }
                },
                new()
                {
                    Name = "eval/run/results",
                    Description = "Get all evaluation results for a run.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            runId = new { type = "string", description = "Evaluation run identifier." }
                        },
                        required = new[] { "runId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetEvalRunResultsAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "runId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "eval/result/get",
                    Description = "Get a single evaluation result by identifier.",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            resultId = new { type = "string", description = "Evaluation result identifier." }
                        },
                        required = new[] { "resultId" }
                    },
                    Handler = args => AssistantHubMcpServerHelpers.Serialize(context, context.Sdk.GetEvalResultAsync(AssistantHubMcpServerHelpers.GetStringRequired(args, "resultId")).GetAwaiter().GetResult(), includeSecrets: true)
                },
                new()
                {
                    Name = "eval/judge-prompt/default",
                    Description = "Get the default evaluation judge prompt.",
                    InputSchema = McpRegistrationHelper.EmptySchema,
                    Handler = _ => AssistantHubMcpServerHelpers.Serialize(context, new { Prompt = context.Sdk.GetDefaultJudgePromptAsync().GetAwaiter().GetResult() }, includeSecrets: true)
                }
            };
        }
    }
}
