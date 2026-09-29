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
        /// NFS protocol version. Only V3 is supported: Blobject 6 (OpenNFS) rejects NFSv2 and NFSv4, so a plan with
        /// another version fails validation.
        /// Default: V3.
        /// </summary>
        public NfsVersionEnum NfsVersion { get; set; } = NfsVersionEnum.V3;

        /// <summary>
        /// TCP port of the NFS service (1 to 65535). Null uses 2049.
        /// </summary>
        public int? NfsPort { get; set; } = null;

        /// <summary>
        /// TCP port of the MOUNT service (1 to 65535). Null or 0 discovers it through the portmapper.
        /// </summary>
        public int? NfsMountPort { get; set; } = null;

        /// <summary>
        /// TCP port of the portmapper (rpcbind) used to discover the MOUNT port (1 to 65535). Null uses 111.
        /// </summary>
        public int? NfsPortmapperPort { get; set; } = null;

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
            if (NfsVersion != NfsVersionEnum.V3) errors.Add("NfsVersion " + NfsVersion + " is not supported; NFS crawling supports V3 only.");
            if (NfsPort.HasValue && (NfsPort.Value < 1 || NfsPort.Value > 65535)) errors.Add("NfsPort must be between 1 and 65535.");

            // Common path mistakes: host:/export in the hostname, or a relative export path.
            if (!String.IsNullOrWhiteSpace(NfsHostname) && (NfsHostname.IndexOfAny(new[] { '\\', '/', ':' }) >= 0))
                errors.Add("NfsHostname must be only the server name or IPv4 address (for example nfs.example.com), without the export path; put the export in NfsShareName.");
            if (!String.IsNullOrWhiteSpace(NfsShareName) && !NfsShareName.Trim().Replace('\\', '/').StartsWith("/", StringComparison.Ordinal))
                errors.Add("NfsShareName must be the absolute export path, starting with / (for example /exports/content).");
            if (NfsMountPort.HasValue && (NfsMountPort.Value < 0 || NfsMountPort.Value > 65535)) errors.Add("NfsMountPort must be between 0 and 65535.");
            if (NfsPortmapperPort.HasValue && (NfsPortmapperPort.Value < 1 || NfsPortmapperPort.Value > 65535)) errors.Add("NfsPortmapperPort must be between 1 and 65535.");
            return errors;
        }

        #endregion
    }
}
