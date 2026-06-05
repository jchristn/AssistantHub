namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    /// <summary>
    /// Provides shared non-streaming chat execution helpers.
    /// </summary>
    public abstract class AssistantChatServiceBase
    {
        private protected static readonly string _Header = "[AssistantChatService] ";

        private protected static readonly string _DefaultQueryRewritePrompt =
            "Evaluate the following prompt.\n" +
            "Return up to three variants of this prompt using different words or phrasing to maximize retrieval accuracy.\n" +
            "If you are unable to rewrite this prompt, respond ONLY with the original prompt.\n" +
            "Always include the original prompt in the response.\n" +
            "Return nothing other than a newline-separated list of prompts.\n\n" +
            "Example #1 (positive example)\n" +
            "User prompt:\n" +
            "\"How do I speed up my Postgres vector search? It's getting slow as my data grows.\"\n\n" +
            "LLM response:\n" +
            "\"How do I speed up my Postgres vector search? It's getting slow as my data grows.\"\n" +
            "\"How can I optimize pgvector similarity queries in Postgres (index type, HNSW/IVFFlat settings, query patterns, and maintenance like VACUUM/ANALYZE)?\"\n" +
            "\"Best practices for scaling Postgres vector retrieval with pgvector: schema design, indexing strategy, and query structure to reduce response time.\"\n" +
            "\"pgvector performance tuning checklist: choosing distance function, index parameters, batch sizes, and filters without killing recall or speed.\"\n\n" +
            "Example #2 (negative example)\n" +
            "User prompt:\n" +
            "\"Why is it doing that thing again?\"\n\n" +
            "LLM response:\n" +
            "\"Why is it doing that thing again?\"\n\n" +
            "The prompt to evaluate is: {prompt}";

        private protected static readonly string _RetrievalGatePrompt =
            "You are a retrieval classifier. Given a conversation and the user's latest message, " +
            "decide whether answering the latest message requires searching an external knowledge base " +
            "for new information, or whether the answer can be constructed entirely from the existing " +
            "conversation context.\n\n" +
            "Respond with exactly one word: RETRIEVE or SKIP\n\n" +
            "Rules:\n" +
            "- RETRIEVE: The user is asking about new topics, new entities, new data points, " +
            "or information not already present in the conversation.\n" +
            "- SKIP: The user is asking to reformat, reorder, summarize, compare, explain, " +
            "or otherwise manipulate information already provided in the conversation. " +
            "Also SKIP for greetings, meta-questions about the conversation, or clarifications " +
            "about previously retrieved content.\n\n" +
            "Conversation context (last few turns):\n{recentMessages}\n\n" +
            "Latest user message:\n{lastUserMessage}\n\n" +
            "Decision:";

        private protected static readonly string _DefaultRerankPrompt =
            "You are a relevance judge. Given a user query and a numbered list of text chunks retrieved from a document collection, score each chunk's relevance to answering the query.\n\n" +
            "Score each chunk from 0 to 10:\n" +
            "- 0: Completely irrelevant, no connection to the query\n" +
            "- 1-3: Tangentially related but does not help answer the query\n" +
            "- 4-6: Somewhat relevant, contains related information but may not directly answer\n" +
            "- 7-8: Highly relevant, directly addresses the query\n" +
            "- 9-10: Perfect match, contains the exact information needed to answer\n\n" +
            "Respond with ONLY a JSON array of objects, each with \"index\" (the chunk number) and \"score\" (your relevance rating).\n\n" +
            "Example response:\n" +
            "[{\"index\": 1, \"score\": 8}, {\"index\": 2, \"score\": 3}, {\"index\": 3, \"score\": 7}]\n\n" +
            "User query:\n{query}\n\n" +
            "Retrieved chunks:\n{chunks}";

        private protected static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private protected readonly DatabaseDriverBase _Database;
        private protected readonly LoggingModule _Logging;
        private protected readonly AssistantHubSettings _Settings;
        private protected readonly RetrievalService _Retrieval;
        private protected readonly InferenceService _Inference;

        private protected ChatMetadataFilter BuildEffectiveMetadataFilter(AssistantSettings settings, ChatMetadataFilter requestFilter)
        {
            ChatMetadataFilter assistantFilter = null;
            bool hasAssistantLabelFilter = !String.IsNullOrEmpty(settings.RetrievalLabelFilter);
            bool hasAssistantTagFilter = !String.IsNullOrEmpty(settings.RetrievalTagFilter);

            if (hasAssistantLabelFilter || hasAssistantTagFilter)
            {
                assistantFilter = new ChatMetadataFilter();
                if (hasAssistantLabelFilter)
                {
                    Dictionary<string, List<string>>? labelFilter = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(settings.RetrievalLabelFilter, _JsonOptions);
                    if (labelFilter != null)
                    {
                        labelFilter.TryGetValue("Required", out List<string>? reqLabels);
                        labelFilter.TryGetValue("Excluded", out List<string>? exclLabels);
                        assistantFilter.RequiredLabels = reqLabels;
                        assistantFilter.ExcludedLabels = exclLabels;
                    }
                }

                if (hasAssistantTagFilter)
                {
                    Dictionary<string, List<ChatTagCondition>>? tagFilter = JsonSerializer.Deserialize<Dictionary<string, List<ChatTagCondition>>>(settings.RetrievalTagFilter, _JsonOptions);
                    if (tagFilter != null)
                    {
                        tagFilter.TryGetValue("Required", out List<ChatTagCondition>? reqTags);
                        tagFilter.TryGetValue("Excluded", out List<ChatTagCondition>? exclTags);
                        assistantFilter.RequiredTags = reqTags;
                        assistantFilter.ExcludedTags = exclTags;
                    }
                }
            }

            if (assistantFilter != null && requestFilter != null)
            {
                ChatMetadataFilter merged = new ChatMetadataFilter
                {
                    RequiredLabels = assistantFilter.RequiredLabels != null ? new List<string>(assistantFilter.RequiredLabels) : null,
                    ExcludedLabels = assistantFilter.ExcludedLabels != null ? new List<string>(assistantFilter.ExcludedLabels) : null,
                    RequiredTags = assistantFilter.RequiredTags != null ? new List<ChatTagCondition>(assistantFilter.RequiredTags) : null,
                    ExcludedTags = assistantFilter.ExcludedTags != null ? new List<ChatTagCondition>(assistantFilter.ExcludedTags) : null
                };
                merged.Merge(requestFilter);
                return merged;
            }

            if (assistantFilter != null) return assistantFilter;
            if (requestFilter != null) return requestFilter;
            return null;
        }

        private protected string GetLastUserMessage(List<ChatCompletionMessage> messages)
        {
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (String.Equals(messages[i].Role, "user", StringComparison.OrdinalIgnoreCase))
                    return messages[i].Content;
            }

            return null;
        }

        private protected string BuildRetrievalGatePrompt(List<ChatCompletionMessage> messages, string lastUserMessage)
        {
            const int maxCharsPerMessage = 200;
            int recentCount = Math.Min(messages.Count, 6);
            int startIndex = messages.Count - recentCount;
            StringBuilder recentMessages = new StringBuilder();

            for (int i = startIndex; i < messages.Count; i++)
            {
                if (messages[i] == messages.Last() && String.Equals(messages[i].Role, "user", StringComparison.OrdinalIgnoreCase))
                    continue;

                string content = messages[i].Content ?? "";
                if (content.Length > maxCharsPerMessage)
                    content = content.Substring(0, maxCharsPerMessage) + "...";
                recentMessages.AppendLine(messages[i].Role + ": " + content);
            }

            return _RetrievalGatePrompt
                .Replace("{recentMessages}", recentMessages.ToString())
                .Replace("{lastUserMessage}", lastUserMessage);
        }

        private protected async Task<List<ChatCompletionMessage>> CompactIfNeeded(
            List<ChatCompletionMessage> messages,
            AssistantSettings settings,
            Enums.InferenceProviderEnum inferenceProvider,
            string model,
            string inferenceEndpoint,
            string inferenceApiKey,
            string inferenceEndpointId,
            int inferenceMaxConcurrentRequests,
            CancellationToken token)
        {
            int estimatedTokens = EstimateTokenCount(messages);
            int availableTokens = settings.ContextWindow - settings.MaxTokens;

            if (estimatedTokens <= availableTokens || messages.Count <= 3)
                return messages;

            try
            {
                ChatCompletionMessage systemMessage = null;
                List<ChatCompletionMessage> compactableMessages = new List<ChatCompletionMessage>();
                ChatCompletionMessage lastUserMessage = null;

                if (messages.Count > 0 && String.Equals(messages[0].Role, "system", StringComparison.OrdinalIgnoreCase))
                    systemMessage = messages[0];

                for (int i = messages.Count - 1; i >= 0; i--)
                {
                    if (String.Equals(messages[i].Role, "user", StringComparison.OrdinalIgnoreCase))
                    {
                        lastUserMessage = messages[i];
                        break;
                    }
                }

                int startIdx = systemMessage != null ? 1 : 0;
                for (int i = startIdx; i < messages.Count; i++)
                {
                    if (messages[i] == lastUserMessage) continue;
                    compactableMessages.Add(messages[i]);
                }

                if (compactableMessages.Count < 1)
                    return messages;

                StringBuilder conversationText = new StringBuilder();
                foreach (ChatCompletionMessage msg in compactableMessages)
                    conversationText.AppendLine(msg.Role + ": " + msg.Content);

                InferenceResult summaryResult = await GenerateWithCompletionEndpointLimitAsync(
                    new List<ChatCompletionMessage>
                    {
                        new ChatCompletionMessage
                        {
                            Role = "system",
                            Content = "You are a helpful assistant that summarizes conversations concisely."
                        },
                        new ChatCompletionMessage
                        {
                            Role = "user",
                            Content = "Summarize the following conversation preserving key facts, decisions, and context:\n\n" + conversationText
                        }
                    },
                    model,
                    1024,
                    0.3,
                    1.0,
                    inferenceProvider,
                    inferenceEndpoint,
                    inferenceApiKey,
                    inferenceEndpointId,
                    inferenceMaxConcurrentRequests,
                    token).ConfigureAwait(false);

                if (summaryResult == null || !summaryResult.Success || String.IsNullOrEmpty(summaryResult.Content))
                    return messages;

                List<ChatCompletionMessage> compactedMessages = new List<ChatCompletionMessage>();
                if (systemMessage != null)
                    compactedMessages.Add(systemMessage);

                compactedMessages.Add(new ChatCompletionMessage
                {
                    Role = "system",
                    Content = "[Conversation Summary]\n" + summaryResult.Content
                });

                if (lastUserMessage != null)
                    compactedMessages.Add(lastUserMessage);

                return compactedMessages;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "compaction failed: " + e.Message + ", proceeding with original messages");
                return messages;
            }
        }


        /// <summary>
        /// Instantiate the shared assistant chat helper base.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">Application settings.</param>
        /// <param name="retrieval">Retrieval service.</param>
        /// <param name="inference">Inference service.</param>
        protected AssistantChatServiceBase(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            RetrievalService retrieval,
            InferenceService inference)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval));
            _Inference = inference ?? throw new ArgumentNullException(nameof(inference));
        }

        private protected async Task<ChatHistory> WriteChatHistoryAsync(
            string tenantId,
            string threadId,
            string assistantId,
            string collectionId,
            DateTime userMessageUtc,
            string userMessage,
            DateTime? retrievalStartUtc,
            double retrievalDurationMs,
            string retrievalContext,
            DateTime? promptSentUtc,
            int promptTokens,
            double endpointResolutionDurationMs,
            double compactionDurationMs,
            double inferenceConnectionDurationMs,
            double timeToFirstTokenMs,
            double timeToLastTokenMs,
            string assistantResponse,
            int completionTokens,
            string retrievalGateDecision,
            double retrievalGateDurationMs,
            string queryRewriteResult,
            double queryRewriteDurationMs,
            double rerankDurationMs,
            int rerankInputCount,
            int rerankOutputCount,
            string metadataFilterJson,
            string origin,
            string traceId,
            string requestHistoryId,
            AssistantPerformanceStage finalInferenceTelemetry,
            AssistantPerformanceStage retrievalGateTelemetry,
            AssistantPerformanceStage queryRewriteTelemetry,
            AssistantPerformanceStage rerankTelemetry,
            int retrievalQueryCount,
            int retrievalChunksReturned,
            CancellationToken token)
        {
            try
            {
                ChatHistory history = new ChatHistory
                {
                    Id = IdGenerator.NewChatHistoryId(),
                    TenantId = tenantId,
                    ThreadId = threadId,
                    AssistantId = assistantId,
                    CollectionId = collectionId,
                    UserMessageUtc = userMessageUtc,
                    UserMessage = userMessage,
                    RetrievalStartUtc = retrievalStartUtc,
                    RetrievalDurationMs = retrievalDurationMs,
                    RetrievalGateDecision = retrievalGateDecision,
                    RetrievalGateDurationMs = retrievalGateDurationMs,
                    QueryRewriteResult = queryRewriteResult,
                    QueryRewriteDurationMs = queryRewriteDurationMs,
                    RerankDurationMs = rerankDurationMs,
                    RerankInputCount = rerankInputCount,
                    RerankOutputCount = rerankOutputCount,
                    RetrievalContext = retrievalContext,
                    PromptSentUtc = promptSentUtc,
                    PromptTokens = promptTokens,
                    EndpointResolutionDurationMs = endpointResolutionDurationMs,
                    CompactionDurationMs = compactionDurationMs,
                    InferenceConnectionDurationMs = inferenceConnectionDurationMs,
                    TimeToFirstTokenMs = timeToFirstTokenMs,
                    TimeToLastTokenMs = timeToLastTokenMs,
                    MetadataFilter = metadataFilterJson,
                    AssistantResponse = assistantResponse,
                    CompletionTokens = completionTokens,
                    Origin = origin,
                    TraceId = traceId,
                    RequestHistoryId = requestHistoryId,
                    PerformanceSchemaVersion = 1
                };

                if (completionTokens > 0 && timeToLastTokenMs > 0)
                    history.TokensPerSecondOverall = Math.Round(completionTokens / (timeToLastTokenMs / 1000.0), 2);

                double generationMs = timeToLastTokenMs - timeToFirstTokenMs;
                if (completionTokens > 0 && generationMs > 0)
                    history.TokensPerSecondGeneration = Math.Round(completionTokens / (generationMs / 1000.0), 2);

                AssistantPerformanceTelemetry telemetry = AssistantPerformanceTelemetryBuilder.Build(
                    history,
                    finalInferenceTelemetry,
                    retrievalQueryCount,
                    retrievalChunksReturned,
                    retrievalGateTelemetry,
                    queryRewriteTelemetry,
                    rerankTelemetry);
                history.PerformanceJson = AssistantPerformanceTelemetryBuilder.Serialize(telemetry);

                await _Database.ChatHistory.CreateAsync(history, token).ConfigureAwait(false);

                if (_Database.ChatHistoryPerformanceEvent != null)
                {
                    List<ChatHistoryPerformanceEvent> events =
                        AssistantPerformanceTelemetryBuilder.ToEvents(telemetry, history.TenantId);
                    if (events.Count > 0)
                    {
                        await _Database.ChatHistoryPerformanceEvent.CreateManyAsync(events, token).ConfigureAwait(false);
                        _Logging.Debug(_Header + "persisted " + events.Count + " performance event row(s) for chat history " + history.Id);
                    }
                    else
                    {
                        _Logging.Warn(_Header + "no performance event rows generated for chat history " + history.Id);
                    }
                }

                return history;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "failed to write chat history: " + e.Message);
                return null;
            }
        }

        private protected async Task<ResolvedEndpoint?> ResolveCompletionEndpointAsync(string endpointId, CancellationToken token)
        {
            if (String.IsNullOrEmpty(endpointId)) return null;

            try
            {
                string url = _Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/completion/" + endpointId;
                using (HttpClient client = new HttpClient())
                {
                    if (!String.IsNullOrEmpty(_Settings.Chunking.AccessKey))
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _Settings.Chunking.AccessKey);

                    HttpResponseMessage response = await client.GetAsync(url, token).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(_Header + "failed to resolve completion endpoint " + endpointId + ": " + (int)response.StatusCode);
                        return null;
                    }

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    PartioEndpointConfig ep = JsonSerializer.Deserialize<PartioEndpointConfig>(body, _JsonOptions);

                    Enums.InferenceProviderEnum provider = InferenceProviderHelper.FromApiFormat(ep?.ApiFormat, Enums.InferenceProviderEnum.Ollama);

                    return new ResolvedEndpoint
                    {
                        EndpointId = endpointId,
                        Provider = provider,
                        Endpoint = ep?.Endpoint ?? _Settings.Inference.Endpoint,
                        ApiKey = ep?.ApiKey ?? _Settings.Inference.ApiKey,
                        Model = ep?.Model,
                        MaxConcurrentRequests = Math.Max(1, ep?.MaxConcurrentRequests ?? 1)
                    };
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception resolving completion endpoint " + endpointId + ": " + e.Message);
                return null;
            }
        }

        private protected ResolvedEndpoint BuildFallbackCompletionEndpoint(string endpointId)
        {
            return new ResolvedEndpoint
            {
                EndpointId = endpointId,
                Provider = _Settings.Inference.Provider,
                Endpoint = _Settings.Inference.Endpoint,
                ApiKey = _Settings.Inference.ApiKey,
                Model = _Settings.Inference.DefaultModel,
                MaxConcurrentRequests = 1
            };
        }

        private protected async Task<ResolvedEndpoint> ResolveCompletionEndpointOrFallbackAsync(string endpointId, CancellationToken token)
        {
            ResolvedEndpoint? resolved = await ResolveCompletionEndpointAsync(endpointId, token).ConfigureAwait(false);
            return resolved ?? BuildFallbackCompletionEndpoint(endpointId);
        }

        private protected async Task<InferenceResult> GenerateWithCompletionEndpointLimitAsync(
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            Enums.InferenceProviderEnum provider,
            string endpoint,
            string apiKey,
            string endpointId,
            int maxConcurrentRequests,
            CancellationToken token)
        {
            int max = Math.Max(1, maxConcurrentRequests);
            Stopwatch waitSw = Stopwatch.StartNew();
            using (IDisposable lease = await EndpointConcurrencyLimiter.AcquireAsync("completion", endpointId, max, token).ConfigureAwait(false))
            {
                waitSw.Stop();
                if (waitSw.ElapsedMilliseconds > 0)
                {
                    _Logging.Info(
                        _Header +
                        "completion endpoint concurrency slot acquired: " +
                        EndpointConcurrencyLimiter.BuildKey("completion", endpointId) +
                        ", maxConcurrentRequests=" + max +
                        ", waitedMs=" + waitSw.ElapsedMilliseconds);
                }

                InferenceResult result = await _Inference.GenerateResponseAsync(
                    messages, model, maxTokens, temperature, topP,
                    provider, endpoint, apiKey, token).ConfigureAwait(false);

                AttachEndpointTelemetry(result?.Telemetry, endpointId, endpoint, provider, model, max, waitSw.Elapsed.TotalMilliseconds);
                return result;
            }
        }

        private protected static void AttachEndpointTelemetry(
            AssistantPerformanceStage telemetry,
            string endpointId,
            string endpoint,
            Enums.InferenceProviderEnum provider,
            string model,
            int maxConcurrentRequests,
            double waitMs)
        {
            if (telemetry == null) return;

            telemetry.EndpointId = endpointId;
            telemetry.EndpointName ??= endpoint;
            telemetry.EndpointType ??= "completion";
            telemetry.Provider ??= provider.ToString();
            telemetry.ApiFormat ??= provider.ToString();
            telemetry.Model ??= model;
            telemetry.ClientTimings ??= new AssistantPerformanceClientTimings();
            telemetry.ClientTimings.EndpointLimiterWaitMs = Math.Round(Math.Max(0, waitMs), 2);
            telemetry.Metadata ??= new Dictionary<string, object>();
            telemetry.Metadata["max_concurrent_requests"] = Math.Max(1, maxConcurrentRequests);
        }

        private protected void TrimRetrievalContextToPromptBudget(
            List<ChatCompletionMessage> messages,
            AssistantSettings settings,
            int maxTokens,
            List<RetrievalChunk> retrievalChunks,
            List<string> chunkLabels,
            List<CitationSource> citationSources,
            string baseSystemPrompt,
            int systemMessageIndex)
        {
            if (messages == null || settings == null || retrievalChunks == null || retrievalChunks.Count < 1)
                return;
            if (String.IsNullOrEmpty(baseSystemPrompt) || systemMessageIndex < 0 || systemMessageIndex >= messages.Count)
                return;

            int availablePromptTokens = settings.ContextWindow - maxTokens;
            if (availablePromptTokens <= 0)
                return;

            int estimatedTokens = EstimateTokenCount(messages);
            if (estimatedTokens <= availablePromptTokens)
                return;

            int originalChunkCount = retrievalChunks.Count;

            while (retrievalChunks.Count > 0 && estimatedTokens > availablePromptTokens)
            {
                retrievalChunks.RemoveAt(retrievalChunks.Count - 1);

                if (chunkLabels != null && chunkLabels.Count > retrievalChunks.Count)
                    chunkLabels.RemoveAt(chunkLabels.Count - 1);

                if (citationSources != null && citationSources.Count > retrievalChunks.Count)
                    citationSources.RemoveAt(citationSources.Count - 1);

                messages[systemMessageIndex] = new ChatCompletionMessage
                {
                    Role = "system",
                    Content = _Inference.BuildSystemMessage(
                        baseSystemPrompt,
                        retrievalChunks.Select(c => c.MergedContent).ToList(),
                        settings.EnableCitations,
                        chunkLabels)
                };

                estimatedTokens = EstimateTokenCount(messages);
            }

            if (retrievalChunks.Count < originalChunkCount)
            {
                _Logging.Warn(
                    _Header +
                    "trimmed retrieval context from " + originalChunkCount +
                    " to " + retrievalChunks.Count +
                    " chunks to fit prompt budget (" + estimatedTokens +
                    "/" + availablePromptTokens + " estimated tokens)");
            }
        }

        private protected static int EstimateTokenCount(string text)
        {
            if (String.IsNullOrEmpty(text)) return 0;
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        private protected static string ResolveUtilityInferenceEndpointId(string utilityEndpointId, string fallbackEndpointId)
        {
            return !String.IsNullOrWhiteSpace(utilityEndpointId) ? utilityEndpointId : fallbackEndpointId;
        }

        private protected static int EstimateTokenCount(List<ChatCompletionMessage> messages)
        {
            if (messages == null) return 0;
            int total = 0;
            foreach (ChatCompletionMessage msg in messages)
            {
                total += 4;
                total += EstimateTokenCount(msg.Content);
            }

            return total;
        }
    }
}
