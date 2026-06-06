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
    }
}
