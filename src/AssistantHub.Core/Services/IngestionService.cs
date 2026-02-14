namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
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
        public IngestionService(
            DatabaseDriverBase database,
            StorageService storage,
            DocumentAtomSettings documentAtomSettings,
            ChunkingSettings chunkingSettings,
            RecallDbSettings recallDbSettings,
            LoggingModule logging)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _DocumentAtomSettings = documentAtomSettings ?? throw new ArgumentNullException(nameof(documentAtomSettings));
            _ChunkingSettings = chunkingSettings ?? throw new ArgumentNullException(nameof(chunkingSettings));
            _RecallDbSettings = recallDbSettings ?? throw new ArgumentNullException(nameof(recallDbSettings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
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

            _Logging.Info(_Header + "starting ingestion pipeline for document " + documentId);

            try
            {
                // Step 1: Read document from database
                AssistantDocument document = await _Database.AssistantDocument.ReadAsync(documentId, token).ConfigureAwait(false);
                if (document == null)
                {
                    _Logging.Warn(_Header + "document not found: " + documentId);
                    return;
                }

                // Step 2: Update status to TypeDetecting
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.TypeDetecting, "Detecting document type.", token).ConfigureAwait(false);

                // Step 3: Download file bytes from S3
                byte[] fileBytes = await _Storage.DownloadAsync(document.S3Key, token).ConfigureAwait(false);
                if (fileBytes == null || fileBytes.Length == 0)
                {
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "File data is empty or could not be downloaded.", token).ConfigureAwait(false);
                    return;
                }

                _Logging.Debug(_Header + "downloaded " + fileBytes.Length + " bytes for document " + documentId);

                // Step 4: Call DocumentAtom type detection
                string detectedType = await DetectDocumentTypeAsync(fileBytes, document.OriginalFilename, token).ConfigureAwait(false);

                // Step 5: Check if type is unknown
                if (String.IsNullOrEmpty(detectedType) || String.Equals(detectedType, "unknown", StringComparison.OrdinalIgnoreCase))
                {
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.TypeDetectionFailed, "Document type could not be detected.", token).ConfigureAwait(false);
                    _Logging.Warn(_Header + "type detection failed for document " + documentId);
                    return;
                }

                // Step 6: Update status to TypeDetectionSuccess, then Processing
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.TypeDetectionSuccess, "Detected type: " + detectedType, token).ConfigureAwait(false);
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Processing, "Processing document content.", token).ConfigureAwait(false);

                // Step 7: Call DocumentAtom processing endpoint
                string extractedContent = await ProcessDocumentContentAsync(fileBytes, detectedType, document.OriginalFilename, token).ConfigureAwait(false);
                if (String.IsNullOrEmpty(extractedContent))
                {
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "Failed to extract content from document.", token).ConfigureAwait(false);
                    return;
                }

                _Logging.Debug(_Header + "extracted " + extractedContent.Length + " characters from document " + documentId);

                // Step 8: Update status to ProcessingChunks
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.ProcessingChunks, "Chunking and embedding content.", token).ConfigureAwait(false);

                // Step 9: Call Partio chunking service
                List<ChunkWithEmbedding> chunks = await ChunkAndEmbedContentAsync(extractedContent, token).ConfigureAwait(false);
                if (chunks == null || chunks.Count == 0)
                {
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "Failed to chunk document content.", token).ConfigureAwait(false);
                    return;
                }

                _Logging.Debug(_Header + "generated " + chunks.Count + " chunks for document " + documentId);

                // Step 10: Update status to StoringEmbeddings
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.StoringEmbeddings, "Storing " + chunks.Count + " embeddings.", token).ConfigureAwait(false);

                // Step 11: Store each chunk+embedding in RecallDB
                AssistantDocument refreshedDocument = await _Database.AssistantDocument.ReadAsync(documentId, token).ConfigureAwait(false);
                string collectionId = null;
                if (refreshedDocument != null)
                {
                    AssistantSettings assistantSettings = await _Database.AssistantSettings.ReadAsync(refreshedDocument.AssistantId, token).ConfigureAwait(false);
                    if (assistantSettings != null)
                    {
                        collectionId = assistantSettings.CollectionId;
                    }
                }

                if (String.IsNullOrEmpty(collectionId))
                {
                    await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "No collection identifier configured for this assistant.", token).ConfigureAwait(false);
                    return;
                }

                int storedCount = 0;
                foreach (ChunkWithEmbedding chunk in chunks)
                {
                    bool stored = await StoreEmbeddingAsync(collectionId, documentId, chunk, token).ConfigureAwait(false);
                    if (stored) storedCount++;
                }

                _Logging.Info(_Header + "stored " + storedCount + "/" + chunks.Count + " embeddings for document " + documentId);

                // Step 12: Update status to Completed
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Completed, "Ingestion complete. " + storedCount + " chunks stored.", token).ConfigureAwait(false);
                _Logging.Info(_Header + "ingestion pipeline completed for document " + documentId);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during ingestion of document " + documentId + ": " + e.Message);
                await UpdateDocumentStatusAsync(documentId, DocumentStatusEnum.Failed, "Ingestion failed: " + e.Message, token).ConfigureAwait(false);
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
        /// <param name="fileBytes">Raw file bytes.</param>
        /// <param name="filename">Original filename.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Detected document type string.</returns>
        private async Task<string> DetectDocumentTypeAsync(byte[] fileBytes, string filename, CancellationToken token)
        {
            string url = _DocumentAtomSettings.Endpoint.TrimEnd('/') + "/typedetect";

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                MultipartFormDataContent formData = new MultipartFormDataContent();
                ByteArrayContent fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                formData.Add(fileContent, "file", filename ?? "document");
                request.Content = formData;

                if (!String.IsNullOrEmpty(_DocumentAtomSettings.AccessKey))
                {
                    request.Headers.Add("x-api-key", _DocumentAtomSettings.AccessKey);
                }

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "type detection returned " + (int)response.StatusCode + ": " + responseBody);
                    return null;
                }

                TypeDetectResponse typeResult = JsonSerializer.Deserialize<TypeDetectResponse>(responseBody, _JsonOptions);
                return typeResult?.Type;
            }
        }

        /// <summary>
        /// Process document content using the DocumentAtom service.
        /// </summary>
        /// <param name="fileBytes">Raw file bytes.</param>
        /// <param name="documentType">Detected document type.</param>
        /// <param name="filename">Original filename.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Extracted text content.</returns>
        private async Task<string> ProcessDocumentContentAsync(byte[] fileBytes, string documentType, string filename, CancellationToken token)
        {
            string url = _DocumentAtomSettings.Endpoint.TrimEnd('/') + "/process";

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                MultipartFormDataContent formData = new MultipartFormDataContent();
                ByteArrayContent fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                formData.Add(fileContent, "file", filename ?? "document");
                formData.Add(new StringContent(documentType), "type");
                request.Content = formData;

                if (!String.IsNullOrEmpty(_DocumentAtomSettings.AccessKey))
                {
                    request.Headers.Add("x-api-key", _DocumentAtomSettings.AccessKey);
                }

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "document processing returned " + (int)response.StatusCode + ": " + responseBody);
                    return null;
                }

                ProcessResponse processResult = JsonSerializer.Deserialize<ProcessResponse>(responseBody, _JsonOptions);
                return processResult?.Content;
            }
        }

        /// <summary>
        /// Chunk and embed document content using the Partio chunking service.
        /// </summary>
        /// <param name="content">Extracted text content.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of chunks with their embeddings.</returns>
        private async Task<List<ChunkWithEmbedding>> ChunkAndEmbedContentAsync(string content, CancellationToken token)
        {
            string url = _ChunkingSettings.Endpoint.TrimEnd('/') + "/v1.0/endpoints/" + _ChunkingSettings.EndpointId + "/process";

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                object requestBody = new { Text = content };
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
                    _Logging.Warn(_Header + "chunking service returned " + (int)response.StatusCode + ": " + responseBody);
                    return null;
                }

                ChunkingResponse chunkResult = JsonSerializer.Deserialize<ChunkingResponse>(responseBody, _JsonOptions);
                return chunkResult?.Chunks;
            }
        }

        /// <summary>
        /// Store a single chunk embedding in RecallDB.
        /// </summary>
        /// <param name="collectionId">Collection identifier.</param>
        /// <param name="documentId">Source document identifier.</param>
        /// <param name="chunk">Chunk with embedding data.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the embedding was stored successfully.</returns>
        private async Task<bool> StoreEmbeddingAsync(string collectionId, string documentId, ChunkWithEmbedding chunk, CancellationToken token)
        {
            string url = _RecallDbSettings.Endpoint.TrimEnd('/') + "/v1.0/collections/" + collectionId + "/documents";

            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url))
                {
                    object requestBody = new
                    {
                        Content = chunk.Content,
                        Embeddings = chunk.Embeddings,
                        Metadata = new
                        {
                            SourceDocumentId = documentId,
                            ChunkIndex = chunk.Index
                        }
                    };

                    string json = JsonSerializer.Serialize(requestBody, _JsonOptions);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    if (!String.IsNullOrEmpty(_RecallDbSettings.AccessKey))
                    {
                        request.Headers.Add("x-api-key", _RecallDbSettings.AccessKey);
                    }

                    HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                        _Logging.Warn(_Header + "RecallDB store returned " + (int)response.StatusCode + ": " + responseBody);
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception storing embedding: " + e.Message);
                return false;
            }
        }

        #endregion

        #region Private-Classes

        /// <summary>
        /// Type detection response from DocumentAtom.
        /// </summary>
        private class TypeDetectResponse
        {
            /// <summary>
            /// Detected document type.
            /// </summary>
            public string Type { get; set; } = null;
        }

        /// <summary>
        /// Processing response from DocumentAtom.
        /// </summary>
        private class ProcessResponse
        {
            /// <summary>
            /// Extracted text content.
            /// </summary>
            public string Content { get; set; } = null;
        }

        /// <summary>
        /// Chunking response from the Partio service.
        /// </summary>
        private class ChunkingResponse
        {
            /// <summary>
            /// List of chunks with embeddings.
            /// </summary>
            public List<ChunkWithEmbedding> Chunks { get; set; } = null;
        }

        /// <summary>
        /// A single chunk with its embedding vector.
        /// </summary>
        private class ChunkWithEmbedding
        {
            /// <summary>
            /// Chunk index.
            /// </summary>
            public int Index { get; set; } = 0;

            /// <summary>
            /// Text content of the chunk.
            /// </summary>
            public string Content { get; set; } = null;

            /// <summary>
            /// Embedding vector for the chunk.
            /// </summary>
            public List<double> Embeddings { get; set; } = null;
        }

        #endregion
    }
}
