namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;
    using AssistantHub.Core.Settings;
    using Blobject.AmazonS3;
    using Blobject.Core;
    using SyslogLogging;

    /// <summary>
    /// Storage service wrapping an S3-compatible blob client.
    /// </summary>
    public class StorageService : IObjectStorageService
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[StorageService] ";
        private S3Settings _Settings = null;
        private LoggingModule _Logging = null;
        private AmazonS3BlobClient _Client = null;
        private AmazonS3Client _S3Client = null;
        private ConcurrentDictionary<string, AmazonS3BlobClient> _BucketClients = new ConcurrentDictionary<string, AmazonS3BlobClient>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">S3 storage settings.</param>
        /// <param name="logging">Logging module.</param>
        public StorageService(S3Settings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));

            AwsSettings awsSettings = new AwsSettings(
                _Settings.EndpointUrl,
                _Settings.UseSsl,
                _Settings.AccessKey,
                _Settings.SecretKey,
                _Settings.Region,
                _Settings.BucketName,
                _Settings.BaseUrl);

            _Client = new AmazonS3BlobClient(awsSettings);
            _S3Client = new AmazonS3Client(
                new BasicAWSCredentials(_Settings.AccessKey, _Settings.SecretKey),
                new AmazonS3Config
                {
                    ServiceURL = _Settings.EndpointUrl,
                    ForcePathStyle = true,
                    UseHttp = !_Settings.UseSsl
                });

            _Logging.Info(_Header + "initialized with bucket " + _Settings.BucketName);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Upload a file to S3-compatible storage (default bucket).
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="contentType">MIME content type.</param>
        /// <param name="data">File data.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task UploadAsync(string key, string contentType, byte[] data, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (data == null) throw new ArgumentNullException(nameof(data));

            _Logging.Debug(_Header + "uploading " + key + " (" + data.Length + " bytes)");
            await _Client.WriteAsync(key, contentType, data, token).ConfigureAwait(false);
            _Logging.Debug(_Header + "upload complete for " + key);
        }

        /// <summary>
        /// Upload a file to a specific S3 bucket.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="key">Object key.</param>
        /// <param name="contentType">MIME content type.</param>
        /// <param name="data">File data.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task UploadAsync(string bucketName, string key, string contentType, byte[] data, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (data == null) throw new ArgumentNullException(nameof(data));

            AmazonS3BlobClient client = GetClientForBucket(bucketName);
            _Logging.Debug(_Header + "uploading " + key + " to bucket " + bucketName + " (" + data.Length + " bytes)");
            await client.WriteAsync(key, contentType, data, token).ConfigureAwait(false);
            _Logging.Debug(_Header + "upload complete for " + key + " in bucket " + bucketName);
        }

        /// <summary>
        /// Download a file from S3-compatible storage (default bucket).
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>File data as byte array.</returns>
        public async Task<byte[]> DownloadAsync(string key, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            _Logging.Debug(_Header + "downloading " + key);
            byte[] data = await _Client.GetAsync(key, token).ConfigureAwait(false);
            _Logging.Debug(_Header + "download complete for " + key + " (" + (data != null ? data.Length : 0) + " bytes)");
            return data;
        }

        /// <summary>
        /// Download a file from a specific S3 bucket.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="key">Object key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>File data as byte array.</returns>
        public async Task<byte[]> DownloadAsync(string bucketName, string key, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            AmazonS3BlobClient client = GetClientForBucket(bucketName);
            _Logging.Debug(_Header + "downloading " + key + " from bucket " + bucketName);
            byte[] data = await client.GetAsync(key, token).ConfigureAwait(false);
            _Logging.Debug(_Header + "download complete for " + key + " from bucket " + bucketName + " (" + (data != null ? data.Length : 0) + " bytes)");
            return data;
        }

        /// <summary>
        /// Download a byte range from a specific S3 bucket.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="key">Object key.</param>
        /// <param name="start">Zero-based byte offset.</param>
        /// <param name="length">Maximum bytes to read.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Requested object bytes.</returns>
        public async Task<byte[]> DownloadRangeAsync(string bucketName, string key, long start, int length, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (length == 0) return Array.Empty<byte>();

            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                ByteRange = new ByteRange(start, start + length - 1)
            };

            _Logging.Debug(_Header + "range downloading " + key + " from bucket " + bucketName + " start " + start + " length " + length);
            using GetObjectResponse response = await _S3Client.GetObjectAsync(request, token).ConfigureAwait(false);
            using MemoryStream ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, token).ConfigureAwait(false);
            byte[] data = ms.ToArray();
            _Logging.Debug(_Header + "range download complete for " + key + " from bucket " + bucketName + " (" + data.Length + " bytes)");
            return data;
        }

        /// <summary>
        /// Read safe object metadata from a specific S3 bucket.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="key">Object key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Object metadata.</returns>
        public async Task<ObjectStorageItem> GetObjectMetadataAsync(string bucketName, string key, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            GetObjectMetadataRequest request = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = key
            };

            _Logging.Debug(_Header + "reading metadata for " + key + " from bucket " + bucketName);
            GetObjectMetadataResponse response = await _S3Client.GetObjectMetadataAsync(request, token).ConfigureAwait(false);
            return new ObjectStorageItem
            {
                Key = key,
                SizeBytes = response.Headers?.ContentLength ?? 0,
                ContentType = response.Headers?.ContentType ?? response.ContentType,
                ETag = response.ETag,
                LastModifiedUtc = response.LastModified
            };
        }

        /// <summary>
        /// Delete a file from S3-compatible storage (default bucket).
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DeleteAsync(string key, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            _Logging.Debug(_Header + "deleting " + key);
            await _Client.DeleteAsync(key, token).ConfigureAwait(false);
            _Logging.Debug(_Header + "delete complete for " + key);
        }

        /// <summary>
        /// Delete a file from a specific S3 bucket.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="key">Object key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DeleteAsync(string bucketName, string key, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            AmazonS3BlobClient client = GetClientForBucket(bucketName);
            _Logging.Debug(_Header + "deleting " + key + " from bucket " + bucketName);
            await client.DeleteAsync(key, token).ConfigureAwait(false);
            _Logging.Debug(_Header + "delete complete for " + key + " from bucket " + bucketName);
        }

        /// <summary>
        /// Check whether a file exists in S3-compatible storage.
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the object exists.</returns>
        public async Task<bool> ExistsAsync(string key, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            bool exists = await _Client.ExistsAsync(key, token).ConfigureAwait(false);
            _Logging.Debug(_Header + "exists check for " + key + ": " + exists);
            return exists;
        }

        /// <summary>
        /// List objects in a specific S3 bucket.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="prefix">Optional object key prefix.</param>
        /// <param name="maxResults">Maximum results.</param>
        /// <param name="continuationToken">Continuation token.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Object listing.</returns>
        public async Task<ObjectStorageListResult> ListObjectsAsync(
            string bucketName,
            string prefix = null,
            int maxResults = 100,
            string continuationToken = null,
            CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(bucketName)) throw new ArgumentNullException(nameof(bucketName));

            int cappedMaxResults = Math.Clamp(maxResults, 1, 1000);
            ListObjectsV2Request request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = prefix ?? "",
                MaxKeys = cappedMaxResults,
                ContinuationToken = continuationToken
            };

            _Logging.Debug(_Header + "listing bucket " + bucketName + " prefix " + (prefix ?? "") + " maxResults " + cappedMaxResults);
            ListObjectsV2Response response = await _S3Client.ListObjectsV2Async(request, token).ConfigureAwait(false);

            List<ObjectStorageItem> objects = new List<ObjectStorageItem>();
            if (response.S3Objects != null)
            {
                foreach (S3Object obj in response.S3Objects)
                {
                    if (obj == null) continue;
                    objects.Add(new ObjectStorageItem
                    {
                        Key = obj.Key,
                        SizeBytes = obj.Size ?? 0,
                        ETag = obj.ETag,
                        LastModifiedUtc = obj.LastModified
                    });
                }
            }

            return new ObjectStorageListResult
            {
                BucketName = bucketName,
                Prefix = prefix,
                MaxResults = cappedMaxResults,
                ContinuationToken = response.NextContinuationToken,
                EndOfResults = !response.IsTruncated.GetValueOrDefault(),
                Objects = objects
            };
        }


        #endregion

        #region Private-Methods

        /// <summary>
        /// Get or create an S3 client for a specific bucket.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <returns>S3 blob client for the bucket.</returns>
        private AmazonS3BlobClient GetClientForBucket(string bucketName)
        {
            if (String.IsNullOrEmpty(bucketName) || bucketName == _Settings.BucketName)
                return _Client;

            return _BucketClients.GetOrAdd(bucketName, (name) =>
            {
                AwsSettings awsSettings = new AwsSettings(
                    _Settings.EndpointUrl,
                    _Settings.UseSsl,
                    _Settings.AccessKey,
                    _Settings.SecretKey,
                    _Settings.Region,
                    name,
                    _Settings.BaseUrl);

                _Logging.Info(_Header + "creating client for bucket " + name);
                return new AmazonS3BlobClient(awsSettings);
            });
        }

        #endregion
    }
}
