namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
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
            StorageService storage,
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

                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

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

                // Create the document record
                AssistantDocument doc = new AssistantDocument();
                doc.Id = IdGenerator.NewAssistantDocumentId();
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
                EnumerationQuery query = BuildEnumerationQuery(ctx);
                EnumerationResult<AssistantDocument> result = await Database.AssistantDocument.EnumerateAsync(query).ConfigureAwait(false);

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
                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                AssistantDocument doc = await Database.AssistantDocument.ReadAsync(documentId).ConfigureAwait(false);
                if (doc == null)
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
                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                AssistantDocument doc = await Database.AssistantDocument.ReadAsync(documentId).ConfigureAwait(false);
                if (doc == null)
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
                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                AssistantDocument doc = await Database.AssistantDocument.ReadAsync(documentId).ConfigureAwait(false);
                if (doc == null)
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

                // Delete embeddings from RecallDB
                if (!String.IsNullOrEmpty(doc.ChunkRecordIds) && !String.IsNullOrEmpty(doc.CollectionId) && Ingestion != null)
                {
                    try
                    {
                        List<string> recordIds = JsonSerializer.Deserialize<List<string>>(doc.ChunkRecordIds);
                        if (recordIds != null)
                        {
                            foreach (string recordId in recordIds)
                            {
                                try
                                {
                                    await Ingestion.DeleteEmbeddingAsync(doc.CollectionId, recordId).ConfigureAwait(false);
                                }
                                catch (Exception embeddingEx)
                                {
                                    Logging.Warn(_Header + "failed to delete embedding " + recordId + ": " + embeddingEx.Message);
                                }
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Logging.Warn(_Header + "failed to parse chunk record IDs for document " + documentId + ": " + parseEx.Message);
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
        /// HEAD /v1.0/documents/{documentId} - Check document existence.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task HeadDocumentAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                bool exists = await Database.AssistantDocument.ExistsAsync(documentId).ConfigureAwait(false);
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
                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest))).ConfigureAwait(false);
                    return;
                }

                bool exists = await Database.AssistantDocument.ExistsAsync(documentId).ConfigureAwait(false);
                if (!exists)
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

        private class DocumentUploadRequest
        {
            public string IngestionRuleId { get; set; } = null;
            public string Name { get; set; } = null;
            public string OriginalFilename { get; set; } = null;
            public string ContentType { get; set; } = null;
            public List<string> Labels { get; set; } = null;
            public Dictionary<string, string> Tags { get; set; } = null;
            public string Base64Content { get; set; } = null;
        }

        #endregion
    }
}
