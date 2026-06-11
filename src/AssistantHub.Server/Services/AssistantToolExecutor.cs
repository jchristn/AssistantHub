namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Executes policy-approved, read-only server-side tools for assistant chat.
    /// </summary>
    public class AssistantToolExecutor : IAssistantToolExecutor
    {
        private static readonly string _Header = "[AssistantToolExecutor] ";

        private readonly DatabaseDriverBase _Database;
        private readonly LoggingModule _Logging;
        private readonly AssistantHubSettings _Settings;
        private readonly RetrievalService _Retrieval;
        private readonly IObjectStorageService _Storage;
        private readonly IInvertedIndexService _InvertedIndex;
        private readonly HttpClient _TavilyHttpClient;

        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AssistantToolExecutor(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            RetrievalService retrieval,
            IObjectStorageService storage = null,
            IInvertedIndexService invertedIndex = null,
            HttpClient tavilyHttpClient = null)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval));
            _Storage = storage;
            _InvertedIndex = invertedIndex;
            _TavilyHttpClient = tavilyHttpClient;
        }

        /// <summary>
        /// Execute a server-side assistant tool if policy and prerequisites allow it.
        /// </summary>
        public async Task<AssistantToolExecutionResult> ExecuteAsync(
            AssistantToolExecutionContext context,
            AssistantToolExecutionRequest request,
            CancellationToken token = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (request == null) throw new ArgumentNullException(nameof(request));

            Stopwatch sw = Stopwatch.StartNew();
            AssistantToolExecutionResult result = new AssistantToolExecutionResult
            {
                ToolName = AssistantToolRegistry.NormalizeToolName(request.ToolName) ?? request.ToolName?.Trim(),
                CreatedUtc = DateTime.UtcNow
            };

            try
            {
                if (String.IsNullOrWhiteSpace(result.ToolName))
                    return FinishError(result, sw, "ToolName is required.", true);

                if (context.Assistant == null || context.Settings == null)
                    return FinishError(result, sw, "Assistant context is incomplete.", true);

                AssistantToolPolicy policy = context.Policy ?? context.Settings.ToolPolicy ?? new AssistantToolPolicy();
                policy.Normalize();
                context.Policy = policy;

                AssistantToolDescriptor descriptor = new AssistantToolPolicyResolver(_Settings)
                    .Resolve(context.Assistant, context.Settings, true)
                    .FirstOrDefault(tool => String.Equals(tool.ToolName, result.ToolName, StringComparison.OrdinalIgnoreCase));

                if (descriptor == null)
                    return FinishError(result, sw, "Unknown tool.", true);

                if (!descriptor.EnabledByPolicy || !descriptor.Available)
                    return FinishError(result, sw, descriptor.UnavailableReason ?? "Tool is not available.", true);

                using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeoutCts.CancelAfter(policy.ToolCallTimeoutMs);
                timeoutCts.Token.ThrowIfCancellationRequested();

                using JsonDocument arguments = ParseArguments(request.ArgumentsJson);
                AssistantToolArgumentValidator.Validate(result.ToolName, arguments.RootElement);
                object output = result.ToolName switch
                {
                    "collection_search" => await ExecuteCollectionSearchAsync(context, arguments.RootElement, timeoutCts.Token).ConfigureAwait(false),
                    "collection_read_chunks" => await ExecuteCollectionReadChunksAsync(context, arguments.RootElement, timeoutCts.Token).ConfigureAwait(false),
                    "collection_enumerate_documents" => await ExecuteCollectionEnumerateDocumentsAsync(context, arguments.RootElement, timeoutCts.Token).ConfigureAwait(false),
                    "verbex_full_text_search" => await ExecuteVerbexFullTextSearchAsync(context, arguments.RootElement, timeoutCts.Token).ConfigureAwait(false),
                    "index_enumerate_records" => await ExecuteIndexEnumerateRecordsAsync(context, arguments.RootElement, timeoutCts.Token).ConfigureAwait(false),
                    "s3_object_read" => await ExecuteS3ObjectReadAsync(context, arguments.RootElement, timeoutCts.Token).ConfigureAwait(false),
                    "document_atom_extract" => await ExecuteDocumentAtomExtractAsync(context, arguments.RootElement, timeoutCts.Token).ConfigureAwait(false),
                    "bucket_enumerate_objects" => await ExecuteBucketEnumerateObjectsAsync(context, arguments.RootElement, timeoutCts.Token).ConfigureAwait(false),
                    "web_search" => await ExecuteWebSearchAsync(context, arguments.RootElement, timeoutCts.Token).ConfigureAwait(false),
                    _ => throw new NotSupportedException("Tool execution is not implemented for " + result.ToolName + ".")
                };

                timeoutCts.Token.ThrowIfCancellationRequested();
                ApplyProviderTelemetry(result, output);
                AssistantToolOutputLimiter.ApplyPerCallLimit(result, output, policy.MaxToolOutputChars);
                result.Success = true;
                return Finish(result, sw);
            }
            catch (OperationCanceledException)
            {
                string message = token.IsCancellationRequested
                    ? "Tool execution canceled."
                    : "Tool execution timed out.";
                return FinishError(result, sw, message, false);
            }
            catch (JsonException e)
            {
                return FinishError(result, sw, "Tool arguments are not valid JSON: " + e.Message, false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + result.ToolName + " failed: " + e.Message);
                return FinishError(result, sw, e.Message, false);
            }
        }

        private static void ApplyProviderTelemetry(AssistantToolExecutionResult result, object output)
        {
            if (result == null || output == null) return;

            if (output is WebSearchResponse webSearch)
            {
                result.CreditsUsed = webSearch.CreditsUsed;
                result.ProviderLatencyMs = webSearch.LatencySeconds.HasValue
                    ? Math.Round(webSearch.LatencySeconds.Value * 1000.0, 2)
                    : null;
            }

            if (String.Equals(result.ToolName, "s3_object_read", StringComparison.OrdinalIgnoreCase))
                result.ObjectBytesReturned = ExtractObjectBytesReturned(output);
        }

        private static int? ExtractObjectBytesReturned(object output)
        {
            if (output == null) return null;

            try
            {
                string json = JsonSerializer.Serialize(output, _JsonOptions);
                using JsonDocument document = JsonDocument.Parse(json);
                if (document.RootElement.TryGetProperty("RangeLength", out JsonElement rangeLength)
                    && rangeLength.ValueKind == JsonValueKind.Number
                    && rangeLength.TryGetInt32(out int value))
                    return Math.Max(0, value);
            }
            catch (Exception)
            {
            }

            return null;
        }

        private async Task<object> ExecuteCollectionSearchAsync(
            AssistantToolExecutionContext context,
            JsonElement arguments,
            CancellationToken token)
        {
            AssistantToolPolicy policy = context.Policy;
            string query = GetString(arguments, "query");
            List<string> queries = GetStringList(arguments, "queries");
            if (!String.IsNullOrWhiteSpace(query))
                queries.Insert(0, query);

            queries = queries
                .Where(value => !String.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            HashSet<string> modelQueries = new HashSet<string>(queries, StringComparer.OrdinalIgnoreCase);

            if (queries.Count == 0)
                throw new ArgumentException("collection_search requires query or queries.");

            int maxSearchResults = MinLimit(policy.MaxSearchResultsPerCall, policy.MaxSearchTopK, policy.MaxToolResultItems);
            int requestedMaxResults = GetIntAny(arguments, "max_results", "top_k") ?? maxSearchResults;
            int maxResults = Math.Clamp(requestedMaxResults, 1, maxSearchResults);
            int includeNeighbors = Math.Clamp(GetInt(arguments, "include_neighbors", policy.MaxNeighborWindow), 0, policy.MaxNeighborWindow);
            string strategy = NormalizeCollectionSearchStrategy(GetString(arguments, "strategy"));
            if (String.Equals(strategy, "single", StringComparison.Ordinal))
            {
                queries = queries.Take(1).ToList();
            }
            else if (policy.EnableServerGeneratedQueryVariants)
            {
                List<string> serverVariants = BuildServerGeneratedQueryVariants(queries);
                foreach (string variant in serverVariants)
                {
                    if (!queries.Contains(variant, StringComparer.OrdinalIgnoreCase))
                        queries.Add(variant);
                }
            }

            bool queryLimitApplied = queries.Count > policy.MaxSearchQueriesPerCall;
            queries = queries.Take(policy.MaxSearchQueriesPerCall).ToList();
            List<string> serverGeneratedQueries = policy.EnableServerGeneratedQueryVariants
                ? queries
                    .Where(value => !modelQueries.Contains(value))
                    .ToList()
                : new List<string>();

            string requestedSearchMode = GetString(arguments, "search_mode");
            string defaultSearchMode = NormalizeCollectionSearchMode(policy.DefaultSearchMode ?? context.Settings.SearchMode, "Hybrid");
            List<string> searchedModes = ResolveCollectionSearchModes(strategy, requestedSearchMode, defaultSearchMode, policy);
            List<string> exactPhraseQueries = BuildExactPhraseQueries(queries)
                .Where(value => !queries.Contains(value, StringComparer.OrdinalIgnoreCase))
                .Take(policy.MaxSearchQueriesPerCall)
                .ToList();
            bool runExactPhrasePasses = exactPhraseQueries.Count > 0
                && policy.AllowedSearchModes.Contains("FullText", StringComparer.OrdinalIgnoreCase);
            string fullTextSearchType = GetString(arguments, "fulltext_search_type") ?? context.Settings.FullTextSearchType;
            string fullTextLanguage = GetString(arguments, "fulltext_language") ?? context.Settings.FullTextLanguage;
            int fullTextNormalization = Math.Clamp(GetInt(arguments, "fulltext_normalization", context.Settings.FullTextNormalization), 0, 64);
            double? fullTextMinimumScore = GetDoubleAny(arguments, "fulltext_minimum_score") ?? context.Settings.FullTextMinimumScore;
            if (fullTextMinimumScore.HasValue)
                fullTextMinimumScore = Math.Clamp(fullTextMinimumScore.Value, 0, 1);
            ChatMetadataFilter requestMetadataFilter = BuildModelMetadataFilter(arguments);
            ChatMetadataFilter metadataFilter = AssistantDocumentPolicyFilter.BuildAssistantMetadataFilter(context.Settings);
            if (requestMetadataFilter != null)
            {
                metadataFilter ??= new ChatMetadataFilter();
                metadataFilter.Merge(requestMetadataFilter);
            }

            string sourceUrlContains = GetString(arguments, "source_url_contains");
            if (!String.IsNullOrWhiteSpace(sourceUrlContains) && !policy.AllowDocumentSourceUrls)
                throw new InvalidOperationException("source_url_contains requires AllowDocumentSourceUrls in assistant policy.");

            double scoreThreshold = Math.Clamp(context.Settings.RetrievalScoreThreshold, 0, 1);
            double? requestedScoreThreshold = GetDoubleAny(arguments, "score_threshold");
            if (requestedScoreThreshold.HasValue)
                scoreThreshold = Math.Max(scoreThreshold, Math.Clamp(requestedScoreThreshold.Value, 0, 1));

            List<string> requestedDocumentIds = await ResolveCollectionDocumentIdsAsync(
                context,
                GetStringList(arguments, "document_ids"),
                token).ConfigureAwait(false);
            CollectionDocumentScope documentScope = await ResolveCollectionDocumentScopeAsync(
                context,
                requestedDocumentIds,
                requestMetadataFilter,
                token).ConfigureAwait(false);
            List<string> documentIds = documentScope.DocumentIds;
            int? documentsConsidered = documentScope.DocumentsConsidered;

            List<object> queryResults = new List<object>();
            HashSet<string> seenResults = new HashSet<string>(StringComparer.Ordinal);
            int resultsConsidered = 0;
            int returnedResultCount = 0;
            bool passResultLimitApplied = false;
            bool resultsConsideredLimitApplied = false;
            bool hybridFallbackRan = false;
            List<object> suggestedNextCalls = new List<object>();
            HashSet<string> suggestedNextCallKeys = new HashSet<string>(StringComparer.Ordinal);
            List<object> searchPassMetadata = new List<object>();
            Dictionary<string, int> resultBuckets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            List<(string Query, string Mode, bool ExactPhrasePass)> searchPasses = new List<(string Query, string Mode, bool ExactPhrasePass)>();
            if (runExactPhrasePasses)
            {
                foreach (string exactQuery in exactPhraseQueries)
                    searchPasses.Add((exactQuery, "FullText", true));
            }

            foreach (string oneQuery in queries)
            {
                foreach (string oneMode in searchedModes)
                {
                    searchPasses.Add((oneQuery, oneMode, false));
                }
            }

            foreach (var pass in searchPasses)
            {
                token.ThrowIfCancellationRequested();
                int remainingResultsToConsider = policy.MaxResultsConsideredPerSearch - resultsConsidered;
                if (remainingResultsToConsider <= 0)
                {
                    resultsConsideredLimitApplied = true;
                    break;
                }

                int passMaxResults = Math.Min(maxResults, remainingResultsToConsider);
                RetrievalSearchOptions options = new RetrievalSearchOptions
                {
                    SearchMode = pass.Mode,
                    TextWeight = context.Settings.TextWeight,
                    FullTextSearchType = fullTextSearchType,
                    FullTextLanguage = fullTextLanguage,
                    FullTextNormalization = fullTextNormalization,
                    FullTextMinimumScore = fullTextMinimumScore,
                    IncludeNeighbors = includeNeighbors,
                    MetadataFilter = metadataFilter,
                    DocumentIds = documentIds
                };

                List<RetrievalChunk> chunks = await _Retrieval.RetrieveAsync(
                    context.Assistant.TenantId,
                    context.Settings.CollectionId,
                    pass.Query,
                    passMaxResults,
                    scoreThreshold,
                    token,
                    context.Settings.EmbeddingEndpointId,
                    options).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                hybridFallbackRan = hybridFallbackRan || options.HybridFallbackRan;
                resultsConsidered += chunks.Count;
                if (chunks.Count >= maxResults)
                    passResultLimitApplied = true;
                if (resultsConsidered >= policy.MaxResultsConsideredPerSearch)
                    resultsConsideredLimitApplied = true;
                List<object> passResults = new List<object>();
                foreach (RetrievalChunk chunk in chunks)
                {
                    string dedupeKey = (chunk.DocumentId ?? "") + ":" + (chunk.Position.HasValue ? chunk.Position.Value.ToString() : "");
                    if (!String.IsNullOrWhiteSpace(dedupeKey)
                        && !seenResults.Add(dedupeKey))
                        continue;

                    AssistantDocument chunkDocument = await ResolveVisibleChunkDocumentAsync(context, chunk.DocumentId, token).ConfigureAwait(false);
                    if (chunkDocument != null && !IsAvailableCollectionDocument(chunkDocument, context))
                        continue;
                    if (requestMetadataFilter != null
                        && (chunkDocument == null || !AssistantDocumentPolicyFilter.MatchesMetadataFilter(chunkDocument, requestMetadataFilter)))
                        continue;
                    if (!String.IsNullOrWhiteSpace(sourceUrlContains)
                        && (chunkDocument == null || !Contains(chunkDocument.SourceUrl, sourceUrlContains)))
                        continue;

                    if (!String.IsNullOrWhiteSpace(chunk.DocumentId)
                        && chunk.Position.HasValue
                        && suggestedNextCalls.Count < policy.MaxReadRangesPerCall
                        && suggestedNextCallKeys.Add(chunk.DocumentId + ":" + chunk.Position.Value))
                    {
                        suggestedNextCalls.Add(new
                        {
                            Tool = "collection_read_chunks",
                            Arguments = new Dictionary<string, object>
                            {
                                ["document_id"] = chunk.DocumentId,
                                ["positions"] = new List<int> { chunk.Position.Value }
                            },
                            Reason = "Read the matching collection chunk for more context."
                        });
                    }

                    string resultBucket = BuildCollectionResultBucket(pass.Mode, pass.ExactPhrasePass, chunk);
                    IncrementBucket(resultBuckets, resultBucket);
                    passResults.Add(new
                    {
                        ResultId = dedupeKey,
                        ResultBucket = resultBucket,
                        ExactPhrasePass = pass.ExactPhrasePass,
                        chunk.DocumentId,
                        DocumentName = chunkDocument != null ? chunkDocument.Name ?? chunkDocument.OriginalFilename : null,
                        ContentType = chunkDocument?.ContentType,
                        chunk.Score,
                        chunk.TextScore,
                        chunk.FusionScore,
                        chunk.Position,
                        Excerpt = BuildExcerpt(chunk.Content, 800),
                        Content = policy.ReturnFullSearchContent ? chunk.Content : null,
                        ContentOmitted = !policy.ReturnFullSearchContent && !String.IsNullOrEmpty(chunk.Content),
                        Neighbors = chunk.Neighbors?.Select(neighbor => new
                        {
                            neighbor.Position,
                            neighbor.Score,
                            neighbor.TextScore,
                            neighbor.FusionScore,
                            Excerpt = BuildExcerpt(neighbor.Content, 300),
                            Content = policy.ReturnFullSearchContent ? neighbor.Content : null,
                            ContentOmitted = !policy.ReturnFullSearchContent && !String.IsNullOrEmpty(neighbor.Content)
                        }).ToList(),
                        Labels = policy.ReturnLabels || policy.AllowDocumentMetadataDetails ? AssistantDocumentPolicyFilter.ParseLabels(chunkDocument?.Labels).ToList() : null,
                        Tags = policy.ReturnTags || policy.AllowDocumentMetadataDetails ? AssistantDocumentPolicyFilter.ParseTags(chunkDocument?.Tags) : null,
                        CitationHandle = !String.IsNullOrWhiteSpace(chunk.DocumentId) && chunk.Position.HasValue
                            ? chunk.DocumentId + ":" + chunk.Position.Value
                            : null
                    });
                }

                returnedResultCount += passResults.Count;
                searchPassMetadata.Add(new
                {
                    Query = pass.Query,
                    SearchMode = pass.Mode,
                    ExactPhrasePass = pass.ExactPhrasePass,
                    ResultsConsidered = chunks.Count,
                    ResultsReturned = passResults.Count
                });
                queryResults.Add(new
                {
                    Query = pass.Query,
                    SearchMode = pass.Mode,
                    ExactPhrasePass = pass.ExactPhrasePass,
                    Results = passResults
                });
            }

            bool exhaustive = String.Equals(strategy, "exhaustive", StringComparison.Ordinal);
            List<string> exhaustiveIncompleteReasons = new List<string>();
            if (exhaustive && queryLimitApplied)
                exhaustiveIncompleteReasons.Add("query_limit");
            if (exhaustive && passResultLimitApplied)
                exhaustiveIncompleteReasons.Add("result_limit");
            if (exhaustive && documentScope.DocumentLimitApplied)
                exhaustiveIncompleteReasons.Add("document_limit");
            if (exhaustive && resultsConsideredLimitApplied)
                exhaustiveIncompleteReasons.Add("results_considered_limit");

            return new
            {
                Tool = "collection_search",
                CollectionId = context.Settings.CollectionId,
                Strategy = strategy,
                QueryCount = queryResults.Count,
                SearchedQueries = queries,
                ServerGeneratedQueries = serverGeneratedQueries.Count > 0 ? serverGeneratedQueries : null,
                QueryLimitApplied = queryLimitApplied,
                SearchedModes = searchedModes,
                ExactPhraseQueries = runExactPhrasePasses ? exactPhraseQueries : null,
                SearchPasses = searchPassMetadata,
                ResultBuckets = resultBuckets.Count > 0 ? resultBuckets : null,
                ScoreThreshold = scoreThreshold,
                FullTextSearchType = fullTextSearchType,
                FullTextLanguage = fullTextLanguage,
                FullTextNormalization = fullTextNormalization,
                FullTextMinimumScore = fullTextMinimumScore,
                FullSearchContentReturned = policy.ReturnFullSearchContent,
                DocumentsConsidered = documentsConsidered,
                MaxDocumentsConsidered = policy.MaxDocumentsConsideredPerSearch,
                DocumentLimitApplied = documentScope.DocumentLimitApplied,
                ResultsConsidered = resultsConsidered,
                MaxResultsConsidered = policy.MaxResultsConsideredPerSearch,
                ResultsConsideredLimitApplied = resultsConsideredLimitApplied,
                TotalResults = returnedResultCount,
                HybridFallbackRan = hybridFallbackRan,
                MoreAvailable = false,
                NextOffset = (string)null,
                ExhaustiveComplete = exhaustive ? exhaustiveIncompleteReasons.Count == 0 : (bool?)null,
                ExhaustiveIncompleteReasons = exhaustive && exhaustiveIncompleteReasons.Count > 0 ? exhaustiveIncompleteReasons : null,
                SuggestedNextCalls = (queryLimitApplied || passResultLimitApplied || returnedResultCount >= maxResults) && suggestedNextCalls.Count > 0
                    ? suggestedNextCalls
                    : null,
                Results = queryResults
            };
        }

        private async Task<CollectionDocumentScope> ResolveCollectionDocumentScopeAsync(
            AssistantToolExecutionContext context,
            List<string> requestedDocumentIds,
            ChatMetadataFilter requestMetadataFilter,
            CancellationToken token)
        {
            CollectionDocumentScope scope = new CollectionDocumentScope();
            if (context == null || context.Assistant == null || context.Settings == null || String.IsNullOrWhiteSpace(context.Settings.CollectionId))
                return scope;

            try
            {
                int maxDocuments = Math.Max(1, context.Policy?.MaxDocumentsConsideredPerSearch ?? 1000);
                if (requestedDocumentIds != null && requestedDocumentIds.Count > 0)
                {
                    List<string> visibleIds = new List<string>();
                    foreach (string documentId in requestedDocumentIds.Distinct(StringComparer.Ordinal))
                    {
                        if (String.IsNullOrWhiteSpace(documentId)) continue;
                        AssistantDocument document = await _Database.AssistantDocument.ReadAsync(documentId, token).ConfigureAwait(false);
                        if (!IsAvailableCollectionDocument(document, context)) continue;
                        if (requestMetadataFilter != null && !AssistantDocumentPolicyFilter.MatchesMetadataFilter(document, requestMetadataFilter)) continue;
                        visibleIds.Add(document.Id);
                    }

                    scope.DocumentLimitApplied = visibleIds.Count > maxDocuments;
                    scope.DocumentIds = visibleIds.Take(maxDocuments).ToList();
                    scope.DocumentsConsidered = scope.DocumentIds.Count;
                    return scope;
                }

                List<AssistantDocument> visibleList = new List<AssistantDocument>();
                string continuationToken = null;
                int pageSize = Math.Min(1000, Math.Max(100, maxDocuments + 1));
                do
                {
                    EnumerationResult<AssistantDocument> documents = await _Database.AssistantDocument.EnumerateAsync(
                        context.Assistant.TenantId,
                        new EnumerationQuery
                        {
                            CollectionIdFilter = context.Settings.CollectionId,
                            ContinuationToken = continuationToken,
                            Ordering = EnumerationOrderEnum.CreatedAscending,
                            MaxResults = pageSize
                        },
                        token).ConfigureAwait(false);

                    if (documents == null || documents.Objects == null || documents.Objects.Count == 0)
                        break;

                    IEnumerable<AssistantDocument> visibleDocuments = documents.Objects
                        .Where(document => IsAvailableCollectionDocument(document, context));
                    if (requestMetadataFilter != null)
                        visibleDocuments = visibleDocuments.Where(document => AssistantDocumentPolicyFilter.MatchesMetadataFilter(document, requestMetadataFilter));

                    visibleList.AddRange(visibleDocuments);
                    if (visibleList.Count > maxDocuments)
                        break;

                    continuationToken = documents.ContinuationToken;
                    if (documents.EndOfResults || String.IsNullOrWhiteSpace(continuationToken))
                        break;
                }
                while (true);

                visibleList = visibleList
                    .OrderBy(document => document.CreatedUtc)
                    .ThenBy(document => document.Id, StringComparer.Ordinal)
                    .Take(maxDocuments + 1)
                    .ToList();
                scope.DocumentLimitApplied = visibleList.Count > maxDocuments;
                scope.DocumentsConsidered = Math.Min(visibleList.Count, maxDocuments);
                if (scope.DocumentLimitApplied)
                {
                    scope.DocumentIds = visibleList
                        .Take(maxDocuments)
                        .Select(document => document.Id)
                        .Where(id => !String.IsNullOrWhiteSpace(id))
                        .ToList();
                }

                return scope;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "failed to count visible collection documents for assistant " + context.Assistant.Id + ": " + e.Message);
                return scope;
            }
        }

        private async Task<object> ExecuteCollectionReadChunksAsync(
            AssistantToolExecutionContext context,
            JsonElement arguments,
            CancellationToken token)
        {
            AssistantToolPolicy policy = context.Policy;
            string documentId = GetString(arguments, "document_id");
            if (String.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("collection_read_chunks requires document_id.");

            AssistantDocument document = await _Database.AssistantDocument.ReadAsync(documentId, token).ConfigureAwait(false);
            if (!IsAvailableCollectionDocument(document, context))
                throw new InvalidOperationException("Requested document_id is not available for this assistant: " + documentId + ".");

            List<string> chunkRecordIds = ParseChunkRecordIds(document.ChunkRecordIds);
            if (chunkRecordIds.Count == 0)
                throw new InvalidOperationException("Requested document has no readable chunk records.");

            int maxChunkLimit = MinLimit(policy.MaxChunksPerRead, policy.MaxToolResultItems);
            int maxChunks = Math.Clamp(GetInt(arguments, "max_chunks", maxChunkLimit), 1, maxChunkLimit);
            int neighborWindow = Math.Clamp(GetInt(arguments, "neighbor_window", 0), 0, policy.MaxNeighborWindow);
            SortedSet<int> requestedPositions = ResolveRequestedChunkPositions(arguments, chunkRecordIds.Count, policy.MaxReadRangesPerCall);
            if (requestedPositions.Count == 0)
                throw new ArgumentException("collection_read_chunks requires positions or ranges.");

            SortedSet<int> expandedPositions = ExpandChunkPositions(requestedPositions, neighborWindow, chunkRecordIds.Count);
            List<int> selectedPositions = expandedPositions.Take(maxChunks).ToList();
            int omittedPositionCount = Math.Max(0, expandedPositions.Count - selectedPositions.Count);

            List<object> chunks = new List<object>();
            int readErrorCount = 0;
            foreach (int position in selectedPositions)
            {
                token.ThrowIfCancellationRequested();
                string recordId = chunkRecordIds[position];
                RetrievalChunk chunk = await _Retrieval.ReadCollectionRecordAsync(
                    context.Assistant.TenantId,
                    context.Settings.CollectionId,
                    recordId,
                    token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                if (chunk == null)
                {
                    readErrorCount++;
                    continue;
                }

                if (!String.IsNullOrWhiteSpace(chunk.DocumentId)
                    && !String.Equals(chunk.DocumentId, document.Id, StringComparison.Ordinal))
                {
                    readErrorCount++;
                    continue;
                }

                chunks.Add(new
                {
                    DocumentId = document.Id,
                    DocumentName = document.Name ?? document.OriginalFilename,
                    document.ContentType,
                    Position = position,
                    Content = chunk.Content,
                    IsRequested = requestedPositions.Contains(position),
                    CitationHandle = document.Id + ":" + position
                });
            }

            return new
            {
                Tool = "collection_read_chunks",
                CollectionId = context.Settings.CollectionId,
                DocumentId = document.Id,
                DocumentName = document.Name ?? document.OriginalFilename,
                document.ContentType,
                TotalAvailableChunks = chunkRecordIds.Count,
                RequestedPositions = requestedPositions.ToList(),
                NeighborWindow = neighborWindow,
                MaxChunks = maxChunks,
                OmittedPositionCount = omittedPositionCount,
                ReadErrorCount = readErrorCount,
                TotalRecords = chunks.Count,
                Chunks = chunks
            };
        }

        private async Task<object> ExecuteVerbexFullTextSearchAsync(
            AssistantToolExecutionContext context,
            JsonElement arguments,
            CancellationToken token)
        {
            string query = GetString(arguments, "query");
            if (String.IsNullOrWhiteSpace(query))
                throw new ArgumentException("verbex_full_text_search requires query.");

            IInvertedIndexService invertedIndex = _InvertedIndex ?? new VerbexInvertedIndexService(_Settings.Verbex, _Logging);
            AssistantToolPolicy policy = context.Policy;
            string indexId = await ResolveVerbexIndexIdAsync(context, GetString(arguments, "index_id"), token).ConfigureAwait(false);
            List<string> recordIdFilters = await ResolveVerbexRecordIdFiltersAsync(context, indexId, GetStringList(arguments, "record_ids"), token).ConfigureAwait(false);
            int maxVerbexResults = MinLimit(policy.MaxSearchResultsPerCall, policy.MaxVerbexResults, policy.MaxToolResultItems);
            int maxResults = Math.Clamp(GetInt(arguments, "max_results", maxVerbexResults), 1, maxVerbexResults);

            Dictionary<string, object> body = new Dictionary<string, object>
            {
                ["Query"] = query,
                ["MaxResults"] = maxResults,
                ["UseAndLogic"] = GetBool(arguments, "use_and_logic", false),
                ["IncludeMatchedTerms"] = true,
                ["IncludeTermDetails"] = false,
                ["IncludeDocumentTermStats"] = false
            };

            List<string> requiredTerms = GetStringList(arguments, "required_terms");
            if (requiredTerms.Count > 0) body["RequiredTerms"] = requiredTerms;

            List<string> excludedTerms = GetStringList(arguments, "excluded_terms");
            if (excludedTerms.Count > 0) body["ExcludedTerms"] = excludedTerms;

            string requestJson = JsonSerializer.Serialize(body, _JsonOptions);
            string path = "/v1.0/indices/" + Uri.EscapeDataString(indexId) + "/search";
            string traceId = String.IsNullOrWhiteSpace(context.TraceId) ? Guid.NewGuid().ToString("N") : context.TraceId;
            Stopwatch verbexSw = Stopwatch.StartNew();

            _Logging.Debug(_Header + "Verbex search request trace " + traceId + " path " + path + " index " + indexId + " body " + BuildRedactedVerbexSearchBodyLog(requestJson));

            token.ThrowIfCancellationRequested();
            using HttpResponseMessage response = await invertedIndex.SendAsync(HttpMethod.Post, path, requestJson).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _Logging.Debug(_Header + "Verbex search response trace " + traceId + " index " + indexId + " status " + (int)response.StatusCode + " resultCount 0 durationMs " + verbexSw.ElapsedMilliseconds);
                throw new HttpRequestException("Verbex search failed with status code " + (int)response.StatusCode + ".");
            }

            using JsonDocument document = JsonDocument.Parse(responseBody);
            List<object> results = await NormalizeVerbexSearchResultsAsync(
                context,
                indexId,
                GetFirstArray(document.RootElement, "Results", "Documents", "Objects"),
                recordIdFilters,
                maxResults,
                token).ConfigureAwait(false);
            _Logging.Debug(_Header + "Verbex search response trace " + traceId + " index " + indexId + " status " + (int)response.StatusCode + " resultCount " + results.Count + " durationMs " + verbexSw.ElapsedMilliseconds);

            return new
            {
                Tool = "verbex_full_text_search",
                IndexId = indexId,
                Query = query,
                RecordIdFilters = recordIdFilters,
                TotalRecords = results.Count,
                Results = results
            };
        }

        private async Task<object> ExecuteIndexEnumerateRecordsAsync(
            AssistantToolExecutionContext context,
            JsonElement arguments,
            CancellationToken token)
        {
            AssistantToolPolicy policy = context.Policy;
            IInvertedIndexService invertedIndex = _InvertedIndex ?? new VerbexInvertedIndexService(_Settings.Verbex, _Logging);
            string indexId = await ResolveVerbexIndexIdAsync(context, GetString(arguments, "index_id"), token).ConfigureAwait(false);
            List<string> recordIdFilters = await ResolveVerbexRecordIdFiltersAsync(context, indexId, GetStringList(arguments, "record_ids"), token).ConfigureAwait(false);
            int maxVerbexResults = MinLimit(policy.MaxSearchResultsPerCall, policy.MaxVerbexResults, policy.MaxToolResultItems);
            int maxResults = Math.Clamp(GetInt(arguments, "max_results", maxVerbexResults), 1, maxVerbexResults);
            string continuationToken = GetString(arguments, "continuation_token");
            string queryText = GetString(arguments, "query");
            string recordIdPrefix = GetString(arguments, "record_id_prefix");

            string path = "/v1.0/indices/" + Uri.EscapeDataString(indexId) + "/documents?maxResults=" + maxResults;
            if (!String.IsNullOrWhiteSpace(continuationToken))
                path += "&continuationToken=" + Uri.EscapeDataString(continuationToken);

            token.ThrowIfCancellationRequested();
            using HttpResponseMessage response = await invertedIndex.SendAsync(HttpMethod.Get, path).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException("Verbex record enumeration failed with status code " + (int)response.StatusCode + ".");

            using JsonDocument document = JsonDocument.Parse(responseBody);
            JsonElement? recordsElement = GetFirstArray(document.RootElement, "Objects", "Records", "Documents", "Results");
            List<object> records = await NormalizeVerbexEnumerateRecordsAsync(
                context,
                indexId,
                recordsElement,
                maxResults,
                queryText,
                recordIdPrefix,
                recordIdFilters,
                token).ConfigureAwait(false);

            return new
            {
                Tool = "index_enumerate_records",
                IndexId = indexId,
                RecordIdFilters = recordIdFilters,
                MaxResults = maxResults,
                ContinuationToken = GetStringAny(document.RootElement, "ContinuationToken", "NextContinuationToken"),
                EndOfResults = GetBoolAny(document.RootElement, "EndOfResults"),
                TotalRecords = records.Count,
                Objects = records
            };
        }

        private async Task<object> ExecuteS3ObjectReadAsync(
            AssistantToolExecutionContext context,
            JsonElement arguments,
            CancellationToken token)
        {
            if (_Storage == null)
                throw new InvalidOperationException("S3 storage service is not configured.");

            AssistantToolPolicy policy = context.Policy;
            string documentId = GetString(arguments, "document_id");
            string objectKeyArgument = GetString(arguments, "object_key");

            if (String.IsNullOrWhiteSpace(documentId) && String.IsNullOrWhiteSpace(objectKeyArgument))
                throw new ArgumentException("s3_object_read requires document_id or object_key.");

            AssistantDocument document = null;
            string bucket = null;
            string objectKey = null;
            bool documentBacked = !String.IsNullOrWhiteSpace(documentId);

            if (documentBacked)
            {
                document = await _Database.AssistantDocument.ReadAsync(documentId, token).ConfigureAwait(false);
                if (!IsAvailableCollectionDocument(document, context))
                    throw new InvalidOperationException("Requested document_id is not available for this assistant: " + documentId + ".");

                bucket = String.IsNullOrWhiteSpace(document.BucketName) ? _Settings.S3?.BucketName : document.BucketName.Trim();
                objectKey = document.S3Key?.Trim();
                if (String.IsNullOrWhiteSpace(bucket) || String.IsNullOrWhiteSpace(objectKey))
                    throw new InvalidOperationException("Requested document does not reference an S3 object.");
            }
            else
            {
                if (policy.DocumentBackedObjectsOnly || !policy.AllowBucketWideObjectRead)
                    throw new InvalidOperationException("Bucket-wide S3 object reads require AllowBucketWideObjectRead=true and DocumentBackedObjectsOnly=false in assistant policy.");

                if (policy.AllowedBucketPrefixes == null || policy.AllowedBucketPrefixes.Count == 0)
                    throw new InvalidOperationException("Bucket-wide S3 object reads require at least one AllowedBucketPrefixes entry in assistant policy.");

                bucket = GetStringAny(arguments, "bucket", "bucket_name") ?? _Settings.S3?.BucketName;
                objectKey = objectKeyArgument.Trim();
                if (String.IsNullOrWhiteSpace(bucket) || String.IsNullOrWhiteSpace(objectKey))
                    throw new ArgumentException("s3_object_read bucket-wide reads require bucket configuration and object_key.");
            }

            ValidateS3BucketPolicy(policy, _Settings.S3?.BucketName, bucket, objectKey);
            ValidateS3ObjectSecretPathPolicy(objectKey);

            ObjectStorageItem metadata = null;
            try
            {
                token.ThrowIfCancellationRequested();
                metadata = await _Storage.GetObjectMetadataAsync(bucket, objectKey, token).ConfigureAwait(false);
            }
            catch (NotImplementedException)
            {
                metadata = null;
            }

            if (!documentBacked && document == null)
            {
                Dictionary<string, AssistantDocument> mappedDocuments = await BuildObjectDocumentMapAsync(context, bucket, token).ConfigureAwait(false);
                mappedDocuments.TryGetValue(MakeObjectMapKey(bucket, objectKey), out document);
            }

            string contentType = !String.IsNullOrWhiteSpace(metadata?.ContentType)
                ? metadata.ContentType.Trim()
                : String.IsNullOrWhiteSpace(document?.ContentType) ? "application/octet-stream" : document.ContentType.Trim();
            ValidateS3ObjectShapePolicy(policy, objectKey, contentType);

            int rangeStart = GetInt(arguments, "range_start", 0);
            long objectSize = metadata?.SizeBytes > 0
                ? metadata.SizeBytes
                : document?.SizeBytes ?? 0;

            if (rangeStart < 0)
                throw new ArgumentOutOfRangeException(nameof(rangeStart), "range_start is outside the object.");
            if (objectSize > 0 && rangeStart > objectSize)
                throw new ArgumentOutOfRangeException(nameof(rangeStart), "range_start is outside the object.");

            int defaultRangeLength = objectSize > 0
                ? (int)Math.Min(objectSize - rangeStart, policy.MaxObjectReadBytes)
                : policy.MaxObjectReadBytes;
            int requestedRangeLength = GetInt(arguments, "range_length", defaultRangeLength);
            if (requestedRangeLength < 0)
                throw new ArgumentOutOfRangeException(nameof(requestedRangeLength), "range_length cannot be negative.");

            int cappedRangeLength = Math.Min(requestedRangeLength, policy.MaxObjectReadBytes);
            int rangeLength = objectSize > 0
                ? (int)Math.Min(Math.Max(0, objectSize - rangeStart), cappedRangeLength)
                : cappedRangeLength;

            if (objectSize <= 0 && requestedRangeLength > policy.MaxObjectReadBytes)
                throw new InvalidOperationException("Requested S3 range exceeds MaxObjectReadBytes and object metadata is unavailable.");

            token.ThrowIfCancellationRequested();
            byte[] segment = await _Storage.DownloadRangeAsync(bucket, objectKey, rangeStart, rangeLength, token).ConfigureAwait(false) ?? Array.Empty<byte>();

            string contentMode = NormalizeContentMode(GetString(arguments, "content_mode"), contentType, segment);
            string text = null;
            string base64 = null;
            bool textTruncated = false;

            if (String.Equals(contentMode, "base64", StringComparison.Ordinal))
            {
                if (!policy.AllowBinaryObjectOutput)
                    throw new InvalidOperationException("Binary S3 object output requires AllowBinaryObjectOutput in assistant policy.");

                base64 = Convert.ToBase64String(segment);
            }
            else if (String.Equals(contentMode, "text", StringComparison.Ordinal))
            {
                if (!IsTextLikeContentType(contentType) && LooksBinary(segment))
                    throw new InvalidOperationException("Object content appears to be binary. Use content_mode=base64 only when binary output is enabled, or content_mode=metadata_only.");

                text = DecodeUtf8(segment);
                text = SliceDecodedText(text, arguments, policy.MaxToolOutputChars, out textTruncated);
            }

            bool byteTruncated = (objectSize > 0 && rangeStart + rangeLength < objectSize)
                || requestedRangeLength > rangeLength
                || segment.Length < rangeLength;

            return new
            {
                Tool = "s3_object_read",
                DocumentBacked = documentBacked,
                DocumentId = document?.Id,
                DocumentName = document != null ? document.Name ?? document.OriginalFilename : null,
                ContentType = contentType,
                SizeBytes = objectSize > 0 ? objectSize : segment.Length,
                Bucket = bucket,
                ObjectKey = RedactObjectKey(objectKey, policy),
                ETag = metadata?.ETag,
                RangeStart = rangeStart,
                RangeLength = segment.Length,
                RangeEndExclusive = rangeStart + segment.Length,
                Truncated = byteTruncated || textTruncated,
                ContentMode = contentMode,
                Content = text,
                Base64 = base64,
                CitationHandle = document != null ? document.Id + ":object:" + rangeStart : null
            };
        }

        private async Task<object> ExecuteDocumentAtomExtractAsync(
            AssistantToolExecutionContext context,
            JsonElement arguments,
            CancellationToken token)
        {
            AssistantToolPolicy policy = context.Policy;
            string documentId = GetString(arguments, "document_id");
            string localAttachmentId = GetString(arguments, "local_attachment_id");
            if (String.IsNullOrWhiteSpace(documentId) == String.IsNullOrWhiteSpace(localAttachmentId))
                throw new ArgumentException("document_atom_extract requires exactly one of document_id or local_attachment_id.");

            AssistantDocument document = null;
            ChatLocalAttachmentContext localAttachment = null;
            string sourceType;
            string name;
            string contentType;
            string documentType = NormalizeDocumentType(GetString(arguments, "document_type"));
            byte[] sourceBytes;
            int sizeBytes;

            if (!String.IsNullOrWhiteSpace(documentId))
            {
                if (_Storage == null)
                    throw new InvalidOperationException("S3 storage service is not configured.");

                document = await _Database.AssistantDocument.ReadAsync(documentId, token).ConfigureAwait(false);
                if (!IsAvailableCollectionDocument(document, context))
                    throw new InvalidOperationException("Requested document_id is not available for this assistant: " + documentId + ".");
                if (String.IsNullOrWhiteSpace(document.S3Key))
                    throw new InvalidOperationException("Requested document does not reference an S3 object.");

                string bucket = String.IsNullOrWhiteSpace(document.BucketName) ? _Settings.S3?.BucketName : document.BucketName.Trim();
                string objectKey = document.S3Key.Trim();
                ValidateS3ObjectSecretPathPolicy(objectKey);
                ValidateS3BucketPolicy(policy, _Settings.S3?.BucketName, bucket, objectKey);

                ObjectStorageItem metadata = null;
                try
                {
                    metadata = await _Storage.GetObjectMetadataAsync(bucket, objectKey, token).ConfigureAwait(false);
                }
                catch (NotImplementedException)
                {
                    metadata = null;
                }

                contentType = !String.IsNullOrWhiteSpace(metadata?.ContentType)
                    ? metadata.ContentType.Trim()
                    : (String.IsNullOrWhiteSpace(document.ContentType) ? "application/octet-stream" : document.ContentType.Trim());
                ValidateS3ObjectShapePolicy(policy, objectKey, contentType);

                long sourceSize = metadata?.SizeBytes > 0 ? metadata.SizeBytes : document.SizeBytes;
                if (sourceSize > policy.MaxAtomExtractionBytes)
                    throw new InvalidOperationException("Requested document exceeds MaxAtomExtractionBytes for document_atom_extract.");

                int bytesToRead = sourceSize > 0
                    ? (int)sourceSize
                    : policy.MaxAtomExtractionBytes + 1;
                sourceBytes = await _Storage.DownloadRangeAsync(bucket, objectKey, 0, bytesToRead, token).ConfigureAwait(false) ?? Array.Empty<byte>();
                if (sourceBytes.Length > policy.MaxAtomExtractionBytes)
                    throw new InvalidOperationException("Requested document exceeds MaxAtomExtractionBytes for document_atom_extract.");

                sourceType = "assistant_document";
                name = document.Name ?? document.OriginalFilename ?? document.Id;
                sizeBytes = sourceBytes.Length;
            }
            else
            {
                localAttachment = (context.LocalAttachments ?? new List<ChatLocalAttachmentContext>())
                    .FirstOrDefault(item => String.Equals(item?.AttachmentId, localAttachmentId, StringComparison.OrdinalIgnoreCase));
                if (localAttachment == null)
                    throw new InvalidOperationException("Requested local_attachment_id is not available in this chat turn: " + localAttachmentId + ".");

                sourceBytes = localAttachment.SourceBytes;
                if ((sourceBytes == null || sourceBytes.Length == 0) && !String.IsNullOrWhiteSpace(localAttachment.Text))
                    sourceBytes = Encoding.UTF8.GetBytes(localAttachment.Text);
                if (sourceBytes == null || sourceBytes.Length == 0)
                    throw new InvalidOperationException("Requested local attachment does not have source bytes available.");
                if (sourceBytes.Length > policy.MaxAtomExtractionBytes)
                    throw new InvalidOperationException("Requested local attachment exceeds MaxAtomExtractionBytes for document_atom_extract.");

                sourceType = "local_attachment";
                name = localAttachment.Name ?? localAttachment.AttachmentId;
                contentType = String.IsNullOrWhiteSpace(localAttachment.ContentType) ? "application/octet-stream" : localAttachment.ContentType.Trim();
                sizeBytes = sourceBytes.Length;
                if (String.IsNullOrWhiteSpace(documentType))
                    documentType = NormalizeDocumentType(localAttachment.DocumentType);
            }

            if (String.IsNullOrWhiteSpace(documentType))
                documentType = ResolveDocumentType(name, contentType);

            string extractedText;
            bool usedDocumentAtom = false;
            if (IsTextLike(name, contentType) && TryDecodeUtf8(sourceBytes, out string decodedText))
            {
                extractedText = decodedText;
                if (String.IsNullOrWhiteSpace(documentType)) documentType = "text";
            }
            else
            {
                DocumentAtomAtomizationService atomization = new DocumentAtomAtomizationService(_Settings.DocumentAtom, _Logging);
                if (String.IsNullOrWhiteSpace(documentType))
                {
                    TypeDetectResponse detected = await atomization.DetectDocumentTypeAsync(documentId ?? localAttachmentId, sourceBytes, name, token).ConfigureAwait(false);
                    documentType = detected?.Type;
                }

                extractedText = await atomization.ExtractTextAsync(documentId ?? localAttachmentId, sourceBytes, documentType, name, token).ConfigureAwait(false);
                usedDocumentAtom = true;
            }

            if (String.IsNullOrWhiteSpace(extractedText))
                throw new InvalidOperationException("DocumentAtom extraction did not return readable text.");

            string normalizedText = NormalizeExtractedText(extractedText);
            int textStart = GetInt(arguments, "text_start", 0);
            if (textStart < 0 || textStart > normalizedText.Length)
                throw new ArgumentOutOfRangeException(nameof(textStart), "text_start is outside the extracted text.");

            int maxLength = Math.Min(policy.MaxAtomExtractionCharacters, policy.MaxToolOutputChars);
            int defaultTextLength = Math.Min(normalizedText.Length - textStart, maxLength);
            int requestedTextLength = GetInt(arguments, "text_length", defaultTextLength);
            if (requestedTextLength < 0)
                throw new ArgumentOutOfRangeException(nameof(requestedTextLength), "text_length cannot be negative.");

            int textLength = Math.Min(requestedTextLength, defaultTextLength);
            string outputText = normalizedText.Substring(textStart, textLength);
            bool truncated = textStart + textLength < normalizedText.Length || requestedTextLength > textLength;

            return new
            {
                Tool = "document_atom_extract",
                SourceType = sourceType,
                DocumentId = document?.Id,
                LocalAttachmentId = localAttachment?.AttachmentId,
                DocumentName = document != null ? document.Name ?? document.OriginalFilename : localAttachment?.Name,
                ContentType = contentType,
                SizeBytes = sizeBytes,
                DocumentType = documentType,
                UsedDocumentAtom = usedDocumentAtom,
                TextStart = textStart,
                TextLength = outputText.Length,
                TotalTextCharacters = normalizedText.Length,
                Truncated = truncated,
                Text = outputText,
                CitationHandle = document != null ? document.Id + ":atom:" + textStart : null
            };
        }

        private async Task<object> ExecuteCollectionEnumerateDocumentsAsync(
            AssistantToolExecutionContext context,
            JsonElement arguments,
            CancellationToken token)
        {
            int maxResults = Math.Clamp(GetInt(arguments, "max_results", 100), 1, MinLimit(context.Policy.MaxSearchResultsPerCall, context.Policy.MaxToolResultItems));
            string continuationToken = GetString(arguments, "continuation_token");
            string queryText = GetString(arguments, "query");
            string contentType = GetString(arguments, "content_type");
            string statusText = GetString(arguments, "status");
            string sourceUrlContains = GetString(arguments, "source_url_contains");
            ChatMetadataFilter requestMetadataFilter = BuildModelMetadataFilter(arguments);
            int maxDocumentsToScan = Math.Max(maxResults, context.Policy.MaxDocumentsConsideredPerSearch);
            if (!String.IsNullOrWhiteSpace(continuationToken) && !Int32.TryParse(continuationToken, out _))
                throw new ArgumentException("collection_enumerate_documents requires continuation_token to be empty or an exact ContinuationToken value returned by a previous collection_enumerate_documents response.");
            if (!String.IsNullOrWhiteSpace(statusText) && !context.Policy.AllowNonCompletedDocumentMetadata)
                throw new InvalidOperationException("status filtering requires AllowNonCompletedDocumentMetadata in assistant policy.");
            if (!String.IsNullOrWhiteSpace(sourceUrlContains) && !context.Policy.AllowDocumentSourceUrls)
                throw new InvalidOperationException("source_url_contains requires AllowDocumentSourceUrls in assistant policy.");

            DocumentStatusEnum? requestedStatus = TryParseDocumentStatus(statusText);
            List<object> items = new List<object>();
            string scanToken = continuationToken;
            int rawOffset = 0;
            if (!String.IsNullOrWhiteSpace(continuationToken))
                Int32.TryParse(continuationToken, out rawOffset);

            int documentsScanned = 0;
            long totalRecords = 0;
            long recordsRemaining = 0;
            bool endOfResults = true;
            bool scanLimitReached = false;
            string responseContinuationToken = null;

            while (items.Count < maxResults)
            {
                int remainingScan = maxDocumentsToScan - documentsScanned;
                if (remainingScan <= 0)
                {
                    scanLimitReached = true;
                    endOfResults = false;
                    responseContinuationToken = rawOffset.ToString();
                    recordsRemaining = Math.Max(0, totalRecords - rawOffset);
                    break;
                }

                EnumerationQuery query = new EnumerationQuery
                {
                    MaxResults = Math.Clamp(Math.Min(maxResults, remainingScan), 1, 1000),
                    ContinuationToken = scanToken,
                    CollectionIdFilter = context.Settings.CollectionId
                };

                token.ThrowIfCancellationRequested();
                EnumerationResult<AssistantDocument> documents = await _Database.AssistantDocument
                    .EnumerateAsync(context.Assistant.TenantId, query, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                totalRecords = documents.TotalRecords;
                List<AssistantDocument> pageDocuments = documents.Objects ?? new List<AssistantDocument>();
                if (pageDocuments.Count == 0)
                {
                    endOfResults = documents.EndOfResults || String.IsNullOrWhiteSpace(documents.ContinuationToken);
                    responseContinuationToken = endOfResults ? null : documents.ContinuationToken;
                    recordsRemaining = documents.RecordsRemaining;
                    break;
                }

                foreach (AssistantDocument doc in pageDocuments)
                {
                    documentsScanned++;
                    rawOffset++;

                    if (MatchesCollectionEnumerationFilters(
                        doc,
                        context,
                        requestedStatus,
                        requestMetadataFilter,
                        queryText,
                        contentType,
                        sourceUrlContains))
                    {
                        items.Add(BuildDocumentEnumerationItem(doc, context.Policy));
                        if (items.Count >= maxResults)
                            break;
                    }

                    if (documentsScanned >= maxDocumentsToScan)
                    {
                        scanLimitReached = !documents.EndOfResults || rawOffset < totalRecords;
                        break;
                    }
                }

                if (items.Count >= maxResults || scanLimitReached)
                {
                    endOfResults = rawOffset >= totalRecords;
                    responseContinuationToken = endOfResults ? null : rawOffset.ToString();
                    recordsRemaining = Math.Max(0, totalRecords - rawOffset);
                    break;
                }

                if (documents.EndOfResults || String.IsNullOrWhiteSpace(documents.ContinuationToken))
                {
                    endOfResults = true;
                    responseContinuationToken = null;
                    recordsRemaining = 0;
                    break;
                }

                if (String.Equals(scanToken, documents.ContinuationToken, StringComparison.Ordinal))
                {
                    endOfResults = false;
                    responseContinuationToken = documents.ContinuationToken;
                    recordsRemaining = documents.RecordsRemaining;
                    break;
                }

                scanToken = documents.ContinuationToken;
            }

            return new
            {
                Tool = "collection_enumerate_documents",
                CollectionId = context.Settings.CollectionId,
                MaxResults = maxResults,
                ContinuationToken = responseContinuationToken,
                EndOfResults = endOfResults,
                TotalRecords = totalRecords,
                RecordsRemaining = recordsRemaining,
                PageRecords = items.Count,
                MoreResultsAvailable = !endOfResults,
                DocumentsScanned = documentsScanned,
                MaxDocumentsScanned = maxDocumentsToScan,
                ScanLimitReached = scanLimitReached,
                Objects = items
            };
        }

        private static bool MatchesCollectionEnumerationFilters(
            AssistantDocument doc,
            AssistantToolExecutionContext context,
            DocumentStatusEnum? requestedStatus,
            ChatMetadataFilter requestMetadataFilter,
            string queryText,
            string contentType,
            string sourceUrlContains)
        {
            if (doc == null || context == null) return false;

            if (context.Policy.AllowNonCompletedDocumentMetadata)
            {
                if (requestedStatus.HasValue && doc.Status != requestedStatus.Value)
                    return false;
            }
            else if (doc.Status != DocumentStatusEnum.Completed)
            {
                return false;
            }

            if (!AssistantDocumentPolicyFilter.MatchesAssistantMetadataFilters(doc, context.Settings))
                return false;
            if (requestMetadataFilter != null && !AssistantDocumentPolicyFilter.MatchesMetadataFilter(doc, requestMetadataFilter))
                return false;
            if (!String.IsNullOrWhiteSpace(queryText)
                && !Contains(doc.Name, queryText)
                && !Contains(doc.OriginalFilename, queryText)
                && !Contains(doc.ContentType, queryText)
                && !(context.Policy.AllowDocumentSourceUrls && Contains(doc.SourceUrl, queryText)))
                return false;
            if (!String.IsNullOrWhiteSpace(contentType) && !Contains(doc.ContentType, contentType))
                return false;
            if (!String.IsNullOrWhiteSpace(sourceUrlContains) && !Contains(doc.SourceUrl, sourceUrlContains))
                return false;

            return true;
        }

        private async Task<object> ExecuteBucketEnumerateObjectsAsync(
            AssistantToolExecutionContext context,
            JsonElement arguments,
            CancellationToken token)
        {
            if (_Storage == null)
                throw new InvalidOperationException("S3 storage service is not configured.");

            AssistantToolPolicy policy = context.Policy;
            string defaultBucket = _Settings.S3?.BucketName;
            string bucket = GetStringAny(arguments, "bucket", "bucket_name") ?? defaultBucket;
            if (String.IsNullOrWhiteSpace(bucket))
                throw new InvalidOperationException("S3 bucket is not configured.");

            ValidateS3BucketNamePolicy(policy, defaultBucket, bucket);

            List<string> allowedPrefixes = policy.AllowedBucketPrefixes ?? new List<string>();
            if (allowedPrefixes.Count == 0)
                throw new InvalidOperationException("bucket_enumerate_objects requires at least one AllowedBucketPrefixes entry in assistant policy.");

            string prefix = GetString(arguments, "prefix");
            if (String.IsNullOrWhiteSpace(prefix))
            {
                if (allowedPrefixes.Count == 1)
                    prefix = allowedPrefixes[0];
                else
                    throw new ArgumentException("bucket_enumerate_objects requires prefix when multiple allowed prefixes are configured.");
            }

            if (!allowedPrefixes.Any(allowed => prefix.StartsWith(allowed, StringComparison.Ordinal)))
                throw new InvalidOperationException("Requested S3 object key prefix is not allowed by assistant policy.");

            string suffix = GetString(arguments, "suffix");
            string contentType = GetString(arguments, "content_type");
            int bucketMaxResults = MinLimit(policy.MaxSearchResultsPerCall, policy.MaxBucketEnumerationResults, policy.MaxToolResultItems);
            int maxResults = Math.Clamp(GetInt(arguments, "max_results", bucketMaxResults), 1, bucketMaxResults);
            string continuationToken = GetString(arguments, "continuation_token");

            token.ThrowIfCancellationRequested();
            ObjectStorageListResult listing = await _Storage.ListObjectsAsync(
                bucket,
                prefix,
                maxResults,
                continuationToken,
                token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            Dictionary<string, AssistantDocument> mappedDocuments = await BuildObjectDocumentMapAsync(context, bucket, token).ConfigureAwait(false);
            List<object> objects = new List<object>();

            foreach (ObjectStorageItem item in listing.Objects ?? new List<ObjectStorageItem>())
            {
                token.ThrowIfCancellationRequested();
                if (item == null || String.IsNullOrWhiteSpace(item.Key)) continue;
                if (!String.IsNullOrWhiteSpace(suffix) && !item.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
                if (!IsAllowedObjectSuffix(policy, item.Key)) continue;

                mappedDocuments.TryGetValue(MakeObjectMapKey(bucket, item.Key), out AssistantDocument document);
                string effectiveContentType = !String.IsNullOrWhiteSpace(item.ContentType)
                    ? item.ContentType
                    : document?.ContentType;
                if (!String.IsNullOrWhiteSpace(contentType)
                    && !String.Equals(effectiveContentType, contentType, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!IsAllowedContentType(policy, effectiveContentType)) continue;

                objects.Add(new
                {
                    Bucket = bucket,
                    Key = RedactObjectKey(item.Key, policy),
                    item.SizeBytes,
                    ContentType = effectiveContentType,
                    item.LastModifiedUtc,
                    item.ETag,
                    DocumentId = document?.Id,
                    DocumentName = document != null ? document.Name ?? document.OriginalFilename : null,
                    ReadAllowed = document != null && IsAvailableCollectionDocument(document, context)
                });
            }

            return new
            {
                Tool = "bucket_enumerate_objects",
                Bucket = bucket,
                Prefix = prefix,
                Suffix = suffix,
                ContentType = contentType,
                MaxResults = maxResults,
                listing.ContinuationToken,
                listing.EndOfResults,
                TotalRecords = objects.Count,
                Objects = objects
            };
        }

        private async Task<object> ExecuteWebSearchAsync(
            AssistantToolExecutionContext context,
            JsonElement arguments,
            CancellationToken token)
        {
            string query = GetString(arguments, "query");
            if (String.IsNullOrWhiteSpace(query))
                throw new ArgumentException("web_search requires query.");

            AssistantToolPolicy policy = context.Policy;
            ExternalSearchProviderSettings provider = ResolveTavilyProvider(policy);
            if (provider == null)
            {
                _Logging.Warn(_Header + "web_search requested but Tavily is not configured globally or on assistant " + context.Assistant.Id + ".");
                throw new InvalidOperationException("Tavily web search is not configured.");
            }
            if (policy.AllowedProviders.Count > 0
                && !policy.AllowedProviders.Contains("Tavily", StringComparer.OrdinalIgnoreCase)
                && !policy.AllowedProviders.Contains(provider.ProviderType, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Tavily web search provider is not allowed by assistant policy.");

            List<string> includeDomains = GetStringList(arguments, "include_domains");
            List<string> excludeDomains = GetStringList(arguments, "exclude_domains");
            ValidatePublicWebSearchBoundary(query, includeDomains);
            ExternalSearchSettings externalSearch = _Settings.ExternalSearch ?? new ExternalSearchSettings();
            ExternalSearchConfigurationHelper.Normalize(externalSearch);

            if (externalSearch.IncludeDomains.Count > 0)
            {
                includeDomains = includeDomains.Count > 0
                    ? includeDomains.Where(domain => externalSearch.IncludeDomains.Contains(domain, StringComparer.OrdinalIgnoreCase)).ToList()
                    : new List<string>(externalSearch.IncludeDomains);
            }

            if (policy.AllowedWebDomains.Count > 0)
            {
                includeDomains = includeDomains.Count > 0
                    ? includeDomains.Where(domain => policy.AllowedWebDomains.Contains(domain, StringComparer.OrdinalIgnoreCase)).ToList()
                    : new List<string>(policy.AllowedWebDomains);
            }

            excludeDomains = excludeDomains
                .Concat(externalSearch.ExcludeDomains)
                .Concat(policy.BlockedWebDomains)
                .Where(value => !String.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            int maxResults = MinLimit(policy.MaxWebResults, 20, policy.MaxSearchResultsPerCall, externalSearch.MaxResults, policy.MaxToolResultItems);
            bool allowRawContent = policy.AllowRawWebContent && externalSearch.AllowRawContent;
            string searchDepth = GetString(arguments, "search_depth") ?? policy.SearchDepth ?? "basic";
            if (String.Equals(searchDepth, "advanced", StringComparison.OrdinalIgnoreCase) && !policy.AllowAdvancedSearchDepth)
                searchDepth = "basic";

            string topic = GetString(arguments, "topic") ?? "general";
            if (String.Equals(topic, "news", StringComparison.OrdinalIgnoreCase) && !policy.AllowNewsTopic)
                topic = "general";
            string includeAnswerMode = ResolveTavilyIncludeAnswerMode(arguments);

            WebSearchRequest webSearchRequest = new WebSearchRequest
            {
                Query = query,
                MaxResults = Math.Clamp(GetInt(arguments, "max_results", maxResults), 1, maxResults),
                SearchDepth = searchDepth,
                Topic = topic,
                TimeRange = GetString(arguments, "time_range"),
                StartDate = GetString(arguments, "start_date"),
                EndDate = GetString(arguments, "end_date"),
                IncludeAnswerMode = includeAnswerMode,
                IncludeRawContentMode = allowRawContent && GetBool(arguments, "include_raw_content", false) ? "basic" : null,
                IncludeImages = policy.AllowWebImages && GetBool(arguments, "include_images", false),
                IncludeImageDescriptions = policy.AllowWebImages && GetBool(arguments, "include_image_descriptions", false),
                IncludeDomains = includeDomains,
                ExcludeDomains = excludeDomains,
                Country = GetString(arguments, "country"),
                SafeSearch = policy.RequireSafeSearch || externalSearch.SafeSearch || GetBool(arguments, "safe_search", false)
            };

            IWebSearchService webSearch = new WebSearchService(provider, _TavilyHttpClient);
            WebSearchResponse response = await webSearch.SearchAsync(webSearchRequest, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            if (!policy.AllowWebImages)
                response.Images.Clear();
            if (!allowRawContent)
                foreach (WebSearchResultItem item in response.Results)
                    item.RawContent = null;
            if (response.Results.Count > webSearchRequest.MaxResults)
                response.Results = response.Results.Take(webSearchRequest.MaxResults).ToList();
            if (!policy.AllowWebImages)
                foreach (WebSearchResultItem item in response.Results)
                    item.Images.Clear();
            response.RequestId = null;
            response.Attempts.Clear();

            return response;
        }

        private async Task<string> ResolveVerbexIndexIdAsync(AssistantToolExecutionContext context, string requestedIndexId, CancellationToken token)
        {
            AssistantToolPolicy policy = context.Policy;
            string defaultIndexId = await ResolveDefaultVerbexIndexIdAsync(context.Assistant.TenantId, token).ConfigureAwait(false);
            if (!String.IsNullOrWhiteSpace(policy.DefaultIndexId))
                defaultIndexId = policy.DefaultIndexId;
            string indexId = String.IsNullOrWhiteSpace(requestedIndexId) ? defaultIndexId : requestedIndexId.Trim();
            List<string> allowedIndexIds = await ResolveAllowedVerbexIndexIdsAsync(context, defaultIndexId, token).ConfigureAwait(false);

            if (!allowedIndexIds.Contains(indexId, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Requested Verbex index is not allowed by assistant policy.");

            return indexId;
        }

        private async Task<List<string>> ResolveAllowedVerbexIndexIdsAsync(
            AssistantToolExecutionContext context,
            string defaultIndexId,
            CancellationToken token)
        {
            List<string> allowed = new List<string>();
            void Add(string value)
            {
                if (!String.IsNullOrWhiteSpace(value)
                    && !allowed.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
                    allowed.Add(value.Trim());
            }

            Add(defaultIndexId);
            Add(context.Policy?.DefaultIndexId);
            foreach (string indexId in context.Policy?.AllowedVerbexIndexIds ?? new List<string>())
                Add(indexId);

            try
            {
                EnumerationResult<AssistantDocument> documents = await _Database.AssistantDocument.EnumerateAsync(
                    context.Assistant.TenantId,
                    new EnumerationQuery
                    {
                        CollectionIdFilter = context.Settings.CollectionId,
                        MaxResults = 1000
                    },
                    token).ConfigureAwait(false);

                foreach (AssistantDocument document in documents.Objects ?? new List<AssistantDocument>())
                {
                    if (!IsAvailableCollectionDocument(document, context)) continue;
                    Add(document.VerbexIndexId);
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "failed to resolve document-scoped Verbex indices for assistant " + context.Assistant.Id + ": " + e.Message);
            }

            return allowed;
        }

        private static string NormalizeCollectionSearchStrategy(string strategy)
        {
            if (String.IsNullOrWhiteSpace(strategy)) return "multi_query";

            string normalized = strategy.Trim().ToLowerInvariant();
            if (normalized == "single"
                || normalized == "multi_query"
                || normalized == "broad"
                || normalized == "narrow"
                || normalized == "exhaustive")
                return normalized;

            throw new ArgumentException("collection_search strategy must be single, multi_query, broad, narrow, or exhaustive.");
        }

        private static List<string> BuildServerGeneratedQueryVariants(IEnumerable<string> queries)
        {
            List<string> variants = new List<string>();
            foreach (string query in queries ?? new List<string>())
            {
                if (String.IsNullOrWhiteSpace(query)) continue;

                string withoutQuotes = NormalizeQueryVariant(query.Replace("\"", " ").Replace("'", " "));
                AddQueryVariant(variants, query, withoutQuotes);

                string punctuationAsSpaces = NormalizeQueryVariant(Regex.Replace(query, @"[\p{P}\p{S}]+", " "));
                AddQueryVariant(variants, query, punctuationAsSpaces);
            }

            return variants
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string NormalizeQueryVariant(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;
            string normalized = Regex.Replace(value, @"\s+", " ").Trim();
            return normalized.Length > 1 ? normalized : null;
        }

        private static void AddQueryVariant(List<string> variants, string original, string candidate)
        {
            if (String.IsNullOrWhiteSpace(candidate)) return;
            if (String.Equals(candidate, original?.Trim(), StringComparison.OrdinalIgnoreCase)) return;
            if (variants.Contains(candidate, StringComparer.OrdinalIgnoreCase)) return;
            variants.Add(candidate);
        }

        private static string NormalizeCollectionSearchMode(string searchMode, string fallback)
        {
            string normalized = String.IsNullOrWhiteSpace(searchMode) ? fallback : searchMode.Trim();
            if (String.IsNullOrWhiteSpace(normalized)) normalized = "Hybrid";

            if (String.Equals(normalized, "Auto", StringComparison.OrdinalIgnoreCase))
                return NormalizeCollectionSearchMode(fallback, "Hybrid");
            if (String.Equals(normalized, "Vector", StringComparison.OrdinalIgnoreCase)) return "Vector";
            if (String.Equals(normalized, "FullText", StringComparison.OrdinalIgnoreCase)) return "FullText";
            if (String.Equals(normalized, "Hybrid", StringComparison.OrdinalIgnoreCase)) return "Hybrid";

            throw new ArgumentException("collection_search search_mode must be Vector, FullText, Hybrid, or Auto.");
        }

        private static List<string> ResolveCollectionSearchModes(string strategy, string requestedSearchMode, string defaultSearchMode, AssistantToolPolicy policy)
        {
            List<string> allowedModes = policy?.AllowedSearchModes ?? new List<string> { "Vector", "FullText", "Hybrid" };
            if (String.Equals(strategy, "exhaustive", StringComparison.Ordinal)
                && (String.IsNullOrWhiteSpace(requestedSearchMode)
                    || String.Equals(requestedSearchMode.Trim(), "Auto", StringComparison.OrdinalIgnoreCase)))
            {
                List<string> exhaustiveModes = new List<string> { "FullText", "Vector", "Hybrid" }
                    .Where(mode => allowedModes.Contains(mode, StringComparer.OrdinalIgnoreCase))
                    .ToList();
                if (exhaustiveModes.Count == 0)
                    throw new InvalidOperationException("No collection search modes are allowed by assistant policy.");
                return exhaustiveModes;
            }

            string mode = NormalizeCollectionSearchMode(requestedSearchMode, defaultSearchMode);
            if (!allowedModes.Contains(mode, StringComparer.OrdinalIgnoreCase))
            {
                if (String.IsNullOrWhiteSpace(requestedSearchMode))
                    mode = allowedModes.FirstOrDefault();
            }

            if (String.IsNullOrWhiteSpace(mode) || !allowedModes.Contains(mode, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Requested collection search_mode is not allowed by assistant policy.");
            
            return new List<string> { mode };
        }

        private static List<string> BuildExactPhraseQueries(List<string> queries)
        {
            List<string> exact = new List<string>();
            foreach (string query in queries ?? new List<string>())
            {
                if (String.IsNullOrWhiteSpace(query)) continue;

                foreach (Match match in Regex.Matches(query, "\"([^\"]{2,200})\"|'([^']{2,200})'"))
                {
                    string value = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                    AddExactQuery(exact, value);
                }

                foreach (string token in query.Split(new[] { ' ', '\t', '\r', '\n', ',', ';', ':', '(', ')', '[', ']', '{', '}' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = token.Trim('"', '\'', '.', '?', '!');
                    if (LooksLikeIdentifier(trimmed))
                        AddExactQuery(exact, trimmed);
                }
            }

            return exact;
        }

        private static void AddExactQuery(List<string> exact, string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return;
            string normalized = value.Trim();
            if (normalized.Length < 2) return;
            if (!exact.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                exact.Add(normalized);
        }

        private static bool LooksLikeIdentifier(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return false;
            string normalized = value.Trim();
            if (normalized.Length < 4 || normalized.Length > 120) return false;

            bool hasSeparator = normalized.Any(c => c == '-' || c == '_' || c == '.' || c == '/' || c == '\\' || c == '#');
            bool hasDigit = normalized.Any(Char.IsDigit);
            bool mostlyIdentifierChars = normalized.All(c => Char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' || c == '/' || c == '\\' || c == '#');
            return mostlyIdentifierChars && (hasSeparator || hasDigit);
        }

        private static string BuildCollectionResultBucket(string searchMode, bool exactPhrasePass, RetrievalChunk chunk)
        {
            if (exactPhrasePass) return "exact";

            double bestScore = Math.Max(chunk?.Score ?? 0, Math.Max(chunk?.TextScore ?? 0, chunk?.FusionScore ?? 0));
            if (bestScore > 0 && bestScore < 0.25) return "low_confidence";

            if (String.Equals(searchMode, "FullText", StringComparison.OrdinalIgnoreCase)) return "full_text";
            if (String.Equals(searchMode, "Vector", StringComparison.OrdinalIgnoreCase)) return "semantic";
            if (String.Equals(searchMode, "Hybrid", StringComparison.OrdinalIgnoreCase)) return "hybrid";
            return "semantic";
        }

        private static void IncrementBucket(Dictionary<string, int> buckets, string bucket)
        {
            if (buckets == null || String.IsNullOrWhiteSpace(bucket)) return;
            buckets.TryGetValue(bucket, out int current);
            buckets[bucket] = current + 1;
        }

        private static string BuildExcerpt(string content, int maxLength)
        {
            if (String.IsNullOrEmpty(content)) return content;
            if (content.Length <= maxLength) return content;
            return content.Substring(0, maxLength);
        }

        private async Task<string> ResolveDefaultVerbexIndexIdAsync(string assistantHubTenantId, CancellationToken token)
        {
            string configuredIndexId = String.IsNullOrWhiteSpace(_Settings.Verbex?.DefaultIndexId)
                ? "default"
                : _Settings.Verbex.DefaultIndexId;
            string effectiveTenantId = String.IsNullOrWhiteSpace(assistantHubTenantId) ? "default" : assistantHubTenantId;
            string fallback = String.Equals(effectiveTenantId, "default", StringComparison.OrdinalIgnoreCase)
                ? configuredIndexId
                : effectiveTenantId + "_" + configuredIndexId;

            try
            {
                TenantMetadata tenant = await _Database.Tenant.ReadByIdAsync(effectiveTenantId, token).ConfigureAwait(false);
                if (tenant?.Tags != null
                    && tenant.Tags.TryGetValue(Constants.VerbexDefaultIndexIdTag, out string mappedIndexId)
                    && !String.IsNullOrWhiteSpace(mappedIndexId))
                    return mappedIndexId;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "failed to resolve Verbex default index for tenant " + effectiveTenantId + ": " + e.Message);
            }

            return fallback;
        }

        private async Task<List<string>> ResolveVerbexRecordIdFiltersAsync(
            AssistantToolExecutionContext context,
            string indexId,
            List<string> requestedRecordIds,
            CancellationToken token)
        {
            if (requestedRecordIds == null || requestedRecordIds.Count == 0) return null;

            List<string> normalized = requestedRecordIds
                .Where(recordId => !String.IsNullOrWhiteSpace(recordId))
                .Select(recordId => recordId.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(context.Policy.MaxToolResultItems)
                .ToList();
            if (normalized.Count == 0) return null;

            foreach (string recordId in normalized)
            {
                token.ThrowIfCancellationRequested();
                AssistantDocument mapped = await ResolveVerbexMappedDocumentAsync(
                    context,
                    null,
                    recordId,
                    indexId,
                    token).ConfigureAwait(false);

                if (mapped == null)
                    throw new InvalidOperationException("Requested Verbex record_id is not available for this assistant: " + recordId + ".");
            }

            return normalized;
        }

        private async Task<AssistantDocument> ResolveVisibleChunkDocumentAsync(
            AssistantToolExecutionContext context,
            string documentId,
            CancellationToken token)
        {
            if (String.IsNullOrWhiteSpace(documentId)) return null;

            try
            {
                return await _Database.AssistantDocument.ReadAsync(documentId, token).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        private static ChatMetadataFilter BuildModelMetadataFilter(JsonElement arguments)
        {
            List<string> requiredLabels = GetStringList(arguments, "labels")
                .Concat(GetStringList(arguments, "required_labels"))
                .Where(value => !String.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            List<string> excludedLabels = GetStringList(arguments, "excluded_labels")
                .Where(value => !String.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<ChatTagCondition> requiredTags = GetTagConditions(arguments, "tags")
                .Concat(GetTagConditions(arguments, "required_tags"))
                .GroupBy(TagConditionKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            List<ChatTagCondition> excludedTags = GetTagConditions(arguments, "excluded_tags")
                .GroupBy(TagConditionKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            ChatMetadataFilter filter = new ChatMetadataFilter
            {
                RequiredLabels = requiredLabels.Count > 0 ? requiredLabels : null,
                ExcludedLabels = excludedLabels.Count > 0 ? excludedLabels : null,
                RequiredTags = requiredTags.Count > 0 ? requiredTags : null,
                ExcludedTags = excludedTags.Count > 0 ? excludedTags : null
            };

            return filter.IsEmpty ? null : filter;
        }

        private static List<ChatTagCondition> GetTagConditions(JsonElement arguments, string name)
        {
            List<ChatTagCondition> ret = new List<ChatTagCondition>();
            if (arguments.ValueKind != JsonValueKind.Object) return ret;
            if (!TryGetPropertyIgnoreCase(arguments, name, out JsonElement value)) return ret;

            if (value.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in value.EnumerateObject())
                {
                    if (String.IsNullOrWhiteSpace(property.Name)) continue;
                    ret.Add(new ChatTagCondition
                    {
                        Key = property.Name.Trim(),
                        Condition = "Equals",
                        Value = property.Value.ValueKind == JsonValueKind.String
                            ? property.Value.GetString()
                            : property.Value.GetRawText()
                    });
                }
            }
            else if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    string key = GetStringAny(item, "key", "Key");
                    if (String.IsNullOrWhiteSpace(key)) continue;
                    ret.Add(new ChatTagCondition
                    {
                        Key = key,
                        Condition = GetStringAny(item, "condition", "Condition") ?? "Equals",
                        Value = GetStringAny(item, "value", "Value")
                    });
                }
            }

            return ret;
        }

        private static string TagConditionKey(ChatTagCondition condition)
        {
            if (condition == null) return "";
            return (condition.Key ?? "").Trim() + "|"
                + (condition.Condition ?? "").Trim() + "|"
                + (condition.Value ?? "").Trim();
        }

        private static DocumentStatusEnum? TryParseDocumentStatus(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;
            if (Enum.TryParse(value.Trim(), true, out DocumentStatusEnum status)) return status;
            throw new ArgumentException("status is not a valid document status.");
        }

        private static object BuildDocumentEnumerationItem(AssistantDocument document, AssistantToolPolicy policy)
        {
            AssistantDocumentSelectionItem item = AssistantDocumentSelectionItem.FromDocument(
                document,
                policy.AllowDocumentSourceUrls);

            if (item == null) return null;

            bool includeLabels = policy.ReturnLabels || policy.AllowDocumentMetadataDetails;
            bool includeTags = policy.ReturnTags || policy.AllowDocumentMetadataDetails;
            bool includeStatus = policy.AllowNonCompletedDocumentMetadata;
            if (!includeLabels && !includeTags && !includeStatus)
                return item;

            return new
            {
                item.Id,
                item.Name,
                item.OriginalFilename,
                item.ContentType,
                item.SizeBytes,
                item.SourceUrl,
                Status = includeStatus ? document.Status.ToString() : null,
                Labels = includeLabels ? AssistantDocumentPolicyFilter.ParseLabels(document.Labels).ToList() : null,
                Tags = includeTags ? AssistantDocumentPolicyFilter.ParseTags(document.Tags) : null,
                item.CreatedUtc,
                item.LastUpdateUtc
            };
        }

        private async Task<List<string>> ResolveCollectionDocumentIdsAsync(
            AssistantToolExecutionContext context,
            List<string> requestedDocumentIds,
            CancellationToken token)
        {
            if (requestedDocumentIds == null || requestedDocumentIds.Count == 0) return null;
            if (!context.Policy.AllowModelDocumentIdFilter)
                throw new InvalidOperationException("Model-supplied document_id filters are disabled by assistant policy.");

            requestedDocumentIds = requestedDocumentIds
                .Where(documentId => !String.IsNullOrWhiteSpace(documentId))
                .Select(documentId => documentId.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            List<string> resolved = new List<string>();
            foreach (string documentId in requestedDocumentIds)
            {
                token.ThrowIfCancellationRequested();
                AssistantDocument document = await _Database.AssistantDocument.ReadAsync(documentId, token).ConfigureAwait(false);
                if (!IsAvailableCollectionDocument(document, context))
                    throw new InvalidOperationException("Requested document_id is not available for this assistant: " + documentId + ".");

                resolved.Add(document.Id);
            }

            return resolved
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static bool IsAvailableCollectionDocument(AssistantDocument document, AssistantToolExecutionContext context)
        {
            if (document == null || context == null || context.Assistant == null || context.Settings == null) return false;
            if (!String.Equals(document.TenantId, context.Assistant.TenantId, StringComparison.Ordinal)) return false;
            if (!String.Equals(document.CollectionId, context.Settings.CollectionId, StringComparison.Ordinal)) return false;
            if (document.Status != DocumentStatusEnum.Completed) return false;
            return AssistantDocumentPolicyFilter.MatchesAssistantMetadataFilters(document, context.Settings);
        }

        private static List<string> ParseChunkRecordIds(string chunkRecordIdsJson)
        {
            if (String.IsNullOrWhiteSpace(chunkRecordIdsJson)) return new List<string>();

            try
            {
                List<string> recordIds = JsonSerializer.Deserialize<List<string>>(chunkRecordIdsJson, _JsonOptions);
                if (recordIds == null) return new List<string>();

                return recordIds
                    .Where(recordId => !String.IsNullOrWhiteSpace(recordId))
                    .Select(recordId => recordId.Trim())
                    .ToList();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }

        private static int? ResolveChunkPosition(List<string> chunkRecordIds, string recordId)
        {
            if (chunkRecordIds == null || chunkRecordIds.Count < 1 || String.IsNullOrWhiteSpace(recordId))
                return null;

            int index = chunkRecordIds.FindIndex(id => String.Equals(id, recordId, StringComparison.Ordinal));
            return index >= 0 ? index : null;
        }

        private static List<object> BuildDocumentFollowUpCalls(
            AssistantToolExecutionContext context,
            AssistantDocument document,
            List<string> chunkRecordIds,
            int? chunkPosition)
        {
            List<object> calls = new List<object>();
            if (document == null) return calls;

            if (chunkPosition.HasValue)
            {
                calls.Add(new
                {
                    Tool = "collection_read_chunks",
                    Arguments = new Dictionary<string, object>
                    {
                        ["document_id"] = document.Id,
                        ["positions"] = new List<int> { chunkPosition.Value }
                    },
                    Reason = "Read the matching collection chunk."
                });
            }
            else if (chunkRecordIds != null && chunkRecordIds.Count > 0)
            {
                calls.Add(new
                {
                    Tool = "collection_read_chunks",
                    Arguments = new Dictionary<string, object>
                    {
                        ["document_id"] = document.Id,
                        ["ranges"] = new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object>
                            {
                                ["start_position"] = 0,
                                ["count"] = Math.Min(3, chunkRecordIds.Count)
                            }
                        }
                    },
                    Reason = "Inspect nearby collection chunks for the mapped document."
                });
            }

            if (context?.Policy?.EnableS3ObjectReadTool == true && !String.IsNullOrWhiteSpace(document.S3Key))
            {
                calls.Add(new
                {
                    Tool = "s3_object_read",
                    Arguments = new Dictionary<string, object>
                    {
                        ["document_id"] = document.Id,
                        ["content_mode"] = "text"
                    },
                    Reason = "Read the document-backed source object when chunk evidence is insufficient."
                });
            }

            return calls;
        }

        private static SortedSet<int> ResolveRequestedChunkPositions(JsonElement arguments, int availableChunkCount, int maxRanges)
        {
            SortedSet<int> positions = new SortedSet<int>();

            foreach (int position in GetIntList(arguments, "positions"))
            {
                if (position < 0 || position >= availableChunkCount)
                    throw new ArgumentException("Requested chunk position is outside the document: " + position + ".");

                positions.Add(position);
            }

            List<(int StartPosition, int Count)> ranges = GetChunkRanges(arguments, "ranges");
            if (ranges.Count > maxRanges)
                throw new ArgumentException("collection_read_chunks ranges exceeds assistant policy limit of " + maxRanges + ".");

            foreach ((int StartPosition, int Count) range in ranges)
            {
                if (range.StartPosition < 0 || range.StartPosition >= availableChunkCount)
                    throw new ArgumentException("Requested chunk range start is outside the document: " + range.StartPosition + ".");

                int endExclusive = Math.Min(availableChunkCount, range.StartPosition + Math.Max(0, range.Count));
                for (int position = range.StartPosition; position < endExclusive; position++)
                    positions.Add(position);
            }

            return positions;
        }

        private static SortedSet<int> ExpandChunkPositions(SortedSet<int> requestedPositions, int neighborWindow, int availableChunkCount)
        {
            SortedSet<int> expanded = new SortedSet<int>();
            if (requestedPositions == null) return expanded;

            foreach (int requestedPosition in requestedPositions)
            {
                int start = Math.Max(0, requestedPosition - neighborWindow);
                int end = Math.Min(availableChunkCount - 1, requestedPosition + neighborWindow);
                for (int position = start; position <= end; position++)
                    expanded.Add(position);
            }

            return expanded;
        }

        private async Task<List<object>> NormalizeVerbexSearchResultsAsync(
            AssistantToolExecutionContext context,
            string indexId,
            JsonElement? resultsElement,
            List<string> recordIdFilters,
            int maxResults,
            CancellationToken token)
        {
            List<object> results = new List<object>();
            if (!resultsElement.HasValue || resultsElement.Value.ValueKind != JsonValueKind.Array)
                return results;

            foreach (JsonElement item in resultsElement.Value.EnumerateArray())
            {
                if (results.Count >= maxResults) break;

                JsonElement record = GetObjectOrSelf(item, "Document", "Record", "Data");
                JsonElement metadata = GetObjectOrNull(record, "CustomMetadata") ?? GetObjectOrNull(item, "CustomMetadata") ?? default;
                string recordId = GetStringAny(item, "Id", "RecordId", "DocumentId") ?? GetStringAny(record, "Id", "RecordId", "DocumentId");
                string documentId = GetStringAny(item, "AssistantHubDocumentId", "DocumentId", "Id")
                    ?? GetStringAny(record, "AssistantHubDocumentId", "DocumentId", "Id")
                    ?? (metadata.ValueKind == JsonValueKind.Object ? GetStringAny(metadata, "AssistantHubDocumentId", "DocumentId") : null);

                if (String.IsNullOrWhiteSpace(documentId) && String.IsNullOrWhiteSpace(recordId))
                    continue;
                if (recordIdFilters != null
                    && (String.IsNullOrWhiteSpace(recordId)
                        || !recordIdFilters.Contains(recordId, StringComparer.OrdinalIgnoreCase)))
                    continue;

                AssistantDocument assistantDocument = await ResolveVerbexMappedDocumentAsync(
                    context,
                    documentId,
                    recordId,
                    indexId,
                    token).ConfigureAwait(false);
                if (assistantDocument == null)
                    continue;

                string content = GetStringAny(item, "Excerpt", "Snippet", "Content", "Text")
                    ?? GetStringAny(record, "Excerpt", "Snippet", "Content", "Text");
                if (!String.IsNullOrEmpty(content) && content.Length > 1000)
                    content = content.Substring(0, 1000);

                List<string> chunkRecordIds = ParseChunkRecordIds(assistantDocument.ChunkRecordIds);
                int? chunkPosition = ResolveChunkPosition(chunkRecordIds, recordId);

                results.Add(new
                {
                    IndexId = indexId,
                    RecordId = recordId,
                    DocumentId = assistantDocument.Id,
                    DocumentName = assistantDocument.Name ?? assistantDocument.OriginalFilename,
                    assistantDocument.ContentType,
                    Score = GetDoubleAny(item, "Score", "TextScore", "TfIdfScore", "SimilarityScore"),
                    Excerpt = content,
                    MatchedTerms = GetStringArray(item, "MatchedTerms", "Terms", "TermMatches"),
                    AvailableChunkCount = chunkRecordIds.Count,
                    ChunkPosition = chunkPosition,
                    CanReadChunks = chunkRecordIds.Count > 0,
                    CanReadSourceObject = context.Policy.EnableS3ObjectReadTool && !String.IsNullOrWhiteSpace(assistantDocument.S3Key),
                    CitationHandle = chunkPosition.HasValue
                        ? assistantDocument.Id + ":" + chunkPosition.Value
                        : assistantDocument.Id + ":verbex:" + (String.IsNullOrWhiteSpace(recordId) ? "record" : recordId),
                    SuggestedNextCalls = BuildDocumentFollowUpCalls(context, assistantDocument, chunkRecordIds, chunkPosition)
                });
            }

            return results;
        }

        private async Task<List<object>> NormalizeVerbexEnumerateRecordsAsync(
            AssistantToolExecutionContext context,
            string indexId,
            JsonElement? recordsElement,
            int maxResults,
            string queryText,
            string recordIdPrefix,
            List<string> recordIdFilters,
            CancellationToken token)
        {
            List<object> records = new List<object>();
            if (!recordsElement.HasValue || recordsElement.Value.ValueKind != JsonValueKind.Array)
                return records;

            foreach (JsonElement item in recordsElement.Value.EnumerateArray())
            {
                if (records.Count >= maxResults) break;

                JsonElement record = GetObjectOrSelf(item, "Document", "Record", "Data");
                JsonElement metadata = GetObjectOrNull(record, "CustomMetadata") ?? GetObjectOrNull(item, "CustomMetadata") ?? default;
                string recordId = GetStringAny(item, "Id", "RecordId", "Identifier", "DocumentId")
                    ?? GetStringAny(record, "Id", "RecordId", "Identifier", "DocumentId");
                string assistantDocumentId = GetStringAny(item, "AssistantHubDocumentId")
                    ?? GetStringAny(record, "AssistantHubDocumentId")
                    ?? (metadata.ValueKind == JsonValueKind.Object ? GetStringAny(metadata, "AssistantHubDocumentId", "DocumentId") : null)
                    ?? recordId;

                if (String.IsNullOrWhiteSpace(recordId) || String.IsNullOrWhiteSpace(assistantDocumentId))
                    continue;
                if (recordIdFilters != null
                    && !recordIdFilters.Contains(recordId, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (!String.IsNullOrWhiteSpace(recordIdPrefix)
                    && !recordId.StartsWith(recordIdPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                AssistantDocument assistantDocument = await ResolveVerbexMappedDocumentAsync(
                    context,
                    assistantDocumentId,
                    recordId,
                    indexId,
                    token).ConfigureAwait(false);
                if (assistantDocument == null)
                    continue;

                string excerpt = null;
                if (context.Policy.AllowDocumentMetadataDetails)
                {
                    excerpt = GetStringAny(item, "Excerpt", "Snippet", "Content", "Text")
                        ?? GetStringAny(record, "Excerpt", "Snippet", "Content", "Text");
                    if (!String.IsNullOrEmpty(excerpt) && excerpt.Length > 500)
                        excerpt = excerpt.Substring(0, 500);
                }

                if (!MatchesIndexRecordQuery(recordId, assistantDocument, excerpt, queryText, context.Policy.AllowDocumentSourceUrls))
                    continue;

                List<string> chunkRecordIds = ParseChunkRecordIds(assistantDocument.ChunkRecordIds);
                int? chunkPosition = ResolveChunkPosition(chunkRecordIds, recordId);

                records.Add(new
                {
                    IndexId = indexId,
                    RecordId = recordId,
                    DocumentId = assistantDocument.Id,
                    DocumentName = assistantDocument.Name ?? assistantDocument.OriginalFilename,
                    assistantDocument.ContentType,
                    SourceUrl = context.Policy.AllowDocumentSourceUrls ? assistantDocument.SourceUrl : null,
                    Excerpt = excerpt,
                    AvailableChunkCount = chunkRecordIds.Count,
                    ChunkPosition = chunkPosition,
                    CanReadChunks = chunkRecordIds.Count > 0,
                    CanReadSourceObject = context.Policy.EnableS3ObjectReadTool && !String.IsNullOrWhiteSpace(assistantDocument.S3Key),
                    CitationHandle = chunkPosition.HasValue
                        ? assistantDocument.Id + ":" + chunkPosition.Value
                        : assistantDocument.Id + ":verbex:" + recordId,
                    SuggestedNextCalls = BuildDocumentFollowUpCalls(context, assistantDocument, chunkRecordIds, chunkPosition)
                });
            }

            return records;
        }

        private static bool MatchesIndexRecordQuery(
            string recordId,
            AssistantDocument document,
            string excerpt,
            string queryText,
            bool includeSourceUrl)
        {
            if (String.IsNullOrWhiteSpace(queryText)) return true;
            if (Contains(recordId, queryText)) return true;
            if (document == null) return false;
            if (Contains(document.Name, queryText)) return true;
            if (Contains(document.OriginalFilename, queryText)) return true;
            if (Contains(document.ContentType, queryText)) return true;
            if (includeSourceUrl && Contains(document.SourceUrl, queryText)) return true;
            return Contains(excerpt, queryText);
        }

        private async Task<AssistantDocument> ResolveVerbexMappedDocumentAsync(
            AssistantToolExecutionContext context,
            string assistantDocumentId,
            string recordId,
            string indexId,
            CancellationToken token)
        {
            if (!String.IsNullOrWhiteSpace(assistantDocumentId))
            {
                AssistantDocument document = await _Database.AssistantDocument.ReadAsync(assistantDocumentId, token).ConfigureAwait(false);
                if (IsAvailableCollectionDocument(document, context))
                    return document;
            }

            if (String.IsNullOrWhiteSpace(recordId))
                return null;

            EnumerationResult<AssistantDocument> documents = await _Database.AssistantDocument.EnumerateAsync(
                context.Assistant.TenantId,
                new EnumerationQuery
                {
                    CollectionIdFilter = context.Settings.CollectionId,
                    MaxResults = 1000
                },
                token).ConfigureAwait(false);

            foreach (AssistantDocument document in documents.Objects ?? new List<AssistantDocument>())
            {
                if (!IsAvailableCollectionDocument(document, context)) continue;
                bool recordMatchesDocument = String.Equals(document.Id, recordId, StringComparison.Ordinal)
                    || String.Equals(document.VerbexRecordId, recordId, StringComparison.Ordinal)
                    || ParseChunkRecordIds(document.ChunkRecordIds).Contains(recordId, StringComparer.Ordinal);
                if (!recordMatchesDocument) continue;
                if (!String.IsNullOrWhiteSpace(document.VerbexIndexId)
                    && !String.IsNullOrWhiteSpace(indexId)
                    && !String.Equals(document.VerbexIndexId, indexId, StringComparison.OrdinalIgnoreCase))
                    continue;
                return document;
            }

            return null;
        }

        private ExternalSearchProviderSettings ResolveTavilyProvider(AssistantToolPolicy policy)
        {
            if (policy != null)
            {
                ExternalSearchProviderSettings assistantProvider = ExternalSearchConfigurationHelper.ResolveAssistantTavilyProvider(
                    policy.TavilyEndpoint,
                    policy.TavilyApiKey,
                    policy.ToolCallTimeoutMs);
                if (assistantProvider != null)
                    return assistantProvider;
            }

            return ExternalSearchConfigurationHelper.ResolveDefaultTavilyProvider(_Settings.ExternalSearch);
        }

        private static string ResolveTavilyIncludeAnswerMode(JsonElement arguments)
        {
            if (arguments.ValueKind != JsonValueKind.Object) return "basic";
            if (!TryGetPropertyIgnoreCase(arguments, "include_answer", out JsonElement value)) return "basic";

            if (value.ValueKind == JsonValueKind.False) return null;
            if (value.ValueKind == JsonValueKind.True) return "basic";
            if (value.ValueKind == JsonValueKind.String)
            {
                string mode = value.GetString()?.Trim();
                if (String.IsNullOrWhiteSpace(mode)) return null;
                if (String.Equals(mode, "false", StringComparison.OrdinalIgnoreCase)) return null;
                if (String.Equals(mode, "true", StringComparison.OrdinalIgnoreCase)) return "basic";
                if (String.Equals(mode, "basic", StringComparison.OrdinalIgnoreCase)) return "basic";
                if (String.Equals(mode, "advanced", StringComparison.OrdinalIgnoreCase)) return "advanced";
            }

            throw new ArgumentException("include_answer must be true, false, basic, or advanced.");
        }

        private static JsonDocument ParseArguments(string argumentsJson)
        {
            if (String.IsNullOrWhiteSpace(argumentsJson))
                return JsonDocument.Parse("{}");

            return JsonDocument.Parse(argumentsJson);
        }

        private static string BuildRedactedVerbexSearchBodyLog(string json)
        {
            if (String.IsNullOrWhiteSpace(json)) return "{}";

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return "{\"body\":\"[redacted]\"}";

                Dictionary<string, object> summary = new Dictionary<string, object>();

                if (TryGetPropertyIgnoreCase(root, "Query", out JsonElement query) && query.ValueKind == JsonValueKind.String)
                {
                    summary["HasQuery"] = !String.IsNullOrEmpty(query.GetString());
                    summary["QueryLength"] = query.GetString()?.Length ?? 0;
                }

                if (TryGetPropertyIgnoreCase(root, "MaxResults", out JsonElement maxResults) && maxResults.ValueKind == JsonValueKind.Number)
                    summary["MaxResults"] = maxResults.GetRawText();
                if (TryGetPropertyIgnoreCase(root, "UseAndLogic", out JsonElement useAndLogic))
                    summary["UseAndLogic"] = useAndLogic.ValueKind == JsonValueKind.True;
                if (TryGetPropertyIgnoreCase(root, "IncludeMatchedTerms", out JsonElement includeMatchedTerms))
                    summary["IncludeMatchedTerms"] = includeMatchedTerms.ValueKind == JsonValueKind.True;
                if (TryGetPropertyIgnoreCase(root, "IncludeTermDetails", out JsonElement includeTermDetails))
                    summary["IncludeTermDetails"] = includeTermDetails.ValueKind == JsonValueKind.True;
                if (TryGetPropertyIgnoreCase(root, "IncludeDocumentTermStats", out JsonElement includeDocumentTermStats))
                    summary["IncludeDocumentTermStats"] = includeDocumentTermStats.ValueKind == JsonValueKind.True;
                if (TryGetPropertyIgnoreCase(root, "RequiredTerms", out JsonElement requiredTerms) && requiredTerms.ValueKind == JsonValueKind.Array)
                    summary["RequiredTermCount"] = requiredTerms.GetArrayLength();
                if (TryGetPropertyIgnoreCase(root, "ExcludedTerms", out JsonElement excludedTerms) && excludedTerms.ValueKind == JsonValueKind.Array)
                    summary["ExcludedTermCount"] = excludedTerms.GetArrayLength();

                return JsonSerializer.Serialize(summary, _JsonOptions);
            }
            catch (JsonException)
            {
                return "{\"body\":\"[redacted-unparseable]\"}";
            }
        }

        private static AssistantToolExecutionResult FinishError(AssistantToolExecutionResult result, Stopwatch sw, string message, bool denied)
        {
            result.Success = false;
            result.Denied = denied;
            result.ErrorMessage = message;
            result.ErrorCode = BuildToolErrorCode(message, denied);
            return Finish(result, sw);
        }

        private static string BuildToolErrorCode(string message, bool denied)
        {
            string value = message ?? String.Empty;
            if (value.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                return "timeout";
            if (value.IndexOf("canceled", StringComparison.OrdinalIgnoreCase) >= 0)
                return "canceled";
            if (value.IndexOf("not configured", StringComparison.OrdinalIgnoreCase) >= 0)
                return "provider_missing";
            if (value.IndexOf("failed with status code", StringComparison.OrdinalIgnoreCase) >= 0)
                return "provider_http_error";
            if (value.IndexOf("invalid JSON", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("not valid JSON", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("requires", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("Unknown argument", StringComparison.OrdinalIgnoreCase) >= 0)
                return "invalid_arguments";
            if (value.IndexOf("Unknown tool", StringComparison.OrdinalIgnoreCase) >= 0)
                return "unknown_tool";
            if (value.IndexOf("not available", StringComparison.OrdinalIgnoreCase) >= 0)
                return denied ? "tool_unavailable" : "tool_error";
            if (value.IndexOf("not allowed", StringComparison.OrdinalIgnoreCase) >= 0 || denied)
                return "policy_denial";

            return "tool_error";
        }

        private static AssistantToolExecutionResult Finish(AssistantToolExecutionResult result, Stopwatch sw)
        {
            sw.Stop();
            result.DurationMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2);
            return result;
        }

        private static string GetString(JsonElement element, string name)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;
            if (!TryGetPropertyIgnoreCase(element, name, out JsonElement value)) return null;
            return value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : null;
        }

        private static int GetInt(JsonElement element, string name, int defaultValue)
        {
            if (element.ValueKind != JsonValueKind.Object) return defaultValue;
            if (!TryGetPropertyIgnoreCase(element, name, out JsonElement value)) return defaultValue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int numeric)) return numeric;
            if (value.ValueKind == JsonValueKind.String && Int32.TryParse(value.GetString(), out numeric)) return numeric;
            return defaultValue;
        }

        private static bool GetBool(JsonElement element, string name, bool defaultValue)
        {
            if (element.ValueKind != JsonValueKind.Object) return defaultValue;
            if (!TryGetPropertyIgnoreCase(element, name, out JsonElement value)) return defaultValue;
            if (value.ValueKind == JsonValueKind.True) return true;
            if (value.ValueKind == JsonValueKind.False) return false;
            if (value.ValueKind == JsonValueKind.String && Boolean.TryParse(value.GetString(), out bool boolean)) return boolean;
            return defaultValue;
        }

        private static List<string> GetStringList(JsonElement element, string name)
        {
            List<string> values = new List<string>();
            if (element.ValueKind != JsonValueKind.Object) return values;
            if (!TryGetPropertyIgnoreCase(element, name, out JsonElement value)) return values;

            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in value.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.String && !String.IsNullOrWhiteSpace(item.GetString()))
                        values.Add(item.GetString().Trim());
            }
            else if (value.ValueKind == JsonValueKind.String && !String.IsNullOrWhiteSpace(value.GetString()))
            {
                values.Add(value.GetString().Trim());
            }

            return values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<int> GetIntList(JsonElement element, string name)
        {
            List<int> values = new List<int>();
            if (element.ValueKind != JsonValueKind.Object) return values;
            if (!TryGetPropertyIgnoreCase(element, name, out JsonElement value)) return values;

            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out int numeric))
                        values.Add(numeric);
                    else if (item.ValueKind == JsonValueKind.String && Int32.TryParse(item.GetString(), out numeric))
                        values.Add(numeric);
                }
            }
            else if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int singleNumber))
            {
                values.Add(singleNumber);
            }
            else if (value.ValueKind == JsonValueKind.String && Int32.TryParse(value.GetString(), out singleNumber))
            {
                values.Add(singleNumber);
            }

            return values.Distinct().ToList();
        }

        private static int MinLimit(params int[] values)
        {
            int min = Int32.MaxValue;
            foreach (int value in values ?? Array.Empty<int>())
            {
                if (value > 0 && value < min)
                    min = value;
            }

            return min == Int32.MaxValue ? 1 : min;
        }

        private static void ValidatePublicWebSearchBoundary(string query, List<string> includeDomains)
        {
            foreach (string host in ExtractPotentialWebHosts(query)
                .Concat(includeDomains ?? new List<string>()))
            {
                if (IsPrivateWebHost(host))
                    throw new InvalidOperationException("web_search cannot target localhost, private IP ranges, or internal-only domains.");
            }
        }

        private static List<string> ExtractPotentialWebHosts(string query)
        {
            List<string> hosts = new List<string>();
            if (String.IsNullOrWhiteSpace(query)) return hosts;

            foreach (Match match in Regex.Matches(query, @"(?i)\bhttps?://[^\s<>()]+"))
            {
                if (Uri.TryCreate(match.Value, UriKind.Absolute, out Uri uri)
                    && !String.IsNullOrWhiteSpace(uri.Host))
                    hosts.Add(uri.Host);
            }

            foreach (string token in query.Split(new[] { ' ', '\t', '\r', '\n', ',', ';', '(', ')', '[', ']', '{', '}', '<', '>' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = token.Trim().Trim('"', '\'', '.', '?', '!', '/', '\\');
                if (candidate.Contains("://", StringComparison.Ordinal)) continue;
                if (candidate.Contains(".", StringComparison.Ordinal) || candidate.Contains(":", StringComparison.Ordinal) || String.Equals(candidate, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    int slashIndex = candidate.IndexOf('/');
                    if (slashIndex >= 0) candidate = candidate.Substring(0, slashIndex);
                    int portIndex = candidate.LastIndexOf(':');
                    if (portIndex > 0 && candidate.Count(c => c == ':') == 1)
                        candidate = candidate.Substring(0, portIndex);
                    hosts.Add(candidate);
                }
            }

            return hosts
                .Where(host => !String.IsNullOrWhiteSpace(host))
                .Select(host => host.Trim().Trim('[', ']'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsPrivateWebHost(string host)
        {
            if (String.IsNullOrWhiteSpace(host)) return false;
            string normalized = host.Trim().Trim('[', ']').ToLowerInvariant();
            if (normalized == "localhost" || normalized.EndsWith(".localhost", StringComparison.Ordinal)) return true;
            if (normalized.EndsWith(".local", StringComparison.Ordinal) || normalized.EndsWith(".internal", StringComparison.Ordinal)) return true;

            if (!IPAddress.TryParse(normalized, out IPAddress address)) return false;
            byte[] bytes = address.GetAddressBytes();
            if (IPAddress.IsLoopback(address)) return true;

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return bytes[0] == 10
                    || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    || (bytes[0] == 192 && bytes[1] == 168)
                    || (bytes[0] == 169 && bytes[1] == 254)
                    || bytes[0] == 0;
            }

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                return address.IsIPv6LinkLocal
                    || address.IsIPv6SiteLocal
                    || normalized == "::1"
                    || normalized.StartsWith("fc", StringComparison.Ordinal)
                    || normalized.StartsWith("fd", StringComparison.Ordinal);
            }

            return false;
        }

        private static void ValidateS3BucketPolicy(AssistantToolPolicy policy, string defaultBucket, string bucket, string objectKey)
        {
            ValidateS3BucketNamePolicy(policy, defaultBucket, bucket);

            if (policy.AllowedBucketPrefixes != null
                && policy.AllowedBucketPrefixes.Count > 0
                && !policy.AllowedBucketPrefixes.Any(prefix => objectKey.StartsWith(prefix, StringComparison.Ordinal)))
                throw new InvalidOperationException("Requested S3 object key prefix is not allowed by assistant policy.");
        }

        private static void ValidateS3ObjectShapePolicy(AssistantToolPolicy policy, string objectKey, string contentType)
        {
            if (!IsAllowedObjectSuffix(policy, objectKey))
                throw new InvalidOperationException("Requested S3 object key suffix is not allowed by assistant policy.");

            if (!IsAllowedContentType(policy, contentType))
                throw new InvalidOperationException("Requested S3 object content type is not allowed by assistant policy.");
        }

        private static void ValidateS3ObjectSecretPathPolicy(string objectKey)
        {
            if (String.IsNullOrWhiteSpace(objectKey)) return;

            string normalized = objectKey.Replace('\\', '/').Trim().ToLowerInvariant();
            string[] segments = normalized
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string segment in segments)
            {
                if (IsBlockedSecretPathSegment(segment))
                    throw new InvalidOperationException("Requested S3 object path is blocked by the default secret/config path policy.");
            }

            string fileName = segments.Length > 0 ? segments[segments.Length - 1] : normalized;
            if (IsBlockedSecretFileName(fileName) || IsBlockedSecretFileSuffix(fileName))
                throw new InvalidOperationException("Requested S3 object path is blocked by the default secret/config path policy.");
        }

        private static bool IsBlockedSecretPathSegment(string segment)
        {
            if (String.IsNullOrWhiteSpace(segment)) return false;
            return segment == ".ssh"
                || segment == ".aws"
                || segment == ".azure"
                || segment == ".gcp"
                || segment == ".kube"
                || segment == ".docker"
                || segment == ".gnupg"
                || segment == "secrets"
                || segment == "credentials";
        }

        private static bool IsBlockedSecretFileName(string fileName)
        {
            if (String.IsNullOrWhiteSpace(fileName)) return false;
            if (fileName == ".env" || fileName.StartsWith(".env.", StringComparison.Ordinal)) return true;

            return fileName == ".npmrc"
                || fileName == ".pypirc"
                || fileName == "nuget.config"
                || fileName == "web.config"
                || fileName == "app.config"
                || fileName == "appsettings.json"
                || fileName.StartsWith("appsettings.", StringComparison.Ordinal)
                || fileName == "connectionstrings.json"
                || fileName == "credentials.json"
                || fileName == "service-account.json"
                || fileName == "id_rsa"
                || fileName == "id_dsa"
                || fileName == "id_ecdsa"
                || fileName == "id_ed25519";
        }

        private static bool IsBlockedSecretFileSuffix(string fileName)
        {
            if (String.IsNullOrWhiteSpace(fileName)) return false;
            return fileName.EndsWith(".pem", StringComparison.Ordinal)
                || fileName.EndsWith(".pfx", StringComparison.Ordinal)
                || fileName.EndsWith(".p12", StringComparison.Ordinal)
                || fileName.EndsWith(".key", StringComparison.Ordinal);
        }

        private static bool IsAllowedObjectSuffix(AssistantToolPolicy policy, string objectKey)
        {
            if (policy?.AllowedObjectSuffixes == null || policy.AllowedObjectSuffixes.Count == 0) return true;
            if (String.IsNullOrWhiteSpace(objectKey)) return false;
            return policy.AllowedObjectSuffixes.Any(suffix => objectKey.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsAllowedContentType(AssistantToolPolicy policy, string contentType)
        {
            if (policy?.AllowedContentTypes == null || policy.AllowedContentTypes.Count == 0) return true;
            if (String.IsNullOrWhiteSpace(contentType)) return false;
            return policy.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
        }

        private static void ValidateS3BucketNamePolicy(AssistantToolPolicy policy, string defaultBucket, string bucket)
        {
            bool isDefaultBucket = !String.IsNullOrWhiteSpace(defaultBucket)
                && String.Equals(bucket, defaultBucket, StringComparison.OrdinalIgnoreCase);

            if (!isDefaultBucket && (policy.AllowedBucketNames == null
                || !policy.AllowedBucketNames.Contains(bucket, StringComparer.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Requested S3 bucket is not allowed by assistant policy.");
        }

        private async Task<Dictionary<string, AssistantDocument>> BuildObjectDocumentMapAsync(
            AssistantToolExecutionContext context,
            string bucket,
            CancellationToken token)
        {
            Dictionary<string, AssistantDocument> ret = new Dictionary<string, AssistantDocument>(StringComparer.Ordinal);
            EnumerationResult<AssistantDocument> documents = await _Database.AssistantDocument.EnumerateAsync(
                context.Assistant.TenantId,
                new EnumerationQuery
                {
                    CollectionIdFilter = context.Settings.CollectionId,
                    MaxResults = 1000
                },
                token).ConfigureAwait(false);

            string defaultBucket = _Settings.S3?.BucketName;
            foreach (AssistantDocument document in documents.Objects ?? new List<AssistantDocument>())
            {
                if (document == null || String.IsNullOrWhiteSpace(document.S3Key)) continue;
                string documentBucket = String.IsNullOrWhiteSpace(document.BucketName) ? defaultBucket : document.BucketName.Trim();
                if (!String.Equals(documentBucket, bucket, StringComparison.OrdinalIgnoreCase)) continue;
                if (!IsAvailableCollectionDocument(document, context)) continue;
                ret[MakeObjectMapKey(documentBucket, document.S3Key)] = document;
            }

            return ret;
        }

        private static string MakeObjectMapKey(string bucket, string key)
        {
            return (bucket ?? "") + "/" + (key ?? "");
        }

        private static byte[] SliceBytes(byte[] data, int start, int length)
        {
            if (data == null || length <= 0) return Array.Empty<byte>();

            byte[] segment = new byte[length];
            Array.Copy(data, start, segment, 0, length);
            return segment;
        }

        private static string NormalizeContentMode(string requestedMode, string contentType, byte[] data)
        {
            if (String.IsNullOrWhiteSpace(requestedMode))
                return IsTextLikeContentType(contentType) || !LooksBinary(data) ? "text" : "metadata_only";

            string mode = requestedMode.Trim().ToLowerInvariant();
            if (mode == "text" || mode == "base64" || mode == "metadata_only")
                return mode;

            throw new ArgumentException("content_mode must be text, base64, or metadata_only.");
        }

        private static bool IsTextLikeContentType(string contentType)
        {
            if (String.IsNullOrWhiteSpace(contentType)) return false;

            string normalized = contentType.Trim().ToLowerInvariant();
            return normalized.StartsWith("text/")
                || normalized.Contains("json")
                || normalized.Contains("xml")
                || normalized.Contains("yaml")
                || normalized.Contains("csv")
                || normalized.Contains("markdown")
                || normalized.Contains("javascript")
                || normalized.Contains("html")
                || normalized.Contains("x-www-form-urlencoded");
        }

        private static bool LooksBinary(byte[] data)
        {
            if (data == null || data.Length == 0) return false;

            int sampleLength = Math.Min(data.Length, 4096);
            int controlCharacters = 0;
            for (int i = 0; i < sampleLength; i++)
            {
                byte value = data[i];
                if (value == 0) return true;
                if (value < 8 || (value > 13 && value < 32))
                    controlCharacters++;
            }

            return controlCharacters > Math.Max(1, sampleLength / 20);
        }

        private static string DecodeUtf8(byte[] data)
        {
            return Encoding.UTF8.GetString(data ?? Array.Empty<byte>());
        }

        private static string SliceDecodedText(string text, JsonElement arguments, int maxCharacters, out bool truncated)
        {
            text ??= "";
            int textStart = GetInt(arguments, "text_start", 0);
            if (textStart < 0 || textStart > text.Length)
                throw new ArgumentOutOfRangeException(nameof(textStart), "text_start is outside the decoded text.");

            int defaultTextLength = Math.Min(text.Length - textStart, maxCharacters);
            int requestedTextLength = GetInt(arguments, "text_length", defaultTextLength);
            if (requestedTextLength < 0)
                throw new ArgumentOutOfRangeException(nameof(requestedTextLength), "text_length cannot be negative.");

            int cappedTextLength = Math.Min(requestedTextLength, defaultTextLength);
            truncated = textStart + cappedTextLength < text.Length || requestedTextLength > cappedTextLength;
            return text.Substring(textStart, cappedTextLength);
        }

        private static string NormalizeExtractedText(string text)
        {
            return (text ?? String.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\r", "\n", StringComparison.Ordinal)
                .Trim();
        }

        private static string NormalizeDocumentType(string documentType)
        {
            if (String.IsNullOrWhiteSpace(documentType)) return null;
            string normalized = documentType.Trim().Trim('.').ToLowerInvariant();
            if (String.Equals(normalized, "txt", StringComparison.Ordinal)) return "text";
            if (String.Equals(normalized, "md", StringComparison.Ordinal)) return "markdown";
            return normalized;
        }

        private static string ResolveDocumentType(string name, string contentType)
        {
            string extension = NormalizeDocumentType(System.IO.Path.GetExtension(name ?? String.Empty));
            if (!String.IsNullOrWhiteSpace(extension)) return extension;

            string type = (contentType ?? String.Empty).ToLowerInvariant();
            if (type.Contains("pdf")) return "pdf";
            if (type.Contains("word")) return "docx";
            if (type.Contains("spreadsheet") || type.Contains("excel")) return "xlsx";
            if (type.Contains("presentation") || type.Contains("powerpoint")) return "pptx";
            if (type.Contains("html")) return "html";
            if (type.Contains("json")) return "json";
            if (type.Contains("xml")) return "xml";
            if (type.Contains("markdown")) return "markdown";
            if (type.Contains("csv")) return "csv";
            if (type.StartsWith("text/", StringComparison.Ordinal)) return "text";
            return null;
        }

        private static bool IsTextLike(string name, string contentType)
        {
            if (IsTextLikeContentType(contentType)) return true;

            string extension = NormalizeDocumentType(System.IO.Path.GetExtension(name ?? String.Empty));
            return extension == "text" || extension == "markdown" || extension == "json"
                || extension == "csv" || extension == "tsv" || extension == "xml" || extension == "html"
                || extension == "htm" || extension == "log";
        }

        private static bool TryDecodeUtf8(byte[] bytes, out string text)
        {
            text = null;
            if (bytes == null || bytes.Length < 1) return false;

            try
            {
                text = new UTF8Encoding(false, true).GetString(bytes);
                return true;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }

        private static string RedactObjectKey(string objectKey, AssistantToolPolicy policy)
        {
            if (String.IsNullOrWhiteSpace(objectKey)) return null;

            string normalized = objectKey.Replace('\\', '/').Trim();
            if (policy != null && !policy.RedactObjectKeys) return normalized;

            int index = normalized.LastIndexOf('/');
            if (index < 0) return normalized;
            if (index == normalized.Length - 1) return ".../";
            return ".../" + normalized.Substring(index + 1);
        }

        private static List<(int StartPosition, int Count)> GetChunkRanges(JsonElement element, string name)
        {
            List<(int StartPosition, int Count)> ranges = new List<(int StartPosition, int Count)>();
            if (element.ValueKind != JsonValueKind.Object) return ranges;
            if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Array) return ranges;

            foreach (JsonElement item in value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;

                int? startPosition = GetIntAny(item, "start_position", "startPosition", "start");
                int? count = GetIntAny(item, "count", "length");
                if (startPosition.HasValue && count.HasValue && count.Value > 0)
                    ranges.Add((startPosition.Value, count.Value));
            }

            return ranges;
        }

        private static JsonElement? GetFirstArray(JsonElement element, params string[] names)
        {
            JsonElement current = element;
            if (current.ValueKind == JsonValueKind.Array)
                return current;

            if (TryGetPropertyIgnoreCase(current, "Data", out JsonElement data) && data.ValueKind == JsonValueKind.Object)
                current = data;

            foreach (string name in names)
            {
                if (TryGetPropertyIgnoreCase(current, name, out JsonElement value) && value.ValueKind == JsonValueKind.Array)
                    return value;
            }

            return null;
        }

        private static bool? GetBoolAny(JsonElement element, params string[] names)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;

            foreach (string name in names)
            {
                if (TryGetPropertyIgnoreCase(element, name, out JsonElement value))
                {
                    if (value.ValueKind == JsonValueKind.True) return true;
                    if (value.ValueKind == JsonValueKind.False) return false;
                    if (value.ValueKind == JsonValueKind.String && Boolean.TryParse(value.GetString(), out bool boolean)) return boolean;
                }
            }

            return null;
        }

        private static JsonElement GetObjectOrSelf(JsonElement element, params string[] names)
        {
            foreach (string name in names)
            {
                if (TryGetPropertyIgnoreCase(element, name, out JsonElement value) && value.ValueKind == JsonValueKind.Object)
                    return value;
            }

            return element;
        }

        private static JsonElement? GetObjectOrNull(JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object
                && TryGetPropertyIgnoreCase(element, name, out JsonElement value)
                && value.ValueKind == JsonValueKind.Object)
                return value;

            return null;
        }

        private static string GetStringAny(JsonElement element, params string[] names)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;

            foreach (string name in names)
            {
                if (TryGetPropertyIgnoreCase(element, name, out JsonElement value))
                {
                    if (value.ValueKind == JsonValueKind.String) return value.GetString()?.Trim();
                    if (value.ValueKind == JsonValueKind.Number) return value.GetRawText();
                }
            }

            return null;
        }

        private static double? GetDoubleAny(JsonElement element, params string[] names)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;

            foreach (string name in names)
            {
                if (TryGetPropertyIgnoreCase(element, name, out JsonElement value))
                {
                    if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double numeric)) return numeric;
                    if (value.ValueKind == JsonValueKind.String && Double.TryParse(value.GetString(), out numeric)) return numeric;
                }
            }

            return null;
        }

        private static int? GetIntAny(JsonElement element, params string[] names)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;

            foreach (string name in names)
            {
                if (TryGetPropertyIgnoreCase(element, name, out JsonElement value))
                {
                    if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int numeric)) return numeric;
                    if (value.ValueKind == JsonValueKind.String && Int32.TryParse(value.GetString(), out numeric)) return numeric;
                }
            }

            return null;
        }

        private static List<string> GetStringArray(JsonElement element, params string[] names)
        {
            List<string> values = new List<string>();
            if (element.ValueKind != JsonValueKind.Object) return values;

            foreach (string name in names)
            {
                if (!TryGetPropertyIgnoreCase(element, name, out JsonElement value)) continue;

                if (value.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in value.EnumerateArray())
                        if (item.ValueKind == JsonValueKind.String && !String.IsNullOrWhiteSpace(item.GetString()))
                            values.Add(item.GetString().Trim());
                    break;
                }

                if (value.ValueKind == JsonValueKind.String && !String.IsNullOrWhiteSpace(value.GetString()))
                {
                    values.Add(value.GetString().Trim());
                    break;
                }
            }

            return values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
        {
            value = default;
            if (element.ValueKind != JsonValueKind.Object) return false;

            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (String.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(string value, string query)
        {
            return !String.IsNullOrWhiteSpace(value)
                && !String.IsNullOrWhiteSpace(query)
                && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private class CollectionDocumentScope
        {
            public List<string> DocumentIds { get; set; }
            public int? DocumentsConsidered { get; set; }
            public bool DocumentLimitApplied { get; set; }
        }
    }
}
