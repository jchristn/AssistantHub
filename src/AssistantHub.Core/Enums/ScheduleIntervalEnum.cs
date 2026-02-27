namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Schedule interval enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ScheduleIntervalEnum
    {
        /// <summary>
        /// One time.
        /// </summary>
        [EnumMember(Value = "OneTime")]
        OneTime,

        /// <summary>
        /// Minutes.
        /// </summary>
        [EnumMember(Value = "Minutes")]
        Minutes,

        /// <summary>
        /// Hours.
        /// </summary>
        [EnumMember(Value = "Hours")]
        Hours,

        /// <summary>
        /// Days.
        /// </summary>
        [EnumMember(Value = "Days")]
        Days,

        /// <summary>
        /// Weeks.
        /// </summary>
        [EnumMember(Value = "Weeks")]
        Weeks
    }
}
