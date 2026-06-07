namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// NFS protocol version enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NfsVersionEnum
    {
        /// <summary>
        /// NFS version 2.
        /// </summary>
        [EnumMember(Value = "V2")]
        V2,

        /// <summary>
        /// NFS version 3.
        /// </summary>
        [EnumMember(Value = "V3")]
        V3,

        /// <summary>
        /// NFS version 4.
        /// </summary>
        [EnumMember(Value = "V4")]
        V4
    }
}
