namespace AssistantHub.Core.Settings
{
    using System;

    /// <summary>
    /// Chunking service settings.
    /// </summary>
    public class ChunkingSettings
    {
        #region Public-Members

        /// <summary>
        /// Endpoint URL for the chunking service.
        /// </summary>
        public string Endpoint
        {
            get => _Endpoint;
            set { if (!String.IsNullOrEmpty(value)) _Endpoint = value; }
        }

        /// <summary>
        /// Access key for the chunking service.
        /// </summary>
        public string AccessKey { get; set; } = "";

        /// <summary>
        /// Endpoint identifier.
        /// </summary>
        public string EndpointId
        {
            get => _EndpointId;
            set { if (!String.IsNullOrEmpty(value)) _EndpointId = value; }
        }

        #endregion

        #region Private-Members

        private string _Endpoint = "http://localhost:8321";
        private string _EndpointId = "default";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChunkingSettings()
        {
        }

        #endregion
    }
}
