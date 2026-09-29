namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Google Cloud Storage crawl repository settings.
    /// </summary>
    public class GoogleCloudCrawlRepositorySettings : CrawlRepositorySettings
    {
        #region Public-Members

        /// <summary>
        /// Google Cloud project ID, for example contoso-docs-123456.
        /// </summary>
        public string GcpProjectId { get; set; } = null;

        /// <summary>
        /// Bucket name only, for example contoso-documents (not gs://contoso-documents).
        /// </summary>
        public string GcpBucketName { get; set; } = null;

        /// <summary>
        /// Full JSON key of a service account with read access to the bucket (Storage Object Viewer).
        /// </summary>
        public string GcpJsonCredentials { get; set; } = null;

        /// <summary>
        /// Custom endpoint, for example for a private endpoint or an emulator. Null or empty uses the standard endpoint.
        /// </summary>
        public string GcpEndpoint { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GoogleCloudCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.GoogleCloud;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override List<string> Validate()
        {
            List<string> errors = new List<string>();
            if (String.IsNullOrWhiteSpace(GcpProjectId)) errors.Add("GcpProjectId is required for Google Cloud Storage crawl repository settings.");

            string bucket = (GcpBucketName ?? "").Trim();
            if (String.IsNullOrWhiteSpace(bucket)) errors.Add("GcpBucketName is required for Google Cloud Storage crawl repository settings.");
            else if (bucket.StartsWith("gs://", StringComparison.OrdinalIgnoreCase) || bucket.Contains("/"))
                errors.Add("GcpBucketName must be the bucket name only (for example contoso-documents), not gs://bucket or a path; to crawl a folder, use Filter.ObjectPrefix.");

            if (String.IsNullOrWhiteSpace(GcpJsonCredentials))
            {
                errors.Add("GcpJsonCredentials is required: paste the full JSON key of a service account with read access to the bucket.");
            }
            else
            {
                try
                {
                    using (JsonDocument document = JsonDocument.Parse(GcpJsonCredentials))
                    {
                        if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("private_key", out _))
                            errors.Add("GcpJsonCredentials must be a service account JSON key (it contains type, project_id, private_key and client_email).");
                    }
                }
                catch (JsonException)
                {
                    errors.Add("GcpJsonCredentials is not valid JSON; paste the whole service account key file.");
                }
            }

            if (!String.IsNullOrWhiteSpace(GcpEndpoint)
                && (!Uri.TryCreate(GcpEndpoint.Trim(), UriKind.Absolute, out Uri endpoint) || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)))
                errors.Add("GcpEndpoint must be an absolute http or https URL, or empty for the standard endpoint.");

            return errors;
        }

        #endregion
    }
}
