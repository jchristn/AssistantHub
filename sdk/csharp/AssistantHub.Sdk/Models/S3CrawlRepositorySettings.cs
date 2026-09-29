namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Amazon S3 or S3-compatible (MinIO, Less3, Ceph, Wasabi, Cloudflare R2) crawl repository settings.
    /// </summary>
    public class S3CrawlRepositorySettings : CrawlRepositorySettings
    {
        /// <summary>
        /// Service URL of an S3-compatible store, for example http://minio.example.com:9000/. Null or empty uses Amazon S3
        /// in <see cref="S3Region"/>. The scheme decides whether TLS is used.
        /// </summary>
        [JsonPropertyName("S3Endpoint")]
        public string S3Endpoint { get; set; }

        /// <summary>
        /// Region, for example us-east-1. Required by Amazon S3; S3-compatible stores usually accept any value.
        /// Default: us-east-1.
        /// </summary>
        [JsonPropertyName("S3Region")]
        public string S3Region { get; set; } = "us-east-1";

        /// <summary>
        /// Bucket name only, for example company-docs (not a URL or s3:// path).
        /// </summary>
        [JsonPropertyName("S3BucketName")]
        public string S3BucketName { get; set; }

        /// <summary>
        /// Access key ID. Leave both keys empty for a public bucket.
        /// </summary>
        [JsonPropertyName("S3AccessKey")]
        public string S3AccessKey { get; set; }

        /// <summary>
        /// Secret access key. Sensitive: stored with the crawl plan and returned by the API; treat it as a secret.
        /// </summary>
        [JsonPropertyName("S3SecretKey")]
        public string S3SecretKey { get; set; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        public S3CrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.S3;
        }
    }
}
