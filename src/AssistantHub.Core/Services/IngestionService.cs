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
    public class IngestionService : IngestionServiceBase
    {
        #region Public-Members

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
        /// <param name="verbexSettings">Verbex service settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="processingLog">Optional processing log service.</param>
        public IngestionService(
            DatabaseDriverBase database,
            IObjectStorageService storage,
            DocumentAtomSettings documentAtomSettings,
            ChunkingSettings chunkingSettings,
            RecallDbSettings recallDbSettings,
            VerbexSettings verbexSettings,
            LoggingModule logging,
            ProcessingLogService processingLog = null)
            : base(database, storage, documentAtomSettings, chunkingSettings, recallDbSettings, verbexSettings, logging, processingLog)
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Reindex a document's extracted text into Verbex.
        /// </summary>
        /// <param name="documentId">Assistant document identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Reindex result.</returns>
        public async Task<DocumentReindexResult> ReindexDocumentTextAsync(string documentId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(documentId)) throw new ArgumentNullException(nameof(documentId));

            AssistantDocument document = await _Database.AssistantDocument.ReadAsync(documentId, token).ConfigureAwait(false);
            if (document == null)
            {
                return new DocumentReindexResult
                {
                    DocumentId = documentId,
                    Success = false,
                    Status = "NotFound",
                    Message = "Document not found."
                };
            }

            return await ReindexDocumentTextAsync(document, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Reindex a document's extracted text into Verbex.
        /// </summary>
        /// <param name="document">Assistant document record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Reindex result.</returns>
        public async Task<DocumentReindexResult> ReindexDocumentTextAsync(AssistantDocument document, CancellationToken token = default)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            Stopwatch sw = Stopwatch.StartNew();
            DocumentReindexResult result = new DocumentReindexResult
            {
                DocumentId = document.Id,
                VerbexTenantId = document.VerbexTenantId,
                VerbexIndexId = document.VerbexIndexId,
                VerbexRecordId = document.VerbexRecordId
            };

            try
            {
                if (!_VerbexSettings.EnableIngestion)
                {
                    result.Success = true;
                    result.Status = "Skipped";
                    result.Message = "Verbex ingestion is disabled in configuration.";
                    return result;
                }

                if (String.IsNullOrEmpty(document.S3Key))
                {
                    result.Success = false;
                    result.Status = "Failed";
                    result.Message = "Document has no stored object key.";
                    return result;
                }

                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(document.Id, "INFO", "Verbex reindex started for document " + document.Id).ConfigureAwait(false);

                byte[] fileBytes;
                if (!String.IsNullOrEmpty(document.BucketName))
                    fileBytes = await _Storage.DownloadAsync(document.BucketName, document.S3Key, token).ConfigureAwait(false);
                else
                    fileBytes = await _Storage.DownloadAsync(document.S3Key, token).ConfigureAwait(false);

                if (fileBytes == null || fileBytes.Length == 0)
                {
                    result.Success = false;
                    result.Status = "Failed";
                    result.Message = "File data is empty or could not be downloaded.";
                    return result;
                }

                string filename = document.OriginalFilename ?? document.Name ?? document.Id;
                TypeDetectResponse typeDetectResult = await DetectDocumentTypeAsync(document.Id, fileBytes, filename, token).ConfigureAwait(false);
                string detectedType = typeDetectResult?.Type;
                if (String.IsNullOrEmpty(detectedType) || String.Equals(detectedType, "unknown", StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = false;
                    result.Status = "Failed";
                    result.Message = "Document type could not be detected.";
                    return result;
                }

                if (!String.IsNullOrEmpty(typeDetectResult?.MimeType) && document.ContentType != typeDetectResult.MimeType)
                {
                    document.ContentType = typeDetectResult.MimeType;
                    await _Database.AssistantDocument.UpdateAsync(document, token).ConfigureAwait(false);
                }

                string extractedContent = await ProcessDocumentContentAsync(document.Id, fileBytes, detectedType, filename, token).ConfigureAwait(false);
                if (String.IsNullOrWhiteSpace(extractedContent))
                {
                    result.Success = false;
                    result.Status = "Failed";
                    result.Message = "Failed to extract text from document.";
                    return result;
                }

                IngestionRule rule = null;
                if (!String.IsNullOrEmpty(document.IngestionRuleId))
                    rule = await _Database.IngestionRule.ReadAsync(document.IngestionRuleId, token).ConfigureAwait(false);

                List<string> mergedLabels = MergeLabels(rule, document);
                Dictionary<string, string> mergedTags = MergeTags(rule, document);
                bool indexed = await IndexDocumentTextAsync(document, extractedContent, rule, mergedLabels, mergedTags, token).ConfigureAwait(false);

                result.Success = indexed;
                result.Status = indexed ? "Reindexed" : "Failed";
                result.Message = indexed
                    ? "Document text reindexed into Verbex."
                    : "Verbex indexing did not complete.";
                result.VerbexTenantId = document.VerbexTenantId;
                result.VerbexIndexId = document.VerbexIndexId;
                result.VerbexRecordId = document.VerbexRecordId;

                return result;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during Verbex reindex of document " + document.Id + ": " + e.Message);
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(document.Id, "ERROR", "Verbex reindex failed: " + e.Message).ConfigureAwait(false);

                result.Success = false;
                result.Status = "Failed";
                result.Message = e.Message;
                return result;
            }
            finally
            {
                sw.Stop();
                result.TotalMs = sw.Elapsed.TotalMilliseconds;
            }
        }

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
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "Type detection failed - could not detect document type").ConfigureAwait(false);
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
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "Atom extraction failed" + elapsedStr + " - no content returned").ConfigureAwait(false);
                    }
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "Failed to extract content from document.", token).ConfigureAwait(false);
                    return;
                }

                if (_ProcessingLog != null)
                    await _ProcessingLog.LogStepCompleteAsync(documentId, "Atom extraction", extractedContent.Length + " characters extracted", extractSw).ConfigureAwait(false);

                _Logging.Debug(_Header + "extracted " + extractedContent.Length + " characters from document " + documentId);

                // Step 8: Merge labels and tags from rule + document
                List<string> mergedLabels = MergeLabels(rule, document);
                Dictionary<string, string> mergedTags = MergeTags(rule, document);

                // Step 9: Index full extracted text stream into Verbex
                currentStep = "Verbex indexing";
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.StoringText, "Storing extracted text.", token).ConfigureAwait(false);
                await IndexDocumentTextAsync(document, extractedContent, rule, mergedLabels, mergedTags, token).ConfigureAwait(false);

                // Step 10: Prepare Partio processing service with rule config
                currentStep = "Chunking and embedding via Partio";
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.ProcessingChunks, "Processing content.", token).ConfigureAwait(false);
                bool hasSummarization = false;

                // Determine if summarization is enabled and has required fields
                if (rule == null)
                {
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Summarization skipped - no ingestion rule assigned").ConfigureAwait(false);
                }
                else if (rule.Summarization == null)
                {
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Summarization skipped - summarization configuration is null in rule \"" + rule.Name + "\"").ConfigureAwait(false);
                }
                else if (String.IsNullOrWhiteSpace(rule.Summarization.CompletionEndpointId))
                {
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Summarization skipped - CompletionEndpointId is not set in summarization configuration").ConfigureAwait(false);
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
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Embedding endpoint: " + embEndpointId + (embEndpointInfo != null ? " - " + embEndpointInfo : "")).ConfigureAwait(false);

                    // Resolve and log completion endpoint if summarization is enabled
                    if (hasSummarization)
                    {
                        string compEndpointInfo = await ResolveEndpointInfoAsync("completion", rule.Summarization.CompletionEndpointId, token).ConfigureAwait(false);
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Completion endpoint: " + rule.Summarization.CompletionEndpointId + (compEndpointInfo != null ? " - " + compEndpointInfo : "")).ConfigureAwait(false);
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
                        await _ProcessingLog.LogAsync(documentId, "INFO", "Summarization enabled - " + sumParams).ConfigureAwait(false);
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
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Processing started - " + chunkParams).ConfigureAwait(false);
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
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "Summarization and chunking failed in " + chunkSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms - no chunks returned").ConfigureAwait(false);
                    }
                    else if (_ProcessingLog != null)
                    {
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "Chunking failed in " + chunkSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms - no chunks returned").ConfigureAwait(false);
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

                // Step 13: Store chunk embeddings in RecallDB via batch API
                if (_ProcessingLog != null)
                {
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Embedding storage started - " + chunks.Count + " chunks to store (batch)").ConfigureAwait(false);
                }

                Stopwatch storeSw = Stopwatch.StartNew();
                List<string> chunkRecordIds = await StoreEmbeddingBatchAsync(document.TenantId, collectionId, documentId, chunks, token).ConfigureAwait(false);
                int storedCount = chunkRecordIds.Count;

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
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Pipeline complete - total runtime " + pipelineSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during ingestion of document " + documentId + ": " + e.Message);
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "Ingestion failed: " + e.Message, token).ConfigureAwait(false);

                pipelineSw.Stop();
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "ERROR", "Pipeline failed during step: " + currentStep + " - " + e.Message + " - total runtime " + pipelineSw.Elapsed.TotalMilliseconds.ToString("F2") + "ms").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Delete multiple embedding records from a RecallDB collection in a single batch request.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="recordIds">List of record identifiers to delete.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DeleteEmbeddingBatchAsync(string tenantId, string collectionId, List<string> recordIds, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            if (recordIds == null || recordIds.Count == 0) return;

            string path = "/v1.0/tenants/" + tenantId + "/collections/" + collectionId + "/documents/batch/delete";

            string body = JsonSerializer.Serialize(new { DocumentKeys = recordIds });

            using (HttpResponseMessage response = await _VectorStore.SendAsync(HttpMethod.Post, path, body, token).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    _Logging.Warn(_Header + "RecallDB batch delete returned " + (int)response.StatusCode + " for " + recordIds.Count + " records in collection " + collectionId + ": " + responseBody);
                }
                else
                {
                    _Logging.Debug(_Header + "batch deleted " + recordIds.Count + " embedding records from collection " + collectionId);
                }
            }
        }

        /// <summary>
        /// Delete a single embedding record from a RecallDB collection.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="recordId">Record identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DeleteEmbeddingAsync(string tenantId, string collectionId, string recordId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            if (String.IsNullOrEmpty(recordId)) throw new ArgumentNullException(nameof(recordId));

            string path = "/v1.0/tenants/" + tenantId + "/collections/" + collectionId + "/documents/" + recordId;

            using (HttpResponseMessage response = await _VectorStore.SendAsync(HttpMethod.Delete, path, null, token).ConfigureAwait(false))
            {
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

        /// <summary>
        /// Delete an indexed document record from Verbex.
        /// </summary>
        /// <param name="tenantId">AssistantHub tenant identifier.</param>
        /// <param name="indexId">Index identifier.</param>
        /// <param name="recordId">Record identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DeleteIndexRecordAsync(string tenantId, string indexId, string recordId, CancellationToken token = default)
        {
            await DeleteIndexRecordInternalAsync(tenantId, indexId, recordId, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete multiple indexed document records from a Verbex index.
        /// </summary>
        /// <param name="tenantId">AssistantHub tenant identifier.</param>
        /// <param name="indexId">Index identifier.</param>
        /// <param name="recordIds">Record identifiers.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DeleteIndexRecordBatchAsync(string tenantId, string indexId, List<string> recordIds, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (recordIds == null || recordIds.Count == 0) return;

            List<string> distinctIds = recordIds
                .Where(id => !String.IsNullOrEmpty(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (distinctIds.Count == 0) return;

            (string _, string scopedIndexId) = await ResolveVerbexScopeAsync(tenantId, token).ConfigureAwait(false);
            string configuredIndexId = String.IsNullOrEmpty(_VerbexSettings.DefaultIndexId) ? "default" : _VerbexSettings.DefaultIndexId;
            string effectiveIndexId = String.IsNullOrEmpty(indexId) || String.Equals(indexId, configuredIndexId, StringComparison.OrdinalIgnoreCase)
                ? scopedIndexId
                : indexId;

            string path = "/v1.0/indices/" + Uri.EscapeDataString(effectiveIndexId) + "/documents/delete";
            string body = JsonSerializer.Serialize(new { DocumentIds = distinctIds }, _JsonOptions);

            using (HttpResponseMessage response = await _InvertedIndex.SendAsync(HttpMethod.Post, path, body).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _Logging.Debug(_Header + "batch deleted " + distinctIds.Count + " Verbex index records from index " + effectiveIndexId + " for tenant " + tenantId);
                    return;
                }

                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                _Logging.Warn(_Header + "Verbex batch delete returned " + (int)response.StatusCode + " for " + distinctIds.Count + " records in index " + effectiveIndexId + " for tenant " + tenantId + ": " + responseBody);
            }
        }

        #endregion

    }
}
