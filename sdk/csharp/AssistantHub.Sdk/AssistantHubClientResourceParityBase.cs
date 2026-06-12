namespace AssistantHub.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk.Enums;
    using AssistantHub.Sdk.Models;
    /// <summary>
    /// Adds collection, bucket, crawl, eval, request-history, tenant, user, and credential parity APIs.
    /// </summary>
    public abstract class AssistantHubClientResourceParityBase : AssistantHubClientAssistantParityBase
    {

        private protected AssistantHubClientResourceParityBase(string baseUrl, string apiKey = null)
            : base(baseUrl, apiKey)
        {
        }

        private protected AssistantHubClientResourceParityBase(string baseUrl, HttpClient httpClient, string apiKey = null)
            : base(baseUrl, httpClient, apiKey)
        {
        }

        #region Collections-and-Buckets

        /// <summary>
        /// List collections with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<Collection>> ListCollectionsAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<Collection>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/collections", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get distinct labels from a collection.
        /// </summary>
        public async Task<List<string>> GetCollectionDistinctLabelsAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            return await SendAsync<List<string>>(HttpMethod.Get, "/v1.0/collections/" + UrlEncode(collectionId) + "/labels/distinct", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get distinct tags from a collection.
        /// </summary>
        public async Task<List<string>> GetCollectionDistinctTagsAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            return await SendAsync<List<string>>(HttpMethod.Get, "/v1.0/collections/" + UrlEncode(collectionId) + "/tags/distinct", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a record in a collection.
        /// </summary>
        public async Task<CollectionRecord> CreateCollectionRecordAsync(string collectionId, CollectionRecord record, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            return await SendAsync<CollectionRecord>(HttpMethod.Put, "/v1.0/collections/" + UrlEncode(collectionId) + "/records", record, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List collection records.
        /// </summary>
        public async Task<EnumerationResult<CollectionRecord>> ListCollectionRecordsAsync(string collectionId, EnumerationQuery query = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            string path = AppendEnumerationQuery("/v1.0/collections/" + UrlEncode(collectionId) + "/records", query);
            return await SendAsync<EnumerationResult<CollectionRecord>>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a single collection record.
        /// </summary>
        public async Task<CollectionRecord> GetCollectionRecordAsync(string collectionId, string recordId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));
            if (String.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));

            return await SendAsync<CollectionRecord>(HttpMethod.Get, "/v1.0/collections/" + UrlEncode(collectionId) + "/records/" + UrlEncode(recordId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a single collection record.
        /// </summary>
        public async Task DeleteCollectionRecordAsync(string collectionId, string recordId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));
            if (String.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));

            await SendAsync(HttpMethod.Delete, "/v1.0/collections/" + UrlEncode(collectionId) + "/records/" + UrlEncode(recordId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete multiple collection records.
        /// </summary>
        public async Task BatchDeleteCollectionRecordsAsync(string collectionId, List<string> recordIds, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));
            if (recordIds == null)
                throw new ArgumentNullException(nameof(recordIds));

            await SendAsync(HttpMethod.Post, "/v1.0/collections/" + UrlEncode(collectionId) + "/records/delete", new { RecordIds = recordIds }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Search records in a RecallDB collection.
        /// </summary>
        public async Task<JsonElement> SearchCollectionAsync(string collectionId, object request, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendAsync<JsonElement>(HttpMethod.Post, "/v1.0/collections/" + UrlEncode(collectionId) + "/search", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List inverted indices.
        /// </summary>
        public async Task<JsonElement> ListIndicesAsync(EnumerationQuery query = null, CancellationToken cancellationToken = default)
        {
            return await SendAsync<JsonElement>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/indices", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create an inverted index.
        /// </summary>
        public async Task<JsonElement> CreateIndexAsync(object index, CancellationToken cancellationToken = default)
        {
            if (index == null)
                throw new ArgumentNullException(nameof(index));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/indices", index, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get inverted index metadata.
        /// </summary>
        public async Task<JsonElement> GetIndexAsync(string indexId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/indices/" + UrlEncode(indexId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update inverted index metadata.
        /// </summary>
        public async Task<JsonElement> UpdateIndexAsync(string indexId, object index, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (index == null)
                throw new ArgumentNullException(nameof(index));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/indices/" + UrlEncode(indexId), index, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an inverted index.
        /// </summary>
        public async Task DeleteIndexAsync(string indexId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));

            await SendAsync(HttpMethod.Delete, "/v1.0/indices/" + UrlEncode(indexId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update labels on an inverted index.
        /// </summary>
        public async Task<JsonElement> UpdateIndexLabelsAsync(string indexId, object labels, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/indices/" + UrlEncode(indexId) + "/labels", labels, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update tags on an inverted index.
        /// </summary>
        public async Task<JsonElement> UpdateIndexTagsAsync(string indexId, object tags, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/indices/" + UrlEncode(indexId) + "/tags", tags, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update custom metadata on an inverted index.
        /// </summary>
        public async Task<JsonElement> UpdateIndexCustomMetadataAsync(string indexId, object customMetadata, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (customMetadata == null)
                throw new ArgumentNullException(nameof(customMetadata));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/indices/" + UrlEncode(indexId) + "/custom-metadata", customMetadata, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get top terms from an inverted index.
        /// </summary>
        public async Task<JsonElement> GetIndexTopTermsAsync(string indexId, int? maxResults = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (maxResults.HasValue && maxResults.Value > 0)
                parameters["maxResults"] = maxResults.Value.ToString();

            string path = AppendQueryString("/v1.0/indices/" + UrlEncode(indexId) + "/terms/top", parameters);
            return await SendAsync<JsonElement>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Search an inverted index.
        /// </summary>
        public async Task<JsonElement> SearchIndexAsync(string indexId, object request, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendAsync<JsonElement>(HttpMethod.Post, "/v1.0/indices/" + UrlEncode(indexId) + "/search", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List records in an inverted index.
        /// </summary>
        public async Task<JsonElement> ListIndexRecordsAsync(string indexId, EnumerationQuery query = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));

            return await SendAsync<JsonElement>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/indices/" + UrlEncode(indexId) + "/records", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a record in an inverted index.
        /// </summary>
        public async Task<JsonElement> CreateIndexRecordAsync(string indexId, object record, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/indices/" + UrlEncode(indexId) + "/records", record, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create records in an inverted index in batch.
        /// </summary>
        public async Task<JsonElement> CreateIndexRecordsBatchAsync(string indexId, object records, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (records == null)
                throw new ArgumentNullException(nameof(records));

            return await SendAsync<JsonElement>(HttpMethod.Post, "/v1.0/indices/" + UrlEncode(indexId) + "/records/batch", records, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check whether multiple inverted-index records exist.
        /// </summary>
        public async Task<JsonElement> CheckIndexRecordsExistAsync(string indexId, object recordIds, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (recordIds == null)
                throw new ArgumentNullException(nameof(recordIds));

            return await SendAsync<JsonElement>(HttpMethod.Post, "/v1.0/indices/" + UrlEncode(indexId) + "/records/exists", recordIds, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete multiple records from an inverted index.
        /// </summary>
        public async Task DeleteIndexRecordsAsync(string indexId, List<string> recordIds, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (recordIds == null)
                throw new ArgumentNullException(nameof(recordIds));

            await SendAsync(HttpMethod.Post, "/v1.0/indices/" + UrlEncode(indexId) + "/records/delete", new { RecordIds = recordIds }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get an inverted-index record.
        /// </summary>
        public async Task<JsonElement> GetIndexRecordAsync(string indexId, string recordId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (String.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/indices/" + UrlEncode(indexId) + "/records/" + UrlEncode(recordId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an inverted-index record.
        /// </summary>
        public async Task DeleteIndexRecordAsync(string indexId, string recordId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (String.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));

            await SendAsync(HttpMethod.Delete, "/v1.0/indices/" + UrlEncode(indexId) + "/records/" + UrlEncode(recordId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update labels on an inverted-index record.
        /// </summary>
        public async Task<JsonElement> UpdateIndexRecordLabelsAsync(string indexId, string recordId, object labels, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (String.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/indices/" + UrlEncode(indexId) + "/records/" + UrlEncode(recordId) + "/labels", labels, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update tags on an inverted-index record.
        /// </summary>
        public async Task<JsonElement> UpdateIndexRecordTagsAsync(string indexId, string recordId, object tags, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (String.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/indices/" + UrlEncode(indexId) + "/records/" + UrlEncode(recordId) + "/tags", tags, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update custom metadata on an inverted-index record.
        /// </summary>
        public async Task<JsonElement> UpdateIndexRecordCustomMetadataAsync(string indexId, string recordId, object customMetadata, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(indexId))
                throw new ArgumentNullException(nameof(indexId));
            if (String.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));
            if (customMetadata == null)
                throw new ArgumentNullException(nameof(customMetadata));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/indices/" + UrlEncode(indexId) + "/records/" + UrlEncode(recordId) + "/custom-metadata", customMetadata, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a bucket.
        /// </summary>
        public async Task<JsonElement> CreateBucketAsync(BucketCreateRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return await SendAsync<JsonElement>(HttpMethod.Put, "/v1.0/buckets", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List buckets.
        /// </summary>
        public async Task<JsonElement> ListBucketsAsync(CancellationToken cancellationToken = default)
        {
            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/buckets", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a bucket.
        /// </summary>
        public async Task<JsonElement> GetBucketAsync(string bucketName, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/buckets/" + UrlEncode(bucketName), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a bucket.
        /// </summary>
        public async Task DeleteBucketAsync(string bucketName, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));

            await SendAsync(HttpMethod.Delete, "/v1.0/buckets/" + UrlEncode(bucketName), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create an empty object marker in a bucket.
        /// </summary>
        public async Task<JsonElement> PutBucketObjectAsync(string bucketName, string key, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            string path = AppendQueryString("/v1.0/buckets/" + UrlEncode(bucketName) + "/objects", new Dictionary<string, string>
            {
                ["key"] = key
            });

            return await SendAsync<JsonElement>(HttpMethod.Put, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List objects in a bucket.
        /// </summary>
        public async Task<JsonElement> ListBucketObjectsAsync(string bucketName, string prefix = null, string delimiter = "/", CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));

            string path = AppendQueryString("/v1.0/buckets/" + UrlEncode(bucketName) + "/objects", new Dictionary<string, string>
            {
                ["prefix"] = prefix,
                ["delimiter"] = delimiter
            });

            return await SendAsync<JsonElement>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an object from a bucket.
        /// </summary>
        public async Task DeleteBucketObjectAsync(string bucketName, string key, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            string path = AppendQueryString("/v1.0/buckets/" + UrlEncode(bucketName) + "/objects", new Dictionary<string, string>
            {
                ["key"] = key
            });

            await SendAsync(HttpMethod.Delete, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve object metadata from a bucket.
        /// </summary>
        public async Task<JsonElement> GetBucketObjectMetadataAsync(string bucketName, string key, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            string path = AppendQueryString("/v1.0/buckets/" + UrlEncode(bucketName) + "/objects/metadata", new Dictionary<string, string>
            {
                ["key"] = key
            });

            return await SendAsync<JsonElement>(HttpMethod.Get, path, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Download an object from a bucket.
        /// </summary>
        public async Task<byte[]> DownloadBucketObjectAsync(string bucketName, string key, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            string path = AppendQueryString("/v1.0/buckets/" + UrlEncode(bucketName) + "/objects/download", new Dictionary<string, string>
            {
                ["key"] = key
            });

            return await DownloadBytesAsync(path, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Upload binary content to a bucket object.
        /// </summary>
        public async Task<JsonElement> UploadBucketObjectAsync(string bucketName, string key, byte[] data, string contentType = "application/octet-stream", CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string path = AppendQueryString("/v1.0/buckets/" + UrlEncode(bucketName) + "/objects/upload", new Dictionary<string, string>
            {
                ["key"] = key
            });

            ByteArrayContent content = new ByteArrayContent(data);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(String.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

            return await SendContentAsync<JsonElement>(HttpMethod.Post, path, content, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Crawl-and-Eval

        /// <summary>
        /// List crawl plans with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<CrawlPlan>> ListCrawlPlansAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<CrawlPlan>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/crawlplans", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Test connectivity for a crawl plan.
        /// </summary>
        public async Task<CrawlConnectivityResult> TestCrawlConnectivityAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await SendAsync<CrawlConnectivityResult>(HttpMethod.Post, "/v1.0/crawlplans/" + UrlEncode(planId) + "/connectivity", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Test connectivity for an unsaved crawl plan.
        /// </summary>
        public async Task<CrawlConnectivityResult> TestCrawlPlanDraftConnectivityAsync(CrawlPlan plan, CancellationToken cancellationToken = default)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            return await SendAsync<CrawlConnectivityResult>(HttpMethod.Post, "/v1.0/crawlplans/connectivity", plan, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Enumerate crawl plan contents.
        /// </summary>
        public async Task<JsonElement> EnumerateCrawlAsync(string planId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/crawlplans/" + UrlEncode(planId) + "/enumerate", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List crawl operations with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<CrawlOperation>> ListCrawlOperationsAsync(string planId, EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));

            return await SendAsync<EnumerationResult<CrawlOperation>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/crawlplans/" + UrlEncode(planId) + "/operations", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get an operation's saved enumeration payload.
        /// </summary>
        public async Task<JsonElement> GetCrawlOperationEnumerationAsync(string planId, string operationId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(planId))
                throw new ArgumentNullException(nameof(planId));
            if (String.IsNullOrWhiteSpace(operationId))
                throw new ArgumentNullException(nameof(operationId));

            return await SendAsync<JsonElement>(HttpMethod.Get, "/v1.0/crawlplans/" + UrlEncode(planId) + "/operations/" + UrlEncode(operationId) + "/enumeration", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List evaluation facts with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<EvalFact>> ListEvalFactsAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<EvalFact>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/eval/facts", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List evaluation runs with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<EvalRun>> ListEvalRunsAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<EvalRun>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/eval/runs", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve an evaluation result by identifier.
        /// </summary>
        public async Task<EvalResult> GetEvalResultAsync(string resultId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(resultId))
                throw new ArgumentNullException(nameof(resultId));

            return await SendAsync<EvalResult>(HttpMethod.Get, "/v1.0/eval/results/" + UrlEncode(resultId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get all evaluation results for a run.
        /// </summary>
        public async Task<List<EvalResult>> GetEvalRunResultsAsync(string runId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(runId))
                throw new ArgumentNullException(nameof(runId));

            return await SendAsync<List<EvalResult>>(HttpMethod.Get, "/v1.0/eval/runs/" + UrlEncode(runId) + "/results", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Stream evaluation run updates via SSE.
        /// </summary>
        public async IAsyncEnumerable<string> StreamEvalRunAsync(string runId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(runId))
                throw new ArgumentNullException(nameof(runId));

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, BaseUrl + "/v1.0/eval/runs/" + UrlEncode(runId) + "/stream"))
            {
                using (HttpResponseMessage response = await SendRawAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        while (!reader.EndOfStream)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            string line = await reader.ReadLineAsync().ConfigureAwait(false);
                            if (line == null)
                                break;

                            if (line.StartsWith("data: "))
                            {
                                string data = line.Substring(6);
                                if (data == "[DONE]")
                                    yield break;

                                yield return data;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Request-History

        /// <summary>
        /// List request-history entries.
        /// </summary>
        public async Task<EnumerationResult<RequestHistoryEntry>> ListRequestHistoryAsync(RequestHistorySearchFilter filter = null, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<RequestHistoryEntry>>(HttpMethod.Get, AppendRequestHistoryFilter("/v1.0/requesthistory", filter), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Summarize request-history entries into time buckets.
        /// </summary>
        public async Task<RequestHistorySummaryResult> GetRequestHistorySummaryAsync(RequestHistorySearchFilter filter = null, CancellationToken cancellationToken = default)
        {
            return await SendAsync<RequestHistorySummaryResult>(HttpMethod.Get, AppendRequestHistoryFilter("/v1.0/requesthistory/summary", filter), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a fully hydrated request-history entry.
        /// </summary>
        public async Task<RequestHistoryEntry> GetRequestHistoryAsync(string requestId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(requestId))
                throw new ArgumentNullException(nameof(requestId));

            return await SendAsync<RequestHistoryEntry>(HttpMethod.Get, "/v1.0/requesthistory/" + UrlEncode(requestId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the alias detail payload for a request-history entry.
        /// </summary>
        public async Task<RequestHistoryEntry> GetRequestHistoryDetailAsync(string requestId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(requestId))
                throw new ArgumentNullException(nameof(requestId));

            return await SendAsync<RequestHistoryEntry>(HttpMethod.Get, "/v1.0/requesthistory/" + UrlEncode(requestId) + "/detail", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a single request-history entry.
        /// </summary>
        public async Task DeleteRequestHistoryAsync(string requestId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(requestId))
                throw new ArgumentNullException(nameof(requestId));

            await SendAsync(HttpMethod.Delete, "/v1.0/requesthistory/" + UrlEncode(requestId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete request-history entries matching the supplied filter.
        /// </summary>
        public async Task<RequestHistoryDeleteResult> DeleteRequestHistoryBulkAsync(RequestHistorySearchFilter filter = null, CancellationToken cancellationToken = default)
        {
            return await SendAsync<RequestHistoryDeleteResult>(HttpMethod.Delete, AppendRequestHistoryFilter("/v1.0/requesthistory/bulk", filter), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Tenants-Users-Credentials

        /// <summary>
        /// List tenants with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<TenantMetadata>> ListTenantsAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<TenantMetadata>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/tenants", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List users with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<UserMaster>> ListUsersAsync(string tenantId, EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            return await SendAsync<EnumerationResult<UserMaster>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/tenants/" + UrlEncode(tenantId) + "/users", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List credentials with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<Credential>> ListCredentialsAsync(string tenantId, EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            return await SendAsync<EnumerationResult<Credential>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/tenants/" + UrlEncode(tenantId) + "/credentials", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List ingestion rules with an enumeration query.
        /// </summary>
        public async Task<EnumerationResult<IngestionRule>> ListIngestionRulesAsync(EnumerationQuery query, CancellationToken cancellationToken = default)
        {
            return await SendAsync<EnumerationResult<IngestionRule>>(HttpMethod.Get, AppendEnumerationQuery("/v1.0/ingestion-rules", query), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
