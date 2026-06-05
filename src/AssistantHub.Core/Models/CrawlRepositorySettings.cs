namespace AssistantHub.Core.Models
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
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
    }
}
