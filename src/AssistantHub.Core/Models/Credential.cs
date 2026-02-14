namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// API credential record.
    /// </summary>
    public class Credential
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix cred_.
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// User identifier to which this credential belongs.
        /// </summary>
        public string UserId
        {
            get => _UserId;
            set => _UserId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(UserId));
        }

        /// <summary>
        /// Display name for the credential.
        /// </summary>
        public string Name { get; set; } = "Default credential";

        /// <summary>
        /// Bearer token value.
        /// </summary>
        public string BearerToken
        {
            get => _BearerToken;
            set => _BearerToken = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(BearerToken));
        }

        /// <summary>
        /// Indicates whether the credential is active.
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

        private string _Id = IdGenerator.NewCredentialId();
        private string _UserId = "usr_placeholder";
        private string _BearerToken = IdGenerator.NewBearerToken();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Credential()
        {
        }

        /// <summary>
        /// Create a Credential from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>Credential instance or null.</returns>
        public static Credential FromDataRow(DataRow row)
        {
            if (row == null) return null;
            Credential obj = new Credential();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.UserId = DataTableHelper.GetStringValue(row, "user_id");
            obj.Name = DataTableHelper.GetStringValue(row, "name");
            obj.BearerToken = DataTableHelper.GetStringValue(row, "bearer_token");
            obj.Active = DataTableHelper.GetBooleanValue(row, "active", true);
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion
    }
}
