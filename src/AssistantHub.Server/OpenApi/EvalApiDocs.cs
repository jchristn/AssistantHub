namespace AssistantHub.Server.OpenApi
{
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for RAG evaluation routes: facts, runs, results, and progress streaming.
    /// </summary>
    public static class EvalApiDocs
    {
        #region Private-Members

        private const string _Tag = "Evaluation";

        private const string _FactId = "ef_01JH4E6F7G8H9J0K1M2N3P4Q5R";
        private const string _RunId = "erun_01JH4E7G8H9J0K1M2N3P4Q5R6S";
        private const string _ResultId = "eres_01JH4E8H9J0K1M2N3P4Q5R6S7T";
        private const string _ChatHistoryId = "chist_01JH4E9J0K1M2N3P4Q5R6S7T8V";
        private const string _TraceId = "trace_01JH4EAK1M2N3P4Q5R6S7T8V9W";

        private const string _Question = "What is the return policy?";
        private const string _ExpectedFacts = "[\"30 days\",\"full refund\",\"original receipt required\"]";

        private static EvalFact ExampleFactRequest()
        {
            return new EvalFact
            {
                AssistantId = ApiExamples.AssistantId,
                Category = "citation_required",
                Question = _Question,
                ExpectedFacts = _ExpectedFacts
            };
        }

        private static EvalFact ExampleFact()
        {
            EvalFact fact = ExampleFactRequest();
            fact.Id = _FactId;
            fact.TenantId = ApiExamples.TenantId;
            fact.CreatedUtc = ApiExamples.Created;
            fact.LastUpdateUtc = ApiExamples.Updated;
            return fact;
        }

        private static EvalRun ExampleRun(EvalStatusEnum status)
        {
            EvalRun run = new EvalRun
            {
                Id = _RunId,
                TenantId = ApiExamples.TenantId,
                AssistantId = ApiExamples.AssistantId,
                Status = status,
                TotalFacts = 10,
                ExecutionMode = "ChatRail",
                CategoryFilterJson = "[\"citation_required\",\"unanswerable\"]",
                StartedUtc = ApiExamples.Updated,
                CreatedUtc = ApiExamples.Updated
            };

            if (status == EvalStatusEnum.Completed)
            {
                run.FactsEvaluated = 10;
                run.FactsPassed = 8;
                run.FactsFailed = 2;
                run.PassRate = 80.0;
                run.CompletedUtc = ApiExamples.Updated.AddSeconds(90);
            }

            return run;
        }

        private static EvalResult ExampleResult()
        {
            return new EvalResult
            {
                Id = _ResultId,
                RunId = _RunId,
                FactId = _FactId,
                Question = _Question,
                ExpectedFacts = _ExpectedFacts,
                LlmResponse = "Our return policy allows returns within 30 days of purchase for a full refund. Please bring your original receipt [1].",
                FactVerdicts = "[{\"fact\":\"30 days\",\"pass\":true,\"reasoning\":\"PASS\\nThe response states returns are allowed within 30 days.\"},{\"fact\":\"full refund\",\"pass\":true,\"reasoning\":\"PASS\\nThe response mentions a full refund.\"},{\"fact\":\"original receipt required\",\"pass\":true,\"reasoning\":\"PASS\\nThe response asks the customer to bring the original receipt.\"}]",
                OverallPass = true,
                ChatHistoryId = _ChatHistoryId,
                TraceId = _TraceId,
                RetrievalJson = "{\"ChunksReturned\":3,\"TopScore\":0.87}",
                CitationsJson = "[{\"Index\":1,\"DocumentId\":\"" + ApiExamples.DocumentId + "\",\"DocumentName\":\"returns-policy.pdf\"}]",
                ToolCallsJson = "[]",
                QueryClass = "factual_lookup",
                AnswerabilityDecision = "answerable",
                DurationMs = 1500,
                CreatedUtc = ApiExamples.Updated.AddSeconds(5)
            };
        }

        private const string _StreamExample =
            "id: 1\n" +
            "event: update\n" +
            "data: {\"Run\":{\"Id\":\"" + _RunId + "\",\"TenantId\":\"" + ApiExamples.TenantId + "\",\"AssistantId\":\"" + ApiExamples.AssistantId + "\",\"Status\":\"Running\",\"TotalFacts\":10,\"FactsEvaluated\":1,\"FactsPassed\":1,\"FactsFailed\":0,\"PassRate\":100,\"ExecutionMode\":\"ChatRail\",\"StartedUtc\":\"2026-01-16T09:45:00Z\",\"CreatedUtc\":\"2026-01-16T09:45:00Z\"},\"Results\":[{\"Id\":\"" + _ResultId + "\",\"RunId\":\"" + _RunId + "\",\"FactId\":\"" + _FactId + "\",\"Question\":\"What is the return policy?\",\"OverallPass\":true,\"DurationMs\":1500}]}\n" +
            "\n" +
            "id: 2\n" +
            "event: update\n" +
            "data: {\"Run\":{\"Id\":\"" + _RunId + "\",\"Status\":\"Completed\",\"TotalFacts\":10,\"FactsEvaluated\":10,\"FactsPassed\":8,\"FactsFailed\":2,\"PassRate\":80},\"Results\":[]}\n" +
            "\n" +
            "id: 3\n" +
            "event: done\n" +
            "data: [DONE]\n" +
            "\n";

        #endregion

        #region Public-Members

        /// <summary>PUT /v1.0/eval/facts.</summary>
        public static OpenApiRouteMetadata CreateFact => ApiDoc.Create("Create eval fact", _Tag)
            .Describe("Creates an evaluation fact: a question to ask an assistant plus the facts its answer is expected to contain. AssistantId is required. TenantId is always set to the caller's tenant; Id is generated when omitted. ExpectedFacts is a JSON-encoded array of strings. Recommended categories are factual_lookup, multi_hop, aggregation, temporal, ambiguous_query, unanswerable, citation_required, tool_required, and structured_data; custom categories are allowed.")
            .Body("Fact to create.", ExampleFactRequest())
            .Returns(201, "Fact created.", ExampleFact())
            .Errors(400, 401, 500);

        /// <summary>GET /v1.0/eval/facts.</summary>
        public static OpenApiRouteMetadata ListFacts => ApiDoc.Create("List eval facts", _Tag)
            .Describe("Enumerates evaluation facts in the caller's tenant.")
            .Paged()
            .Query("assistantId", "string", "Only return facts for this assistant.", false, ApiExamples.AssistantId)
            .Returns(200, "Page of eval facts.", ApiExamples.Page(ExampleFact()))
            .Errors(401, 500);

        /// <summary>GET /v1.0/eval/facts/{factId}.</summary>
        public static OpenApiRouteMetadata ReadFact => ApiDoc.Create("Get eval fact", _Tag)
            .Describe("Returns one evaluation fact. The fact must belong to the caller's tenant (global administrators can read any tenant).")
            .Returns(200, "Eval fact.", ExampleFact())
            .Errors(400, 401, 404, 500);

        /// <summary>PUT /v1.0/eval/facts/{factId}.</summary>
        public static OpenApiRouteMetadata UpdateFact => ApiDoc.Create("Update eval fact", _Tag)
            .Describe("Updates an evaluation fact. Only Category, Question, and ExpectedFacts are applied (omitted values are cleared); Id, TenantId, AssistantId, and CreatedUtc are preserved. The fact must belong to the caller's tenant.")
            .Body("Updated fact values.", ExampleFactRequest())
            .Returns(200, "Updated eval fact.", ExampleFact())
            .Errors(400, 401, 404, 500);

        /// <summary>DELETE /v1.0/eval/facts/{factId}.</summary>
        public static OpenApiRouteMetadata DeleteFact => ApiDoc.Create("Delete eval fact", _Tag)
            .Describe("Deletes an evaluation fact. The fact must belong to the caller's tenant. Results from earlier runs are not deleted.")
            .ReturnsNoContent(204, "Eval fact deleted.")
            .Errors(400, 401, 404, 500);

        /// <summary>POST /v1.0/eval/runs.</summary>
        public static OpenApiRouteMetadata StartRun => ApiDoc.Create("Start eval run", _Tag)
            .Describe("Starts an evaluation run for an assistant and returns immediately with the run in the Running state; facts are evaluated in the background. Each fact's question is sent to the assistant (ExecutionMode ChatRail runs the full chat/RAG pipeline; InferenceOnly calls the model directly), then an LLM judge grades the answer against each expected fact. JudgePrompt overrides the default judge prompt when it contains the {EXPECTED_FACT} placeholder. Categories limits the run to facts in those categories. Returns 400 when AssistantId is missing, and 400 with a body of the form {\"Message\": \"...\"} when the assistant has no matching facts or its settings or inference endpoint are not configured. Track progress with GET /v1.0/eval/runs/{runId} or the stream endpoint.")
            .Body("Run parameters.", new EvalRunRequest
            {
                AssistantId = ApiExamples.AssistantId,
                ExecutionMode = "ChatRail",
                Categories = new List<string> { "citation_required", "unanswerable" }
            })
            .Returns(201, "Run created and started.", ExampleRun(EvalStatusEnum.Running))
            .Errors(400, 401, 500);

        /// <summary>GET /v1.0/eval/runs.</summary>
        public static OpenApiRouteMetadata ListRuns => ApiDoc.Create("List eval runs", _Tag)
            .Describe("Enumerates evaluation runs in the caller's tenant.")
            .Paged()
            .Query("assistantId", "string", "Only return runs for this assistant.", false, ApiExamples.AssistantId)
            .Returns(200, "Page of eval runs.", ApiExamples.Page(ExampleRun(EvalStatusEnum.Completed)))
            .Errors(401, 500);

        /// <summary>GET /v1.0/eval/runs/{runId}.</summary>
        public static OpenApiRouteMetadata ReadRun => ApiDoc.Create("Get eval run", _Tag)
            .Describe("Returns one evaluation run with its status (Pending, Running, Completed, or Failed) and pass/fail counters. The run must belong to the caller's tenant.")
            .Returns(200, "Eval run.", ExampleRun(EvalStatusEnum.Completed))
            .Errors(400, 401, 404, 500);

        /// <summary>DELETE /v1.0/eval/runs/{runId}.</summary>
        public static OpenApiRouteMetadata DeleteRun => ApiDoc.Create("Delete eval run", _Tag)
            .Describe("Deletes an evaluation run and its results. The run must belong to the caller's tenant.")
            .ReturnsNoContent(204, "Eval run deleted.")
            .Errors(400, 401, 404, 500);

        /// <summary>GET /v1.0/eval/runs/{runId}/results.</summary>
        public static OpenApiRouteMetadata ListRunResults => ApiDoc.Create("List eval run results", _Tag)
            .Describe("Returns every result recorded so far for a run as a plain array (not a paged envelope). FactVerdicts is a JSON-encoded array of {fact, pass, reasoning} objects. The run must belong to the caller's tenant.")
            .Returns(200, "Eval results.", new List<EvalResult> { ExampleResult() })
            .Errors(400, 401, 404, 500);

        /// <summary>GET /v1.0/eval/runs/{runId}/stream.</summary>
        public static OpenApiRouteMetadata StreamRun => ApiDoc.Create("Stream eval run progress", _Tag)
            .Describe("Streams run progress as server-sent events. Every 2 seconds an 'update' event is sent whose data is a JSON object {Run, Results} with the current run and all results so far. When the run reaches Completed or Failed, a final 'update' event is followed by a 'done' event with data [DONE] and the stream closes. The run must belong to the caller's tenant; 400 and 404 are returned as JSON before the stream starts.")
            .ReturnsContent(200, "Server-sent event stream of run progress.", "text/event-stream", ApiSchema.String("Server-sent events: 'update' events carrying {Run, Results} JSON, then a final 'done' event."), _StreamExample)
            .Errors(400, 401, 404);

        /// <summary>GET /v1.0/eval/results/{resultId}.</summary>
        public static OpenApiRouteMetadata ReadResult => ApiDoc.Create("Get eval result", _Tag)
            .Describe("Returns one evaluation result: the question, the assistant's response, per-fact judge verdicts, and chat-rail diagnostics (retrieval, citations, tool calls, query class, answerability).")
            .Returns(200, "Eval result.", ExampleResult())
            .Errors(400, 401, 404, 500);

        /// <summary>GET /v1.0/eval/judge-prompt/default.</summary>
        public static OpenApiRouteMetadata DefaultJudgePrompt => ApiDoc.Create("Get default judge prompt", _Tag)
            .Describe("Returns the built-in judge prompt template. The placeholders {QUESTION}, {RESPONSE}, and {EXPECTED_FACT} are substituted for each expected fact, and the judge must answer PASS or FAIL on the first line.")
            .Returns(200, "Default judge prompt.", new { Prompt = EvalService.DefaultJudgePrompt })
            .Errors(401, 500);

        #endregion
    }
}
