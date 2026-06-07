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
    /// Shared non-streaming assistant chat execution service.
    /// </summary>
    public class AssistantChatService : AssistantChatServiceBase
    {
        /// <summary>
        /// Instantiate the shared assistant chat execution service.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">Application settings.</param>
        /// <param name="retrieval">Retrieval service.</param>
        /// <param name="inference">Inference service.</param>
        public AssistantChatService(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            RetrievalService retrieval,
            InferenceService inference)
            : base(database, logging, settings, retrieval, inference)
        {
        }

        /// <summary>
        /// Execute a non-streaming chat completion using the shared AssistantHub chat rail.
        /// </summary>
        /// <param name="request">Execution request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Execution result.</returns>
        public async Task<AssistantChatExecutionResult> ExecuteNonStreamingAsync(AssistantChatExecutionRequest request, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (String.IsNullOrEmpty(request.AssistantId)) throw new ArgumentNullException(nameof(request.AssistantId));
            if (request.Messages == null || request.Messages.Count < 1) throw new ArgumentNullException(nameof(request.Messages));

            Assistant assistant = request.Assistant ?? await _Database.Assistant.ReadAsync(request.AssistantId, token).ConfigureAwait(false);
            if (assistant == null || !assistant.Active)
                return new AssistantChatExecutionResult { Success = false, ErrorMessage = "Assistant not found." };

            AssistantSettings settings = request.AssistantSettings ?? await _Database.AssistantSettings.ReadByAssistantIdAsync(request.AssistantId, token).ConfigureAwait(false);
            if (settings == null)
                return new AssistantChatExecutionResult { Success = false, ErrorMessage = "Assistant settings not configured." };
            if (String.IsNullOrWhiteSpace(settings.InferenceEndpointId))
                return new AssistantChatExecutionResult { Success = false, ErrorMessage = "Assistant inference endpoint not configured." };

            ChatMetadataFilter effectiveMetadataFilter = BuildEffectiveMetadataFilter(settings, request.MetadataFilter);
            string metadataFilterJson = null;
            if (effectiveMetadataFilter != null && !effectiveMetadataFilter.IsEmpty)
            {
                metadataFilterJson = JsonSerializer.Serialize(effectiveMetadataFilter, _JsonOptions);
                _Logging.Info(_Header + "effective metadata filter: " + metadataFilterJson);
            }

            DateTime userMessageUtc = request.UserMessageUtc ?? DateTime.UtcNow;
            string lastUserMessage = GetLastUserMessage(request.Messages);

            string retrievalGateDecision = null;
            double retrievalGateDurationMs = 0;
            AssistantPerformanceStage retrievalGateTelemetry = null;
            bool shouldRetrieve = true;

            if (settings.EnableRag && settings.EnableRetrievalGate
                && !String.IsNullOrEmpty(settings.CollectionId) && !String.IsNullOrEmpty(lastUserMessage))
            {
                int userMessageCount = request.Messages.Count(m =>
                    String.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase));

                if (userMessageCount > 1)
                {
                    string gatePrompt = BuildRetrievalGatePrompt(request.Messages, lastUserMessage);
                    string gateEndpointId = ResolveUtilityInferenceEndpointId(settings.RetrievalGateInferenceEndpointId, settings.InferenceEndpointId);
                    ResolvedEndpoint gateEndpoint = await ResolveCompletionEndpointOrFallbackAsync(gateEndpointId, token).ConfigureAwait(false);
                    string gateModel = !String.IsNullOrEmpty(gateEndpoint.Model) ? gateEndpoint.Model : _Settings.Inference.DefaultModel;

                    Stopwatch gateSw = Stopwatch.StartNew();
                    try
                    {
                        InferenceResult gateResult = await GenerateWithCompletionEndpointLimitAsync(
                            new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "system", Content = gatePrompt } },
                            gateModel,
                            3,
                            0.0,
                            1.0,
                            gateEndpoint.Provider,
                            gateEndpoint.Endpoint,
                            gateEndpoint.ApiKey,
                            gateEndpoint.EndpointId,
                            gateEndpoint.MaxConcurrentRequests,
                            token).ConfigureAwait(false);
                        retrievalGateTelemetry = gateResult?.Telemetry;

                        gateSw.Stop();
                        retrievalGateDurationMs = Math.Round(gateSw.Elapsed.TotalMilliseconds, 2);

                        if (gateResult != null && gateResult.Success && !String.IsNullOrEmpty(gateResult.Content))
                        {
                            string decision = gateResult.Content.Trim().ToUpperInvariant();
                            if (decision.Contains("SKIP"))
                            {
                                retrievalGateDecision = "SKIP";
                                shouldRetrieve = false;
                            }
                            else
                            {
                                retrievalGateDecision = "RETRIEVE";
                            }
                        }
                        else
                        {
                            retrievalGateDecision = "RETRIEVE";
                        }
                    }
                    catch (Exception gateEx)
                    {
                        gateSw.Stop();
                        retrievalGateDurationMs = Math.Round(gateSw.Elapsed.TotalMilliseconds, 2);
                        retrievalGateDecision = "RETRIEVE";
                        _Logging.Warn(_Header + "retrieval gate failed, defaulting to RETRIEVE: " + gateEx.Message);
                    }
                }
            }

            string queryRewriteResult = null;
            double queryRewriteDurationMs = 0;
            AssistantPerformanceStage queryRewriteTelemetry = null;
            List<string> retrievalQueries = !String.IsNullOrEmpty(lastUserMessage) ? new List<string> { lastUserMessage } : new List<string>();

            if (settings.EnableRag && settings.EnableQueryRewrite && shouldRetrieve
                && !String.IsNullOrEmpty(settings.CollectionId) && !String.IsNullOrEmpty(lastUserMessage))
            {
                string rewriteEndpointId = ResolveUtilityInferenceEndpointId(settings.QueryRewriteInferenceEndpointId, settings.InferenceEndpointId);
                ResolvedEndpoint rewriteEndpoint = await ResolveCompletionEndpointOrFallbackAsync(rewriteEndpointId, token).ConfigureAwait(false);
                string rewriteModel = !String.IsNullOrEmpty(rewriteEndpoint.Model) ? rewriteEndpoint.Model : _Settings.Inference.DefaultModel;
                string rewritePromptTemplate = !String.IsNullOrEmpty(settings.QueryRewritePrompt)
                    ? settings.QueryRewritePrompt
                    : _DefaultQueryRewritePrompt;

                string rewritePrompt = rewritePromptTemplate.Replace("{prompt}", lastUserMessage);
                Stopwatch rewriteSw = Stopwatch.StartNew();

                try
                {
                    InferenceResult rewriteResult = await GenerateWithCompletionEndpointLimitAsync(
                        new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "system", Content = rewritePrompt } },
                        rewriteModel,
                        512,
                        0.7,
                        1.0,
                        rewriteEndpoint.Provider,
                        rewriteEndpoint.Endpoint,
                        rewriteEndpoint.ApiKey,
                        rewriteEndpoint.EndpointId,
                        rewriteEndpoint.MaxConcurrentRequests,
                        token).ConfigureAwait(false);
                    queryRewriteTelemetry = rewriteResult?.Telemetry;

                    rewriteSw.Stop();
                    queryRewriteDurationMs = Math.Round(rewriteSw.Elapsed.TotalMilliseconds, 2);

                    if (rewriteResult != null && rewriteResult.Success && !String.IsNullOrEmpty(rewriteResult.Content))
                    {
                        queryRewriteResult = rewriteResult.Content.Trim();
                        List<string> rewrittenQueries = rewriteResult.Content
                            .Split('\n')
                            .Select(q => q.Trim().Trim('"'))
                            .Where(q => !String.IsNullOrWhiteSpace(q))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        if (rewrittenQueries.Count > 0)
                            retrievalQueries = rewrittenQueries;
                    }
                }
                catch (Exception rewriteEx)
                {
                    rewriteSw.Stop();
                    queryRewriteDurationMs = Math.Round(rewriteSw.Elapsed.TotalMilliseconds, 2);
                    _Logging.Warn(_Header + "query rewrite failed, using original query: " + rewriteEx.Message);
                }
            }

            List<RetrievalChunk> retrievalChunks = new List<RetrievalChunk>();
            DateTime? retrievalStartUtc = null;
            double retrievalDurationMs = 0;

            if (settings.EnableRag && !String.IsNullOrEmpty(settings.CollectionId) && !String.IsNullOrEmpty(lastUserMessage) && shouldRetrieve)
            {
                retrievalStartUtc = DateTime.UtcNow;
                Stopwatch retrievalSw = Stopwatch.StartNew();

                RetrievalSearchOptions searchOptions = new RetrievalSearchOptions
                {
                    SearchMode = settings.SearchMode,
                    TextWeight = settings.TextWeight,
                    FullTextSearchType = settings.FullTextSearchType,
                    FullTextLanguage = settings.FullTextLanguage,
                    FullTextNormalization = settings.FullTextNormalization,
                    FullTextMinimumScore = settings.FullTextMinimumScore,
                    IncludeNeighbors = settings.RetrievalIncludeNeighbors,
                    MetadataFilter = effectiveMetadataFilter
                };

                if (retrievalQueries.Count > 1)
                {
                    const double rrfK = 60.0;
                    Dictionary<string, double> rrfScores = new Dictionary<string, double>();
                    Dictionary<string, RetrievalChunk> chunkMap = new Dictionary<string, RetrievalChunk>();

                    foreach (string query in retrievalQueries)
                    {
                        List<RetrievalChunk> retrieved = await _Retrieval.RetrieveAsync(
                            assistant.TenantId,
                            settings.CollectionId,
                            query,
                            settings.RetrievalTopK,
                            settings.RetrievalScoreThreshold,
                            default,
                            settings.EmbeddingEndpointId,
                            searchOptions).ConfigureAwait(false);

                        if (retrieved == null) continue;

                        for (int rank = 0; rank < retrieved.Count; rank++)
                        {
                            string dedupeKey = (retrieved[rank].DocumentId ?? "") + ":" + retrieved[rank].Position;
                            double rrfContribution = 1.0 / (rrfK + rank + 1);

                            if (!rrfScores.ContainsKey(dedupeKey))
                            {
                                rrfScores[dedupeKey] = 0;
                                chunkMap[dedupeKey] = retrieved[rank];
                            }
                            else if (retrieved[rank].Score > chunkMap[dedupeKey].Score)
                            {
                                chunkMap[dedupeKey] = retrieved[rank];
                            }

                            rrfScores[dedupeKey] += rrfContribution;
                            chunkMap[dedupeKey].FusionScore = rrfScores[dedupeKey];
                        }
                    }

                    retrievalChunks = chunkMap.Values
                        .OrderByDescending(c => c.FusionScore)
                        .Take(settings.RetrievalTopK)
                        .ToList();
                }
                else
                {
                    HashSet<string> seenChunks = new HashSet<string>();

                    foreach (string query in retrievalQueries)
                    {
                        List<RetrievalChunk> retrieved = await _Retrieval.RetrieveAsync(
                            assistant.TenantId,
                            settings.CollectionId,
                            query,
                            settings.RetrievalTopK,
                            settings.RetrievalScoreThreshold,
                            default,
                            settings.EmbeddingEndpointId,
                            searchOptions).ConfigureAwait(false);

                        if (retrieved == null) continue;

                        foreach (RetrievalChunk chunk in retrieved)
                        {
                            string dedupeKey = (chunk.DocumentId ?? "") + ":" + chunk.Position;
                            if (seenChunks.Add(dedupeKey))
                                retrievalChunks.Add(chunk);
                        }
                    }

                    retrievalChunks = retrievalChunks
                        .OrderByDescending(c => c.Score)
                        .Take(settings.RetrievalTopK)
                        .ToList();
                }

                retrievalSw.Stop();
                retrievalDurationMs = Math.Round(retrievalSw.Elapsed.TotalMilliseconds, 2);
            }

            double rerankDurationMs = 0;
            int rerankInputCount = 0;
            int rerankOutputCount = 0;
            AssistantPerformanceStage rerankTelemetry = null;

            if (settings.EnableRag && settings.EnableReranking && shouldRetrieve && retrievalChunks.Count > 0)
            {
                rerankInputCount = retrievalChunks.Count;
                Stopwatch rerankSw = Stopwatch.StartNew();

                try
                {
                    string rerankEndpointId = ResolveUtilityInferenceEndpointId(settings.RerankInferenceEndpointId, settings.InferenceEndpointId);
                    ResolvedEndpoint rerankEndpoint = await ResolveCompletionEndpointOrFallbackAsync(rerankEndpointId, token).ConfigureAwait(false);
                    string rerankModel = !String.IsNullOrEmpty(rerankEndpoint.Model) ? rerankEndpoint.Model : _Settings.Inference.DefaultModel;
                    string rerankPromptTemplate = !String.IsNullOrEmpty(settings.RerankPrompt)
                        ? settings.RerankPrompt
                        : _DefaultRerankPrompt;

                    StringBuilder chunksBuilder = new StringBuilder();
                    for (int i = 0; i < retrievalChunks.Count; i++)
                    {
                        string chunkText = retrievalChunks[i].Content ?? "";
                        if (chunkText.Length > 500) chunkText = chunkText.Substring(0, 500);
                        chunksBuilder.AppendLine("[" + (i + 1) + "] " + chunkText);
                    }

                    string rerankPrompt = rerankPromptTemplate
                        .Replace("{query}", lastUserMessage)
                        .Replace("{chunks}", chunksBuilder.ToString());

                    InferenceResult rerankResult = await GenerateWithCompletionEndpointLimitAsync(
                        new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "system", Content = rerankPrompt } },
                        rerankModel,
                        512,
                        0.0,
                        1.0,
                        rerankEndpoint.Provider,
                        rerankEndpoint.Endpoint,
                        rerankEndpoint.ApiKey,
                        rerankEndpoint.EndpointId,
                        rerankEndpoint.MaxConcurrentRequests,
                        token).ConfigureAwait(false);
                    rerankTelemetry = rerankResult?.Telemetry;

                    if (rerankResult != null && rerankResult.Success && !String.IsNullOrEmpty(rerankResult.Content))
                    {
                        string rerankContent = rerankResult.Content.Trim();
                        if (rerankContent.StartsWith("```json")) rerankContent = rerankContent.Substring(7);
                        else if (rerankContent.StartsWith("```")) rerankContent = rerankContent.Substring(3);
                        if (rerankContent.EndsWith("```")) rerankContent = rerankContent.Substring(0, rerankContent.Length - 3);
                        rerankContent = rerankContent.Trim();

                        int firstBracket = rerankContent.IndexOf('[');
                        int lastBracket = rerankContent.LastIndexOf(']');
                        if (firstBracket >= 0 && lastBracket > firstBracket)
                            rerankContent = rerankContent.Substring(firstBracket, lastBracket - firstBracket + 1);

                        List<RerankResult> scores = JsonSerializer.Deserialize<List<RerankResult>>(rerankContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (scores != null)
                        {
                            foreach (RerankResult score in scores)
                            {
                                int idx = score.Index - 1;
                                if (idx >= 0 && idx < retrievalChunks.Count)
                                    retrievalChunks[idx].RerankScore = score.Score;
                            }

                            retrievalChunks = retrievalChunks
                                .Where(c => c.RerankScore.HasValue && c.RerankScore.Value >= settings.RerankerScoreThreshold)
                                .OrderByDescending(c => c.RerankScore!.Value)
                                .Take(settings.RerankerTopK)
                                .ToList();
                        }
                    }
                }
                catch (Exception rerankEx)
                {
                    _Logging.Warn(_Header + "re-ranking failed, using original retrieval ordering: " + rerankEx.Message);
                }

                rerankSw.Stop();
                rerankDurationMs = Math.Round(rerankSw.Elapsed.TotalMilliseconds, 2);
                rerankOutputCount = retrievalChunks.Count;
            }

            List<string> contextChunks = retrievalChunks.Select(c => c.MergedContent).ToList();
            List<string> chunkLabels = null;
            List<CitationSource> citationSources = null;

            if (settings.EnableCitations && settings.EnableRag && retrievalChunks.Count > 0)
            {
                chunkLabels = new List<string>();
                citationSources = new List<CitationSource>();
                int citationIndex = 1;

                foreach (RetrievalChunk chunk in retrievalChunks)
                {
                    string docName = "Unknown Document";
                    string contentType = null;
                    AssistantDocument doc = null;

                    if (!String.IsNullOrEmpty(chunk.DocumentId))
                    {
                        doc = await _Database.AssistantDocument.ReadAsync(chunk.DocumentId, token).ConfigureAwait(false);
                        if (doc != null)
                        {
                            docName = doc.Name ?? doc.OriginalFilename ?? "Unknown Document";
                            contentType = doc.ContentType;
                        }
                    }

                    chunkLabels.Add("(Source: \"" + docName + "\")");

                    string downloadUrl = null;
                    if (!String.IsNullOrEmpty(chunk.DocumentId))
                    {
                        if (String.Equals(settings.CitationLinkMode, "Authenticated", StringComparison.OrdinalIgnoreCase))
                            downloadUrl = "/v1.0/documents/" + chunk.DocumentId + "/download";
                        else if (String.Equals(settings.CitationLinkMode, "Public", StringComparison.OrdinalIgnoreCase))
                            downloadUrl = "/v1.0/assistants/" + assistant.Id + "/documents/" + chunk.DocumentId + "/download";
                    }

                    citationSources.Add(new CitationSource
                    {
                        Index = citationIndex++,
                        DocumentId = chunk.DocumentId,
                        DocumentName = docName,
                        ContentType = contentType,
                        Score = chunk.Score,
                        FusionScore = chunk.FusionScore,
                        RerankScore = chunk.RerankScore,
                        Excerpt = chunk.Content?.Length > 200 ? chunk.Content.Substring(0, 200) + "..." : chunk.Content,
                        DownloadUrl = downloadUrl
                    });
                }
            }

            List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>(request.Messages);
            string baseSystemPrompt = null;
            int systemMessageIndex = -1;
            bool hasSystemMessage = messages.Any(m =>
                String.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase)
                && !IsConversationSummaryMessage(m));
            if (!hasSystemMessage && !String.IsNullOrEmpty(settings.SystemPrompt))
            {
                baseSystemPrompt = settings.SystemPrompt;
                messages.Insert(0, new ChatCompletionMessage
                {
                    Role = "system",
                    Content = _Inference.BuildSystemMessage(settings.SystemPrompt, contextChunks, settings.EnableCitations, chunkLabels)
                });
                systemMessageIndex = 0;
            }
            else if (hasSystemMessage && contextChunks.Count > 0)
            {
                for (int i = 0; i < messages.Count; i++)
                {
                    if (String.Equals(messages[i].Role, "system", StringComparison.OrdinalIgnoreCase)
                        && !IsConversationSummaryMessage(messages[i]))
                    {
                        baseSystemPrompt = messages[i].Content;
                        messages[i] = new ChatCompletionMessage
                        {
                            Role = "system",
                            Content = _Inference.BuildSystemMessage(messages[i].Content, contextChunks, settings.EnableCitations, chunkLabels)
                        };
                        systemMessageIndex = i;
                        break;
                    }
                }
            }

            string model = !String.IsNullOrEmpty(request.Model) ? request.Model : _Settings.Inference.DefaultModel;
            double temperature = request.Temperature ?? settings.Temperature;
            double topP = request.TopP ?? settings.TopP;
            int maxTokens = request.MaxTokens ?? settings.MaxTokens;

            TrimRetrievalContextToPromptBudget(
                messages,
                settings,
                maxTokens,
                retrievalChunks,
                chunkLabels,
                citationSources,
                baseSystemPrompt,
                systemMessageIndex);

            Enums.InferenceProviderEnum inferenceProvider = _Settings.Inference.Provider;
            string inferenceEndpoint = _Settings.Inference.Endpoint;
            string inferenceApiKey = _Settings.Inference.ApiKey;
            string inferenceEndpointId = settings.InferenceEndpointId;
            int inferenceMaxConcurrentRequests = 1;

            double endpointResolutionMs = 0;
            if (!String.IsNullOrEmpty(settings.InferenceEndpointId))
            {
                Stopwatch endpointSw = Stopwatch.StartNew();
                ResolvedEndpoint? resolved = await ResolveCompletionEndpointAsync(settings.InferenceEndpointId, token).ConfigureAwait(false);
                endpointSw.Stop();
                endpointResolutionMs = Math.Round(endpointSw.Elapsed.TotalMilliseconds, 2);
                if (resolved != null)
                {
                    inferenceProvider = resolved.Value.Provider;
                    inferenceEndpoint = resolved.Value.Endpoint;
                    inferenceApiKey = resolved.Value.ApiKey;
                    inferenceEndpointId = resolved.Value.EndpointId;
                    inferenceMaxConcurrentRequests = resolved.Value.MaxConcurrentRequests;
                    if (String.IsNullOrEmpty(request.Model) && !String.IsNullOrEmpty(resolved.Value.Model))
                        model = resolved.Value.Model;
                }
            }

            Stopwatch compactionSw = Stopwatch.StartNew();
            messages = await CompactIfNeeded(
                messages,
                settings,
                inferenceProvider,
                model,
                inferenceEndpoint,
                inferenceApiKey,
                inferenceEndpointId,
                inferenceMaxConcurrentRequests,
                token).ConfigureAwait(false);
            compactionSw.Stop();
            double compactionMs = Math.Round(compactionSw.Elapsed.TotalMilliseconds, 2);

            int promptTokenEstimate = EstimateTokenCount(messages);
            DateTime promptSentUtc = DateTime.UtcNow;
            Stopwatch inferenceSw = Stopwatch.StartNew();

            InferenceResult inferenceResult = await GenerateWithCompletionEndpointLimitAsync(
                messages, model, maxTokens, temperature, topP,
                inferenceProvider, inferenceEndpoint, inferenceApiKey,
                inferenceEndpointId, inferenceMaxConcurrentRequests, token).ConfigureAwait(false);

            inferenceSw.Stop();
            double timeToLastTokenMs = Math.Round(inferenceSw.Elapsed.TotalMilliseconds, 2);

            if (inferenceResult == null || !inferenceResult.Success || String.IsNullOrEmpty(inferenceResult.Content))
            {
                return new AssistantChatExecutionResult
                {
                    Success = false,
                    ErrorMessage = inferenceResult?.ErrorMessage ?? "Inference failed."
                };
            }

            string canonicalResponseText = (settings.EnableCitations && citationSources != null && citationSources.Count > 0)
                ? CitationExtractor.StripBibliography(inferenceResult.Content)
                : inferenceResult.Content;

            int responsePromptTokens = EstimateTokenCount(messages);
            int completionTokens = EstimateTokenCount(canonicalResponseText);

            ChatCompletionResponse response = new ChatCompletionResponse
            {
                Id = IdGenerator.NewChatCompletionId(),
                Object = "chat.completion",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = model,
                Choices = new List<ChatCompletionChoice>
                {
                    new ChatCompletionChoice
                    {
                        Index = 0,
                        Message = new ChatCompletionMessage { Role = "assistant", Content = canonicalResponseText },
                        FinishReason = "stop"
                    }
                },
                Usage = new ChatCompletionUsage
                {
                    PromptTokens = responsePromptTokens,
                    CompletionTokens = completionTokens,
                    TotalTokens = responsePromptTokens + completionTokens,
                    ContextWindow = settings.ContextWindow
                },
                Retrieval = settings.EnableRag ? new ChatCompletionRetrieval
                {
                    CollectionId = settings.CollectionId,
                    DurationMs = retrievalDurationMs,
                    ChunksReturned = retrievalChunks.Count,
                    Chunks = retrievalChunks,
                    RerankDurationMs = rerankDurationMs,
                    RerankInputCount = rerankInputCount,
                    RerankOutputCount = rerankOutputCount
                } : null,
                Citations = (settings.EnableCitations && citationSources != null && citationSources.Count > 0)
                    ? CitationExtractor.Extract(citationSources, canonicalResponseText)
                    : null
            };

            string persistedChatHistoryId = null;
            if (!String.IsNullOrEmpty(request.ThreadId))
            {
                ChatHistory history = await WriteChatHistoryAsync(
                    assistant.TenantId,
                    request.ThreadId,
                    assistant.Id,
                    settings.CollectionId,
                    userMessageUtc,
                    lastUserMessage,
                    retrievalStartUtc,
                    retrievalDurationMs,
                    retrievalChunks.Count > 0 ? Serializer.SerializeJson(retrievalChunks, true) : null,
                    promptSentUtc,
                    promptTokenEstimate,
                    endpointResolutionMs,
                    compactionMs,
                    0,
                    timeToLastTokenMs,
                    timeToLastTokenMs,
                    canonicalResponseText,
                    completionTokens,
                    retrievalGateDecision,
                    retrievalGateDurationMs,
                    queryRewriteResult,
                    queryRewriteDurationMs,
                    rerankDurationMs,
                    rerankInputCount,
                    rerankOutputCount,
                    metadataFilterJson,
                    request.Origin,
                    request.TraceId,
                    request.RequestHistoryId,
                    inferenceResult.Telemetry,
                    retrievalGateTelemetry,
                    queryRewriteTelemetry,
                    rerankTelemetry,
                    retrievalQueries.Count,
                    retrievalChunks.Count,
                    token);

                if (history != null)
                {
                    persistedChatHistoryId = history.Id;
                    request.ChatHistoryPersisted?.Invoke(history.Id);
                }
            }

            return new AssistantChatExecutionResult
            {
                Success = true,
                Assistant = assistant,
                AssistantSettings = settings,
                Response = response,
                CanonicalResponseText = canonicalResponseText,
                ChatHistoryId = persistedChatHistoryId
            };
        }

    }
}
