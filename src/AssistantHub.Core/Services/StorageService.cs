namespace AssistantHub.Core.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Settings;
    using Blobject.AmazonS3;
    using Blobject.Core;
    using SyslogLogging;

    /// <summary>
    /// Storage service wrapping an S3-compatible blob client.
    /// </summary>
    public class StorageService
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[StorageService] ";
        private S3Settings _Settings = null;
        private LoggingModule _Logging = null;
        private AmazonS3BlobClient _Client = null;

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

            _Logging.Info(_Header + "initialized with bucket " + _Settings.BucketName);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Upload a file to S3-compatible storage.
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
        /// Download a file from S3-compatible storage.
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
        /// Delete a file from S3-compatible storage.
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

        #endregion

        #region Private-Methods

        #endregion
    }
}
