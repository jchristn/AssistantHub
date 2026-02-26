#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Database.SqlServer.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// SQL Server crawl operation methods implementation.
    /// </summary>
    public class CrawlOperationMethods : ICrawlOperationMethods
    {
        #region Private-Members

        private readonly string _Header = "[CrawlOperationMethods] ";
        private readonly SqlServerDatabaseDriver _Driver;
        private readonly DatabaseSettings _Settings;
        private readonly LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="driver">SQL Server database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public CrawlOperationMethods(SqlServerDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<CrawlOperation> CreateAsync(CrawlOperation operation, CancellationToken token = default)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            if (String.IsNullOrEmpty(operation.Id)) operation.Id = IdGenerator.NewCrawlOperationId();
            operation.CreatedUtc = DateTime.UtcNow;
            operation.LastUpdateUtc = operation.CreatedUtc;

            string query =
                "INSERT INTO crawl_operations " +
                "(id, tenant_id, crawl_plan_id, state, status_message, " +
                "objects_enumerated, bytes_enumerated, objects_added, bytes_added, " +
                "objects_updated, bytes_updated, objects_deleted, bytes_deleted, " +
                "objects_success, bytes_success, objects_failed, bytes_failed, " +
                "enumeration_file, start_utc, start_enumeration_utc, finish_enumeration_utc, " +
                "start_retrieval_utc, finish_retrieval_utc, finish_utc, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(operation.Id) + "', " +
                "'" + _Driver.Sanitize(operation.TenantId) + "', " +
                "'" + _Driver.Sanitize(operation.CrawlPlanId) + "', " +
                "'" + _Driver.Sanitize(operation.State.ToString()) + "', " +
                _Driver.FormatNullableString(operation.StatusMessage) + ", " +
                operation.ObjectsEnumerated + ", " +
                operation.BytesEnumerated + ", " +
                operation.ObjectsAdded + ", " +
                operation.BytesAdded + ", " +
                operation.ObjectsUpdated + ", " +
                operation.BytesUpdated + ", " +
                operation.ObjectsDeleted + ", " +
                operation.BytesDeleted + ", " +
                operation.ObjectsSuccess + ", " +
                operation.BytesSuccess + ", " +
                operation.ObjectsFailed + ", " +
                operation.BytesFailed + ", " +
                _Driver.FormatNullableString(operation.EnumerationFile) + ", " +
                _Driver.FormatNullableDateTime(operation.StartUtc) + ", " +
                _Driver.FormatNullableDateTime(operation.StartEnumerationUtc) + ", " +
                _Driver.FormatNullableDateTime(operation.FinishEnumerationUtc) + ", " +
                _Driver.FormatNullableDateTime(operation.StartRetrievalUtc) + ", " +
                _Driver.FormatNullableDateTime(operation.FinishRetrievalUtc) + ", " +
                _Driver.FormatNullableDateTime(operation.FinishUtc) + ", " +
                "'" + _Driver.FormatDateTime(operation.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(operation.LastUpdateUtc) + "'" +
                ");";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return operation;
        }

        /// <inheritdoc />
        public async Task<CrawlOperation> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT * FROM crawl_operations WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return CrawlOperation.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<CrawlOperation> UpdateAsync(CrawlOperation operation, CancellationToken token = default)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            operation.LastUpdateUtc = DateTime.UtcNow;

            string query =
                "UPDATE crawl_operations SET " +
                "tenant_id = '" + _Driver.Sanitize(operation.TenantId) + "', " +
                "crawl_plan_id = '" + _Driver.Sanitize(operation.CrawlPlanId) + "', " +
                "state = '" + _Driver.Sanitize(operation.State.ToString()) + "', " +
                "status_message = " + _Driver.FormatNullableString(operation.StatusMessage) + ", " +
                "objects_enumerated = " + operation.ObjectsEnumerated + ", " +
                "bytes_enumerated = " + operation.BytesEnumerated + ", " +
                "objects_added = " + operation.ObjectsAdded + ", " +
                "bytes_added = " + operation.BytesAdded + ", " +
                "objects_updated = " + operation.ObjectsUpdated + ", " +
                "bytes_updated = " + operation.BytesUpdated + ", " +
                "objects_deleted = " + operation.ObjectsDeleted + ", " +
                "bytes_deleted = " + operation.BytesDeleted + ", " +
                "objects_success = " + operation.ObjectsSuccess + ", " +
                "bytes_success = " + operation.BytesSuccess + ", " +
                "objects_failed = " + operation.ObjectsFailed + ", " +
                "bytes_failed = " + operation.BytesFailed + ", " +
                "enumeration_file = " + _Driver.FormatNullableString(operation.EnumerationFile) + ", " +
                "start_utc = " + _Driver.FormatNullableDateTime(operation.StartUtc) + ", " +
                "start_enumeration_utc = " + _Driver.FormatNullableDateTime(operation.StartEnumerationUtc) + ", " +
                "finish_enumeration_utc = " + _Driver.FormatNullableDateTime(operation.FinishEnumerationUtc) + ", " +
                "start_retrieval_utc = " + _Driver.FormatNullableDateTime(operation.StartRetrievalUtc) + ", " +
                "finish_retrieval_utc = " + _Driver.FormatNullableDateTime(operation.FinishRetrievalUtc) + ", " +
                "finish_utc = " + _Driver.FormatNullableDateTime(operation.FinishUtc) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(operation.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(operation.Id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return operation;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "DELETE FROM crawl_operations WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT COUNT(*) AS cnt FROM crawl_operations WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return false;
            int count = Convert.ToInt32(result.Rows[0]["cnt"]);
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<CrawlOperation>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (query == null) throw new ArgumentNullException(nameof(query));

            Stopwatch sw = Stopwatch.StartNew();

            int maxResults = query.MaxResults;
            int skip = 0;
            if (!String.IsNullOrEmpty(query.ContinuationToken))
            {
                if (!Int32.TryParse(query.ContinuationToken, out skip)) skip = 0;
            }

            string orderBy = query.Ordering == EnumerationOrderEnum.CreatedAscending
                ? "ORDER BY created_utc ASC"
                : "ORDER BY created_utc DESC";

            string whereClause = "WHERE tenant_id = '" + _Driver.Sanitize(tenantId) + "' ";

            string countQuery = "SELECT COUNT(*) AS cnt FROM crawl_operations " + whereClause + ";";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM crawl_operations " +
                whereClause +
                orderBy + " " +
                "OFFSET " + skip + " ROWS FETCH NEXT " + maxResults + " ROWS ONLY;";

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            EnumerationResult<CrawlOperation> ret = new EnumerationResult<CrawlOperation>();
            ret.MaxResults = maxResults;
            ret.TotalRecords = totalRecords;

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    CrawlOperation obj = CrawlOperation.FromDataRow(row);
                    if (obj != null) ret.Objects.Add(obj);
                }
            }

            long nextSkip = skip + maxResults;
            ret.RecordsRemaining = Math.Max(0, totalRecords - nextSkip);

            if (nextSkip < totalRecords)
            {
                ret.ContinuationToken = nextSkip.ToString();
                ret.EndOfResults = false;
            }
            else
            {
                ret.ContinuationToken = null;
                ret.EndOfResults = true;
            }

            sw.Stop();
            ret.TotalMs = sw.Elapsed.TotalMilliseconds;
            return ret;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<CrawlOperation>> EnumerateByCrawlPlanAsync(string crawlPlanId, EnumerationQuery query, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(crawlPlanId)) throw new ArgumentNullException(nameof(crawlPlanId));
            if (query == null) throw new ArgumentNullException(nameof(query));

            Stopwatch sw = Stopwatch.StartNew();

            int maxResults = query.MaxResults;
            int skip = 0;
            if (!String.IsNullOrEmpty(query.ContinuationToken))
            {
                if (!Int32.TryParse(query.ContinuationToken, out skip)) skip = 0;
            }

            string orderBy = query.Ordering == EnumerationOrderEnum.CreatedAscending
                ? "ORDER BY created_utc ASC"
                : "ORDER BY created_utc DESC";

            string whereClause = "WHERE crawl_plan_id = '" + _Driver.Sanitize(crawlPlanId) + "' ";

            string countQuery = "SELECT COUNT(*) AS cnt FROM crawl_operations " + whereClause + ";";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM crawl_operations " +
                whereClause +
                orderBy + " " +
                "OFFSET " + skip + " ROWS FETCH NEXT " + maxResults + " ROWS ONLY;";

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            EnumerationResult<CrawlOperation> ret = new EnumerationResult<CrawlOperation>();
            ret.MaxResults = maxResults;
            ret.TotalRecords = totalRecords;

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    CrawlOperation obj = CrawlOperation.FromDataRow(row);
                    if (obj != null) ret.Objects.Add(obj);
                }
            }

            long nextSkip = skip + maxResults;
            ret.RecordsRemaining = Math.Max(0, totalRecords - nextSkip);

            if (nextSkip < totalRecords)
            {
                ret.ContinuationToken = nextSkip.ToString();
                ret.EndOfResults = false;
            }
            else
            {
                ret.ContinuationToken = null;
                ret.EndOfResults = true;
            }

            sw.Stop();
            ret.TotalMs = sw.Elapsed.TotalMilliseconds;
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteByCrawlPlanAsync(string crawlPlanId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(crawlPlanId)) throw new ArgumentNullException(nameof(crawlPlanId));

            string query =
                "DELETE FROM crawl_operations WHERE crawl_plan_id = '" + _Driver.Sanitize(crawlPlanId) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteExpiredAsync(string crawlPlanId, int retentionDays, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(crawlPlanId)) throw new ArgumentNullException(nameof(crawlPlanId));

            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);

            string query =
                "DELETE FROM crawl_operations " +
                "WHERE crawl_plan_id = '" + _Driver.Sanitize(crawlPlanId) + "' " +
                "AND created_utc < '" + _Driver.FormatDateTime(cutoff) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
