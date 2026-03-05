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
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// SQLite eval result methods implementation.
    /// </summary>
    public class EvalResultMethods : IEvalResultMethods
    {
        #region Private-Members

        private readonly string _Header = "[EvalResultMethods] ";
        private readonly SqliteDatabaseDriver _Driver;
        private readonly DatabaseSettings _Settings;
        private readonly LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EvalResultMethods(SqliteDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<EvalResult> CreateAsync(EvalResult result, CancellationToken token = default)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (String.IsNullOrEmpty(result.Id)) result.Id = IdGenerator.NewEvalResultId();
            result.CreatedUtc = DateTime.UtcNow;

            string query =
                "INSERT INTO eval_results " +
                "(id, run_id, fact_id, question, expected_facts, llm_response, " +
                "fact_verdicts, overall_pass, duration_ms, created_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(result.Id) + "', " +
                "'" + _Driver.Sanitize(result.RunId) + "', " +
                "'" + _Driver.Sanitize(result.FactId) + "', " +
                _Driver.FormatNullableString(result.Question) + ", " +
                _Driver.FormatNullableString(result.ExpectedFacts) + ", " +
                _Driver.FormatNullableString(result.LlmResponse) + ", " +
                _Driver.FormatNullableString(result.FactVerdicts) + ", " +
                _Driver.FormatBoolean(result.OverallPass) + ", " +
                result.DurationMs + ", " +
                "'" + _Driver.FormatDateTime(result.CreatedUtc) + "'" +
                ");";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc />
        public async Task<EvalResult> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT * FROM eval_results WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return EvalResult.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<List<EvalResult>> GetByRunIdAsync(string runId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(runId)) throw new ArgumentNullException(nameof(runId));

            string query =
                "SELECT * FROM eval_results WHERE run_id = '" + _Driver.Sanitize(runId) + "' ORDER BY created_utc ASC;";

            List<EvalResult> ret = new List<EvalResult>();
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    ret.Add(EvalResult.FromDataRow(row));
                }
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "DELETE FROM eval_results WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByRunIdAsync(string runId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(runId)) throw new ArgumentNullException(nameof(runId));

            string query =
                "DELETE FROM eval_results WHERE run_id = '" + _Driver.Sanitize(runId) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
