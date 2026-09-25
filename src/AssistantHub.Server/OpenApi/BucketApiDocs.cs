namespace AssistantHub.Server.OpenApi
{
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for S3 bucket and bucket object routes. Buckets live on the configured S3-compatible storage
    /// server and are tenant-scoped by name: non-global-admin callers can only use buckets named "{tenantId}_...".
    /// </summary>
    public static class BucketApiDocs
    {
        #region Private-Members

        private const string _BucketsTag = "Buckets";
        private const string _ObjectsTag = "Bucket Objects";

        private const string _BucketName = ApiExamples.TenantId + "_documents";
        private const string _ObjectKey = "policies/refund-policy.pdf";
        private const string _FolderKey = "policies/";
        private const string _ETag = "\"9b2cf535f27731c974343645a3985328\"";

        private const string _BucketScope = "Global administrators can use any bucket; other callers can only use buckets whose name starts with their tenant ID followed by an underscore, and receive 403 otherwise. ";
        private const string _ObjectAccess = "Any authenticated user. " + _BucketScope;
        private const string _S3Errors = "Other storage server errors are relayed with the storage server's status code and an InternalError body carrying the storage server message. ";

        private static ApiErrorResponse KeyRequired()
        {
            return new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "Bucket name and key are required.");
        }

        #endregion

        #region Public-Members

        #region Buckets

        /// <summary>PUT /v1.0/buckets.</summary>
        public static OpenApiRouteMetadata Create => ApiDoc.Create("Create bucket", _BucketsTag)
            .Describe("Creates a bucket on the S3-compatible storage server. Global or tenant administrators only (403 otherwise). "
                + "Name is required. For callers that are not global administrators the name is prefixed with \"{tenantId}_\" unless it already starts with that prefix; the response returns the effective name. "
                + "Returns 409 when the bucket already exists. " + _S3Errors)
            .Body("Bucket to create.", new { Name = "documents" })
            .Returns(201, "Bucket created; Name is the effective (tenant-prefixed) bucket name.", new { Name = _BucketName })
            .Error(400, "Request body or Name is missing.", new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "Name is required."))
            .Error(409, "Bucket already exists.", new ApiErrorResponse(ApiErrorEnum.Conflict, null, "Bucket already exists."))
            .Errors(401, 403, 500);

        /// <summary>GET /v1.0/buckets.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List buckets", _BucketsTag)
            .Describe("Lists buckets on the storage server. Any authenticated user; global administrators see every bucket, other callers only buckets whose name starts with \"{tenantId}_\". "
                + "The result is not paged. " + _S3Errors)
            .Returns(200, "Buckets visible to the caller.", new
            {
                Objects = new[] { new { Name = _BucketName, CreationDate = ApiExamples.Created } },
                TotalRecords = 1
            })
            .Errors(401, 500);

        /// <summary>GET /v1.0/buckets/{name}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get bucket", _BucketsTag)
            .Describe("Returns one bucket by name. Any authenticated user. " + _BucketScope + _S3Errors)
            .Returns(200, "Bucket.", new
            {
                Name = _BucketName,
                CreationDate = ApiExamples.Created
            })
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/buckets/{name}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete bucket", _BucketsTag)
            .Describe("Deletes a bucket. Global or tenant administrators only. " + _BucketScope
                + "The bucket must be empty; a non-empty bucket returns 409. " + _S3Errors)
            .ReturnsNoContent(204, "Bucket deleted.")
            .Error(409, "Bucket is not empty.", new ApiErrorResponse(ApiErrorEnum.Conflict, null, "Bucket is not empty. Remove all objects before deleting."))
            .Errors(400, 401, 403, 404, 500);

        /// <summary>HEAD /v1.0/buckets/{name}.</summary>
        public static OpenApiRouteMetadata Exists => ApiDoc.Create("Check bucket existence", _BucketsTag)
            .Describe("Returns 200 when the bucket exists and 404 otherwise. No response body on any status. Any authenticated user. " + _BucketScope
                + "Other storage server errors are relayed as the storage server's status code without a body.")
            .ReturnsNoContent(200, "Bucket exists.")
            .ReturnsNoContent(400, "Bucket name is missing.")
            .ReturnsNoContent(401, "Authentication failed.")
            .ReturnsNoContent(403, "Bucket does not belong to the caller's tenant.")
            .ReturnsNoContent(404, "Bucket not found.")
            .ReturnsNoContent(500, "Internal error.");

        #endregion

        #region Bucket-Objects

        /// <summary>GET /v1.0/buckets/{name}/objects.</summary>
        public static OpenApiRouteMetadata ListObjects => ApiDoc.Create("List bucket objects", _ObjectsTag)
            .Describe("Lists up to 1000 objects in a bucket for folder-style navigation. " + _ObjectAccess
                + "Keys sharing the prefix up to the next delimiter are grouped into CommonPrefixes; the prefix itself is excluded from both lists. TotalRecords counts Objects only. "
                + "Returns 404 when the bucket does not exist. " + _S3Errors)
            .Query("prefix", "string", "Only return keys that start with this prefix (URL-encoded). Defaults to empty.", false, _FolderKey)
            .Query("delimiter", "string", "Delimiter used to group keys into CommonPrefixes (URL-encoded). Defaults to \"/\".", false, "/")
            .Returns(200, "Objects and common prefixes under the prefix.", new
            {
                Prefix = _FolderKey,
                Delimiter = "/",
                CommonPrefixes = new[] { new { Prefix = "policies/archive/" } },
                Objects = new[]
                {
                    new
                    {
                        Key = _ObjectKey,
                        Size = 248913L,
                        LastModified = ApiExamples.Updated,
                        ETag = _ETag
                    }
                },
                TotalRecords = 1
            })
            .Error(404, "Bucket not found.", new ApiErrorResponse(ApiErrorEnum.NotFound, null, "Bucket not found."))
            .Errors(400, 401, 403, 500);

        /// <summary>PUT /v1.0/buckets/{name}/objects.</summary>
        public static OpenApiRouteMetadata CreateObject => ApiDoc.Create("Create empty object", _ObjectsTag)
            .Describe("Creates an empty object, typically a folder marker whose key ends with \"/\". The request body is ignored. " + _ObjectAccess
                + "Returns 404 when the bucket does not exist. " + _S3Errors)
            .Query("key", "string", "Object key to create (URL-encoded), typically ending with \"/\".", true, "policies/archive/")
            .Returns(201, "Object created.", new { Key = "policies/archive/" })
            .Error(400, "Bucket name or key is missing.", KeyRequired())
            .Error(404, "Bucket not found.", new ApiErrorResponse(ApiErrorEnum.NotFound, null, "Bucket not found."))
            .Errors(401, 403, 500);

        /// <summary>DELETE /v1.0/buckets/{name}/objects.</summary>
        public static OpenApiRouteMetadata DeleteObject => ApiDoc.Create("Delete object", _ObjectsTag)
            .Describe("Deletes an object. " + _ObjectAccess
                + "When the key ends with \"/\" it is treated as a folder: every object under that prefix is deleted, then the folder marker itself (a missing marker is ignored). "
                + _S3Errors)
            .Query("key", "string", "Object key to delete (URL-encoded). A key ending with \"/\" deletes the folder recursively.", true, _ObjectKey)
            .ReturnsNoContent(204, "Object (or folder) deleted.")
            .Error(400, "Bucket name or key is missing.", KeyRequired())
            .Errors(401, 403, 404, 500);

        /// <summary>GET /v1.0/buckets/{name}/objects/metadata.</summary>
        public static OpenApiRouteMetadata ReadObjectMetadata => ApiDoc.Create("Get object metadata", _ObjectsTag)
            .Describe("Returns size, content type, modification time, ETag, and user metadata for one object without downloading it. " + _ObjectAccess + _S3Errors)
            .Query("key", "string", "Object key (URL-encoded).", true, _ObjectKey)
            .Returns(200, "Object metadata.", new
            {
                Key = _ObjectKey,
                ContentLength = 248913L,
                ContentType = "application/pdf",
                LastModified = ApiExamples.Updated,
                ETag = _ETag,
                Metadata = new Dictionary<string, string>()
            })
            .Error(400, "Bucket name or key is missing.", KeyRequired())
            .Errors(401, 403, 404, 500);

        /// <summary>GET /v1.0/buckets/{name}/objects/download.</summary>
        public static OpenApiRouteMetadata Download => ApiDoc.Create("Download object", _ObjectsTag)
            .Describe("Downloads an object's bytes. The response Content-Type is the stored content type (application/octet-stream when unknown) and Content-Disposition is \"attachment; filename=\\\"{last key segment}\\\"\". "
                + _ObjectAccess + _S3Errors)
            .Query("key", "string", "Object key (URL-encoded).", true, _ObjectKey)
            .ReturnsContent(200, "Object content.", "application/octet-stream", ApiSchema.Binary("Raw object bytes, served with the object's stored content type."))
            .Error(400, "Bucket name or key is missing.", KeyRequired())
            .Errors(401, 403, 404, 500);

        /// <summary>POST /v1.0/buckets/{name}/objects/upload.</summary>
        public static OpenApiRouteMetadata Upload => ApiDoc.Create("Upload object", _ObjectsTag)
            .Describe("Uploads the raw request body as an object, replacing any existing object with the same key. The request Content-Type is stored as the object's content type (application/octet-stream when absent). An empty body creates an empty object. "
                + _ObjectAccess + "Returns 404 when the bucket does not exist. " + _S3Errors)
            .Query("key", "string", "Object key to write (URL-encoded).", true, _ObjectKey)
            .RawBody("application/octet-stream", "Raw file bytes. Send the file's MIME type as the Content-Type header.", ApiSchema.Binary("File content."), null, false)
            .Returns(201, "Object uploaded; Size is the number of bytes stored.", new { Key = _ObjectKey, Size = 248913 })
            .Error(400, "Bucket name or key is missing.", KeyRequired())
            .Error(404, "Bucket not found.", new ApiErrorResponse(ApiErrorEnum.NotFound, null, "Bucket not found."))
            .Errors(401, 403, 500);

        #endregion

        #endregion
    }
}
