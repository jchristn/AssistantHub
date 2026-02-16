namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
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
        /// <returns>List of content strings from matching document chunks.</returns>
        public async Task<List<string>> RetrieveAsync(string collectionId, string query, int topK, double scoreThreshold, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

            List<string> results = new List<string>();

            try
            {
                // Step 1: Embed the query using the Partio chunking service
                List<double> queryEmbeddings = await EmbedQueryAsync(query, token).ConfigureAwait(false);
                if (queryEmbeddings == null || queryEmbeddings.Count == 0)
                {
                    _Logging.Warn(_Header + "failed to generate embeddings for query");
                    return results;
                }

                _Logging.Debug(_Header + "generated " + queryEmbeddings.Count + "-dimensional embedding for query");

                // Step 2: Search RecallDB with the embeddings
                List<SearchResult> searchResults = await SearchRecallDbAsync(collectionId, queryEmbeddings, topK, token).ConfigureAwait(false);
                if (searchResults == null || searchResults.Count == 0)
                {
                    _Logging.Debug(_Header + "no search results returned from RecallDB");
                    return results;
                }

                _Logging.Debug(_Header + "received " + searchResults.Count + " results from RecallDB");

                // Step 3: Filter by score threshold and collect content
                foreach (SearchResult result in searchResults)
                {
                    if (result.Score >= scoreThreshold)
                    {
                        if (!String.IsNullOrEmpty(result.Content))
                        {
                            results.Add(result.Content);
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
        /// Embed a query string using the Partio chunking service.
        /// </summary>
        /// <param name="query">Query text.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Embedding vector.</returns>
        private async Task<List<double>> EmbedQueryAsync(string query, CancellationToken token)
        {
            string url = _ChunkingSettings.Endpoint.TrimEnd('/') + "/v1.0/endpoints/" + _ChunkingSettings.EndpointId + "/embeddings";

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                object requestBody = new { Text = query };
                string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(_ChunkingSettings.AccessKey))
                {
                    request.Headers.Add("x-api-key", _ChunkingSettings.AccessKey);
                }

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "embedding service returned " + (int)response.StatusCode + ": " + responseBody);
                    return null;
                }

                EmbeddingResponse embeddingResult = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody, _JsonOptions);
                return embeddingResult?.Embeddings;
            }
        }

        /// <summary>
        /// Search RecallDB for similar documents.
        /// </summary>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="embeddings">Query embedding vector.</param>
        /// <param name="topK">Number of top results to retrieve.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of search results.</returns>
        private async Task<List<SearchResult>> SearchRecallDbAsync(string collectionId, List<double> embeddings, int topK, CancellationToken token)
        {
            string url = _RecallDbSettings.Endpoint.TrimEnd('/') + "/v1.0/tenants/" + _RecallDbSettings.TenantId + "/collections/" + collectionId + "/search";

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                object requestBody = new
                {
                    Embeddings = embeddings,
                    TopK = topK
                };

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

        #endregion

        #region Private-Classes

        /// <summary>
        /// Embedding response from the Partio service.
        /// </summary>
        private class EmbeddingResponse
        {
            /// <summary>
            /// Embedding vector.
            /// </summary>
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
            /// Similarity score.
            /// </summary>
            public double Score { get; set; } = 0;

            /// <summary>
            /// Text content of the matching chunk.
            /// </summary>
            public string Content { get; set; } = null;
        }

        #endregion
    }
}
