namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Services.Crawlers;

    /// <summary>
    /// Local disk crawl repository settings: a folder on the AssistantHub server's own file system. The folder must be
    /// inside one of the roots the operator lists in Crawl.AllowedLocalPaths.
    /// </summary>
    public class LocalDiskCrawlRepositorySettings : CrawlRepositorySettings
    {
        #region Public-Members

        /// <summary>
        /// Absolute path of the folder to crawl on the AssistantHub server, for example /data/documents or
        /// D:\Shared\Documents. In Docker this is a path inside the container, so the host folder must be mounted.
        /// </summary>
        public string DiskPath { get; set; } = null;

        /// <summary>
        /// Include files in subfolders while crawling.
        /// Default: true.
        /// </summary>
        public bool IncludeSubdirectories { get; set; } = true;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public LocalDiskCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.LocalDisk;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override List<string> Validate()
        {
            List<string> errors = new List<string>();
            if (String.IsNullOrWhiteSpace(DiskPath))
            {
                errors.Add("DiskPath is required for local disk crawl repository settings.");
                return errors;
            }

            string error = LocalDiskCrawlPolicy.Check(DiskPath);
            if (error != null) errors.Add(error);
            return errors;
        }

        #endregion
    }
}
