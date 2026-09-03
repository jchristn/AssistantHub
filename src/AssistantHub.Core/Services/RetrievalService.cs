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

                if (!searchOptions.SearchMode.Equals("FullText", StringComparison.OrdinalIgnoreCase))
                {
                    queryEmbeddings = await EmbedQueryAsync(query, token, embeddingEndpointId).ConfigureAwait(false);
                    if (queryEmbeddings == null || queryEmbeddings.Count == 0)
                    {
                        _Logging.Warn(_Header + "failed to generate embeddings for query");
                        return results;
                    }

                    _Logging.Debug(_Header + "generated " + queryEmbeddings.Count + "-dimensional embedding for query");
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
                    searchOptions,
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
                    if (result.Score >= scoreThreshold)
                    {
                        if (!String.IsNullOrEmpty(result.Content))
                        {
                            results.Add(new RetrievalChunk
                            {
                                DocumentId = result.DocumentId,
                                Score = Math.Round(result.Score, 6),
                                TextScore = result.TextScore.HasValue ? Math.Round(result.TextScore.Value, 6) : null,
                                Content = result.Content,
                                Position = result.Position,
                                Neighbors = result.Neighbors?.Select(n => new RetrievalChunk
                                {
                                    DocumentId = n.DocumentId,
                                    Content = n.Content,
                                    Position = n.Position
                                }).ToList()
                            });
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
                body["Vector"] = new { SearchType = "CosineSimilarity", Embeddings = embeddings };
                body["FullText"] = new
                {
                    Query = query,
                    SearchType = options.FullTextSearchType,
                    Language = options.FullTextLanguage,
                    Normalization = options.FullTextNormalization,
                    TextWeight = options.TextWeight,
                    MinimumScore = options.FullTextMinimumScore
                };
            }
            else
            {
                // Vector mode (default)
                body["Vector"] = new { SearchType = "CosineSimilarity", Embeddings = embeddings };
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

                RetrievalSearchOptions perDocumentOptions = CloneSearchOptionsForDocument(options, documentId);
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

        private static RetrievalSearchOptions CloneSearchOptionsForDocument(RetrievalSearchOptions options, string documentId)
        {
            return new RetrievalSearchOptions
            {
                SearchMode = options.SearchMode,
                TextWeight = options.TextWeight,
                FullTextSearchType = options.FullTextSearchType,
                FullTextLanguage = options.FullTextLanguage,
                FullTextNormalization = options.FullTextNormalization,
                FullTextMinimumScore = options.FullTextMinimumScore,
                IncludeNeighbors = options.IncludeNeighbors,
                MetadataFilter = options.MetadataFilter,
                DocumentIds = new List<string> { documentId }
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
                ["Vector"] = new { SearchType = "CosineSimilarity", Embeddings = embeddings },
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

            if (normalized.Count == 1)
            {
                body["DocumentId"] = normalized[0];
            }
            else
            {
                body["DocumentIds"] = normalized;
            }
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
                    summary["Vector"] = vectorSummary;
                }

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
        /// Embed a query string using the Partio chunking service.
        /// </summary>
        /// <param name="query">Query text.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="embeddingEndpointId">Optional embedding endpoint override.</param>
        /// <returns>Embedding vector.</returns>
        private async Task<List<double>> EmbedQueryAsync(string query, CancellationToken token, string embeddingEndpointId = null)
        {
            string effectiveEndpointId = !String.IsNullOrEmpty(embeddingEndpointId) ? embeddingEndpointId : _ChunkingSettings.EndpointId;
            object requestBody = new
            {
                Type = "Text",
                Text = query,
                EmbeddingConfiguration = new { EmbeddingEndpointId = effectiveEndpointId }
            };
            string json = JsonSerializer.Serialize(requestBody, _JsonOptions);

            using (HttpResponseMessage response = await _ChunkingService.SendAsync(HttpMethod.Post, "/v1.0/process", json, token).ConfigureAwait(false))
            {
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "embedding service returned " + (int)response.StatusCode + ": " + responseBody);
                    return null;
                }

                ProcessResponse processResult = JsonSerializer.Deserialize<ProcessResponse>(responseBody, _JsonOptions);
                if (processResult?.Chunks != null && processResult.Chunks.Count > 0)
                {
                    return processResult.Chunks[0].Embeddings;
                }

                return null;
            }
        }

        #endregion

        #region Private-Classes

        #endregion
    }
}
