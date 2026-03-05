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
    /// SQLite eval run methods implementation.
    /// </summary>
    public class EvalRunMethods : IEvalRunMethods
    {
        #region Private-Members

        private readonly string _Header = "[EvalRunMethods] ";
        private readonly SqliteDatabaseDriver _Driver;
        private readonly DatabaseSettings _Settings;
        private readonly LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EvalRunMethods(SqliteDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<EvalRun> CreateAsync(EvalRun run, CancellationToken token = default)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));

            if (String.IsNullOrEmpty(run.Id)) run.Id = IdGenerator.NewEvalRunId();
            run.CreatedUtc = DateTime.UtcNow;

            string query =
                "INSERT INTO eval_runs " +
                "(id, tenant_id, assistant_id, status, total_facts, facts_evaluated, " +
                "facts_passed, facts_failed, pass_rate, judge_prompt, started_utc, completed_utc, created_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(run.Id) + "', " +
                "'" + _Driver.Sanitize(run.TenantId) + "', " +
                "'" + _Driver.Sanitize(run.AssistantId) + "', " +
                "'" + _Driver.Sanitize(run.Status.ToString()) + "', " +
                run.TotalFacts + ", " +
                run.FactsEvaluated + ", " +
                run.FactsPassed + ", " +
                run.FactsFailed + ", " +
                _Driver.FormatDouble(run.PassRate) + ", " +
                _Driver.FormatNullableString(run.JudgePrompt) + ", " +
                _Driver.FormatNullableDateTime(run.StartedUtc) + ", " +
                _Driver.FormatNullableDateTime(run.CompletedUtc) + ", " +
                "'" + _Driver.FormatDateTime(run.CreatedUtc) + "'" +
                ");";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return run;
        }

        /// <inheritdoc />
        public async Task<EvalRun> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT * FROM eval_runs WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return EvalRun.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<EvalRun> UpdateAsync(EvalRun run, CancellationToken token = default)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));

            string query =
                "UPDATE eval_runs SET " +
                "status = '" + _Driver.Sanitize(run.Status.ToString()) + "', " +
                "total_facts = " + run.TotalFacts + ", " +
                "facts_evaluated = " + run.FactsEvaluated + ", " +
                "facts_passed = " + run.FactsPassed + ", " +
                "facts_failed = " + run.FactsFailed + ", " +
                "pass_rate = " + _Driver.FormatDouble(run.PassRate) + ", " +
                "started_utc = " + _Driver.FormatNullableDateTime(run.StartedUtc) + ", " +
                "completed_utc = " + _Driver.FormatNullableDateTime(run.CompletedUtc) + " " +
                "WHERE id = '" + _Driver.Sanitize(run.Id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return run;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            // Delete associated results first
            string deleteResults =
                "DELETE FROM eval_results WHERE run_id = '" + _Driver.Sanitize(id) + "';";
            string deleteRun =
                "DELETE FROM eval_runs WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(deleteResults + " " + deleteRun, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<EvalRun>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (query == null) throw new ArgumentNullException(nameof(query));

            EnumerationResult<EvalRun> ret = new EnumerationResult<EvalRun>();
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
                "SELECT * FROM eval_runs " +
                whereClause +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + skip + ";";

            string countQuery =
                "SELECT COUNT(*) AS cnt FROM eval_runs " + whereClause + ";";

            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            if (countResult != null && countResult.Rows.Count > 0)
                ret.TotalRecords = DataTableHelper.GetLongValue(countResult.Rows[0], "cnt");

            DataTable selectResult = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);
            if (selectResult != null && selectResult.Rows.Count > 0)
            {
                foreach (DataRow row in selectResult.Rows)
                {
                    ret.Objects.Add(EvalRun.FromDataRow(row));
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

            // Delete results for all runs of this assistant first
            string deleteResults =
                "DELETE FROM eval_results WHERE run_id IN (SELECT id FROM eval_runs WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "');";
            string deleteRuns =
                "DELETE FROM eval_runs WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "';";

            await _Driver.ExecuteQueryAsync(deleteResults + " " + deleteRuns, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
