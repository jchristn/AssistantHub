namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Response containing an identifier for a created resource.
    /// </summary>
    public class IdentifierResponse
    {
        /// <summary>
        /// Resource identifier.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Resource GUID.
        /// </summary>
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }
    }
}
