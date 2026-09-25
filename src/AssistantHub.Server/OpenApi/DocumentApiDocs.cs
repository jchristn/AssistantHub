namespace AssistantHub.Server.OpenApi
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Server.Handlers;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for document routes and ingestion analytics.
    /// </summary>
    public static class DocumentApiDocs
    {
        #region Private-Members

        private const string _Tag = "Documents";

        private const string _RuleId = "irule_01JH3ZD1A3B5C7D9E1F3G5H7J9";

        private const string _Bucket = ApiExamples.TenantId + "_kb-documents";

        private const string _Filename = "employee-handbook.pdf";

        private const string _SecondDocumentId = "adoc_01JH3ZE2B4C6D8E0F2G4H6J8K0";

        private const string _Tenancy = "Documents are tenant-scoped: a document owned by another tenant is reported as not found, except to global administrators. ";

        private static AssistantDocument ExampleDocument(DocumentStatusEnum status, string statusMessage)
        {
            bool indexed = status == DocumentStatusEnum.Completed;

            return new AssistantDocument
            {
                Id = ApiExamples.DocumentId,
                TenantId = ApiExamples.TenantId,
                Name = _Filename,
                OriginalFilename = _Filename,
                ContentType = "application/pdf",
                SizeBytes = 1048576,
                S3Key = _RuleId + "/" + ApiExamples.DocumentId + "/" + _Filename,
                Status = status,
                StatusMessage = statusMessage,
                IngestionRuleId = _RuleId,
                BucketName = _Bucket,
                CollectionId = ApiExamples.CollectionId,
                VerbexTenantId = indexed ? ApiExamples.TenantId : null,
                VerbexIndexId = indexed ? "default" : null,
                VerbexRecordId = indexed ? ApiExamples.DocumentId : null,
                Labels = "[\"handbook\",\"hr\"]",
                Tags = "{\"version\":\"2.0\"}",
                ChunkRecordIds = indexed ? "[\"doc_01JH3ZF1A2B3C4D5E6F7G8H9J0\",\"doc_01JH3ZF2B3C4D5E6F7G8H9J0K1\"]" : null,
                CrawlPlanId = null,
                CrawlOperationId = null,
                SourceUrl = null,
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = indexed ? ApiExamples.Updated : ApiExamples.Created
            };
        }

        private static AssistantDocument ExampleUploadedDocument()
        {
            return ExampleDocument(DocumentStatusEnum.Uploaded, "File uploaded successfully.");
        }

        private static AssistantDocument ExampleCompletedDocument()
        {
            return ExampleDocument(DocumentStatusEnum.Completed, "Document processing completed.");
        }

        private static DocumentUploadRequest ExampleUploadRequest()
        {
            return new DocumentUploadRequest
            {
                IngestionRuleId = _RuleId,
                Name = _Filename,
                OriginalFilename = _Filename,
                ContentType = "application/pdf",
                Labels = new List<string> { "handbook", "hr" },
                Tags = new Dictionary<string, string> { { "version", "2.0" } },
                Base64Content = "JVBERi0xLjQKJcfsj6IKMSAwIG9iago8PC9UeXBlL0NhdGFsb2c+PgplbmRvYmoK"
            };
        }

        private static DocumentReindexResult ExampleReindexResult()
        {
            return new DocumentReindexResult
            {
                DocumentId = ApiExamples.DocumentId,
                Success = true,
                Status = "Reindexed",
                Message = "Document text reindexed into Verbex.",
                VerbexTenantId = ApiExamples.TenantId,
                VerbexIndexId = "default",
                VerbexRecordId = ApiExamples.DocumentId,
                TotalMs = 842.7
            };
        }

        private static List<DocumentPerformanceEvent> ExamplePerformanceEvents()
        {
            DateTime start = ApiExamples.Created;
            return new List<DocumentPerformanceEvent>
            {
                ExamplePerformanceEvent("dpe_01JH3ZG1A3B5C7D9E1F3G5H7J9", 0, "File download from S3", "1048576 bytes", start, 38.2),
                ExamplePerformanceEvent("dpe_01JH3ZG2B4C6D8E0F2G4H6J8K0", 1, "Type detection", "detected type: Pdf", start.AddMilliseconds(38.2), 12.6),
                ExamplePerformanceEvent("dpe_01JH3ZG3C5D7E9F1G3H5J7K9M1", 2, "Atom extraction", "48213 characters extracted", start.AddMilliseconds(50.8), 812.4),
                ExamplePerformanceEvent("dpe_01JH3ZG4D6E8F0G2H4J6K8M0N2", 3, "Chunking", "18 chunks generated", start.AddMilliseconds(863.2), 1331.2),
                ExamplePerformanceEvent("dpe_01JH3ZG5E7F9G1H3J5K7M9N1P3", 4, "Embedding storage", "18/18 stored", start.AddMilliseconds(2194.4), 204.9)
            };
        }

        private static DocumentPerformanceEvent ExamplePerformanceEvent(string id, int sequence, string stage, string detail, DateTime startedUtc, double durationMs)
        {
            DateTime finishedUtc = startedUtc.AddMilliseconds(durationMs);
            return new DocumentPerformanceEvent
            {
                Id = id,
                TenantId = ApiExamples.TenantId,
                DocumentId = ApiExamples.DocumentId,
                IngestionRuleId = _RuleId,
                SequenceNumber = sequence,
                Stage = stage,
                Detail = detail,
                StartedUtc = startedUtc,
                FinishedUtc = finishedUtc,
                DurationMs = durationMs,
                Success = true,
                ErrorMessage = null,
                CreatedUtc = finishedUtc
            };
        }

        #endregion

        #region Public-Members

        /// <summary>PUT /v1.0/documents.</summary>
        public static OpenApiRouteMetadata Upload => ApiDoc.Create("Upload document", _Tag)
            .Describe("Uploads a document as a JSON body with base64-encoded content and an ingestion rule. The rule must belong to the caller's tenant (global administrators may use any rule) and determines the storage bucket, RecallDB collection, and processing configuration. "
                + "The document record is created in the caller's tenant with status Uploading, the file is written to object storage under {ruleId}/{documentId}/{name}, and the status becomes Uploaded (or Failed if storage rejects it; the record is still returned with 201). "
                + "When the upload succeeds the ingestion pipeline (type detection, extraction, Verbex indexing, chunking, summarization, embedding) starts asynchronously; poll the document or its processing log for progress. "
                + "Name defaults to OriginalFilename (or upload_{timestamp}); ContentType defaults to application/octet-stream. Labels and Tags are stored on the record as JSON strings. Returns 503 when object storage is not configured.")
            .Body("Upload request. IngestionRuleId and Base64Content are required.", ExampleUploadRequest())
            .Returns(201, "Document created; Status is Uploaded (ingestion started) or Failed (storage upload failed).", ExampleUploadedDocument())
            .Errors(400, 401, 404, 500)
            .Error(503, "Object storage is not configured.", new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Document upload is unavailable. S3 storage is not configured."));

        /// <summary>GET /v1.0/documents.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List documents", _Tag)
            .Describe("Lists documents in the caller's tenant (for global administrators as well), optionally filtered by bucket and collection. The continuation token is the numeric offset of the next page.")
            .Paged()
            .Query("bucketName", "string", "Only return documents stored in this bucket.", false, _Bucket)
            .Query("collectionId", "string", "Only return documents embedded into this RecallDB collection.", false, ApiExamples.CollectionId)
            .Returns(200, "Page of documents.", ApiExamples.Page(ExampleCompletedDocument()))
            .Errors(401, 500);

        /// <summary>GET /v1.0/documents/{documentId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get document", _Tag)
            .Describe("Returns one document record, including its processing status. " + _Tenancy)
            .Returns(200, "Document.", ExampleCompletedDocument())
            .Errors(400, 401, 404, 500);

        /// <summary>DELETE /v1.0/documents/{documentId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete document", _Tag)
            .Describe("Deletes a document: its stored object, its RecallDB chunk embeddings, its Verbex text-search record, the document record, and its ingestion performance events. "
                + "Failures while cleaning up subordinate records are logged and do not stop the deletion. " + _Tenancy)
            .ReturnsNoContent(204, "Document deleted.")
            .Errors(400, 401, 404, 500);

        /// <summary>HEAD /v1.0/documents/{documentId}.</summary>
        public static OpenApiRouteMetadata Exists => ApiDoc.Create("Check document existence", _Tag)
            .Describe("Returns 200 when the document exists and the caller can see it, 404 otherwise. No response body. " + _Tenancy)
            .ReturnsNoContent(200, "Document exists.")
            .ReturnsNoContent(404, "Document not found.")
            .ReturnsNoContent(400, "Bad request.")
            .ReturnsNoContent(500, "Internal error.");

        /// <summary>POST /v1.0/documents/delete.</summary>
        public static OpenApiRouteMetadata BulkDelete => ApiDoc.Create("Bulk delete documents", _Tag)
            .Describe("Deletes several documents in one call: stored objects, RecallDB chunk embeddings (batched per collection), Verbex text-search records (batched per index), and document records. "
                + "IDs that do not exist or belong to another tenant (unless the caller is a global administrator) are silently skipped, and subordinate cleanup failures are logged without stopping the operation. The DocumentIds property name is case-sensitive.")
            .Body("Documents to delete. DocumentIds must contain at least one ID.", new BulkDeleteRequest
            {
                DocumentIds = new List<string> { ApiExamples.DocumentId, _SecondDocumentId }
            })
            .ReturnsNoContent(204, "Documents deleted.")
            .Errors(400, 401, 500);

        /// <summary>POST /v1.0/documents/reindex.</summary>
        public static OpenApiRouteMetadata ReindexBatch => ApiDoc.Create("Reindex documents", _Tag)
            .Describe("Global or tenant administrators only. Re-extracts and reindexes the text of completed documents into Verbex for backfill and repair. "
                + "When DocumentIds is supplied, those documents are processed (unknown or other-tenant IDs are reported as NotFound failures). Otherwise one page of the caller's tenant documents is enumerated using the paging and filter query parameters; continue with ContinuationToken until EndOfResults is true. "
                + "Documents that are not Completed are skipped, as are documents that already have a VerbexRecordId unless IncludeAlreadyIndexed is true. Per-document outcomes are reported in Results.")
            .Paged()
            .Query("bucketName", "string", "When enumerating, only include documents stored in this bucket.", false, _Bucket)
            .Query("collectionId", "string", "When enumerating, only include documents embedded into this RecallDB collection.", false, ApiExamples.CollectionId)
            .Body("Reindex options. Optional; an empty body reindexes one enumerated page.", new DocumentReindexRequest
            {
                DocumentIds = new List<string> { ApiExamples.DocumentId, _SecondDocumentId },
                IncludeAlreadyIndexed = false
            }, false)
            .Returns(200, "Batch reindex summary.", new DocumentReindexBatchResult
            {
                Requested = 2,
                Eligible = 1,
                Reindexed = 1,
                Skipped = 1,
                Failed = 0,
                ContinuationToken = null,
                EndOfResults = true,
                Results = new List<DocumentReindexResult>
                {
                    ExampleReindexResult(),
                    new DocumentReindexResult
                    {
                        DocumentId = _SecondDocumentId,
                        Success = true,
                        Status = "Skipped",
                        Message = "Document already has Verbex indexing metadata.",
                        VerbexTenantId = ApiExamples.TenantId,
                        VerbexIndexId = "default",
                        VerbexRecordId = _SecondDocumentId,
                        TotalMs = 0
                    }
                },
                TotalMs = 1240.4
            })
            .Errors(401, 403, 500);

        /// <summary>POST /v1.0/documents/{documentId}/reindex.</summary>
        public static OpenApiRouteMetadata Reindex => ApiDoc.Create("Reindex document", _Tag)
            .Describe("Global or tenant administrators only. Re-runs text extraction for one document from its stored object and reindexes the text into Verbex using the ingestion rule's labels, tags, and index selection. "
                + "Idempotent: the Verbex record ID stays the document ID and the existing record is replaced. Returns 200 when reindexing succeeds and 502 (with the same result shape) when extraction or indexing fails. " + _Tenancy)
            .Returns(200, "Document reindexed.", ExampleReindexResult())
            .Returns(502, "Extraction or Verbex indexing failed.", new DocumentReindexResult
            {
                DocumentId = ApiExamples.DocumentId,
                Success = false,
                Status = "Failed",
                Message = "Verbex indexing did not complete.",
                VerbexTenantId = null,
                VerbexIndexId = null,
                VerbexRecordId = null,
                TotalMs = 611.3
            })
            .Errors(400, 401, 403, 404, 500);

        /// <summary>POST /v1.0/documents/{documentId}/reprocess.</summary>
        public static OpenApiRouteMetadata Reprocess => ApiDoc.Create("Reprocess document", _Tag)
            .Describe("Re-runs the full ingestion pipeline for a document when its source object still exists in storage. The status is reset to Uploaded and ingestion runs asynchronously; poll the document or its processing log for progress. "
                + "When the source object is no longer stored, returns 200 with Success and SourceAvailable false and the document must be uploaded again. No request body. Returns 503 when object storage is not configured. " + _Tenancy)
            .Returns(200, "Reprocessing started, or SourceAvailable false when the source object is missing.", new
            {
                DocumentId = ApiExamples.DocumentId,
                Success = true,
                SourceAvailable = true,
                Message = "Reprocessing started."
            })
            .Errors(400, 401, 404, 500)
            .Error(503, "Object storage is not configured.", new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Reprocessing is unavailable. S3 storage is not configured."));

        /// <summary>GET /v1.0/documents/{documentId}/processing-log.</summary>
        public static OpenApiRouteMetadata ProcessingLog => ApiDoc.Create("Get document processing log", _Tag)
            .Describe("Returns the ingestion pipeline log for a document as a single newline-delimited string. Log is null when no log exists or processing logs are disabled. " + _Tenancy)
            .Returns(200, "Processing log.", new
            {
                DocumentId = ApiExamples.DocumentId,
                Log = "2026-01-15T17:30:00Z [INFO] Starting document processing\n2026-01-15T17:30:00Z [INFO] Type detection complete: Pdf\n2026-01-15T17:30:01Z [INFO] Atom extraction complete: 48213 characters extracted\n2026-01-15T17:30:02Z [INFO] Chunking complete: 18 chunks generated\n2026-01-15T17:30:02Z [INFO] Embedding storage complete: 18/18 stored"
            })
            .Errors(400, 401, 404, 500);

        /// <summary>GET /v1.0/documents/{documentId}/performance.</summary>
        public static OpenApiRouteMetadata Performance => ApiDoc.Create("Get document ingestion performance", _Tag)
            .Describe("Returns per-stage ingestion timing for a document (download, type detection, extraction, chunking, summarization, embedding storage) ordered by sequence number, plus TotalMs, the sum of stage durations. " + _Tenancy)
            .Returns(200, "Per-stage ingestion timing.", new
            {
                DocumentId = ApiExamples.DocumentId,
                TotalMs = 2399.3,
                Stages = ExamplePerformanceEvents()
            })
            .Errors(400, 401, 404, 500);

        /// <summary>GET /v1.0/analytics/ingestion.</summary>
        public static OpenApiRouteMetadata IngestionAnalytics => ApiDoc.Create("Get ingestion analytics", _Tag)
            .Describe("Lists ingestion performance events (one per pipeline stage per document) for the caller's tenant within a trailing time window, newest first. Use it to chart ingestion throughput and stage latency. "
                + "Returns 403 when the caller is not associated with a tenant. Invalid or non-positive query values fall back to the defaults.")
            .Query("hours", "integer", "Size of the trailing window in hours (default 24, maximum 2160).", false, 24)
            .Query("maxResults", "integer", "Maximum number of events to return (default 5000, maximum 50000).", false, 5000)
            .Returns(200, "Ingestion events in the window.", new
            {
                WindowStartUtc = ApiExamples.Updated.AddHours(-24),
                WindowEndUtc = ApiExamples.Updated,
                TotalRecords = 5,
                Events = ExamplePerformanceEvents()
            })
            .Errors(401, 403, 500);

        /// <summary>GET /v1.0/documents/{documentId}/download.</summary>
        public static OpenApiRouteMetadata Download => ApiDoc.Create("Download document", _Tag)
            .Describe("Downloads the original file from object storage. The response Content-Type is the document's ContentType (application/octet-stream when unset) and Content-Disposition is attachment; filename=\"{OriginalFilename}\". "
                + "Returns 404 when the document has no stored object or the object is empty. " + _Tenancy)
            .ReturnsContent(200, "Document file contents.", "application/octet-stream", ApiSchema.Binary("Original document bytes."))
            .Errors(400, 401, 404, 500);

        #endregion
    }
}
