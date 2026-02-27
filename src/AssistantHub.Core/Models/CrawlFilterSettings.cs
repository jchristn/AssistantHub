namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Crawl filter settings sub-object.
    /// </summary>
    public class CrawlFilterSettings
    {
        #region Public-Members

        /// <summary>
        /// Object key prefix filter.
        /// </summary>
        public string ObjectPrefix { get; set; } = null;

        /// <summary>
        /// Object key suffix filter.
        /// </summary>
        public string ObjectSuffix { get; set; } = null;

        /// <summary>
        /// Allowed content types filter.
        /// </summary>
        public List<string> AllowedContentTypes { get; set; } = null;

        /// <summary>
        /// Minimum object size in bytes.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long MinimumSize
        {
            get => _MinimumSize;
            set => _MinimumSize = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Maximum object size in bytes.
        /// Nullable. Clamped >= 0 when set.
        /// </summary>
        public long? MaximumSize
        {
            get => _MaximumSize;
            set => _MaximumSize = (value == null || value >= 0) ? value : 0;
        }

        #endregion

        #region Private-Members

        private long _MinimumSize = 0;
        private long? _MaximumSize = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CrawlFilterSettings()
        {
        }

        #endregion
    }
}
