namespace AssistantHub.Core.Settings
{
    using System;

    /// <summary>
    /// Crawl settings.
    /// </summary>
    public class CrawlSettings
    {
        /// <summary>
        /// Directory for storing crawl enumeration files.
        /// Default: ./crawl-enumerations/
        /// </summary>
        public string EnumerationDirectory { get; set; } = "./crawl-enumerations/";
    }
}
