namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Document processing status enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DocumentStatusEnum
    {
        /// <summary>
        /// Pending.
        /// </summary>
        [EnumMember(Value = "Pending")]
        Pending,

        /// <summary>
        /// Uploading.
        /// </summary>
        [EnumMember(Value = "Uploading")]
        Uploading,

        /// <summary>
        /// Uploaded.
        /// </summary>
        [EnumMember(Value = "Uploaded")]
        Uploaded,

        /// <summary>
        /// Type detecting.
        /// </summary>
        [EnumMember(Value = "TypeDetecting")]
        TypeDetecting,

        /// <summary>
        /// Type detection success.
        /// </summary>
        [EnumMember(Value = "TypeDetectionSuccess")]
        TypeDetectionSuccess,

        /// <summary>
        /// Type detection failed.
        /// </summary>
        [EnumMember(Value = "TypeDetectionFailed")]
        TypeDetectionFailed,

        /// <summary>
        /// Processing.
        /// </summary>
        [EnumMember(Value = "Processing")]
        Processing,

        /// <summary>
        /// Processing chunks.
        /// </summary>
        [EnumMember(Value = "ProcessingChunks")]
        ProcessingChunks,

        /// <summary>
        /// Storing embeddings.
        /// </summary>
        [EnumMember(Value = "StoringEmbeddings")]
        StoringEmbeddings,

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
