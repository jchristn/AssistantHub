namespace AssistantHub.Core.Settings
{
    using System;

    /// <summary>
    /// S3-compatible storage settings.
    /// </summary>
    public class S3Settings
    {
        #region Public-Members

        /// <summary>
        /// AWS region.
        /// </summary>
        public string Region
        {
            get => _Region;
            set { if (!String.IsNullOrEmpty(value)) _Region = value; }
        }

        /// <summary>
        /// Bucket name.
        /// </summary>
        public string BucketName
        {
            get => _BucketName;
            set { if (!String.IsNullOrEmpty(value)) _BucketName = value; }
        }

        /// <summary>
        /// Access key.
        /// </summary>
        public string AccessKey { get; set; } = "default";

        /// <summary>
        /// Secret key.
        /// </summary>
        public string SecretKey { get; set; } = "default";

        /// <summary>
        /// Endpoint URL for S3-compatible storage.
        /// </summary>
        public string EndpointUrl
        {
            get => _EndpointUrl;
            set { if (!String.IsNullOrEmpty(value)) _EndpointUrl = value; }
        }

        /// <summary>
        /// Enable or disable SSL.
        /// </summary>
        public bool UseSsl { get; set; } = false;

        /// <summary>
        /// Base URL for accessing stored objects.
        /// </summary>
        public string BaseUrl
        {
            get => _BaseUrl;
            set { if (!String.IsNullOrEmpty(value)) _BaseUrl = value; }
        }

        #endregion

        #region Private-Members

        private string _Region = "USWest1";
        private string _BucketName = "assistanthub";
        private string _EndpointUrl = "http://localhost:8000";
        private string _BaseUrl = "http://localhost:8000";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public S3Settings()
        {
        }

        #endregion
    }
}
