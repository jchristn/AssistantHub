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
    /// Provides secondary public chat route handlers.
    /// </summary>
    public abstract class ChatHandlerRouteBase : ChatHandlerExecutionBase
    {
        /// <summary>
        /// Instantiate the chat route base.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">Application settings.</param>
        /// <param name="authentication">Authentication service.</param>
        /// <param name="storage">Storage service.</param>
        /// <param name="ingestion">Ingestion service.</param>
        /// <param name="retrieval">Retrieval service.</param>
        /// <param name="inference">Inference service.</param>
        protected ChatHandlerRouteBase(
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
        /// GET /v1.0/assistants/{assistantId}/public - Public assistant info.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetAssistantPublicAsync(HttpContextBase ctx)
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

                AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new
                {
                    Id = assistant.Id,
                    Name = assistant.Name,
                    Description = assistant.Description,
                    Title = settings?.Title,
                    LogoUrl = settings?.LogoUrl,
                    FaviconUrl = settings?.FaviconUrl,
                    LoadModelsOnChatOpen = settings?.LoadModelsOnChatOpen ?? false,
                    ExposeThinking = settings?.ExposeThinking ?? false,
                    EnableDocumentAttachments = settings?.EnableDocumentAttachments ?? false,
                    DocumentAttachmentMaxCount = settings?.DocumentAttachmentMaxCount ?? 10,
                    ExposeDocumentSourceUrls = settings?.ExposeDocumentSourceUrls ?? false
                })).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetAssistantPublicAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/documents - List selectable public chat documents.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetAssistantDocumentsAsync(HttpContextBase ctx)
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

                AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);
                if (settings == null || !settings.EnableDocumentAttachments)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed, null, "Document attachments are not enabled for this assistant."))).ConfigureAwait(false);
                    return;
                }

                if (String.IsNullOrEmpty(settings.CollectionId))
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new EnumerationResult<AssistantDocumentSelectionItem>())).ConfigureAwait(false);
                    return;
                }

                EnumerationQuery query = BuildEnumerationQuery(ctx);
                query.CollectionIdFilter = settings.CollectionId;

                string queryText = ctx.Request.Query.Elements.Get("query");
                List<string> contentTypeFilters = ParseContentTypeFilters(
                    ctx.Request.Query.Elements.Get("contentType") ?? ctx.Request.Query.Elements.Get("content_type"));

                EnumerationResult<AssistantDocument> documents = await Database.AssistantDocument.EnumerateAsync(assistant.TenantId, query).ConfigureAwait(false);
                List<AssistantDocumentSelectionItem> items = documents.Objects
                    .Where(doc => doc.Status == Enums.DocumentStatusEnum.Completed)
                    .Where(doc => AssistantDocumentPolicyFilter.MatchesAssistantMetadataFilters(doc, settings))
                    .Where(doc => MatchesDocumentQuery(doc, queryText, settings.ExposeDocumentSourceUrls))
                    .Where(doc => MatchesContentTypeFilter(doc, contentTypeFilters))
                    .Select(doc => AssistantDocumentSelectionItem.FromDocument(doc, settings.ExposeDocumentSourceUrls))
                    .ToList();

                EnumerationResult<AssistantDocumentSelectionItem> result = new EnumerationResult<AssistantDocumentSelectionItem>
                {
                    MaxResults = documents.MaxResults,
                    TotalRecords = items.Count,
                    RecordsRemaining = documents.EndOfResults ? 0 : documents.RecordsRemaining,
                    ContinuationToken = documents.ContinuationToken,
                    EndOfResults = documents.EndOfResults,
                    Objects = items,
                    TotalMs = documents.TotalMs
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetAssistantDocumentsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/assistants/{assistantId}/chat/open - Load configured endpoint models when a chat window opens.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PostChatOpenAsync(HttpContextBase ctx)
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

                AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);
                if (settings == null)
                {
                    ctx.Response.StatusCode = 500;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, "Assistant settings not configured."))).ConfigureAwait(false);
                    return;
                }

                if (!settings.LoadModelsOnChatOpen)
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new
                    {
                        Success = true,
                        Enabled = false,
                        Loaded = false,
                        CompletionEndpointCount = 0,
                        EmbeddingEndpointCount = 0,
                        Results = Array.Empty<object>()
                    })).ConfigureAwait(false);
                    return;
                }

                HashSet<string> completionEndpointIds = new HashSet<string>(StringComparer.Ordinal);
                AddEndpointId(completionEndpointIds, settings.InferenceEndpointId);
                AddEndpointId(completionEndpointIds, settings.ToolRoutingInferenceEndpointId);
                AddEndpointId(completionEndpointIds, settings.RetrievalGateInferenceEndpointId);
                AddEndpointId(completionEndpointIds, settings.QueryRewriteInferenceEndpointId);
                AddEndpointId(completionEndpointIds, settings.RerankInferenceEndpointId);

                HashSet<string> embeddingEndpointIds = new HashSet<string>(StringComparer.Ordinal);
                AddEndpointId(embeddingEndpointIds, settings.EmbeddingEndpointId);

                List<object> results = new List<object>();
                bool allSucceeded = true;

                foreach (string endpointId in completionEndpointIds)
                {
                    (object Result, bool Success) result = await LoadConfiguredEndpointModelAsync("Completion", endpointId).ConfigureAwait(false);
                    results.Add(result.Result);
                    allSucceeded &= result.Success;
                }

                foreach (string endpointId in embeddingEndpointIds)
                {
                    (object Result, bool Success) result = await LoadConfiguredEndpointModelAsync("Embedding", endpointId).ConfigureAwait(false);
                    results.Add(result.Result);
                    allSucceeded &= result.Success;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new
                {
                    Success = allSucceeded,
                    Enabled = true,
                    Loaded = results.Count > 0,
                    CompletionEndpointCount = completionEndpointIds.Count,
                    EmbeddingEndpointCount = embeddingEndpointIds.Count,
                    Results = results
                })).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PostChatOpenAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// POST /v1.0/assistants/{assistantId}/feedback - Public feedback submission.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PostFeedbackAsync(HttpContextBase ctx)
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
                FeedbackRequest feedbackReq = Serializer.DeserializeJson<FeedbackRequest>(body);
                if (feedbackReq == null)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                AssistantFeedback feedback = new AssistantFeedback();
                feedback.Id = IdGenerator.NewAssistantFeedbackId();
                feedback.TenantId = assistant.TenantId;
                feedback.AssistantId = assistantId;
                feedback.UserMessage = feedbackReq.UserMessage;
                feedback.AssistantResponse = feedbackReq.AssistantResponse;
                feedback.Rating = feedbackReq.Rating;
                feedback.FeedbackText = feedbackReq.FeedbackText;
                feedback.MessageHistory = feedbackReq.MessageHistory;

                feedback = await Database.AssistantFeedback.CreateAsync(feedback).ConfigureAwait(false);

                ctx.Response.StatusCode = 201;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(feedback)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PostFeedbackAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// POST /v1.0/assistants/{assistantId}/threads - Create a new thread ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PostCreateThreadAsync(HttpContextBase ctx)
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

                string threadId = IdGenerator.NewThreadId();

                ctx.Response.StatusCode = 201;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new { ThreadId = threadId })).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PostCreateThreadAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// POST /v1.0/assistants/{assistantId}/compact - Force conversation compaction.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PostCompactAsync(HttpContextBase ctx)
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

                string threadId = ctx.Request.Headers[Constants.ThreadIdHeader];

                // Build message list with system prompt + RAG context (same as PostChatAsync)
                string lastUserMessage = null;
                for (int i = chatReq.Messages.Count - 1; i >= 0; i--)
                {
                    if (String.Equals(chatReq.Messages[i].Role, "user", StringComparison.OrdinalIgnoreCase))
                    {
                        lastUserMessage = chatReq.Messages[i].Content;
                        break;
                    }
                }

                List<string> contextChunks = new List<string>();
                List<RetrievalChunk> retrievalChunks = new List<RetrievalChunk>();
                if (settings.EnableRag && !String.IsNullOrEmpty(settings.CollectionId) && !String.IsNullOrEmpty(lastUserMessage))
                {
                    List<RetrievalChunk> retrievedChunks = await Retrieval.RetrieveAsync(
                        assistant.TenantId,
                        settings.CollectionId, lastUserMessage,
                        settings.RetrievalTopK, settings.RetrievalScoreThreshold,
                        default,
                        settings.EmbeddingEndpointId,
                        new RetrievalSearchOptions
                        {
                            SearchMode = settings.SearchMode,
                            TextWeight = settings.TextWeight,
                            FullTextSearchType = settings.FullTextSearchType,
                            FullTextLanguage = settings.FullTextLanguage,
                            FullTextNormalization = settings.FullTextNormalization,
                            FullTextMinimumScore = settings.FullTextMinimumScore,
                            IncludeNeighbors = settings.RetrievalIncludeNeighbors
                        }).ConfigureAwait(false);
                    if (retrievedChunks != null)
                    {
                        retrievalChunks.AddRange(retrievedChunks);
                        contextChunks.AddRange(retrievedChunks.Select(c => c.MergedContent));
                    }
                }

                // Resolve document names for citation labels
                List<string> chunkLabels = null;

                if (settings.EnableCitations && settings.EnableRag && retrievalChunks.Count > 0)
                {
                    chunkLabels = new List<string>();

                    foreach (RetrievalChunk chunk in retrievalChunks)
                    {
                        string docName = "Unknown Document";

                        if (!String.IsNullOrEmpty(chunk.DocumentId))
                        {
                            AssistantDocument doc = await Database.AssistantDocument.ReadAsync(chunk.DocumentId).ConfigureAwait(false);
                            if (doc != null)
                            {
                                docName = doc.Name ?? doc.OriginalFilename ?? "Unknown Document";
                            }
                        }

                        chunkLabels.Add("(Source: \"" + docName + "\")");
                    }
                }

                List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>(chatReq.Messages);

                bool hasSystemMessage = messages.Any(m =>
                    String.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase)
                    && !IsConversationSummaryMessage(m));
                if (!hasSystemMessage && !String.IsNullOrEmpty(settings.SystemPrompt))
                {
                    string fullSystemMessage = Inference.BuildSystemMessage(
                        settings.SystemPrompt, contextChunks,
                        settings.EnableCitations, chunkLabels);
                    messages.Insert(0, new ChatCompletionMessage { Role = "system", Content = fullSystemMessage });
                }
                else if (hasSystemMessage && contextChunks.Count > 0)
                {
                    for (int i = 0; i < messages.Count; i++)
                    {
                        if (String.Equals(messages[i].Role, "system", StringComparison.OrdinalIgnoreCase)
                            && !IsConversationSummaryMessage(messages[i]))
                        {
                            messages[i] = new ChatCompletionMessage
                            {
                                Role = "system",
                                Content = Inference.BuildSystemMessage(
                                    messages[i].Content, contextChunks,
                                    settings.EnableCitations, chunkLabels)
                            };
                            break;
                        }
                    }
                }

                string model = !String.IsNullOrEmpty(chatReq.Model) ? chatReq.Model : Settings.Inference.DefaultModel;

                // Resolve inference endpoint details
                Enums.InferenceProviderEnum compactInferenceProvider = Settings.Inference.Provider;
                string inferenceEndpoint = Settings.Inference.Endpoint;
                string inferenceApiKey = Settings.Inference.ApiKey;
                string inferenceEndpointId = settings.InferenceEndpointId;
                int inferenceMaxConcurrentRequests = 1;

                if (!String.IsNullOrEmpty(settings.InferenceEndpointId))
                {
                    ResolvedEndpoint? resolved = await ResolveCompletionEndpointAsync(settings.InferenceEndpointId).ConfigureAwait(false);
                    if (resolved != null)
                    {
                        compactInferenceProvider = resolved.Value.Provider;
                        inferenceEndpoint = resolved.Value.Endpoint;
                        inferenceApiKey = resolved.Value.ApiKey;
                        inferenceEndpointId = resolved.Value.EndpointId;
                        inferenceMaxConcurrentRequests = resolved.Value.MaxConcurrentRequests;
                        if (String.IsNullOrEmpty(chatReq.Model) && !String.IsNullOrEmpty(resolved.Value.Model))
                            model = resolved.Value.Model;
                    }
                }

                // Force compaction
                messages = await CompactIfNeeded(
                    messages,
                    settings,
                    compactInferenceProvider,
                    model,
                    inferenceEndpoint,
                    inferenceApiKey,
                    inferenceEndpointId,
                    inferenceMaxConcurrentRequests,
                    null,
                    force: true).ConfigureAwait(false);

                // Return the generated conversation summary so clients can carry it into the next turn.
                // The configured assistant system prompt is still excluded because the server injects it.
                List<ChatCompletionMessage> responseMessages = messages
                    .Where(m =>
                        !String.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase)
                        || IsConversationSummaryMessage(m))
                    .ToList();

                int promptTokens = EstimateTokenCount(messages);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new
                {
                    messages = responseMessages,
                    usage = new ChatCompletionUsage
                    {
                        PromptTokens = promptTokens,
                        TotalTokens = promptTokens,
                        ContextWindow = settings.ContextWindow
                    }
                })).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PostCompactAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// POST /v1.0/assistants/{assistantId}/generate - Lightweight inference-only endpoint.
        /// Skips RAG retrieval, compaction, system prompt injection, and chat history persistence.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PostGenerateAsync(HttpContextBase ctx)
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

                // Resolve parameters (request overrides fall back to settings)
                string model = !String.IsNullOrEmpty(chatReq.Model) ? chatReq.Model : Settings.Inference.DefaultModel;
                double temperature = chatReq.Temperature ?? settings.Temperature;
                double topP = chatReq.TopP ?? settings.TopP;
                int maxTokens = chatReq.MaxTokens ?? settings.MaxTokens;

                // Resolve inference endpoint details
                Enums.InferenceProviderEnum inferenceProvider = Settings.Inference.Provider;
                string inferenceEndpoint = Settings.Inference.Endpoint;
                string inferenceApiKey = Settings.Inference.ApiKey;
                string inferenceEndpointId = settings.InferenceEndpointId;
                int inferenceMaxConcurrentRequests = 1;

                if (!String.IsNullOrEmpty(settings.InferenceEndpointId))
                {
                    ResolvedEndpoint? resolved = await ResolveCompletionEndpointAsync(settings.InferenceEndpointId).ConfigureAwait(false);
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

                // Pass messages as-is — no RAG, no system prompt injection, no compaction
                List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>(chatReq.Messages);

                Logging.Info(_Header + "generate: sending " + messages.Count + " messages to " + model);

                InferenceResult inferenceResult = await GenerateWithCompletionEndpointLimitAsync(
                    messages, model, maxTokens, temperature, topP,
                    inferenceProvider, inferenceEndpoint, inferenceApiKey,
                    inferenceEndpointId, inferenceMaxConcurrentRequests).ConfigureAwait(false);

                if (inferenceResult != null && inferenceResult.Success && !String.IsNullOrEmpty(inferenceResult.Content))
                {
                    int promptTokens = EstimateTokenCount(messages);
                    int completionTokens = EstimateTokenCount(inferenceResult.Content);

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
                                Message = new ChatCompletionMessage { Role = "assistant", Content = inferenceResult.Content },
                                FinishReason = "stop"
                            }
                        },
                        Usage = new ChatCompletionUsage
                        {
                            PromptTokens = promptTokens,
                            CompletionTokens = completionTokens,
                            TotalTokens = promptTokens + completionTokens,
                            ContextWindow = settings.ContextWindow
                        }
                    };

                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(response)).ConfigureAwait(false);
                }
                else
                {
                    ctx.Response.StatusCode = 502;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(
                        Enums.ApiErrorEnum.InternalError, null,
                        inferenceResult?.ErrorMessage ?? "Inference failed."))).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PostGenerateAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/threads/{threadId}/history - Retrieve thread chat history.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetThreadHistoryAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                string assistantId = ctx.Request.Url.Parameters["assistantId"];
                string threadId = ctx.Request.Url.Parameters["threadId"];
                if (String.IsNullOrEmpty(assistantId) || String.IsNullOrEmpty(threadId))
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

                EnumerationQuery query = new EnumerationQuery();
                query.ThreadIdFilter = threadId;
                query.AssistantIdFilter = assistantId;
                query.Ordering = Enums.EnumerationOrderEnum.CreatedAscending;
                query.MaxResults = 1000;

                EnumerationResult<ChatHistory> result = await Database.ChatHistory.EnumerateAsync(assistant.TenantId, query).ConfigureAwait(false);

                List<object> items = new List<object>();
                if (result?.Objects != null)
                {
                    foreach (ChatHistory h in result.Objects)
                    {
                        items.Add(new
                        {
                            Id = h.Id,
                            ThreadId = h.ThreadId,
                            AssistantId = h.AssistantId,
                            CollectionId = h.CollectionId,
                            UserMessageUtc = h.UserMessageUtc,
                            UserMessage = h.UserMessage,
                            RetrievalStartUtc = h.RetrievalStartUtc,
                            RetrievalDurationMs = h.RetrievalDurationMs,
                            RetrievalGateDecision = h.RetrievalGateDecision,
                            RetrievalGateDurationMs = h.RetrievalGateDurationMs,
                            RetrievalContext = h.RetrievalContext,
                            PromptSentUtc = h.PromptSentUtc,
                            PromptTokens = h.PromptTokens,
                            CompletionTokens = h.CompletionTokens,
                            TokensPerSecondOverall = h.TokensPerSecondOverall,
                            TokensPerSecondGeneration = h.TokensPerSecondGeneration,
                            TimeToFirstTokenMs = h.TimeToFirstTokenMs,
                            TimeToLastTokenMs = h.TimeToLastTokenMs,
                            AssistantResponse = h.AssistantResponse,
                            CreatedUtc = h.CreatedUtc
                        });
                    }
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(items)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetThreadHistoryAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/documents/{documentId}/download - Public document download.
        /// Only available when the assistant's CitationLinkMode is "Public".
        /// Proxies the file from S3 storage through the server.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetPublicDocumentDownloadAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                string assistantId = ctx.Request.Url.Parameters["assistantId"];
                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(assistantId) || String.IsNullOrEmpty(documentId))
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

                AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);
                if (settings == null || !String.Equals(settings.CitationLinkMode, "Public", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                AssistantDocument doc = await Database.AssistantDocument.ReadAsync(documentId).ConfigureAwait(false);
                if (doc == null || doc.TenantId != assistant.TenantId)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (String.IsNullOrEmpty(doc.S3Key) || String.IsNullOrEmpty(doc.BucketName))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                byte[] data = null;
                try
                {
                    data = await Storage.DownloadAsync(doc.BucketName, doc.S3Key).ConfigureAwait(false);
                }
                catch (Exception storageEx)
                {
                    Logging.Warn(_Header + "storage download failed for document " + documentId + " (bucket: " + doc.BucketName + ", key: " + doc.S3Key + "): " + storageEx.Message);
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (data == null || data.Length == 0)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                string filename = doc.OriginalFilename ?? doc.Name ?? "document";
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = doc.ContentType ?? "application/octet-stream";
                ctx.Response.Headers.Add("Content-Disposition", "attachment; filename=\"" + filename + "\"");
                await ctx.Response.Send(data).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetPublicDocumentDownloadAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        private async Task<(object Result, bool Success)> LoadConfiguredEndpointModelAsync(string endpointType, string endpointId)
        {
            try
            {
                string requestBody = Serializer.SerializeJson(new
                {
                    Strategy = "Auto",
                    TimeoutMs = 60000,
                    KeepAlive = "30m",
                    SampleInput = "AssistantHub chat open model load",
                    MaxTokens = 1,
                    RecordRequestHistory = false,
                    RequireNativeLoad = false
                }, false);

                bool isEmbedding = String.Equals(endpointType, "Embedding", StringComparison.OrdinalIgnoreCase);
                string path = isEmbedding
                    ? "/v1.0/endpoints/embedding/" + endpointId + "/load"
                    : "/v1.0/endpoints/completion/" + endpointId + "/load";

                using (HttpResponseMessage resp = isEmbedding
                    ? await EmbeddingEndpoints.SendAsync(System.Net.Http.HttpMethod.Post, path, requestBody).ConfigureAwait(false)
                    : await InferenceEndpoints.SendAsync(System.Net.Http.HttpMethod.Post, path, requestBody).ConfigureAwait(false))
                {
                    string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    bool success = resp.IsSuccessStatusCode;
                    if (!success)
                        Logging.Warn(_Header + "chat-open model load failed for " + endpointType.ToLowerInvariant() + " endpoint " + endpointId + ": HTTP " + (int)resp.StatusCode + " " + body);

                    return (new
                    {
                        EndpointType = endpointType,
                        Success = success,
                        StatusCode = (int)resp.StatusCode
                    }, success);
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "chat-open model load exception for " + endpointType.ToLowerInvariant() + " endpoint " + endpointId + ": " + e.Message);
                return (new
                {
                    EndpointType = endpointType,
                    Success = false,
                    StatusCode = 0
                }, false);
            }
        }

        private static void AddEndpointId(HashSet<string> endpointIds, string endpointId)
        {
            if (endpointIds == null) return;
            string trimmed = endpointId?.Trim();
            if (!String.IsNullOrEmpty(trimmed))
                endpointIds.Add(trimmed);
        }

        private static bool MatchesDocumentQuery(AssistantDocument document, string queryText, bool includeSourceUrl)
        {
            if (document == null) return false;

            string needle = queryText?.Trim();
            if (String.IsNullOrEmpty(needle)) return true;
            needle = needle.ToLowerInvariant();

            List<string> values = new List<string>
            {
                document.Id,
                document.Name,
                document.OriginalFilename,
                document.ContentType
            };

            if (includeSourceUrl)
                values.Add(document.SourceUrl);

            return values.Any(value =>
                !String.IsNullOrEmpty(value)
                && value.ToLowerInvariant().Contains(needle));
        }

        private static List<string> ParseContentTypeFilters(string contentType)
        {
            if (String.IsNullOrWhiteSpace(contentType)) return new List<string>();

            return contentType
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !String.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList();
        }

        private static bool MatchesContentTypeFilter(AssistantDocument document, List<string> filters)
        {
            if (document == null) return false;
            if (filters == null || filters.Count == 0) return true;

            string contentType = document.ContentType?.Trim();
            if (String.IsNullOrEmpty(contentType)) return false;

            foreach (string filter in filters)
            {
                if (String.Equals(contentType, filter, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (filter.EndsWith("/*", StringComparison.Ordinal)
                    && contentType.StartsWith(filter.Substring(0, filter.Length - 1), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }


        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/labels/distinct - Get distinct labels for an assistant's collection (public).
        /// </summary>
        public async Task GetAssistantDistinctLabelsAsync(HttpContextBase ctx)
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

                AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);
                if (settings == null || String.IsNullOrEmpty(settings.CollectionId))
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send("[]").ConfigureAwait(false);
                    return;
                }

                using (HttpResponseMessage resp = await VectorStore.SendAsync(
                    System.Net.Http.HttpMethod.Get,
                    "/v1.0/tenants/" + assistant.TenantId + "/collections/" + settings.CollectionId + "/labels/distinct").ConfigureAwait(false))
                {
                    string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    ctx.Response.StatusCode = (int)resp.StatusCode;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(respBody).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetAssistantDistinctLabelsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// GET /v1.0/assistants/{assistantId}/tags/distinct - Get distinct tag keys for an assistant's collection (public).
        /// </summary>
        public async Task GetAssistantDistinctTagsAsync(HttpContextBase ctx)
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

                AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);
                if (settings == null || String.IsNullOrEmpty(settings.CollectionId))
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send("[]").ConfigureAwait(false);
                    return;
                }

                using (HttpResponseMessage resp = await VectorStore.SendAsync(
                    System.Net.Http.HttpMethod.Get,
                    "/v1.0/tenants/" + assistant.TenantId + "/collections/" + settings.CollectionId + "/tags/distinct").ConfigureAwait(false))
                {
                    string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    ctx.Response.StatusCode = (int)resp.StatusCode;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(respBody).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetAssistantDistinctTagsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }


    }
}
