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
    /// Provides shared execution helpers for chat handlers.
    /// </summary>
    public abstract class ChatHandlerExecutionBase : HandlerBase
    {
        private protected static readonly string _Header = "[ChatHandler] ";

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

        private protected static readonly JsonSerializerOptions _SseJsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };

        private protected static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };


        /// <summary>
        /// Instantiate the chat handler execution base.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">Application settings.</param>
        /// <param name="authentication">Authentication service.</param>
        /// <param name="storage">Storage service.</param>
        /// <param name="ingestion">Ingestion service.</param>
        /// <param name="retrieval">Retrieval service.</param>
        /// <param name="inference">Inference service.</param>
        protected ChatHandlerExecutionBase(
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

        #region Private-Methods

        private protected async Task HandleNonStreamingResponse(
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
            string inferenceEndpointId,
            int inferenceMaxConcurrentRequests,
            string tenantId = null,
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
            int promptTokens = 0,
            double endpointResolutionMs = 0,
            double compactionMs = 0,
            string retrievalGateDecision = null,
            double retrievalGateDurationMs = 0,
            List<CitationSource> citationSources = null,
            string queryRewriteResult = null,
            double queryRewriteDurationMs = 0,
            double rerankDurationMs = 0,
            int rerankInputCount = 0,
            int rerankOutputCount = 0,
            string metadataFilterJson = null,
            string traceId = null,
            string requestHistoryId = null,
            AssistantPerformanceStage retrievalGateTelemetry = null,
            AssistantPerformanceStage queryRewriteTelemetry = null,
            AssistantPerformanceStage rerankTelemetry = null,
            int retrievalQueryCount = 1)
        {
            InferenceResult inferenceResult = await GenerateWithCompletionEndpointLimitAsync(
                messages, model, maxTokens, temperature, topP,
                inferenceProvider, inferenceEndpoint, inferenceApiKey,
                inferenceEndpointId, inferenceMaxConcurrentRequests).ConfigureAwait(false);

            double timeToLastTokenMs = 0;
            if (inferenceSw != null)
            {
                inferenceSw.Stop();
                timeToLastTokenMs = Math.Round(inferenceSw.Elapsed.TotalMilliseconds, 2);
            }

            if (inferenceResult != null && inferenceResult.Success && !String.IsNullOrEmpty(inferenceResult.Content))
            {
                // Strip any model-generated bibliography before building the response
                string responseContent = (settings.EnableCitations && citationSources != null && citationSources.Count > 0)
                    ? CitationExtractor.StripBibliography(inferenceResult.Content)
                    : inferenceResult.Content;

                int responsePromptTokens = EstimateTokenCount(messages);
                int completionTokens = EstimateTokenCount(responseContent);

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
                            Message = new ChatCompletionMessage { Role = "assistant", Content = responseContent },
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
                        Chunks = retrievalChunks ?? new List<RetrievalChunk>(),
                        RerankDurationMs = rerankDurationMs,
                        RerankInputCount = rerankInputCount,
                        RerankOutputCount = rerankOutputCount
                    } : null,
                    Citations = (settings.EnableCitations && citationSources != null && citationSources.Count > 0)
                        ? CitationExtractor.Extract(citationSources, responseContent)
                        : null
                };

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(response)).ConfigureAwait(false);

                // Fire-and-forget chat history write
                if (!String.IsNullOrEmpty(threadId))
                {
                    ChatHistory history = await WriteChatHistoryAsync(tenantId, threadId, assistantId, collectionId,
                        userMessageUtc ?? DateTime.UtcNow, lastUserMessage,
                        retrievalStartUtc, retrievalDurationMs, retrievalContext,
                        promptSentUtc, promptTokens,
                        endpointResolutionMs, compactionMs, 0,
                        timeToLastTokenMs, timeToLastTokenMs,
                        inferenceResult.Content,
                        completionTokens,
                        retrievalGateDecision, retrievalGateDurationMs,
                        queryRewriteResult, queryRewriteDurationMs,
                        rerankDurationMs, rerankInputCount, rerankOutputCount,
                        metadataFilterJson,
                        "web",
                        traceId,
                        requestHistoryId,
                        inferenceResult.Telemetry,
                        retrievalGateTelemetry,
                        queryRewriteTelemetry,
                        rerankTelemetry,
                        retrievalQueryCount,
                        retrievalChunks?.Count ?? 0);
                    if (history != null)
                        SetChatHistoryId(ctx, history.Id);
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

        private protected async Task HandleStreamingResponse(
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
            string inferenceEndpointId,
            int inferenceMaxConcurrentRequests,
            string tenantId = null,
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
            int promptTokens = 0,
            double endpointResolutionMs = 0,
            double compactionMs = 0,
            string retrievalGateDecision = null,
            double retrievalGateDurationMs = 0,
            List<CitationSource> citationSources = null,
            string queryRewriteResult = null,
            double queryRewriteDurationMs = 0,
            double rerankDurationMs = 0,
            int rerankInputCount = 0,
            int rerankOutputCount = 0,
            string metadataFilterJson = null,
            string traceId = null,
            string requestHistoryId = null,
            AssistantPerformanceStage retrievalGateTelemetry = null,
            AssistantPerformanceStage queryRewriteTelemetry = null,
            AssistantPerformanceStage rerankTelemetry = null,
            int retrievalQueryCount = 1)
        {
            string completionId = IdGenerator.NewChatCompletionId();
            long created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double timeToFirstTokenMs = 0;
            double inferenceConnectionMs = 0;
            bool firstTokenCaptured = false;
            AssistantPerformanceStage finalInferenceTelemetry = null;

            ctx.Response.StatusCode = 200;
            ctx.Response.ServerSentEvents = true;

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
            await GenerateStreamingWithCompletionEndpointLimitAsync(
                messages, model, maxTokens, temperature, topP,
                inferenceProvider, inferenceEndpoint, inferenceApiKey,
                inferenceEndpointId, inferenceMaxConcurrentRequests,
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

                    Logging.Info(_Header + "inference timing: connection=" + inferenceConnectionMs + "ms, TTFT=" + timeToFirstTokenMs + "ms, TTLT=" + timeToLastTokenMs + "ms");

                    // Strip any model-generated bibliography for citation extraction
                    string cleanedContent = (settings.EnableCitations && citationSources != null && citationSources.Count > 0)
                        ? CitationExtractor.StripBibliography(fullContent)
                        : fullContent;

                    // Send finish chunk with usage
                    int finishPromptTokens = EstimateTokenCount(messages);
                    int finishCompletionTokens = EstimateTokenCount(fullContent);

                    // Fire-and-forget chat history write (use cleaned content)
                    if (!String.IsNullOrEmpty(threadId))
                    {
                        ChatHistory history = await WriteChatHistoryAsync(tenantId, threadId, assistantId, collectionId,
                            userMessageUtc ?? DateTime.UtcNow, lastUserMessage,
                            retrievalStartUtc, retrievalDurationMs, retrievalContext,
                            promptSentUtc, promptTokens,
                            endpointResolutionMs, compactionMs, inferenceConnectionMs,
                            timeToFirstTokenMs, timeToLastTokenMs,
                            cleanedContent,
                            finishCompletionTokens,
                            retrievalGateDecision, retrievalGateDurationMs,
                            queryRewriteResult, queryRewriteDurationMs,
                            rerankDurationMs, rerankInputCount, rerankOutputCount,
                            metadataFilterJson,
                            "web",
                            traceId,
                            requestHistoryId,
                            finalInferenceTelemetry,
                            retrievalGateTelemetry,
                            queryRewriteTelemetry,
                            rerankTelemetry,
                            retrievalQueryCount,
                            retrievalChunks?.Count ?? 0);
                        if (history != null)
                            SetChatHistoryId(ctx, history.Id);
                    }
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
                            Chunks = retrievalChunks ?? new List<RetrievalChunk>(),
                            RerankDurationMs = rerankDurationMs,
                            RerankInputCount = rerankInputCount,
                            RerankOutputCount = rerankOutputCount
                        } : null,
                        Citations = (settings.EnableCitations && citationSources != null && citationSources.Count > 0)
                            ? CitationExtractor.Extract(citationSources, cleanedContent)
                            : null
                    };
                    await WriteSseEvent(ctx, finishChunk).ConfigureAwait(false);

                    // Send [DONE]
                    await ctx.Response.SendEvent(new ServerSentEvent { Data = "[DONE]" }, true).ConfigureAwait(false);
                },
                onError: async (errorMessage) =>
                {
                    Logging.Warn(_Header + "streaming error: " + errorMessage);
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
                                    Content = "AssistantHub could not generate a response: " + errorMessage
                                },
                                FinishReason = "stop"
                            }
                        },
                        Status = "Error"
                    };
                    await WriteSseEvent(ctx, errorChunk).ConfigureAwait(false);
                    await ctx.Response.SendEvent(new ServerSentEvent { Data = "[DONE]" }, true).ConfigureAwait(false);
                },
                onConnectionEstablished: () =>
                {
                    if (inferenceSw != null)
                    {
                        inferenceConnectionMs = Math.Round(inferenceSw.Elapsed.TotalMilliseconds, 2);
                        Logging.Info(_Header + "inference connection established in " + inferenceConnectionMs + " ms");
                    }
                },
                onTelemetry: telemetry =>
                {
                    finalInferenceTelemetry = telemetry;
                }).ConfigureAwait(false);
        }

        private protected async Task WriteSseEvent(HttpContextBase ctx, ChatCompletionResponse chunk)
        {
            string json = JsonSerializer.Serialize(chunk, _SseJsonOptions);
            await ctx.Response.SendEvent(new ServerSentEvent { Data = json }, false).ConfigureAwait(false);
        }

        private protected TelemetryContext EnsureTelemetryContext(HttpContextBase ctx)
        {
            Dictionary<string, object> metadata = GetMetadata(ctx);

            string traceId = metadata.TryGetValue("traceId", out object traceValue)
                ? traceValue as string
                : null;
            if (String.IsNullOrWhiteSpace(traceId))
            {
                traceId = IdGenerator.NewTraceId();
                metadata["traceId"] = traceId;
            }

            string requestHistoryId = metadata.TryGetValue("requestHistoryId", out object requestHistoryValue)
                ? requestHistoryValue as string
                : null;
            if (String.IsNullOrWhiteSpace(requestHistoryId))
            {
                requestHistoryId = IdGenerator.NewRequestHistoryId();
                metadata["requestHistoryId"] = requestHistoryId;
            }

            return new TelemetryContext
            {
                TraceId = traceId,
                RequestHistoryId = requestHistoryId
            };
        }

        private protected void SetChatHistoryId(HttpContextBase ctx, string chatHistoryId)
        {
            if (ctx == null || String.IsNullOrWhiteSpace(chatHistoryId)) return;
            GetMetadata(ctx)["chatHistoryId"] = chatHistoryId;
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
                    streamingCtx.Response.ServerSentEvents = true;

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

                InferenceResult summaryResult = await GenerateWithCompletionEndpointLimitAsync(
                    summaryMessages, model, 1024, 0.3, 1.0,
                    inferenceProvider, inferenceEndpoint, inferenceApiKey,
                    inferenceEndpointId, inferenceMaxConcurrentRequests).ConfigureAwait(false);

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

        private protected async Task<ChatHistory> WriteChatHistoryAsync(
            string tenantId, string threadId, string assistantId, string collectionId,
            DateTime userMessageUtc, string userMessage,
            DateTime? retrievalStartUtc, double retrievalDurationMs, string retrievalContext,
            DateTime? promptSentUtc, int promptTokens,
            double endpointResolutionDurationMs, double compactionDurationMs, double inferenceConnectionDurationMs,
            double timeToFirstTokenMs, double timeToLastTokenMs,
            string assistantResponse,
            int completionTokens = 0,
            string retrievalGateDecision = null, double retrievalGateDurationMs = 0,
            string queryRewriteResult = null, double queryRewriteDurationMs = 0,
            double rerankDurationMs = 0, int rerankInputCount = 0, int rerankOutputCount = 0,
            string metadataFilterJson = null,
            string origin = "web",
            string traceId = null,
            string requestHistoryId = null,
            AssistantPerformanceStage finalInferenceTelemetry = null,
            AssistantPerformanceStage retrievalGateTelemetry = null,
            AssistantPerformanceStage queryRewriteTelemetry = null,
            AssistantPerformanceStage rerankTelemetry = null,
            int retrievalQueryCount = 1,
            int retrievalChunksReturned = 0)
        {
            try
            {
                ChatHistory history = new ChatHistory();
                history.Id = IdGenerator.NewChatHistoryId();
                history.TenantId = tenantId;
                history.ThreadId = threadId;
                history.AssistantId = assistantId;
                history.CollectionId = collectionId;
                history.UserMessageUtc = userMessageUtc;
                history.UserMessage = userMessage;
                history.RetrievalStartUtc = retrievalStartUtc;
                history.RetrievalDurationMs = retrievalDurationMs;
                history.RetrievalGateDecision = retrievalGateDecision;
                history.RetrievalGateDurationMs = retrievalGateDurationMs;
                history.QueryRewriteResult = queryRewriteResult;
                history.QueryRewriteDurationMs = queryRewriteDurationMs;
                history.RerankDurationMs = rerankDurationMs;
                history.RerankInputCount = rerankInputCount;
                history.RerankOutputCount = rerankOutputCount;
                history.RetrievalContext = retrievalContext;
                history.PromptSentUtc = promptSentUtc;
                history.PromptTokens = promptTokens;
                history.EndpointResolutionDurationMs = endpointResolutionDurationMs;
                history.CompactionDurationMs = compactionDurationMs;
                history.InferenceConnectionDurationMs = inferenceConnectionDurationMs;
                history.TimeToFirstTokenMs = timeToFirstTokenMs;
                history.TimeToLastTokenMs = timeToLastTokenMs;
                history.MetadataFilter = metadataFilterJson;
                history.AssistantResponse = assistantResponse;
                history.Origin = origin;
                history.TraceId = traceId;
                history.RequestHistoryId = requestHistoryId;
                history.PerformanceSchemaVersion = 1;

                history.CompletionTokens = completionTokens;

                // Compute tokens per second (overall): completion tokens / TTLT in seconds
                if (completionTokens > 0 && timeToLastTokenMs > 0)
                    history.TokensPerSecondOverall = Math.Round(completionTokens / (timeToLastTokenMs / 1000.0), 2);

                // Compute tokens per second (generation only): completion tokens / (TTLT - TTFT) in seconds
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

                await Database.ChatHistory.CreateAsync(history).ConfigureAwait(false);

                if (Database.ChatHistoryPerformanceEvent != null)
                {
                    List<ChatHistoryPerformanceEvent> events =
                        AssistantPerformanceTelemetryBuilder.ToEvents(telemetry, history.TenantId);
                    await Database.ChatHistoryPerformanceEvent.CreateManyAsync(events).ConfigureAwait(false);
                }

                return history;
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "failed to write chat history: " + e.Message);
                return null;
            }
        }

        private protected static string ResolveUtilityInferenceEndpointId(string utilityEndpointId, string fallbackEndpointId)
        {
            return !String.IsNullOrWhiteSpace(utilityEndpointId) ? utilityEndpointId : fallbackEndpointId;
        }

        private protected async Task<ResolvedEndpoint?> ResolveCompletionEndpointAsync(string endpointId)
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
                    PartioEndpointConfig ep = JsonSerializer.Deserialize<PartioEndpointConfig>(body, _SseJsonOptions);

                    Enums.InferenceProviderEnum provider = InferenceProviderHelper.FromApiFormat(ep?.ApiFormat, Enums.InferenceProviderEnum.Ollama);

                    return new ResolvedEndpoint
                    {
                        EndpointId = endpointId,
                        Provider = provider,
                        Endpoint = ep?.Endpoint ?? Settings.Inference.Endpoint,
                        ApiKey = ep?.ApiKey ?? Settings.Inference.ApiKey,
                        Model = ep?.Model,
                        MaxConcurrentRequests = Math.Max(1, ep?.MaxConcurrentRequests ?? 1)
                    };
                }
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception resolving completion endpoint " + endpointId + ": " + e.Message);
                return null;
            }
        }

        private protected ResolvedEndpoint BuildFallbackCompletionEndpoint(string endpointId)
        {
            return new ResolvedEndpoint
            {
                EndpointId = endpointId,
                Provider = Settings.Inference.Provider,
                Endpoint = Settings.Inference.Endpoint,
                ApiKey = Settings.Inference.ApiKey,
                Model = Settings.Inference.DefaultModel,
                MaxConcurrentRequests = 1
            };
        }

        private protected async Task<ResolvedEndpoint> ResolveCompletionEndpointOrFallbackAsync(string endpointId)
        {
            ResolvedEndpoint? resolved = await ResolveCompletionEndpointAsync(endpointId).ConfigureAwait(false);
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
            int maxConcurrentRequests)
        {
            int max = Math.Max(1, maxConcurrentRequests);
            Stopwatch waitSw = Stopwatch.StartNew();
            using (IDisposable lease = await EndpointConcurrencyLimiter.AcquireAsync("completion", endpointId, max).ConfigureAwait(false))
            {
                waitSw.Stop();
                if (waitSw.ElapsedMilliseconds > 0)
                {
                    Logging.Info(
                        _Header +
                        "completion endpoint concurrency slot acquired: " +
                        EndpointConcurrencyLimiter.BuildKey("completion", endpointId) +
                        ", maxConcurrentRequests=" + max +
                        ", waitedMs=" + waitSw.ElapsedMilliseconds);
                }

                InferenceResult result = await Inference.GenerateResponseAsync(
                    messages, model, maxTokens, temperature, topP,
                    provider, endpoint, apiKey).ConfigureAwait(false);

                AttachEndpointTelemetry(result?.Telemetry, endpointId, endpoint, provider, model, max, waitSw.Elapsed.TotalMilliseconds);
                return result;
            }
        }

        private protected async Task GenerateStreamingWithCompletionEndpointLimitAsync(
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
            Func<string, Task> onDelta,
            Func<string, Task> onComplete,
            Func<string, Task> onError,
            Action onConnectionEstablished = null,
            Action<AssistantPerformanceStage> onTelemetry = null)
        {
            int max = Math.Max(1, maxConcurrentRequests);
            Stopwatch waitSw = Stopwatch.StartNew();
            using (IDisposable lease = await EndpointConcurrencyLimiter.AcquireAsync("completion", endpointId, max).ConfigureAwait(false))
            {
                waitSw.Stop();
                if (waitSw.ElapsedMilliseconds > 0)
                {
                    Logging.Info(
                        _Header +
                        "completion endpoint concurrency slot acquired: " +
                        EndpointConcurrencyLimiter.BuildKey("completion", endpointId) +
                        ", maxConcurrentRequests=" + max +
                        ", waitedMs=" + waitSw.ElapsedMilliseconds);
                }

                await Inference.GenerateResponseStreamingAsync(
                    messages, model, maxTokens, temperature, topP,
                    provider, endpoint, apiKey,
                    onDelta, onComplete, onError, onConnectionEstablished,
                    telemetry =>
                    {
                        AttachEndpointTelemetry(telemetry, endpointId, endpoint, provider, model, max, waitSw.Elapsed.TotalMilliseconds);
                        onTelemetry?.Invoke(telemetry);
                    }).ConfigureAwait(false);
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
                    Content = Inference.BuildSystemMessage(
                        baseSystemPrompt,
                        retrievalChunks.Select(c => c.MergedContent).ToList(),
                        settings.EnableCitations,
                        chunkLabels)
                };

                estimatedTokens = EstimateTokenCount(messages);
            }

            if (retrievalChunks.Count < originalChunkCount)
            {
                Logging.Warn(
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

        private protected static int EstimateTokenCount(List<ChatCompletionMessage> messages)
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
