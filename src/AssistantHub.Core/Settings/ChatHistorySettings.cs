namespace AssistantHub.Core.Settings
{
    using System;

    /// <summary>
    /// Chat history settings.
    /// </summary>
    public class ChatHistorySettings
    {
        #region Public-Members

        /// <summary>
        /// Number of days to retain chat history records before cleanup.
        /// </summary>
        public int RetentionDays
        {
            get => _RetentionDays;
            set { if (value >= 0) _RetentionDays = value; }
        }

        #endregion

        #region Private-Members

        private int _RetentionDays = 7;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatHistorySettings()
        {
        }

        #endregion
    }
}
