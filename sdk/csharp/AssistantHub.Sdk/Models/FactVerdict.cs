namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Verdict for a single expected fact.
    /// </summary>
    public class FactVerdict
    {
        /// <summary>
        /// The expected fact.
        /// </summary>
        [JsonPropertyName("Fact")]
        public string Fact { get; set; }

        /// <summary>
        /// Whether the fact was found in the response.
        /// </summary>
        [JsonPropertyName("Pass")]
        public bool Pass { get; set; }

        /// <summary>
        /// Reasoning for the verdict.
        /// </summary>
        [JsonPropertyName("Reasoning")]
        public string Reasoning { get; set; }
    }
}
