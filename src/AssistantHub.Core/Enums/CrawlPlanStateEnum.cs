namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Crawl plan state enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CrawlPlanStateEnum
    {
        /// <summary>
        /// Stopped.
        /// </summary>
        [EnumMember(Value = "Stopped")]
        Stopped,

        /// <summary>
        /// Running.
        /// </summary>
        [EnumMember(Value = "Running")]
        Running
    }
}
