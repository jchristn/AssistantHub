namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Summarization traversal order.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SummarizationOrderEnum
    {
        /// <summary>
        /// Bottom-up traversal.
        /// </summary>
        [EnumMember(Value = "BottomUp")]
        BottomUp,

        /// <summary>
        /// Top-down traversal.
        /// </summary>
        [EnumMember(Value = "TopDown")]
        TopDown
    }
}
