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
    /// Client for the AssistantHub API providing methods for assistants, collections, threads, chat,
    /// endpoints, documents, ingestion rules, and search.
    /// </summary>
    public partial class AssistantHubClient : AssistantHubClientBase
    {
        #region Private-Members

        private const string _ThreadIdHeader = "X-Thread-ID";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the AssistantHub client.
        /// </summary>
        /// <param name="baseUrl">Base URL of the AssistantHub server.</param>
        /// <param name="apiKey">Optional API key for authentication.</param>
        public AssistantHubClient(string baseUrl, string apiKey = null)
            : base(baseUrl, apiKey)
        {
        }

        /// <summary>
        /// Instantiate the AssistantHub client with a provided HttpClient.
        /// </summary>
        /// <param name="baseUrl">Base URL of the AssistantHub server.</param>
        /// <param name="httpClient">HttpClient instance to use.</param>
        /// <param name="apiKey">Optional API key for authentication.</param>
        public AssistantHubClient(string baseUrl, HttpClient httpClient, string apiKey = null)
            : base(baseUrl, httpClient, apiKey)
        {
        }

        #endregion

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

        #region Documents

        /// <summary>
        /// List documents with optional filtering.
        /// </summary>
        /// <param name="query">Optional enumeration query for pagination and filtering.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing documents.</returns>
        public async Task<EnumerationResult<AssistantDocument>> ListDocumentsAsync(EnumerationQuery query = null, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<AssistantDocument>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/documents", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a document by identifier.
        /// </summary>
        /// <param name="documentId">Document identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The document.</returns>
        public async Task<AssistantDocument> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(documentId))
                throw new ArgumentNullException(nameof(documentId));

            return await SendAsync<AssistantDocument>(HttpMethod.Get, "/v1.0/documents/" + documentId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Upload a document for ingestion.
        /// </summary>
        /// <param name="ingestionRuleId">Ingestion rule identifier to process the document with.</param>
        /// <param name="content">Raw file content as bytes.</param>
        /// <param name="name">Optional display name for the document.</param>
        /// <param name="originalFilename">Optional original filename.</param>
        /// <param name="contentType">Optional MIME content type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created document.</returns>
        public async Task<AssistantDocument> UploadDocumentAsync(
            string ingestionRuleId,
            byte[] content,
            string name = null,
            string originalFilename = null,
            string contentType = null,
            CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(ingestionRuleId))
                throw new ArgumentNullException(nameof(ingestionRuleId));
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            Dictionary<string, object> body = new Dictionary<string, object>
            {
                { "IngestionRuleId", ingestionRuleId },
                { "Base64Content", Convert.ToBase64String(content) }
            };

            if (!String.IsNullOrEmpty(name))
                body["Name"] = name;
            if (!String.IsNullOrEmpty(originalFilename))
                body["OriginalFilename"] = originalFilename;
            if (!String.IsNullOrEmpty(contentType))
                body["ContentType"] = contentType;

            return await SendAsync<AssistantDocument>(HttpMethod.Put, "/v1.0/documents", body, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Upload a document for ingestion from a stream.
        /// </summary>
        /// <param name="ingestionRuleId">Ingestion rule identifier to process the document with.</param>
        /// <param name="stream">Stream containing the file content.</param>
        /// <param name="name">Optional display name for the document.</param>
        /// <param name="originalFilename">Optional original filename.</param>
        /// <param name="contentType">Optional MIME content type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created document.</returns>
        public async Task<AssistantDocument> UploadDocumentAsync(
            string ingestionRuleId,
            Stream stream,
            string name = null,
            string originalFilename = null,
            string contentType = null,
            CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(ingestionRuleId))
                throw new ArgumentNullException(nameof(ingestionRuleId));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (MemoryStream memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream, 81920, cancellationToken).ConfigureAwait(false);
                return await UploadDocumentAsync(ingestionRuleId, memoryStream.ToArray(), name, originalFilename, contentType, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Delete a document.
        /// </summary>
        /// <param name="documentId">Document identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(documentId))
                throw new ArgumentNullException(nameof(documentId));

            await SendAsync(HttpMethod.Delete, "/v1.0/documents/" + documentId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete multiple documents at once.
        /// </summary>
        /// <param name="documentIds">List of document identifiers to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task BulkDeleteDocumentsAsync(List<string> documentIds, CancellationToken cancellationToken = default)
        {
            if (documentIds == null)
                throw new ArgumentNullException(nameof(documentIds));

            await SendAsync(HttpMethod.Post, "/v1.0/documents/delete", documentIds, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Ingestion-Rules

        /// <summary>
        /// List ingestion rules.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing ingestion rules.</returns>
        public async Task<EnumerationResult<IngestionRule>> ListIngestionRulesAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<IngestionRule>>(HttpMethod.Get, "/v1.0/ingestion-rules", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get an ingestion rule by identifier.
        /// </summary>
        /// <param name="ruleId">Ingestion rule identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The ingestion rule.</returns>
        public async Task<IngestionRule> GetIngestionRuleAsync(string ruleId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(ruleId))
                throw new ArgumentNullException(nameof(ruleId));

            return await SendAsync<IngestionRule>(HttpMethod.Get, "/v1.0/ingestion-rules/" + ruleId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new ingestion rule.
        /// </summary>
        /// <param name="rule">Ingestion rule to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created ingestion rule.</returns>
        public async Task<IngestionRule> CreateIngestionRuleAsync(IngestionRule rule, CancellationToken cancellationToken = default)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            return await SendAsync<IngestionRule>(HttpMethod.Put, "/v1.0/ingestion-rules", rule, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing ingestion rule.
        /// </summary>
        /// <param name="ruleId">Ingestion rule identifier.</param>
        /// <param name="rule">Updated ingestion rule data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated ingestion rule.</returns>
        public async Task<IngestionRule> UpdateIngestionRuleAsync(string ruleId, IngestionRule rule, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(ruleId))
                throw new ArgumentNullException(nameof(ruleId));
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            return await SendAsync<IngestionRule>(HttpMethod.Put, "/v1.0/ingestion-rules/" + ruleId, rule, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an ingestion rule.
        /// </summary>
        /// <param name="ruleId">Ingestion rule identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteIngestionRuleAsync(string ruleId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(ruleId))
                throw new ArgumentNullException(nameof(ruleId));

            await SendAsync(HttpMethod.Delete, "/v1.0/ingestion-rules/" + ruleId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Inference

        /// <summary>
        /// List available inference models.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of available models.</returns>
        public async Task<List<InferenceModel>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<List<InferenceModel>>(HttpMethod.Get, "/v1.0/models", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Pull a model from the provider.
        /// </summary>
        /// <param name="modelName">Name of the model to pull.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task PullModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(modelName))
                throw new ArgumentNullException(nameof(modelName));

            Dictionary<string, string> body = new Dictionary<string, string> { { "Name", modelName } };
            await SendAsync(HttpMethod.Post, "/v1.0/models/pull", body, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the status of a model pull operation.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Pull progress information.</returns>
        public async Task<PullProgress> GetPullStatusAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<PullProgress>(HttpMethod.Get, "/v1.0/models/pull/status", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a model from the provider.
        /// </summary>
        /// <param name="modelName">Name of the model to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(modelName))
                throw new ArgumentNullException(nameof(modelName));

            await SendAsync(HttpMethod.Delete, "/v1.0/models/" + Uri.EscapeDataString(modelName), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a lightweight inference-only request (no RAG retrieval).
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="request">Chat completion request.</param>
        /// <param name="threadId">Optional thread identifier for conversation continuity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Chat completion response.</returns>
        public async Task<ChatCompletionResponse> GenerateAsync(string assistantId, ChatCompletionRequest request, string threadId = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.Stream = false;

            string path = "/v1.0/assistants/" + assistantId + "/generate";
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
        /// Send a lightweight inference-only request and stream the response as server-sent events.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="request">Chat completion request.</param>
        /// <param name="threadId">Optional thread identifier for conversation continuity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async enumerable of SSE data strings.</returns>
        public async IAsyncEnumerable<string> GenerateStreamAsync(string assistantId, ChatCompletionRequest request, string threadId = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.Stream = true;

            string path = "/v1.0/assistants/" + assistantId + "/generate";
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

        #region Eval

        /// <summary>
        /// List evaluation facts.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing eval facts.</returns>
        public async Task<EnumerationResult<EvalFact>> ListEvalFactsAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<EvalFact>>(HttpMethod.Get, "/v1.0/eval/facts", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get an evaluation fact by identifier.
        /// </summary>
        /// <param name="factId">Eval fact identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The eval fact.</returns>
        public async Task<EvalFact> GetEvalFactAsync(string factId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(factId))
                throw new ArgumentNullException(nameof(factId));

            return await SendAsync<EvalFact>(HttpMethod.Get, "/v1.0/eval/facts/" + factId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new evaluation fact.
        /// </summary>
        /// <param name="fact">Eval fact to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created eval fact.</returns>
        public async Task<EvalFact> CreateEvalFactAsync(EvalFact fact, CancellationToken cancellationToken = default)
        {
            if (fact == null)
                throw new ArgumentNullException(nameof(fact));

            return await SendAsync<EvalFact>(HttpMethod.Put, "/v1.0/eval/facts", fact, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing evaluation fact.
        /// </summary>
        /// <param name="factId">Eval fact identifier.</param>
        /// <param name="fact">Updated eval fact data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated eval fact.</returns>
        public async Task<EvalFact> UpdateEvalFactAsync(string factId, EvalFact fact, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(factId))
                throw new ArgumentNullException(nameof(factId));
            if (fact == null)
                throw new ArgumentNullException(nameof(fact));

            return await SendAsync<EvalFact>(HttpMethod.Put, "/v1.0/eval/facts/" + factId, fact, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an evaluation fact.
        /// </summary>
        /// <param name="factId">Eval fact identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteEvalFactAsync(string factId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(factId))
                throw new ArgumentNullException(nameof(factId));

            await SendAsync(HttpMethod.Delete, "/v1.0/eval/facts/" + factId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Start an evaluation run.
        /// </summary>
        /// <param name="request">Eval run request specifying the assistant and optional judge prompt.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created eval run.</returns>
        public async Task<EvalRun> StartEvalRunAsync(EvalRunRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendAsync<EvalRun>(HttpMethod.Post, "/v1.0/eval/runs", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List evaluation runs.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing eval runs.</returns>
        public async Task<EnumerationResult<EvalRun>> ListEvalRunsAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<EvalRun>>(HttpMethod.Get, "/v1.0/eval/runs", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get an evaluation run by identifier.
        /// </summary>
        /// <param name="runId">Eval run identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The eval run.</returns>
        public async Task<EvalRun> GetEvalRunAsync(string runId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(runId))
                throw new ArgumentNullException(nameof(runId));

            return await SendAsync<EvalRun>(HttpMethod.Get, "/v1.0/eval/runs/" + runId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an evaluation run.
        /// </summary>
        /// <param name="runId">Eval run identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteEvalRunAsync(string runId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(runId))
                throw new ArgumentNullException(nameof(runId));

            await SendAsync(HttpMethod.Delete, "/v1.0/eval/runs/" + runId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get results for an evaluation run.
        /// </summary>
        /// <param name="runId">Eval run identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing eval results.</returns>
        public async Task<EnumerationResult<EvalResult>> ListEvalResultsAsync(string runId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(runId))
                throw new ArgumentNullException(nameof(runId));

            List<EvalResult> results = await GetEvalRunResultsAsync(runId, cancellationToken).ConfigureAwait(false);

            return new EnumerationResult<EvalResult>
            {
                Success = true,
                MaxResults = results?.Count ?? 0,
                TotalRecords = results?.Count ?? 0,
                RecordsRemaining = 0,
                ContinuationToken = null,
                EndOfResults = true,
                Objects = results ?? new List<EvalResult>(),
                TotalMs = 0
            };
        }

        /// <summary>
        /// Get the default judge prompt.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The default judge prompt string.</returns>
        public async Task<string> GetDefaultJudgePromptAsync(CancellationToken cancellationToken = default)
        {
            Dictionary<string, string> response = await SendAsync<Dictionary<string, string>>(HttpMethod.Get, "/v1.0/eval/judge-prompt/default", cancellationToken: cancellationToken).ConfigureAwait(false);
            if (response != null && response.TryGetValue("Prompt", out string prompt) && !String.IsNullOrEmpty(prompt))
                return prompt;

            return String.Empty;
        }

        #endregion

        #region Crawl-Plans

        /// <summary>
        /// List crawl plans.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing crawl plans.</returns>
        public async Task<EnumerationResult<CrawlPlan>> ListCrawlPlansAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<CrawlPlan>>(HttpMethod.Get, "/v1.0/crawlplans", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a crawl plan by identifier.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The crawl plan.</returns>
        public async Task<CrawlPlan> GetCrawlPlanAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await SendAsync<CrawlPlan>(HttpMethod.Get, "/v1.0/crawlplans/" + planId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new crawl plan.
        /// </summary>
        /// <param name="plan">Crawl plan to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created crawl plan.</returns>
        public async Task<CrawlPlan> CreateCrawlPlanAsync(CrawlPlan plan, CancellationToken cancellationToken = default)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            return await SendAsync<CrawlPlan>(HttpMethod.Put, "/v1.0/crawlplans", plan, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing crawl plan.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="plan">Updated crawl plan data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated crawl plan.</returns>
        public async Task<CrawlPlan> UpdateCrawlPlanAsync(string planId, CrawlPlan plan, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            return await SendAsync<CrawlPlan>(HttpMethod.Put, "/v1.0/crawlplans/" + planId, plan, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a crawl plan.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteCrawlPlanAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            await SendAsync(HttpMethod.Delete, "/v1.0/crawlplans/" + planId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Start a crawl for the specified plan.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task StartCrawlAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            await SendAsync(HttpMethod.Post, "/v1.0/crawlplans/" + planId + "/start", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Stop a running crawl for the specified plan.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task StopCrawlAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            await SendAsync(HttpMethod.Post, "/v1.0/crawlplans/" + planId + "/stop", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Crawl-Operations

        /// <summary>
        /// List crawl operations for a plan.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing crawl operations.</returns>
        public async Task<EnumerationResult<CrawlOperation>> ListCrawlOperationsAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await SendAsync<EnumerationResult<CrawlOperation>>(HttpMethod.Get, "/v1.0/crawlplans/" + planId + "/operations", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a crawl operation by identifier.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="operationId">Crawl operation identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The crawl operation.</returns>
        public async Task<CrawlOperation> GetCrawlOperationAsync(string planId, string operationId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));
            if (String.IsNullOrWhiteSpace(operationId))
                throw new ArgumentNullException(nameof(operationId));

            return await SendAsync<CrawlOperation>(HttpMethod.Get, "/v1.0/crawlplans/" + planId + "/operations/" + operationId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a crawl operation.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="operationId">Crawl operation identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteCrawlOperationAsync(string planId, string operationId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));
            if (String.IsNullOrWhiteSpace(operationId))
                throw new ArgumentNullException(nameof(operationId));

            await SendAsync(HttpMethod.Delete, "/v1.0/crawlplans/" + planId + "/operations/" + operationId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get statistics for all operations under a crawl plan.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Statistics as a JSON element.</returns>
        public async Task<JsonElement> GetCrawlStatisticsAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/crawlplans/" + planId + "/operations/statistics", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get statistics for a specific crawl operation.
        /// </summary>
        /// <param name="planId">Crawl plan identifier.</param>
        /// <param name="operationId">Crawl operation identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Statistics as a JSON element.</returns>
        public async Task<JsonElement> GetCrawlOperationStatisticsAsync(string planId, string operationId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));
            if (String.IsNullOrWhiteSpace(operationId))
                throw new ArgumentNullException(nameof(operationId));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/crawlplans/" + planId + "/operations/" + operationId + "/statistics", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Get the current server configuration.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Configuration as a JSON element.</returns>
        public async Task<JsonElement> GetConfigAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/configuration", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update the server configuration.
        /// </summary>
        /// <param name="configuration">Full configuration object to apply.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated configuration as a JSON element.</returns>
        public async Task<JsonElement> UpdateConfigAsync(JsonElement configuration, CancellationToken cancellationToken = default)
        {
            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/configuration", configuration, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Health

        /// <summary>
        /// Check server health by requesting the root endpoint.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the server is healthy.</returns>
        public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, BaseUrl + "/"))
            {
                using (HttpResponseMessage response = await SendRawAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    return response.IsSuccessStatusCode;
                }
            }
        }

        /// <summary>
        /// Get the identity of the authenticated user.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>User identity as a JSON element.</returns>
        public async Task<JsonElement> WhoAmIAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/whoami", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Tenants

        /// <summary>
        /// List all tenants.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing tenants.</returns>
        public async Task<EnumerationResult<TenantMetadata>> ListTenantsAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<TenantMetadata>>(HttpMethod.Get, "/v1.0/tenants", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a tenant by identifier.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The tenant.</returns>
        public async Task<TenantMetadata> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            return await SendAsync<TenantMetadata>(HttpMethod.Get, "/v1.0/tenants/" + tenantId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new tenant.
        /// </summary>
        /// <param name="tenant">Tenant to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Composite response containing Tenant and Provisioning data.</returns>
        public async Task<JsonElement> CreateTenantAsync(TenantMetadata tenant, CancellationToken cancellationToken = default)
        {
            if (tenant == null)
                throw new ArgumentNullException(nameof(tenant));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/tenants", tenant, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="tenant">Updated tenant data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated tenant.</returns>
        public async Task<TenantMetadata> UpdateTenantAsync(string tenantId, TenantMetadata tenant, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (tenant == null)
                throw new ArgumentNullException(nameof(tenant));

            return await SendAsync<TenantMetadata>(HttpMethod.Put, "/v1.0/tenants/" + tenantId, tenant, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            await SendAsync(HttpMethod.Delete, "/v1.0/tenants/" + tenantId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Users

        /// <summary>
        /// List users for a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing users.</returns>
        public async Task<EnumerationResult<UserMaster>> ListUsersAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            return await SendAsync<EnumerationResult<UserMaster>>(HttpMethod.Get, "/v1.0/tenants/" + tenantId + "/users", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a user by identifier.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user.</returns>
        public async Task<UserMaster> GetUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            return await SendAsync<UserMaster>(HttpMethod.Get, "/v1.0/tenants/" + tenantId + "/users/" + userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new user for a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="user">User to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created user.</returns>
        public async Task<UserMaster> CreateUserAsync(string tenantId, UserMaster user, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return await SendAsync<UserMaster>(HttpMethod.Put, "/v1.0/tenants/" + tenantId + "/users", user, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing user.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="user">Updated user data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated user.</returns>
        public async Task<UserMaster> UpdateUserAsync(string tenantId, string userId, UserMaster user, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return await SendAsync<UserMaster>(HttpMethod.Put, "/v1.0/tenants/" + tenantId + "/users/" + userId, user, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a user.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            await SendAsync(HttpMethod.Delete, "/v1.0/tenants/" + tenantId + "/users/" + userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Credentials

        /// <summary>
        /// List credentials for a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Enumeration result containing credentials.</returns>
        public async Task<EnumerationResult<Credential>> ListCredentialsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            return await SendAsync<EnumerationResult<Credential>>(HttpMethod.Get, "/v1.0/tenants/" + tenantId + "/credentials", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a credential by identifier.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="credentialId">Credential identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The credential.</returns>
        public async Task<Credential> GetCredentialAsync(string tenantId, string credentialId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(credentialId))
                throw new ArgumentNullException(nameof(credentialId));

            return await SendAsync<Credential>(HttpMethod.Get, "/v1.0/tenants/" + tenantId + "/credentials/" + credentialId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new credential for a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="credential">Credential to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created credential.</returns>
        public async Task<Credential> CreateCredentialAsync(string tenantId, Credential credential, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (credential == null)
                throw new ArgumentNullException(nameof(credential));

            return await SendAsync<Credential>(HttpMethod.Put, "/v1.0/tenants/" + tenantId + "/credentials", credential, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an existing credential.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="credentialId">Credential identifier.</param>
        /// <param name="credential">Updated credential data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated credential.</returns>
        public async Task<Credential> UpdateCredentialAsync(string tenantId, string credentialId, Credential credential, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(credentialId))
                throw new ArgumentNullException(nameof(credentialId));
            if (credential == null)
                throw new ArgumentNullException(nameof(credential));

            return await SendAsync<Credential>(HttpMethod.Put, "/v1.0/tenants/" + tenantId + "/credentials/" + credentialId, credential, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a credential.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="credentialId">Credential identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteCredentialAsync(string tenantId, string credentialId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(credentialId))
                throw new ArgumentNullException(nameof(credentialId));

            await SendAsync(HttpMethod.Delete, "/v1.0/tenants/" + tenantId + "/credentials/" + credentialId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Search

        /// <summary>
        /// Search documents via RAG retrieval through the chat endpoint.
        /// Sends a single user message to the assistant and returns the full response
        /// including retrieval results and citations.
        /// </summary>
        /// <param name="assistantId">Assistant identifier to search through.</param>
        /// <param name="query">Search query text.</param>
        /// <param name="threadId">Optional thread identifier for conversation continuity.</param>
        /// <param name="maxTokens">Optional maximum tokens for generation.</param>
        /// <param name="temperature">Optional temperature for generation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Chat completion response containing retrieval results and citations.</returns>
        public async Task<ChatCompletionResponse> SearchAsync(
            string assistantId,
            string query,
            string threadId = null,
            int? maxTokens = null,
            double? temperature = null,
            CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(assistantId))
                throw new ArgumentNullException(nameof(assistantId));
            if (String.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            ChatCompletionRequest request = new ChatCompletionRequest
            {
                Messages = new List<ChatCompletionMessage>
                {
                    new ChatCompletionMessage { Role = "user", Content = query }
                }
            };

            if (maxTokens.HasValue)
                request.MaxTokens = maxTokens.Value;
            if (temperature.HasValue)
                request.Temperature = temperature.Value;

            return await SendMessageAsync(assistantId, request, threadId, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
