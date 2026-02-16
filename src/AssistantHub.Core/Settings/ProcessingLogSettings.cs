namespace AssistantHub.Core.Settings
{
    using System;

    /// <summary>
    /// Per-document processing log settings.
    /// </summary>
    public class ProcessingLogSettings
    {
        #region Public-Members

        /// <summary>
        /// Directory where per-document processing log files are stored.
        /// </summary>
        public string Directory
        {
            get => _Directory;
            set { if (!String.IsNullOrEmpty(value)) _Directory = value; }
        }

        /// <summary>
        /// Number of days to retain processing log files before cleanup.
        /// </summary>
        public int RetentionDays
        {
            get => _RetentionDays;
            set { if (value >= 0) _RetentionDays = value; }
        }

        #endregion

        #region Private-Members

        private string _Directory = "./processing-logs/";
        private int _RetentionDays = 30;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ProcessingLogSettings()
        {
        }

        #endregion
    }
}
