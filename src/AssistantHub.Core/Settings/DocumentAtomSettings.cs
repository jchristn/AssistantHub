namespace AssistantHub.Core.Settings
{
    using System;

    /// <summary>
    /// DocumentAtom service settings.
    /// </summary>
    public class DocumentAtomSettings
    {
        #region Public-Members

        /// <summary>
        /// Endpoint URL for the DocumentAtom service.
        /// </summary>
        public string Endpoint
        {
            get => _Endpoint;
            set { if (!String.IsNullOrEmpty(value)) _Endpoint = value; }
        }

        /// <summary>
        /// Access key for the DocumentAtom service.
        /// </summary>
        public string AccessKey { get; set; } = "";

        /// <summary>
        /// Browser URL for the DocumentAtom dashboard.
        /// </summary>
        public string DashboardUrl
        {
            get => _DashboardUrl;
            set { if (value != null) _DashboardUrl = value; }
        }

        #endregion

        #region Private-Members

        private string _Endpoint = "http://localhost:8301";
        private string _DashboardUrl = "";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public DocumentAtomSettings()
        {
        }

        #endregion
    }
}
