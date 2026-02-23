#pragma warning disable CS8625, CS8603, CS8600

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
    /// MySQL ingestion rule methods implementation.
    /// </summary>
    public class IngestionRuleMethods : IIngestionRuleMethods
    {
        #region Private-Members

        private readonly string _Header = "[IngestionRuleMethods] ";
        private readonly MysqlDatabaseDriver _Driver;
        private readonly DatabaseSettings _Settings;
        private readonly LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="driver">MySQL database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public IngestionRuleMethods(MysqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<IngestionRule> CreateAsync(IngestionRule rule, CancellationToken token = default)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            if (String.IsNullOrEmpty(rule.Id)) rule.Id = IdGenerator.NewIngestionRuleId();
            rule.CreatedUtc = DateTime.UtcNow;
            rule.LastUpdateUtc = rule.CreatedUtc;

            string query =
                "INSERT INTO ingestion_rules " +
                "(id, name, description, bucket, collection_name, collection_id, labels_json, tags_json, atomization_json, summarization_json, chunking_json, embedding_json, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(rule.Id) + "', " +
                "'" + _Driver.Sanitize(rule.Name) + "', " +
                _Driver.FormatNullableString(rule.Description) + ", " +
                "'" + _Driver.Sanitize(rule.Bucket) + "', " +
                "'" + _Driver.Sanitize(rule.CollectionName) + "', " +
                _Driver.FormatNullableString(rule.CollectionId) + ", " +
                _Driver.FormatNullableString(Serializer.SerializeJson(rule.Labels)) + ", " +
                _Driver.FormatNullableString(Serializer.SerializeJson(rule.Tags)) + ", " +
                _Driver.FormatNullableString(Serializer.SerializeJson(rule.Atomization)) + ", " +
                _Driver.FormatNullableString(Serializer.SerializeJson(rule.Summarization)) + ", " +
                _Driver.FormatNullableString(Serializer.SerializeJson(rule.Chunking)) + ", " +
                _Driver.FormatNullableString(Serializer.SerializeJson(rule.Embedding)) + ", " +
                "'" + _Driver.FormatDateTime(rule.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(rule.LastUpdateUtc) + "'" +
                ")";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return rule;
        }

        /// <inheritdoc />
        public async Task<IngestionRule> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM ingestion_rules WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return IngestionRule.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<IngestionRule> UpdateAsync(IngestionRule rule, CancellationToken token = default)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            rule.LastUpdateUtc = DateTime.UtcNow;

            string query =
                "UPDATE ingestion_rules SET " +
                "name = '" + _Driver.Sanitize(rule.Name) + "', " +
                "description = " + _Driver.FormatNullableString(rule.Description) + ", " +
                "bucket = '" + _Driver.Sanitize(rule.Bucket) + "', " +
                "collection_name = '" + _Driver.Sanitize(rule.CollectionName) + "', " +
                "collection_id = " + _Driver.FormatNullableString(rule.CollectionId) + ", " +
                "labels_json = " + _Driver.FormatNullableString(Serializer.SerializeJson(rule.Labels)) + ", " +
                "tags_json = " + _Driver.FormatNullableString(Serializer.SerializeJson(rule.Tags)) + ", " +
                "atomization_json = " + _Driver.FormatNullableString(Serializer.SerializeJson(rule.Atomization)) + ", " +
                "summarization_json = " + _Driver.FormatNullableString(Serializer.SerializeJson(rule.Summarization)) + ", " +
                "chunking_json = " + _Driver.FormatNullableString(Serializer.SerializeJson(rule.Chunking)) + ", " +
                "embedding_json = " + _Driver.FormatNullableString(Serializer.SerializeJson(rule.Embedding)) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(rule.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(rule.Id) + "'";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return rule;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM ingestion_rules WHERE id = '" + _Driver.Sanitize(id) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT COUNT(*) AS cnt FROM ingestion_rules WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return false;
            int count = Convert.ToInt32(result.Rows[0]["cnt"]);
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<IngestionRule>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
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

            string countQuery = "SELECT COUNT(*) AS cnt FROM ingestion_rules";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM ingestion_rules " +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + offset;

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            List<IngestionRule> objects = new List<IngestionRule>();
            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    IngestionRule obj = IngestionRule.FromDataRow(row);
                    if (obj != null) objects.Add(obj);
                }
            }

            long nextOffset = offset + objects.Count;
            long remaining = totalRecords - nextOffset;
            if (remaining < 0) remaining = 0;

            sw.Stop();

            EnumerationResult<IngestionRule> enumerationResult = new EnumerationResult<IngestionRule>();
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

#pragma warning restore CS8625, CS8603, CS8600
