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
    /// Background document ingestion pipeline service.
    /// </summary>
    public class IngestionService
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[IngestionService] ";
        private DatabaseDriverBase _Database = null;
        private StorageService _Storage = null;
        private DocumentAtomSettings _DocumentAtomSettings = null;
        private ChunkingSettings _ChunkingSettings = null;
        private RecallDbSettings _RecallDbSettings = null;
        private LoggingModule _Logging = null;
        private ProcessingLogService _ProcessingLog = null;
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
        /// <param name="database">Database driver.</param>
        /// <param name="storage">Storage service.</param>
        /// <param name="documentAtomSettings">DocumentAtom service settings.</param>
        /// <param name="chunkingSettings">Chunking service settings.</param>
        /// <param name="recallDbSettings">RecallDb service settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="processingLog">Optional processing log service.</param>
        public IngestionService(
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

        #endregion

        #region Public-Methods

        /// <summary>
        /// Process a document through the full ingestion pipeline.
        /// </summary>
        /// <param name="documentId">Document identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task ProcessDocumentAsync(string documentId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(documentId)) throw new ArgumentNullException(nameof(documentId));

            Stopwatch pipelineSw = Stopwatch.StartNew();

            _Logging.Info(_Header + "starting ingestion pipeline for document " + documentId);

            string currentStep = "Initialization";

            try
            {
                // Step 1: Read document from database
                AssistantDocument document = await _Database.AssistantDocument.ReadAsync(documentId, token).ConfigureAwait(false);
                if (document == null)
                {
                    _Logging.Warn(_Header + "document not found: " + documentId);
                    return;
                }

                string tenantId = document.TenantId;

                // Pipeline start log
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Pipeline started for document " + documentId + ", filename: " + (document.OriginalFilename ?? "unknown"), tenantId).ConfigureAwait(false);

                // Step 1b: Load ingestion rule if set
                IngestionRule rule = null;
                if (!String.IsNullOrEmpty(document.IngestionRuleId))
                {
                    rule = await _Database.IngestionRule.ReadAsync(document.IngestionRuleId, token).ConfigureAwait(false);
                    if (rule != null)
                        _Logging.Debug(_Header + "using ingestion rule " + rule.Id + " (" + rule.Name + ")");
                }

                if (_ProcessingLog != null)
                {
                    if (rule != null)
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Ingestion rule loaded: " + rule.Id + " (" + rule.Name + ")").ConfigureAwait(false);
                    else
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Ingestion rule loaded: no rule").ConfigureAwait(false);
                }

                // Step 2: Update status to TypeDetecting
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.TypeDetecting, "Detecting document type.", token).ConfigureAwait(false);

                // Step 3: Download file bytes from S3 (bucket-aware)
                currentStep = "File download from S3";
                Stopwatch downloadSw = _ProcessingLog != null ? await _ProcessingLog.LogStepStartAsync(documentId, "File download from S3").ConfigureAwait(false) : null;

                byte[] fileBytes;
                if (!String.IsNullOrEmpty(document.BucketName))
                    fileBytes = await _Storage.DownloadAsync(document.BucketName, document.S3Key, token).ConfigureAwait(false);
                else
                    fileBytes = await _Storage.DownloadAsync(document.S3Key, token).ConfigureAwait(false);

                if (fileBytes == null || fileBytes.Length == 0)
                {
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "File data is empty or could not be downloaded").ConfigureAwait(false);
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "File data is empty or could not be downloaded.", token).ConfigureAwait(false);
                    return;
                }

                if (_ProcessingLog != null)
                    await _ProcessingLog.LogStepCompleteAsync(documentId, "File download from S3", "bucket: " + (document.BucketName ?? "default") + ", key: " + document.S3Key + ", " + fileBytes.Length + " bytes", downloadSw).ConfigureAwait(false);

                _Logging.Debug(_Header + "downloaded " + fileBytes.Length + " bytes for document " + documentId);

                // Step 4: Call DocumentAtom type detection
                currentStep = "Type detection";
                Stopwatch typeDetectSw = _ProcessingLog != null ? await _ProcessingLog.LogStepStartAsync(documentId, "Type detection").ConfigureAwait(false) : null;

                TypeDetectResponse typeDetectResult = await DetectDocumentTypeAsync(documentId, fileBytes, document.OriginalFilename, token).ConfigureAwait(false);
                string detectedType = typeDetectResult?.Type;

                // Step 5: Check if type is unknown
                if (String.IsNullOrEmpty(detectedType) || String.Equals(detectedType, "unknown", StringComparison.OrdinalIgnoreCase))
                {
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "Type detection failed — could not detect document type").ConfigureAwait(false);
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.TypeDetectionFailed, "Document type could not be detected.", token).ConfigureAwait(false);
                    _Logging.Warn(_Header + "type detection failed for document " + documentId);
                    return;
                }

                if (_ProcessingLog != null)
                    await _ProcessingLog.LogStepCompleteAsync(documentId, "Type detection", "detected type: " + detectedType, typeDetectSw).ConfigureAwait(false);

                // Update document content type from type detection result
                if (!String.IsNullOrEmpty(typeDetectResult.MimeType) && document.ContentType != typeDetectResult.MimeType)
                {
                    document.ContentType = typeDetectResult.MimeType;
                    await _Database.AssistantDocument.UpdateAsync(document, token).ConfigureAwait(false);
                    _Logging.Debug(_Header + "updated content type for document " + documentId + " to " + typeDetectResult.MimeType);
                }

                // Step 6: Update status to TypeDetectionSuccess, then Processing
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.TypeDetectionSuccess, "Detected type: " + detectedType, token).ConfigureAwait(false);
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Processing, "Processing document content.", token).ConfigureAwait(false);

                // Step 7: Call DocumentAtom processing endpoint
                currentStep = "Atom extraction";
                Stopwatch extractSw = _ProcessingLog != null ? await _ProcessingLog.LogStepStartAsync(documentId, "Atom extraction").ConfigureAwait(false) : null;

                string extractedContent = await ProcessDocumentContentAsync(documentId, fileBytes, detectedType, document.OriginalFilename, token).ConfigureAwait(false);
                if (String.IsNullOrEmpty(extractedContent))
                {
                    if (_ProcessingLog != null)
                    {
                        extractSw?.Stop();
                        string elapsedStr = extractSw != null ? " in " + extractSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms" : "";
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "Atom extraction failed" + elapsedStr + " — no content returned").ConfigureAwait(false);
                    }
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "Failed to extract content from document.", token).ConfigureAwait(false);
                    return;
                }

                if (_ProcessingLog != null)
                    await _ProcessingLog.LogStepCompleteAsync(documentId, "Atom extraction", extractedContent.Length + " characters extracted", extractSw).ConfigureAwait(false);

                _Logging.Debug(_Header + "extracted " + extractedContent.Length + " characters from document " + documentId);

                // Step 8: Update status to ProcessingChunks
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.ProcessingChunks, "Processing content.", token).ConfigureAwait(false);

                // Step 9: Merge labels and tags from rule + document
                List<string> mergedLabels = MergeLabels(rule, document);
                Dictionary<string, string> mergedTags = MergeTags(rule, document);

                // Step 10: Call Partio processing service with rule config
                currentStep = "Chunking and embedding via Partio";
                bool hasSummarization = false;

                // Determine if summarization is enabled and has required fields
                if (rule == null)
                {
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Summarization skipped — no ingestion rule assigned").ConfigureAwait(false);
                }
                else if (rule.Summarization == null)
                {
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Summarization skipped — summarization configuration is null in rule \"" + rule.Name + "\"").ConfigureAwait(false);
                }
                else if (String.IsNullOrWhiteSpace(rule.Summarization.CompletionEndpointId))
                {
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Summarization skipped — CompletionEndpointId is not set in summarization configuration").ConfigureAwait(false);
                }
                else
                {
                    hasSummarization = true;
                }

                // Resolve and log endpoint details
                if (_ProcessingLog != null)
                {
                    // Log Partio service URL
                    string partioUrl = _ChunkingSettings.Endpoint.TrimEnd('/') + "/v1.0/process";
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Partio service URL: " + partioUrl).ConfigureAwait(false);

                    // Resolve and log embedding endpoint
                    string embEndpointId = rule?.Embedding?.EmbeddingEndpointId ?? _ChunkingSettings.EndpointId;
                    string embEndpointInfo = await ResolveEndpointInfoAsync("embedding", embEndpointId, token).ConfigureAwait(false);
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Embedding endpoint: " + embEndpointId + (embEndpointInfo != null ? " — " + embEndpointInfo : "")).ConfigureAwait(false);

                    // Resolve and log completion endpoint if summarization is enabled
                    if (hasSummarization)
                    {
                        string compEndpointInfo = await ResolveEndpointInfoAsync("completion", rule.Summarization.CompletionEndpointId, token).ConfigureAwait(false);
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Completion endpoint: " + rule.Summarization.CompletionEndpointId + (compEndpointInfo != null ? " — " + compEndpointInfo : "")).ConfigureAwait(false);
                    }
                }

                if (hasSummarization)
                {
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Summarizing, "Summarizing document content.", token).ConfigureAwait(false);

                    if (_ProcessingLog != null)
                    {
                        string sumParams = "order: " + rule.Summarization.Order.ToString()
                            + ", completionEndpoint: " + rule.Summarization.CompletionEndpointId
                            + ", maxTokens: " + rule.Summarization.MaxSummaryTokens
                            + ", minCellLength: " + rule.Summarization.MinCellLength
                            + ", maxParallelTasks: " + rule.Summarization.MaxParallelTasks
                            + ", timeoutMs: " + rule.Summarization.TimeoutMs;
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Summarization enabled — " + sumParams).ConfigureAwait(false);
                    }
                }

                if (_ProcessingLog != null)
                {
                    string chunkParams = "strategy: " + (rule?.Chunking?.Strategy ?? "default");
                    if (rule?.Chunking != null)
                    {
                        chunkParams += ", tokenCount: " + rule.Chunking.FixedTokenCount
                            + ", overlapCount: " + rule.Chunking.OverlapCount;
                        if (!String.IsNullOrEmpty(rule.Chunking.OverlapStrategy))
                            chunkParams += ", overlapStrategy: " + rule.Chunking.OverlapStrategy;
                    }
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Processing started — " + chunkParams).ConfigureAwait(false);
                }

                Stopwatch summarizeSw = hasSummarization && _ProcessingLog != null ? await _ProcessingLog.LogStepStartAsync(documentId, "Summarization").ConfigureAwait(false) : null;
                Stopwatch chunkSw = Stopwatch.StartNew();

                List<ChunkResult> chunks = await ChunkAndEmbedContentAsync(documentId, extractedContent, rule, mergedLabels, mergedTags, token).ConfigureAwait(false);
                if (chunks == null || chunks.Count == 0)
                {
                    chunkSw.Stop();
                    if (hasSummarization && _ProcessingLog != null)
                    {
                        summarizeSw?.Stop();
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "Summarization and chunking failed in " + chunkSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms — no chunks returned").ConfigureAwait(false);
                    }
                    else if (_ProcessingLog != null)
                    {
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "Chunking failed in " + chunkSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms — no chunks returned").ConfigureAwait(false);
                    }
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "Failed to chunk document content.", token).ConfigureAwait(false);
                    return;
                }

                if (hasSummarization && _ProcessingLog != null)
                    await _ProcessingLog.LogStepCompleteAsync(documentId, "Summarization", "summarization complete, " + chunks.Count + " chunks generated", summarizeSw).ConfigureAwait(false);

                if (_ProcessingLog != null)
                    await _ProcessingLog.LogStepCompleteAsync(documentId, "Chunking", chunks.Count + " chunks generated", chunkSw).ConfigureAwait(false);

                _Logging.Debug(_Header + "generated " + chunks.Count + " chunks for document " + documentId);

                // Step 11: Update status to StoringEmbeddings
                currentStep = "Embedding storage";
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.StoringEmbeddings, "Storing " + chunks.Count + " embeddings.", token).ConfigureAwait(false);

                // Step 12: Determine collection ID
                string collectionId = document.CollectionId;

                if (String.IsNullOrEmpty(collectionId))
                {
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "No collection identifier configured").ConfigureAwait(false);
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "No collection identifier configured.", token).ConfigureAwait(false);
                    return;
                }

                // Step 12b: Ensure collection exists in RecallDB
                bool collectionReady = await EnsureCollectionExistsAsync(document.TenantId, collectionId, token).ConfigureAwait(false);
                if (!collectionReady)
                {
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "RecallDB collection " + collectionId + " could not be found or created").ConfigureAwait(false);
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "RecallDB collection not available.", token).ConfigureAwait(false);
                    return;
                }

                // Step 13: Store each chunk+embedding in RecallDB and capture record IDs
                if (_ProcessingLog != null)
                {
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Embedding storage started — " + chunks.Count + " chunks to store").ConfigureAwait(false);
                }

                Stopwatch storeSw = Stopwatch.StartNew();
                int storedCount = 0;
                List<string> chunkRecordIds = new List<string>();

                for (int i = 0; i < chunks.Count; i++)
                {
                    ChunkResult chunk = chunks[i];
                    string recordId = await StoreEmbeddingAsync(document.TenantId, collectionId, documentId, chunk, i, token).ConfigureAwait(false);
                    if (!String.IsNullOrEmpty(recordId))
                    {
                        storedCount++;
                        chunkRecordIds.Add(recordId);
                    }

                    if (_ProcessingLog != null && (i + 1) % 10 == 0)
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Embedding storage progress: [" + (i + 1) + "/" + chunks.Count + "] chunks stored").ConfigureAwait(false);
                }

                if (_ProcessingLog != null)
                    await _ProcessingLog.LogStepCompleteAsync(documentId, "Embedding storage", storedCount + "/" + chunks.Count + " stored", storeSw).ConfigureAwait(false);

                _Logging.Info(_Header + "stored " + storedCount + "/" + chunks.Count + " embeddings for document " + documentId);

                // Step 14: Persist chunk record IDs on the document
                if (chunkRecordIds.Count > 0)
                {
                    string chunkRecordIdsJson = JsonSerializer.Serialize(chunkRecordIds, _JsonOptions);
                    await _Database.AssistantDocument.UpdateChunkRecordIdsAsync(documentId, chunkRecordIdsJson, token).ConfigureAwait(false);
                }

                // Step 15: Update status to Completed
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Completed, "Ingestion complete. " + storedCount + " chunks stored.", token).ConfigureAwait(false);
                _Logging.Info(_Header + "ingestion pipeline completed for document " + documentId);

                pipelineSw.Stop();
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Pipeline complete — total runtime " + pipelineSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during ingestion of document " + documentId + ": " + e.Message);
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "Ingestion failed: " + e.Message, token).ConfigureAwait(false);

                pipelineSw.Stop();
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "ERROR", "Pipeline failed during step: " + currentStep + " — " + e.Message + " — total runtime " + pipelineSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Delete a single embedding record from a RecallDB collection.
        /// </summary>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="recordId">Record identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DeleteEmbeddingAsync(string tenantId, string collectionId, string recordId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            if (String.IsNullOrEmpty(recordId)) throw new ArgumentNullException(nameof(recordId));

            string url = _RecallDbSettings.Endpoint.TrimEnd('/') + "/v1.0/tenants/" + tenantId + "/collections/" + collectionId + "/documents/" + recordId;

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, url))
            {
                if (!String.IsNullOrEmpty(_RecallDbSettings.AccessKey))
                {
                    request.Headers.Add("Authorization", "Bearer " + _RecallDbSettings.AccessKey);
                }

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    _Logging.Warn(_Header + "RecallDB delete returned " + (int)response.StatusCode + " for record " + recordId + ": " + responseBody);
                }
                else
                {
                    _Logging.Debug(_Header + "deleted embedding record " + recordId + " from collection " + collectionId);
                }
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Update the status of a document in the database.
        /// </summary>
        /// <param name="documentId">Document identifier.</param>
        /// <param name="status">New status.</param>
        /// <param name="statusMessage">Status message.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task UpdateDocumentStatusAsync(string documentId, DocumentStatusEnum status, string statusMessage, CancellationToken token)
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
        private async Task<TypeDetectResponse> DetectDocumentTypeAsync(string documentId, byte[] fileBytes, string filename, CancellationToken token)
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
        private async Task<string> ProcessDocumentContentAsync(string documentId, byte[] fileBytes, string documentType, string filename, CancellationToken token)
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
                            await _ProcessingLog.LogAsync(documentId, "DEBUG", "Atom [" + atomIndex + "/" + atoms.Count + "] — " + atom.Text.Length + " characters").ConfigureAwait(false);
                    }
                }

                return sb.Length > 0 ? sb.ToString() : null;
            }
        }

        /// <summary>
        /// Map a detected document type to the corresponding DocumentAtom atom endpoint path.
        /// </summary>
        private static string GetAtomPath(string documentType)
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
        private async Task<List<ChunkResult>> ChunkAndEmbedContentAsync(
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
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Chunking strategy is None — skipping chunking, treating entire content as a single chunk").ConfigureAwait(false);

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
                    sumConfig["MaxParallelTasks"] = rule.Summarization.MaxParallelTasks;
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
                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                apiSw.Stop();

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "processing service returned " + (int)response.StatusCode + " in " + apiSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms: " + responseBody);
                    if (_ProcessingLog != null)
                    {
                        await _ProcessingLog.LogAsync(documentId, "ERROR",
                            "Step: Chunking/Embedding/Summarization via Partio — HTTP " + (int)response.StatusCode + " in " + apiSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms").ConfigureAwait(false);
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

        /// <summary>
        /// Recursively flatten all chunks from a SemanticCellResponse hierarchy.
        /// </summary>
        private List<ChunkResult> FlattenChunks(SemanticCellResponse cell)
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
        private async Task<List<float>> ComputeEmbeddingAsync(string documentId, string text, IngestionRule rule, CancellationToken token)
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

                    HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                    string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        _Logging.Warn(_Header + "embedding service returned " + (int)response.StatusCode + ": " + responseBody);
                        if (_ProcessingLog != null)
                        {
                            await _ProcessingLog.LogAsync(documentId, "ERROR",
                                "Step: Single-chunk embedding via Partio /v1.0/embed — HTTP " + (int)response.StatusCode).ConfigureAwait(false);
                            await _ProcessingLog.LogAsync(documentId, "ERROR",
                                "Source content: " + text.Length + " chars, excerpt: " + Excerpt(text)).ConfigureAwait(false);
                            await _ProcessingLog.LogAsync(documentId, "ERROR",
                                "Response body: " + responseBody).ConfigureAwait(false);
                        }
                        return null;
                    }

                    // Partio returns { "Embeddings": [float, ...] }
                    using (JsonDocument doc = JsonDocument.Parse(responseBody))
                    {
                        if (doc.RootElement.TryGetProperty("Embeddings", out JsonElement embArr) && embArr.ValueKind == JsonValueKind.Array)
                        {
                            List<float> embeddings = new List<float>();
                            foreach (JsonElement el in embArr.EnumerateArray())
                                embeddings.Add(el.GetSingle());
                            return embeddings;
                        }
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception computing embedding: " + e.Message);
                if (_ProcessingLog != null)
                {
                    await _ProcessingLog.LogAsync(documentId, "ERROR",
                        "Step: Single-chunk embedding — exception: " + e.Message).ConfigureAwait(false);
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
        private async Task<bool> EnsureCollectionExistsAsync(string tenantId, string collectionId, CancellationToken token)
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
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="documentId">Source document identifier.</param>
        /// <param name="chunk">Chunk with embedding data.</param>
        /// <param name="chunkIndex">Zero-based chunk position within the document.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Document key if stored successfully, null otherwise.</returns>
        private async Task<string> StoreEmbeddingAsync(string tenantId, string collectionId, string documentId, ChunkResult chunk, int chunkIndex, CancellationToken token)
        {
            string url = _RecallDbSettings.Endpoint.TrimEnd('/') + "/v1.0/tenants/" + tenantId + "/collections/" + collectionId + "/documents";

            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url))
                {
                    object requestBody = new
                    {
                        Content = chunk.Text,
                        Embeddings = chunk.Embeddings,
                        DocumentId = documentId,
                        Position = chunkIndex,
                        ContentType = "Text"
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
        /// Truncate text to a maximum length for logging, appending total length if truncated.
        /// </summary>
        private string Excerpt(string text, int maxLength = 500)
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
        private async Task<string> ResolveEndpointInfoAsync(string endpointType, string endpointId, CancellationToken token)
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
                    using (JsonDocument doc = JsonDocument.Parse(responseBody))
                    {
                        JsonElement root = doc.RootElement;
                        string model = null;
                        string endpoint = null;
                        string name = null;

                        if (root.TryGetProperty("Model", out JsonElement modelEl))
                            model = modelEl.GetString();
                        if (root.TryGetProperty("Endpoint", out JsonElement endpointEl))
                            endpoint = endpointEl.GetString();
                        if (root.TryGetProperty("Name", out JsonElement nameEl))
                            name = nameEl.GetString();

                        List<string> parts = new List<string>();
                        if (!String.IsNullOrEmpty(name)) parts.Add("name: " + name);
                        if (!String.IsNullOrEmpty(model)) parts.Add("model: " + model);
                        if (!String.IsNullOrEmpty(endpoint)) parts.Add("url: " + endpoint);
                        return parts.Count > 0 ? String.Join(", ", parts) : null;
                    }
                }
            }
            catch (Exception e)
            {
                _Logging.Debug(_Header + "could not resolve " + endpointType + " endpoint " + endpointId + ": " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Merge labels from ingestion rule and document.
        /// Document labels are appended to rule labels.
        /// </summary>
        private List<string> MergeLabels(IngestionRule rule, AssistantDocument document)
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
        private Dictionary<string, string> MergeTags(IngestionRule rule, AssistantDocument document)
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

        /// <summary>
        /// Type detection response from DocumentAtom.
        /// </summary>
        private class TypeDetectResponse
        {
            /// <summary>
            /// Detected MIME type.
            /// </summary>
            public string MimeType { get; set; } = null;

            /// <summary>
            /// Detected document type.
            /// </summary>
            public string Type { get; set; } = null;
        }

        /// <summary>
        /// Atom response from DocumentAtom.
        /// </summary>
        private class AtomResponse
        {
            /// <summary>
            /// Text content of the atom.
            /// </summary>
            public string Text { get; set; } = null;
        }

        /// <summary>
        /// Semantic cell response from the Partio v0.4.0 service.
        /// </summary>
        private class SemanticCellResponse
        {
            public Guid GUID { get; set; }
            public Guid? ParentGUID { get; set; }
            public string Type { get; set; } = null;
            public string Text { get; set; } = null;
            public List<ChunkResult> Chunks { get; set; } = null;
            public List<SemanticCellResponse> Children { get; set; } = null;
        }

        /// <summary>
        /// A single chunk result from Partio v0.4.0.
        /// </summary>
        private class ChunkResult
        {
            public Guid CellGUID { get; set; }
            public string Text { get; set; } = null;
            public List<string> Labels { get; set; } = null;
            public Dictionary<string, string> Tags { get; set; } = null;
            public List<float> Embeddings { get; set; } = null;
        }

        /// <summary>
        /// Response from RecallDB when storing a document/embedding.
        /// </summary>
        private class RecallDbStoreResponse
        {
            /// <summary>
            /// Document key (unique identifier within the collection).
            /// </summary>
            public string DocumentKey { get; set; } = null;
        }

        #endregion
    }
}
