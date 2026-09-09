#pragma warning disable CS8625, CS8603

namespace AssistantHub.Core.Database.Mysql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// MySQL document performance event methods.
    /// </summary>
    public class DocumentPerformanceEventMethods : IDocumentPerformanceEventMethods
    {
        private readonly MysqlDatabaseDriver _Driver;

        /// <summary>
        /// Instantiate the MySQL document performance-event data access layer.
        /// </summary>
        /// <param name="driver">MySQL database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public DocumentPerformanceEventMethods(MysqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        /// <inheritdoc />
        public async Task<DocumentPerformanceEvent> CreateAsync(DocumentPerformanceEvent evt, CancellationToken token = default)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));
            if (String.IsNullOrEmpty(evt.Id)) evt.Id = IdGenerator.NewDocumentPerformanceEventId();
            evt.CreatedUtc = DateTime.UtcNow;
            await _Driver.ExecuteQueryAsync(BuildInsert(evt), true, token).ConfigureAwait(false);
            return evt;
        }

        /// <inheritdoc />
        public async Task CreateManyAsync(IEnumerable<DocumentPerformanceEvent> events, CancellationToken token = default)
        {
            if (events == null) return;
            List<string> queries = events.Where(e => e != null).Select(BuildInsert).ToList();
            if (queries.Count < 1) return;
            await _Driver.ExecuteQueriesAsync(queries, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<DocumentPerformanceEvent>> ListByDocumentIdAsync(string documentId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(documentId)) throw new ArgumentNullException(nameof(documentId));
            DataTable result = await _Driver.ExecuteQueryAsync("SELECT * FROM document_performance_events WHERE document_id = '" + _Driver.Sanitize(documentId) + "' ORDER BY sequence_number ASC, created_utc ASC", false, token).ConfigureAwait(false);
            List<DocumentPerformanceEvent> ret = new List<DocumentPerformanceEvent>();
            if (result == null) return ret;
            foreach (DataRow row in result.Rows) ret.Add(DocumentPerformanceEvent.FromDataRow(row));
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteByDocumentIdAsync(string documentId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(documentId)) throw new ArgumentNullException(nameof(documentId));
            await _Driver.ExecuteQueryAsync("DELETE FROM document_performance_events WHERE document_id = '" + _Driver.Sanitize(documentId) + "'", true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            await _Driver.ExecuteQueryAsync("DELETE FROM document_performance_events WHERE created_utc < '" + _Driver.FormatDateTime(cutoff) + "'", true, token).ConfigureAwait(false);
        }

        private string BuildInsert(DocumentPerformanceEvent evt)
        {
            if (String.IsNullOrEmpty(evt.Id)) evt.Id = IdGenerator.NewDocumentPerformanceEventId();
            if (evt.CreatedUtc == default) evt.CreatedUtc = DateTime.UtcNow;

            return
                "INSERT INTO document_performance_events " +
                "(id, tenant_id, document_id, ingestion_rule_id, sequence_number, stage, detail, started_utc, finished_utc, duration_ms, success, error_message, created_utc) VALUES (" +
                _Driver.FormatNullableString(evt.Id) + ", " +
                _Driver.FormatNullableString(evt.TenantId) + ", " +
                _Driver.FormatNullableString(evt.DocumentId) + ", " +
                _Driver.FormatNullableString(evt.IngestionRuleId) + ", " +
                evt.SequenceNumber + ", " +
                _Driver.FormatNullableString(evt.Stage) + ", " +
                _Driver.FormatNullableString(evt.Detail) + ", " +
                _Driver.FormatNullableDateTime(evt.StartedUtc) + ", " +
                _Driver.FormatNullableDateTime(evt.FinishedUtc) + ", " +
                _Driver.FormatDouble(evt.DurationMs) + ", " +
                _Driver.FormatBoolean(evt.Success) + ", " +
                _Driver.FormatNullableString(evt.ErrorMessage) + ", " +
                _Driver.FormatNullableDateTime(evt.CreatedUtc) +
                ")";
        }
    }
}

#pragma warning restore CS8625, CS8603
