namespace AssistantHub.Sdk.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Evaluation status enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EvalStatusEnum
    {
        /// <summary>
        /// Pending.
        /// </summary>
        [EnumMember(Value = "Pending")]
        Pending,

        /// <summary>
        /// Running.
        /// </summary>
        [EnumMember(Value = "Running")]
        Running,

        /// <summary>
        /// Completed.
        /// </summary>
        [EnumMember(Value = "Completed")]
        Completed,

        /// <summary>
        /// Failed.
        /// </summary>
        [EnumMember(Value = "Failed")]
        Failed
    }
}
