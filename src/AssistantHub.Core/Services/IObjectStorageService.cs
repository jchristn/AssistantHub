namespace AssistantHub.Core.Services
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Object storage service abstraction for Less3/S3-compatible storage.
    /// </summary>
    public interface IObjectStorageService
    {
        /// <summary>
        /// Upload a file to the default bucket.
        /// </summary>
        Task UploadAsync(string key, string contentType, byte[] data, CancellationToken token = default);

        /// <summary>
        /// Upload a file to a specific bucket.
        /// </summary>
        Task UploadAsync(string bucketName, string key, string contentType, byte[] data, CancellationToken token = default);

        /// <summary>
        /// Download a file from the default bucket.
        /// </summary>
        Task<byte[]> DownloadAsync(string key, CancellationToken token = default);

        /// <summary>
        /// Download a file from a specific bucket.
        /// </summary>
        Task<byte[]> DownloadAsync(string bucketName, string key, CancellationToken token = default);

        /// <summary>
        /// Download a byte range from a specific bucket.
        /// </summary>
        Task<byte[]> DownloadRangeAsync(string bucketName, string key, long start, int length, CancellationToken token = default);

        /// <summary>
        /// Read safe object metadata from a specific bucket.
        /// </summary>
        Task<ObjectStorageItem> GetObjectMetadataAsync(string bucketName, string key, CancellationToken token = default);

        /// <summary>
        /// Delete a file from the default bucket.
        /// </summary>
        Task DeleteAsync(string key, CancellationToken token = default);

        /// <summary>
        /// Delete a file from a specific bucket.
        /// </summary>
        Task DeleteAsync(string bucketName, string key, CancellationToken token = default);

        /// <summary>
        /// Check whether a file exists in the default bucket.
        /// </summary>
        Task<bool> ExistsAsync(string key, CancellationToken token = default);

        /// <summary>
        /// List objects from a specific bucket and optional prefix.
        /// </summary>
        Task<ObjectStorageListResult> ListObjectsAsync(
            string bucketName,
            string prefix = null,
            int maxResults = 100,
            string continuationToken = null,
            CancellationToken token = default);
    }
}
