namespace AssistantHub.Core.Database.Mysql.Implementations
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
    /// MySQL crawl plan methods implementation.
    /// </summary>
    public class CrawlPlanMethods : ICrawlPlanMethods
    {
        #region Private-Members

        private MysqlDatabaseDriver _Driver;
        private DatabaseSettings _Settings;
        private LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="driver">MySQL database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public CrawlPlanMethods(MysqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<CrawlPlan> CreateAsync(CrawlPlan plan, CancellationToken token = default)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            if (String.IsNullOrEmpty(plan.Id)) plan.Id = IdGenerator.NewCrawlPlanId();
            plan.CreatedUtc = DateTime.UtcNow;
            plan.LastUpdateUtc = plan.CreatedUtc;

            string ingestionJson = Serializer.SerializeJson(plan.IngestionSettings, false);
            string repoJson = Serializer.SerializeJson(plan.RepositorySettings, false);
            string scheduleJson = Serializer.SerializeJson(plan.Schedule, false);
            string filterJson = Serializer.SerializeJson(plan.Filter, false);

            string query =
                "INSERT INTO crawl_plans " +
                "(id, tenant_id, name, repository_type, ingestion_settings_json, repository_settings_json, " +
                "schedule_json, filter_json, process_additions, process_updates, process_deletions, " +
                "max_drain_tasks, retention_days, state, last_crawl_start_utc, last_crawl_finish_utc, " +
                "last_crawl_success, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(plan.Id) + "', " +
                "'" + _Driver.Sanitize(plan.TenantId) + "', " +
                "'" + _Driver.Sanitize(plan.Name) + "', " +
                "'" + _Driver.Sanitize(plan.RepositoryType.ToString()) + "', " +
                _Driver.FormatNullableString(ingestionJson) + ", " +
                _Driver.FormatNullableString(repoJson) + ", " +
                _Driver.FormatNullableString(scheduleJson) + ", " +
                _Driver.FormatNullableString(filterJson) + ", " +
                _Driver.FormatBoolean(plan.ProcessAdditions) + ", " +
                _Driver.FormatBoolean(plan.ProcessUpdates) + ", " +
                _Driver.FormatBoolean(plan.ProcessDeletions) + ", " +
                plan.MaxDrainTasks + ", " +
                plan.RetentionDays + ", " +
                "'" + _Driver.Sanitize(plan.State.ToString()) + "', " +
                _Driver.FormatNullableDateTime(plan.LastCrawlStartUtc) + ", " +
                _Driver.FormatNullableDateTime(plan.LastCrawlFinishUtc) + ", " +
                (plan.LastCrawlSuccess == null ? "NULL" : _Driver.FormatBoolean(plan.LastCrawlSuccess.Value)) + ", " +
                "'" + _Driver.FormatDateTime(plan.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(plan.LastUpdateUtc) + "'" +
                ")";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return plan;
        }

        /// <inheritdoc />
        public async Task<CrawlPlan> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM crawl_plans WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return CrawlPlan.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<CrawlPlan> UpdateAsync(CrawlPlan plan, CancellationToken token = default)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            plan.LastUpdateUtc = DateTime.UtcNow;

            string ingestionJson = Serializer.SerializeJson(plan.IngestionSettings, false);
            string repoJson = Serializer.SerializeJson(plan.RepositorySettings, false);
            string scheduleJson = Serializer.SerializeJson(plan.Schedule, false);
            string filterJson = Serializer.SerializeJson(plan.Filter, false);

            string query =
                "UPDATE crawl_plans SET " +
                "tenant_id = '" + _Driver.Sanitize(plan.TenantId) + "', " +
                "name = '" + _Driver.Sanitize(plan.Name) + "', " +
                "repository_type = '" + _Driver.Sanitize(plan.RepositoryType.ToString()) + "', " +
                "ingestion_settings_json = " + _Driver.FormatNullableString(ingestionJson) + ", " +
                "repository_settings_json = " + _Driver.FormatNullableString(repoJson) + ", " +
                "schedule_json = " + _Driver.FormatNullableString(scheduleJson) + ", " +
                "filter_json = " + _Driver.FormatNullableString(filterJson) + ", " +
                "process_additions = " + _Driver.FormatBoolean(plan.ProcessAdditions) + ", " +
                "process_updates = " + _Driver.FormatBoolean(plan.ProcessUpdates) + ", " +
                "process_deletions = " + _Driver.FormatBoolean(plan.ProcessDeletions) + ", " +
                "max_drain_tasks = " + plan.MaxDrainTasks + ", " +
                "retention_days = " + plan.RetentionDays + ", " +
                "state = '" + _Driver.Sanitize(plan.State.ToString()) + "', " +
                "last_crawl_start_utc = " + _Driver.FormatNullableDateTime(plan.LastCrawlStartUtc) + ", " +
                "last_crawl_finish_utc = " + _Driver.FormatNullableDateTime(plan.LastCrawlFinishUtc) + ", " +
                "last_crawl_success = " + (plan.LastCrawlSuccess == null ? "NULL" : _Driver.FormatBoolean(plan.LastCrawlSuccess.Value)) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(plan.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(plan.Id) + "'";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return plan;
        }

        /// <inheritdoc />
        public async Task UpdateStateAsync(string id, CrawlPlanStateEnum state, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            DateTime now = DateTime.UtcNow;

            string query =
                "UPDATE crawl_plans SET " +
                "state = '" + _Driver.Sanitize(state.ToString()) + "', " +
                "last_update_utc = '" + _Driver.FormatDateTime(now) + "' " +
                "WHERE id = '" + _Driver.Sanitize(id) + "'";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM crawl_plans WHERE id = '" + _Driver.Sanitize(id) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT COUNT(*) AS cnt FROM crawl_plans WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return false;
            int count = Convert.ToInt32(result.Rows[0]["cnt"]);
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<CrawlPlan>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (query == null) throw new ArgumentNullException(nameof(query));

            Stopwatch sw = Stopwatch.StartNew();

            int offset = 0;
            if (!String.IsNullOrEmpty(query.ContinuationToken))
            {
                if (!Int32.TryParse(query.ContinuationToken, out offset)) offset = 0;
            }

            string orderBy = query.Ordering == EnumerationOrderEnum.CreatedDescending
                ? "ORDER BY created_utc DESC"
                : "ORDER BY created_utc ASC";

            string whereClause = "WHERE tenant_id = '" + _Driver.Sanitize(tenantId) + "' ";

            string countQuery = "SELECT COUNT(*) AS cnt FROM crawl_plans " + whereClause;
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM crawl_plans " +
                whereClause +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + offset;

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            List<CrawlPlan> objects = new List<CrawlPlan>();
            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    CrawlPlan obj = CrawlPlan.FromDataRow(row);
                    if (obj != null) objects.Add(obj);
                }
            }

            long nextOffset = offset + objects.Count;
            long remaining = totalRecords - nextOffset;
            if (remaining < 0) remaining = 0;

            sw.Stop();

            EnumerationResult<CrawlPlan> enumerationResult = new EnumerationResult<CrawlPlan>();
            enumerationResult.MaxResults = query.MaxResults;
            enumerationResult.TotalRecords = totalRecords;
            enumerationResult.RecordsRemaining = remaining;
            enumerationResult.Objects = objects;
            enumerationResult.ContinuationToken = remaining > 0 ? nextOffset.ToString() : null;
            enumerationResult.EndOfResults = remaining <= 0;
            enumerationResult.TotalMs = sw.Elapsed.TotalMilliseconds;
            return enumerationResult;
        }

        #endregion
    }
}
