namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
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

            List<RetrievalChunk> results = new List<RetrievalChunk>();

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

                // Step 2: Build search request body based on search mode
                object searchBody = BuildSearchBody(query, queryEmbeddings, topK, searchOptions);

                // Step 3: Execute search against RecallDB
                List<SearchResult> searchResults = await ExecuteSearchAsync(tenantId, collectionId, searchBody, token).ConfigureAwait(false);

                // Step 3.5: Hybrid fallback to vector-only if 0 results
                if (searchOptions.SearchMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase)
                    && (searchResults == null || searchResults.Count == 0)
                    && queryEmbeddings != null)
                {
                    _Logging.Info(_Header + "hybrid search returned 0 results, falling back to vector-only");
                    Dictionary<string, object> vectorOnlyBody = new Dictionary<string, object>
                    {
                        ["Vector"] = new { SearchType = "CosineSimilarity", Embeddings = queryEmbeddings },
                        ["MaxResults"] = topK
                    };
                    if (searchOptions.IncludeNeighbors > 0) vectorOnlyBody["IncludeNeighbors"] = searchOptions.IncludeNeighbors;
                    if (searchOptions.MetadataFilter != null && !searchOptions.MetadataFilter.IsEmpty)
                        AddMetadataFilters(vectorOnlyBody, searchOptions.MetadataFilter);
                    searchResults = await ExecuteSearchAsync(tenantId, collectionId, vectorOnlyBody, token).ConfigureAwait(false);
                }

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
                _Logging.Warn(_Header + "exception during retrieval: " + e.Message);
            }

            return results;
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

            // Add metadata filters when present
            if (options.MetadataFilter != null && !options.MetadataFilter.IsEmpty)
            {
                AddMetadataFilters(body, options.MetadataFilter);
            }

            return body;
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

            using (HttpResponseMessage response = await _VectorStore.SendAsync(HttpMethod.Post, path, json, token).ConfigureAwait(false))
            {
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "RecallDB search returned " + (int)response.StatusCode + ": " + responseBody);
                    return null;
                }

                SearchResponse searchResult = JsonSerializer.Deserialize<SearchResponse>(responseBody, _JsonOptions);
                return searchResult?.Documents;
            }
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
