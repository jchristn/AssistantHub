namespace AssistantHub.Core.Settings
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Verbex service settings.
    /// </summary>
    public class VerbexSettings
    {
        #region Public-Members

        /// <summary>
        /// Endpoint URL for the Verbex service.
        /// </summary>
        public string Endpoint
        {
            get => _Endpoint;
            set { if (!String.IsNullOrEmpty(value)) _Endpoint = value; }
        }

        /// <summary>
        /// Access key for the Verbex service.
        /// </summary>
        public string AccessKey { get; set; } = "verbexadmin";

        /// <summary>
        /// Browser URL for the Verbex dashboard.
        /// </summary>
        public string DashboardUrl
        {
            get => _DashboardUrl;
            set { if (value != null) _DashboardUrl = value; }
        }

        /// <summary>
        /// Default index identifier used for document ingestion.
        /// </summary>
        public string DefaultIndexId
        {
            get => _DefaultIndexId;
            set { if (!String.IsNullOrEmpty(value)) _DefaultIndexId = value; }
        }

        /// <summary>
        /// Enable document text ingestion into Verbex.
        /// </summary>
        public bool EnableIngestion { get; set; } = true;

        /// <summary>
        /// Fail document ingestion when Verbex indexing fails.
        /// </summary>
        public bool RequireIngestion { get; set; } = true;

        /// <summary>
        /// Maximum normalized content characters sent to Verbex for one record.  Zero means unlimited.
        /// </summary>
        public int MaxContentCharacters
        {
            get => _MaxContentCharacters;
            set => _MaxContentCharacters = Math.Max(0, value);
        }

        /// <summary>
        /// Maximum concurrent document indexing requests sent to Verbex by this process.
        /// </summary>
        public int MaxConcurrentIndexingRequests
        {
            get => _MaxConcurrentIndexingRequests;
            set => _MaxConcurrentIndexingRequests = Math.Clamp(value, 1, 64);
        }

        /// <summary>
        /// Number of retries for transient Verbex indexing failures.
        /// </summary>
        public int IndexingRetryCount
        {
            get => _IndexingRetryCount;
            set => _IndexingRetryCount = Math.Clamp(value, 0, 10);
        }

        /// <summary>
        /// Base delay in milliseconds before retrying a transient Verbex indexing failure.
        /// </summary>
        public int IndexingRetryDelayMs
        {
            get => _IndexingRetryDelayMs;
            set => _IndexingRetryDelayMs = Math.Clamp(value, 0, 60000);
        }

        #endregion

        #region Private-Members

        private string _Endpoint = "http://localhost:8501";
        private string _DashboardUrl = "";
        private string _DefaultIndexId = "default";
        private int _MaxContentCharacters = 0;
        private int _MaxConcurrentIndexingRequests = 2;
        private int _IndexingRetryCount = 2;
        private int _IndexingRetryDelayMs = 2000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public VerbexSettings()
        {
        }

        /// <summary>
        /// Validate Verbex settings.
        /// </summary>
        /// <returns>List of validation errors.</returns>
        public List<string> Validate()
        {
            List<string> errors = new List<string>();

            if (String.IsNullOrWhiteSpace(Endpoint))
            {
                errors.Add("Verbex.Endpoint is required.");
            }
            else if (!IsHttpUrl(Endpoint))
            {
                errors.Add("Verbex.Endpoint must be an absolute http or https URL.");
            }

            if (!String.IsNullOrWhiteSpace(DashboardUrl) && !IsHttpUrl(DashboardUrl))
                errors.Add("Verbex.DashboardUrl must be an absolute http or https URL when set.");

            if (String.IsNullOrWhiteSpace(DefaultIndexId))
            {
                errors.Add("Verbex.DefaultIndexId is required.");
            }
            else if (ContainsUnsafePathSegmentCharacter(DefaultIndexId))
            {
                errors.Add("Verbex.DefaultIndexId must not contain path separators or control characters.");
            }

            if (EnableIngestion && String.IsNullOrWhiteSpace(AccessKey))
                errors.Add("Verbex.AccessKey is required when Verbex.EnableIngestion is true.");

            return errors;
        }

        #endregion

        #region Private-Methods

        private static bool IsHttpUrl(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                return false;

            return String.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || String.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsUnsafePathSegmentCharacter(string value)
        {
            foreach (char c in value)
            {
                if (Char.IsControl(c) || c == '/' || c == '\\')
                    return true;
            }

            return false;
        }

        #endregion
    }
}
