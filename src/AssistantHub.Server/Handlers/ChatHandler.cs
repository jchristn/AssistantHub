namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Net.Http;
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
    using AssistantHub.Server.Services;
    using SyslogLogging;
    using WatsonWebserver.Core;
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;

    /// <summary>
    /// Handles public assistant info, chat, and feedback submission routes.
    /// </summary>
    public class ChatHandler : ChatHandlerRouteBase
    {
        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">Application settings.</param>
        /// <param name="authentication">Authentication service.</param>
        /// <param name="storage">Storage service.</param>
        /// <param name="ingestion">Ingestion service.</param>
        /// <param name="retrieval">Retrieval service.</param>
        /// <param name="inference">Inference service.</param>
        public ChatHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            IObjectStorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference)
        {
        }

        /// <summary>
        /// POST /v1.0/assistants/{assistantId}/chat - OpenAI-compatible chat completion endpoint.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PostChatAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                string assistantId = ctx.Request.Url.Parameters["assistantId"];
                if (String.IsNullOrEmpty(assistantId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                Assistant assistant = await Database.Assistant.ReadAsync(assistantId).ConfigureAwait(false);
                if (assistant == null || !assistant.Active)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                string body = ctx.Request.DataAsString;
                ChatCompletionRequest chatReq = Serializer.DeserializeJson<ChatCompletionRequest>(body);
                if (chatReq == null || chatReq.Messages == null || chatReq.Messages.Count == 0)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "At least one message is required."))).ConfigureAwait(false);
                    return;
                }

                TelemetryContext telemetryContext = EnsureTelemetryContext(ctx);

                AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);
                if (settings == null)
                {
                    ctx.Response.StatusCode = 500;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, "Assistant settings not configured."))).ConfigureAwait(false);
                    return;
                }
                if (String.IsNullOrWhiteSpace(settings.InferenceEndpointId))
                {
                    ctx.Response.StatusCode = 500;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, "Assistant inference endpoint not configured."))).ConfigureAwait(false);
                    return;
                }

                List<string> attachedDocumentIds = NormalizeDocumentIds(chatReq.AttachedDocumentIds);

                if (settings.Streaming && ShouldUseToolAwareStreaming(assistant, settings))
                {
                    await HandleToolAwareStreamingChatAsync(
                        ctx,
                        assistant,
                        settings,
                        chatReq,
                        attachedDocumentIds,
                        telemetryContext).ConfigureAwait(false);
                    return;
                }

                if (!settings.Streaming)
                {
                    AssistantChatService chatService = new AssistantChatService(
                        Database,
                        Logging,
                        Settings,
                        Retrieval,
                        Inference,
                        Storage,
                        inferenceEndpoints: InferenceEndpoints);
                    AssistantChatExecutionResult result = await chatService.ExecuteNonStreamingAsync(
                        new AssistantChatExecutionRequest
                        {
                            AssistantId = assistantId,
                            Assistant = assistant,
                            AssistantSettings = settings,
                            Messages = chatReq.Messages,
                            ThreadId = ctx.Request.Headers[Constants.ThreadIdHeader],
                            TraceId = telemetryContext.TraceId,
                            RequestHistoryId = telemetryContext.RequestHistoryId,
                            ChatHistoryPersisted = chatHistoryId => SetChatHistoryId(ctx, chatHistoryId),
                            Model = chatReq.Model,
                            Temperature = chatReq.Temperature,
                            TopP = chatReq.TopP,
                            MaxTokens = chatReq.MaxTokens,
                            MetadataFilter = chatReq.MetadataFilter,
                            AttachedDocumentIds = attachedDocumentIds,
                            LocalAttachments = chatReq.LocalAttachments,
                            Origin = "web"
                        }).ConfigureAwait(false);

                    if (!result.Success)
                    {
                        ctx.Response.StatusCode = result.StatusCode > 0 ? result.StatusCode : 502;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(
                            Enums.ApiErrorEnum.InternalError, null,
                            result.ErrorMessage ?? "Inference failed."))).ConfigureAwait(false);
                        return;
                    }

                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    if (!String.IsNullOrEmpty(result.ChatHistoryId))
                        SetChatHistoryId(ctx, result.ChatHistoryId);
                    await ctx.Response.Send(Serializer.SerializeJson(result.Response)).ConfigureAwait(false);
                    return;
                }

                List<AssistantDocumentSelectionItem> attachedDocuments = null;
                AssistantDocumentAttachmentResolver attachmentResolver = new AssistantDocumentAttachmentResolver(Database);
                AssistantDocumentAttachmentResolution attachmentResolution = await attachmentResolver.ResolveAsync(
                    assistant, settings, attachedDocumentIds).ConfigureAwait(false);
                if (!attachmentResolution.Success)
                {
                    ctx.Response.StatusCode = attachmentResolution.StatusCode;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(
                        Enums.ApiErrorEnum.BadRequest, null,
                        attachmentResolution.ErrorMessage))).ConfigureAwait(false);
                    return;
                }

                attachedDocumentIds = attachmentResolution.DocumentIds.Count > 0 ? attachmentResolution.DocumentIds : null;
                attachedDocuments = attachmentResolution.Documents.Count > 0 ? attachmentResolution.Documents : null;
                if (attachedDocumentIds != null && attachedDocumentIds.Count > 0)
                    Logging.Info(_Header + "attached document filter active: count=" + attachedDocumentIds.Count);

                int localAttachmentCount = ChatLocalAttachmentProcessor.Count(chatReq.LocalAttachments);
                if (localAttachmentCount > 0 && (attachedDocumentIds?.Count ?? 0) + localAttachmentCount > settings.DocumentAttachmentMaxCount)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(
                        Enums.ApiErrorEnum.BadRequest, null,
                        "Too many attachments. The assistant allows " + settings.DocumentAttachmentMaxCount + " attachment(s) per request."))).ConfigureAwait(false);
                    return;
                }

                ChatLocalAttachmentResolution localAttachmentResolution = await ChatLocalAttachmentProcessor.ResolveAsync(
                    settings,
                    chatReq.LocalAttachments,
                    Settings,
                    Logging).ConfigureAwait(false);
                if (!localAttachmentResolution.Success)
                {
                    ctx.Response.StatusCode = localAttachmentResolution.StatusCode;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(
                        Enums.ApiErrorEnum.BadRequest, null,
                        localAttachmentResolution.ErrorMessage))).ConfigureAwait(false);
                    return;
                }

                string localAttachmentContext = ChatLocalAttachmentProcessor.BuildPromptContext(localAttachmentResolution.Attachments);
                if (localAttachmentResolution.Attachments.Count > 0)
                    Logging.Info(_Header + "local chat attachments active: count=" + localAttachmentResolution.Attachments.Count);

                // Build effective metadata filter by merging assistant defaults + request-level filter
                ChatMetadataFilter effectiveMetadataFilter = null;
                string metadataFilterJson = null;
                {
                    // Deserialize assistant-level filters
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

                    // Merge assistant-level and request-level filters
                    if (assistantFilter != null && chatReq.MetadataFilter != null)
                    {
                        effectiveMetadataFilter = new ChatMetadataFilter
                        {
                            RequiredLabels = assistantFilter.RequiredLabels != null ? new List<string>(assistantFilter.RequiredLabels) : null,
                            ExcludedLabels = assistantFilter.ExcludedLabels != null ? new List<string>(assistantFilter.ExcludedLabels) : null,
                            RequiredTags = assistantFilter.RequiredTags != null ? new List<ChatTagCondition>(assistantFilter.RequiredTags) : null,
                            ExcludedTags = assistantFilter.ExcludedTags != null ? new List<ChatTagCondition>(assistantFilter.ExcludedTags) : null
                        };
                        effectiveMetadataFilter.Merge(chatReq.MetadataFilter);
                    }
                    else if (assistantFilter != null)
                    {
                        effectiveMetadataFilter = assistantFilter;
                    }
                    else if (chatReq.MetadataFilter != null)
                    {
                        effectiveMetadataFilter = chatReq.MetadataFilter;
                    }

                    if (effectiveMetadataFilter != null && !effectiveMetadataFilter.IsEmpty)
                    {
                        metadataFilterJson = JsonSerializer.Serialize(effectiveMetadataFilter, _JsonOptions);
                        Logging.Info(_Header + "effective metadata filter: " + metadataFilterJson);
                    }
                }

                // Check for thread ID header for history tracking
                string threadId = ctx.Request.Headers[Constants.ThreadIdHeader];

                DateTime userMessageUtc = DateTime.UtcNow;

                // Extract last user message for RAG retrieval
                string lastUserMessage = null;
                for (int i = chatReq.Messages.Count - 1; i >= 0; i--)
                {
                    if (String.Equals(chatReq.Messages[i].Role, "user", StringComparison.OrdinalIgnoreCase))
                    {
                        lastUserMessage = chatReq.Messages[i].Content;
                        break;
                    }
                }

                // Retrieval stages, prompt ordering and citations are shared with the non-streaming chat rail.
                AssistantChatService contextService = new AssistantChatService(
                    Database,
                    Logging,
                    Settings,
                    Retrieval,
                    Inference,
                    Storage,
                    inferenceEndpoints: InferenceEndpoints);
                AssistantPromptContext promptContext = await contextService.BuildPromptContextAsync(
                    assistant,
                    settings,
                    chatReq.Messages,
                    lastUserMessage,
                    effectiveMetadataFilter,
                    attachedDocumentIds,
                    attachedDocuments).ConfigureAwait(false);
                AssistantRetrievalStagesResult stages = promptContext.Stages;
                string retrievalGateDecision = stages.GateDecision;
                double retrievalGateDurationMs = stages.GateDurationMs;
                AssistantPerformanceStage retrievalGateTelemetry = stages.GateTelemetry;
                string queryRewriteResult = stages.QueryRewriteResult;
                double queryRewriteDurationMs = stages.QueryRewriteDurationMs;
                AssistantPerformanceStage queryRewriteTelemetry = stages.QueryRewriteTelemetry;
                List<string> retrievalQueries = stages.Queries ?? new List<string>();
                List<RetrievalChunk> retrievalChunks = promptContext.Chunks;
                DateTime? retrievalStartUtc = stages.RetrievalStartUtc;
                double retrievalDurationMs = stages.RetrievalDurationMs;
                double rerankDurationMs = stages.RerankDurationMs;
                int rerankInputCount = stages.RerankInputCount;
                int rerankOutputCount = stages.RerankOutputCount;
                AssistantPerformanceStage rerankTelemetry = stages.RerankTelemetry;
                List<string> contextChunks = promptContext.ContextChunks;
                List<string> chunkLabels = promptContext.ChunkLabels;
                List<CitationSource> citationSources = promptContext.CitationSources;
                if (!String.IsNullOrEmpty(promptContext.PromptNote))
                    localAttachmentContext = String.IsNullOrWhiteSpace(localAttachmentContext) ? promptContext.PromptNote : localAttachmentContext + "\n\n" + promptContext.PromptNote;

                // Build message list
                List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>(chatReq.Messages);
                string baseSystemPrompt = null;
                int systemMessageIndex = -1;

                // If no system message in request, prepend one from settings
                bool hasSystemMessage = messages.Any(m =>
                    String.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase)
                    && !IsConversationSummaryMessage(m));
                if (!hasSystemMessage && (!String.IsNullOrEmpty(settings.SystemPrompt) || !String.IsNullOrEmpty(localAttachmentContext)))
                {
                    baseSystemPrompt = ChatLocalAttachmentProcessor.AppendToSystemPrompt(settings.SystemPrompt, localAttachmentContext);
                    string fullSystemMessage = Inference.BuildSystemMessage(
                        baseSystemPrompt, contextChunks,
                        settings.EnableCitations, chunkLabels);
                    messages.Insert(0, new ChatCompletionMessage { Role = "system", Content = fullSystemMessage });
                    systemMessageIndex = 0;
                }
                else if (hasSystemMessage && (contextChunks.Count > 0 || !String.IsNullOrEmpty(localAttachmentContext)))
                {
                    // Append RAG context to existing system message
                    for (int i = 0; i < messages.Count; i++)
                    {
                        if (String.Equals(messages[i].Role, "system", StringComparison.OrdinalIgnoreCase)
                            && !IsConversationSummaryMessage(messages[i]))
                        {
                            baseSystemPrompt = ChatLocalAttachmentProcessor.AppendToSystemPrompt(messages[i].Content, localAttachmentContext);
                            messages[i] = new ChatCompletionMessage
                            {
                                Role = "system",
                                Content = Inference.BuildSystemMessage(
                                    baseSystemPrompt, contextChunks,
                                    settings.EnableCitations, chunkLabels)
                            };
                            systemMessageIndex = i;
                            break;
                        }
                    }
                }

                // Resolve parameters (request overrides fall back to settings)
                string model = !String.IsNullOrEmpty(chatReq.Model) ? chatReq.Model : Settings.Inference.DefaultModel;
                double temperature = chatReq.Temperature ?? settings.Temperature;
                double topP = chatReq.TopP ?? settings.TopP;
                int maxTokens = chatReq.MaxTokens ?? settings.MaxTokens;

                TrimRetrievalContextToPromptBudget(
                    messages,
                    settings,
                    maxTokens,
                    retrievalChunks,
                    chunkLabels,
                    citationSources,
                    baseSystemPrompt,
                    systemMessageIndex);

                // Resolve inference endpoint details
                Enums.InferenceProviderEnum inferenceProvider = Settings.Inference.Provider;
                string inferenceEndpoint = Settings.Inference.Endpoint;
                string inferenceApiKey = Settings.Inference.ApiKey;
                string inferenceEndpointId = settings.InferenceEndpointId;
                int inferenceMaxConcurrentRequests = 1;

                double endpointResolutionMs = 0;
                if (!String.IsNullOrEmpty(settings.InferenceEndpointId))
                {
                    Stopwatch endpointSw = Stopwatch.StartNew();
                    ResolvedEndpoint? resolved = await ResolveCompletionEndpointAsync(settings.InferenceEndpointId).ConfigureAwait(false);
                    endpointSw.Stop();
                    endpointResolutionMs = Math.Round(endpointSw.Elapsed.TotalMilliseconds, 2);
                    Logging.Info(_Header + "endpoint resolution took " + endpointResolutionMs + " ms");
                    if (resolved != null)
                    {
                        inferenceProvider = resolved.Value.Provider;
                        inferenceEndpoint = resolved.Value.Endpoint;
                        inferenceApiKey = resolved.Value.ApiKey;
                        inferenceEndpointId = resolved.Value.EndpointId;
                        inferenceMaxConcurrentRequests = resolved.Value.MaxConcurrentRequests;
                        if (String.IsNullOrEmpty(chatReq.Model) && !String.IsNullOrEmpty(resolved.Value.Model))
                            model = resolved.Value.Model;
                    }
                }

                // Conversation compaction
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
                    settings.Streaming ? ctx : null).ConfigureAwait(false);
                compactionSw.Stop();
                double compactionMs = Math.Round(compactionSw.Elapsed.TotalMilliseconds, 2);
                Logging.Info(_Header + "compaction phase took " + compactionMs + " ms");

                int promptTokenEstimate = EstimateTokenCount(messages);
                Logging.Info(_Header + "sending " + messages.Count + " messages, ~" + promptTokenEstimate + " tokens to " + model);

                DateTime promptSentUtc = DateTime.UtcNow;
                Stopwatch inferenceSw = Stopwatch.StartNew();

                string retrievalContextText = retrievalChunks.Count > 0 ? Serializer.SerializeJson(retrievalChunks, true) : null;

                if (settings.Streaming)
                {
                    await HandleStreamingResponse(ctx, messages, model, maxTokens, temperature, topP,
                        settings, inferenceProvider, inferenceEndpoint, inferenceApiKey,
                        inferenceEndpointId, inferenceMaxConcurrentRequests,
                        assistant.TenantId, threadId, assistantId, settings.CollectionId, userMessageUtc, lastUserMessage,
                        retrievalStartUtc, retrievalDurationMs, retrievalContextText, retrievalChunks, attachedDocumentIds, attachedDocuments, promptSentUtc, inferenceSw,
                        promptTokenEstimate, endpointResolutionMs, compactionMs,
                        retrievalGateDecision, retrievalGateDurationMs, citationSources,
                        queryRewriteResult, queryRewriteDurationMs,
                        rerankDurationMs, rerankInputCount, rerankOutputCount,
                        metadataFilterJson,
                        telemetryContext.TraceId,
                        telemetryContext.RequestHistoryId,
                        retrievalGateTelemetry,
                        queryRewriteTelemetry,
                        rerankTelemetry,
                        retrievalQueries.Count).ConfigureAwait(false);
                }
                else
                {
                    await HandleNonStreamingResponse(ctx, messages, model, maxTokens, temperature, topP,
                        settings, inferenceProvider, inferenceEndpoint, inferenceApiKey,
                        inferenceEndpointId, inferenceMaxConcurrentRequests,
                        assistant.TenantId, threadId, assistantId, settings.CollectionId, userMessageUtc, lastUserMessage,
                        retrievalStartUtc, retrievalDurationMs, retrievalContextText, retrievalChunks, attachedDocumentIds, attachedDocuments, promptSentUtc, inferenceSw,
                        promptTokenEstimate, endpointResolutionMs, compactionMs,
                        retrievalGateDecision, retrievalGateDurationMs, citationSources,
                        queryRewriteResult, queryRewriteDurationMs,
                        rerankDurationMs, rerankInputCount, rerankOutputCount,
                        metadataFilterJson,
                        telemetryContext.TraceId,
                        telemetryContext.RequestHistoryId,
                        retrievalGateTelemetry,
                        queryRewriteTelemetry,
                        rerankTelemetry,
                        retrievalQueries.Count).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PostChatAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        private bool ShouldUseToolAwareStreaming(Assistant assistant, AssistantSettings settings)
        {
            if (assistant == null || settings == null) return false;

            AssistantToolPolicy policy = settings.ToolPolicy ?? new AssistantToolPolicy();
            policy.Normalize();
            settings.ToolPolicy = policy;

            if (!policy.EnableToolCalls
                || String.Equals(policy.ToolChoiceMode, "None", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            List<AssistantToolDefinition> definitions = new AssistantToolRegistry(Settings).BuildDefinitions(assistant, settings);
            return definitions.Any(definition =>
                definition?.Function != null
                && !String.IsNullOrWhiteSpace(definition.Function.Name));
        }

        private async Task HandleToolAwareStreamingChatAsync(
            HttpContextBase ctx,
            Assistant assistant,
            AssistantSettings settings,
            ChatCompletionRequest chatReq,
            List<string> attachedDocumentIds,
            TelemetryContext telemetryContext)
        {
            string completionId = IdGenerator.NewChatCompletionId();
            long created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string model = !String.IsNullOrWhiteSpace(chatReq.Model)
                ? chatReq.Model
                : Settings.Inference.DefaultModel;

            ctx.Response.StatusCode = 200;
            ctx.Response.ServerSentEvents = true;

            ChatCompletionResponse initialChunk = new ChatCompletionResponse
            {
                Id = completionId,
                Object = "chat.completion.chunk",
                Created = created,
                Model = model,
                Choices = new List<ChatCompletionChoice>
                {
                    new ChatCompletionChoice
                    {
                        Index = 0,
                        Delta = new ChatCompletionMessage { Role = "assistant" }
                    }
                }
            };
            await WriteSseEvent(ctx, initialChunk).ConfigureAwait(false);
            StringBuilder streamedContent = new StringBuilder();
            StringBuilder streamedThinking = new StringBuilder();

            AssistantChatService chatService = new AssistantChatService(
                Database,
                Logging,
                Settings,
                Retrieval,
                Inference,
                Storage,
                inferenceEndpoints: InferenceEndpoints);

            AssistantChatExecutionResult result = await chatService.ExecuteNonStreamingAsync(
                new AssistantChatExecutionRequest
                {
                    AssistantId = assistant.Id,
                    Assistant = assistant,
                    AssistantSettings = settings,
                    Messages = chatReq.Messages,
                    ThreadId = ctx.Request.Headers[Constants.ThreadIdHeader],
                    TraceId = telemetryContext.TraceId,
                    RequestHistoryId = telemetryContext.RequestHistoryId,
                    ChatHistoryPersisted = chatHistoryId => SetChatHistoryId(ctx, chatHistoryId),
                    Model = chatReq.Model,
                    Temperature = chatReq.Temperature,
                    TopP = chatReq.TopP,
                    MaxTokens = chatReq.MaxTokens,
                    MetadataFilter = chatReq.MetadataFilter,
                    AttachedDocumentIds = attachedDocumentIds,
                    LocalAttachments = chatReq.LocalAttachments,
                    Origin = "web",
                    ToolProgress = async evt =>
                    {
                        if (evt == null) return;
                        AssistantToolProgressEvent publicEvent = ShapePublicToolProgressEvent(evt);
                        if (publicEvent == null) return;
                        await WriteSseNamedEvent(ctx, publicEvent.EventType, publicEvent).ConfigureAwait(false);
                    },
                    ResponseDelta = async delta =>
                    {
                        if (String.IsNullOrEmpty(delta)) return;
                        streamedContent.Append(delta);
                        ChatCompletionResponse deltaChunk = new ChatCompletionResponse
                        {
                            Id = completionId,
                            Object = "chat.completion.chunk",
                            Created = created,
                            Model = model,
                            Choices = new List<ChatCompletionChoice>
                            {
                                new ChatCompletionChoice
                                {
                                    Index = 0,
                                    Delta = new ChatCompletionMessage { Content = delta }
                                }
                            }
                        };
                        await WriteSseEvent(ctx, deltaChunk).ConfigureAwait(false);
                    },
                    ThinkingDelta = async delta =>
                    {
                        if (String.IsNullOrEmpty(delta) || !settings.ExposeThinking) return;
                        streamedThinking.Append(delta);
                        ChatCompletionResponse thinkingChunk = new ChatCompletionResponse
                        {
                            Id = completionId,
                            Object = "chat.completion.chunk",
                            Created = created,
                            Model = model,
                            Choices = new List<ChatCompletionChoice>
                            {
                                new ChatCompletionChoice
                                {
                                    Index = 0,
                                    Delta = new ChatCompletionMessage { Thinking = delta }
                                }
                            }
                        };
                        await WriteSseEvent(ctx, thinkingChunk).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);

            if (!result.Success)
            {
                ChatCompletionResponse errorChunk = new ChatCompletionResponse
                {
                    Id = completionId,
                    Object = "chat.completion.chunk",
                    Created = created,
                    Model = model,
                    Choices = new List<ChatCompletionChoice>
                    {
                        new ChatCompletionChoice
                        {
                            Index = 0,
                            Delta = new ChatCompletionMessage
                            {
                                Content = "AssistantHub could not generate a response: " + (result.ErrorMessage ?? "Inference failed.")
                            },
                            FinishReason = "stop"
                        }
                    },
                    Status = "Error"
                };
                await WriteSseEvent(ctx, errorChunk).ConfigureAwait(false);
                await ctx.Response.SendEvent(new ServerSentEvent { Data = "[DONE]" }, true).ConfigureAwait(false);
                return;
            }

            ChatCompletionResponse response = result.Response;
            string content = result.CanonicalResponseText
                ?? response?.Choices?.FirstOrDefault()?.Message?.Content
                ?? "";
            string thinking = settings.ExposeThinking
                ? response?.Choices?.FirstOrDefault()?.Message?.Thinking
                : null;

            if (!String.IsNullOrEmpty(result.ChatHistoryId))
                SetChatHistoryId(ctx, result.ChatHistoryId);

            if (!String.IsNullOrEmpty(thinking) && streamedThinking.Length == 0)
            {
                ChatCompletionResponse thinkingChunk = new ChatCompletionResponse
                {
                    Id = completionId,
                    Object = "chat.completion.chunk",
                    Created = created,
                    Model = response?.Model ?? model,
                    Choices = new List<ChatCompletionChoice>
                    {
                        new ChatCompletionChoice
                        {
                            Index = 0,
                            Delta = new ChatCompletionMessage { Thinking = thinking }
                        }
                    }
                };
                await WriteSseEvent(ctx, thinkingChunk).ConfigureAwait(false);
            }

            if (!String.IsNullOrEmpty(content) && streamedContent.Length == 0)
            {
                ChatCompletionResponse deltaChunk = new ChatCompletionResponse
                {
                    Id = completionId,
                    Object = "chat.completion.chunk",
                    Created = created,
                    Model = response?.Model ?? model,
                    Choices = new List<ChatCompletionChoice>
                    {
                        new ChatCompletionChoice
                        {
                            Index = 0,
                            Delta = new ChatCompletionMessage { Content = content }
                        }
                    }
                };
                await WriteSseEvent(ctx, deltaChunk).ConfigureAwait(false);
            }

            ChatCompletionResponse finishChunk = new ChatCompletionResponse
            {
                Id = completionId,
                Object = "chat.completion.chunk",
                Created = created,
                Model = response?.Model ?? model,
                Choices = new List<ChatCompletionChoice>
                {
                    new ChatCompletionChoice
                    {
                        Index = 0,
                        Delta = new ChatCompletionMessage(),
                        FinishReason = "stop"
                    }
                },
                Usage = response?.Usage,
                Retrieval = response?.Retrieval,
                Citations = response?.Citations,
                ToolCalls = ShapePublicToolTraces(response?.ToolCalls)
            };
            await WriteSseEvent(ctx, finishChunk).ConfigureAwait(false);
            await ctx.Response.SendEvent(new ServerSentEvent { Data = "[DONE]" }, true).ConfigureAwait(false);
        }

        private static AssistantToolProgressEvent ShapePublicToolProgressEvent(AssistantToolProgressEvent evt)
        {
            if (evt == null) return null;

            return new AssistantToolProgressEvent
            {
                EventType = evt.EventType,
                ToolCallId = evt.ToolCallId,
                ToolName = evt.ToolName,
                DisplayLabel = evt.DisplayLabel,
                StatusCode = evt.StatusCode,
                Iteration = evt.Iteration,
                SequenceNumber = evt.SequenceNumber,
                StartedUtc = evt.StartedUtc,
                FinishedUtc = evt.FinishedUtc,
                Truncated = evt.Truncated == true ? true : null,
                Denied = evt.Denied == true ? true : null,
                Success = evt.Success,
                DurationMs = evt.DurationMs,
                ResultCount = evt.ResultCount,
                Summary = evt.Summary
            };
        }

        private static List<ChatCompletionToolTrace> ShapePublicToolTraces(List<ChatCompletionToolTrace> traces)
        {
            if (traces == null || traces.Count < 1) return null;

            List<ChatCompletionToolTrace> ret = new List<ChatCompletionToolTrace>();
            foreach (ChatCompletionToolTrace trace in traces)
            {
                if (trace == null) continue;

                ret.Add(new ChatCompletionToolTrace
                {
                    ToolCallId = trace.ToolCallId,
                    ToolName = trace.ToolName,
                    DisplayLabel = trace.DisplayLabel,
                    Iteration = trace.Iteration,
                    SequenceNumber = trace.SequenceNumber,
                    Success = trace.Success,
                    Denied = trace.Denied,
                    Truncated = trace.Truncated,
                    OutputCharacters = 0,
                    ResultCount = trace.ResultCount,
                    CreditsUsed = null,
                    ProviderLatencyMs = null,
                    DurationMs = trace.DurationMs,
                    Summary = trace.Summary,
                    StartedUtc = trace.StartedUtc,
                    FinishedUtc = trace.FinishedUtc
                });
            }

            return ret.Count > 0 ? ret : null;
        }

    }
}
