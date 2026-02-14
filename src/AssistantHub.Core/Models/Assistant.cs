namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Assistant record.
    /// </summary>
    public class Assistant
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix asst_.
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// User identifier to which this assistant belongs.
        /// </summary>
        public string UserId
        {
            get => _UserId;
            set => _UserId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(UserId));
        }

        /// <summary>
        /// Display name for the assistant.
        /// </summary>
        public string Name
        {
            get => _Name;
            set => _Name = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Name));
        }

        /// <summary>
        /// Description of the assistant.
        /// </summary>
        public string Description { get; set; } = null;

        /// <summary>
        /// Indicates whether the assistant is active.
        /// </summary>
        public bool Active { get; set; } = true;

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

        private string _Id = IdGenerator.NewAssistantId();
        private string _UserId = "usr_placeholder";
        private string _Name = "My Assistant";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Assistant()
        {
        }

        /// <summary>
        /// Create an Assistant from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>Assistant instance or null.</returns>
        public static Assistant FromDataRow(DataRow row)
        {
            if (row == null) return null;
            Assistant obj = new Assistant();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.UserId = DataTableHelper.GetStringValue(row, "user_id");
            obj.Name = DataTableHelper.GetStringValue(row, "name");
            obj.Description = DataTableHelper.GetStringValue(row, "description");
            obj.Active = DataTableHelper.GetBooleanValue(row, "active", true);
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion
    }
}
