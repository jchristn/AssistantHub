namespace AssistantHub.Sdk.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Summarization order enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SummarizationOrderEnum
    {
        /// <summary>
        /// Bottom up.
        /// </summary>
        [EnumMember(Value = "BottomUp")]
        BottomUp,

        /// <summary>
        /// Top down.
        /// </summary>
        [EnumMember(Value = "TopDown")]
        TopDown
    }
}
