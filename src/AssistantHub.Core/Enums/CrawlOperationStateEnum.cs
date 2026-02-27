namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Crawl operation state enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CrawlOperationStateEnum
    {
        /// <summary>
        /// Not started.
        /// </summary>
        [EnumMember(Value = "NotStarted")]
        NotStarted,

        /// <summary>
        /// Starting.
        /// </summary>
        [EnumMember(Value = "Starting")]
        Starting,

        /// <summary>
        /// Enumerating.
        /// </summary>
        [EnumMember(Value = "Enumerating")]
        Enumerating,

        /// <summary>
        /// Retrieving.
        /// </summary>
        [EnumMember(Value = "Retrieving")]
        Retrieving,

        /// <summary>
        /// Success.
        /// </summary>
        [EnumMember(Value = "Success")]
        Success,

        /// <summary>
        /// Failed.
        /// </summary>
        [EnumMember(Value = "Failed")]
        Failed,

        /// <summary>
        /// Stopped.
        /// </summary>
        [EnumMember(Value = "Stopped")]
        Stopped,

        /// <summary>
        /// Canceled.
        /// </summary>
        [EnumMember(Value = "Canceled")]
        Canceled
    }
}
