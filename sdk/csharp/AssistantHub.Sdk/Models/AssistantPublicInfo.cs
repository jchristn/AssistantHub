namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Public assistant information returned without authentication.
    /// </summary>
    public class AssistantPublicInfo
    {
        /// <summary>
        /// Assistant identifier.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Assistant display name.
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Assistant description.
        /// </summary>
        [JsonPropertyName("Description")]
        public string Description { get; set; }

        /// <summary>
        /// Optional public title.
        /// </summary>
        [JsonPropertyName("Title")]
        public string Title { get; set; }

        /// <summary>
        /// Optional logo URL.
        /// </summary>
        [JsonPropertyName("LogoUrl")]
        public string LogoUrl { get; set; }

        /// <summary>
        /// Optional favicon URL.
        /// </summary>
        [JsonPropertyName("FaviconUrl")]
        public string FaviconUrl { get; set; }

        /// <summary>
        /// Whether configured endpoint models should be loaded when a chat window opens.
        /// </summary>
        [JsonPropertyName("LoadModelsOnChatOpen")]
        public bool LoadModelsOnChatOpen { get; set; }
    }
}
