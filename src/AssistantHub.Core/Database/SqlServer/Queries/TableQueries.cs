namespace AssistantHub.Core.Database.SqlServer.Queries
{
    /// <summary>
    /// SQL Server table and index creation queries.
    /// </summary>
    internal static class TableQueries
    {
        #region Tables

        internal static readonly string CreateUsersTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
            CREATE TABLE users (
                id NVARCHAR(256) NOT NULL,
                email NVARCHAR(256) NOT NULL,
                password_sha256 NVARCHAR(256) NULL,
                first_name NVARCHAR(MAX) NULL,
                last_name NVARCHAR(MAX) NULL,
                is_admin INT NOT NULL DEFAULT 0,
                active INT NOT NULL DEFAULT 1,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_users PRIMARY KEY (id)
            );";

        internal static readonly string CreateCredentialsTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'credentials')
            CREATE TABLE credentials (
                id NVARCHAR(256) NOT NULL,
                user_id NVARCHAR(256) NOT NULL,
                name NVARCHAR(MAX) NULL,
                bearer_token NVARCHAR(256) NOT NULL,
                active INT NOT NULL DEFAULT 1,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_credentials PRIMARY KEY (id)
            );";

        internal static readonly string CreateAssistantsTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'assistants')
            CREATE TABLE assistants (
                id NVARCHAR(256) NOT NULL,
                user_id NVARCHAR(256) NOT NULL,
                name NVARCHAR(MAX) NOT NULL,
                description NVARCHAR(MAX) NULL,
                active INT NOT NULL DEFAULT 1,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_assistants PRIMARY KEY (id)
            );";

        internal static readonly string CreateAssistantSettingsTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'assistant_settings')
            CREATE TABLE assistant_settings (
                id NVARCHAR(256) NOT NULL,
                assistant_id NVARCHAR(256) NOT NULL,
                temperature FLOAT NOT NULL DEFAULT 0.7,
                top_p FLOAT NOT NULL DEFAULT 1.0,
                system_prompt NVARCHAR(MAX) NULL,
                max_tokens INT NOT NULL DEFAULT 4096,
                context_window INT NOT NULL DEFAULT 8192,
                model NVARCHAR(MAX) NOT NULL DEFAULT 'gemma3:4b',
                enable_rag BIT NOT NULL DEFAULT 0,
                collection_id NVARCHAR(256) NULL,
                retrieval_top_k INT NOT NULL DEFAULT 10,
                retrieval_score_threshold FLOAT NOT NULL DEFAULT 0.3,
                search_mode NVARCHAR(MAX) NULL DEFAULT 'Vector',
                text_weight FLOAT NULL DEFAULT 0.3,
                fulltext_search_type NVARCHAR(MAX) NULL DEFAULT 'TsRank',
                fulltext_language NVARCHAR(MAX) NULL DEFAULT 'english',
                fulltext_normalization INT NULL DEFAULT 32,
                fulltext_minimum_score FLOAT NULL,
                inference_endpoint_id NVARCHAR(MAX) NULL,
                embedding_endpoint_id NVARCHAR(MAX) NULL,
                title NVARCHAR(MAX) NULL,
                logo_url NVARCHAR(MAX) NULL,
                favicon_url NVARCHAR(MAX) NULL,
                streaming BIT NOT NULL DEFAULT 1,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_assistant_settings PRIMARY KEY (id)
            );";

        internal static readonly string CreateAssistantDocumentsTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'assistant_documents')
            CREATE TABLE assistant_documents (
                id NVARCHAR(256) NOT NULL,
                name NVARCHAR(MAX) NOT NULL,
                original_filename NVARCHAR(MAX) NULL,
                content_type NVARCHAR(MAX) NULL DEFAULT 'application/octet-stream',
                size_bytes BIGINT NOT NULL DEFAULT 0,
                s3_key NVARCHAR(MAX) NULL,
                status NVARCHAR(MAX) NOT NULL DEFAULT 'Pending',
                status_message NVARCHAR(MAX) NULL,
                ingestion_rule_id NVARCHAR(256) NULL,
                bucket_name NVARCHAR(MAX) NULL,
                collection_id NVARCHAR(MAX) NULL,
                labels_json NVARCHAR(MAX) NULL,
                tags_json NVARCHAR(MAX) NULL,
                chunk_record_ids NVARCHAR(MAX) NULL,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_assistant_documents PRIMARY KEY (id)
            );";

        internal static readonly string CreateAssistantFeedbackTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'assistant_feedback')
            CREATE TABLE assistant_feedback (
                id NVARCHAR(256) NOT NULL,
                assistant_id NVARCHAR(256) NOT NULL,
                user_message NVARCHAR(MAX) NULL,
                assistant_response NVARCHAR(MAX) NULL,
                rating NVARCHAR(MAX) NOT NULL DEFAULT 'ThumbsUp',
                feedback_text NVARCHAR(MAX) NULL,
                message_history NVARCHAR(MAX) NULL,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_assistant_feedback PRIMARY KEY (id)
            );";

        internal static readonly string CreateIngestionRulesTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ingestion_rules')
            CREATE TABLE ingestion_rules (
                id NVARCHAR(256) NOT NULL,
                name NVARCHAR(MAX) NOT NULL,
                description NVARCHAR(MAX) NULL,
                bucket NVARCHAR(MAX) NOT NULL,
                collection_name NVARCHAR(MAX) NOT NULL,
                collection_id NVARCHAR(MAX) NULL,
                labels_json NVARCHAR(MAX) NULL,
                tags_json NVARCHAR(MAX) NULL,
                atomization_json NVARCHAR(MAX) NULL,
                summarization_json NVARCHAR(MAX) NULL,
                chunking_json NVARCHAR(MAX) NULL,
                embedding_json NVARCHAR(MAX) NULL,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_ingestion_rules PRIMARY KEY (id)
            );";

        internal static readonly string CreateChatHistoryTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'chat_history')
            CREATE TABLE chat_history (
                id NVARCHAR(256) NOT NULL,
                thread_id NVARCHAR(256) NOT NULL,
                assistant_id NVARCHAR(256) NOT NULL,
                collection_id NVARCHAR(256) NULL,
                user_message_utc NVARCHAR(64) NOT NULL,
                user_message NVARCHAR(MAX) NULL,
                retrieval_start_utc NVARCHAR(64) NULL,
                retrieval_duration_ms FLOAT NOT NULL DEFAULT 0,
                retrieval_context NVARCHAR(MAX) NULL,
                prompt_sent_utc NVARCHAR(64) NULL,
                prompt_tokens INT NOT NULL DEFAULT 0,
                endpoint_resolution_duration_ms FLOAT NOT NULL DEFAULT 0,
                compaction_duration_ms FLOAT NOT NULL DEFAULT 0,
                inference_connection_duration_ms FLOAT NOT NULL DEFAULT 0,
                time_to_first_token_ms FLOAT NOT NULL DEFAULT 0,
                time_to_last_token_ms FLOAT NOT NULL DEFAULT 0,
                assistant_response NVARCHAR(MAX) NULL,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_chat_history PRIMARY KEY (id)
            );";

        #endregion

        #region Indices

        internal static readonly string CreateUsersEmailIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_users_email')
            CREATE INDEX idx_users_email ON users (email);";

        internal static readonly string CreateCredentialsUserIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_credentials_user_id')
            CREATE INDEX idx_credentials_user_id ON credentials (user_id);";

        internal static readonly string CreateCredentialsBearerTokenIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_credentials_bearer_token')
            CREATE INDEX idx_credentials_bearer_token ON credentials (bearer_token);";

        internal static readonly string CreateAssistantsUserIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistants_user_id')
            CREATE INDEX idx_assistants_user_id ON assistants (user_id);";

        internal static readonly string CreateAssistantSettingsAssistantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_settings_assistant_id')
            CREATE INDEX idx_assistant_settings_assistant_id ON assistant_settings (assistant_id);";

        internal static readonly string CreateAssistantFeedbackAssistantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_feedback_assistant_id')
            CREATE INDEX idx_assistant_feedback_assistant_id ON assistant_feedback (assistant_id);";

        internal static readonly string CreateIngestionRulesNameIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_ingestion_rules_name')
            CREATE INDEX idx_ingestion_rules_name ON ingestion_rules (name);";

        internal static readonly string CreateAssistantDocumentsIngestionRuleIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_documents_ingestion_rule_id')
            CREATE INDEX idx_assistant_documents_ingestion_rule_id ON assistant_documents (ingestion_rule_id);";

        internal static readonly string CreateChatHistoryAssistantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_assistant_id')
            CREATE INDEX idx_chat_history_assistant_id ON chat_history (assistant_id);";

        internal static readonly string CreateChatHistoryThreadIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_thread_id')
            CREATE INDEX idx_chat_history_thread_id ON chat_history (thread_id);";

        internal static readonly string CreateChatHistoryCreatedUtcIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_created_utc')
            CREATE INDEX idx_chat_history_created_utc ON chat_history (created_utc);";

        #endregion
    }
}
