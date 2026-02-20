namespace AssistantHub.Core.Database.SqlServer.Implementations
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using SyslogLogging;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;

    /// <summary>
    /// SQL Server assistant settings methods.
    /// </summary>
    public class AssistantSettingsMethods : IAssistantSettingsMethods
    {
        #region Private-Members

        private SqlServerDatabaseDriver _Driver;
        private DatabaseSettings _Settings;
        private LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="driver">SQL Server database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public AssistantSettingsMethods(SqlServerDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<AssistantSettings> CreateAsync(AssistantSettings settings, CancellationToken token = default)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            settings.CreatedUtc = DateTime.UtcNow;
            settings.LastUpdateUtc = settings.CreatedUtc;

            string query =
                "INSERT INTO assistant_settings " +
                "(id, assistant_id, temperature, top_p, system_prompt, max_tokens, context_window, " +
                "model, enable_rag, collection_id, retrieval_top_k, retrieval_score_threshold, " +
                "inference_endpoint_id, embedding_endpoint_id, title, logo_url, favicon_url, streaming, created_utc, last_update_utc) " +
                "VALUES " +
                "('" + _Driver.Sanitize(settings.Id) + "', " +
                "'" + _Driver.Sanitize(settings.AssistantId) + "', " +
                _Driver.FormatDouble(settings.Temperature) + ", " +
                _Driver.FormatDouble(settings.TopP) + ", " +
                _Driver.FormatNullableString(settings.SystemPrompt) + ", " +
                settings.MaxTokens + ", " +
                settings.ContextWindow + ", " +
                _Driver.FormatNullableString(settings.Model) + ", " +
                (settings.EnableRag ? 1 : 0) + ", " +
                _Driver.FormatNullableString(settings.CollectionId) + ", " +
                settings.RetrievalTopK + ", " +
                _Driver.FormatDouble(settings.RetrievalScoreThreshold) + ", " +
                _Driver.FormatNullableString(settings.InferenceEndpointId) + ", " +
                _Driver.FormatNullableString(settings.EmbeddingEndpointId) + ", " +
                _Driver.FormatNullableString(settings.Title) + ", " +
                _Driver.FormatNullableString(settings.LogoUrl) + ", " +
                _Driver.FormatNullableString(settings.FaviconUrl) + ", " +
                (settings.Streaming ? 1 : 0) + ", " +
                "'" + _Driver.FormatDateTime(settings.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(settings.LastUpdateUtc) + "');";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return settings;
        }

        /// <inheritdoc />
        public async Task<AssistantSettings> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM assistant_settings WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return AssistantSettings.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<AssistantSettings> ReadByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(assistantId)) throw new ArgumentNullException(nameof(assistantId));

            string query = "SELECT * FROM assistant_settings WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return AssistantSettings.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<AssistantSettings> UpdateAsync(AssistantSettings settings, CancellationToken token = default)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            settings.LastUpdateUtc = DateTime.UtcNow;

            string query =
                "UPDATE assistant_settings SET " +
                "assistant_id = '" + _Driver.Sanitize(settings.AssistantId) + "', " +
                "temperature = " + _Driver.FormatDouble(settings.Temperature) + ", " +
                "top_p = " + _Driver.FormatDouble(settings.TopP) + ", " +
                "system_prompt = " + _Driver.FormatNullableString(settings.SystemPrompt) + ", " +
                "max_tokens = " + settings.MaxTokens + ", " +
                "context_window = " + settings.ContextWindow + ", " +
                "model = " + _Driver.FormatNullableString(settings.Model) + ", " +
                "enable_rag = " + (settings.EnableRag ? 1 : 0) + ", " +
                "collection_id = " + _Driver.FormatNullableString(settings.CollectionId) + ", " +
                "retrieval_top_k = " + settings.RetrievalTopK + ", " +
                "retrieval_score_threshold = " + _Driver.FormatDouble(settings.RetrievalScoreThreshold) + ", " +
                "inference_endpoint_id = " + _Driver.FormatNullableString(settings.InferenceEndpointId) + ", " +
                "embedding_endpoint_id = " + _Driver.FormatNullableString(settings.EmbeddingEndpointId) + ", " +
                "title = " + _Driver.FormatNullableString(settings.Title) + ", " +
                "logo_url = " + _Driver.FormatNullableString(settings.LogoUrl) + ", " +
                "favicon_url = " + _Driver.FormatNullableString(settings.FaviconUrl) + ", " +
                "streaming = " + (settings.Streaming ? 1 : 0) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(settings.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(settings.Id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return settings;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM assistant_settings WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(assistantId)) throw new ArgumentNullException(nameof(assistantId));

            string query = "DELETE FROM assistant_settings WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}
