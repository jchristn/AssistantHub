namespace AssistantHub.Core.Database.Postgresql.Implementations
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// PostgreSQL assistant settings methods implementation.
    /// </summary>
    public class AssistantSettingsMethods : IAssistantSettingsMethods
    {
        #region Private-Members

        private PostgresqlDatabaseDriver _Driver;
        private DatabaseSettings _Settings;
        private LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="driver">PostgreSQL database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public AssistantSettingsMethods(PostgresqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<AssistantSettings> CreateAsync(AssistantSettings assistantSettings, CancellationToken token = default)
        {
            if (assistantSettings == null) throw new ArgumentNullException(nameof(assistantSettings));

            assistantSettings.CreatedUtc = DateTime.UtcNow;
            assistantSettings.LastUpdateUtc = assistantSettings.CreatedUtc;

            string query =
                "INSERT INTO assistant_settings " +
                "(id, assistant_id, temperature, top_p, system_prompt, max_tokens, context_window, " +
                "model, enable_rag, collection_id, retrieval_top_k, retrieval_score_threshold, " +
                "search_mode, text_weight, fulltext_search_type, fulltext_language, fulltext_normalization, fulltext_minimum_score, " +
                "inference_endpoint_id, embedding_endpoint_id, title, logo_url, favicon_url, streaming, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(assistantSettings.Id) + "', " +
                "'" + _Driver.Sanitize(assistantSettings.AssistantId) + "', " +
                _Driver.FormatDouble(assistantSettings.Temperature) + ", " +
                _Driver.FormatDouble(assistantSettings.TopP) + ", " +
                _Driver.FormatNullableString(assistantSettings.SystemPrompt) + ", " +
                assistantSettings.MaxTokens + ", " +
                assistantSettings.ContextWindow + ", " +
                _Driver.FormatNullableString(assistantSettings.Model) + ", " +
                (assistantSettings.EnableRag ? 1 : 0) + ", " +
                _Driver.FormatNullableString(assistantSettings.CollectionId) + ", " +
                assistantSettings.RetrievalTopK + ", " +
                _Driver.FormatDouble(assistantSettings.RetrievalScoreThreshold) + ", " +
                _Driver.FormatNullableString(assistantSettings.SearchMode) + ", " +
                _Driver.FormatDouble(assistantSettings.TextWeight) + ", " +
                _Driver.FormatNullableString(assistantSettings.FullTextSearchType) + ", " +
                _Driver.FormatNullableString(assistantSettings.FullTextLanguage) + ", " +
                assistantSettings.FullTextNormalization + ", " +
                (assistantSettings.FullTextMinimumScore.HasValue ? _Driver.FormatDouble(assistantSettings.FullTextMinimumScore.Value) : "NULL") + ", " +
                _Driver.FormatNullableString(assistantSettings.InferenceEndpointId) + ", " +
                _Driver.FormatNullableString(assistantSettings.EmbeddingEndpointId) + ", " +
                _Driver.FormatNullableString(assistantSettings.Title) + ", " +
                _Driver.FormatNullableString(assistantSettings.LogoUrl) + ", " +
                _Driver.FormatNullableString(assistantSettings.FaviconUrl) + ", " +
                (assistantSettings.Streaming ? 1 : 0) + ", " +
                "'" + _Driver.FormatDateTime(assistantSettings.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(assistantSettings.LastUpdateUtc) + "'" +
                ")";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return assistantSettings;
        }

        /// <inheritdoc />
        public async Task<AssistantSettings> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM assistant_settings WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return AssistantSettings.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<AssistantSettings> ReadByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(assistantId)) throw new ArgumentNullException(nameof(assistantId));

            string query = "SELECT * FROM assistant_settings WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return AssistantSettings.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<AssistantSettings> UpdateAsync(AssistantSettings assistantSettings, CancellationToken token = default)
        {
            if (assistantSettings == null) throw new ArgumentNullException(nameof(assistantSettings));

            assistantSettings.LastUpdateUtc = DateTime.UtcNow;

            string query =
                "UPDATE assistant_settings SET " +
                "assistant_id = '" + _Driver.Sanitize(assistantSettings.AssistantId) + "', " +
                "temperature = " + _Driver.FormatDouble(assistantSettings.Temperature) + ", " +
                "top_p = " + _Driver.FormatDouble(assistantSettings.TopP) + ", " +
                "system_prompt = " + _Driver.FormatNullableString(assistantSettings.SystemPrompt) + ", " +
                "max_tokens = " + assistantSettings.MaxTokens + ", " +
                "context_window = " + assistantSettings.ContextWindow + ", " +
                "model = " + _Driver.FormatNullableString(assistantSettings.Model) + ", " +
                "enable_rag = " + (assistantSettings.EnableRag ? 1 : 0) + ", " +
                "collection_id = " + _Driver.FormatNullableString(assistantSettings.CollectionId) + ", " +
                "retrieval_top_k = " + assistantSettings.RetrievalTopK + ", " +
                "retrieval_score_threshold = " + _Driver.FormatDouble(assistantSettings.RetrievalScoreThreshold) + ", " +
                "search_mode = " + _Driver.FormatNullableString(assistantSettings.SearchMode) + ", " +
                "text_weight = " + _Driver.FormatDouble(assistantSettings.TextWeight) + ", " +
                "fulltext_search_type = " + _Driver.FormatNullableString(assistantSettings.FullTextSearchType) + ", " +
                "fulltext_language = " + _Driver.FormatNullableString(assistantSettings.FullTextLanguage) + ", " +
                "fulltext_normalization = " + assistantSettings.FullTextNormalization + ", " +
                "fulltext_minimum_score = " + (assistantSettings.FullTextMinimumScore.HasValue ? _Driver.FormatDouble(assistantSettings.FullTextMinimumScore.Value) : "NULL") + ", " +
                "inference_endpoint_id = " + _Driver.FormatNullableString(assistantSettings.InferenceEndpointId) + ", " +
                "embedding_endpoint_id = " + _Driver.FormatNullableString(assistantSettings.EmbeddingEndpointId) + ", " +
                "title = " + _Driver.FormatNullableString(assistantSettings.Title) + ", " +
                "logo_url = " + _Driver.FormatNullableString(assistantSettings.LogoUrl) + ", " +
                "favicon_url = " + _Driver.FormatNullableString(assistantSettings.FaviconUrl) + ", " +
                "streaming = " + (assistantSettings.Streaming ? 1 : 0) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(assistantSettings.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(assistantSettings.Id) + "'";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return assistantSettings;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM assistant_settings WHERE id = '" + _Driver.Sanitize(id) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(assistantId)) throw new ArgumentNullException(nameof(assistantId));

            string query = "DELETE FROM assistant_settings WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}
