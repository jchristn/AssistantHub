namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request body for creating a bucket.
    /// </summary>
    public class BucketCreateRequest
    {
        /// <summary>
        /// Bucket name.
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; }
    }
}
