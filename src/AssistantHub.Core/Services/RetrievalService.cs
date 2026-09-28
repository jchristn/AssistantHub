namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using AssistantHub.Core.Telemetry;
    using SyslogLogging;

    /// <summary>
    /// Retrieval service for querying embedded document chunks.
    /// </summary>
    public class RetrievalService
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[RetrievalService] ";
        private ChunkingSettings _ChunkingSettings = null;
        private IChunkingService _ChunkingService = null;
        private RecallDbSettings _RecallDbSettings = null;
        private IVectorStoreService _VectorStore = null;
        private LoggingModule _Logging = null;
        private HttpClient _HttpClient = null;
        private QueryEmbeddingCache _EmbeddingCache = null;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Prefix, DateTime ExpiresUtc)> _QueryPrefixes =
            new System.Collections.Concurrent.ConcurrentDictionary<string, (string Prefix, DateTime ExpiresUtc)>(StringComparer.Ordinal);

        private JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="chunkingSettings">Chunking service settings.</param>
        /// <param name="recallDbSettings">RecallDb service settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="vectorStore">Optional vector-store service implementation.</param>
        /// <param name="chunkingService">Optional chunking service implementation.</param>
        public RetrievalService(ChunkingSettings chunkingSettings, RecallDbSettings recallDbSettings, LoggingModule logging, IVectorStoreService vectorStore = null, IChunkingService chunkingService = null)
        {
            _ChunkingSettings = chunkingSettings ?? throw new ArgumentNullException(nameof(chunkingSettings));
            _ChunkingService = chunkingService ?? new PartioChunkingService(_ChunkingSettings, logging);
            _RecallDbSettings = recallDbSettings ?? throw new ArgumentNullException(nameof(recallDbSettings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _VectorStore = vectorStore ?? new RecallDbVectorStoreService(_RecallDbSettings, _Logging);
            _HttpClient = new HttpClient();
            _EmbeddingCache = new QueryEmbeddingCache(_ChunkingSettings.QueryEmbeddingCacheSize);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Retrieve relevant document chunks for a given query.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="query">Search query text.</param>
        /// <param name="topK">Number of top results to retrieve.</param>
        /// <param name="scoreThreshold">Minimum score threshold for results (0.0 to 1.0).</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="embeddingEndpointId">Optional embedding endpoint override.</param>
        /// <param name="searchOptions">Search mode and full-text options.</param>
        /// <returns>List of retrieval chunks with source identification and scoring.</returns>
        public async Task<List<RetrievalChunk>> RetrieveAsync(
            string tenantId,
            string collectionId,
            string query,
            int topK,
            double scoreThreshold,
            CancellationToken token = default,
            string embeddingEndpointId = null,
            RetrievalSearchOptions searchOptions = null)
        {
            if (String.IsNullOrEmpty(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

            if (searchOptions == null) searchOptions = new RetrievalSearchOptions();
            searchOptions.HybridFallbackRan = false;
            searchOptions.EmbeddingFailed = false;
            searchOptions.KeywordFallbackRan = false;

            List<RetrievalChunk> results = new List<RetrievalChunk>();

            using (OperationScope op = AssistantHubTelemetry.StartOperation("retrieval", "search"))
            {
                string mode = ResolveRetrievalMode(searchOptions.SearchMode);
                op.SetTag("tenant.id", tenantId);
                op.SetTag("collection.id", collectionId);
                op.SetTag("retrieval.mode", mode);
                op.SetTag("retrieval.top_k", topK);

                try
                {
                    // Step 1: Embed the query (skip for FullText-only mode)
                List<double> queryEmbeddings = null;
                RetrievalSearchOptions effectiveOptions = searchOptions;

                if (!searchOptions.SearchMode.Equals("FullText", StringComparison.OrdinalIgnoreCase))
                {
                    string embeddingText = query;
                    if (searchOptions.EmbeddingTaskPrefixes)
                    {
                        string queryPrefix = await ResolveQueryPrefixAsync(embeddingEndpointId, token).ConfigureAwait(false);
                        if (!String.IsNullOrEmpty(queryPrefix)) embeddingText = queryPrefix + query;
                    }

                    queryEmbeddings = await EmbedQueryAsync(embeddingText, token, embeddingEndpointId).ConfigureAwait(false);
                    if (queryEmbeddings == null || queryEmbeddings.Count == 0)
                    {
                        _Logging.Warn(_Header + "failed to generate embeddings for query");
                        searchOptions.EmbeddingFailed = true;
                        op.SetTag("retrieval.embedding_failed", true);
                        if (mode != "hybrid") return results;

                        // Hybrid still has a full-text leg that needs no embedding; run it alone rather than
                        // returning no context for the turn.
                        _Logging.Info(_Header + "hybrid search could not embed the query, falling back to full-text only");
                        searchOptions.KeywordFallbackRan = true;
                        op.SetTag("retrieval.keyword_fallback", true);
                        effectiveOptions = CloneSearchOptions(searchOptions, "FullText", searchOptions.DocumentIds);
                        mode = "keyword";
                        queryEmbeddings = null;
                    }
                    else
                    {
                        _Logging.Debug(_Header + "generated " + queryEmbeddings.Count + "-dimensional embedding for query");
                    }
                }
                else
                {
                    _Logging.Debug(_Header + "FullText mode: skipping embedding step");
                }

                List<SearchResult> searchResults = await ExecuteSearchWithDocumentFilterAsync(
                    tenantId,
                    collectionId,
                    query,
                    queryEmbeddings,
                    topK,
                    effectiveOptions,
                    token).ConfigureAwait(false);

                if (searchResults == null || searchResults.Count == 0)
                {
                    _Logging.Debug(_Header + "no search results returned from RecallDB");
                    return results;
                }

                _Logging.Debug(_Header + "received " + searchResults.Count + " results from RecallDB");

                // Step 4: Filter by score threshold and collect results with source info
                foreach (SearchResult result in searchResults)
                {
                    if (PassesScoreThreshold(result.Score, result.VectorScore, result.TextRank, mode, searchOptions.HybridFallbackRan, scoreThreshold, searchOptions.ApplyThresholdToFullText))
                    {
                        if (!String.IsNullOrEmpty(result.Content))
                        {
                            RetrievalChunk retrievalChunk = new RetrievalChunk
                            {
                                DocumentId = result.DocumentId,
                                Score = Math.Round(result.Score, 6),
                                TextScore = result.TextScore.HasValue ? Math.Round(result.TextScore.Value, 6) : null,
                                VectorScore = result.VectorScore.HasValue
                                    ? Math.Round(result.VectorScore.Value, 6)
                                    : (mode == "vector" || searchOptions.HybridFallbackRan ? Math.Round(result.Score, 6) : null),
                                VectorRank = result.VectorRank,
                                TextRank = result.TextRank,
                                Content = result.Content,
                                Position = result.Position,
                                Neighbors = result.Neighbors?.Select(n => new RetrievalChunk
                                {
                                    DocumentId = n.DocumentId,
                                    Content = n.Content,
                                    Position = n.Position
                                }).ToList()
                            };
                            ProvenanceTags.Apply(retrievalChunk, result.Tags);
                            results.Add(retrievalChunk);
                        }
                    }
                }

                _Logging.Info(_Header + "returning " + results.Count + " results above score threshold " + scoreThreshold);
                }
                catch (Exception e)
                {
                    op.Fail(e);
                    _Logging.Warn(_Header + "exception during retrieval: " + e.Message);
                }

                op.SetTag("retrieval.result_count", results.Count);
                AssistantHubTelemetry.RecordRetrievalResults(mode, results.Count);
                return results;
            }
        }

        private static string ResolveRetrievalMode(string searchMode)
        {
            if (String.IsNullOrEmpty(searchMode)) return "vector";
            if (searchMode.Equals("FullText", StringComparison.OrdinalIgnoreCase)) return "keyword";
            if (searchMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase)) return "hybrid";
            return "vector";
        }

        /// <summary>
        /// Apply the retrieval score threshold on a scale that means the same thing in every mode: vector similarity.
        /// Full-text scores (ts_rank) sit far below any similarity threshold, so keyword search is not thresholded here;
        /// FullText.MinimumScore is its cutoff. In hybrid mode the fused score caps a chunk found only by the text leg
        /// at TextWeight, so the threshold is applied to vector similarity instead, and only to chunks the vector leg
        /// found on its own: a chunk the text leg also found is kept whatever its similarity, since strong keyword
        /// matches often have low embedding similarity.
        /// </summary>
        /// <param name="score">Score reported by the store (similarity, ts_rank or fused, depending on mode).</param>
        /// <param name="vectorScore">Vector-leg similarity reported for a hybrid result, when present.</param>
        /// <param name="textRank">Text-leg rank reported for a hybrid result, when present.</param>
        /// <param name="mode">Retrieval mode that produced the result (vector, keyword or hybrid).</param>
        /// <param name="hybridFallbackRan">Whether a hybrid search fell back to vector-only.</param>
        /// <param name="scoreThreshold">Minimum similarity.</param>
        /// <param name="applyToFullText">Also threshold keyword-only results on their full-text score.</param>
        /// <returns>True when the result is kept.</returns>
        public static bool PassesScoreThreshold(double score, double? vectorScore, int? textRank, string mode, bool hybridFallbackRan, double scoreThreshold, bool applyToFullText = false)
        {
            if (mode == "keyword") return !applyToFullText || score >= scoreThreshold;

            if (mode == "hybrid" && !hybridFallbackRan)
            {
                if (textRank.HasValue) return true;
                if (vectorScore.HasValue) return vectorScore.Value >= scoreThreshold;
            }

            return score >= scoreThreshold;
        }

        /// <summary>
        /// Read a single stored RecallDB collection record by its server-known record identifier.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="recordId">RecallDB record identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Retrieval chunk when found, otherwise null.</returns>
        public async Task<RetrievalChunk> ReadCollectionRecordAsync(
            string tenantId,
            string collectionId,
            string recordId,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            if (String.IsNullOrEmpty(recordId)) throw new ArgumentNullException(nameof(recordId));

            string path = "/v1.0/tenants/" + tenantId + "/collections/" + collectionId + "/documents/" + Uri.EscapeDataString(recordId);

            try
            {
                using (HttpResponseMessage response = await _VectorStore.SendAsync(HttpMethod.Get, path, null, token).ConfigureAwait(false))
                {
                    string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(_Header + "RecallDB record read returned " + (int)response.StatusCode + " for record " + recordId + ": " + responseBody);
                        return null;
                    }

                    using JsonDocument document = JsonDocument.Parse(responseBody);
                    JsonElement record = GetObjectOrSelf(document.RootElement, "Document", "Record", "Data");
                    return new RetrievalChunk
                    {
                        DocumentId = GetStringAny(record, "DocumentId", "AssistantHubDocumentId"),
                        Content = GetStringAny(record, "Content", "Text"),
                        Position = GetIntAny(record, "Position", "ChunkIndex")
                    };
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception reading RecallDB record " + recordId + ": " + e.Message);
                return null;
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Build the RecallDB search request body based on search mode.
        /// </summary>
        private object BuildSearchBody(string query, List<double> embeddings, int topK, RetrievalSearchOptions options)
        {
            int? includeNeighbors = options.IncludeNeighbors > 0 ? options.IncludeNeighbors : null;

            Dictionary<string, object> body = new Dictionary<string, object>();

            if (options.SearchMode.Equals("FullText", StringComparison.OrdinalIgnoreCase))
            {
                body["FullText"] = new
                {
                    Query = query,
                    SearchType = options.FullTextSearchType,
                    Language = options.FullTextLanguage,
                    Normalization = options.FullTextNormalization,
                    MinimumScore = options.FullTextMinimumScore
                };
            }
            else if (options.SearchMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
            {
                body["Vector"] = BuildVectorQuery(embeddings, options);
                body["FullText"] = new
                {
                    Query = query,
                    SearchType = options.FullTextSearchType,
                    Language = options.FullTextLanguage,
                    Normalization = options.FullTextNormalization,
                    TextWeight = options.TextWeight,
                    MinimumScore = options.FullTextMinimumScore
                };
                body["Hybrid"] = BuildHybridOptions(options);
            }
            else
            {
                // Vector mode (default)
                body["Vector"] = BuildVectorQuery(embeddings, options);
            }

            body["MaxResults"] = topK;
            if (includeNeighbors.HasValue) body["IncludeNeighbors"] = includeNeighbors.Value;
            AddDocumentFilters(body, options.DocumentIds);

            // Add metadata filters when present
            if (options.MetadataFilter != null && !options.MetadataFilter.IsEmpty)
            {
                AddMetadataFilters(body, options.MetadataFilter);
            }

            return body;
        }

        /// <summary>
        /// Build the RecallDB vector query. A label, tag or document filter is applied after pgvector's HNSW scan,
        /// so filtered searches raise ef_search to keep enough candidates for the filter to leave a full page.
        /// </summary>
        private Dictionary<string, object> BuildVectorQuery(List<double> embeddings, RetrievalSearchOptions options)
        {
            Dictionary<string, object> vector = new Dictionary<string, object>
            {
                ["SearchType"] = "CosineSimilarity",
                ["Embeddings"] = embeddings
            };

            if (_RecallDbSettings.FilteredEfSearch > 0 && HasSearchFilters(options))
                vector["EfSearch"] = _RecallDbSettings.FilteredEfSearch;

            return vector;
        }

        private static bool HasSearchFilters(RetrievalSearchOptions options)
        {
            if (options == null) return false;
            if (NormalizeDocumentIds(options.DocumentIds) != null) return true;
            return options.MetadataFilter != null && !options.MetadataFilter.IsEmpty;
        }

        /// <summary>
        /// Build RecallDB's hybrid fusion options. They are sent explicitly so the fusion that runs is the one the
        /// assistant is configured for, not whatever the store defaults to.
        /// </summary>
        private static Dictionary<string, object> BuildHybridOptions(RetrievalSearchOptions options)
        {
            bool linear = String.Equals(options.FusionStrategy, "Linear", StringComparison.OrdinalIgnoreCase);
            Dictionary<string, object> hybrid = new Dictionary<string, object>
            {
                ["Strategy"] = linear ? "Linear" : "Rrf"
            };

            if (!linear)
            {
                hybrid["RrfK"] = Math.Clamp(options.RrfK, 1, 100000);
                if (options.RecencyWeight > 0) hybrid["RecencyWeight"] = Math.Clamp(options.RecencyWeight, 0.0, 1.0);
            }

            if (options.FusionCandidatePool.HasValue) hybrid["CandidatePool"] = Math.Clamp(options.FusionCandidatePool.Value, 1, 10000);
            return hybrid;
        }

        private async Task<List<SearchResult>> ExecuteSearchWithDocumentFilterAsync(
            string tenantId,
            string collectionId,
            string query,
            List<double> embeddings,
            int topK,
            RetrievalSearchOptions options,
            CancellationToken token)
        {
            List<string> documentIds = NormalizeDocumentIds(options.DocumentIds);
            if (documentIds != null && documentIds.Count > 1 && !_RecallDbSettings.SupportsMultiDocumentFilter)
            {
                _Logging.Warn(_Header + "RecallDB native multi-document filtering is disabled or unavailable; using single-document fallback loop for " + documentIds.Count + " document filters.");
                return await ExecuteSingleDocumentFallbackSearchAsync(tenantId, collectionId, query, embeddings, topK, options, documentIds, token).ConfigureAwait(false);
            }

            return await ExecuteNativeSearchAsync(tenantId, collectionId, query, embeddings, topK, options, token).ConfigureAwait(false);
        }

        private async Task<List<SearchResult>> ExecuteNativeSearchAsync(
            string tenantId,
            string collectionId,
            string query,
            List<double> embeddings,
            int topK,
            RetrievalSearchOptions options,
            CancellationToken token)
        {
            object searchBody = BuildSearchBody(query, embeddings, topK, options);
            List<SearchResult> searchResults = await ExecuteSearchAsync(tenantId, collectionId, searchBody, token).ConfigureAwait(false);

            if (ShouldRunHybridFallback(searchResults, embeddings, options))
            {
                _Logging.Info(_Header + "hybrid search returned 0 results, falling back to vector-only");
                options.HybridFallbackRan = true;
                Dictionary<string, object> vectorOnlyBody = BuildVectorOnlySearchBody(embeddings, topK, options);
                searchResults = await ExecuteSearchAsync(tenantId, collectionId, vectorOnlyBody, token).ConfigureAwait(false);
            }

            return searchResults;
        }

        private async Task<List<SearchResult>> ExecuteSingleDocumentFallbackSearchAsync(
            string tenantId,
            string collectionId,
            string query,
            List<double> embeddings,
            int topK,
            RetrievalSearchOptions options,
            List<string> documentIds,
            CancellationToken token)
        {
            Dictionary<string, (SearchResult Result, int Order)> merged = new Dictionary<string, (SearchResult Result, int Order)>(StringComparer.Ordinal);
            int order = 0;

            foreach (string documentId in documentIds)
            {
                token.ThrowIfCancellationRequested();

                RetrievalSearchOptions perDocumentOptions = CloneSearchOptions(options, options.SearchMode, new List<string> { documentId });
                List<SearchResult> results = await ExecuteNativeSearchAsync(tenantId, collectionId, query, embeddings, topK, perDocumentOptions, token).ConfigureAwait(false);
                if (perDocumentOptions.HybridFallbackRan) options.HybridFallbackRan = true;

                if (results == null || results.Count < 1) continue;
                foreach (SearchResult result in results)
                {
                    if (result == null) continue;
                    string key = BuildSearchResultDedupeKey(result, order);
                    if (merged.TryGetValue(key, out (SearchResult Result, int Order) existing))
                    {
                        if ((result.Score > existing.Result.Score)
                            || (Math.Abs(result.Score - existing.Result.Score) < 0.0000001 && result.TextScore.GetValueOrDefault() > existing.Result.TextScore.GetValueOrDefault()))
                        {
                            merged[key] = (result, existing.Order);
                        }
                    }
                    else
                    {
                        merged[key] = (result, order++);
                    }
                }
            }

            return merged.Values
                .OrderByDescending(item => item.Result.Score)
                .ThenBy(item => item.Order)
                .Take(topK)
                .Select(item => item.Result)
                .ToList();
        }

        private static RetrievalSearchOptions CloneSearchOptions(RetrievalSearchOptions options, string searchMode, List<string> documentIds)
        {
            return new RetrievalSearchOptions
            {
                SearchMode = searchMode,
                TextWeight = options.TextWeight,
                FusionStrategy = options.FusionStrategy,
                RrfK = options.RrfK,
                FusionCandidatePool = options.FusionCandidatePool,
                RecencyWeight = options.RecencyWeight,
                FullTextSearchType = options.FullTextSearchType,
                FullTextLanguage = options.FullTextLanguage,
                FullTextNormalization = options.FullTextNormalization,
                FullTextMinimumScore = options.FullTextMinimumScore,
                ApplyThresholdToFullText = options.ApplyThresholdToFullText,
                EmbeddingTaskPrefixes = options.EmbeddingTaskPrefixes,
                IncludeNeighbors = options.IncludeNeighbors,
                MetadataFilter = options.MetadataFilter,
                DocumentIds = documentIds
            };
        }

        private static bool ShouldRunHybridFallback(List<SearchResult> searchResults, List<double> embeddings, RetrievalSearchOptions options)
        {
            return options.SearchMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase)
                && (searchResults == null || searchResults.Count == 0)
                && embeddings != null;
        }

        private Dictionary<string, object> BuildVectorOnlySearchBody(List<double> embeddings, int topK, RetrievalSearchOptions options)
        {
            Dictionary<string, object> vectorOnlyBody = new Dictionary<string, object>
            {
                ["Vector"] = BuildVectorQuery(embeddings, options),
                ["MaxResults"] = topK
            };
            if (options.IncludeNeighbors > 0) vectorOnlyBody["IncludeNeighbors"] = options.IncludeNeighbors;
            AddDocumentFilters(vectorOnlyBody, options.DocumentIds);
            if (options.MetadataFilter != null && !options.MetadataFilter.IsEmpty)
                AddMetadataFilters(vectorOnlyBody, options.MetadataFilter);

            return vectorOnlyBody;
        }

        private static string BuildSearchResultDedupeKey(SearchResult result, int fallbackOrder)
        {
            string documentId = result.DocumentId?.Trim();
            if (!String.IsNullOrWhiteSpace(documentId) && result.Position.HasValue)
                return documentId + "|" + result.Position.Value;
            if (!String.IsNullOrWhiteSpace(documentId) && !String.IsNullOrWhiteSpace(result.Content))
                return documentId + "|" + result.Content;
            return "row|" + fallbackOrder;
        }

        /// <summary>
        /// Add DocumentId or DocumentIds to the search body after removing empty and duplicate identifiers.
        /// </summary>
        private void AddDocumentFilters(Dictionary<string, object> body, IEnumerable<string> documentIds)
        {
            List<string> normalized = NormalizeDocumentIds(documentIds);
            if (normalized == null || normalized.Count < 1) return;

            // RecallDB's search query only accepts the DocumentIds list; a singular DocumentId property is ignored,
            // which silently searched the whole collection for single-document scopes.
            body["DocumentIds"] = normalized;
        }

        /// <summary>
        /// Normalize document identifiers for retrieval requests.
        /// </summary>
        private static List<string> NormalizeDocumentIds(IEnumerable<string> documentIds)
        {
            if (documentIds == null) return null;

            List<string> normalized = documentIds
                .Where(id => !String.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return normalized.Count > 0 ? normalized : null;
        }

        /// <summary>
        /// Add LabelFilter and TagFilter to the search body from a ChatMetadataFilter.
        /// </summary>
        private void AddMetadataFilters(Dictionary<string, object> body, ChatMetadataFilter filter)
        {
            bool hasRequiredLabels = filter.RequiredLabels != null && filter.RequiredLabels.Count > 0;
            bool hasExcludedLabels = filter.ExcludedLabels != null && filter.ExcludedLabels.Count > 0;
            if (hasRequiredLabels || hasExcludedLabels)
            {
                Dictionary<string, object> labelFilter = new Dictionary<string, object>();
                if (hasRequiredLabels) labelFilter["Required"] = filter.RequiredLabels;
                if (hasExcludedLabels) labelFilter["Excluded"] = filter.ExcludedLabels;
                body["LabelFilter"] = labelFilter;
            }

            bool hasRequiredTags = filter.RequiredTags != null && filter.RequiredTags.Count > 0;
            bool hasExcludedTags = filter.ExcludedTags != null && filter.ExcludedTags.Count > 0;
            if (hasRequiredTags || hasExcludedTags)
            {
                Dictionary<string, object> tagFilter = new Dictionary<string, object>();
                if (hasRequiredTags)
                    tagFilter["Required"] = filter.RequiredTags.Select(t => new { Key = t.Key, Condition = t.Condition, Value = t.Value }).ToList();
                if (hasExcludedTags)
                    tagFilter["Excluded"] = filter.ExcludedTags.Select(t => new { Key = t.Key, Condition = t.Condition, Value = t.Value }).ToList();
                body["TagFilter"] = tagFilter;
            }
        }

        /// <summary>
        /// Execute a search request against RecallDB.
        /// </summary>
        private async Task<List<SearchResult>> ExecuteSearchAsync(string tenantId, string collectionId, object requestBody, CancellationToken token)
        {
            string path = "/v1.0/tenants/" + tenantId + "/collections/" + collectionId + "/search";
            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
            string traceId = Guid.NewGuid().ToString("N");
            Stopwatch sw = Stopwatch.StartNew();

            _Logging.Debug(_Header + "RecallDB search request trace " + traceId + " path " + path + " body " + BuildRedactedSearchBodyLog(json));

            using (HttpResponseMessage response = await _VectorStore.SendAsync(HttpMethod.Post, path, json, token).ConfigureAwait(false))
            {
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Debug(_Header + "RecallDB search response trace " + traceId + " status " + (int)response.StatusCode + " resultCount 0 durationMs " + sw.ElapsedMilliseconds);
                    _Logging.Warn(_Header + "RecallDB search returned " + (int)response.StatusCode + ": " + responseBody);
                    return null;
                }

                SearchResponse searchResult = JsonSerializer.Deserialize<SearchResponse>(responseBody, _JsonOptions);
                int resultCount = searchResult?.Documents != null ? searchResult.Documents.Count : 0;
                _Logging.Debug(_Header + "RecallDB search response trace " + traceId + " status " + (int)response.StatusCode + " resultCount " + resultCount + " durationMs " + sw.ElapsedMilliseconds);
                return searchResult?.Documents;
            }
        }

        private static string BuildRedactedSearchBodyLog(string json)
        {
            if (String.IsNullOrWhiteSpace(json)) return "{}";

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return "{\"body\":\"[redacted]\"}";

                Dictionary<string, object> summary = new Dictionary<string, object>();

                if (TryGetPropertyIgnoreCase(root, "Vector", out JsonElement vector) && vector.ValueKind == JsonValueKind.Object)
                {
                    Dictionary<string, object> vectorSummary = new Dictionary<string, object>();
                    string vectorSearchType = GetStringAny(vector, "SearchType");
                    if (!String.IsNullOrWhiteSpace(vectorSearchType)) vectorSummary["SearchType"] = vectorSearchType;
                    if (TryGetPropertyIgnoreCase(vector, "Embeddings", out JsonElement embeddings) && embeddings.ValueKind == JsonValueKind.Array)
                        vectorSummary["EmbeddingDimensions"] = embeddings.GetArrayLength();
                    if (TryGetPropertyIgnoreCase(vector, "EfSearch", out JsonElement efSearch) && efSearch.ValueKind == JsonValueKind.Number)
                        vectorSummary["EfSearch"] = efSearch.GetRawText();
                    summary["Vector"] = vectorSummary;
                }

                if (TryGetPropertyIgnoreCase(root, "Hybrid", out JsonElement hybrid) && hybrid.ValueKind == JsonValueKind.Object)
                    summary["Hybrid"] = hybrid.Clone();

                if (TryGetPropertyIgnoreCase(root, "FullText", out JsonElement fullText) && fullText.ValueKind == JsonValueKind.Object)
                {
                    Dictionary<string, object> fullTextSummary = new Dictionary<string, object>
                    {
                        ["HasQuery"] = TryGetPropertyIgnoreCase(fullText, "Query", out JsonElement queryElement)
                            && queryElement.ValueKind == JsonValueKind.String
                            && !String.IsNullOrEmpty(queryElement.GetString())
                    };

                    if (TryGetPropertyIgnoreCase(fullText, "Query", out queryElement) && queryElement.ValueKind == JsonValueKind.String)
                        fullTextSummary["QueryLength"] = queryElement.GetString()?.Length ?? 0;

                    string fullTextSearchType = GetStringAny(fullText, "SearchType");
                    if (!String.IsNullOrWhiteSpace(fullTextSearchType)) fullTextSummary["SearchType"] = fullTextSearchType;

                    string language = GetStringAny(fullText, "Language");
                    if (!String.IsNullOrWhiteSpace(language)) fullTextSummary["Language"] = language;

                    if (TryGetPropertyIgnoreCase(fullText, "Normalization", out JsonElement normalization) && normalization.ValueKind == JsonValueKind.Number)
                        fullTextSummary["Normalization"] = normalization.GetRawText();
                    if (TryGetPropertyIgnoreCase(fullText, "TextWeight", out JsonElement textWeight) && textWeight.ValueKind == JsonValueKind.Number)
                        fullTextSummary["TextWeight"] = textWeight.GetRawText();
                    if (TryGetPropertyIgnoreCase(fullText, "MinimumScore", out JsonElement minimumScore) && minimumScore.ValueKind == JsonValueKind.Number)
                        fullTextSummary["MinimumScore"] = minimumScore.GetRawText();

                    summary["FullText"] = fullTextSummary;
                }

                if (TryGetPropertyIgnoreCase(root, "MaxResults", out JsonElement maxResults) && maxResults.ValueKind == JsonValueKind.Number)
                    summary["MaxResults"] = maxResults.GetRawText();
                if (TryGetPropertyIgnoreCase(root, "IncludeNeighbors", out JsonElement includeNeighbors) && includeNeighbors.ValueKind == JsonValueKind.Number)
                    summary["IncludeNeighbors"] = includeNeighbors.GetRawText();

                if (TryGetPropertyIgnoreCase(root, "DocumentId", out JsonElement documentId) && documentId.ValueKind == JsonValueKind.String && !String.IsNullOrWhiteSpace(documentId.GetString()))
                    summary["DocumentIdCount"] = 1;
                if (TryGetPropertyIgnoreCase(root, "DocumentIds", out JsonElement documentIds) && documentIds.ValueKind == JsonValueKind.Array)
                    summary["DocumentIdCount"] = documentIds.GetArrayLength();

                if (TryGetPropertyIgnoreCase(root, "LabelFilter", out JsonElement labelFilter) && labelFilter.ValueKind == JsonValueKind.Object)
                    summary["LabelFilter"] = SummarizeRequiredExcludedFilter(labelFilter);
                if (TryGetPropertyIgnoreCase(root, "TagFilter", out JsonElement tagFilter) && tagFilter.ValueKind == JsonValueKind.Object)
                    summary["TagFilter"] = SummarizeRequiredExcludedFilter(tagFilter);

                return JsonSerializer.Serialize(summary);
            }
            catch (JsonException)
            {
                return "{\"body\":\"[redacted-unparseable]\"}";
            }
        }

        private static Dictionary<string, int> SummarizeRequiredExcludedFilter(JsonElement filter)
        {
            Dictionary<string, int> summary = new Dictionary<string, int>();

            if (TryGetPropertyIgnoreCase(filter, "Required", out JsonElement required) && required.ValueKind == JsonValueKind.Array)
                summary["RequiredCount"] = required.GetArrayLength();
            if (TryGetPropertyIgnoreCase(filter, "Excluded", out JsonElement excluded) && excluded.ValueKind == JsonValueKind.Array)
                summary["ExcludedCount"] = excluded.GetArrayLength();

            return summary;
        }

        private static JsonElement GetObjectOrSelf(JsonElement element, params string[] names)
        {
            if (element.ValueKind != JsonValueKind.Object) return element;

            foreach (string name in names)
            {
                if (TryGetPropertyIgnoreCase(element, name, out JsonElement value) && value.ValueKind == JsonValueKind.Object)
                    return value;
            }

            return element;
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

        /// <summary>
        /// Embed a query string with Partio's standalone embedding route.
        /// </summary>
        /// <remarks>
        /// Queries go to <c>/v1.0/embed</c>, which embeds the text as given. The chunking route (<c>/v1.0/process</c>)
        /// would split a long query at the default chunk size and keep only the first chunk. Each attempt is bounded by
        /// <see cref="ChunkingSettings.QueryEmbeddingTimeoutMs"/> rather than the ingestion timeout, and a timed-out
        /// attempt is not retried. Successful embeddings are cached per endpoint and query text.
        /// </remarks>
        /// <param name="query">Query text.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="embeddingEndpointId">Optional embedding endpoint override.</param>
        /// <returns>Embedding vector, or null when it could not be generated.</returns>
        private async Task<List<double>> EmbedQueryAsync(string query, CancellationToken token, string embeddingEndpointId = null)
        {
            string effectiveEndpointId = !String.IsNullOrEmpty(embeddingEndpointId) ? embeddingEndpointId : _ChunkingSettings.EndpointId;
            string cacheKey = QueryEmbeddingCache.BuildKey(effectiveEndpointId, query);
            if (_EmbeddingCache.Capacity > 0)
            {
                bool hit = _EmbeddingCache.TryGet(cacheKey, out List<double> cached);
                AssistantHubTelemetry.RecordQueryEmbeddingCache(hit);
                if (hit) return cached;
            }

            Dictionary<string, object> requestBody = new Dictionary<string, object>
            {
                ["EndpointId"] = effectiveEndpointId,
                ["Input"] = new List<string> { query }
            };
            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
            int maxAttempts = Math.Max(1, _ChunkingSettings.MaxRetries + 1);

            for (int attempt = 1; ; attempt++)
            {
                int statusCode;
                string responseBody;

                using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    timeout.CancelAfter(_ChunkingSettings.QueryEmbeddingTimeoutMs);
                    try
                    {
                        using (HttpResponseMessage response = await _ChunkingService.SendAsync(HttpMethod.Post, "/v1.0/embed", json, timeout.Token).ConfigureAwait(false))
                        {
                            statusCode = (int)response.StatusCode;
                            responseBody = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
                        }
                    }
                    catch (Exception e) when (!token.IsCancellationRequested && (e is OperationCanceledException || e is TimeoutException))
                    {
                        _Logging.Warn(_Header + "query embedding did not complete within " + _ChunkingSettings.QueryEmbeddingTimeoutMs + " ms (Chunking.QueryEmbeddingTimeoutMs); not retrying");
                        return null;
                    }
                }

                if (statusCode >= 200 && statusCode < 300)
                {
                    List<double> embedding = ParseQueryEmbedding(responseBody);
                    if (embedding == null)
                    {
                        _Logging.Warn(_Header + "embedding service returned no embedding for the query");
                        return null;
                    }

                    _EmbeddingCache.Set(cacheKey, embedding);
                    return embedding;
                }

                bool transient = IsTransientEmbeddingStatus(statusCode) || IngestionServiceBase.IsWrappedTransientPartioError(statusCode, responseBody);
                if (attempt >= maxAttempts || !transient || token.IsCancellationRequested)
                {
                    _Logging.Warn(_Header + "embedding service returned " + statusCode + " after " + attempt + " attempt(s): " + responseBody);
                    return null;
                }

                int delayMs = GetQueryEmbeddingRetryDelayMs(attempt);
                _Logging.Debug(_Header + "embedding service returned " + statusCode + " (transient); retrying in " + delayMs + "ms after attempt " + attempt + " of " + maxAttempts);
                if (delayMs > 0) await Task.Delay(delayMs, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Resolve the query task prefix for an embedding endpoint's model (cached for five minutes).
        /// </summary>
        private async Task<string> ResolveQueryPrefixAsync(string embeddingEndpointId, CancellationToken token)
        {
            string endpointId = !String.IsNullOrEmpty(embeddingEndpointId) ? embeddingEndpointId : _ChunkingSettings.EndpointId;
            if (_QueryPrefixes.TryGetValue(endpointId, out (string Prefix, DateTime ExpiresUtc) cached) && cached.ExpiresUtc > DateTime.UtcNow)
                return cached.Prefix;

            string prefix = "";
            try
            {
                using (HttpResponseMessage response = await _ChunkingService.SendAsync(HttpMethod.Get, "/v1.0/endpoints/embedding/" + Uri.EscapeDataString(endpointId), null, token).ConfigureAwait(false))
                {
                    string body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        using JsonDocument document = JsonDocument.Parse(body);
                        string model = GetStringAny(document.RootElement, "Model");
                        prefix = EmbeddingModelProfiles.Resolve(model).QueryPrefix;
                    }
                    else
                    {
                        _Logging.Warn(_Header + "could not read embedding endpoint " + endpointId + " for task prefixes: " + (int)response.StatusCode);
                    }
                }
            }
            catch (Exception e) when (!token.IsCancellationRequested)
            {
                _Logging.Warn(_Header + "could not resolve task prefix for embedding endpoint " + endpointId + ": " + e.Message);
            }

            _QueryPrefixes[endpointId] = (prefix, DateTime.UtcNow.AddMinutes(5));
            return prefix;
        }

        private List<double> ParseQueryEmbedding(string responseBody)
        {
            if (String.IsNullOrWhiteSpace(responseBody)) return null;

            try
            {
                PartioEmbedResponse embedResponse = JsonSerializer.Deserialize<PartioEmbedResponse>(responseBody, _JsonOptions);
                List<float> vector = embedResponse?.Embeddings != null && embedResponse.Embeddings.Count > 0 ? embedResponse.Embeddings[0] : null;
                if (vector == null || vector.Count == 0) return null;
                return vector.Select(value => (double)value).ToList();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Whether an embedding status code is transient: request timeout (408), Partio's concurrency/queue rejection
        /// (429), or gateway conditions (502/503/504). Matches the ingestion retry policy.
        /// </summary>
        private static bool IsTransientEmbeddingStatus(int statusCode)
        {
            return statusCode == 408 || statusCode == 429 || statusCode == 502 || statusCode == 503 || statusCode == 504;
        }

        /// <summary>
        /// Backoff before the next query-embedding attempt. Queries are latency sensitive, so the base delay is capped at
        /// 250 ms (doubling per attempt) with up to 50% jitter so concurrent queries do not retry in lockstep.
        /// </summary>
        private int GetQueryEmbeddingRetryDelayMs(int failedAttempt)
        {
            int baseDelayMs = Math.Min(_ChunkingSettings.RetryDelayMs, 250);
            if (baseDelayMs <= 0) return 0;
            int delay = baseDelayMs * (1 << Math.Min(Math.Max(0, failedAttempt - 1), 4));
            return delay + Random.Shared.Next(0, delay / 2 + 1);
        }

        #endregion

        #region Private-Classes

        #endregion
    }
}
