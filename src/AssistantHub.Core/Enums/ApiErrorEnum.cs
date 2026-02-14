namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// API error enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApiErrorEnum
    {
        /// <summary>
        /// Authentication failed.
        /// </summary>
        [EnumMember(Value = "AuthenticationFailed")]
        AuthenticationFailed,

        /// <summary>
        /// Authorization failed.
        /// </summary>
        [EnumMember(Value = "AuthorizationFailed")]
        AuthorizationFailed,

        /// <summary>
        /// Bad request.
        /// </summary>
        [EnumMember(Value = "BadRequest")]
        BadRequest,

        /// <summary>
        /// Not found.
        /// </summary>
        [EnumMember(Value = "NotFound")]
        NotFound,

        /// <summary>
        /// Conflict.
        /// </summary>
        [EnumMember(Value = "Conflict")]
        Conflict,

        /// <summary>
        /// Internal error.
        /// </summary>
        [EnumMember(Value = "InternalError")]
        InternalError
    }
}
