namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Crawl plan record.
    /// </summary>
    public class CrawlPlan
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix cplan_.
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId
        {
            get => _TenantId;
            set => _TenantId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(TenantId));
        }

        /// <summary>
        /// Display name for the crawl plan.
        /// Default: My crawl plan.
        /// </summary>
        public string Name
        {
            get => _Name;
            set => _Name = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Name));
        }

        /// <summary>
        /// Repository type.
        /// Default: Web.
        /// </summary>
        public RepositoryTypeEnum RepositoryType { get; set; } = RepositoryTypeEnum.Web;

        /// <summary>
        /// Ingestion settings (stored as JSON).
        /// </summary>
        public CrawlIngestionSettings IngestionSettings { get; set; } = new CrawlIngestionSettings();

        /// <summary>
        /// Repository settings (stored as JSON).
        /// </summary>
        public CrawlRepositorySettings RepositorySettings { get; set; } = new WebCrawlRepositorySettings();

        /// <summary>
        /// Schedule settings (stored as JSON).
        /// </summary>
        public CrawlScheduleSettings Schedule { get; set; } = new CrawlScheduleSettings();

        /// <summary>
        /// Filter settings (stored as JSON).
        /// </summary>
        public CrawlFilterSettings Filter { get; set; } = new CrawlFilterSettings();

        /// <summary>
        /// Process new objects found during crawling.
        /// Default: true.
        /// </summary>
        public bool ProcessAdditions { get; set; } = true;

        /// <summary>
        /// Process updated objects found during crawling.
        /// Default: true.
        /// </summary>
        public bool ProcessUpdates { get; set; } = true;

        /// <summary>
        /// Process deleted objects found during crawling.
        /// Default: false.
        /// </summary>
        public bool ProcessDeletions { get; set; } = false;

        /// <summary>
        /// Maximum concurrent drain tasks.
        /// Default: 8. Clamped 1-64.
        /// </summary>
        public int MaxDrainTasks
        {
            get => _MaxDrainTasks;
            set => _MaxDrainTasks = Math.Clamp(value, 1, 64);
        }

        /// <summary>
        /// Operation retention in days.
        /// Default: 7. Clamped 0-14.
        /// </summary>
        public int RetentionDays
        {
            get => _RetentionDays;
            set => _RetentionDays = Math.Clamp(value, 0, 14);
        }

        /// <summary>
        /// Current state of the crawl plan.
        /// Default: Stopped.
        /// </summary>
        public CrawlPlanStateEnum State { get; set; } = CrawlPlanStateEnum.Stopped;

        /// <summary>
        /// Timestamp of the last crawl start in UTC.
        /// </summary>
        public DateTime? LastCrawlStartUtc { get; set; } = null;

        /// <summary>
        /// Timestamp of the last crawl finish in UTC.
        /// </summary>
        public DateTime? LastCrawlFinishUtc { get; set; } = null;

        /// <summary>
        /// Whether the last crawl was successful.
        /// </summary>
        public bool? LastCrawlSuccess { get; set; } = null;

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

        private string _Id = IdGenerator.NewCrawlPlanId();
        private string _TenantId = Constants.DefaultTenantId;
        private string _Name = "My crawl plan";
        private int _MaxDrainTasks = 8;
        private int _RetentionDays = 7;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CrawlPlan()
        {
        }

        /// <summary>
        /// Create a CrawlPlan from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>CrawlPlan instance or null.</returns>
        public static CrawlPlan FromDataRow(DataRow row)
        {
            if (row == null) return null;
            CrawlPlan obj = new CrawlPlan();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.TenantId = DataTableHelper.GetStringValue(row, "tenant_id");
            obj.Name = DataTableHelper.GetStringValue(row, "name");
            obj.RepositoryType = DataTableHelper.GetEnumValue<RepositoryTypeEnum>(row, "repository_type", RepositoryTypeEnum.Web);

            string ingestionJson = DataTableHelper.GetStringValue(row, "ingestion_settings_json");
            if (!String.IsNullOrEmpty(ingestionJson))
                obj.IngestionSettings = Serializer.DeserializeJson<CrawlIngestionSettings>(ingestionJson);

            string repoJson = DataTableHelper.GetStringValue(row, "repository_settings_json");
            if (!String.IsNullOrEmpty(repoJson))
                obj.RepositorySettings = Serializer.DeserializeJson<CrawlRepositorySettings>(repoJson);

            string scheduleJson = DataTableHelper.GetStringValue(row, "schedule_json");
            if (!String.IsNullOrEmpty(scheduleJson))
                obj.Schedule = Serializer.DeserializeJson<CrawlScheduleSettings>(scheduleJson);

            string filterJson = DataTableHelper.GetStringValue(row, "filter_json");
            if (!String.IsNullOrEmpty(filterJson))
                obj.Filter = Serializer.DeserializeJson<CrawlFilterSettings>(filterJson);

            obj.ProcessAdditions = DataTableHelper.GetBooleanValue(row, "process_additions", true);
            obj.ProcessUpdates = DataTableHelper.GetBooleanValue(row, "process_updates", true);
            obj.ProcessDeletions = DataTableHelper.GetBooleanValue(row, "process_deletions", false);
            obj.MaxDrainTasks = DataTableHelper.GetIntValue(row, "max_drain_tasks", 8);
            obj.RetentionDays = DataTableHelper.GetIntValue(row, "retention_days", 7);
            obj.State = DataTableHelper.GetEnumValue<CrawlPlanStateEnum>(row, "state", CrawlPlanStateEnum.Stopped);
            obj.LastCrawlStartUtc = DataTableHelper.GetNullableDateTimeValue(row, "last_crawl_start_utc");
            obj.LastCrawlFinishUtc = DataTableHelper.GetNullableDateTimeValue(row, "last_crawl_finish_utc");

            string lastSuccess = DataTableHelper.GetStringValue(row, "last_crawl_success");
            if (!String.IsNullOrEmpty(lastSuccess))
                obj.LastCrawlSuccess = (lastSuccess == "1" || lastSuccess.Equals("True", StringComparison.OrdinalIgnoreCase));

            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion
    }
}
