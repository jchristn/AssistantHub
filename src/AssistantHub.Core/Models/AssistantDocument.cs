namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Assistant document record.
    /// </summary>
    public class AssistantDocument
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix adoc_.
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// Assistant identifier to which this document belongs.
        /// </summary>
        public string AssistantId
        {
            get => _AssistantId;
            set => _AssistantId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(AssistantId));
        }

        /// <summary>
        /// Display name for the document.
        /// </summary>
        public string Name
        {
            get => _Name;
            set => _Name = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Name));
        }

        /// <summary>
        /// Original filename as uploaded by the user.
        /// </summary>
        public string OriginalFilename { get; set; } = null;

        /// <summary>
        /// MIME content type of the document.
        /// </summary>
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Size of the document in bytes.
        /// </summary>
        public long SizeBytes
        {
            get => _SizeBytes;
            set => _SizeBytes = (value >= 0) ? value : throw new ArgumentOutOfRangeException(nameof(SizeBytes));
        }

        /// <summary>
        /// S3 object key for the stored document.
        /// </summary>
        public string S3Key { get; set; } = null;

        /// <summary>
        /// Processing status of the document.
        /// </summary>
        public DocumentStatusEnum Status { get; set; } = DocumentStatusEnum.Pending;

        /// <summary>
        /// Status message providing additional processing details.
        /// </summary>
        public string StatusMessage { get; set; } = null;

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the record was last updated in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.NewAssistantDocumentId();
        private string _AssistantId = "asst_placeholder";
        private string _Name = "Untitled Document";
        private long _SizeBytes = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AssistantDocument()
        {
        }

        /// <summary>
        /// Create an AssistantDocument from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>AssistantDocument instance or null.</returns>
        public static AssistantDocument FromDataRow(DataRow row)
        {
            if (row == null) return null;
            AssistantDocument obj = new AssistantDocument();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.AssistantId = DataTableHelper.GetStringValue(row, "assistant_id");
            obj.Name = DataTableHelper.GetStringValue(row, "name");
            obj.OriginalFilename = DataTableHelper.GetStringValue(row, "original_filename");
            obj.ContentType = DataTableHelper.GetStringValue(row, "content_type");
            obj.SizeBytes = DataTableHelper.GetLongValue(row, "size_bytes");
            obj.S3Key = DataTableHelper.GetStringValue(row, "s3_key");
            obj.Status = DataTableHelper.GetEnumValue<DocumentStatusEnum>(row, "status", DocumentStatusEnum.Pending);
            obj.StatusMessage = DataTableHelper.GetStringValue(row, "status_message");
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion
    }
}
