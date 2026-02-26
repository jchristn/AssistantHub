namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Repository type enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RepositoryTypeEnum
    {
        /// <summary>
        /// Web.
        /// </summary>
        [EnumMember(Value = "Web")]
        Web
    }
}
