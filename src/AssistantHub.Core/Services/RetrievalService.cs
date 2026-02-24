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
        private RecallDbSettings _RecallDbSettings = null;
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
        public RetrievalService(ChunkingSettings chunkingSettings, RecallDbSettings recallDbSettings, LoggingModule logging)
        {
            _ChunkingSettings = chunkingSettings ?? throw new ArgumentNullException(nameof(chunkingSettings));
            _RecallDbSettings = recallDbSettings ?? throw new ArgumentNullException(nameof(recallDbSettings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _HttpClient = new HttpClient();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Retrieve relevant document chunks for a given query.
        /// </summary>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="query">Search query text.</param>
        /// <param name="topK">Number of top results to retrieve.</param>
        /// <param name="scoreThreshold">Minimum score threshold for results (0.0 to 1.0).</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="embeddingEndpointId">Optional embedding endpoint override.</param>
        /// <param name="searchOptions">Search mode and full-text options.</param>
        /// <returns>List of retrieval chunks with source identification and scoring.</returns>
        public async Task<List<RetrievalChunk>> RetrieveAsync(
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
                List<SearchResult> searchResults = await ExecuteSearchAsync(collectionId, searchBody, token).ConfigureAwait(false);

                // Step 3.5: Hybrid fallback to vector-only if 0 results
                if (searchOptions.SearchMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase)
                    && (searchResults == null || searchResults.Count == 0)
                    && queryEmbeddings != null)
                {
                    _Logging.Info(_Header + "hybrid search returned 0 results, falling back to vector-only");
                    object vectorOnlyBody = new
                    {
                        Vector = new { SearchType = "CosineSimilarity", Embeddings = queryEmbeddings },
                        MaxResults = topK,
                        IncludeNeighbors = searchOptions.IncludeNeighbors > 0 ? searchOptions.IncludeNeighbors : (int?)null
                    };
                    searchResults = await ExecuteSearchAsync(collectionId, vectorOnlyBody, token).ConfigureAwait(false);
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

            if (options.SearchMode.Equals("FullText", StringComparison.OrdinalIgnoreCase))
            {
                return new
                {
                    FullText = new
                    {
                        Query = query,
                        SearchType = options.FullTextSearchType,
                        Language = options.FullTextLanguage,
                        Normalization = options.FullTextNormalization,
                        MinimumScore = options.FullTextMinimumScore
                    },
                    MaxResults = topK,
                    IncludeNeighbors = includeNeighbors
                };
            }
            else if (options.SearchMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
            {
                return new
                {
                    Vector = new
                    {
                        SearchType = "CosineSimilarity",
                        Embeddings = embeddings
                    },
                    FullText = new
                    {
                        Query = query,
                        SearchType = options.FullTextSearchType,
                        Language = options.FullTextLanguage,
                        Normalization = options.FullTextNormalization,
                        TextWeight = options.TextWeight,
                        MinimumScore = options.FullTextMinimumScore
                    },
                    MaxResults = topK,
                    IncludeNeighbors = includeNeighbors
                };
            }
            else
            {
                // Vector mode (default)
                return new
                {
                    Vector = new
                    {
                        SearchType = "CosineSimilarity",
                        Embeddings = embeddings
                    },
                    MaxResults = topK,
                    IncludeNeighbors = includeNeighbors
                };
            }
        }

        /// <summary>
        /// Execute a search request against RecallDB.
        /// </summary>
        private async Task<List<SearchResult>> ExecuteSearchAsync(string collectionId, object requestBody, CancellationToken token)
        {
            string url = _RecallDbSettings.Endpoint.TrimEnd('/') + "/v1.0/tenants/" + _RecallDbSettings.TenantId + "/collections/" + collectionId + "/search";

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(_RecallDbSettings.AccessKey))
                {
                    request.Headers.Add("Authorization", "Bearer " + _RecallDbSettings.AccessKey);
                }

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
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
            string url = _ChunkingSettings.Endpoint.TrimEnd('/') + "/v1.0/process";

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                string effectiveEndpointId = !String.IsNullOrEmpty(embeddingEndpointId) ? embeddingEndpointId : _ChunkingSettings.EndpointId;
                object requestBody = new
                {
                    Type = "Text",
                    Text = query,
                    EmbeddingConfiguration = new { EmbeddingEndpointId = effectiveEndpointId }
                };
                string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(_ChunkingSettings.AccessKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _ChunkingSettings.AccessKey);
                }

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
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

        /// <summary>
        /// Process response from the Partio v0.2.0 service.
        /// </summary>
        private class ProcessResponse
        {
            public Guid GUID { get; set; }
            public string Type { get; set; } = null;
            public string Text { get; set; } = null;
            public List<ProcessChunk> Chunks { get; set; } = null;
            public List<ProcessResponse> Children { get; set; } = null;
        }

        /// <summary>
        /// A single chunk from the Partio process response.
        /// </summary>
        private class ProcessChunk
        {
            public Guid CellGUID { get; set; }
            public string Text { get; set; } = null;
            public List<double> Embeddings { get; set; } = null;
        }

        /// <summary>
        /// Search response from RecallDB.
        /// </summary>
        private class SearchResponse
        {
            /// <summary>
            /// List of matching documents.
            /// </summary>
            public List<SearchResult> Documents { get; set; } = null;
        }

        /// <summary>
        /// A single search result from RecallDB.
        /// </summary>
        private class SearchResult
        {
            /// <summary>
            /// Document identifier (maps to AssistantDocument.Id).
            /// </summary>
            public string DocumentId { get; set; } = null;

            /// <summary>
            /// Similarity score.
            /// </summary>
            public double Score { get; set; } = 0;

            /// <summary>
            /// Full-text relevance score (null in vector-only mode).
            /// </summary>
            public double? TextScore { get; set; }

            /// <summary>
            /// Text content of the matching chunk.
            /// </summary>
            public string Content { get; set; } = null;

            /// <summary>
            /// Positional index of this chunk within its source document.
            /// </summary>
            public int? Position { get; set; } = null;

            /// <summary>
            /// Neighboring chunks surrounding this match in positional order.
            /// Populated when IncludeNeighbors is specified on the search query.
            /// </summary>
            public List<SearchResult> Neighbors { get; set; } = null;
        }

        #endregion
    }
}
