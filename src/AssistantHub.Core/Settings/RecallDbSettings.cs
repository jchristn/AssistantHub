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

        /// <summary>
        /// Indicates whether RecallDB accepts a native multi-document DocumentIds search filter.
        /// When false, AssistantHub loops over single DocumentId searches and merges results server-side.
        /// </summary>
        public bool SupportsMultiDocumentFilter { get; set; } = true;

        /// <summary>
        /// HNSW <c>ef_search</c> sent with vector and hybrid searches that carry a label, tag or document filter.
        /// pgvector applies filters after the index scan, so a restrictive filter can leave fewer than the requested
        /// number of rows at RecallDB's default, which only covers the page. Zero leaves RecallDB's default in place.
        /// Range 0 to 1,000; default 400.
        /// </summary>
        public int FilteredEfSearch
        {
            get => _FilteredEfSearch;
            set => _FilteredEfSearch = Math.Clamp(value, 0, 1000);
        }

        #endregion

        #region Private-Members

        private string _Endpoint = "http://localhost:8401";
        private string _DashboardUrl = "";
        private int _FilteredEfSearch = 400;

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
