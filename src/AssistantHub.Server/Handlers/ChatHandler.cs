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
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Handles public assistant info, chat, and feedback submission routes.
    /// </summary>
    public class ChatHandler : HandlerBase
    {
        private static readonly string _Header = "[ChatHandler] ";

        private static readonly JsonSerializerOptions _SseJsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };

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
            StorageService storage,
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
                    FaviconUrl = settings?.FaviconUrl
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

                AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId).ConfigureAwait(false);
                if (settings == null)
                {
                    ctx.Response.StatusCode = 500;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, "Assistant settings not configured."))).ConfigureAwait(false);
                    return;
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

                // Retrieve relevant context from the vector database
                List<RetrievalChunk> retrievalChunks = new List<RetrievalChunk>();
                DateTime? retrievalStartUtc = null;
                double retrievalDurationMs = 0;
                if (settings.EnableRag && !String.IsNullOrEmpty(settings.CollectionId) && !String.IsNullOrEmpty(lastUserMessage))
                {
                    retrievalStartUtc = DateTime.UtcNow;
                    Stopwatch retrievalSw = Stopwatch.StartNew();

                    List<RetrievalChunk> retrieved = await Retrieval.RetrieveAsync(
                        settings.CollectionId,
                        lastUserMessage,
                        settings.RetrievalTopK,
                        settings.RetrievalScoreThreshold,
                        default,
                        settings.EmbeddingEndpointId,
                        new RetrievalSearchOptions
                        {
                            SearchMode = settings.SearchMode,
                            TextWeight = settings.TextWeight,
                            FullTextSearchType = settings.FullTextSearchType,
                            FullTextLanguage = settings.FullTextLanguage,
                            FullTextNormalization = settings.FullTextNormalization,
                            FullTextMinimumScore = settings.FullTextMinimumScore
                        }).ConfigureAwait(false);

                    retrievalSw.Stop();
                    retrievalDurationMs = Math.Round(retrievalSw.Elapsed.TotalMilliseconds, 2);

                    if (retrieved != null)
                    {
                        retrievalChunks.AddRange(retrieved);
                    }
                }

                // Extract content strings for system message building
                List<string> contextChunks = retrievalChunks.Select(c => c.Content).ToList();

                // Build message list
                List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>(chatReq.Messages);

                // If no system message in request, prepend one from settings
                bool hasSystemMessage = messages.Any(m => String.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase));
                if (!hasSystemMessage && !String.IsNullOrEmpty(settings.SystemPrompt))
                {
                    string fullSystemMessage = Inference.BuildSystemMessage(settings.SystemPrompt, contextChunks);
                    messages.Insert(0, new ChatCompletionMessage { Role = "system", Content = fullSystemMessage });
                }
                else if (hasSystemMessage && contextChunks.Count > 0)
                {
                    // Append RAG context to existing system message
                    for (int i = 0; i < messages.Count; i++)
                    {
                        if (String.Equals(messages[i].Role, "system", StringComparison.OrdinalIgnoreCase))
                        {
                            messages[i] = new ChatCompletionMessage
                            {
                                Role = "system",
                                Content = Inference.BuildSystemMessage(messages[i].Content, contextChunks)
                            };
                            break;
                        }
                    }
                }

                // Resolve parameters (request overrides fall back to settings)
                string model = !String.IsNullOrEmpty(chatReq.Model) ? chatReq.Model : settings.Model;
                double temperature = chatReq.Temperature ?? settings.Temperature;
                double topP = chatReq.TopP ?? settings.TopP;
                int maxTokens = chatReq.MaxTokens ?? settings.MaxTokens;

                // Resolve inference endpoint details
                Enums.InferenceProviderEnum inferenceProvider = Settings.Inference.Provider;
                string inferenceEndpoint = Settings.Inference.Endpoint;
                string inferenceApiKey = Settings.Inference.ApiKey;

                if (!String.IsNullOrEmpty(settings.InferenceEndpointId))
                {
                    var resolved = await ResolveCompletionEndpointAsync(settings.InferenceEndpointId).ConfigureAwait(false);
                    if (resolved != null)
                    {
                        inferenceProvider = resolved.Value.Provider;
                        inferenceEndpoint = resolved.Value.Endpoint;
                        inferenceApiKey = resolved.Value.ApiKey;
                    }
                }

                // Conversation compaction
                messages = await CompactIfNeeded(messages, settings, inferenceProvider, model, inferenceEndpoint, inferenceApiKey,
                    settings.Streaming ? ctx : null).ConfigureAwait(false);

                int promptTokenEstimate = EstimateTokenCount(messages);
                Logging.Info(_Header + "sending " + messages.Count + " messages, ~" + promptTokenEstimate + " tokens to " + model);

                DateTime promptSentUtc = DateTime.UtcNow;
                Stopwatch inferenceSw = Stopwatch.StartNew();

                string retrievalContextText = retrievalChunks.Count > 0 ? Serializer.SerializeJson(retrievalChunks, true) : null;

                if (settings.Streaming)
                {
                    await HandleStreamingResponse(ctx, messages, model, maxTokens, temperature, topP,
                        settings, inferenceProvider, inferenceEndpoint, inferenceApiKey,
                        threadId, assistantId, settings.CollectionId, userMessageUtc, lastUserMessage,
                        retrievalStartUtc, retrievalDurationMs, retrievalContextText, retrievalChunks, promptSentUtc, inferenceSw,
                        promptTokenEstimate).ConfigureAwait(false);
                }
                else
                {
                    await HandleNonStreamingResponse(ctx, messages, model, maxTokens, temperature, topP,
                        settings, inferenceProvider, inferenceEndpoint, inferenceApiKey,
                        threadId, assistantId, settings.CollectionId, userMessageUtc, lastUserMessage,
                        retrievalStartUtc, retrievalDurationMs, retrievalContextText, retrievalChunks, promptSentUtc, inferenceSw,
                        promptTokenEstimate).ConfigureAwait(false);
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
                if (settings.EnableRag && !String.IsNullOrEmpty(settings.CollectionId) && !String.IsNullOrEmpty(lastUserMessage))
                {
                    List<RetrievalChunk> retrievedChunks = await Retrieval.RetrieveAsync(
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
                            FullTextMinimumScore = settings.FullTextMinimumScore
                        }).ConfigureAwait(false);
                    if (retrievedChunks != null) contextChunks.AddRange(retrievedChunks.Select(c => c.Content));
                }

                List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>(chatReq.Messages);

                bool hasSystemMessage = messages.Any(m => String.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase));
                if (!hasSystemMessage && !String.IsNullOrEmpty(settings.SystemPrompt))
                {
                    string fullSystemMessage = Inference.BuildSystemMessage(settings.SystemPrompt, contextChunks);
                    messages.Insert(0, new ChatCompletionMessage { Role = "system", Content = fullSystemMessage });
                }
                else if (hasSystemMessage && contextChunks.Count > 0)
                {
                    for (int i = 0; i < messages.Count; i++)
                    {
                        if (String.Equals(messages[i].Role, "system", StringComparison.OrdinalIgnoreCase))
                        {
                            messages[i] = new ChatCompletionMessage
                            {
                                Role = "system",
                                Content = Inference.BuildSystemMessage(messages[i].Content, contextChunks)
                            };
                            break;
                        }
                    }
                }

                string model = !String.IsNullOrEmpty(chatReq.Model) ? chatReq.Model : settings.Model;

                // Resolve inference endpoint details
                Enums.InferenceProviderEnum compactInferenceProvider = Settings.Inference.Provider;
                string inferenceEndpoint = Settings.Inference.Endpoint;
                string inferenceApiKey = Settings.Inference.ApiKey;

                if (!String.IsNullOrEmpty(settings.InferenceEndpointId))
                {
                    var resolved = await ResolveCompletionEndpointAsync(settings.InferenceEndpointId).ConfigureAwait(false);
                    if (resolved != null)
                    {
                        compactInferenceProvider = resolved.Value.Provider;
                        inferenceEndpoint = resolved.Value.Endpoint;
                        inferenceApiKey = resolved.Value.ApiKey;
                    }
                }

                // Force compaction
                messages = await CompactIfNeeded(messages, settings, compactInferenceProvider, model, inferenceEndpoint, inferenceApiKey, null, force: true).ConfigureAwait(false);

                // Filter out system messages for the response
                List<ChatCompletionMessage> responseMessages = messages
                    .Where(m => !String.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
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

                EnumerationResult<ChatHistory> result = await Database.ChatHistory.EnumerateAsync(query).ConfigureAwait(false);

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
                            RetrievalContext = h.RetrievalContext,
                            PromptSentUtc = h.PromptSentUtc,
                            PromptTokens = h.PromptTokens,
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

        #region Private-Methods

        private async Task HandleNonStreamingResponse(
            HttpContextBase ctx,
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            AssistantSettings settings,
            Enums.InferenceProviderEnum inferenceProvider,
            string inferenceEndpoint,
            string inferenceApiKey,
            string threadId = null,
            string assistantId = null,
            string collectionId = null,
            DateTime? userMessageUtc = null,
            string lastUserMessage = null,
            DateTime? retrievalStartUtc = null,
            double retrievalDurationMs = 0,
            string retrievalContext = null,
            List<RetrievalChunk> retrievalChunks = null,
            DateTime? promptSentUtc = null,
            Stopwatch inferenceSw = null,
            int promptTokens = 0)
        {
            InferenceResult inferenceResult = await Inference.GenerateResponseAsync(
                messages, model, maxTokens, temperature, topP,
                inferenceProvider, inferenceEndpoint, inferenceApiKey).ConfigureAwait(false);

            double timeToLastTokenMs = 0;
            if (inferenceSw != null)
            {
                inferenceSw.Stop();
                timeToLastTokenMs = Math.Round(inferenceSw.Elapsed.TotalMilliseconds, 2);
            }

            if (inferenceResult != null && inferenceResult.Success && !String.IsNullOrEmpty(inferenceResult.Content))
            {
                int responsePromptTokens = EstimateTokenCount(messages);
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
                        PromptTokens = responsePromptTokens,
                        CompletionTokens = completionTokens,
                        TotalTokens = responsePromptTokens + completionTokens,
                        ContextWindow = settings.ContextWindow
                    },
                    Retrieval = settings.EnableRag ? new ChatCompletionRetrieval
                    {
                        CollectionId = collectionId,
                        DurationMs = retrievalDurationMs,
                        ChunksReturned = retrievalChunks?.Count ?? 0,
                        Chunks = retrievalChunks ?? new List<RetrievalChunk>()
                    } : null
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(response)).ConfigureAwait(false);

                // Fire-and-forget chat history write
                if (!String.IsNullOrEmpty(threadId))
                {
                    _ = WriteChatHistoryAsync(threadId, assistantId, collectionId,
                        userMessageUtc ?? DateTime.UtcNow, lastUserMessage,
                        retrievalStartUtc, retrievalDurationMs, retrievalContext,
                        promptSentUtc, promptTokens, timeToLastTokenMs, timeToLastTokenMs,
                        inferenceResult.Content);
                }
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

        private async Task HandleStreamingResponse(
            HttpContextBase ctx,
            List<ChatCompletionMessage> messages,
            string model,
            int maxTokens,
            double temperature,
            double topP,
            AssistantSettings settings,
            Enums.InferenceProviderEnum inferenceProvider,
            string inferenceEndpoint,
            string inferenceApiKey,
            string threadId = null,
            string assistantId = null,
            string collectionId = null,
            DateTime? userMessageUtc = null,
            string lastUserMessage = null,
            DateTime? retrievalStartUtc = null,
            double retrievalDurationMs = 0,
            string retrievalContext = null,
            List<RetrievalChunk> retrievalChunks = null,
            DateTime? promptSentUtc = null,
            Stopwatch inferenceSw = null,
            int promptTokens = 0)
        {
            string completionId = IdGenerator.NewChatCompletionId();
            long created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double timeToFirstTokenMs = 0;
            bool firstTokenCaptured = false;

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.Add("Cache-Control", "no-cache");
            ctx.Response.Headers.Add("Connection", "keep-alive");
            ctx.Response.ChunkedTransfer = true;

            // Send initial chunk with role
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

            // Stream the inference response
            await Inference.GenerateResponseStreamingAsync(
                messages, model, maxTokens, temperature, topP,
                inferenceProvider, inferenceEndpoint, inferenceApiKey,
                onDelta: async (deltaContent) =>
                {
                    if (!firstTokenCaptured && inferenceSw != null)
                    {
                        timeToFirstTokenMs = Math.Round(inferenceSw.Elapsed.TotalMilliseconds, 2);
                        firstTokenCaptured = true;
                    }

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
                                Delta = new ChatCompletionMessage { Content = deltaContent }
                            }
                        }
                    };
                    await WriteSseEvent(ctx, deltaChunk).ConfigureAwait(false);
                },
                onComplete: async (fullContent) =>
                {
                    double timeToLastTokenMs = 0;
                    if (inferenceSw != null)
                    {
                        inferenceSw.Stop();
                        timeToLastTokenMs = Math.Round(inferenceSw.Elapsed.TotalMilliseconds, 2);
                    }

                    // Fire-and-forget chat history write
                    if (!String.IsNullOrEmpty(threadId))
                    {
                        _ = WriteChatHistoryAsync(threadId, assistantId, collectionId,
                            userMessageUtc ?? DateTime.UtcNow, lastUserMessage,
                            retrievalStartUtc, retrievalDurationMs, retrievalContext,
                            promptSentUtc, promptTokens, timeToFirstTokenMs, timeToLastTokenMs,
                            fullContent);
                    }

                    // Send finish chunk with usage
                    int finishPromptTokens = EstimateTokenCount(messages);
                    int finishCompletionTokens = EstimateTokenCount(fullContent);
                    ChatCompletionResponse finishChunk = new ChatCompletionResponse
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
                                Delta = new ChatCompletionMessage(),
                                FinishReason = "stop"
                            }
                        },
                        Usage = new ChatCompletionUsage
                        {
                            PromptTokens = finishPromptTokens,
                            CompletionTokens = finishCompletionTokens,
                            TotalTokens = finishPromptTokens + finishCompletionTokens,
                            ContextWindow = settings.ContextWindow
                        },
                        Retrieval = settings.EnableRag ? new ChatCompletionRetrieval
                        {
                            CollectionId = collectionId,
                            DurationMs = retrievalDurationMs,
                            ChunksReturned = retrievalChunks?.Count ?? 0,
                            Chunks = retrievalChunks ?? new List<RetrievalChunk>()
                        } : null
                    };
                    await WriteSseEvent(ctx, finishChunk).ConfigureAwait(false);

                    // Send [DONE]
                    byte[] doneBytes = Encoding.UTF8.GetBytes("data: [DONE]\n\n");
                    await ctx.Response.SendChunk(doneBytes, true).ConfigureAwait(false);
                },
                onError: async (errorMessage) =>
                {
                    Logging.Warn(_Header + "streaming error: " + errorMessage);
                    // Send error as a final chunk
                    byte[] errorBytes = Encoding.UTF8.GetBytes("data: [DONE]\n\n");
                    await ctx.Response.SendChunk(errorBytes, true).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        private async Task WriteSseEvent(HttpContextBase ctx, ChatCompletionResponse chunk)
        {
            string json = JsonSerializer.Serialize(chunk, _SseJsonOptions);
            string sseData = "data: " + json + "\n\n";
            byte[] bytes = Encoding.UTF8.GetBytes(sseData);
            await ctx.Response.SendChunk(bytes, false).ConfigureAwait(false);
        }

        private async Task<List<ChatCompletionMessage>> CompactIfNeeded(
            List<ChatCompletionMessage> messages,
            AssistantSettings settings,
            Enums.InferenceProviderEnum inferenceProvider,
            string model,
            string inferenceEndpoint,
            string inferenceApiKey,
            HttpContextBase streamingCtx,
            bool force = false)
        {
            int estimatedTokens = EstimateTokenCount(messages);
            int availableTokens = settings.ContextWindow - settings.MaxTokens;

            if (!force && (estimatedTokens <= availableTokens || messages.Count <= 3))
            {
                return messages;
            }

            Logging.Info(_Header + "compacting conversation (" + estimatedTokens + " estimated tokens, " + availableTokens + " available)");

            // Send compaction status via SSE if streaming
            if (streamingCtx != null)
            {
                try
                {
                    string completionId = IdGenerator.NewChatCompletionId();
                    long created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    streamingCtx.Response.StatusCode = 200;
                    streamingCtx.Response.ContentType = "text/event-stream";
                    streamingCtx.Response.Headers.Add("Cache-Control", "no-cache");
                    streamingCtx.Response.Headers.Add("Connection", "keep-alive");
                    streamingCtx.Response.ChunkedTransfer = true;

                    ChatCompletionResponse statusChunk = new ChatCompletionResponse
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
                                Delta = new ChatCompletionMessage { Content = "" }
                            }
                        },
                        Status = "Compacting the conversation..."
                    };
                    await WriteSseEvent(streamingCtx, statusChunk).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logging.Warn(_Header + "failed to send compaction status: " + e.Message);
                }
            }

            try
            {
                // Separate messages
                ChatCompletionMessage systemMessage = null;
                List<ChatCompletionMessage> compactableMessages = new List<ChatCompletionMessage>();
                ChatCompletionMessage lastUserMessage = null;

                // Find system message
                if (messages.Count > 0 && String.Equals(messages[0].Role, "system", StringComparison.OrdinalIgnoreCase))
                {
                    systemMessage = messages[0];
                }

                // Find last user message
                for (int i = messages.Count - 1; i >= 0; i--)
                {
                    if (String.Equals(messages[i].Role, "user", StringComparison.OrdinalIgnoreCase))
                    {
                        lastUserMessage = messages[i];
                        break;
                    }
                }

                // Collect compactable messages (everything between system and last user message)
                int startIdx = systemMessage != null ? 1 : 0;
                for (int i = startIdx; i < messages.Count; i++)
                {
                    if (messages[i] == lastUserMessage) continue;
                    compactableMessages.Add(messages[i]);
                }

                if (compactableMessages.Count == 0)
                {
                    return messages;
                }

                // Build summary prompt
                StringBuilder conversationText = new StringBuilder();
                foreach (ChatCompletionMessage msg in compactableMessages)
                {
                    conversationText.AppendLine(msg.Role + ": " + msg.Content);
                }

                List<ChatCompletionMessage> summaryMessages = new List<ChatCompletionMessage>
                {
                    new ChatCompletionMessage
                    {
                        Role = "system",
                        Content = "You are a helpful assistant that summarizes conversations concisely."
                    },
                    new ChatCompletionMessage
                    {
                        Role = "user",
                        Content = "Summarize the following conversation preserving key facts, decisions, and context:\n\n" + conversationText.ToString()
                    }
                };

                InferenceResult summaryResult = await Inference.GenerateResponseAsync(
                    summaryMessages, model, 1024, 0.3, 1.0,
                    inferenceProvider, inferenceEndpoint, inferenceApiKey).ConfigureAwait(false);

                if (summaryResult == null || !summaryResult.Success || String.IsNullOrEmpty(summaryResult.Content))
                {
                    Logging.Warn(_Header + "compaction summary failed, proceeding with original messages");
                    return messages;
                }

                // Rebuild messages
                List<ChatCompletionMessage> compactedMessages = new List<ChatCompletionMessage>();
                if (systemMessage != null)
                {
                    compactedMessages.Add(systemMessage);
                }
                compactedMessages.Add(new ChatCompletionMessage
                {
                    Role = "system",
                    Content = "[Conversation Summary]\n" + summaryResult.Content
                });
                if (lastUserMessage != null)
                {
                    compactedMessages.Add(lastUserMessage);
                }

                Logging.Info(_Header + "compaction complete: " + messages.Count + " messages -> " + compactedMessages.Count + " messages");
                return compactedMessages;
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "compaction failed: " + e.Message + ", proceeding with original messages");
                return messages;
            }
        }

        private async Task WriteChatHistoryAsync(
            string threadId, string assistantId, string collectionId,
            DateTime userMessageUtc, string userMessage,
            DateTime? retrievalStartUtc, double retrievalDurationMs, string retrievalContext,
            DateTime? promptSentUtc, int promptTokens, double timeToFirstTokenMs, double timeToLastTokenMs,
            string assistantResponse)
        {
            try
            {
                ChatHistory history = new ChatHistory();
                history.Id = IdGenerator.NewChatHistoryId();
                history.ThreadId = threadId;
                history.AssistantId = assistantId;
                history.CollectionId = collectionId;
                history.UserMessageUtc = userMessageUtc;
                history.UserMessage = userMessage;
                history.RetrievalStartUtc = retrievalStartUtc;
                history.RetrievalDurationMs = retrievalDurationMs;
                history.RetrievalContext = retrievalContext;
                history.PromptSentUtc = promptSentUtc;
                history.PromptTokens = promptTokens;
                history.TimeToFirstTokenMs = timeToFirstTokenMs;
                history.TimeToLastTokenMs = timeToLastTokenMs;
                history.AssistantResponse = assistantResponse;

                await Database.ChatHistory.CreateAsync(history).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "failed to write chat history: " + e.Message);
            }
        }

        private struct ResolvedEndpoint
        {
            public Enums.InferenceProviderEnum Provider;
            public string Endpoint;
            public string ApiKey;
        }

        private async Task<ResolvedEndpoint?> ResolveCompletionEndpointAsync(string endpointId)
        {
            try
            {
                string url = Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/completion/" + endpointId;
                using (HttpClient client = new HttpClient())
                {
                    if (!String.IsNullOrEmpty(Settings.Chunking.AccessKey))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Settings.Chunking.AccessKey);
                    }

                    HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        Logging.Warn(_Header + "failed to resolve completion endpoint " + endpointId + ": " + (int)response.StatusCode);
                        return null;
                    }

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    JsonElement ep = JsonSerializer.Deserialize<JsonElement>(body, _SseJsonOptions);

                    string apiFormat = ep.TryGetProperty("ApiFormat", out JsonElement af) ? af.GetString() : null;
                    string epUrl = ep.TryGetProperty("Endpoint", out JsonElement eu) ? eu.GetString() : null;
                    string apiKey = ep.TryGetProperty("ApiKey", out JsonElement ak) ? ak.GetString() : null;

                    Enums.InferenceProviderEnum provider = Enums.InferenceProviderEnum.Ollama;
                    if (!String.IsNullOrEmpty(apiFormat) && apiFormat.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
                        provider = Enums.InferenceProviderEnum.OpenAI;

                    return new ResolvedEndpoint
                    {
                        Provider = provider,
                        Endpoint = epUrl ?? Settings.Inference.Endpoint,
                        ApiKey = apiKey ?? Settings.Inference.ApiKey
                    };
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception resolving completion endpoint " + endpointId + ": " + e.Message);
                return null;
            }
        }

        private static int EstimateTokenCount(string text)
        {
            if (String.IsNullOrEmpty(text)) return 0;
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        private static int EstimateTokenCount(List<ChatCompletionMessage> messages)
        {
            if (messages == null) return 0;
            int total = 0;
            foreach (ChatCompletionMessage msg in messages)
            {
                total += 4; // message overhead
                total += EstimateTokenCount(msg.Content);
            }
            return total;
        }

        #endregion
    }
}
