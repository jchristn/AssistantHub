namespace AssistantHub.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk.Models;

    /// <summary>
    /// Adds assistant, collection, thread, chat, and endpoint APIs to the SDK client.
    /// </summary>
    public abstract class AssistantHubClientEndpointBase : AssistantHubClientResourceParityBase
    {

        private protected AssistantHubClientEndpointBase(string baseUrl, string apiKey = null)
            : base(baseUrl, apiKey)
        {
        }

        private protected AssistantHubClientEndpointBase(string baseUrl, HttpClient httpClient, string apiKey = null)
            : base(baseUrl, httpClient, apiKey)
        {
        }

        #region Assistants

        /// <summary>
        /// List all assistants.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing assistants.</returns>
        public async Task<EnumerationResult<Assistant>> ListAssistantsAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<Assistant>>(HttpMethod.Get, "/v1.0/assistants", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get an assistant by identifier.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The assistant.</returns>
        public async Task<Assistant> GetAssistantAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            return await SendAsync<Assistant>(HttpMethod.Get, "/v1.0/assistants/" + assistantId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new assistant.
        /// </summary>
        /// <param name="assistant">Assistant to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created assistant.</returns>
        public async Task<Assistant> CreateAssistantAsync(Assistant assistant, CancellationToken cancellationToken = default)
        {
            if (assistant == null)
                throw new ArgumentNullException(nameof(assistant));

            return await SendAsync<Assistant>(HttpMethod.Put, "/v1.0/assistants", assistant, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing assistant.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="assistant">Updated assistant data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated assistant.</returns>
        public async Task<Assistant> UpdateAssistantAsync(string assistantId, Assistant assistant, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (assistant == null)
                throw new ArgumentNullException(nameof(assistant));

            return await SendAsync<Assistant>(HttpMethod.Put, "/v1.0/assistants/" + assistantId, assistant, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an assistant.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteAssistantAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            await SendAsync(HttpMethod.Delete, "/v1.0/assistants/" + assistantId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Collections

        /// <summary>
        /// List all collections.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing collections.</returns>
        public async Task<EnumerationResult<Collection>> ListCollectionsAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<Collection>>(HttpMethod.Get, "/v1.0/collections", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a collection by identifier.
        /// </summary>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The collection.</returns>
        public async Task<Collection> GetCollectionAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            return await SendAsync<Collection>(HttpMethod.Get, "/v1.0/collections/" + collectionId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new collection.
        /// </summary>
        /// <param name="collection">Collection to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created collection.</returns>
        public async Task<Collection> CreateCollectionAsync(Collection collection, CancellationToken cancellationToken = default)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            return await SendAsync<Collection>(HttpMethod.Put, "/v1.0/collections", collection, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing collection.
        /// </summary>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="collection">Updated collection data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated collection.</returns>
        public async Task<Collection> UpdateCollectionAsync(string collectionId, Collection collection, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            return await SendAsync<Collection>(HttpMethod.Put, "/v1.0/collections/" + collectionId, collection, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a collection.
        /// </summary>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteCollectionAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            await SendAsync(HttpMethod.Delete, "/v1.0/collections/" + collectionId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Threads

        /// <summary>
        /// List threads for an assistant.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing thread identifiers.</returns>
        public async Task<EnumerationResult<string>> ListThreadsAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            List<ThreadSummary> summaries = await ListThreadSummariesAsync(
                new EnumerationQuery { AssistantIdFilter = assistantId },
                cancellationToken).ConfigureAwait(false);

            List<string> threadIds = new List<string>();
            if (summaries != null)
            {
                foreach (ThreadSummary summary in summaries)
                {
                    if (!String.IsNullOrWhiteSpace(summary?.ThreadId))
                        threadIds.Add(summary.ThreadId);
                }
            }

            return new EnumerationResult<string>
            {
                Success = true,
                MaxResults = threadIds.Count,
                TotalRecords = threadIds.Count,
                RecordsRemaining = 0,
                ContinuationToken = null,
                EndOfResults = true,
                Objects = threadIds,
                TotalMs = 0
            };
        }

        /// <summary>
        /// Get thread history.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="threadId">Thread identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of messages in the thread.</returns>
        public async Task<List<ChatCompletionMessage>> GetThreadAsync(string assistantId, string threadId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (String.IsNullOrWhiteSpace(threadId))
                throw new ArgumentNullException(nameof(threadId));

            return await SendAsync<List<ChatCompletionMessage>>(HttpMethod.Get, "/v1.0/assistants/" + assistantId + "/threads/" + threadId + "/history", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new thread for an assistant.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created thread identifier.</returns>
        public async Task<string> CreateThreadAsync(string assistantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));

            Dictionary<string, JsonElement> response = await SendAsync<Dictionary<string, JsonElement>>(HttpMethod.Post, "/v1.0/assistants/" + assistantId + "/threads", cancellationToken: cancellationToken).ConfigureAwait(false);
            return response["ThreadId"].GetString();
        }

        /// <summary>
        /// Delete a thread.
        /// </summary>
        /// <param name="threadId">Thread identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(threadId))
                throw new ArgumentNullException(nameof(threadId));

            await SendAsync(HttpMethod.Delete, "/v1.0/threads/" + threadId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Chat

        /// <summary>
        /// Send a chat message to an assistant and receive a complete response.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="request">Chat completion request.</param>
        /// <param name="threadId">Optional thread identifier for conversation continuity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Chat completion response.</returns>
        public async Task<ChatCompletionResponse> SendMessageAsync(string assistantId, ChatCompletionRequest request, string threadId = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.Stream = false;

            string path = "/v1.0/assistants/" + assistantId + "/chat";
            string json = SerializeJson(request);

            using (HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl + path))
            {
                httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(threadId))
                {
                    httpRequest.Headers.Add(_ThreadIdHeader, threadId);
                }

                using (HttpResponseMessage response = await SendRawAsync(httpRequest, cancellationToken).ConfigureAwait(false))
                {
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return DeserializeJson<ChatCompletionResponse>(responseBody);
                }
            }
        }

        /// <summary>
        /// Send a chat message to an assistant and stream the response as server-sent events.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="request">Chat completion request.</param>
        /// <param name="threadId">Optional thread identifier for conversation continuity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async enumerable of SSE data strings.</returns>
        public async IAsyncEnumerable<string> SendMessageStreamAsync(string assistantId, ChatCompletionRequest request, string threadId = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.Stream = true;

            string path = "/v1.0/assistants/" + assistantId + "/chat";
            string json = SerializeJson(request);

            using (HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl + path))
            {
                httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(threadId))
                {
                    httpRequest.Headers.Add(_ThreadIdHeader, threadId);
                }

                using (HttpResponseMessage response = await SendRawAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            while (!reader.EndOfStream)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                string line = await reader.ReadLineAsync().ConfigureAwait(false);

                                if (line == null)
                                    break;

                                if (line.StartsWith("data: "))
                                {
                                    string data = line.Substring(6);

                                    if (data == "[DONE]")
                                        yield break;

                                    yield return data;
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Embedding-Endpoints

        /// <summary>
        /// List embedding endpoints.
        /// </summary>
        /// <param name="query">Optional enumeration query for pagination and filtering.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing embedding endpoints.</returns>
        public async Task<EnumerationResult<EmbeddingEndpoint>> ListEmbeddingEndpointsAsync(EnumerationQuery query = null, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<EmbeddingEndpoint>>(HttpMethod.Post, "/v1.0/endpoints/embedding/enumerate", query, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get an embedding endpoint by identifier.
        /// </summary>
        /// <param name="endpointId">Embedding endpoint identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The embedding endpoint.</returns>
        public async Task<EmbeddingEndpoint> GetEmbeddingEndpointAsync(string endpointId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));

            return await SendAsync<EmbeddingEndpoint>(HttpMethod.Get, "/v1.0/endpoints/embedding/" + endpointId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new embedding endpoint.
        /// </summary>
        /// <param name="endpoint">Embedding endpoint to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created embedding endpoint.</returns>
        public async Task<EmbeddingEndpoint> CreateEmbeddingEndpointAsync(EmbeddingEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            return await SendAsync<EmbeddingEndpoint>(HttpMethod.Put, "/v1.0/endpoints/embedding", endpoint, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing embedding endpoint.
        /// </summary>
        /// <param name="endpointId">Embedding endpoint identifier.</param>
        /// <param name="endpoint">Updated embedding endpoint data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated embedding endpoint.</returns>
        public async Task<EmbeddingEndpoint> UpdateEmbeddingEndpointAsync(string endpointId, EmbeddingEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            return await SendAsync<EmbeddingEndpoint>(HttpMethod.Put, "/v1.0/endpoints/embedding/" + endpointId, endpoint, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an embedding endpoint.
        /// </summary>
        /// <param name="endpointId">Embedding endpoint identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteEmbeddingEndpointAsync(string endpointId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));

            await SendAsync(HttpMethod.Delete, "/v1.0/endpoints/embedding/" + endpointId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check health status of all embedding endpoints.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of endpoint health statuses.</returns>
        public async Task<List<EndpointHealthStatus>> CheckEmbeddingHealthAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<List<EndpointHealthStatus>>(HttpMethod.Get, "/v1.0/endpoints/embedding/health", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Completion-Endpoints

        /// <summary>
        /// List completion endpoints.
        /// </summary>
        /// <param name="query">Optional enumeration query for pagination and filtering.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing completion endpoints.</returns>
        public async Task<EnumerationResult<CompletionEndpoint>> ListCompletionEndpointsAsync(EnumerationQuery query = null, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<CompletionEndpoint>>(HttpMethod.Post, "/v1.0/endpoints/completion/enumerate", query, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a completion endpoint by identifier.
        /// </summary>
        /// <param name="endpointId">Completion endpoint identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The completion endpoint.</returns>
        public async Task<CompletionEndpoint> GetCompletionEndpointAsync(string endpointId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));

            return await SendAsync<CompletionEndpoint>(HttpMethod.Get, "/v1.0/endpoints/completion/" + endpointId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new completion endpoint.
        /// </summary>
        /// <param name="endpoint">Completion endpoint to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created completion endpoint.</returns>
        public async Task<CompletionEndpoint> CreateCompletionEndpointAsync(CompletionEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            return await SendAsync<CompletionEndpoint>(HttpMethod.Put, "/v1.0/endpoints/completion", endpoint, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing completion endpoint.
        /// </summary>
        /// <param name="endpointId">Completion endpoint identifier.</param>
        /// <param name="endpoint">Updated completion endpoint data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated completion endpoint.</returns>
        public async Task<CompletionEndpoint> UpdateCompletionEndpointAsync(string endpointId, CompletionEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            return await SendAsync<CompletionEndpoint>(HttpMethod.Put, "/v1.0/endpoints/completion/" + endpointId, endpoint, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a completion endpoint.
        /// </summary>
        /// <param name="endpointId">Completion endpoint identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteCompletionEndpointAsync(string endpointId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(endpointId))
                throw new ArgumentNullException(nameof(endpointId));

            await SendAsync(HttpMethod.Delete, "/v1.0/endpoints/completion/" + endpointId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check health status of all completion endpoints.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of endpoint health statuses.</returns>
        public async Task<List<EndpointHealthStatus>> CheckCompletionHealthAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<List<EndpointHealthStatus>>(HttpMethod.Get, "/v1.0/endpoints/completion/health", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}