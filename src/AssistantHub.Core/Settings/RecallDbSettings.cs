namespace AssistantHub.Core.Settings
{
    using System;

    /// <summary>
    /// RecallDb service settings.
    /// </summary>
    public class RecallDbSettings
    {
        #region Public-Members

        /// <summary>
        /// Endpoint URL for the RecallDb service.
        /// </summary>
        public string Endpoint
        {
            get => _Endpoint;
            set { if (!String.IsNullOrEmpty(value)) _Endpoint = value; }
        }

        /// <summary>
        /// Tenant identifier for the RecallDb service.
        /// </summary>
        public string TenantId
        {
            get => _TenantId;
            set { if (!String.IsNullOrEmpty(value)) _TenantId = value; }
        }

        /// <summary>
        /// Access key for the RecallDb service.
        /// </summary>
        public string AccessKey { get; set; } = "";

        #endregion

        #region Private-Members

        private string _Endpoint = "http://localhost:8401";
        private string _TenantId = "default";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RecallDbSettings()
        {
        }

        #endregion
    }
}
