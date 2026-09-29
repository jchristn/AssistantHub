namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// CIFS crawl repository settings.
    /// </summary>
    public class CifsCrawlRepositorySettings : CrawlRepositorySettings
    {
        #region Public-Members

        /// <summary>
        /// CIFS hostname or IP address.
        /// </summary>
        public string CifsHostname { get; set; } = null;

        /// <summary>
        /// CIFS username.
        /// </summary>
        public string CifsUsername { get; set; } = null;

        /// <summary>
        /// CIFS password.
        /// </summary>
        public string CifsPassword { get; set; } = null;

        /// <summary>
        /// CIFS share name.
        /// </summary>
        public string CifsShareName { get; set; } = null;

        /// <summary>
        /// TCP port of the SMB server (1 to 65535). Null uses 445.
        /// </summary>
        public int? CifsPort { get; set; } = null;

        /// <summary>
        /// Domain or workgroup of the user, for example an Active Directory domain. Null takes the domain from a
        /// DOMAIN\user username, or sends none.
        /// </summary>
        public string CifsDomain { get; set; } = null;

        /// <summary>
        /// Include files in subdirectories while crawling.
        /// Default: true.
        /// </summary>
        public bool IncludeSubdirectories { get; set; } = true;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CifsCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.CIFS;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override List<string> Validate()
        {
            List<string> errors = new List<string>();
            if (String.IsNullOrWhiteSpace(CifsHostname)) errors.Add("CifsHostname is required for CIFS crawl repository settings.");
            if (String.IsNullOrWhiteSpace(CifsUsername)) errors.Add("CifsUsername is required for CIFS crawl repository settings.");
            if (String.IsNullOrWhiteSpace(CifsPassword)) errors.Add("CifsPassword is required for CIFS crawl repository settings.");
            if (String.IsNullOrWhiteSpace(CifsShareName)) errors.Add("CifsShareName is required for CIFS crawl repository settings.");
            if (CifsPort.HasValue && (CifsPort.Value < 1 || CifsPort.Value > 65535)) errors.Add("CifsPort must be between 1 and 65535.");

            // Common path mistakes: a UNC path or URL in the hostname, or a folder in the share name.
            if (!String.IsNullOrWhiteSpace(CifsHostname) && (CifsHostname.IndexOfAny(new[] { '\\', '/' }) >= 0 || CifsHostname.Contains("://")))
                errors.Add("CifsHostname must be only the server name or IPv4 address (for example fileserver.example.com), not a UNC path or URL; put the share in CifsShareName.");
            string share = (CifsShareName ?? "").Trim().Trim('\\', '/');
            if (share.IndexOfAny(new[] { '\\', '/' }) >= 0)
                errors.Add("CifsShareName must be the share only (for example Documents); to crawl a folder inside it, use Filter.ObjectPrefix (for example Policies/).");
            return errors;
        }

        #endregion
    }
}
