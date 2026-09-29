namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Azure Blob Storage crawl repository settings.
    /// </summary>
    public class AzureBlobCrawlRepositorySettings : CrawlRepositorySettings
    {
        #region Public-Members

        /// <summary>
        /// Storage account name, for example contosodocs.
        /// </summary>
        public string AzureAccountName { get; set; } = null;

        /// <summary>
        /// Storage account access key (key1 or key2 from Access keys in the Azure portal).
        /// </summary>
        public string AzureAccessKey { get; set; } = null;

        /// <summary>
        /// Container name only, for example documents.
        /// </summary>
        public string AzureContainer { get; set; } = null;

        /// <summary>
        /// Blob service endpoint. Null or empty uses https://{account}.blob.core.windows.net/. Set it for sovereign clouds,
        /// private endpoints or the Azurite emulator (for example http://127.0.0.1:10000/devstoreaccount1/).
        /// </summary>
        public string AzureEndpoint { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AzureBlobCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.AzureBlob;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// The endpoint to connect to: <see cref="AzureEndpoint"/>, or the public endpoint for the account.
        /// </summary>
        /// <returns>Endpoint URL ending with a slash.</returns>
        public string GetEffectiveEndpoint()
        {
            string endpoint = !String.IsNullOrWhiteSpace(AzureEndpoint)
                ? AzureEndpoint.Trim()
                : "https://" + (AzureAccountName ?? "").Trim() + ".blob.core.windows.net/";
            return endpoint.EndsWith("/", StringComparison.Ordinal) ? endpoint : endpoint + "/";
        }

        /// <inheritdoc />
        public override List<string> Validate()
        {
            List<string> errors = new List<string>();
            if (String.IsNullOrWhiteSpace(AzureAccountName)) errors.Add("AzureAccountName is required for Azure Blob crawl repository settings.");
            else if (!Regex.IsMatch(AzureAccountName.Trim(), "^[a-z0-9]{3,24}$"))
                errors.Add("AzureAccountName is the storage account name only: 3 to 24 lowercase letters and digits (for example contosodocs), not a URL.");

            if (String.IsNullOrWhiteSpace(AzureAccessKey)) errors.Add("AzureAccessKey is required for Azure Blob crawl repository settings.");

            string container = (AzureContainer ?? "").Trim();
            if (String.IsNullOrWhiteSpace(container)) errors.Add("AzureContainer is required for Azure Blob crawl repository settings.");
            else if (container.Contains("/"))
                errors.Add("AzureContainer must be the container name only (for example documents); to crawl a folder, use Filter.ObjectPrefix.");
            else if (!Regex.IsMatch(container, "^(\\$root|[a-z0-9](?:[a-z0-9-]{1,61}[a-z0-9])?)$"))
                errors.Add("AzureContainer must be 3 to 63 lowercase letters, digits and hyphens.");

            if (!String.IsNullOrWhiteSpace(AzureEndpoint)
                && (!Uri.TryCreate(AzureEndpoint.Trim(), UriKind.Absolute, out Uri endpoint) || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)))
                errors.Add("AzureEndpoint must be an absolute http or https URL, or empty for https://{account}.blob.core.windows.net/.");

            return errors;
        }

        #endregion
    }
}
