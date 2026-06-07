namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Base class for crawl repository settings.
    /// </summary>
    public class CrawlRepositorySettings
    {
        #region Public-Members

        /// <summary>
        /// Repository type.
        /// </summary>
        public RepositoryTypeEnum RepositoryType { get; set; } = RepositoryTypeEnum.Web;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CrawlRepositorySettings()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Validate repository settings.
        /// </summary>
        /// <returns>Validation errors.</returns>
        public virtual List<string> Validate()
        {
            return new List<string>();
        }

        #endregion
    }
}
