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
        private protected IObjectStorageService _Storage = null;
        private protected DocumentAtomSettings _DocumentAtomSettings = null;
        private protected IAtomizationService _Atomization = null;
        private protected ChunkingSettings _ChunkingSettings = null;
        private protected IChunkingService _ChunkingService = null;
        private protected RecallDbSettings _RecallDbSettings = null;
        private protected IVectorStoreService _VectorStore = null;
        private protected VerbexSettings _VerbexSettings = null;
        private protected IInvertedIndexService _InvertedIndex = null;
        private protected SemaphoreSlim _VerbexIndexingSemaphore = null;
        private protected LoggingModule _Logging = null;
        private protected ProcessingLogService _ProcessingLog = null;
        private protected HttpClient _HttpClient = null;
        private const string DefaultAssistantDocumentName = "Untitled Document";

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
        /// <param name="verbexSettings">Verbex service settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="processingLog">Optional processing log service.</param>
        protected IngestionServiceBase(
            DatabaseDriverBase database,
            IObjectStorageService storage,
            DocumentAtomSettings documentAtomSettings,
            ChunkingSettings chunkingSettings,
            RecallDbSettings recallDbSettings,
            VerbexSettings verbexSettings,
            LoggingModule logging,
            ProcessingLogService processingLog = null)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _DocumentAtomSettings = documentAtomSettings ?? throw new ArgumentNullException(nameof(documentAtomSettings));
            _Atomization = new DocumentAtomAtomizationService(_DocumentAtomSettings, logging, processingLog);
            _ChunkingSettings = chunkingSettings ?? throw new ArgumentNullException(nameof(chunkingSettings));
            _ChunkingService = new PartioChunkingService(_ChunkingSettings, logging);
            _RecallDbSettings = recallDbSettings ?? throw new ArgumentNullException(nameof(recallDbSettings));
            _VectorStore = new RecallDbVectorStoreService(_RecallDbSettings, logging);
            _VerbexSettings = verbexSettings ?? throw new ArgumentNullException(nameof(verbexSettings));
            _InvertedIndex = new VerbexInvertedIndexService(_VerbexSettings, logging);
            _VerbexIndexingSemaphore = new SemaphoreSlim(_VerbexSettings.MaxConcurrentIndexingRequests, _VerbexSettings.MaxConcurrentIndexingRequests);
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _ProcessingLog = processingLog;
            _HttpClient = new HttpClient();
        }

        /// <summary>
        /// Normalize extracted text before full-text indexing without collapsing meaningful interior whitespace.
        /// </summary>
        /// <param name="content">Extracted text content.</param>
        /// <returns>Normalized text content.</returns>
        public static string NormalizeTextForIndexing(string content)
        {
            if (content == null) return null;

            string normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalized.Split('\n');
            StringBuilder sb = new StringBuilder(normalized.Length);

            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(lines[i].TrimEnd(' ', '\t'));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Apply the optional Verbex indexing content limit after normalization.
        /// </summary>
        /// <param name="content">Normalized text content.</param>
        /// <param name="maxContentCharacters">Maximum content characters, or zero for unlimited.</param>
        /// <returns>Limited text content.</returns>
        public static string ApplyVerbexContentLimit(string content, int maxContentCharacters)
        {
            if (content == null) return null;
            if (maxContentCharacters <= 0 || content.Length <= maxContentCharacters) return content;
            return content.Substring(0, maxContentCharacters);
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
            return await _Atomization.DetectDocumentTypeAsync(documentId, fileBytes, filename, token).ConfigureAwait(false);
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
            return await _Atomization.ExtractTextAsync(documentId, fileBytes, documentType, filename, token).ConfigureAwait(false);
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

                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "INFO",
                        "Partio process request: contentLength=" + content.Length + " chars"
                        + ", embeddingEndpoint=" + embEndpointId
                        + ", summarization=" + (summarizationEnabled ? "enabled" : "disabled")
                        + ", chunkingStrategy=" + chunkStrategy
                        + ", requestBodyLength=" + json.Length + " chars").ConfigureAwait(false);

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
                    using (HttpResponseMessage response = await _ChunkingService.SendAsync(HttpMethod.Post, "/v1.0/process", json, token).ConfigureAwait(false))
                    {
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
            try
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

                using (IDisposable limiterLease = await AcquireEndpointLimitersAsync(
                    documentId,
                    new List<EndpointLimiterTarget> { new EndpointLimiterTarget("embedding", endpointId) },
                    token).ConfigureAwait(false))
                {
                    using (HttpResponseMessage response = await _ChunkingService.SendAsync(HttpMethod.Post, "/v1.0/embed", json, token).ConfigureAwait(false))
                    {
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
            string path = "/v1.0/tenants/" + tenantId + "/collections/" + collectionId;

            try
            {
                using (HttpResponseMessage headResp = await _VectorStore.SendAsync(HttpMethod.Head, path, null, token).ConfigureAwait(false))
                {
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
        /// Ensure a Verbex index exists.
        /// </summary>
        private protected async Task<bool> EnsureVerbexIndexExistsAsync(string tenantId, string indexId, CancellationToken token)
        {
            if (String.IsNullOrEmpty(indexId)) return false;

            try
            {
                using (HttpResponseMessage headResp = await _InvertedIndex.SendAsync(HttpMethod.Head, "/v1.0/indices/" + Uri.EscapeDataString(indexId)).ConfigureAwait(false))
                {
                    if (headResp.IsSuccessStatusCode)
                        return true;

                    if (headResp.StatusCode != System.Net.HttpStatusCode.NotFound)
                    {
                        string responseBody = await headResp.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                        string message = "Verbex index " + indexId + " check returned HTTP " + (int)headResp.StatusCode + ": " + responseBody;
                        _Logging.Warn(_Header + message);
                        if (IsTransientVerbexResponse((int)headResp.StatusCode, responseBody))
                            throw new InvalidOperationException(message);
                        return false;
                    }
                }

                Dictionary<string, object> createBody = new Dictionary<string, object>
                {
                    { "Identifier", indexId },
                    { "TenantId", tenantId },
                    { "Name", indexId },
                    { "Description", "AssistantHub text search index" }
                };

                string json = JsonSerializer.Serialize(createBody, _JsonOptions);
                using (HttpResponseMessage createResp = await _InvertedIndex.SendAsync(HttpMethod.Post, "/v1.0/indices", json).ConfigureAwait(false))
                {
                    if (createResp.IsSuccessStatusCode || createResp.StatusCode == System.Net.HttpStatusCode.Conflict)
                        return true;

                    string responseBody = await createResp.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    string message = "Verbex index " + indexId + " create returned HTTP " + (int)createResp.StatusCode + ": " + responseBody;
                    _Logging.Warn(_Header + message);
                    if (IsTransientVerbexResponse((int)createResp.StatusCode, responseBody))
                        throw new InvalidOperationException(message);
                    return false;
                }
            }
            catch (Exception e)
            {
                if (IsTransientVerbexIndexingFailure(e))
                    throw;

                _Logging.Warn(_Header + "exception ensuring Verbex index " + indexId + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Resolve the Verbex tenant and default index identifiers for an AssistantHub tenant.
        /// </summary>
        private protected async Task<(string TenantId, string IndexId)> ResolveVerbexScopeAsync(string assistantHubTenantId, CancellationToken token)
        {
            return await ResolveVerbexScopeAsync(assistantHubTenantId, null, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Resolve the Verbex tenant and target index identifiers for an AssistantHub tenant.
        /// </summary>
        private protected async Task<(string TenantId, string IndexId)> ResolveVerbexScopeAsync(string assistantHubTenantId, string requestedIndexId, CancellationToken token)
        {
            string configuredIndexId = String.IsNullOrEmpty(_VerbexSettings.DefaultIndexId) ? "default" : _VerbexSettings.DefaultIndexId;
            string effectiveTenantId = String.IsNullOrEmpty(assistantHubTenantId) ? Constants.DefaultTenantId : assistantHubTenantId;
            string verbexTenantId = effectiveTenantId;
            string verbexIndexId = BuildVerbexDefaultIndexId(effectiveTenantId, configuredIndexId);
            bool isDefaultIndexRequest = String.IsNullOrEmpty(requestedIndexId)
                || String.Equals(requestedIndexId, configuredIndexId, StringComparison.OrdinalIgnoreCase);

            try
            {
                TenantMetadata tenant = await _Database.Tenant.ReadByIdAsync(effectiveTenantId, token).ConfigureAwait(false);
                if (tenant?.Tags != null)
                {
                    if (tenant.Tags.TryGetValue(Constants.VerbexTenantIdTag, out string mappedTenantId) && !String.IsNullOrEmpty(mappedTenantId))
                        verbexTenantId = mappedTenantId;

                    if (isDefaultIndexRequest
                        && tenant.Tags.TryGetValue(Constants.VerbexDefaultIndexIdTag, out string mappedIndexId)
                        && !String.IsNullOrEmpty(mappedIndexId))
                        verbexIndexId = mappedIndexId;
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception resolving Verbex scope for tenant " + effectiveTenantId + ": " + e.Message);
            }

            if (!isDefaultIndexRequest)
            {
                verbexIndexId = requestedIndexId;
            }

            return (verbexTenantId, verbexIndexId);
        }

        private protected string BuildVerbexDefaultIndexId(string assistantHubTenantId, string configuredIndexId = null)
        {
            string effectiveIndexId = String.IsNullOrEmpty(configuredIndexId) ? "default" : configuredIndexId;
            if (String.Equals(assistantHubTenantId, Constants.DefaultTenantId, StringComparison.OrdinalIgnoreCase))
                return effectiveIndexId;

            return assistantHubTenantId + "_" + effectiveIndexId;
        }

        /// <summary>
        /// Index extracted document text into Verbex.
        /// </summary>
        private protected async Task<bool> IndexDocumentTextAsync(
            AssistantDocument document,
            string content,
            IngestionRule rule,
            List<string> labels,
            Dictionary<string, string> tags,
            CancellationToken token)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            content = NormalizeTextForIndexing(content);

            if (!_VerbexSettings.EnableIngestion)
            {
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(document.Id, "INFO", "Verbex indexing skipped - disabled in configuration").ConfigureAwait(false);
                return false;
            }

            if (String.IsNullOrWhiteSpace(content))
            {
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(document.Id, "INFO", "Verbex indexing skipped - extracted text is empty").ConfigureAwait(false);
                return false;
            }

            int originalContentLength = content.Length;
            content = ApplyVerbexContentLimit(content, _VerbexSettings.MaxContentCharacters);
            if (content.Length != originalContentLength)
            {
                _Logging.Warn(_Header + "Verbex indexing content truncated for document " + document.Id + " from " + originalContentLength + " to " + content.Length + " chars");
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(document.Id, "WARN", "Verbex indexing content truncated from " + originalContentLength + " to " + content.Length + " chars").ConfigureAwait(false);
            }

            try
            {
                await _VerbexIndexingSemaphore.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    return await IndexDocumentTextWithRetriesAsync(document, content, rule, labels, tags, token).ConfigureAwait(false);
                }
                finally
                {
                    _VerbexIndexingSemaphore.Release();
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "Verbex indexing failed for document " + document.Id + ": " + e.Message);
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(document.Id, "ERROR", "Verbex indexing failed: " + e.Message).ConfigureAwait(false);

                if (_VerbexSettings.RequireIngestion)
                    throw;

                return false;
            }
        }

        private protected async Task<bool> IndexDocumentTextWithRetriesAsync(
            AssistantDocument document,
            string content,
            IngestionRule rule,
            List<string> labels,
            Dictionary<string, string> tags,
            CancellationToken token)
        {
            int maxAttempts = Math.Max(1, _VerbexSettings.IndexingRetryCount + 1);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await IndexDocumentTextAttemptAsync(document, content, rule, labels, tags, token).ConfigureAwait(false);
                }
                catch (Exception e) when (attempt < maxAttempts && IsTransientVerbexIndexingFailure(e) && !token.IsCancellationRequested)
                {
                    int delayMs = GetVerbexIndexingRetryDelayMs(attempt);
                    string retryMessage = "Verbex indexing retrying in " + delayMs + "ms after attempt " + attempt + " of " + maxAttempts + ": " + e.Message;
                    _Logging.Warn(_Header + retryMessage);
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(document.Id, "WARN", retryMessage).ConfigureAwait(false);

                    if (delayMs > 0)
                        await Task.Delay(delayMs, token).ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException("Verbex indexing failed without producing a result.");
        }

        private protected async Task<bool> IndexDocumentTextAttemptAsync(
            AssistantDocument document,
            string content,
            IngestionRule rule,
            List<string> labels,
            Dictionary<string, string> tags,
            CancellationToken token)
        {
            (string verbexTenantId, string indexId) = await ResolveVerbexScopeAsync(document.TenantId, rule?.VerbexIndexId, token).ConfigureAwait(false);

            if (_ProcessingLog != null)
                await _ProcessingLog.LogAsync(document.Id, "INFO", "Verbex indexing started - index: " + indexId + ", contentLength: " + content.Length + " chars").ConfigureAwait(false);

            bool indexReady = await EnsureVerbexIndexExistsAsync(verbexTenantId, indexId, token).ConfigureAwait(false);
            if (!indexReady)
                throw new InvalidOperationException("Verbex index is not available: " + indexId);

            string recordName = ResolveVerbexRecordName(document);

            Dictionary<string, object> metadata = new Dictionary<string, object>
            {
                { "AssistantHubDocumentId", document.Id },
                { "AssistantHubTenantId", document.TenantId },
                { "VerbexTenantId", verbexTenantId },
                { "VerbexIndexId", indexId },
                { "ObjectName", recordName },
                { "CollectionId", document.CollectionId },
                { "IngestionRuleId", document.IngestionRuleId },
                { "Bucket", document.BucketName },
                { "ObjectKey", document.S3Key },
                { "ContentType", document.ContentType },
                { "OriginalFileName", document.OriginalFilename },
                { "SourceUrl", document.SourceUrl }
            };

            Dictionary<string, object> record = new Dictionary<string, object>
            {
                { "Id", document.Id },
                { "Name", recordName },
                { "Content", content },
                { "CustomMetadata", metadata }
            };

            if (labels != null && labels.Count > 0)
                record["Labels"] = labels;

            if (tags != null && tags.Count > 0)
                record["Tags"] = tags;

            string json = JsonSerializer.Serialize(record, _JsonOptions);
            string path = "/v1.0/indices/" + Uri.EscapeDataString(indexId) + "/documents";

            using (HttpResponseMessage resp = await _InvertedIndex.SendAsync(HttpMethod.Post, path, json).ConfigureAwait(false))
            {
                if (resp.IsSuccessStatusCode)
                {
                    await PersistVerbexIndexMetadataAsync(document, verbexTenantId, indexId, token).ConfigureAwait(false);
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(document.Id, "INFO", "Verbex indexing complete - record: " + document.Id).ConfigureAwait(false);
                    return true;
                }

                if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    await DeleteIndexRecordInternalAsync(document.TenantId, indexId, document.Id, token).ConfigureAwait(false);
                    using (HttpResponseMessage retryResp = await _InvertedIndex.SendAsync(HttpMethod.Post, path, json).ConfigureAwait(false))
                    {
                        if (retryResp.IsSuccessStatusCode)
                        {
                            await PersistVerbexIndexMetadataAsync(document, verbexTenantId, indexId, token).ConfigureAwait(false);
                            if (_ProcessingLog != null)
                                await _ProcessingLog.LogAsync(document.Id, "INFO", "Verbex indexing complete after replacing existing record - record: " + document.Id).ConfigureAwait(false);
                            return true;
                        }

                        string retryBody = await retryResp.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                        throw new InvalidOperationException("Verbex indexing retry returned HTTP " + (int)retryResp.StatusCode + ": " + retryBody);
                    }
                }

                string responseBody = await resp.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                throw new InvalidOperationException("Verbex indexing returned HTTP " + (int)resp.StatusCode + ": " + responseBody);
            }
        }

        private protected int GetVerbexIndexingRetryDelayMs(int failedAttempt)
        {
            int baseDelayMs = _VerbexSettings.IndexingRetryDelayMs;
            if (baseDelayMs <= 0) return 0;

            int multiplier = 1 << Math.Min(Math.Max(0, failedAttempt - 1), 6);
            return Math.Min(60000, baseDelayMs * multiplier);
        }

        private protected static bool IsTransientVerbexIndexingFailure(Exception exception)
        {
            if (exception == null) return false;
            return IsTransientVerbexMessage(exception.ToString());
        }

        private protected static bool IsTransientVerbexResponse(int statusCode, string responseBody)
        {
            if (statusCode == 408 || statusCode == 429 || statusCode == 502 || statusCode == 503 || statusCode == 504)
                return true;

            return IsTransientVerbexMessage(responseBody);
        }

        private static bool IsTransientVerbexMessage(string message)
        {
            if (String.IsNullOrEmpty(message)) return false;

            return message.Contains("connection pool has been exhausted", StringComparison.OrdinalIgnoreCase)
                || message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                || message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase)
                || message.Contains("HTTP 408", StringComparison.OrdinalIgnoreCase)
                || message.Contains("HTTP 429", StringComparison.OrdinalIgnoreCase)
                || message.Contains("HTTP 502", StringComparison.OrdinalIgnoreCase)
                || message.Contains("HTTP 503", StringComparison.OrdinalIgnoreCase)
                || message.Contains("HTTP 504", StringComparison.OrdinalIgnoreCase);
        }

        private protected static string ResolveVerbexRecordName(AssistantDocument document)
        {
            if (document == null) return null;

            if (IsMeaningfulDocumentName(document.Name))
                return document.Name.Trim();

            if (!String.IsNullOrWhiteSpace(document.OriginalFilename))
                return document.OriginalFilename.Trim();

            string objectName = ExtractObjectName(document.S3Key);
            if (!String.IsNullOrWhiteSpace(objectName))
                return objectName;

            string sourceName = ExtractObjectName(document.SourceUrl);
            if (!String.IsNullOrWhiteSpace(sourceName))
                return sourceName;

            if (!String.IsNullOrWhiteSpace(document.Id))
                return document.Id;

            return "document";
        }

        private static bool IsMeaningfulDocumentName(string name)
        {
            return !String.IsNullOrWhiteSpace(name)
                && !String.Equals(name.Trim(), DefaultAssistantDocumentName, StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractObjectName(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;

            string candidate = value.Trim();
            if (Uri.TryCreate(candidate, UriKind.Absolute, out Uri uri) && !String.IsNullOrWhiteSpace(uri.AbsolutePath))
                candidate = uri.AbsolutePath;

            candidate = candidate.TrimEnd('/', '\\');
            if (String.IsNullOrWhiteSpace(candidate)) return null;

            int slash = candidate.LastIndexOf('/');
            int backslash = candidate.LastIndexOf('\\');
            int separator = Math.Max(slash, backslash);

            if (separator >= 0 && separator < candidate.Length - 1)
                return candidate.Substring(separator + 1);

            return candidate;
        }

        private protected async Task PersistVerbexIndexMetadataAsync(AssistantDocument document, string verbexTenantId, string indexId, CancellationToken token)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            document.VerbexTenantId = verbexTenantId;
            document.VerbexIndexId = indexId;
            document.VerbexRecordId = document.Id;

            await _Database.AssistantDocument.UpdateVerbexIndexMetadataAsync(
                document.Id,
                document.VerbexTenantId,
                document.VerbexIndexId,
                document.VerbexRecordId,
                token).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an indexed document record from Verbex.
        /// </summary>
        private protected async Task DeleteIndexRecordInternalAsync(string tenantId, string indexId, string recordId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(recordId)) throw new ArgumentNullException(nameof(recordId));

            (string _, string scopedIndexId) = await ResolveVerbexScopeAsync(tenantId, token).ConfigureAwait(false);
            string configuredIndexId = String.IsNullOrEmpty(_VerbexSettings.DefaultIndexId) ? "default" : _VerbexSettings.DefaultIndexId;
            string effectiveIndexId = String.IsNullOrEmpty(indexId) || String.Equals(indexId, configuredIndexId, StringComparison.OrdinalIgnoreCase)
                ? scopedIndexId
                : indexId;

            string path = "/v1.0/indices/" + Uri.EscapeDataString(effectiveIndexId) + "/documents/" + Uri.EscapeDataString(recordId);

            using (HttpResponseMessage response = await _InvertedIndex.SendAsync(HttpMethod.Delete, path).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _Logging.Debug(_Header + "deleted Verbex index record " + recordId + " from index " + effectiveIndexId + " for tenant " + tenantId);
                    return;
                }

                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                _Logging.Warn(_Header + "Verbex delete returned " + (int)response.StatusCode + " for record " + recordId + " in index " + effectiveIndexId + " for tenant " + tenantId + ": " + responseBody);
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
            string path = "/v1.0/tenants/" + tenantId + "/collections/" + collectionId + "/documents";

            try
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

                using (HttpResponseMessage response = await _VectorStore.SendAsync(HttpMethod.Put, path, json, token).ConfigureAwait(false))
                {
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
            string path = "/v1.0/tenants/" + tenantId + "/collections/" + collectionId + "/documents/batch";

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

                using (HttpResponseMessage response = await _VectorStore.SendAsync(HttpMethod.Post, path, json, token).ConfigureAwait(false))
                {
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
                using (HttpResponseMessage response = await _ChunkingService.SendAsync(
                    HttpMethod.Get,
                    "/v1.0/endpoints/" + endpointType + "/" + endpointId,
                    null,
                    token).ConfigureAwait(false))
                {
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
                using (HttpResponseMessage response = await _ChunkingService.SendAsync(
                    HttpMethod.Get,
                    "/v1.0/endpoints/" + endpointType + "/" + endpointId,
                    null,
                    token).ConfigureAwait(false))
                {
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
