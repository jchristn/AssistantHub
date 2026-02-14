namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Enumeration order enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EnumerationOrderEnum
    {
        /// <summary>
        /// Created ascending.
        /// </summary>
        [EnumMember(Value = "CreatedAscending")]
        CreatedAscending,

        /// <summary>
        /// Created descending.
        /// </summary>
        [EnumMember(Value = "CreatedDescending")]
        CreatedDescending
    }
}
