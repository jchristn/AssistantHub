namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Evaluation run status enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EvalStatusEnum
    {
        /// <summary>
        /// Run is pending execution.
        /// </summary>
        [EnumMember(Value = "Pending")]
        Pending,

        /// <summary>
        /// Run is currently executing.
        /// </summary>
        [EnumMember(Value = "Running")]
        Running,

        /// <summary>
        /// Run completed successfully.
        /// </summary>
        [EnumMember(Value = "Completed")]
        Completed,

        /// <summary>
        /// Run failed.
        /// </summary>
        [EnumMember(Value = "Failed")]
        Failed
    }
}
