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
        /// Access key for the RecallDb service.
        /// </summary>
        public string AccessKey { get; set; } = "recalldbadmin";

        /// <summary>
        /// Browser URL for the RecallDb dashboard.
        /// </summary>
        public string DashboardUrl
        {
            get => _DashboardUrl;
            set { if (value != null) _DashboardUrl = value; }
        }

        #endregion

        #region Private-Members

        private string _Endpoint = "http://localhost:8401";
        private string _DashboardUrl = "";

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
