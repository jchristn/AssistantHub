namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Which folders on the AssistantHub server local disk crawl plans may read. A local disk crawler can read anything
    /// the server process can, so the operator lists the allowed roots in Crawl.AllowedLocalPaths; with none listed,
    /// local disk crawling is disabled. The server configures the policy at startup.
    /// </summary>
    public static class LocalDiskCrawlPolicy
    {
        #region Private-Members

        private static readonly object _Lock = new object();
        private static List<string> _AllowedRoots = new List<string>();

        #endregion

        #region Public-Methods

        /// <summary>
        /// The configured roots, as full paths.
        /// </summary>
        public static IReadOnlyList<string> AllowedRoots
        {
            get
            {
                lock (_Lock) return _AllowedRoots.ToList();
            }
        }

        /// <summary>
        /// Set the allowed roots (from Crawl.AllowedLocalPaths). Relative paths are resolved against the working directory.
        /// </summary>
        /// <param name="roots">Allowed root folders; null or empty disables local disk crawling.</param>
        public static void Configure(IEnumerable<string> roots)
        {
            List<string> normalized = (roots ?? Enumerable.Empty<string>())
                .Where(r => !String.IsNullOrWhiteSpace(r))
                .Select(r => Normalize(r.Trim()))
                .Distinct(PathComparer)
                .ToList();
            lock (_Lock) _AllowedRoots = normalized;
        }

        /// <summary>
        /// Check that a path may be crawled.
        /// </summary>
        /// <param name="path">Folder to crawl.</param>
        /// <returns>An error message, or null when the path is allowed.</returns>
        public static string Check(string path)
        {
            List<string> roots;
            lock (_Lock) roots = _AllowedRoots.ToList();

            if (roots.Count == 0)
                return "Local disk crawling is disabled on this server. An operator must list the folders that may be crawled in Crawl.AllowedLocalPaths (server settings).";

            if (String.IsNullOrWhiteSpace(path)) return "DiskPath is required for local disk crawl repository settings.";
            if (!Path.IsPathRooted(path.Trim()))
                return "DiskPath must be an absolute path on the AssistantHub server (for example /data/documents or D:\\Shared\\Documents).";

            string full;
            try
            {
                full = Normalize(path.Trim());
            }
            catch (Exception e) when (e is ArgumentException || e is NotSupportedException || e is PathTooLongException)
            {
                return "DiskPath is not a valid path: " + e.Message;
            }

            foreach (string root in roots)
            {
                if (PathComparer.Equals(full, root)) return null;
                string rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
                if (full.StartsWith(rootWithSeparator, PathComparison)) return null;
            }

            return "DiskPath '" + path.Trim() + "' is outside the folders this server allows for local disk crawling: " + String.Join(", ", roots) + ".";
        }

        #endregion

        #region Private-Methods

        private static StringComparison PathComparison =>
            OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        private static StringComparer PathComparer =>
            OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        private static string Normalize(string path)
        {
            // GetFullPath collapses "..", so a path cannot escape an allowed root through traversal.
            string full = Path.GetFullPath(path);
            return full.Length > Path.GetPathRoot(full).Length ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : full;
        }

        #endregion
    }
}
