namespace AssistantHub.Server.OpenApi
{
    using System.Collections.Generic;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for collection, collection record, and collection search routes. These routes proxy to
    /// RecallDB; request bodies are forwarded as-is and RecallDB response bodies and status codes are relayed verbatim.
    /// </summary>
    public static class CollectionApiDocs
    {
        #region Private-Members

        private const string _CollectionsTag = "Collections";
        private const string _RecordsTag = "Collection Records";
        private const string _SearchTag = "Collection Search";

        private const string _Access = "Global administrators only. The request is scoped to the caller's own tenant in RecallDB. ";
        private const string _Passthrough = "Errors raised by RecallDB are relayed with RecallDB's status code and error body. ";

        private const string _RecordKey = "doc_01JH3ZA4B6C8D0E2F4G6H8J0K2";
        private const string _RecordKey2 = "doc_01JH3ZA4B6C8D0E2F4G6H8J0K3";
        private const string _RecordKey3 = "doc_01JH3ZA4B6C8D0E2F4G6H8J0K4";
        private const string _Content = "Refunds are issued to the original payment method within 5 to 7 business days of approval.";
        private const string _ContentSha256 = "3f0a6c1d2b4e5f60718293a4b5c6d7e8f90a1b2c3d4e5f60718293a4b5c6d7e8";

        private static object ExampleCollection()
        {
            return new
            {
                Id = ApiExamples.CollectionId,
                TenantId = ApiExamples.TenantId,
                Name = "support-articles",
                Description = "Embeddings for customer support knowledge base articles.",
                Dimensionality = 384,
                Active = true,
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Updated
            };
        }

        private static object ExampleCollectionRequest()
        {
            return new
            {
                Name = "support-articles",
                Description = "Embeddings for customer support knowledge base articles.",
                Dimensionality = 384,
                Active = true
            };
        }

        private static object ExampleRecord()
        {
            return new
            {
                Id = 1042L,
                DocumentKey = _RecordKey,
                DocumentId = ApiExamples.DocumentId,
                ContentLength = 92L,
                Sha256 = _ContentSha256,
                Position = 0,
                ContentType = "Text",
                Content = _Content,
                Embeddings = new List<float> { 0.0213f, -0.0471f, 0.0885f, 0.0132f },
                CreatedUtc = ApiExamples.Created,
                Distance = 0.0,
                Score = 0.0,
                Labels = new List<string> { "billing", "refunds" },
                Tags = new Dictionary<string, string> { { "department", "support" }, { "year", "2026" } }
            };
        }

        private static object ExampleRecordRequest()
        {
            return new
            {
                DocumentId = ApiExamples.DocumentId,
                Position = 0,
                ContentType = "Text",
                Content = _Content,
                Embeddings = new List<float> { 0.0213f, -0.0471f, 0.0885f, 0.0132f },
                Labels = new List<string> { "billing", "refunds" },
                Tags = new Dictionary<string, string> { { "department", "support" }, { "year", "2026" } }
            };
        }

        #endregion

        #region Public-Members

        /// <summary>PUT /v1.0/collections.</summary>
        public static OpenApiRouteMetadata Create => ApiDoc.Create("Create collection", _CollectionsTag)
            .Describe("Creates a RecallDB vector collection in the caller's tenant. " + _Access
                + "The body is forwarded to RecallDB unchanged. Name is required; Dimensionality must match the embedding model output size and cannot be changed later; Id is generated when omitted. "
                + "A collection name must be unique within the tenant (409). " + _Passthrough)
            .Body("Collection to create.", ExampleCollectionRequest())
            .Returns(201, "Collection created (RecallDB collection object).", ExampleCollection())
            .Errors(400, 401, 403, 409, 500);

        /// <summary>GET /v1.0/collections.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List collections", _CollectionsTag)
            .Describe("Returns a page of RecallDB collections in the caller's tenant. " + _Access
                + "The paging query parameters are forwarded to RecallDB's collection enumerate endpoint. " + _Passthrough)
            .Paged()
            .Returns(200, "Page of collections (RecallDB enumeration result).", new
            {
                Success = true,
                MaxResults = 100,
                EndOfResults = true,
                TotalRecords = 1L,
                RecordsRemaining = 0L,
                Objects = new[]
                {
                    new
                    {
                        Id = ApiExamples.CollectionId,
                        TenantId = ApiExamples.TenantId,
                        Name = "support-articles",
                        Description = "Embeddings for customer support knowledge base articles.",
                        Dimensionality = 384,
                        Active = true,
                        CreatedUtc = ApiExamples.Created,
                        LastUpdateUtc = ApiExamples.Updated
                    }
                },
                TotalMs = 2.47
            })
            .Errors(401, 403, 500);

        /// <summary>GET /v1.0/collections/{collectionId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get collection", _CollectionsTag)
            .Describe("Returns one RecallDB collection from the caller's tenant. " + _Access + _Passthrough)
            .Returns(200, "Collection (RecallDB collection object).", ExampleCollection())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>PUT /v1.0/collections/{collectionId}.</summary>
        public static OpenApiRouteMetadata Update => ApiDoc.Create("Update collection", _CollectionsTag)
            .Describe("Updates a RecallDB collection's metadata. " + _Access
                + "The body is forwarded to RecallDB unchanged; RecallDB keeps the Id and tenant from the path. " + _Passthrough)
            .Body("Updated collection values.", ExampleCollectionRequest())
            .Returns(200, "Updated collection (RecallDB collection object).", ExampleCollection())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/collections/{collectionId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete collection", _CollectionsTag)
            .Describe("Deletes a RecallDB collection and all of its records, labels, and tags. " + _Access + _Passthrough)
            .ReturnsNoContent(204, "Collection deleted.")
            .Errors(400, 401, 403, 500);

        /// <summary>HEAD /v1.0/collections/{collectionId}.</summary>
        public static OpenApiRouteMetadata Exists => ApiDoc.Create("Check collection existence", _CollectionsTag)
            .Describe("Returns 200 when the collection exists in the caller's tenant, 404 otherwise; the RecallDB status code is relayed. " + _Access + "No response body.")
            .ReturnsNoContent(200, "Collection exists.")
            .ReturnsNoContent(404, "Collection not found.")
            .ReturnsNoContent(400, "Bad request.")
            .ReturnsNoContent(403, "Authorization failed.")
            .ReturnsNoContent(500, "Internal server error.");

        /// <summary>POST /v1.0/collections/{collectionId}/search.</summary>
        public static OpenApiRouteMetadata Search => ApiDoc.Create("Search collection", _SearchTag)
            .Describe("Searches records in a RecallDB collection using vector similarity (Vector), full-text relevance (FullText), or hybrid search (both), "
                + "with optional label, tag, term, date, and document ID filters and paging. " + _Access
                + "The body is a RecallDB search query forwarded as-is, except that a boolean IncludeNeighbors is normalized: true becomes 1 and false is removed. "
                + "An integer IncludeNeighbors (0 to 10) returns that many neighboring chunks before and after each match. " + _Passthrough)
            .Body("RecallDB search query.", new
            {
                Vector = new
                {
                    SearchType = "CosineSimilarity",
                    Embeddings = new List<float> { 0.0213f, -0.0471f, 0.0885f, 0.0132f }
                },
                FullText = new
                {
                    Query = "refund processing time",
                    MatchMode = "Any",
                    SearchType = "TsRankCd",
                    Language = "english",
                    Normalization = 32,
                    TextWeight = 0.5
                },
                Hybrid = new
                {
                    Strategy = "Rrf",
                    RrfK = 60,
                    CandidatePool = 100
                },
                LabelFilter = new
                {
                    Required = new List<string> { "billing" },
                    Excluded = new List<string> { "draft" }
                },
                TagFilter = new
                {
                    Required = new[] { new { Key = "department", Condition = "Equals", Value = "support" } }
                },
                IncludeNeighbors = 1,
                IncludeEmbeddings = false,
                MaxResults = 10
            })
            .Returns(200, "Search results (RecallDB search result).", new
            {
                Success = true,
                MaxResults = 10,
                EndOfResults = true,
                TotalRecords = 1L,
                RecordsRemaining = 0L,
                Documents = new[]
                {
                    new
                    {
                        Id = 1042L,
                        DocumentKey = _RecordKey,
                        DocumentId = ApiExamples.DocumentId,
                        ContentLength = 92L,
                        Sha256 = _ContentSha256,
                        Position = 0,
                        ContentType = "Text",
                        Content = _Content,
                        CreatedUtc = ApiExamples.Created,
                        Distance = 0.1874,
                        Score = 0.9312,
                        TextScore = 0.4417,
                        VectorScore = 0.8126,
                        VectorRank = 1,
                        TextRank = 1,
                        Labels = new List<string> { "billing", "refunds" },
                        Tags = new Dictionary<string, string> { { "department", "support" }, { "year", "2026" } },
                        Neighbors = new[]
                        {
                            new
                            {
                                Id = 1043L,
                                DocumentKey = _RecordKey2,
                                DocumentId = ApiExamples.DocumentId,
                                Position = 1,
                                ContentType = "Text",
                                Content = "Refunds for annual plans are prorated based on the unused portion of the term.",
                                CreatedUtc = ApiExamples.Created
                            }
                        }
                    }
                },
                TotalMs = 18.63
            })
            .Errors(400, 401, 403, 404, 500);

        /// <summary>PUT /v1.0/collections/{collectionId}/records.</summary>
        public static OpenApiRouteMetadata CreateRecord => ApiDoc.Create("Create collection record", _RecordsTag)
            .Describe("Creates a record (document chunk) in a RecallDB collection. " + _Access
                + "The body is forwarded to RecallDB's document create endpoint unchanged. When Embeddings is supplied its length must equal the collection Dimensionality (400 otherwise). "
                + "DocumentKey is generated when omitted; Labels and Tags are stored with the record. " + _Passthrough)
            .Body("Record to create. The example embedding vector is truncated for brevity.", ExampleRecordRequest())
            .Returns(201, "Record created (RecallDB document record).", ExampleRecord())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>GET /v1.0/collections/{collectionId}/records.</summary>
        public static OpenApiRouteMetadata ListRecords => ApiDoc.Create("List collection records", _RecordsTag)
            .Describe("Returns a page of records in a RecallDB collection. " + _Access
                + "The paging query parameters are forwarded to RecallDB's document enumerate endpoint. " + _Passthrough)
            .Paged()
            .Returns(200, "Page of records (RecallDB enumeration result).", new
            {
                Success = true,
                MaxResults = 100,
                EndOfResults = true,
                TotalRecords = 1L,
                RecordsRemaining = 0L,
                Objects = new[]
                {
                    new
                    {
                        Id = 1042L,
                        DocumentKey = _RecordKey,
                        DocumentId = ApiExamples.DocumentId,
                        ContentLength = 92L,
                        Sha256 = _ContentSha256,
                        Position = 0,
                        ContentType = "Text",
                        Content = _Content,
                        Embeddings = new List<float> { 0.0213f, -0.0471f, 0.0885f, 0.0132f },
                        CreatedUtc = ApiExamples.Created,
                        Distance = 0.0,
                        Score = 0.0,
                        Labels = new List<string> { "billing", "refunds" },
                        Tags = new Dictionary<string, string> { { "department", "support" }, { "year", "2026" } }
                    }
                },
                TotalMs = 3.18
            })
            .Errors(400, 401, 403, 500);

        /// <summary>GET /v1.0/collections/{collectionId}/records/{recordId}.</summary>
        public static OpenApiRouteMetadata ReadRecord => ApiDoc.Create("Get collection record", _RecordsTag)
            .Describe("Returns one record from a RecallDB collection. {recordId} is the record's DocumentKey. " + _Access + _Passthrough)
            .Returns(200, "Record (RecallDB document record).", ExampleRecord())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/collections/{collectionId}/records/{recordId}.</summary>
        public static OpenApiRouteMetadata DeleteRecord => ApiDoc.Create("Delete collection record", _RecordsTag)
            .Describe("Deletes one record, and its labels and tags, from a RecallDB collection. {recordId} is the record's DocumentKey. " + _Access + _Passthrough)
            .ReturnsNoContent(204, "Record deleted.")
            .Errors(400, 401, 403, 500);

        /// <summary>POST /v1.0/collections/{collectionId}/records/delete.</summary>
        public static OpenApiRouteMetadata DeleteRecords => ApiDoc.Create("Delete collection records", _RecordsTag)
            .Describe("Deletes multiple records from a RecallDB collection in one request. " + _Access
                + "The body may be an object with a RecordIds, Ids, or DocumentIds array (property names are case-insensitive, checked in that order) or a bare JSON array of record keys. "
                + "Blank values are dropped and duplicates removed; an empty list returns 400. " + _Passthrough)
            .Body("Record keys to delete.", new
            {
                RecordIds = new List<string> { _RecordKey, _RecordKey2, _RecordKey3 }
            })
            .ReturnsNoContent(204, "Records deleted.")
            .Errors(400, 401, 403, 500);

        /// <summary>GET /v1.0/collections/{collectionId}/labels/distinct.</summary>
        public static OpenApiRouteMetadata DistinctLabels => ApiDoc.Create("List distinct collection labels", _RecordsTag)
            .Describe("Returns the distinct label values used by records in a RecallDB collection, for example to populate filter controls. " + _Access + _Passthrough)
            .Returns(200, "Distinct label values.", new List<string> { "billing", "draft", "internal", "refunds" })
            .Errors(400, 401, 403, 500);

        /// <summary>GET /v1.0/collections/{collectionId}/tags/distinct.</summary>
        public static OpenApiRouteMetadata DistinctTags => ApiDoc.Create("List distinct collection tag keys", _RecordsTag)
            .Describe("Returns the distinct tag keys used by records in a RecallDB collection, for example to populate filter controls. " + _Access + _Passthrough)
            .Returns(200, "Distinct tag keys.", new List<string> { "author", "department", "status", "year" })
            .Errors(400, 401, 403, 500);

        #endregion
    }
}
