namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Git repository crawl settings. Repositories on github.com are crawled through the GitHub REST API, reading the
    /// default branch. Object keys are repository-relative paths, so Filter.ObjectPrefix selects a folder.
    /// </summary>
    public class GitCrawlRepositorySettings : CrawlRepositorySettings
    {
        /// <summary>
        /// Repository URL: https://github.com/owner/repo, https://github.com/owner/repo.git or git@github.com:owner/repo.git.
        /// </summary>
        [JsonPropertyName("GitRepositoryUrl")]
        public string GitRepositoryUrl { get; set; }

        /// <summary>
        /// Personal access token. Required for private repositories, and recommended for public ones: without it GitHub
        /// allows 60 API requests an hour, with it 5,000. Sensitive: stored with the crawl plan and returned by the API;
        /// treat it as a secret.
        /// </summary>
        [JsonPropertyName("GitAccessToken")]
        public string GitAccessToken { get; set; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GitCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.Git;
        }
    }
}
