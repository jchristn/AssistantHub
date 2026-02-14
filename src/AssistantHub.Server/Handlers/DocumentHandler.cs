namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
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
        public DocumentHandler(
            DatabaseDriverBase database,
            LoggingModule logging,
            AssistantHubSettings settings,
            AuthenticationService authentication,
            StorageService storage,
            IngestionService ingestion,
            RetrievalService retrieval,
            InferenceService inference)
            : base(database, logging, settings, authentication, storage, ingestion, retrieval, inference)
        {
        }

        /// <summary>
        /// PUT /v1.0/documents - Upload document and trigger fire-and-forget ingestion.
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

                string assistantId = ctx.Request.Query.Elements.Get("assistantId");
                if (String.IsNullOrEmpty(assistantId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "assistantId query parameter is required."))).ConfigureAwait(false);
                    return;
                }

                Assistant assistant = await Database.Assistant.ReadAsync(assistantId).ConfigureAwait(false);
                if (assistant == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.NotFound, null, "Assistant not found."))).ConfigureAwait(false);
                    return;
                }

                if (!isAdmin && assistant.UserId != user.Id)
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                    return;
                }

                byte[] data = ctx.Request.DataAsBytes;
                if (data == null || data.Length == 0)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, null, "Request body must contain file data."))).ConfigureAwait(false);
                    return;
                }

                // Get filename from query parameter or Content-Disposition header
                string filename = ctx.Request.Query.Elements.Get("filename");
                if (String.IsNullOrEmpty(filename))
                {
                    string contentDisposition = ctx.Request.Headers.Get("Content-Disposition");
                    if (!String.IsNullOrEmpty(contentDisposition))
                    {
                        int filenameIdx = contentDisposition.IndexOf("filename=", StringComparison.OrdinalIgnoreCase);
                        if (filenameIdx >= 0)
                        {
                            filename = contentDisposition.Substring(filenameIdx + 9).Trim(' ', '"', '\'');
                        }
                    }
                }

                if (String.IsNullOrEmpty(filename))
                {
                    filename = "upload_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                }

                string contentType = ctx.Request.ContentType;
                if (String.IsNullOrEmpty(contentType))
                {
                    contentType = "application/octet-stream";
                }

                // Create the document record
                AssistantDocument doc = new AssistantDocument();
                doc.Id = IdGenerator.NewAssistantDocumentId();
                doc.AssistantId = assistantId;
                doc.Name = filename;
                doc.OriginalFilename = filename;
                doc.ContentType = contentType;
                doc.SizeBytes = data.Length;
                doc.S3Key = assistantId + "/" + doc.Id + "/" + filename;
                doc.Status = Enums.DocumentStatusEnum.Uploading;
                doc.CreatedUtc = DateTime.UtcNow;
                doc.LastUpdateUtc = DateTime.UtcNow;

                doc = await Database.AssistantDocument.CreateAsync(doc).ConfigureAwait(false);

                // Upload to storage
                try
                {
                    await Storage.UploadAsync(doc.S3Key, contentType, data).ConfigureAwait(false);
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
        /// GET /v1.0/documents - List documents (non-admins see only their assistants' documents).
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task GetDocumentsAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

                EnumerationQuery query = BuildEnumerationQuery(ctx);
                EnumerationResult<AssistantDocument> result = await Database.AssistantDocument.EnumerateAsync(query).ConfigureAwait(false);

                // Non-admin users: filter to only their assistants' documents
                if (!isAdmin && result != null && result.Objects != null)
                {
                    List<AssistantDocument> filtered = new List<AssistantDocument>();
                    foreach (AssistantDocument doc in result.Objects)
                    {
                        Assistant assistant = await Database.Assistant.ReadAsync(doc.AssistantId).ConfigureAwait(false);
                        if (assistant != null && assistant.UserId == user.Id)
                        {
                            filtered.Add(doc);
                        }
                    }
                    result.Objects = filtered;
                }

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
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

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

                if (!isAdmin)
                {
                    Assistant assistant = await Database.Assistant.ReadAsync(doc.AssistantId).ConfigureAwait(false);
                    if (assistant == null || assistant.UserId != user.Id)
                    {
                        ctx.Response.StatusCode = 403;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                        return;
                    }
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
        /// DELETE /v1.0/documents/{documentId} - Delete document and S3 object.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task DeleteDocumentAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            try
            {
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

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

                if (!isAdmin)
                {
                    Assistant assistant = await Database.Assistant.ReadAsync(doc.AssistantId).ConfigureAwait(false);
                    if (assistant == null || assistant.UserId != user.Id)
                    {
                        ctx.Response.StatusCode = 403;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.Send(Serializer.SerializeJson(new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed))).ConfigureAwait(false);
                        return;
                    }
                }

                // Delete from storage if S3 key is set
                if (Storage != null && !String.IsNullOrEmpty(doc.S3Key))
                {
                    try
                    {
                        await Storage.DeleteAsync(doc.S3Key).ConfigureAwait(false);
                    }
                    catch (Exception storageEx)
                    {
                        Logging.Warn(_Header + "failed to delete S3 object " + doc.S3Key + ": " + storageEx.Message);
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
                UserMaster user = GetUser(ctx);
                bool isAdmin = IsAdmin(ctx);

                string documentId = ctx.Request.Url.Parameters["documentId"];
                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                AssistantDocument doc = await Database.AssistantDocument.ReadAsync(documentId).ConfigureAwait(false);
                if (doc == null)
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                }

                if (!isAdmin)
                {
                    Assistant assistant = await Database.Assistant.ReadAsync(doc.AssistantId).ConfigureAwait(false);
                    if (assistant == null || assistant.UserId != user.Id)
                    {
                        ctx.Response.StatusCode = 403;
                        await ctx.Response.Send().ConfigureAwait(false);
                        return;
                    }
                }

                ctx.Response.StatusCode = 200;
                await ctx.Response.Send().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.Warn(_Header + "exception in HeadDocumentAsync: " + e.Message);
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send().ConfigureAwait(false);
            }
        }
    }
}
