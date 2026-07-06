namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Service for running RAG evaluation against the inference pipeline.
    /// </summary>
    public class EvalService
    {
        #region Private-Members

        private string _Header = "[EvalService] ";
        private AssistantHubSettings _Settings = null;
        private LoggingModule _Logging = null;
        private DatabaseDriverBase _Database = null;
        private InferenceService _Inference = null;
        private IInferenceEndpointService _InferenceEndpoints = null;
        private IEvalChatExecutor _EvalChatExecutor = null;

        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly string _DefaultJudgePrompt =
            "You are an evaluation judge. Given a question, a response, and an expected fact, determine if the response contains or supports the expected fact.\n\n" +
            "Question: {QUESTION}\n\n" +
            "Response: {RESPONSE}\n\n" +
            "Expected Fact: {EXPECTED_FACT}\n\n" +
            "Your first line of output MUST be exactly PASS or FAIL (nothing else on that line).\n" +
            "On subsequent lines, provide a brief explanation of your reasoning.";

        #endregion

        #region Public-Members

        /// <summary>
        /// Default judge prompt template.
        /// </summary>
        public static string DefaultJudgePrompt => _DefaultJudgePrompt;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EvalService(
            AssistantHubSettings settings,
            LoggingModule logging,
            DatabaseDriverBase database,
            InferenceService inference,
            IInferenceEndpointService inferenceEndpoints = null,
            IEvalChatExecutor evalChatExecutor = null)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Inference = inference ?? throw new ArgumentNullException(nameof(inference));
            _InferenceEndpoints = inferenceEndpoints ?? new PartioInferenceEndpointService(_Settings.Chunking, _Logging);
            _EvalChatExecutor = evalChatExecutor;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Start an evaluation run for an assistant. Executes in the background.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="judgePromptOverride">Optional judge prompt override for this run.</param>
        /// <param name="executionMode">Execution mode, either ChatRail or InferenceOnly.</param>
        /// <param name="categories">Optional eval fact categories to include.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The created EvalRun.</returns>
        public async Task<EvalRun> StartRunAsync(
            string tenantId,
            string assistantId,
            string judgePromptOverride = null,
            string executionMode = null,
            List<string> categories = null,
            CancellationToken token = default)
        {
            // Load all facts for this assistant
            EnumerationQuery query = new EnumerationQuery { MaxResults = 1000 };
            query.AssistantIdFilter = assistantId;
            EnumerationResult<EvalFact> factsResult = await _Database.EvalFact.EnumerateAsync(tenantId, query, token).ConfigureAwait(false);

            if (factsResult == null || factsResult.Objects == null || factsResult.Objects.Count == 0)
                throw new InvalidOperationException("No eval facts defined for this assistant. Create at least one fact before starting a run.");

            List<EvalFact> facts = ApplyCategoryFilter(factsResult.Objects, categories);
            if (facts.Count == 0)
                throw new InvalidOperationException("No eval facts matched the requested category filter.");

            // Load assistant settings to resolve inference endpoint and judge prompt
            AssistantSettings settings = await _Database.AssistantSettings.ReadByAssistantIdAsync(assistantId, token).ConfigureAwait(false);
            if (settings == null)
                throw new InvalidOperationException("Assistant settings not found for assistant " + assistantId);
            if (String.IsNullOrWhiteSpace(settings.InferenceEndpointId))
                throw new InvalidOperationException("Assistant inference endpoint is not configured for assistant " + assistantId);

            // Determine the effective judge prompt: run override > assistant setting > default
            string effectiveJudgePrompt = _DefaultJudgePrompt;
            if (!String.IsNullOrEmpty(settings.EvalJudgePrompt) && settings.EvalJudgePrompt.Contains("{EXPECTED_FACT}"))
            {
                effectiveJudgePrompt = settings.EvalJudgePrompt;
            }
            if (!String.IsNullOrEmpty(judgePromptOverride) && judgePromptOverride.Contains("{EXPECTED_FACT}"))
            {
                effectiveJudgePrompt = judgePromptOverride;
            }

            EvalRun run = new EvalRun();
            run.TenantId = tenantId;
            run.AssistantId = assistantId;
            run.Status = EvalStatusEnum.Running;
            run.TotalFacts = facts.Count;
            run.StartedUtc = DateTime.UtcNow;
            run.JudgePrompt = effectiveJudgePrompt != _DefaultJudgePrompt ? effectiveJudgePrompt : null;
            run.ExecutionMode = NormalizeExecutionMode(executionMode);
            run.CategoryFilterJson = SerializeCategoryFilter(categories);
            await _Database.EvalRun.CreateAsync(run, token).ConfigureAwait(false);

            _Logging.Info(_Header + "starting eval run " + run.Id + " for assistant " + assistantId + " with " + facts.Count + " facts, mode=" + run.ExecutionMode);

            // Fire and forget
            _ = Task.Run(async () => await ExecuteRunAsync(run, facts, settings, effectiveJudgePrompt).ConfigureAwait(false));

            return run;
        }

        #endregion

        #region Private-Methods

        private static List<EvalFact> ApplyCategoryFilter(List<EvalFact> facts, List<string> categories)
        {
            if (facts == null) return new List<EvalFact>();
            List<string> normalized = NormalizeCategories(categories);
            if (normalized.Count < 1) return facts;

            HashSet<string> allowed = new HashSet<string>(normalized, StringComparer.OrdinalIgnoreCase);
            return facts
                .Where(fact => fact != null && allowed.Contains((fact.Category ?? "").Trim()))
                .ToList();
        }

        private static string NormalizeExecutionMode(string executionMode)
        {
            if (String.IsNullOrWhiteSpace(executionMode)) return "ChatRail";
            string normalized = executionMode.Trim();
            if (String.Equals(normalized, "InferenceOnly", StringComparison.OrdinalIgnoreCase)) return "InferenceOnly";
            return "ChatRail";
        }

        private static string SerializeCategoryFilter(List<string> categories)
        {
            List<string> normalized = NormalizeCategories(categories);
            return normalized.Count > 0 ? JsonSerializer.Serialize(normalized, _JsonOptions) : null;
        }

        private static List<string> NormalizeCategories(List<string> categories)
        {
            return categories?
                .Where(category => !String.IsNullOrWhiteSpace(category))
                .Select(category => category.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }

        private async Task ExecuteRunAsync(EvalRun run, List<EvalFact> facts, AssistantSettings settings, string judgePrompt)
        {
            try
            {
                // Resolve inference endpoint (same logic as ChatHandler)
                InferenceProviderEnum provider = _Settings.Inference.Provider;
                string endpoint = _Settings.Inference.Endpoint;
                string apiKey = _Settings.Inference.ApiKey;
                string model = _Settings.Inference.DefaultModel;

                if (!String.IsNullOrEmpty(settings.InferenceEndpointId))
                {
                    ResolvedEndpoint? resolved = await ResolveCompletionEndpointAsync(settings.InferenceEndpointId).ConfigureAwait(false);
                    if (resolved != null)
                    {
                        provider = resolved.Value.Provider;
                        endpoint = resolved.Value.Endpoint;
                        apiKey = resolved.Value.ApiKey;
                        if (!String.IsNullOrEmpty(resolved.Value.Model))
                            model = resolved.Value.Model;
                    }
                }

                int maxTokens = settings.MaxTokens;
                double temperature = settings.Temperature;
                double topP = settings.TopP;
                string systemPrompt = settings.SystemPrompt ?? "";

                int passed = 0;
                int failed = 0;

                foreach (EvalFact fact in facts)
                {
                    try
                    {
                        Stopwatch sw = Stopwatch.StartNew();

                        string llmResponse;
                        EvalChatExecutionResult chatRailResult = null;
                        if (String.Equals(run.ExecutionMode, "ChatRail", StringComparison.OrdinalIgnoreCase))
                        {
                            if (_EvalChatExecutor == null)
                                throw new InvalidOperationException("ChatRail eval mode is not available in this host.");

                            chatRailResult = await _EvalChatExecutor.ExecuteAsync(
                                new EvalChatExecutionRequest
                                {
                                    AssistantId = run.AssistantId,
                                    Origin = "eval",
                                    Messages = new List<ChatCompletionMessage>
                                    {
                                        new ChatCompletionMessage { Role = "user", Content = fact.Question }
                                    }
                                }).ConfigureAwait(false);

                            if (chatRailResult == null || !chatRailResult.Success)
                                throw new InvalidOperationException(chatRailResult?.ErrorMessage ?? "ChatRail eval execution failed.");

                            llmResponse = chatRailResult.ResponseText ?? String.Empty;
                        }
                        else
                        {
                            List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>
                            {
                                new ChatCompletionMessage { Role = "system", Content = systemPrompt },
                                new ChatCompletionMessage { Role = "user", Content = fact.Question }
                            };

                            InferenceResult chatResult = await _Inference.GenerateResponseAsync(
                                messages, model, maxTokens, temperature, topP,
                                provider, endpoint, apiKey).ConfigureAwait(false);

                            llmResponse = chatResult?.Content ?? String.Empty;
                        }

                        // Parse expected facts
                        List<string> expectedFacts = new List<string>();
                        if (!String.IsNullOrEmpty(fact.ExpectedFacts))
                        {
                            try
                            {
                                expectedFacts = JsonSerializer.Deserialize<List<string>>(fact.ExpectedFacts, _JsonOptions);
                            }
                            catch
                            {
                                expectedFacts = new List<string> { fact.ExpectedFacts };
                            }
                        }

                        // Judge each expected fact
                        List<FactVerdict> verdicts = new List<FactVerdict>();
                        bool allPass = true;

                        foreach (string expectedFact in expectedFacts)
                        {
                            string judgeQuestion = judgePrompt
                                .Replace("{QUESTION}", fact.Question)
                                .Replace("{RESPONSE}", llmResponse)
                                .Replace("{EXPECTED_FACT}", expectedFact);

                            List<ChatCompletionMessage> judgeMessages = new List<ChatCompletionMessage>
                            {
                                new ChatCompletionMessage { Role = "user", Content = judgeQuestion }
                            };

                            InferenceResult judgeResult = await _Inference.GenerateResponseAsync(
                                judgeMessages, model, 512, 0.0, 1.0,
                                provider, endpoint, apiKey).ConfigureAwait(false);

                            string judgeResponse = judgeResult?.Content ?? String.Empty;

                            // Parse first line for PASS/FAIL
                            string firstLine = judgeResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length > 0
                                ? judgeResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim()
                                : judgeResponse.Trim();
                            bool factPass = firstLine.IndexOf("PASS", StringComparison.OrdinalIgnoreCase) >= 0;

                            FactVerdict verdict = new FactVerdict
                            {
                                Fact = expectedFact,
                                Pass = factPass,
                                Reasoning = judgeResponse.Trim()
                            };

                            verdicts.Add(verdict);
                            if (!factPass) allPass = false;
                        }

                        sw.Stop();

                        // Store result
                        EvalResult evalResult = new EvalResult();
                        evalResult.RunId = run.Id;
                        evalResult.FactId = fact.Id;
                        evalResult.Question = fact.Question;
                        evalResult.ExpectedFacts = fact.ExpectedFacts;
                        evalResult.LlmResponse = llmResponse;
                        evalResult.FactVerdicts = JsonSerializer.Serialize(verdicts, _JsonOptions);
                        evalResult.OverallPass = allPass;
                        evalResult.DurationMs = sw.ElapsedMilliseconds;
                        evalResult.ChatHistoryId = chatRailResult?.ChatHistoryId;
                        evalResult.TraceId = chatRailResult?.TraceId;
                        evalResult.RetrievalJson = chatRailResult?.Retrieval == null ? null : JsonSerializer.Serialize(chatRailResult.Retrieval, _JsonOptions);
                        evalResult.CitationsJson = chatRailResult?.Citations == null ? null : JsonSerializer.Serialize(chatRailResult.Citations, _JsonOptions);
                        evalResult.ToolCallsJson = chatRailResult?.ToolCalls == null || chatRailResult.ToolCalls.Count < 1 ? null : JsonSerializer.Serialize(chatRailResult.ToolCalls, _JsonOptions);
                        evalResult.QueryClass = chatRailResult?.Retrieval?.QueryClass;
                        evalResult.AnswerabilityDecision = chatRailResult?.Retrieval?.AnswerabilityDecision;

                        await _Database.EvalResult.CreateAsync(evalResult).ConfigureAwait(false);

                        if (allPass) passed++;
                        else failed++;

                        // Update run progress
                        run.FactsEvaluated = passed + failed;
                        run.FactsPassed = passed;
                        run.FactsFailed = failed;
                        run.PassRate = run.FactsEvaluated > 0 ? Math.Round((double)passed / run.FactsEvaluated * 100, 1) : 0;
                        await _Database.EvalRun.UpdateAsync(run).ConfigureAwait(false);

                        _Logging.Debug(_Header + "evaluated fact " + fact.Id + " - " + (allPass ? "PASS" : "FAIL") + " (" + run.FactsEvaluated + "/" + run.TotalFacts + ")");
                    }
                    catch (Exception ex)
                    {
                        _Logging.Warn(_Header + "error evaluating fact " + fact.Id + ": " + ex.Message);
                        failed++;

                        run.FactsEvaluated = passed + failed;
                        run.FactsPassed = passed;
                        run.FactsFailed = failed;
                        run.PassRate = run.FactsEvaluated > 0 ? Math.Round((double)passed / run.FactsEvaluated * 100, 1) : 0;
                        await _Database.EvalRun.UpdateAsync(run).ConfigureAwait(false);
                    }
                }

                // Mark completed
                run.Status = EvalStatusEnum.Completed;
                run.CompletedUtc = DateTime.UtcNow;
                run.PassRate = run.FactsEvaluated > 0 ? Math.Round((double)passed / run.FactsEvaluated * 100, 1) : 0;
                await _Database.EvalRun.UpdateAsync(run).ConfigureAwait(false);

                _Logging.Info(_Header + "eval run " + run.Id + " completed: " + passed + " passed, " + failed + " failed, " + run.PassRate + "% pass rate");
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "eval run " + run.Id + " failed: " + ex.Message);
                run.Status = EvalStatusEnum.Failed;
                run.CompletedUtc = DateTime.UtcNow;

                try
                {
                    await _Database.EvalRun.UpdateAsync(run).ConfigureAwait(false);
                }
                catch { }
            }
        }

        private async Task<ResolvedEndpoint?> ResolveCompletionEndpointAsync(string endpointId)
        {
            try
            {
                using (HttpResponseMessage response = await _InferenceEndpoints.SendAsync(
                    HttpMethod.Get,
                    "/v1.0/endpoints/completion/" + endpointId).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(_Header + "failed to resolve completion endpoint " + endpointId + ": " + (int)response.StatusCode);
                        return null;
                    }

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    PartioEndpointConfig ep = JsonSerializer.Deserialize<PartioEndpointConfig>(body, _JsonOptions);
                    PartioEndpointToolMetadata.ReadTagsToToolFields(ep);

                    InferenceProviderEnum provider = InferenceProviderHelper.FromApiFormat(ep?.ApiFormat, InferenceProviderEnum.Ollama);

                    return new ResolvedEndpoint
                    {
                        Provider = provider,
                        Endpoint = ep?.Endpoint ?? _Settings.Inference.Endpoint,
                        ApiKey = ep?.ApiKey ?? _Settings.Inference.ApiKey,
                        Model = ep?.Model,
                        SupportsToolCalling = ep?.SupportsToolCalling == true,
                        ToolCallingApiFormat = ep?.ToolCallingApiFormat,
                        SupportsParallelToolCalls = ep?.SupportsParallelToolCalls == true,
                        SupportsStreamingToolCalls = ep?.SupportsStreamingToolCalls == true
                    };
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception resolving completion endpoint " + endpointId + ": " + e.Message);
                return null;
            }
        }

        #endregion
    }
}
