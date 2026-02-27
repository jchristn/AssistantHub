namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Crawl operation record representing a single crawl execution.
    /// </summary>
    public class CrawlOperation
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix cop_.
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
        /// Associated crawl plan identifier.
        /// </summary>
        public string CrawlPlanId
        {
            get => _CrawlPlanId;
            set => _CrawlPlanId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(CrawlPlanId));
        }

        /// <summary>
        /// Current state of the operation.
        /// Default: NotStarted.
        /// </summary>
        public CrawlOperationStateEnum State { get; set; } = CrawlOperationStateEnum.NotStarted;

        /// <summary>
        /// Status message providing additional details.
        /// </summary>
        public string StatusMessage { get; set; } = null;

        /// <summary>
        /// Number of objects enumerated.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long ObjectsEnumerated
        {
            get => _ObjectsEnumerated;
            set => _ObjectsEnumerated = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Total bytes enumerated.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long BytesEnumerated
        {
            get => _BytesEnumerated;
            set => _BytesEnumerated = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Number of objects added.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long ObjectsAdded
        {
            get => _ObjectsAdded;
            set => _ObjectsAdded = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Total bytes added.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long BytesAdded
        {
            get => _BytesAdded;
            set => _BytesAdded = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Number of objects updated.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long ObjectsUpdated
        {
            get => _ObjectsUpdated;
            set => _ObjectsUpdated = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Total bytes updated.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long BytesUpdated
        {
            get => _BytesUpdated;
            set => _BytesUpdated = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Number of objects deleted.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long ObjectsDeleted
        {
            get => _ObjectsDeleted;
            set => _ObjectsDeleted = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Total bytes deleted.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long BytesDeleted
        {
            get => _BytesDeleted;
            set => _BytesDeleted = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Number of objects successfully processed.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long ObjectsSuccess
        {
            get => _ObjectsSuccess;
            set => _ObjectsSuccess = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Total bytes successfully processed.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long BytesSuccess
        {
            get => _BytesSuccess;
            set => _BytesSuccess = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Number of objects that failed processing.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long ObjectsFailed
        {
            get => _ObjectsFailed;
            set => _ObjectsFailed = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Total bytes that failed processing.
        /// Default: 0. Clamped >= 0.
        /// </summary>
        public long BytesFailed
        {
            get => _BytesFailed;
            set => _BytesFailed = (value >= 0) ? value : 0;
        }

        /// <summary>
        /// Path to the enumeration JSON file on disk.
        /// </summary>
        public string EnumerationFile { get; set; } = null;

        /// <summary>
        /// Operation start timestamp in UTC.
        /// </summary>
        public DateTime? StartUtc { get; set; } = null;

        /// <summary>
        /// Enumeration start timestamp in UTC.
        /// </summary>
        public DateTime? StartEnumerationUtc { get; set; } = null;

        /// <summary>
        /// Enumeration finish timestamp in UTC.
        /// </summary>
        public DateTime? FinishEnumerationUtc { get; set; } = null;

        /// <summary>
        /// Retrieval start timestamp in UTC.
        /// </summary>
        public DateTime? StartRetrievalUtc { get; set; } = null;

        /// <summary>
        /// Retrieval finish timestamp in UTC.
        /// </summary>
        public DateTime? FinishRetrievalUtc { get; set; } = null;

        /// <summary>
        /// Operation finish timestamp in UTC.
        /// </summary>
        public DateTime? FinishUtc { get; set; } = null;

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

        private string _Id = IdGenerator.NewCrawlOperationId();
        private string _TenantId = Constants.DefaultTenantId;
        private string _CrawlPlanId = null;
        private long _ObjectsEnumerated = 0;
        private long _BytesEnumerated = 0;
        private long _ObjectsAdded = 0;
        private long _BytesAdded = 0;
        private long _ObjectsUpdated = 0;
        private long _BytesUpdated = 0;
        private long _ObjectsDeleted = 0;
        private long _BytesDeleted = 0;
        private long _ObjectsSuccess = 0;
        private long _BytesSuccess = 0;
        private long _ObjectsFailed = 0;
        private long _BytesFailed = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CrawlOperation()
        {
        }

        /// <summary>
        /// Create a CrawlOperation from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>CrawlOperation instance or null.</returns>
        public static CrawlOperation FromDataRow(DataRow row)
        {
            if (row == null) return null;
            CrawlOperation obj = new CrawlOperation();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.TenantId = DataTableHelper.GetStringValue(row, "tenant_id");
            obj.CrawlPlanId = DataTableHelper.GetStringValue(row, "crawl_plan_id");
            obj.State = DataTableHelper.GetEnumValue<CrawlOperationStateEnum>(row, "state", CrawlOperationStateEnum.NotStarted);
            obj.StatusMessage = DataTableHelper.GetStringValue(row, "status_message");
            obj.ObjectsEnumerated = DataTableHelper.GetLongValue(row, "objects_enumerated");
            obj.BytesEnumerated = DataTableHelper.GetLongValue(row, "bytes_enumerated");
            obj.ObjectsAdded = DataTableHelper.GetLongValue(row, "objects_added");
            obj.BytesAdded = DataTableHelper.GetLongValue(row, "bytes_added");
            obj.ObjectsUpdated = DataTableHelper.GetLongValue(row, "objects_updated");
            obj.BytesUpdated = DataTableHelper.GetLongValue(row, "bytes_updated");
            obj.ObjectsDeleted = DataTableHelper.GetLongValue(row, "objects_deleted");
            obj.BytesDeleted = DataTableHelper.GetLongValue(row, "bytes_deleted");
            obj.ObjectsSuccess = DataTableHelper.GetLongValue(row, "objects_success");
            obj.BytesSuccess = DataTableHelper.GetLongValue(row, "bytes_success");
            obj.ObjectsFailed = DataTableHelper.GetLongValue(row, "objects_failed");
            obj.BytesFailed = DataTableHelper.GetLongValue(row, "bytes_failed");
            obj.EnumerationFile = DataTableHelper.GetStringValue(row, "enumeration_file");
            obj.StartUtc = DataTableHelper.GetNullableDateTimeValue(row, "start_utc");
            obj.StartEnumerationUtc = DataTableHelper.GetNullableDateTimeValue(row, "start_enumeration_utc");
            obj.FinishEnumerationUtc = DataTableHelper.GetNullableDateTimeValue(row, "finish_enumeration_utc");
            obj.StartRetrievalUtc = DataTableHelper.GetNullableDateTimeValue(row, "start_retrieval_utc");
            obj.FinishRetrievalUtc = DataTableHelper.GetNullableDateTimeValue(row, "finish_retrieval_utc");
            obj.FinishUtc = DataTableHelper.GetNullableDateTimeValue(row, "finish_utc");
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion
    }
}
