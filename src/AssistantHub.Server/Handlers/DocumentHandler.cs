namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.Json;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using WatsonWebserver.Core;
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;

    /// <summary>
    /// Handles document upload, CRUD, and existence check routes.
    /// </summary>
    public class DocumentHandler : HandlerBase
    {
        private static readonly string _Header = "[DocumentHandler] ";

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">Application settings.</param>
        /// <param name="authentication">Authentication service.</param>
        /// <param name="storage">Storage service.</param>
        /// <param name="ingestion">Ingestion service.</param>
        /// <param name="retrieval">Retrieval service.</param>
        /// <param name="inference">Inference service.</param>
        /// <param name="processingLog">Processing log service.</param>
        public DocumentHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            IObjectStorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference,
            ProcessingLogService processingLog = null)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference, processingLog)
        {
        }

        /// <summary>
        /// PUT /v1.0/documents - Upload document via JSON body with IngestionRuleId.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task PutDocumentAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                if (Storage == null || Ingestion == null)
                {
                    ctx.Response.StatusCode = 503;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError, null, "Document upload is unavailable. S3 storage is not configured."))).ConfigureAwait(false);
                    return;
                }

                AuthContext auth = RequireAuth(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthenticationFailed))).ConfigureAwait(false);
                    return;
                }

                string body = ctx.Request.DataAsString;
                if (String.IsNullOrEmpty(body))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Request body is required."))).ConfigureAwait(false);
                    return;
                }

                DocumentUploadRequest uploadRequest = Serializer.DeserializeJson<DocumentUploadRequest>(body);
                if (uploadRequest == null || String.IsNullOrEmpty(uploadRequest.IngestionRuleId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "IngestionRuleId is required."))).ConfigureAwait(false);
                    return;
                }

                // Look up ingestion rule
                IngestionRule rule = await Database.IngestionRule.ReadAsync(uploadRequest.IngestionRuleId).ConfigureAwait(false);
                if (rule == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "Ingestion rule not found."))).ConfigureAwait(false);
                    return;
                }

                // Decode base64 content
                if (String.IsNullOrEmpty(uploadRequest.Base64Content))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Base64Content is required."))).ConfigureAwait(false);
                    return;
                }

                byte[] data;
                try
                {
                    data = Convert.FromBase64String(uploadRequest.Base64Content);
                }
                catch (FormatException)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Invalid Base64Content."))).ConfigureAwait(false);
                    return;
                }

                if (data.Length == 0)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "File data is empty."))).ConfigureAwait(false);
                    return;
                }

                string filename = uploadRequest.Name ?? uploadRequest.OriginalFilename ?? ("upload_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
                string contentType = uploadRequest.ContentType ?? "application/octet-stream";

                // Enforce tenant ownership on the ingestion rule
                if (!EnforceTenantOwnership(auth, rule.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "Ingestion rule not found."))).ConfigureAwait(false);
                    return;
                }

                // Create the document record
                AssistantDocument doc = new AssistantDocument();
                doc.Id = IdGenerator.NewAssistantDocumentId();
                doc.TenantId = auth.TenantId;
                doc.Name = filename;
                doc.OriginalFilename = uploadRequest.OriginalFilename ?? filename;
                doc.ContentType = contentType;
                doc.SizeBytes = data.Length;
                doc.IngestionRuleId = rule.Id;
                doc.BucketName = rule.Bucket;
                doc.CollectionId = rule.CollectionId;
                doc.S3Key = rule.Id + "/" + doc.Id + "/" + filename;
                doc.Status = Enums.DocumentStatusEnum.Uploading;

                // Store user-provided labels/tags as JSON
                if (uploadRequest.Labels != null && uploadRequest.Labels.Count > 0)
                    doc.Labels = Serializer.SerializeJson(uploadRequest.Labels);
                if (uploadRequest.Tags != null && uploadRequest.Tags.Count > 0)
                    doc.Tags = Serializer.SerializeJson(uploadRequest.Tags);

                doc.CreatedUtc = DateTime.UtcNow;
                doc.LastUpdateUtc = DateTime.UtcNow;

                doc = await Database.AssistantDocument.CreateAsync(doc).ConfigureAwait(false);

                // Upload to storage
                try
                {
                    await Storage.UploadAsync(rule.Bucket, doc.S3Key, contentType, data).ConfigureAwait(false);
                    await Database.AssistantDocument.UpdateStatusAsync(doc.Id, Enums.DocumentStatusEnum.Uploaded, "File uploaded successfully.").ConfigureAwait(false);
                    doc.Status = Enums.DocumentStatusEnum.Uploaded;
                    doc.StatusMessage = "File uploaded successfully.";
                }
                catch (Exception uploadEx)
                {
                    Logging.Warn(_Header + "upload failed for document " + doc.Id + ": " + uploadEx.Message);
                    await Database.AssistantDocument.UpdateStatusAsync(doc.Id, Enums.DocumentStatusEnum.Failed, "Upload failed: " + uploadEx.Message).ConfigureAwait(false);
                    doc.Status = Enums.DocumentStatusEnum.Failed;
                    doc.StatusMessage = "Upload failed: " + uploadEx.Message;
                }

                // Trigger ingestion asynchronously (fire-and-forget)
                if (doc.Status == Enums.DocumentStatusEnum.Uploaded)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Ingestion.ProcessDocumentAsync(doc.Id).ConfigureAwait(false);
                        }
                        catch (Exception ingestionEx)
                        {
                            Logging.Warn(_Header + "ingestion failed for document " + doc.Id + ": " + ingestionEx.Message);
                            await Database.AssistantDocument.UpdateStatusAsync(doc.Id, Enums.DocumentStatusEnum.Failed, "Ingestion failed: " + ingestionEx.Message).ConfigureAwait(false);
                        }
                    });
                }

                ctx.Response.StatusCode = 201;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(doc)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in PutDocumentAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/documents - List documents.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetDocumentsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = GetAuthContext(ctx);
                EnumerationQuery query = BuildEnumerationQuery(ctx);
                EnumerationResult<AssistantDocument> result = await Database.AssistantDocument.EnumerateAsync(auth.TenantId, query).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetDocumentsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/documents/{documentId} - Get document by ID.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetDocumentAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = GetAuthContext(ctx);

                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                AssistantDocument doc = await Database.AssistantDocument.ReadAsync(documentId).ConfigureAwait(false);
                if (doc == null || !EnforceTenantOwnership(auth, doc.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(doc)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetDocumentAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/documents/{documentId}/download - Download document content from S3.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DownloadDocumentAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = GetAuthContext(ctx);

                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                AssistantDocument doc = await Database.AssistantDocument.ReadAsync(documentId).ConfigureAwait(false);
                if (doc == null || !EnforceTenantOwnership(auth, doc.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                if (String.IsNullOrEmpty(doc.S3Key) || String.IsNullOrEmpty(doc.BucketName))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                byte[] data = await Storage.DownloadAsync(doc.BucketName, doc.S3Key).ConfigureAwait(false);
                if (data == null || data.Length == 0)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                string filename = doc.OriginalFilename ?? doc.Name ?? "document";
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = doc.ContentType ?? "application/octet-stream";
                ctx.Response.Headers.Add("Content-Disposition", "attachment; filename=\"" + filename + "\"");
                await ctx.Response.Send(data).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DownloadDocumentAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DELETE /v1.0/documents/{documentId} - Delete document, S3 object, and RecallDB embeddings.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteDocumentAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = GetAuthContext(ctx);

                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                AssistantDocument doc = await Database.AssistantDocument.ReadAsync(documentId).ConfigureAwait(false);
                if (doc == null || !EnforceTenantOwnership(auth, doc.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                // Delete from storage
                if (Storage != null && !String.IsNullOrEmpty(doc.S3Key))
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(doc.BucketName))
                            await Storage.DeleteAsync(doc.BucketName, doc.S3Key).ConfigureAwait(false);
                        else
                            await Storage.DeleteAsync(doc.S3Key).ConfigureAwait(false);
                    }
                    catch (Exception storageEx)
                    {
                        Logging.Warn(_Header + "failed to delete S3 object " + doc.S3Key + ": " + storageEx.Message);
                    }
                }

                // Delete embeddings from RecallDB (batch)
                if (!String.IsNullOrEmpty(doc.ChunkRecordIds) && !String.IsNullOrEmpty(doc.CollectionId) && Ingestion != null)
                {
                    try
                    {
                        List<string> recordIds = JsonSerializer.Deserialize<List<string>>(doc.ChunkRecordIds);
                        if (recordIds != null && recordIds.Count > 0)
                        {
                            await Ingestion.DeleteEmbeddingBatchAsync(doc.TenantId, doc.CollectionId, recordIds).ConfigureAwait(false);
                        }
                    }
                    catch (Exception embeddingEx)
                    {
                        Logging.Warn(_Header + "failed to batch delete embeddings for document " + documentId + ": " + embeddingEx.Message);
                    }
                }

                // Delete indexed text from Verbex
                if (Ingestion != null)
                {
                    try
                    {
                        string indexId = String.IsNullOrEmpty(doc.VerbexIndexId) ? Settings.Verbex.DefaultIndexId : doc.VerbexIndexId;
                        string recordId = String.IsNullOrEmpty(doc.VerbexRecordId) ? doc.Id : doc.VerbexRecordId;
                        await Ingestion.DeleteIndexRecordAsync(doc.TenantId, indexId, recordId).ConfigureAwait(false);
                    }
                    catch (Exception indexEx)
                    {
                        Logging.Warn(_Header + "failed to delete Verbex record for document " + documentId + " in tenant " + doc.TenantId + ": " + indexEx.Message);
                    }
                }

                await Database.AssistantDocument.DeleteAsync(documentId).ConfigureAwait(false);

                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in DeleteDocumentAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/documents/delete - Bulk delete multiple documents.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task BulkDeleteDocumentsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = GetAuthContext(ctx);

                string body = ctx.Request.DataAsString;
                if (String.IsNullOrEmpty(body))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                BulkDeleteRequest? request = JsonSerializer.Deserialize<BulkDeleteRequest>(body);
                if (request == null || request.DocumentIds == null || request.DocumentIds.Count == 0)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                // Read all documents and validate tenant ownership
                List<AssistantDocument> docs = new List<AssistantDocument>();
                foreach (string docId in request.DocumentIds)
                {
                    AssistantDocument doc = await Database.AssistantDocument.ReadAsync(docId).ConfigureAwait(false);
                    if (doc != null && EnforceTenantOwnership(auth, doc.TenantId))
                        docs.Add(doc);
                }

                // Group chunk record IDs by collection for batch delete
                Dictionary<string, (string TenantId, string CollectionId, List<string> RecordIds)> recordIdsByCollection = new Dictionary<string, (string TenantId, string CollectionId, List<string> RecordIds)>();
                Dictionary<string, (string TenantId, string IndexId, List<string> RecordIds)> recordIdsByIndex = new Dictionary<string, (string TenantId, string IndexId, List<string> RecordIds)>();
                foreach (AssistantDocument doc in docs)
                {
                    if (!String.IsNullOrEmpty(doc.ChunkRecordIds) && !String.IsNullOrEmpty(doc.CollectionId))
                    {
                        try
                        {
                            List<string> recordIds = JsonSerializer.Deserialize<List<string>>(doc.ChunkRecordIds);
                            if (recordIds != null && recordIds.Count > 0)
                            {
                                string collectionKey = doc.TenantId + "|" + doc.CollectionId;
                                if (!recordIdsByCollection.ContainsKey(collectionKey))
                                    recordIdsByCollection[collectionKey] = (doc.TenantId, doc.CollectionId, new List<string>());
                                recordIdsByCollection[collectionKey].RecordIds.AddRange(recordIds);
                            }
                        }
                        catch (Exception parseEx)
                        {
                            Logging.Warn(_Header + "failed to parse chunk record IDs for document " + doc.Id + ": " + parseEx.Message);
                        }
                    }

                    string indexId = String.IsNullOrEmpty(doc.VerbexIndexId) ? Settings.Verbex.DefaultIndexId : doc.VerbexIndexId;
                    string recordId = String.IsNullOrEmpty(doc.VerbexRecordId) ? doc.Id : doc.VerbexRecordId;
                    string indexKey = doc.TenantId + "|" + indexId;
                    if (!recordIdsByIndex.ContainsKey(indexKey))
                        recordIdsByIndex[indexKey] = (doc.TenantId, indexId, new List<string>());
                    recordIdsByIndex[indexKey].RecordIds.Add(recordId);
                }

                // Batch delete embeddings per collection
                if (Ingestion != null)
                {
                    foreach (KeyValuePair<string, (string TenantId, string CollectionId, List<string> RecordIds)> kvp in recordIdsByCollection)
                    {
                        try
                        {
                            await Ingestion.DeleteEmbeddingBatchAsync(kvp.Value.TenantId, kvp.Value.CollectionId, kvp.Value.RecordIds).ConfigureAwait(false);
                        }
                        catch (Exception embeddingEx)
                        {
                            Logging.Warn(_Header + "failed to batch delete embeddings for tenant " + kvp.Value.TenantId + " collection " + kvp.Value.CollectionId + ": " + embeddingEx.Message);
                        }
                    }
                }

                // Batch delete indexed text per Verbex index
                if (Ingestion != null)
                {
                    foreach (KeyValuePair<string, (string TenantId, string IndexId, List<string> RecordIds)> kvp in recordIdsByIndex)
                    {
                        try
                        {
                            await Ingestion.DeleteIndexRecordBatchAsync(kvp.Value.TenantId, kvp.Value.IndexId, kvp.Value.RecordIds).ConfigureAwait(false);
                        }
                        catch (Exception indexEx)
                        {
                            Logging.Warn(_Header + "failed to batch delete Verbex records for tenant " + kvp.Value.TenantId + " index " + kvp.Value.IndexId + ": " + indexEx.Message);
                        }
                    }
                }

                // Delete S3 objects and DB records
                foreach (AssistantDocument doc in docs)
                {
                    if (Storage != null && !String.IsNullOrEmpty(doc.S3Key))
                    {
                        try
                        {
                            if (!String.IsNullOrEmpty(doc.BucketName))
                                await Storage.DeleteAsync(doc.BucketName, doc.S3Key).ConfigureAwait(false);
                            else
                                await Storage.DeleteAsync(doc.S3Key).ConfigureAwait(false);
                        }
                        catch (Exception storageEx)
                        {
                            Logging.Warn(_Header + "failed to delete S3 object " + doc.S3Key + ": " + storageEx.Message);
                        }
                    }

                    await Database.AssistantDocument.DeleteAsync(doc.Id).ConfigureAwait(false);
                }

                ctx.Response.StatusCode = 204;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in BulkDeleteDocumentsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/documents/{documentId}/reindex - Reindex a single document into Verbex.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task ReindexDocumentAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "DocumentId is required."))).ConfigureAwait(false);
                    return;
                }

                AssistantDocument doc = await Database.AssistantDocument.ReadAsync(documentId).ConfigureAwait(false);
                if (doc == null || !EnforceTenantOwnership(auth, doc.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                DocumentReindexResult result = await Ingestion.ReindexDocumentTextAsync(doc).ConfigureAwait(false);
                ctx.Response.StatusCode = result.Success ? 200 : 502;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(result)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in ReindexDocumentAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// POST /v1.0/documents/reindex - Reindex completed documents into Verbex.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task ReindexDocumentsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            Stopwatch sw = Stopwatch.StartNew();
            DocumentReindexBatchResult batch = new DocumentReindexBatchResult();

            try
            {
                AuthContext auth = RequireAdmin(ctx);
                if (auth == null)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                DocumentReindexRequest request = null;
                string body = ctx.Request.DataAsString;
                if (!String.IsNullOrWhiteSpace(body))
                    request = Serializer.DeserializeJson<DocumentReindexRequest>(body);
                request ??= new DocumentReindexRequest();

                List<AssistantDocument> docs = new List<AssistantDocument>();

                if (request.DocumentIds != null && request.DocumentIds.Count > 0)
                {
                    batch.Requested = request.DocumentIds.Count;

                    foreach (string documentId in request.DocumentIds)
                    {
                        if (String.IsNullOrWhiteSpace(documentId)) continue;

                        AssistantDocument doc = await Database.AssistantDocument.ReadAsync(documentId).ConfigureAwait(false);
                        if (doc == null || !EnforceTenantOwnership(auth, doc.TenantId))
                        {
                            batch.Results.Add(new DocumentReindexResult
                            {
                                DocumentId = documentId,
                                Success = false,
                                Status = "NotFound",
                                Message = "Document not found."
                            });
                            batch.Failed++;
                            continue;
                        }

                        docs.Add(doc);
                    }

                    batch.EndOfResults = true;
                }
                else
                {
                    EnumerationQuery query = BuildEnumerationQuery(ctx);
                    EnumerationResult<AssistantDocument> page = await Database.AssistantDocument.EnumerateAsync(auth.TenantId, query).ConfigureAwait(false);
                    docs.AddRange(page.Objects);
                    batch.Requested = docs.Count;
                    batch.ContinuationToken = page.ContinuationToken;
                    batch.EndOfResults = page.EndOfResults;
                }

                foreach (AssistantDocument doc in docs)
                {
                    if (doc.Status != Enums.DocumentStatusEnum.Completed)
                    {
                        batch.Skipped++;
                        batch.Results.Add(new DocumentReindexResult
                        {
                            DocumentId = doc.Id,
                            Success = true,
                            Status = "Skipped",
                            Message = "Document status is " + doc.Status + "; only completed documents are batch reindexed.",
                            VerbexTenantId = doc.VerbexTenantId,
                            VerbexIndexId = doc.VerbexIndexId,
                            VerbexRecordId = doc.VerbexRecordId
                        });
                        continue;
                    }

                    if (!request.IncludeAlreadyIndexed && !String.IsNullOrEmpty(doc.VerbexRecordId))
                    {
                        batch.Skipped++;
                        batch.Results.Add(new DocumentReindexResult
                        {
                            DocumentId = doc.Id,
                            Success = true,
                            Status = "Skipped",
                            Message = "Document already has Verbex indexing metadata.",
                            VerbexTenantId = doc.VerbexTenantId,
                            VerbexIndexId = doc.VerbexIndexId,
                            VerbexRecordId = doc.VerbexRecordId
                        });
                        continue;
                    }

                    batch.Eligible++;
                    DocumentReindexResult result = await Ingestion.ReindexDocumentTextAsync(doc).ConfigureAwait(false);
                    batch.Results.Add(result);

                    if (String.Equals(result.Status, "Reindexed", StringComparison.OrdinalIgnoreCase))
                        batch.Reindexed++;
                    else if (String.Equals(result.Status, "Skipped", StringComparison.OrdinalIgnoreCase))
                        batch.Skipped++;
                    else
                        batch.Failed++;
                }

                sw.Stop();
                batch.TotalMs = sw.Elapsed.TotalMilliseconds;

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(batch)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in ReindexDocumentsAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// HEAD /v1.0/documents/{documentId} - Check document existence.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task HeadDocumentAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = GetAuthContext(ctx);

                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                AssistantDocument doc = await Database.AssistantDocument.ReadAsync(documentId).ConfigureAwait(false);
                bool exists = doc != null && EnforceTenantOwnership(auth, doc.TenantId);
                ctx.Response.StatusCode = exists ? 200 : 404;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in HeadDocumentAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// GET /v1.0/documents/{documentId}/processing-log - Get processing log for a document.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetDocumentProcessingLogAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                AuthContext auth = GetAuthContext(ctx);

                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                AssistantDocument doc = await Database.AssistantDocument.ReadAsync(documentId).ConfigureAwait(false);
                if (doc == null || !EnforceTenantOwnership(auth, doc.TenantId))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound))).ConfigureAwait(false);
                    return;
                }

                string log = null;
                if (ProcessingLog != null)
                {
                    log = await ProcessingLog.GetLogAsync(documentId).ConfigureAwait(false);
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new { DocumentId = documentId, Log = log })).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in GetDocumentProcessingLogAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.InternalError))).ConfigureAwait(false);
            }
        }

        #region Private-Classes

        #endregion
    }
}
