namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Amazon S3 or S3-compatible (MinIO, Less3, Ceph, Wasabi, Cloudflare R2) crawl repository settings.
    /// </summary>
    public class S3CrawlRepositorySettings : CrawlRepositorySettings
    {
        #region Public-Members

        /// <summary>
        /// Service URL of an S3-compatible store, for example http://minio.example.com:9000/. Null or empty uses Amazon S3
        /// in <see cref="S3Region"/>. The scheme decides whether TLS is used.
        /// </summary>
        public string S3Endpoint { get; set; } = null;

        /// <summary>
        /// Region, for example us-east-1. Required by Amazon S3; S3-compatible stores usually accept any value.
        /// Default: us-east-1.
        /// </summary>
        public string S3Region { get; set; } = "us-east-1";

        /// <summary>
        /// Bucket name only, for example company-docs (not a URL or s3:// path).
        /// </summary>
        public string S3BucketName { get; set; } = null;

        /// <summary>
        /// Access key ID. Leave both keys empty for a public bucket.
        /// </summary>
        public string S3AccessKey { get; set; } = null;

        /// <summary>
        /// Secret access key.
        /// </summary>
        public string S3SecretKey { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public S3CrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.S3;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override List<string> Validate()
        {
            List<string> errors = new List<string>();
            string bucket = (S3BucketName ?? "").Trim();
            if (String.IsNullOrWhiteSpace(bucket)) errors.Add("S3BucketName is required for S3 crawl repository settings.");
            else if (bucket.Contains("/") || bucket.Contains(":"))
                errors.Add("S3BucketName must be the bucket name only (for example company-docs), not a URL or s3:// path; to crawl a folder, use Filter.ObjectPrefix.");
            else if (!Regex.IsMatch(bucket, "^[A-Za-z0-9][A-Za-z0-9._-]{1,254}$"))
                errors.Add("S3BucketName may contain only letters, digits, dots, hyphens and underscores.");

            if (!String.IsNullOrWhiteSpace(S3Endpoint)
                && (!Uri.TryCreate(S3Endpoint.Trim(), UriKind.Absolute, out Uri endpoint) || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)))
                errors.Add("S3Endpoint must be an absolute http or https URL (for example http://minio.example.com:9000/), or empty for Amazon S3.");

            if (String.IsNullOrWhiteSpace(S3Endpoint) && String.IsNullOrWhiteSpace(S3Region))
                errors.Add("S3Region is required for Amazon S3 (for example us-east-1).");

            if (String.IsNullOrWhiteSpace(S3AccessKey) != String.IsNullOrWhiteSpace(S3SecretKey))
                errors.Add("Set both S3AccessKey and S3SecretKey, or neither for a public bucket.");

            return errors;
        }

        #endregion
    }
}
