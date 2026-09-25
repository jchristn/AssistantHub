namespace AssistantHub.Server.OpenApi
{
    using System.Collections.Generic;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for index, index record, and index search routes. These routes proxy to Verbex; request bodies
    /// are forwarded to Verbex and Verbex response bodies (the Verbex response envelope) and status codes are relayed verbatim.
    /// AssistantHub "records" map to Verbex "documents" and AssistantHub "custom-metadata" maps to Verbex "customMetadata".
    /// </summary>
    public static class IndexApiDocs
    {
        #region Private-Members

        private const string _IndicesTag = "Indices";
        private const string _RecordsTag = "Index Records";
        private const string _SearchTag = "Index Search";

        private const string _Access = "Global administrators only; any other caller receives 403. ";
        private const string _Passthrough = "Verbex responses are relayed with Verbex's status code and JSON envelope (Guid, Success, TimestampUtc, StatusCode, Data, ErrorMessage, ProcessingTimeMs); a failure to reach Verbex returns 500 with an AssistantHub error body. ";

        private const string _IndexId = ApiExamples.TenantId + "_default";
        private const string _IndexName = "default";
        private const string _EnvelopeGuid = "5b0c2e6a-8f3d-4c1b-9a7e-2d4f6b8c0e1a";
        private const string _RecordId2 = "adoc_01JH3ZD5E7F9G1H3J5K7M9N1P3";
        private const string _RecordId3 = "adoc_01JH3ZD5E7F9G1H3J5K7M9N1P4";
        private const string _ObjectName = "refund-policy.pdf";
        private const string _Content = "Refunds are issued to the original payment method within 5 to 7 business days of approval.";
        private const string _ContentSha256 = "3f0a6c1d2b4e5f60718293a4b5c6d7e8f90a1b2c3d4e5f60718293a4b5c6d7e8";

        private static object Envelope<T>(int statusCode, T data, double processingTimeMs)
        {
            return new
            {
                Guid = _EnvelopeGuid,
                Success = true,
                TimestampUtc = ApiExamples.Updated,
                StatusCode = statusCode,
                Data = data,
                Headers = new Dictionary<string, string>(),
                ProcessingTimeMs = processingTimeMs
            };
        }

        private static object VerbexError(int statusCode, string message)
        {
            return new
            {
                Guid = _EnvelopeGuid,
                Success = false,
                TimestampUtc = ApiExamples.Updated,
                StatusCode = statusCode,
                ErrorMessage = message,
                Headers = new Dictionary<string, string>(),
                ProcessingTimeMs = 0.42
            };
        }

        private static List<string> IndexLabels()
        {
            return new List<string> { "assistanthub", "default" };
        }

        private static Dictionary<string, string> IndexTags()
        {
            return new Dictionary<string, string>
            {
                { "AssistantHubTenantId", ApiExamples.TenantId },
                { "Purpose", "DocumentTextSearch" }
            };
        }

        private static Dictionary<string, object> IndexCustomMetadata()
        {
            return new Dictionary<string, object>
            {
                { "Owner", "support-team" },
                { "RetentionDays", 365 }
            };
        }

        private static VerbexIndex ExampleIndexSummary()
        {
            return new VerbexIndex
            {
                Identifier = _IndexId,
                TenantId = ApiExamples.TenantId,
                Name = _IndexName,
                Description = "Default AssistantHub text search index for tenant " + ApiExamples.TenantId,
                Enabled = true,
                InMemory = false,
                CreatedUtc = ApiExamples.Created,
                Labels = IndexLabels(),
                Tags = IndexTags()
            };
        }

        private static object ExampleCreateIndexRequest()
        {
            return new
            {
                Identifier = _IndexId,
                TenantId = ApiExamples.TenantId,
                Name = _IndexName,
                Description = "Default AssistantHub text search index for tenant " + ApiExamples.TenantId,
                InMemory = false,
                EnableLemmatizer = false,
                EnableStopWordRemover = false,
                MinTokenLength = 0,
                MaxTokenLength = 0,
                Labels = IndexLabels(),
                Tags = IndexTags(),
                CustomMetadata = IndexCustomMetadata()
            };
        }

        private static List<string> RecordLabels()
        {
            return new List<string> { "support", "billing" };
        }

        private static Dictionary<string, string> RecordTags()
        {
            return new Dictionary<string, string> { { "department", "support" } };
        }

        private static Dictionary<string, object> RecordCustomMetadata()
        {
            return new Dictionary<string, object>
            {
                { "AssistantHubDocumentId", ApiExamples.DocumentId },
                { "AssistantHubTenantId", ApiExamples.TenantId },
                { "VerbexTenantId", ApiExamples.TenantId },
                { "VerbexIndexId", _IndexId },
                { "ObjectName", _ObjectName },
                { "CollectionId", ApiExamples.CollectionId },
                { "Bucket", ApiExamples.TenantId + "_documents" },
                { "ObjectKey", "policies/" + _ObjectName },
                { "ContentType", "application/pdf" },
                { "OriginalFileName", _ObjectName }
            };
        }

        private static VerbexDocument ExampleRecord()
        {
            return new VerbexDocument
            {
                DocumentId = ApiExamples.DocumentId,
                DocumentPath = ApiExamples.DocumentId,
                DocumentLength = 92L,
                IndexedDate = ApiExamples.Created,
                LastModified = ApiExamples.Updated,
                ContentSha256 = _ContentSha256,
                IndexingRuntimeMs = 12.34m,
                Terms = new List<string> { "refunds", "issued", "original", "payment", "method", "business", "days", "approval" },
                IsDeleted = false,
                CustomMetadata = RecordCustomMetadata(),
                Tags = new Dictionary<string, object> { { "department", "support" } },
                Labels = RecordLabels()
            };
        }

        private static object ExampleRecordRequest()
        {
            return new
            {
                Id = ApiExamples.DocumentId,
                Name = _ObjectName,
                Content = _Content,
                Labels = RecordLabels(),
                Tags = RecordTags(),
                CustomMetadata = new Dictionary<string, object> { { "ObjectName", _ObjectName } }
            };
        }

        private static VerbexPage<T> ExampleVerbexPage<T>(params T[] objects)
        {
            return new VerbexPage<T>
            {
                Success = true,
                Timestamp = new VerbexTimestamp
                {
                    Start = ApiExamples.Updated,
                    End = ApiExamples.Updated,
                    TotalMs = 1.87,
                    Messages = new Dictionary<string, string>()
                },
                MaxResults = 100,
                Skip = 0,
                IterationsRequired = 1,
                EndOfResults = true,
                TotalRecords = (long)objects.Length,
                RecordsRemaining = 0L,
                Objects = new List<T>(objects)
            };
        }

        private static OpenApiSchemaMetadata SearchRequestSchema()
        {
            return new OpenApiSchemaMetadata
            {
                Type = "object",
                Description = "Verbex index search request. Unrecognized properties are forwarded to Verbex unchanged.",
                Required = new List<string> { "Query" },
                Properties = new Dictionary<string, OpenApiSchemaMetadata>
                {
                    { "Query", new OpenApiSchemaMetadata { Type = "string", Description = "TF-IDF query text. Use * to browse all records (wildcard results have a score of 0 and are ordered by creation date)." } },
                    { "MaxResults", new OpenApiSchemaMetadata { Type = "integer", Format = "int32", Description = "Maximum number of results to return.", Default = 100 } },
                    { "UseAndLogic", new OpenApiSchemaMetadata { Type = "boolean", Description = "When true, all query terms must match (AND); otherwise any term may match (OR).", Default = false } },
                    { "Labels", new OpenApiSchemaMetadata { Type = "array", Description = "Required labels. Verbex applies AND logic, case-insensitive.", Items = new OpenApiSchemaMetadata { Type = "string" } } },
                    { "Tags", new OpenApiSchemaMetadata { Type = "object", Description = "Required key/value tag filters. Verbex applies AND logic with exact value matching." } },
                    { "IncludeMatchedTerms", new OpenApiSchemaMetadata { Type = "boolean", Description = "Include MatchedTerms on each result.", Default = false } },
                    { "IncludeTermDetails", new OpenApiSchemaMetadata { Type = "boolean", Description = "Include TermDetails (per-term Term, Score, Frequency) and MatchedTerms on each result.", Default = false } },
                    { "IncludeDocumentTermStats", new OpenApiSchemaMetadata { Type = "boolean", Description = "Include whole-document DocumentTermStats (UniqueTermCount, TotalTermOccurrences). Adds one grouped Verbex query.", Default = false } }
                }
            };
        }

        private static object ExampleSearchRequest()
        {
            return new
            {
                Query = "refund payment method",
                MaxResults = 25,
                UseAndLogic = false,
                Labels = new List<string> { "support" },
                Tags = new Dictionary<string, string> { { "department", "support" } },
                IncludeMatchedTerms = true,
                IncludeTermDetails = true,
                IncludeDocumentTermStats = true
            };
        }

        #endregion

        #region Public-Members

        #region Indices

        /// <summary>GET /v1.0/indices.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List indices", _IndicesTag)
            .Describe("Returns a page of Verbex indices visible to AssistantHub's Verbex credential. " + _Access
                + "The request query string is forwarded to Verbex unchanged, so Verbex's maxResults (1 to 1000), skip, continuationToken, and ordering parameters apply. " + _Passthrough)
            .Paged()
            .Query("skip", "integer", "Number of indices to skip (Verbex pagination).", false, 0)
            .Returns(200, "Verbex envelope whose Data is a Verbex enumeration result of index metadata.", Envelope(200, ExampleVerbexPage(ExampleIndexSummary()), 2.34))
            .Errors(401, 403, 500);

        /// <summary>PUT /v1.0/indices.</summary>
        public static OpenApiRouteMetadata Create => ApiDoc.Create("Create index", _IndicesTag)
            .Describe("Creates a Verbex index. " + _Access
                + "AssistantHub forwards the body to Verbex as POST /v1.0/indices, adding TenantId (the caller's tenant, or the default tenant) when the body omits it. "
                + "Name is required; Identifier is generated by Verbex when omitted. Verbex returns 409 when the name already exists in the tenant or the identifier is already in use. " + _Passthrough)
            .Body("Verbex index to create.", ExampleCreateIndexRequest())
            .Returns(201, "Index created (Verbex envelope).", Envelope(201, new
            {
                Message = "Index created successfully",
                Index = new
                {
                    Identifier = _IndexId,
                    TenantId = ApiExamples.TenantId,
                    Name = _IndexName,
                    Description = "Default AssistantHub text search index for tenant " + ApiExamples.TenantId,
                    InMemory = false,
                    CreatedUtc = ApiExamples.Created,
                    Labels = IndexLabels(),
                    Tags = IndexTags(),
                    CustomMetadata = IndexCustomMetadata()
                }
            }, 10.12))
            .Returns(400, "Verbex rejected the request (for example, missing Name or invalid JSON).", VerbexError(400, "Request body is required"))
            .Returns(409, "An index with this name or identifier already exists.", VerbexError(409, "Index with this name already exists in the tenant"))
            .Errors(401, 403, 500);

        /// <summary>GET /v1.0/indices/{indexId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get index", _IndicesTag)
            .Describe("Returns Verbex index metadata and statistics. " + _Access + _Passthrough)
            .Returns(200, "Index metadata and statistics (Verbex envelope).", Envelope(200, new
            {
                Identifier = _IndexId,
                TenantId = ApiExamples.TenantId,
                Name = _IndexName,
                Description = "Default AssistantHub text search index for tenant " + ApiExamples.TenantId,
                Enabled = true,
                InMemory = false,
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Updated,
                Labels = IndexLabels(),
                Tags = IndexTags(),
                CustomMetadata = IndexCustomMetadata(),
                Statistics = new
                {
                    DocumentCount = 150L,
                    TermCount = 5000L,
                    PostingCount = 12500L,
                    AverageDocumentLength = 250.5,
                    TotalDocumentSize = 37575L,
                    TotalTermOccurrences = 50000L,
                    AverageTermsPerDocument = 83.3,
                    AverageDocumentFrequency = 2.5,
                    MaxDocumentFrequency = 150L,
                    MinDocumentLength = 50L,
                    MaxDocumentLength = 500L,
                    GeneratedAt = ApiExamples.Updated,
                    TermIdCacheSize = 5000,
                    TermIdCacheLoaded = true
                }
            }, 3.45))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(400, 401, 403, 500);

        /// <summary>PUT /v1.0/indices/{indexId}.</summary>
        public static OpenApiRouteMetadata Update => ApiDoc.Create("Update index", _IndicesTag)
            .Describe("Updates an index's core properties (Name, Description, Enabled). Omitted properties are left unchanged. " + _Access
                + "The body is forwarded to Verbex unchanged. Verbex returns 409 when the new name is already used in the tenant. " + _Passthrough)
            .Body("Index properties to update.", new
            {
                Name = _IndexName,
                Description = "Support knowledge base text index",
                Enabled = true
            })
            .Returns(200, "Index updated (Verbex envelope).", Envelope(200, new
            {
                Message = "Index updated successfully",
                Index = new
                {
                    Identifier = _IndexId,
                    Name = _IndexName,
                    Description = "Support knowledge base text index",
                    Enabled = true,
                    Labels = IndexLabels(),
                    Tags = IndexTags()
                }
            }, 4.12))
            .Returns(400, "Verbex rejected the request body.", VerbexError(400, "Request body is required"))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Returns(409, "An index with this name already exists.", VerbexError(409, "Index with this name already exists in the tenant"))
            .Errors(401, 403, 500);

        /// <summary>DELETE /v1.0/indices/{indexId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete index", _IndicesTag)
            .Describe("Permanently deletes a Verbex index and all of its records. " + _Access + _Passthrough)
            .Returns(200, "Index deleted (Verbex envelope).", Envelope(200, new
            {
                Message = "Index deleted successfully",
                IndexId = _IndexId
            }, 5.67))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(400, 401, 403, 500);

        /// <summary>HEAD /v1.0/indices/{indexId}.</summary>
        public static OpenApiRouteMetadata Exists => ApiDoc.Create("Check index existence", _IndicesTag)
            .Describe("Returns 200 when the Verbex index exists and 404 otherwise. No response body. " + _Access
                + "Other Verbex status codes are relayed; a failure to reach Verbex returns 500.")
            .ReturnsNoContent(200, "Index exists.")
            .ReturnsNoContent(404, "Index not found.")
            .Errors(400, 401, 403, 500);

        /// <summary>PUT /v1.0/indices/{indexId}/labels.</summary>
        public static OpenApiRouteMetadata UpdateLabels => ApiDoc.Create("Replace index labels", _IndicesTag)
            .Describe("Replaces all labels on an index (full replacement, not additive). " + _Access + "The body is forwarded to Verbex unchanged. " + _Passthrough)
            .Body("New labels for the index.", new { Labels = new List<string> { "assistanthub", "default", "production" } })
            .Returns(200, "Labels updated (Verbex envelope).", Envelope(200, new
            {
                Message = "Labels updated successfully",
                Index = new
                {
                    Identifier = _IndexId,
                    Name = _IndexName,
                    Labels = new List<string> { "assistanthub", "default", "production" },
                    Tags = IndexTags()
                }
            }, 3.45))
            .Returns(400, "Verbex rejected the request body.", VerbexError(400, "Request body is required"))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(401, 403, 500);

        /// <summary>PUT /v1.0/indices/{indexId}/tags.</summary>
        public static OpenApiRouteMetadata UpdateTags => ApiDoc.Create("Replace index tags", _IndicesTag)
            .Describe("Replaces all tags on an index (full replacement, not additive). " + _Access + "The body is forwarded to Verbex unchanged. " + _Passthrough)
            .Body("New tags for the index.", new { Tags = new Dictionary<string, string> { { "AssistantHubTenantId", ApiExamples.TenantId }, { "Purpose", "DocumentTextSearch" }, { "region", "us-west" } } })
            .Returns(200, "Tags updated (Verbex envelope).", Envelope(200, new
            {
                Message = "Tags updated successfully",
                Index = new
                {
                    Identifier = _IndexId,
                    Name = _IndexName,
                    Labels = IndexLabels(),
                    Tags = new Dictionary<string, string> { { "AssistantHubTenantId", ApiExamples.TenantId }, { "Purpose", "DocumentTextSearch" }, { "region", "us-west" } }
                }
            }, 3.45))
            .Returns(400, "Verbex rejected the request body.", VerbexError(400, "Request body is required"))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(401, 403, 500);

        /// <summary>PUT /v1.0/indices/{indexId}/custom-metadata.</summary>
        public static OpenApiRouteMetadata UpdateCustomMetadata => ApiDoc.Create("Replace index custom metadata", _IndicesTag)
            .Describe("Replaces the custom metadata on an index. CustomMetadata can be any JSON value. " + _Access
                + "AssistantHub forwards the body unchanged to Verbex PUT /v1.0/indices/{indexId}/customMetadata. " + _Passthrough)
            .Body("New custom metadata for the index.", new { CustomMetadata = IndexCustomMetadata() })
            .Returns(200, "Custom metadata updated (Verbex envelope).", Envelope(200, new
            {
                Message = "Custom metadata updated successfully",
                Index = new
                {
                    Identifier = _IndexId,
                    Name = _IndexName,
                    Labels = IndexLabels(),
                    Tags = IndexTags(),
                    CustomMetadata = IndexCustomMetadata()
                }
            }, 3.45))
            .Returns(400, "Verbex rejected the request body.", VerbexError(400, "Request body is required"))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(401, 403, 500);

        /// <summary>GET /v1.0/indices/{indexId}/terms/top.</summary>
        public static OpenApiRouteMetadata TopTerms => ApiDoc.Create("Get top index terms", _IndicesTag)
            .Describe("Returns the most common terms in an index, keyed by term with the document frequency (number of records containing the term) as the value, sorted most common first. " + _Access
                + "The request query string is forwarded to Verbex unchanged. " + _Passthrough)
            .Query("limit", "integer", "Maximum number of terms to return (default 10).", false, 10)
            .Returns(200, "Map of term to document frequency (Verbex envelope).", Envelope(200, new Dictionary<string, int>
            {
                { "refund", 42 },
                { "payment", 38 },
                { "account", 35 },
                { "invoice", 30 },
                { "billing", 27 }
            }, 5.23))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(400, 401, 403, 500);

        #endregion

        #region Index-Records

        /// <summary>GET /v1.0/indices/{indexId}/records.</summary>
        public static OpenApiRouteMetadata ListRecords => ApiDoc.Create("List index records", _RecordsTag)
            .Describe("Returns a page of records (Verbex documents) in an index. " + _Access
                + "The request query string is forwarded unchanged to Verbex GET /v1.0/indices/{indexId}/documents. Filter by labels (comma-separated, AND logic, case-insensitive) and by tags with one tag.{key}=value parameter per tag (AND logic, exact match). "
                + "When ids is supplied, Verbex instead returns Data as { Documents, NotFound, Count, RequestedCount } for the requested record IDs. " + _Passthrough)
            .Paged()
            .Query("skip", "integer", "Number of records to skip (Verbex pagination).", false, 0)
            .Query("labels", "string", "Comma-separated labels; records must have all of them.", false, "support,billing")
            .Query("ids", "string", "Comma-separated record IDs to retrieve in batch instead of enumerating.", false, ApiExamples.DocumentId + "," + _RecordId2)
            .Returns(200, "Verbex envelope whose Data is a Verbex enumeration result of records.", Envelope(200, ExampleVerbexPage(ExampleRecord()), 3.45))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(400, 401, 403, 500);

        /// <summary>PUT /v1.0/indices/{indexId}/records.</summary>
        public static OpenApiRouteMetadata CreateRecord => ApiDoc.Create("Create index record", _RecordsTag)
            .Describe("Adds one record (Verbex document) to an index; it is forwarded to Verbex as POST /v1.0/indices/{indexId}/documents. " + _Access
                + "Content is required; Id is generated by Verbex when omitted and Verbex returns 409 when a record with the Id already exists. "
                + "When Name is omitted or blank, AssistantHub populates it from ObjectName, CustomMetadata.ObjectName, the basename of ObjectKey, CustomMetadata.ObjectKey, Key, or CustomMetadata.SourceUrl, then Id or Identifier, and finally \"record\"; it also sets CustomMetadata.ObjectName when absent. " + _Passthrough)
            .Body("Record to add.", ExampleRecordRequest())
            .Returns(201, "Record created (Verbex envelope) with ingestion metrics.", Envelope(201, new
            {
                DocumentId = ApiExamples.DocumentId,
                Message = "Document added successfully",
                Metrics = new
                {
                    TotalMs = 15.23,
                    Steps = new
                    {
                        LockWaitMs = 0.05,
                        TokenizationMs = 0.41,
                        PositionCalculationMs = 0.12,
                        TermLookupMs = 3.87,
                        DocumentTermInsertMs = 6.02,
                        FrequencyUpdateMs = 2.11,
                        DocumentUpdateMs = 1.34,
                        TransactionCommitMs = 1.31
                    },
                    Counts = new
                    {
                        TotalTokens = 16,
                        UniqueTerms = 12,
                        NewTerms = 2,
                        TermCacheHits = 10,
                        TermCacheMisses = 2,
                        TermCacheHitRate = 0.8333
                    }
                }
            }, 15.23))
            .Returns(400, "Verbex rejected the record (for example, missing Content).", VerbexError(400, "Content is required"))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Returns(409, "A record with this Id already exists.", VerbexError(409, "Document with ID '" + ApiExamples.DocumentId + "' already exists"))
            .Errors(401, 403, 500);

        /// <summary>POST /v1.0/indices/{indexId}/records/batch.</summary>
        public static OpenApiRouteMetadata CreateRecordBatch => ApiDoc.Create("Create index records in batch", _RecordsTag)
            .Describe("Adds multiple records to an index in one request; forwarded to Verbex POST /v1.0/indices/{indexId}/documents/batch. " + _Access
                + "Each record in Documents is normalized like the single-record route so Name and CustomMetadata.ObjectName are populated before proxying. "
                + "Verbex reports per-record success in Added and Failed. " + _Passthrough)
            .Body("Records to add.", new
            {
                Documents = new[]
                {
                    new
                    {
                        Id = ApiExamples.DocumentId,
                        Name = _ObjectName,
                        Content = _Content,
                        Labels = RecordLabels(),
                        Tags = RecordTags(),
                        CustomMetadata = new Dictionary<string, object> { { "ObjectName", _ObjectName } }
                    },
                    new
                    {
                        Id = _RecordId2,
                        Name = "shipping-policy.pdf",
                        Content = "Standard shipping takes 3 to 5 business days within the continental United States.",
                        Labels = new List<string> { "support", "shipping" },
                        Tags = RecordTags(),
                        CustomMetadata = new Dictionary<string, object> { { "ObjectName", "shipping-policy.pdf" } }
                    }
                }
            })
            .Returns(201, "Batch processed (Verbex envelope).", Envelope(201, new
            {
                Added = new List<VerbexBatchAddResult>
                {
                    new VerbexBatchAddResult { DocumentId = ApiExamples.DocumentId, Name = _ObjectName, Success = true },
                    new VerbexBatchAddResult { DocumentId = _RecordId2, Name = "shipping-policy.pdf", Success = true }
                },
                Failed = new List<VerbexBatchAddResult>(),
                AddedCount = 2,
                FailedCount = 0,
                RequestedCount = 2
            }, 28.64))
            .Returns(400, "Verbex rejected the batch (for example, no documents supplied).", VerbexError(400, "Request body is required"))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(401, 403, 500);

        /// <summary>POST /v1.0/indices/{indexId}/records/exists.</summary>
        public static OpenApiRouteMetadata RecordsExist => ApiDoc.Create("Check index record existence in batch", _RecordsTag)
            .Describe("Checks which of the supplied record IDs exist in an index; forwarded unchanged to Verbex POST /v1.0/indices/{indexId}/documents/exists. " + _Access + _Passthrough)
            .Body("Record IDs to check.", new { Ids = new List<string> { ApiExamples.DocumentId, _RecordId2, _RecordId3 } })
            .Returns(200, "Existence result (Verbex envelope).", Envelope(200, new
            {
                Exists = new List<string> { ApiExamples.DocumentId, _RecordId2 },
                NotFound = new List<string> { _RecordId3 },
                ExistsCount = 2,
                NotFoundCount = 1,
                RequestedCount = 3
            }, 2.18))
            .Returns(400, "Verbex rejected the request (for example, no Ids supplied).", VerbexError(400, "Request body is required"))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(401, 403, 500);

        /// <summary>POST /v1.0/indices/{indexId}/records/delete.</summary>
        public static OpenApiRouteMetadata DeleteRecords => ApiDoc.Create("Delete index records in batch", _RecordsTag)
            .Describe("Deletes multiple records from an index. " + _Access
                + "The body may be a JSON array of record IDs or an object with RecordIds, Ids, or DocumentIds; IDs are trimmed and de-duplicated, and 400 is returned when none remain. "
                + "AssistantHub sends { DocumentIds } to Verbex POST /v1.0/indices/{indexId}/documents/delete and, if that route returns 404, retries the legacy Verbex DELETE /v1.0/indices/{indexId}/documents?ids= route. " + _Passthrough)
            .Body("Record IDs to delete.", new { RecordIds = new List<string> { ApiExamples.DocumentId, _RecordId2, _RecordId3 } })
            .Returns(200, "Batch delete result (Verbex envelope).", Envelope(200, new
            {
                Deleted = new List<string> { ApiExamples.DocumentId, _RecordId2 },
                NotFound = new List<string> { _RecordId3 },
                DeletedCount = 2,
                NotFoundCount = 1,
                RequestedCount = 3
            }, 15.67))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(400, 401, 403, 500);

        /// <summary>GET /v1.0/indices/{indexId}/records/{recordId}.</summary>
        public static OpenApiRouteMetadata ReadRecord => ApiDoc.Create("Get index record", _RecordsTag)
            .Describe("Returns one record with its metadata, labels, tags, and custom metadata. " + _Access + _Passthrough)
            .Returns(200, "Record (Verbex envelope).", Envelope(200, ExampleRecord(), 3.45))
            .Returns(404, "Index or record not found.", VerbexError(404, "Document not found"))
            .Errors(400, 401, 403, 500);

        /// <summary>DELETE /v1.0/indices/{indexId}/records/{recordId}.</summary>
        public static OpenApiRouteMetadata DeleteRecord => ApiDoc.Create("Delete index record", _RecordsTag)
            .Describe("Deletes one record from an index. " + _Access + _Passthrough)
            .Returns(200, "Record deleted (Verbex envelope).", Envelope(200, new
            {
                DocumentId = ApiExamples.DocumentId,
                Message = "Document deleted successfully"
            }, 8.90))
            .Returns(404, "Index or record not found.", VerbexError(404, "Document not found"))
            .Errors(400, 401, 403, 500);

        /// <summary>HEAD /v1.0/indices/{indexId}/records/{recordId}.</summary>
        public static OpenApiRouteMetadata RecordExists => ApiDoc.Create("Check index record existence", _RecordsTag)
            .Describe("Returns 200 when the record exists and 404 when the index or record does not exist. No response body. " + _Access
                + "Other Verbex status codes are relayed; a failure to reach Verbex returns 500.")
            .ReturnsNoContent(200, "Record exists.")
            .ReturnsNoContent(404, "Index or record not found.")
            .Errors(400, 401, 403, 500);

        /// <summary>PUT /v1.0/indices/{indexId}/records/{recordId}/labels.</summary>
        public static OpenApiRouteMetadata UpdateRecordLabels => ApiDoc.Create("Replace index record labels", _RecordsTag)
            .Describe("Replaces all labels on a record (full replacement, not additive). " + _Access + "The body is forwarded to Verbex unchanged. " + _Passthrough)
            .Body("New labels for the record.", new { Labels = RecordLabels() })
            .Returns(200, "Labels updated; Data.Document is the updated record (Verbex envelope).", Envelope(200, new
            {
                Message = "Labels updated successfully",
                Document = ExampleRecord()
            }, 3.45))
            .Returns(400, "Verbex rejected the request body.", VerbexError(400, "Request body is required"))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(401, 403, 500);

        /// <summary>PUT /v1.0/indices/{indexId}/records/{recordId}/tags.</summary>
        public static OpenApiRouteMetadata UpdateRecordTags => ApiDoc.Create("Replace index record tags", _RecordsTag)
            .Describe("Replaces all tags on a record (full replacement, not additive). " + _Access + "The body is forwarded to Verbex unchanged. " + _Passthrough)
            .Body("New tags for the record.", new { Tags = RecordTags() })
            .Returns(200, "Tags updated; Data.Document is the updated record (Verbex envelope).", Envelope(200, new
            {
                Message = "Tags updated successfully",
                Document = ExampleRecord()
            }, 3.45))
            .Returns(400, "Verbex rejected the request body.", VerbexError(400, "Request body is required"))
            .Returns(404, "Index or record not found.", VerbexError(404, "Document not found"))
            .Errors(401, 403, 500);

        /// <summary>PUT /v1.0/indices/{indexId}/records/{recordId}/custom-metadata.</summary>
        public static OpenApiRouteMetadata UpdateRecordCustomMetadata => ApiDoc.Create("Replace index record custom metadata", _RecordsTag)
            .Describe("Replaces the custom metadata on a record. CustomMetadata can be any JSON value. " + _Access
                + "AssistantHub forwards the body unchanged to Verbex PUT /v1.0/indices/{indexId}/documents/{recordId}/customMetadata. " + _Passthrough)
            .Body("New custom metadata for the record.", new { CustomMetadata = RecordCustomMetadata() })
            .Returns(200, "Custom metadata updated; Data.Document is the updated record (Verbex envelope).", Envelope(200, new
            {
                Message = "Custom metadata updated successfully",
                Document = ExampleRecord()
            }, 3.45))
            .Returns(400, "Verbex rejected the request body.", VerbexError(400, "Request body is required"))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(401, 403, 500);

        #endregion

        #region Index-Search

        /// <summary>POST /v1.0/indices/{indexId}/search.</summary>
        public static OpenApiRouteMetadata Search => ApiDoc.Create("Search index", _SearchTag)
            .Describe("Runs a TF-IDF search against a Verbex index. " + _Access
                + "The body is forwarded to Verbex unchanged. Query is required; use * to browse records. Labels and Tags filter results with AND logic. "
                + "The optional IncludeMatchedTerms, IncludeTermDetails, and IncludeDocumentTermStats flags add MatchedTerms, TermDetails, and DocumentTermStats to each result. " + _Passthrough)
            .RawBody(ApiDoc.Json, "Verbex index search request.", SearchRequestSchema(), ExampleSearchRequest())
            .Returns(200, "Search results (Verbex envelope).", Envelope(200, new
            {
                Query = "refund payment method",
                Results = new[]
                {
                    new
                    {
                        DocumentId = ApiExamples.DocumentId,
                        Document = ExampleRecord(),
                        MatchedTermCount = 3,
                        Score = 0.85,
                        TermScores = new Dictionary<string, double> { { "refund", 0.42 }, { "payment", 0.27 }, { "method", 0.16 } },
                        TermFrequencies = new Dictionary<string, int> { { "refund", 2 }, { "payment", 1 }, { "method", 1 } },
                        TotalTermMatches = 4,
                        MatchedTerms = new List<string> { "method", "payment", "refund" },
                        TermDetails = new[]
                        {
                            new { Term = "refund", Score = 0.42, Frequency = 2 },
                            new { Term = "payment", Score = 0.27, Frequency = 1 },
                            new { Term = "method", Score = 0.16, Frequency = 1 }
                        },
                        DocumentTermStats = new
                        {
                            UniqueTermCount = 1840L,
                            TotalTermOccurrences = 78893L
                        }
                    }
                },
                TotalCount = 1,
                MaxResults = 25,
                SearchTime = 12.34,
                TimingInfo = new Dictionary<string, object>
                {
                    { "TermLookupMs", 1 },
                    { "TermsFound", 3 },
                    { "MainSearchMs", 2 },
                    { "MatchesFound", 1 },
                    { "TermFrequenciesMs", 1 },
                    { "TermFrequencyRecords", 3 },
                    { "DocumentMetadataMs", 2 },
                    { "DocumentsFetched", 1 },
                    { "DocumentCountMs", 1 },
                    { "TotalDocuments", 150 },
                    { "ResultEnrichmentMs", 0 },
                    { "DocumentTermStatsMs", 3 },
                    { "DocumentTermStatsDocuments", 1 }
                }
            }, 12.34))
            .Returns(400, "Verbex rejected the search request (for example, missing Query).", VerbexError(400, "Query is required"))
            .Returns(404, "Index not found.", VerbexError(404, "Index not found"))
            .Errors(401, 403, 500);

        #endregion

        #endregion

        #region Private-Classes

        /// <summary>
        /// Verbex index metadata as returned by the Verbex index enumeration.
        /// </summary>
        private sealed class VerbexIndex
        {
            /// <summary>Index identifier.</summary>
            public string Identifier { get; set; }

            /// <summary>Verbex tenant identifier that owns the index.</summary>
            public string TenantId { get; set; }

            /// <summary>Index name, unique within the tenant.</summary>
            public string Name { get; set; }

            /// <summary>Index description.</summary>
            public string Description { get; set; }

            /// <summary>True if the index is enabled.</summary>
            public bool Enabled { get; set; }

            /// <summary>True if the index is held in memory only.</summary>
            public bool InMemory { get; set; }

            /// <summary>Creation timestamp, UTC.</summary>
            public System.DateTime CreatedUtc { get; set; }

            /// <summary>Index labels.</summary>
            public List<string> Labels { get; set; }

            /// <summary>Index tags.</summary>
            public Dictionary<string, string> Tags { get; set; }
        }

        /// <summary>
        /// Verbex document (AssistantHub index record) metadata.
        /// </summary>
        private sealed class VerbexDocument
        {
            /// <summary>Record identifier. Records created by AssistantHub ingestion use the AssistantHub document ID.</summary>
            public string DocumentId { get; set; }

            /// <summary>Document path or name recorded by Verbex.</summary>
            public string DocumentPath { get; set; }

            /// <summary>Length of the indexed content in characters.</summary>
            public long DocumentLength { get; set; }

            /// <summary>Timestamp when the record was indexed, UTC.</summary>
            public System.DateTime IndexedDate { get; set; }

            /// <summary>Timestamp when the record was last modified, UTC.</summary>
            public System.DateTime LastModified { get; set; }

            /// <summary>SHA-256 hash of the indexed content.</summary>
            public string ContentSha256 { get; set; }

            /// <summary>Time spent indexing the record, in milliseconds.</summary>
            public decimal? IndexingRuntimeMs { get; set; }

            /// <summary>Distinct terms extracted from the content.</summary>
            public List<string> Terms { get; set; }

            /// <summary>True if the record is marked deleted.</summary>
            public bool IsDeleted { get; set; }

            /// <summary>Arbitrary JSON custom metadata.</summary>
            public object CustomMetadata { get; set; }

            /// <summary>Record tags.</summary>
            public Dictionary<string, object> Tags { get; set; }

            /// <summary>Record labels.</summary>
            public List<string> Labels { get; set; }
        }

        /// <summary>
        /// Per-record outcome of a Verbex batch add.
        /// </summary>
        private sealed class VerbexBatchAddResult
        {
            /// <summary>Record identifier.</summary>
            public string DocumentId { get; set; }

            /// <summary>Record name.</summary>
            public string Name { get; set; }

            /// <summary>True if the record was added.</summary>
            public bool Success { get; set; }

            /// <summary>Error message when the record could not be added.</summary>
            public string ErrorMessage { get; set; }
        }

        /// <summary>
        /// Verbex enumeration timestamp.
        /// </summary>
        private sealed class VerbexTimestamp
        {
            /// <summary>Start of the operation, UTC.</summary>
            public System.DateTime Start { get; set; }

            /// <summary>End of the operation, UTC.</summary>
            public System.DateTime End { get; set; }

            /// <summary>Total elapsed time in milliseconds.</summary>
            public double TotalMs { get; set; }

            /// <summary>Timestamped messages.</summary>
            public Dictionary<string, string> Messages { get; set; }
        }

        /// <summary>
        /// Verbex enumeration result.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        private sealed class VerbexPage<T>
        {
            /// <summary>True for successful requests.</summary>
            public bool Success { get; set; }

            /// <summary>When the result was generated.</summary>
            public VerbexTimestamp Timestamp { get; set; }

            /// <summary>Maximum results requested.</summary>
            public int MaxResults { get; set; }

            /// <summary>Number of records skipped.</summary>
            public int Skip { get; set; }

            /// <summary>Iterations required to build the page.</summary>
            public int IterationsRequired { get; set; }

            /// <summary>Continuation token for the next page; omitted on the last page.</summary>
            public string ContinuationToken { get; set; }

            /// <summary>True when this is the last page.</summary>
            public bool EndOfResults { get; set; }

            /// <summary>Total number of matching records.</summary>
            public long TotalRecords { get; set; }

            /// <summary>Records remaining after this page.</summary>
            public long RecordsRemaining { get; set; }

            /// <summary>Records in this page.</summary>
            public List<T> Objects { get; set; }
        }

        #endregion
    }
}
