namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
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
            InferenceService inference)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Inference = inference ?? throw new ArgumentNullException(nameof(inference));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Start an evaluation run for an assistant. Executes in the background.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="judgePromptOverride">Optional judge prompt override for this run.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The created EvalRun.</returns>
        public async Task<EvalRun> StartRunAsync(
            string tenantId,
            string assistantId,
            string judgePromptOverride = null,
            CancellationToken token = default)
        {
            // Load all facts for this assistant
            EnumerationQuery query = new EnumerationQuery { MaxResults = 1000 };
            query.AssistantIdFilter = assistantId;
            EnumerationResult<EvalFact> factsResult = await _Database.EvalFact.EnumerateAsync(tenantId, query, token).ConfigureAwait(false);

            if (factsResult == null || factsResult.Objects == null || factsResult.Objects.Count == 0)
                throw new InvalidOperationException("No eval facts defined for this assistant. Create at least one fact before starting a run.");

            // Load assistant settings to resolve inference endpoint and judge prompt
            AssistantSettings settings = await _Database.AssistantSettings.ReadByAssistantIdAsync(assistantId, token).ConfigureAwait(false);
            if (settings == null)
                throw new InvalidOperationException("Assistant settings not found for assistant " + assistantId);

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
            run.TotalFacts = factsResult.Objects.Count;
            run.StartedUtc = DateTime.UtcNow;
            run.JudgePrompt = effectiveJudgePrompt != _DefaultJudgePrompt ? effectiveJudgePrompt : null;
            await _Database.EvalRun.CreateAsync(run, token).ConfigureAwait(false);

            _Logging.Info(_Header + "starting eval run " + run.Id + " for assistant " + assistantId + " with " + factsResult.Objects.Count + " facts");

            // Fire and forget
            _ = Task.Run(async () => await ExecuteRunAsync(run, factsResult.Objects, settings, effectiveJudgePrompt).ConfigureAwait(false));

            return run;
        }

        #endregion

        #region Private-Methods

        private async Task ExecuteRunAsync(EvalRun run, List<EvalFact> facts, AssistantSettings settings, string judgePrompt)
        {
            try
            {
                // Resolve inference endpoint (same logic as ChatHandler)
                InferenceProviderEnum provider = _Settings.Inference.Provider;
                string endpoint = _Settings.Inference.Endpoint;
                string apiKey = _Settings.Inference.ApiKey;

                if (!String.IsNullOrEmpty(settings.InferenceEndpointId))
                {
                    var resolved = await ResolveCompletionEndpointAsync(settings.InferenceEndpointId).ConfigureAwait(false);
                    if (resolved != null)
                    {
                        provider = resolved.Value.Provider;
                        endpoint = resolved.Value.Endpoint;
                        apiKey = resolved.Value.ApiKey;
                    }
                }

                string model = settings.Model;
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

                        // Send question through inference (non-streamed, inference-only)
                        List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>
                        {
                            new ChatCompletionMessage { Role = "system", Content = systemPrompt },
                            new ChatCompletionMessage { Role = "user", Content = fact.Question }
                        };

                        InferenceResult chatResult = await _Inference.GenerateResponseAsync(
                            messages, model, maxTokens, temperature, topP,
                            provider, endpoint, apiKey).ConfigureAwait(false);

                        string llmResponse = chatResult?.Content ?? String.Empty;

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

        private struct ResolvedEndpoint
        {
            public InferenceProviderEnum Provider;
            public string Endpoint;
            public string ApiKey;
        }

        private async Task<ResolvedEndpoint?> ResolveCompletionEndpointAsync(string endpointId)
        {
            try
            {
                string url = _Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/completion/" + endpointId;
                using (HttpClient client = new HttpClient())
                {
                    if (!String.IsNullOrEmpty(_Settings.Chunking.AccessKey))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _Settings.Chunking.AccessKey);
                    }

                    HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(_Header + "failed to resolve completion endpoint " + endpointId + ": " + (int)response.StatusCode);
                        return null;
                    }

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    JsonElement ep = JsonSerializer.Deserialize<JsonElement>(body);

                    string apiFormat = ep.TryGetProperty("ApiFormat", out JsonElement af) ? af.GetString() : null;
                    string epUrl = ep.TryGetProperty("Endpoint", out JsonElement eu) ? eu.GetString() : null;
                    string epApiKey = ep.TryGetProperty("ApiKey", out JsonElement ak) ? ak.GetString() : null;

                    InferenceProviderEnum provider = InferenceProviderEnum.Ollama;
                    if (!String.IsNullOrEmpty(apiFormat) && apiFormat.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
                        provider = InferenceProviderEnum.OpenAI;

                    return new ResolvedEndpoint
                    {
                        Provider = provider,
                        Endpoint = epUrl ?? _Settings.Inference.Endpoint,
                        ApiKey = epApiKey ?? _Settings.Inference.ApiKey
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
