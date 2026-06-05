namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Crawl schedule settings sub-object.
    /// </summary>
    public class CrawlScheduleSettings
    {
        /// <summary>
        /// Schedule interval type.
        /// </summary>
        [JsonPropertyName("IntervalType")]
        public ScheduleIntervalEnum IntervalType { get; set; }

        /// <summary>
        /// Schedule interval value (1-10080).
        /// </summary>
        [JsonPropertyName("IntervalValue")]
        public int IntervalValue { get; set; }
    }
}
