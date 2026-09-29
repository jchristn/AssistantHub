namespace AssistantHub.Core.Settings
{
    using System;
    using System.Collections.Generic;

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

        /// <summary>
        /// Folders on this server that LocalDisk crawl plans may read, as absolute paths. A crawl plan's DiskPath must be
        /// one of these folders or inside one. Empty (the default) disables local disk crawling, because a local disk
        /// crawler can read anything the server process can.
        /// Default: empty.
        /// </summary>
        public List<string> AllowedLocalPaths
        {
            get => _AllowedLocalPaths;
            set => _AllowedLocalPaths = value ?? new List<string>();
        }

        private List<string> _AllowedLocalPaths = new List<string>();
    }
}
