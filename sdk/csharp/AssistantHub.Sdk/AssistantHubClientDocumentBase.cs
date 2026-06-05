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
    /// Adds document, ingestion rule, inference, and eval APIs to the SDK client.
    /// </summary>
    public abstract class AssistantHubClientDocumentBase : AssistantHubClientEndpointBase
    {

        private protected AssistantHubClientDocumentBase(string baseUrl, string apiKey = null)
            : base(baseUrl, apiKey)
        {
        }

        private protected AssistantHubClientDocumentBase(string baseUrl, HttpClient httpClient, string apiKey = null)
            : base(baseUrl, httpClient, apiKey)
        {
        }

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
    }
}