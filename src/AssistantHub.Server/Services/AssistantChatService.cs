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
    public class AssistantChatService
    {
        private static readonly string _Header = "[AssistantChatService] ";

        private static readonly string _DefaultQueryRewritePrompt =
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

        private static readonly string _RetrievalGatePrompt =
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

        private static readonly string _DefaultRerankPrompt =
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

        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly DatabaseDriverBase _Database;
        private readonly LoggingModule _Logging;
        private readonly AssistantHubSettings _Settings;
        private readonly RetrievalService _Retrieval;
        private readonly InferenceService _Inference;

        private class RerankResult
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("score")]
            public double Score { get; set; }
        }

        private ChatMetadataFilter BuildEffectiveMetadataFilter(AssistantSettings settings, ChatMetadataFilter requestFilter)
        {
            ChatMetadataFilter assistantFilter = null;
            bool hasAssistantLabelFilter = !String.IsNullOrEmpty(settings.RetrievalLabelFilter);
            bool hasAssistantTagFilter = !String.IsNullOrEmpty(settings.RetrievalTagFilter);

            if (hasAssistantLabelFilter || hasAssistantTagFilter)
            {
                assistantFilter = new ChatMetadataFilter();
                if (hasAssistantLabelFilter)
                {
                    var labelFilter = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(settings.RetrievalLabelFilter, _JsonOptions);
                    if (labelFilter != null)
                    {
                        labelFilter.TryGetValue("Required", out var reqLabels);
                        labelFilter.TryGetValue("Excluded", out var exclLabels);
                        assistantFilter.RequiredLabels = reqLabels;
                        assistantFilter.ExcludedLabels = exclLabels;
                    }
                }

                if (hasAssistantTagFilter)
                {
                    var tagFilter = JsonSerializer.Deserialize<Dictionary<string, List<ChatTagCondition>>>(settings.RetrievalTagFilter, _JsonOptions);
                    if (tagFilter != null)
                    {
                        tagFilter.TryGetValue("Required", out var reqTags);
                        tagFilter.TryGetValue("Excluded", out var exclTags);
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

        private string GetLastUserMessage(List<ChatCompletionMessage> messages)
        {
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (String.Equals(messages[i].Role, "user", StringComparison.OrdinalIgnoreCase))
                    return messages[i].Content;
            }

            return null;
        }

        private string BuildRetrievalGatePrompt(List<ChatCompletionMessage> messages, string lastUserMessage)
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

        private async Task<List<ChatCompletionMessage>> CompactIfNeeded(
            List<ChatCompletionMessage> messages,
            AssistantSettings settings,
            Enums.InferenceProviderEnum inferenceProvider,
            string model,
            string inferenceEndpoint,
            string inferenceApiKey,
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

                InferenceResult summaryResult = await _Inference.GenerateResponseAsync(
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
                    inferenceApiKey).ConfigureAwait(false);

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

        private struct ResolvedEndpoint
        {
            public Enums.InferenceProviderEnum Provider;
            public string Endpoint;
            public string ApiKey;
        }

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
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval));
            _Inference = inference ?? throw new ArgumentNullException(nameof(inference));
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
            bool shouldRetrieve = true;

            if (settings.EnableRag && settings.EnableRetrievalGate
                && !String.IsNullOrEmpty(settings.CollectionId) && !String.IsNullOrEmpty(lastUserMessage))
            {
                int userMessageCount = request.Messages.Count(m =>
                    String.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase));

                if (userMessageCount > 1)
                {
                    string gatePrompt = BuildRetrievalGatePrompt(request.Messages, lastUserMessage);
                    var gateEndpoint = await ResolveCompletionEndpointAsync(settings.InferenceEndpointId, token).ConfigureAwait(false);

                    Stopwatch gateSw = Stopwatch.StartNew();
                    try
                    {
                        InferenceResult gateResult = await _Inference.GenerateResponseAsync(
                            new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "system", Content = gatePrompt } },
                            settings.Model,
                            3,
                            0.0,
                            1.0,
                            gateEndpoint?.Provider ?? _Settings.Inference.Provider,
                            gateEndpoint?.Endpoint ?? _Settings.Inference.Endpoint,
                            gateEndpoint?.ApiKey ?? _Settings.Inference.ApiKey).ConfigureAwait(false);

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
            List<string> retrievalQueries = !String.IsNullOrEmpty(lastUserMessage) ? new List<string> { lastUserMessage } : new List<string>();

            if (settings.EnableRag && settings.EnableQueryRewrite && shouldRetrieve
                && !String.IsNullOrEmpty(settings.CollectionId) && !String.IsNullOrEmpty(lastUserMessage))
            {
                var rewriteEndpoint = await ResolveCompletionEndpointAsync(settings.InferenceEndpointId, token).ConfigureAwait(false);
                string rewritePromptTemplate = !String.IsNullOrEmpty(settings.QueryRewritePrompt)
                    ? settings.QueryRewritePrompt
                    : _DefaultQueryRewritePrompt;

                string rewritePrompt = rewritePromptTemplate.Replace("{prompt}", lastUserMessage);
                Stopwatch rewriteSw = Stopwatch.StartNew();

                try
                {
                    InferenceResult rewriteResult = await _Inference.GenerateResponseAsync(
                        new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "system", Content = rewritePrompt } },
                        settings.Model,
                        512,
                        0.7,
                        1.0,
                        rewriteEndpoint?.Provider ?? _Settings.Inference.Provider,
                        rewriteEndpoint?.Endpoint ?? _Settings.Inference.Endpoint,
                        rewriteEndpoint?.ApiKey ?? _Settings.Inference.ApiKey).ConfigureAwait(false);

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

            if (settings.EnableRag && settings.EnableReranking && shouldRetrieve && retrievalChunks.Count > 0)
            {
                rerankInputCount = retrievalChunks.Count;
                Stopwatch rerankSw = Stopwatch.StartNew();

                try
                {
                    var rerankEndpoint = await ResolveCompletionEndpointAsync(settings.InferenceEndpointId, token).ConfigureAwait(false);
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

                    InferenceResult rerankResult = await _Inference.GenerateResponseAsync(
                        new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "system", Content = rerankPrompt } },
                        settings.Model,
                        512,
                        0.0,
                        1.0,
                        rerankEndpoint?.Provider ?? _Settings.Inference.Provider,
                        rerankEndpoint?.Endpoint ?? _Settings.Inference.Endpoint,
                        rerankEndpoint?.ApiKey ?? _Settings.Inference.ApiKey).ConfigureAwait(false);

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
            bool hasSystemMessage = messages.Any(m => String.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase));
            if (!hasSystemMessage && !String.IsNullOrEmpty(settings.SystemPrompt))
            {
                messages.Insert(0, new ChatCompletionMessage
                {
                    Role = "system",
                    Content = _Inference.BuildSystemMessage(settings.SystemPrompt, contextChunks, settings.EnableCitations, chunkLabels)
                });
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
                            Content = _Inference.BuildSystemMessage(messages[i].Content, contextChunks, settings.EnableCitations, chunkLabels)
                        };
                        break;
                    }
                }
            }

            string model = !String.IsNullOrEmpty(request.Model) ? request.Model : settings.Model;
            double temperature = request.Temperature ?? settings.Temperature;
            double topP = request.TopP ?? settings.TopP;
            int maxTokens = request.MaxTokens ?? settings.MaxTokens;

            Enums.InferenceProviderEnum inferenceProvider = _Settings.Inference.Provider;
            string inferenceEndpoint = _Settings.Inference.Endpoint;
            string inferenceApiKey = _Settings.Inference.ApiKey;

            double endpointResolutionMs = 0;
            if (!String.IsNullOrEmpty(settings.InferenceEndpointId))
            {
                Stopwatch endpointSw = Stopwatch.StartNew();
                var resolved = await ResolveCompletionEndpointAsync(settings.InferenceEndpointId, token).ConfigureAwait(false);
                endpointSw.Stop();
                endpointResolutionMs = Math.Round(endpointSw.Elapsed.TotalMilliseconds, 2);
                if (resolved != null)
                {
                    inferenceProvider = resolved.Value.Provider;
                    inferenceEndpoint = resolved.Value.Endpoint;
                    inferenceApiKey = resolved.Value.ApiKey;
                }
            }

            Stopwatch compactionSw = Stopwatch.StartNew();
            messages = await CompactIfNeeded(messages, settings, inferenceProvider, model, inferenceEndpoint, inferenceApiKey, token).ConfigureAwait(false);
            compactionSw.Stop();
            double compactionMs = Math.Round(compactionSw.Elapsed.TotalMilliseconds, 2);

            int promptTokenEstimate = EstimateTokenCount(messages);
            DateTime promptSentUtc = DateTime.UtcNow;
            Stopwatch inferenceSw = Stopwatch.StartNew();

            InferenceResult inferenceResult = await _Inference.GenerateResponseAsync(
                messages, model, maxTokens, temperature, topP,
                inferenceProvider, inferenceEndpoint, inferenceApiKey).ConfigureAwait(false);

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

            if (!String.IsNullOrEmpty(request.ThreadId))
            {
                _ = WriteChatHistoryAsync(
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
                    token);
            }

            return new AssistantChatExecutionResult
            {
                Success = true,
                Assistant = assistant,
                AssistantSettings = settings,
                Response = response,
                CanonicalResponseText = canonicalResponseText
            };
        }

        private async Task WriteChatHistoryAsync(
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
                    Origin = origin
                };

                if (completionTokens > 0 && timeToLastTokenMs > 0)
                    history.TokensPerSecondOverall = Math.Round(completionTokens / (timeToLastTokenMs / 1000.0), 2);

                double generationMs = timeToLastTokenMs - timeToFirstTokenMs;
                if (completionTokens > 0 && generationMs > 0)
                    history.TokensPerSecondGeneration = Math.Round(completionTokens / (generationMs / 1000.0), 2);

                await _Database.ChatHistory.CreateAsync(history, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "failed to write chat history: " + e.Message);
            }
        }

        private async Task<ResolvedEndpoint?> ResolveCompletionEndpointAsync(string endpointId, CancellationToken token)
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
                    JsonElement ep = JsonSerializer.Deserialize<JsonElement>(body, _JsonOptions);

                    string apiFormat = ep.TryGetProperty("ApiFormat", out JsonElement af) ? af.GetString() : null;
                    string epUrl = ep.TryGetProperty("Endpoint", out JsonElement eu) ? eu.GetString() : null;
                    string apiKey = ep.TryGetProperty("ApiKey", out JsonElement ak) ? ak.GetString() : null;

                    Enums.InferenceProviderEnum provider = Enums.InferenceProviderEnum.Ollama;
                    if (!String.IsNullOrEmpty(apiFormat) && apiFormat.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
                        provider = Enums.InferenceProviderEnum.OpenAI;

                    return new ResolvedEndpoint
                    {
                        Provider = provider,
                        Endpoint = epUrl ?? _Settings.Inference.Endpoint,
                        ApiKey = apiKey ?? _Settings.Inference.ApiKey
                    };
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception resolving completion endpoint " + endpointId + ": " + e.Message);
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
                total += 4;
                total += EstimateTokenCount(msg.Content);
            }

            return total;
        }
    }

    /// <summary>
    /// Chat execution request.
    /// </summary>
    public class AssistantChatExecutionRequest
    {
        /// <summary>
        /// Assistant identifier.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Optional preloaded assistant record.
        /// </summary>
        public Assistant Assistant { get; set; } = null;

        /// <summary>
        /// Optional preloaded assistant settings record.
        /// </summary>
        public AssistantSettings AssistantSettings { get; set; } = null;

        /// <summary>
        /// Conversation messages to execute.
        /// </summary>
        public List<ChatCompletionMessage> Messages { get; set; } = new List<ChatCompletionMessage>();

        /// <summary>
        /// Conversation thread identifier used for history persistence.
        /// </summary>
        public string ThreadId { get; set; } = null;

        /// <summary>
        /// Optional model override.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Optional temperature override.
        /// </summary>
        public double? Temperature { get; set; } = null;

        /// <summary>
        /// Optional top-p override.
        /// </summary>
        public double? TopP { get; set; } = null;

        /// <summary>
        /// Optional max token override.
        /// </summary>
        public int? MaxTokens { get; set; } = null;

        /// <summary>
        /// Optional metadata filter override.
        /// </summary>
        public ChatMetadataFilter MetadataFilter { get; set; } = null;

        /// <summary>
        /// Optional user message timestamp override.
        /// </summary>
        public DateTime? UserMessageUtc { get; set; } = null;

        /// <summary>
        /// Request origin label persisted to history.
        /// </summary>
        public string Origin { get; set; } = "api";
    }

    /// <summary>
    /// Chat execution result.
    /// </summary>
    public class AssistantChatExecutionResult
    {
        /// <summary>
        /// Indicates whether execution succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Error message when execution fails.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// Assistant record used for execution.
        /// </summary>
        public Assistant Assistant { get; set; } = null;

        /// <summary>
        /// Assistant settings used for execution.
        /// </summary>
        public AssistantSettings AssistantSettings { get; set; } = null;

        /// <summary>
        /// OpenAI-compatible completion response.
        /// </summary>
        public ChatCompletionResponse Response { get; set; } = null;

        /// <summary>
        /// Canonical assistant response text after transport-agnostic cleanup.
        /// </summary>
        public string CanonicalResponseText { get; set; } = null;
    }
}
