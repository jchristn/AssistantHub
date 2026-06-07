namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// NFS crawl repository settings.
    /// </summary>
    public class NfsCrawlRepositorySettings : CrawlRepositorySettings
    {
        #region Public-Members

        /// <summary>
        /// NFS hostname or IP address.
        /// </summary>
        public string NfsHostname { get; set; } = null;

        /// <summary>
        /// NFS user identifier.
        /// Minimum: 0.
        /// </summary>
        public int? NfsUserId
        {
            get => _NfsUserId;
            set => _NfsUserId = (value == null || value.Value >= 0) ? value : throw new ArgumentOutOfRangeException(nameof(NfsUserId));
        }

        /// <summary>
        /// NFS group identifier.
        /// Minimum: 0.
        /// </summary>
        public int? NfsGroupId
        {
            get => _NfsGroupId;
            set => _NfsGroupId = (value == null || value.Value >= 0) ? value : throw new ArgumentOutOfRangeException(nameof(NfsGroupId));
        }

        /// <summary>
        /// NFS share name.
        /// </summary>
        public string NfsShareName { get; set; } = null;

        /// <summary>
        /// NFS protocol version.
        /// Default: V3.
        /// </summary>
        public NfsVersionEnum NfsVersion { get; set; } = NfsVersionEnum.V3;

        /// <summary>
        /// Include files in subdirectories while crawling.
        /// Default: true.
        /// </summary>
        public bool IncludeSubdirectories { get; set; } = true;

        #endregion

        #region Private-Members

        private int? _NfsUserId = null;
        private int? _NfsGroupId = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public NfsCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.NFS;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override List<string> Validate()
        {
            List<string> errors = new List<string>();
            if (String.IsNullOrWhiteSpace(NfsHostname)) errors.Add("NfsHostname is required for NFS crawl repository settings.");
            if (NfsUserId == null) errors.Add("NfsUserId is required for NFS crawl repository settings.");
            if (NfsGroupId == null) errors.Add("NfsGroupId is required for NFS crawl repository settings.");
            if (String.IsNullOrWhiteSpace(NfsShareName)) errors.Add("NfsShareName is required for NFS crawl repository settings.");
            return errors;
        }

        #endregion
    }
}
