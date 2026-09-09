namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Queryable performance event capturing the duration of a single document ingestion stage.
    /// </summary>
    public class DocumentPerformanceEvent
    {
        /// <summary>
        /// Event identifier.
        /// </summary>
        public string Id { get; set; } = IdGenerator.NewDocumentPerformanceEventId();

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = Constants.DefaultTenantId;

        /// <summary>
        /// Associated document identifier.
        /// </summary>
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// Ingestion rule identifier applied to the document, when available.
        /// </summary>
        public string IngestionRuleId { get; set; } = null;

        /// <summary>
        /// Ordering value within the ingestion pipeline.
        /// </summary>
        public int SequenceNumber { get; set; } = 0;

        /// <summary>
        /// Stage name, such as "File download from S3", "Type detection", "Summarization", or "Chunking".
        /// </summary>
        public string Stage { get; set; } = null;

        /// <summary>
        /// Optional human-readable detail describing the stage result.
        /// </summary>
        public string Detail { get; set; } = null;

        /// <summary>
        /// UTC timestamp when the stage started.
        /// </summary>
        public DateTime? StartedUtc { get; set; } = null;

        /// <summary>
        /// UTC timestamp when the stage finished.
        /// </summary>
        public DateTime? FinishedUtc { get; set; } = null;

        /// <summary>
        /// Stage duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        /// <summary>
        /// Indicates whether the stage completed successfully.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Error message, when the stage failed.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// UTC timestamp when the event row was created.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Build an event from a data row.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>Document performance event.</returns>
        public static DocumentPerformanceEvent FromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new DocumentPerformanceEvent
            {
                Id = DataTableHelper.GetStringValue(row, "id"),
                TenantId = DataTableHelper.GetStringValue(row, "tenant_id"),
                DocumentId = DataTableHelper.GetStringValue(row, "document_id"),
                IngestionRuleId = DataTableHelper.GetStringValue(row, "ingestion_rule_id"),
                SequenceNumber = DataTableHelper.GetIntValue(row, "sequence_number"),
                Stage = DataTableHelper.GetStringValue(row, "stage"),
                Detail = DataTableHelper.GetStringValue(row, "detail"),
                StartedUtc = DataTableHelper.GetNullableDateTimeValue(row, "started_utc"),
                FinishedUtc = DataTableHelper.GetNullableDateTimeValue(row, "finished_utc"),
                DurationMs = DataTableHelper.GetDoubleValue(row, "duration_ms"),
                Success = DataTableHelper.GetBooleanValue(row, "success", true),
                ErrorMessage = DataTableHelper.GetStringValue(row, "error_message"),
                CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc")
            };
        }
    }
}
