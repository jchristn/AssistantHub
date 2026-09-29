namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Git repository crawl settings. Repositories on github.com are crawled through the GitHub REST API (GitHubCrawler),
    /// reading the default branch.
    /// </summary>
    public class GitCrawlRepositorySettings : CrawlRepositorySettings
    {
        #region Public-Members

        /// <summary>
        /// Repository URL: https://github.com/owner/repo, https://github.com/owner/repo.git or git@github.com:owner/repo.git.
        /// </summary>
        public string GitRepositoryUrl { get; set; } = null;

        /// <summary>
        /// Personal access token. Required for private repositories, and recommended for public ones: without it GitHub
        /// allows 60 API requests an hour (one per folder), with it 5,000.
        /// </summary>
        public string GitAccessToken { get; set; } = null;

        #endregion

        #region Private-Members

        private static readonly Regex _GitHubUrl = new Regex(
            "^(?:https?://github\\.com/|git@github\\.com:)(?<owner>[A-Za-z0-9-]+)/(?<repo>[A-Za-z0-9._-]+?)(?:\\.git)?/?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GitCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.Git;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Parse the owner and repository name from <see cref="GitRepositoryUrl"/>.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repository">Repository name.</param>
        /// <returns>True when the URL is a supported GitHub repository URL.</returns>
        public bool TryParseRepository(out string owner, out string repository)
        {
            owner = null;
            repository = null;
            Match match = _GitHubUrl.Match((GitRepositoryUrl ?? "").Trim());
            if (!match.Success) return false;
            owner = match.Groups["owner"].Value;
            repository = match.Groups["repo"].Value;
            return true;
        }

        /// <inheritdoc />
        public override List<string> Validate()
        {
            List<string> errors = new List<string>();
            if (String.IsNullOrWhiteSpace(GitRepositoryUrl))
            {
                errors.Add("GitRepositoryUrl is required for Git crawl repository settings.");
            }
            else if (!TryParseRepository(out _, out _))
            {
                errors.Add("GitRepositoryUrl must be a GitHub repository URL such as https://github.com/owner/repo (a branch, folder or file URL is not accepted; use Filter.ObjectPrefix for a folder). Other Git hosts are not supported yet.");
            }

            return errors;
        }

        #endregion
    }
}
