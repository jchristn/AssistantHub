namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Web authentication type enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WebAuthTypeEnum
    {
        /// <summary>
        /// None.
        /// </summary>
        [EnumMember(Value = "None")]
        None,

        /// <summary>
        /// Basic.
        /// </summary>
        [EnumMember(Value = "Basic")]
        Basic,

        /// <summary>
        /// API key.
        /// </summary>
        [EnumMember(Value = "ApiKey")]
        ApiKey,

        /// <summary>
        /// Bearer token.
        /// </summary>
        [EnumMember(Value = "BearerToken")]
        BearerToken
    }
}
