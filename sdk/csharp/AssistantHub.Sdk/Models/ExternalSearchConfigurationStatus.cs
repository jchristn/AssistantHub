namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Safe external-search configuration status.
    /// </summary>
    public class ExternalSearchConfigurationStatus
    {
        /// <summary>Whether external search is globally enabled.</summary>
        [JsonPropertyName("Enabled")]
        public bool Enabled { get; set; }

        /// <summary>Enabled provider count.</summary>
        [JsonPropertyName("EnabledProviders")]
        public int EnabledProviders { get; set; }

        /// <summary>Fully configured provider count.</summary>
        [JsonPropertyName("ConfiguredProviders")]
        public int ConfiguredProviders { get; set; }

        /// <summary>Enabled but incomplete provider count.</summary>
        [JsonPropertyName("MisconfiguredProviders")]
        public int MisconfiguredProviders { get; set; }
    }
}
