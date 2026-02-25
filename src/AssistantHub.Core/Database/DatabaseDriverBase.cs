namespace AssistantHub.Core.Database
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;

    /// <summary>
    /// Database driver base class.
    /// </summary>
    public abstract class DatabaseDriverBase
    {
        #region Public-Members

        /// <summary>
        /// User methods.
        /// </summary>
        public IUserMethods User { get; protected set; }

        /// <summary>
        /// Credential methods.
        /// </summary>
        public ICredentialMethods Credential { get; protected set; }

        /// <summary>
        /// Assistant methods.
        /// </summary>
        public IAssistantMethods Assistant { get; protected set; }

        /// <summary>
        /// Assistant settings methods.
        /// </summary>
        public IAssistantSettingsMethods AssistantSettings { get; protected set; }

        /// <summary>
        /// Assistant document methods.
        /// </summary>
        public IAssistantDocumentMethods AssistantDocument { get; protected set; }

        /// <summary>
        /// Assistant feedback methods.
        /// </summary>
        public IAssistantFeedbackMethods AssistantFeedback { get; protected set; }

        /// <summary>
        /// Ingestion rule methods.
        /// </summary>
        public IIngestionRuleMethods IngestionRule { get; protected set; }

        /// <summary>
        /// Chat history methods.
        /// </summary>
        public IChatHistoryMethods ChatHistory { get; protected set; }

        /// <summary>
        /// Tenant methods.
        /// </summary>
        public ITenantMethods Tenant { get; protected set; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public DatabaseDriverBase()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Initialize the database (create tables and indices).
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public abstract Task InitializeAsync(CancellationToken token = default);

        /// <summary>
        /// Execute a query.
        /// </summary>
        /// <param name="query">SQL query.</param>
        /// <param name="isTransaction">Execute within a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>DataTable result.</returns>
        public abstract Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default);

        /// <summary>
        /// Execute multiple queries.
        /// </summary>
        /// <param name="queries">SQL queries.</param>
        /// <param name="isTransaction">Execute within a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>DataTable result.</returns>
        public abstract Task<DataTable> ExecuteQueriesAsync(IEnumerable<string> queries, bool isTransaction = false, CancellationToken token = default);

        #endregion

        #region Public-Virtual-Methods

        /// <summary>
        /// Sanitize a string for SQL.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>Sanitized string.</returns>
        public virtual string Sanitize(string input)
        {
            if (String.IsNullOrEmpty(input)) return input;
            return input.Replace("'", "''");
        }

        /// <summary>
        /// Format a boolean for SQL.
        /// </summary>
        /// <param name="value">Boolean value.</param>
        /// <returns>SQL boolean string.</returns>
        public virtual string FormatBoolean(bool value)
        {
            return value ? "1" : "0";
        }

        /// <summary>
        /// Format a DateTime for SQL.
        /// </summary>
        /// <param name="value">DateTime value.</param>
        /// <returns>ISO 8601 string.</returns>
        public virtual string FormatDateTime(DateTime value)
        {
            return value.ToString("o");
        }

        /// <summary>
        /// Format a nullable string for SQL.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <returns>SQL string or NULL.</returns>
        public virtual string FormatNullableString(string value)
        {
            if (value == null) return "NULL";
            return "'" + Sanitize(value) + "'";
        }

        /// <summary>
        /// Format a nullable double for SQL.
        /// </summary>
        /// <param name="value">Double value.</param>
        /// <returns>SQL value string.</returns>
        public virtual string FormatDouble(double value)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Format a nullable DateTime for SQL.
        /// </summary>
        /// <param name="value">Nullable DateTime value.</param>
        /// <returns>SQL string or NULL.</returns>
        public virtual string FormatNullableDateTime(DateTime? value)
        {
            if (value == null) return "NULL";
            return "'" + FormatDateTime(value.Value) + "'";
        }

        #endregion
    }
}
