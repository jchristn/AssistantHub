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
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// MySQL assistant methods implementation.
    /// </summary>
    public class AssistantMethods : IAssistantMethods
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
        public AssistantMethods(MysqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Assistant> CreateAsync(Assistant assistant, CancellationToken token = default)
        {
            if (assistant == null) throw new ArgumentNullException(nameof(assistant));

            assistant.CreatedUtc = DateTime.UtcNow;
            assistant.LastUpdateUtc = assistant.CreatedUtc;

            string query =
                "INSERT INTO assistants " +
                "(id, user_id, name, description, active, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(assistant.Id) + "', " +
                "'" + _Driver.Sanitize(assistant.UserId) + "', " +
                "'" + _Driver.Sanitize(assistant.Name) + "', " +
                _Driver.FormatNullableString(assistant.Description) + ", " +
                _Driver.FormatBoolean(assistant.Active) + ", " +
                "'" + _Driver.FormatDateTime(assistant.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(assistant.LastUpdateUtc) + "'" +
                ")";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return assistant;
        }

        /// <inheritdoc />
        public async Task<Assistant> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM assistants WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return Assistant.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<Assistant> UpdateAsync(Assistant assistant, CancellationToken token = default)
        {
            if (assistant == null) throw new ArgumentNullException(nameof(assistant));

            assistant.LastUpdateUtc = DateTime.UtcNow;

            string query =
                "UPDATE assistants SET " +
                "user_id = '" + _Driver.Sanitize(assistant.UserId) + "', " +
                "name = '" + _Driver.Sanitize(assistant.Name) + "', " +
                "description = " + _Driver.FormatNullableString(assistant.Description) + ", " +
                "active = " + _Driver.FormatBoolean(assistant.Active) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(assistant.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(assistant.Id) + "'";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return assistant;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM assistants WHERE id = '" + _Driver.Sanitize(id) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT COUNT(*) AS cnt FROM assistants WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return false;
            int count = Convert.ToInt32(result.Rows[0]["cnt"]);
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Assistant>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
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

            string countQuery = "SELECT COUNT(*) AS cnt FROM assistants";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM assistants " +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + offset;

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            List<Assistant> objects = new List<Assistant>();
            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    Assistant obj = Assistant.FromDataRow(row);
                    if (obj != null) objects.Add(obj);
                }
            }

            long nextOffset = offset + objects.Count;
            long remaining = totalRecords - nextOffset;
            if (remaining < 0) remaining = 0;

            sw.Stop();

            EnumerationResult<Assistant> enumerationResult = new EnumerationResult<Assistant>();
            enumerationResult.MaxResults = query.MaxResults;
            enumerationResult.TotalRecords = totalRecords;
            enumerationResult.RecordsRemaining = remaining;
            enumerationResult.Objects = objects;
            enumerationResult.ContinuationToken = remaining > 0 ? nextOffset.ToString() : null;
            enumerationResult.EndOfResults = remaining <= 0;
            enumerationResult.TotalMs = sw.Elapsed.TotalMilliseconds;
            return enumerationResult;
        }

        /// <inheritdoc />
        public async Task<long> GetCountAsync(CancellationToken token = default)
        {
            string query = "SELECT COUNT(*) AS cnt FROM assistants";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return 0;
            return Convert.ToInt64(result.Rows[0]["cnt"]);
        }

        #endregion
    }
}
