#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
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
    /// SQLite eval fact methods implementation.
    /// </summary>
    public class EvalFactMethods : IEvalFactMethods
    {
        #region Private-Members

        private readonly string _Header = "[EvalFactMethods] ";
        private readonly SqliteDatabaseDriver _Driver;
        private readonly DatabaseSettings _Settings;
        private readonly LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EvalFactMethods(SqliteDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<EvalFact> CreateAsync(EvalFact fact, CancellationToken token = default)
        {
            if (fact == null) throw new ArgumentNullException(nameof(fact));

            if (String.IsNullOrEmpty(fact.Id)) fact.Id = IdGenerator.NewEvalFactId();
            fact.CreatedUtc = DateTime.UtcNow;
            fact.LastUpdateUtc = fact.CreatedUtc;

            string query =
                "INSERT INTO eval_facts " +
                "(id, tenant_id, assistant_id, category, question, expected_facts, " +
                "created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(fact.Id) + "', " +
                "'" + _Driver.Sanitize(fact.TenantId) + "', " +
                "'" + _Driver.Sanitize(fact.AssistantId) + "', " +
                _Driver.FormatNullableString(fact.Category) + ", " +
                _Driver.FormatNullableString(fact.Question) + ", " +
                _Driver.FormatNullableString(fact.ExpectedFacts) + ", " +
                "'" + _Driver.FormatDateTime(fact.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(fact.LastUpdateUtc) + "'" +
                ");";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return fact;
        }

        /// <inheritdoc />
        public async Task<EvalFact> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT * FROM eval_facts WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return EvalFact.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<EvalFact> UpdateAsync(EvalFact fact, CancellationToken token = default)
        {
            if (fact == null) throw new ArgumentNullException(nameof(fact));

            fact.LastUpdateUtc = DateTime.UtcNow;

            string query =
                "UPDATE eval_facts SET " +
                "category = " + _Driver.FormatNullableString(fact.Category) + ", " +
                "question = " + _Driver.FormatNullableString(fact.Question) + ", " +
                "expected_facts = " + _Driver.FormatNullableString(fact.ExpectedFacts) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(fact.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(fact.Id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return fact;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "DELETE FROM eval_facts WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<EvalFact>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (query == null) throw new ArgumentNullException(nameof(query));

            EnumerationResult<EvalFact> ret = new EnumerationResult<EvalFact>();
            ret.MaxResults = query.MaxResults;

            int skip = 0;
            if (!String.IsNullOrEmpty(query.ContinuationToken))
                Int32.TryParse(query.ContinuationToken, out skip);

            string orderBy;
            switch (query.Ordering)
            {
                case EnumerationOrderEnum.CreatedAscending:
                    orderBy = "ORDER BY created_utc ASC";
                    break;
                case EnumerationOrderEnum.CreatedDescending:
                default:
                    orderBy = "ORDER BY created_utc DESC";
                    break;
            }

            List<string> conditions = new List<string>();
            conditions.Add("tenant_id = '" + _Driver.Sanitize(tenantId) + "'");
            if (!String.IsNullOrEmpty(query.AssistantIdFilter))
                conditions.Add("assistant_id = '" + _Driver.Sanitize(query.AssistantIdFilter) + "'");

            string whereClause = "WHERE " + String.Join(" AND ", conditions) + " ";

            string selectQuery =
                "SELECT * FROM eval_facts " +
                whereClause +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + skip + ";";

            string countQuery =
                "SELECT COUNT(*) AS cnt FROM eval_facts " + whereClause + ";";

            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            if (countResult != null && countResult.Rows.Count > 0)
                ret.TotalRecords = DataTableHelper.GetLongValue(countResult.Rows[0], "cnt");

            DataTable selectResult = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);
            if (selectResult != null && selectResult.Rows.Count > 0)
            {
                foreach (DataRow row in selectResult.Rows)
                {
                    ret.Objects.Add(EvalFact.FromDataRow(row));
                }
            }

            long nextOffset = skip + ret.Objects.Count;
            ret.RecordsRemaining = ret.TotalRecords - nextOffset;
            if (ret.RecordsRemaining < 0) ret.RecordsRemaining = 0;
            ret.EndOfResults = (nextOffset >= ret.TotalRecords);
            ret.ContinuationToken = ret.EndOfResults ? null : nextOffset.ToString();

            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(assistantId)) throw new ArgumentNullException(nameof(assistantId));

            string query =
                "DELETE FROM eval_facts WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
