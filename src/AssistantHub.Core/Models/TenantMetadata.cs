namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.Json;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Tenant metadata record.
    /// </summary>
    public class TenantMetadata
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix ten_ (or "default" for the default tenant).
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// Display name for the tenant.
        /// </summary>
        public string Name
        {
            get => _Name;
            set => _Name = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Name));
        }

        /// <summary>
        /// Indicates whether the tenant is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Indicates whether the record is protected from deletion.
        /// </summary>
        public bool IsProtected { get; set; } = false;

        /// <summary>
        /// Labels for categorization.
        /// </summary>
        public List<string> Labels { get; set; } = null;

        /// <summary>
        /// Key-value metadata tags.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = null;

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

        private string _Id = IdGenerator.NewTenantId();
        private string _Name = "My Tenant";

        private static JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public TenantMetadata()
        {
        }

        /// <summary>
        /// Create a TenantMetadata from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>TenantMetadata instance or null.</returns>
        public static TenantMetadata FromDataRow(DataRow row)
        {
            if (row == null) return null;
            TenantMetadata obj = new TenantMetadata();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.Name = DataTableHelper.GetStringValue(row, "name");
            obj.Active = DataTableHelper.GetBooleanValue(row, "active", true);
            obj.IsProtected = DataTableHelper.GetBooleanValue(row, "is_protected");

            string labelsJson = DataTableHelper.GetStringValue(row, "labels_json");
            if (!String.IsNullOrEmpty(labelsJson))
            {
                try { obj.Labels = JsonSerializer.Deserialize<List<string>>(labelsJson, _JsonOptions); }
                catch { }
            }

            string tagsJson = DataTableHelper.GetStringValue(row, "tags_json");
            if (!String.IsNullOrEmpty(tagsJson))
            {
                try { obj.Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(tagsJson, _JsonOptions); }
                catch { }
            }

            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion
    }
}
