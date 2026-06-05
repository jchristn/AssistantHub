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
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    /// <summary>
    /// Provides shared document ingestion pipeline helpers.
    /// </summary>
    public abstract class IngestionServiceBase
    {
        #region Private-Members

        private protected string _Header = "[IngestionService] ";
        private protected DatabaseDriverBase _Database = null;
        private protected StorageService _Storage = null;
        private protected DocumentAtomSettings _DocumentAtomSettings = null;
        private protected ChunkingSettings _ChunkingSettings = null;
        private protected RecallDbSettings _RecallDbSettings = null;
        private protected LoggingModule _Logging = null;
        private protected ProcessingLogService _ProcessingLog = null;
        private protected HttpClient _HttpClient = null;

        private protected JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        #endregion

        /// <summary>
        /// Instantiate the ingestion service helper base.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="storage">Storage service.</param>
        /// <param name="documentAtomSettings">DocumentAtom service settings.</param>
        /// <param name="chunkingSettings">Chunking service settings.</param>
        /// <param name="recallDbSettings">RecallDb service settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="processingLog">Optional processing log service.</param>
        protected IngestionServiceBase(
            DatabaseDriverBase database,
            StorageService storage,
            DocumentAtomSettings documentAtomSettings,
            ChunkingSettings chunkingSettings,
            RecallDbSettings recallDbSettings,
            LoggingModule logging,
            ProcessingLogService processingLog = null)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _DocumentAtomSettings = documentAtomSettings ?? throw new ArgumentNullException(nameof(documentAtomSettings));
            _ChunkingSettings = chunkingSettings ?? throw new ArgumentNullException(nameof(chunkingSettings));
            _RecallDbSettings = recallDbSettings ?? throw new ArgumentNullException(nameof(recallDbSettings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _ProcessingLog = processingLog;
            _HttpClient = new HttpClient();
        }

        #region Private-Methods

        /// <summary>
        /// Update the status of a document in the database.
        /// </summary>
        /// <param name="documentId">Document identifier.</param>
        /// <param name="status">New status.</param>
        /// <param name="statusMessage">Status message.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        private protected async Task UpdateDocumentStatusAsync(string documentId, DocumentStatusEnum status, string statusMessage, CancellationToken token)
        {
            _Logging.Debug(_Header + "updating document " + documentId + " status to " + status.ToString());
            await _Database.AssistantDocument.UpdateStatusAsync(documentId, status, statusMessage, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Detect the type of a document using the DocumentAtom service.
        /// </summary>
        /// <param name="documentId">Document identifier for logging.</param>
        /// <param name="fileBytes">Raw file bytes.</param>
        /// <param name="filename">Original filename.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Detected document type string.</returns>
        private protected async Task<TypeDetectResponse> DetectDocumentTypeAsync(string documentId, byte[] fileBytes, string filename, CancellationToken token)
        {
            string url = _DocumentAtomSettings.Endpoint.TrimEnd('/') + "/typedetect";

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new ByteArrayContent(fileBytes);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                if (!String.IsNullOrEmpty(_DocumentAtomSettings.AccessKey))
                {
                    request.Headers.Add("x-api-key", _DocumentAtomSettings.AccessKey);
                }

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "type detection returned " + (int)response.StatusCode + ": " + responseBody);
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "Type detection API returned HTTP " + (int)response.StatusCode + ": " + responseBody).ConfigureAwait(false);
                    return null;
                }

                TypeDetectResponse typeResult = JsonSerializer.Deserialize<TypeDetectResponse>(responseBody, _JsonOptions);

                _Logging.Debug(_Header + "type detection response for document " + documentId + ": " + typeResult?.MimeType);
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "DEBUG", "Type detection response: " + typeResult?.MimeType).ConfigureAwait(false);

                return typeResult;
            }
        }

        /// <summary>
        /// Process document content using the DocumentAtom service.
        /// </summary>
        /// <param name="documentId">Document identifier for logging.</param>
        /// <param name="fileBytes">Raw file bytes.</param>
        /// <param name="documentType">Detected document type.</param>
        /// <param name="filename">Original filename.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Extracted text content.</returns>
        private protected async Task<string> ProcessDocumentContentAsync(string documentId, byte[] fileBytes, string documentType, string filename, CancellationToken token)
        {
            string atomPath = GetAtomPath(documentType);
            if (String.IsNullOrEmpty(atomPath))
            {
                _Logging.Warn(_Header + "no atom endpoint for document type: " + documentType);
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "ERROR", "No atom endpoint for document type: " + documentType).ConfigureAwait(false);
                return null;
            }

            string url = _DocumentAtomSettings.Endpoint.TrimEnd('/') + atomPath;

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                // v3.0.0: Send JSON envelope with base64-encoded data
                object atomRequest = new
                {
                    Settings = (object)null,
                    Data = Convert.ToBase64String(fileBytes)
                };

                string requestJson = JsonSerializer.Serialize(atomRequest, _JsonOptions);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(_DocumentAtomSettings.AccessKey))
                {
                    request.Headers.Add("x-api-key", _DocumentAtomSettings.AccessKey);
                }

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "document processing returned " + (int)response.StatusCode + ": " + responseBody);
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "Atom extraction API returned HTTP " + (int)response.StatusCode + ": " + responseBody).ConfigureAwait(false);
                    return null;
                }

                _Logging.Debug(_Header + "atom extraction response for document " + documentId + ": " + responseBody.Length + " characters");
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "DEBUG", "Atom extraction response: " + responseBody.Length + " characters").ConfigureAwait(false);

                List<AtomResponse> atoms = JsonSerializer.Deserialize<List<AtomResponse>>(responseBody, _JsonOptions);
                if (atoms == null || atoms.Count == 0)
                    return null;

                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Atoms extracted: " + atoms.Count + " atom(s)").ConfigureAwait(false);

                StringBuilder sb = new StringBuilder();
                int atomIndex = 0;
                foreach (AtomResponse atom in atoms)
                {
                    atomIndex++;
                    if (!String.IsNullOrEmpty(atom.Text))
                    {
                        if (sb.Length > 0) sb.Append(Environment.NewLine);
                        sb.Append(atom.Text);
                        if (_ProcessingLog != null)
                            await _ProcessingLog.LogAsync(documentId, "DEBUG", "Atom [" + atomIndex + "/" + atoms.Count + "] - " + atom.Text.Length + " characters").ConfigureAwait(false);
                    }
                }

                return sb.Length > 0 ? sb.ToString() : null;
            }
        }

        /// <summary>
        /// Map a detected document type to the corresponding DocumentAtom atom endpoint path.
        /// </summary>
        private protected static string GetAtomPath(string documentType)
        {
            if (String.IsNullOrEmpty(documentType)) return null;

            switch (documentType.ToLowerInvariant())
            {
                case "csv": return "/atom/csv";
                case "xlsx":
                case "xls": return "/atom/excel";
                case "html": return "/atom/html";
                case "json": return "/atom/json";
                case "markdown": return "/atom/markdown";
                case "pdf": return "/atom/pdf";
                case "png":
                case "jpeg":
                case "gif":
                case "tiff":
                case "bmp":
                case "webp":
                case "ico": return "/atom/png";
                case "pptx":
                case "ppt": return "/atom/powerpoint";
                case "rtf": return "/atom/rtf";
                case "text":
                case "tsv": return "/atom/text";
                case "docx":
                case "doc": return "/atom/word";
                case "xml":
                case "svg":
                case "gpx": return "/atom/xml";
                default: return null;
            }
        }

        /// <summary>
        /// Chunk and embed document content using the Partio chunking service.
        /// </summary>
        /// <param name="documentId">Document identifier for logging.</param>
        /// <param name="content">Extracted text content.</param>
        /// <param name="rule">Optional ingestion rule with chunking/embedding config.</param>
        /// <param name="labels">Merged labels.</param>
        /// <param name="tags">Merged tags.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of chunks with their embeddings.</returns>
        private protected async Task<List<ChunkResult>> ChunkAndEmbedContentAsync(
            string documentId,
            string content,
            IngestionRule rule,
            List<string> labels,
            Dictionary<string, string> tags,
            CancellationToken token)
        {
            // When strategy is "None", skip Partio chunking entirely and produce a single chunk
            if (rule?.Chunking != null
                && !String.IsNullOrEmpty(rule.Chunking.Strategy)
                && rule.Chunking.Strategy.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Chunking strategy is None - skipping chunking, treating entire content as a single chunk").ConfigureAwait(false);

                // Compute embeddings for the single chunk via Partio /v1.0/embed
                List<float> embeddings = await ComputeEmbeddingAsync(documentId, content, rule, token).ConfigureAwait(false);

                ChunkResult singleChunk = new ChunkResult
                {
                    CellGUID = Guid.NewGuid(),
                    Text = content,
                    Labels = labels,
                    Tags = tags?.ToDictionary(kv => kv.Key, kv => kv.Value),
                    Embeddings = embeddings
                };

                return new List<ChunkResult> { singleChunk };
            }

            string url = _ChunkingSettings.Endpoint.TrimEnd('/') + "/v1.0/process";

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                // Build SemanticCellRequest body
                Dictionary<string, object> requestBody = new Dictionary<string, object>();
                requestBody["Type"] = "Text";
                requestBody["Text"] = content;

                // EmbeddingConfiguration with endpoint ID
                Dictionary<string, object> embedConfig = new Dictionary<string, object>();
                embedConfig["EmbeddingEndpointId"] = _ChunkingSettings.EndpointId;

                if (rule?.Embedding != null)
                {
                    if (!String.IsNullOrEmpty(rule.Embedding.EmbeddingEndpointId))
                        embedConfig["EmbeddingEndpointId"] = rule.Embedding.EmbeddingEndpointId;
                    embedConfig["L2Normalization"] = rule.Embedding.L2Normalization;
                }

                requestBody["EmbeddingConfiguration"] = embedConfig;

                // SummarizationConfiguration (optional)
                if (rule?.Summarization != null)
                {
                    Dictionary<string, object> sumConfig = new Dictionary<string, object>();
                    sumConfig["CompletionEndpointId"] = rule.Summarization.CompletionEndpointId;
                    sumConfig["Order"] = rule.Summarization.Order.ToString();
                    sumConfig["MaxSummaryTokens"] = rule.Summarization.MaxSummaryTokens;
                    sumConfig["MinCellLength"] = rule.Summarization.MinCellLength;

                    int summarizationMaxParallelTasks = rule.Summarization.MaxParallelTasks;
                    if (!String.IsNullOrWhiteSpace(rule.Summarization.CompletionEndpointId))
                    {
                        int completionMaxConcurrent = await ResolveEndpointMaxConcurrencyAsync("completion", rule.Summarization.CompletionEndpointId, token).ConfigureAwait(false);
                        if (summarizationMaxParallelTasks > completionMaxConcurrent)
                        {
                            if (_ProcessingLog != null)
                                await _ProcessingLog.LogAsync(documentId, "INFO", "Summarization MaxParallelTasks clamped from " + summarizationMaxParallelTasks + " to completion endpoint maxConcurrentRequests " + completionMaxConcurrent).ConfigureAwait(false);
                            summarizationMaxParallelTasks = completionMaxConcurrent;
                        }
                    }
                    sumConfig["MaxParallelTasks"] = summarizationMaxParallelTasks;

                    sumConfig["MaxRetriesPerSummary"] = rule.Summarization.MaxRetriesPerSummary;
                    sumConfig["MaxRetries"] = rule.Summarization.MaxRetries;
                    sumConfig["TimeoutMs"] = rule.Summarization.TimeoutMs;
                    if (!String.IsNullOrEmpty(rule.Summarization.SummarizationPrompt))
                        sumConfig["SummarizationPrompt"] = rule.Summarization.SummarizationPrompt;
                    requestBody["SummarizationConfiguration"] = sumConfig;
                }

                if (rule?.Chunking != null)
                {
                    Dictionary<string, object> chunkConfig = new Dictionary<string, object>();
                    chunkConfig["Strategy"] = rule.Chunking.Strategy ?? "FixedTokenCount";
                    chunkConfig["FixedTokenCount"] = rule.Chunking.FixedTokenCount;
                    chunkConfig["OverlapCount"] = rule.Chunking.OverlapCount;
                    if (rule.Chunking.OverlapPercentage.HasValue)
                        chunkConfig["OverlapPercentage"] = rule.Chunking.OverlapPercentage.Value;
                    if (!String.IsNullOrEmpty(rule.Chunking.OverlapStrategy))
                        chunkConfig["OverlapStrategy"] = rule.Chunking.OverlapStrategy;
                    chunkConfig["RowGroupSize"] = rule.Chunking.RowGroupSize;
                    if (!String.IsNullOrEmpty(rule.Chunking.ContextPrefix))
                        chunkConfig["ContextPrefix"] = rule.Chunking.ContextPrefix;
                    if (!String.IsNullOrEmpty(rule.Chunking.RegexPattern))
                        chunkConfig["RegexPattern"] = rule.Chunking.RegexPattern;
                    requestBody["ChunkingConfiguration"] = chunkConfig;
                }

                if (labels != null && labels.Count > 0)
                    requestBody["Labels"] = labels;

                if (tags != null && tags.Count > 0)
                    requestBody["Tags"] = tags;

                string embEndpointId = embedConfig.ContainsKey("EmbeddingEndpointId") ? embedConfig["EmbeddingEndpointId"]?.ToString() : "(unknown)";
                bool summarizationEnabled = requestBody.ContainsKey("SummarizationConfiguration");
                string chunkStrategy = (rule?.Chunking?.Strategy) ?? "default";

                string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "INFO",
                        "Partio process request: contentLength=" + content.Length + " chars"
                        + ", embeddingEndpoint=" + embEndpointId
                        + ", summarization=" + (summarizationEnabled ? "enabled" : "disabled")
                        + ", chunkingStrategy=" + chunkStrategy
                        + ", requestBodyLength=" + json.Length + " chars").ConfigureAwait(false);

                if (!String.IsNullOrEmpty(_ChunkingSettings.AccessKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _ChunkingSettings.AccessKey);
                }

                Stopwatch apiSw = Stopwatch.StartNew();
                string completionEndpointId = rule?.Summarization?.CompletionEndpointId;
                using (IDisposable limiterLease = await AcquireEndpointLimitersAsync(
                    documentId,
                    new List<EndpointLimiterTarget>
                    {
                        new EndpointLimiterTarget("embedding", embEndpointId),
                        !String.IsNullOrWhiteSpace(completionEndpointId) ? new EndpointLimiterTarget("completion", completionEndpointId) : null
                    },
                    token).ConfigureAwait(false))
                {
                    HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                    string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    apiSw.Stop();

                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(_Header + "processing service returned " + (int)response.StatusCode + " in " + apiSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms: " + responseBody);
                        if (_ProcessingLog != null)
                        {
                            await _ProcessingLog.LogAsync(documentId, "ERROR",
                                    "Step: Chunking/Embedding/Summarization via Partio - HTTP " + (int)response.StatusCode + " in " + apiSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms").ConfigureAwait(false);
                            await _ProcessingLog.LogAsync(documentId, "ERROR",
                                "Source content: " + content.Length + " chars, excerpt: " + Excerpt(content)).ConfigureAwait(false);
                            await _ProcessingLog.LogAsync(documentId, "ERROR",
                                "Request config: embeddingEndpoint=" + embEndpointId
                                + ", summarization=" + (summarizationEnabled ? "enabled" : "disabled")
                                + ", strategy=" + chunkStrategy).ConfigureAwait(false);
                            await _ProcessingLog.LogAsync(documentId, "ERROR",
                                "Response body: " + responseBody).ConfigureAwait(false);
                        }
                        return null;
                    }

                    SemanticCellResponse cellResult = JsonSerializer.Deserialize<SemanticCellResponse>(responseBody, _JsonOptions);
                    if (cellResult == null)
                    {
                        if (_ProcessingLog != null)
                        {
                            await _ProcessingLog.LogAsync(documentId, "ERROR",
                                "Partio returned HTTP 200 but response could not be deserialized").ConfigureAwait(false);
                            await _ProcessingLog.LogAsync(documentId, "ERROR",
                                "Response body: " + responseBody).ConfigureAwait(false);
                        }
                        return null;
                    }
                    return FlattenChunks(cellResult);
                }
            }
        }

        /// <summary>
        /// Recursively flatten all chunks from a SemanticCellResponse hierarchy.
        /// </summary>
        private protected List<ChunkResult> FlattenChunks(SemanticCellResponse cell)
        {
            List<ChunkResult> all = new List<ChunkResult>();
            if (cell.Chunks != null)
                all.AddRange(cell.Chunks);
            if (cell.Children != null)
            {
                foreach (SemanticCellResponse child in cell.Children)
                    all.AddRange(FlattenChunks(child));
            }
            return all;
        }

        /// <summary>
        /// Compute embedding vector for a single piece of text using the Partio embedding endpoint.
        /// Used when chunking strategy is "None" to embed the whole document as a single chunk.
        /// </summary>
        private protected async Task<List<float>> ComputeEmbeddingAsync(string documentId, string text, IngestionRule rule, CancellationToken token)
        {
            string url = _ChunkingSettings.Endpoint.TrimEnd('/') + "/v1.0/embed";

            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    Dictionary<string, object> requestBody = new Dictionary<string, object>();
                    requestBody["Text"] = text;

                    string endpointId = _ChunkingSettings.EndpointId;
                    if (rule?.Embedding != null && !String.IsNullOrEmpty(rule.Embedding.EmbeddingEndpointId))
                        endpointId = rule.Embedding.EmbeddingEndpointId;
                    requestBody["EmbeddingEndpointId"] = endpointId;

                    if (rule?.Embedding != null)
                        requestBody["L2Normalization"] = rule.Embedding.L2Normalization;

                    string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    if (!String.IsNullOrEmpty(_ChunkingSettings.AccessKey))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _ChunkingSettings.AccessKey);

                    using (IDisposable limiterLease = await AcquireEndpointLimitersAsync(
                        documentId,
                        new List<EndpointLimiterTarget> { new EndpointLimiterTarget("embedding", endpointId) },
                        token).ConfigureAwait(false))
                    {
                        HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                        string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode)
                        {
                            _Logging.Warn(_Header + "embedding service returned " + (int)response.StatusCode + ": " + responseBody);
                            if (_ProcessingLog != null)
                            {
                                await _ProcessingLog.LogAsync(documentId, "ERROR",
                                    "Step: Single-chunk embedding via Partio /v1.0/embed - HTTP " + (int)response.StatusCode).ConfigureAwait(false);
                                await _ProcessingLog.LogAsync(documentId, "ERROR",
                                    "Source content: " + text.Length + " chars, excerpt: " + Excerpt(text)).ConfigureAwait(false);
                                await _ProcessingLog.LogAsync(documentId, "ERROR",
                                    "Response body: " + responseBody).ConfigureAwait(false);
                            }
                            return null;
                        }

                        PartioEmbedResponse embedResponse = JsonSerializer.Deserialize<PartioEmbedResponse>(responseBody);
                        return embedResponse?.Embeddings;
                    }
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception computing embedding: " + e.Message);
                if (_ProcessingLog != null)
                {
                    await _ProcessingLog.LogAsync(documentId, "ERROR",
                        "Step: Single-chunk embedding - exception: " + e.Message).ConfigureAwait(false);
                    await _ProcessingLog.LogAsync(documentId, "ERROR",
                        "Source content: " + text.Length + " chars").ConfigureAwait(false);
                }
                return null;
            }
        }

        /// <summary>
        /// Check that a RecallDB collection exists via HEAD.
        /// Collections must be created via the dashboard/RecallDB before ingestion.
        /// </summary>
        private protected async Task<bool> EnsureCollectionExistsAsync(string tenantId, string collectionId, CancellationToken token)
        {
            string url = _RecallDbSettings.Endpoint.TrimEnd('/') + "/v1.0/tenants/" + tenantId + "/collections/" + collectionId;

            try
            {
                using (HttpRequestMessage headReq = new HttpRequestMessage(HttpMethod.Head, url))
                {
                    if (!String.IsNullOrEmpty(_RecallDbSettings.AccessKey))
                        headReq.Headers.Add("Authorization", "Bearer " + _RecallDbSettings.AccessKey);

                    HttpResponseMessage headResp = await _HttpClient.SendAsync(headReq, token).ConfigureAwait(false);
                    if (headResp.IsSuccessStatusCode)
                        return true;

                    _Logging.Warn(_Header + "collection " + collectionId + " not found in RecallDB (HTTP " + (int)headResp.StatusCode + ")");
                    return false;
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception checking collection " + collectionId + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Store a single chunk embedding in RecallDB.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="documentId">Source document identifier.</param>
        /// <param name="chunk">Chunk with embedding data.</param>
        /// <param name="chunkIndex">Zero-based chunk position within the document.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Document key if stored successfully, null otherwise.</returns>
        private protected async Task<string> StoreEmbeddingAsync(string tenantId, string collectionId, string documentId, ChunkResult chunk, int chunkIndex, CancellationToken token)
        {
            string url = _RecallDbSettings.Endpoint.TrimEnd('/') + "/v1.0/tenants/" + tenantId + "/collections/" + collectionId + "/documents";

            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url))
                {
                    Dictionary<string, object> requestBody = new Dictionary<string, object>
                    {
                        { "Content", chunk.Text },
                        { "Embeddings", chunk.Embeddings },
                        { "DocumentId", documentId },
                        { "Position", chunkIndex },
                        { "ContentType", "Text" }
                    };

                    if (chunk.Labels != null && chunk.Labels.Count > 0)
                        requestBody["Labels"] = chunk.Labels;

                    if (chunk.Tags != null && chunk.Tags.Count > 0)
                        requestBody["Tags"] = chunk.Tags;

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
                        _Logging.Warn(_Header + "RecallDB store returned " + (int)response.StatusCode + ": " + responseBody);
                        if (_ProcessingLog != null)
                        {
                            await _ProcessingLog.LogAsync(documentId, "ERROR",
                                "RecallDB store API returned HTTP " + (int)response.StatusCode + " for chunk " + chunkIndex + ": " + responseBody).ConfigureAwait(false);
                            await _ProcessingLog.LogAsync(documentId, "ERROR",
                                "Chunk text: " + (chunk.Text?.Length ?? 0) + " chars, embeddings: " + (chunk.Embeddings?.Count ?? 0) + " dimensions").ConfigureAwait(false);
                        }
                        return null;
                    }

                    // Parse response to extract the document key
                    try
                    {
                        RecallDbStoreResponse storeResult = JsonSerializer.Deserialize<RecallDbStoreResponse>(responseBody, _JsonOptions);
                        return storeResult?.DocumentKey;
                    }
                    catch
                    {
                        _Logging.Debug(_Header + "could not parse RecallDB store response");
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception storing embedding: " + e.Message);
                if (_ProcessingLog != null)
                {
                    await _ProcessingLog.LogAsync(documentId, "ERROR",
                        "RecallDB store exception for chunk " + chunkIndex + ": " + e.Message).ConfigureAwait(false);
                    await _ProcessingLog.LogAsync(documentId, "ERROR",
                        "Chunk text: " + (chunk.Text?.Length ?? 0) + " chars, embeddings: " + (chunk.Embeddings?.Count ?? 0) + " dimensions").ConfigureAwait(false);
                }
                return null;
            }
        }

        /// <summary>
        /// Store all chunk embeddings in RecallDB via the batch API in a single request.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="documentId">Document identifier.</param>
        /// <param name="chunks">List of chunks with embeddings.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of stored document keys.</returns>
        private protected async Task<List<string>> StoreEmbeddingBatchAsync(string tenantId, string collectionId, string documentId, List<ChunkResult> chunks, CancellationToken token)
        {
            string url = _RecallDbSettings.Endpoint.TrimEnd('/') + "/v1.0/tenants/" + tenantId + "/collections/" + collectionId + "/documents/batch";

            try
            {
                List<Dictionary<string, object>> documents = new List<Dictionary<string, object>>();
                for (int i = 0; i < chunks.Count; i++)
                {
                    ChunkResult chunk = chunks[i];
                    Dictionary<string, object> doc = new Dictionary<string, object>
                    {
                        { "Content", chunk.Text },
                        { "Embeddings", chunk.Embeddings },
                        { "DocumentId", documentId },
                        { "Position", i },
                        { "ContentType", "Text" }
                    };

                    if (chunk.Labels != null && chunk.Labels.Count > 0)
                        doc["Labels"] = chunk.Labels;

                    if (chunk.Tags != null && chunk.Tags.Count > 0)
                        doc["Tags"] = chunk.Tags;

                    documents.Add(doc);
                }

                string json = JsonSerializer.Serialize(documents, _JsonOptions);

                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    if (!String.IsNullOrEmpty(_RecallDbSettings.AccessKey))
                    {
                        request.Headers.Add("Authorization", "Bearer " + _RecallDbSettings.AccessKey);
                    }

                    HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                    string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(_Header + "RecallDB batch store returned " + (int)response.StatusCode + " for " + chunks.Count + " chunks: " + responseBody);
                        if (_ProcessingLog != null)
                        {
                            await _ProcessingLog.LogAsync(documentId, "ERROR",
                                "RecallDB batch store API returned HTTP " + (int)response.StatusCode + " for " + chunks.Count + " chunks: " + responseBody).ConfigureAwait(false);
                        }
                        return new List<string>();
                    }

                    try
                    {
                        List<RecallDbStoreResponse> results = JsonSerializer.Deserialize<List<RecallDbStoreResponse>>(responseBody, _JsonOptions);
                        List<string> recordIds = new List<string>();
                        if (results != null)
                        {
                            foreach (RecallDbStoreResponse result in results)
                            {
                                if (!String.IsNullOrEmpty(result?.DocumentKey))
                                    recordIds.Add(result.DocumentKey);
                            }
                        }
                        return recordIds;
                    }
                    catch
                    {
                        _Logging.Debug(_Header + "could not parse RecallDB batch store response");
                        return new List<string>();
                    }
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during batch embedding storage: " + e.Message);
                if (_ProcessingLog != null)
                {
                    await _ProcessingLog.LogAsync(documentId, "ERROR",
                        "RecallDB batch store exception for " + chunks.Count + " chunks: " + e.Message).ConfigureAwait(false);
                }
                return new List<string>();
            }
        }

        /// <summary>
        /// Truncate text to a maximum length for logging, appending total length if truncated.
        /// </summary>
        private protected string Excerpt(string text, int maxLength = 500)
        {
            if (String.IsNullOrEmpty(text)) return "(empty)";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength) + "... (" + text.Length + " chars total)";
        }

        /// <summary>
        /// Resolve endpoint details (model, URL) from Partio for logging.
        /// </summary>
        /// <param name="endpointType">"completion" or "embedding".</param>
        /// <param name="endpointId">Endpoint identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Human-readable endpoint info string, or null on failure.</returns>
        private protected async Task<string> ResolveEndpointInfoAsync(string endpointType, string endpointId, CancellationToken token)
        {
            if (String.IsNullOrEmpty(endpointId)) return null;

            try
            {
                string url = _ChunkingSettings.Endpoint.TrimEnd('/') + "/v1.0/endpoints/" + endpointType + "/" + endpointId;

                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    if (!String.IsNullOrEmpty(_ChunkingSettings.AccessKey))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _ChunkingSettings.AccessKey);

                    HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                        return null;

                    string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    PartioEndpointConfig endpointConfig = JsonSerializer.Deserialize<PartioEndpointConfig>(responseBody);

                    List<string> parts = new List<string>();
                    if (!String.IsNullOrEmpty(endpointConfig?.Name)) parts.Add("name: " + endpointConfig.Name);
                    if (!String.IsNullOrEmpty(endpointConfig?.Model)) parts.Add("model: " + endpointConfig.Model);
                    if (!String.IsNullOrEmpty(endpointConfig?.Endpoint)) parts.Add("url: " + endpointConfig.Endpoint);
                    return parts.Count > 0 ? String.Join(", ", parts) : null;
                }
            }
            catch (Exception e)
            {
                _Logging.Debug(_Header + "could not resolve " + endpointType + " endpoint " + endpointId + ": " + e.Message);
                return null;
            }
        }

        private protected async Task<IDisposable> AcquireEndpointLimitersAsync(string documentId, List<EndpointLimiterTarget> targets, CancellationToken token)
        {
            List<EndpointLimiterTarget> normalized = targets?
                .Where(t => t != null && !String.IsNullOrWhiteSpace(t.EndpointType) && !String.IsNullOrWhiteSpace(t.EndpointId))
                .GroupBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<EndpointLimiterTarget>();

            List<IDisposable> leases = new List<IDisposable>();

            try
            {
                foreach (EndpointLimiterTarget target in normalized)
                {
                    int maxConcurrent = await ResolveEndpointMaxConcurrencyAsync(target.EndpointType, target.EndpointId, token).ConfigureAwait(false);
                    Stopwatch waitSw = Stopwatch.StartNew();
                    IDisposable lease = await EndpointConcurrencyLimiter.AcquireAsync(target.Key, maxConcurrent, token).ConfigureAwait(false);
                    waitSw.Stop();
                    leases.Add(lease);

                    if (_ProcessingLog != null)
                    {
                        string message = "Endpoint concurrency slot acquired: " + target.Key + ", maxConcurrentRequests=" + maxConcurrent;
                        if (waitSw.ElapsedMilliseconds > 0)
                            message += ", waitedMs=" + waitSw.ElapsedMilliseconds;
                        await _ProcessingLog.LogAsync(documentId, "INFO", message).ConfigureAwait(false);
                    }
                }

                return EndpointConcurrencyLimiter.CreateCompositeLease(leases);
            }
            catch
            {
                for (int i = leases.Count - 1; i >= 0; i--)
                    leases[i].Dispose();
                throw;
            }
        }

        private protected async Task<int> ResolveEndpointMaxConcurrencyAsync(string endpointType, string endpointId, CancellationToken token)
        {
            try
            {
                string url = _ChunkingSettings.Endpoint.TrimEnd('/') + "/v1.0/endpoints/" + endpointType + "/" + endpointId;

                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    if (!String.IsNullOrEmpty(_ChunkingSettings.AccessKey))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _ChunkingSettings.AccessKey);

                    HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(_Header + "could not resolve max concurrency for " + endpointType + " endpoint " + endpointId + ": HTTP " + (int)response.StatusCode);
                        return 1;
                    }

                    string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    PartioEndpointConfig endpointConfig = JsonSerializer.Deserialize<PartioEndpointConfig>(responseBody, _JsonOptions);
                    return Math.Max(1, endpointConfig?.MaxConcurrentRequests ?? 1);
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception resolving max concurrency for " + endpointType + " endpoint " + endpointId + ": " + e.Message);
                return 1;
            }
        }

        /// <summary>
        /// Merge labels from ingestion rule and document.
        /// Document labels are appended to rule labels.
        /// </summary>
        private protected List<string> MergeLabels(IngestionRule rule, AssistantDocument document)
        {
            List<string> merged = new List<string>();

            if (rule?.Labels != null)
                merged.AddRange(rule.Labels);

            if (!String.IsNullOrEmpty(document.Labels))
            {
                try
                {
                    List<string> docLabels = JsonSerializer.Deserialize<List<string>>(document.Labels, _JsonOptions);
                    if (docLabels != null)
                        merged.AddRange(docLabels);
                }
                catch { }
            }

            return merged.Count > 0 ? merged : null;
        }

        /// <summary>
        /// Merge tags from ingestion rule and document.
        /// Document tags override rule tags for the same key.
        /// </summary>
        private protected Dictionary<string, string> MergeTags(IngestionRule rule, AssistantDocument document)
        {
            Dictionary<string, string> merged = new Dictionary<string, string>();

            if (rule?.Tags != null)
            {
                foreach (KeyValuePair<string, string> kvp in rule.Tags)
                    merged[kvp.Key] = kvp.Value;
            }

            if (!String.IsNullOrEmpty(document.Tags))
            {
                try
                {
                    Dictionary<string, string> docTags = JsonSerializer.Deserialize<Dictionary<string, string>>(document.Tags, _JsonOptions);
                    if (docTags != null)
                    {
                        foreach (KeyValuePair<string, string> kvp in docTags)
                            merged[kvp.Key] = kvp.Value;
                    }
                }
                catch { }
            }

            return merged.Count > 0 ? merged : null;
        }

        #endregion

        #region Private-Classes

        #endregion
    }
}
