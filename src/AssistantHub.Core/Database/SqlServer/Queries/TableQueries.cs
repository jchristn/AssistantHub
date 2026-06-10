namespace AssistantHub.Core.Database.SqlServer.Queries
{
    /// <summary>
    /// SQL Server table and index creation queries.
    /// </summary>
    internal static class TableQueries
    {
        #region Tables

        internal static readonly string CreateTenantsTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tenants')
            CREATE TABLE tenants (
                id NVARCHAR(256) NOT NULL,
                name NVARCHAR(256) NOT NULL,
                active INT NOT NULL DEFAULT 1,
                is_protected INT NOT NULL DEFAULT 0,
                labels_json NVARCHAR(MAX) NULL,
                tags_json NVARCHAR(MAX) NULL,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_tenants PRIMARY KEY (id)
            );";

        internal static readonly string CreateUsersTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
            CREATE TABLE users (
                id NVARCHAR(256) NOT NULL,
                tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default',
                email NVARCHAR(256) NOT NULL,
                password_sha256 NVARCHAR(256) NULL,
                first_name NVARCHAR(MAX) NULL,
                last_name NVARCHAR(MAX) NULL,
                is_admin INT NOT NULL DEFAULT 0,
                is_tenant_admin INT NOT NULL DEFAULT 0,
                active INT NOT NULL DEFAULT 1,
                is_protected INT NOT NULL DEFAULT 0,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_users PRIMARY KEY (id)
            );";

        internal static readonly string CreateCredentialsTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'credentials')
            CREATE TABLE credentials (
                id NVARCHAR(256) NOT NULL,
                tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default',
                user_id NVARCHAR(256) NOT NULL,
                name NVARCHAR(MAX) NULL,
                bearer_token NVARCHAR(256) NOT NULL,
                active INT NOT NULL DEFAULT 1,
                is_protected INT NOT NULL DEFAULT 0,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_credentials PRIMARY KEY (id)
            );";

        internal static readonly string CreateAssistantsTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'assistants')
            CREATE TABLE assistants (
                id NVARCHAR(256) NOT NULL,
                tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default',
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
                enable_retrieval_gate BIT NOT NULL DEFAULT 0,
                enable_query_rewrite BIT NOT NULL DEFAULT 0,
                query_rewrite_prompt NVARCHAR(MAX) NULL,
                enable_reranking BIT NOT NULL DEFAULT 0,
                reranker_top_k INT NOT NULL DEFAULT 5,
                reranker_score_threshold FLOAT NOT NULL DEFAULT 3.0,
                rerank_prompt NVARCHAR(MAX) NULL,
                enable_citations BIT NOT NULL DEFAULT 0,
                citation_link_mode NVARCHAR(32) NOT NULL DEFAULT 'None',
                enable_document_attachments BIT NOT NULL DEFAULT 0,
                document_attachment_max_count INT NOT NULL DEFAULT 10,
                expose_document_source_urls BIT NOT NULL DEFAULT 0,
                collection_id NVARCHAR(256) NULL,
                retrieval_top_k INT NOT NULL DEFAULT 10,
                retrieval_score_threshold FLOAT NOT NULL DEFAULT 0.3,
                search_mode NVARCHAR(MAX) NULL DEFAULT 'Vector',
                text_weight FLOAT NULL DEFAULT 0.3,
                fulltext_search_type NVARCHAR(MAX) NULL DEFAULT 'TsRank',
                fulltext_language NVARCHAR(MAX) NULL DEFAULT 'english',
                fulltext_normalization INT NULL DEFAULT 32,
                fulltext_minimum_score FLOAT NULL,
                retrieval_include_neighbors INT NOT NULL DEFAULT 0,
                inference_endpoint_id NVARCHAR(MAX) NULL,
                retrieval_gate_inference_endpoint_id NVARCHAR(MAX) NULL,
                query_rewrite_inference_endpoint_id NVARCHAR(MAX) NULL,
                rerank_inference_endpoint_id NVARCHAR(MAX) NULL,
                embedding_endpoint_id NVARCHAR(MAX) NULL,
                load_models_on_chat_open BIT NOT NULL DEFAULT 0,
                title NVARCHAR(MAX) NULL,
                logo_url NVARCHAR(MAX) NULL,
                favicon_url NVARCHAR(MAX) NULL,
                retrieval_label_filter NVARCHAR(MAX) NULL,
                retrieval_tag_filter NVARCHAR(MAX) NULL,
                streaming BIT NOT NULL DEFAULT 1,
                enable_slack BIT NOT NULL DEFAULT 0,
                slack_app_token NVARCHAR(MAX) NULL,
                slack_bot_token NVARCHAR(MAX) NULL,
                slack_channel_id NVARCHAR(MAX) NULL,
                slack_message_prefix NVARCHAR(MAX) NULL,
                tool_policy_json NVARCHAR(MAX) NULL,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_assistant_settings PRIMARY KEY (id)
            );";

        internal static readonly string AddAssistantSettingsRetrievalGateInferenceEndpointIdColumn =
            @"IF COL_LENGTH('assistant_settings', 'retrieval_gate_inference_endpoint_id') IS NULL
            ALTER TABLE assistant_settings ADD retrieval_gate_inference_endpoint_id NVARCHAR(MAX) NULL;";

        internal static readonly string AddAssistantSettingsQueryRewriteInferenceEndpointIdColumn =
            @"IF COL_LENGTH('assistant_settings', 'query_rewrite_inference_endpoint_id') IS NULL
            ALTER TABLE assistant_settings ADD query_rewrite_inference_endpoint_id NVARCHAR(MAX) NULL;";

        internal static readonly string AddAssistantSettingsRerankInferenceEndpointIdColumn =
            @"IF COL_LENGTH('assistant_settings', 'rerank_inference_endpoint_id') IS NULL
            ALTER TABLE assistant_settings ADD rerank_inference_endpoint_id NVARCHAR(MAX) NULL;";

        internal static readonly string AddAssistantSettingsLoadModelsOnChatOpenColumn =
            @"IF COL_LENGTH('assistant_settings', 'load_models_on_chat_open') IS NULL
            ALTER TABLE assistant_settings ADD load_models_on_chat_open BIT NOT NULL DEFAULT 0;";

        internal static readonly string AddAssistantSettingsToolPolicyJsonColumn =
            @"IF COL_LENGTH('assistant_settings', 'tool_policy_json') IS NULL
            ALTER TABLE assistant_settings ADD tool_policy_json NVARCHAR(MAX) NULL;";

        internal static readonly string AddAssistantSettingsEnableDocumentAttachmentsColumn =
            @"IF COL_LENGTH('assistant_settings', 'enable_document_attachments') IS NULL
            ALTER TABLE assistant_settings ADD enable_document_attachments BIT NOT NULL DEFAULT 0;";

        internal static readonly string AddAssistantSettingsDocumentAttachmentMaxCountColumn =
            @"IF COL_LENGTH('assistant_settings', 'document_attachment_max_count') IS NULL
            ALTER TABLE assistant_settings ADD document_attachment_max_count INT NOT NULL DEFAULT 10;";

        internal static readonly string AddAssistantSettingsExposeDocumentSourceUrlsColumn =
            @"IF COL_LENGTH('assistant_settings', 'expose_document_source_urls') IS NULL
            ALTER TABLE assistant_settings ADD expose_document_source_urls BIT NOT NULL DEFAULT 0;";

        internal static readonly string CreateAssistantDocumentsTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'assistant_documents')
            CREATE TABLE assistant_documents (
                id NVARCHAR(256) NOT NULL,
                tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default',
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
                verbex_tenant_id NVARCHAR(MAX) NULL,
                verbex_index_id NVARCHAR(MAX) NULL,
                verbex_record_id NVARCHAR(MAX) NULL,
                labels_json NVARCHAR(MAX) NULL,
                tags_json NVARCHAR(MAX) NULL,
                chunk_record_ids NVARCHAR(MAX) NULL,
                crawl_plan_id NVARCHAR(256) NULL,
                crawl_operation_id NVARCHAR(256) NULL,
                source_url NVARCHAR(MAX) NULL,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_assistant_documents PRIMARY KEY (id)
            );";

        internal static readonly string CreateAssistantFeedbackTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'assistant_feedback')
            CREATE TABLE assistant_feedback (
                id NVARCHAR(256) NOT NULL,
                tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default',
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
                tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default',
                name NVARCHAR(256) NOT NULL,
                description NVARCHAR(MAX) NULL,
                bucket NVARCHAR(MAX) NOT NULL,
                collection_name NVARCHAR(MAX) NOT NULL,
                collection_id NVARCHAR(MAX) NULL,
                verbex_index_id NVARCHAR(MAX) NULL,
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

        internal static readonly string AddAssistantDocumentsVerbexTenantIdColumn =
            @"IF COL_LENGTH('assistant_documents', 'verbex_tenant_id') IS NULL
            ALTER TABLE assistant_documents ADD verbex_tenant_id NVARCHAR(MAX) NULL;";

        internal static readonly string AddAssistantDocumentsVerbexIndexIdColumn =
            @"IF COL_LENGTH('assistant_documents', 'verbex_index_id') IS NULL
            ALTER TABLE assistant_documents ADD verbex_index_id NVARCHAR(MAX) NULL;";

        internal static readonly string AddAssistantDocumentsVerbexRecordIdColumn =
            @"IF COL_LENGTH('assistant_documents', 'verbex_record_id') IS NULL
            ALTER TABLE assistant_documents ADD verbex_record_id NVARCHAR(MAX) NULL;";

        internal static readonly string AddIngestionRulesVerbexIndexIdColumn =
            @"IF COL_LENGTH('ingestion_rules', 'verbex_index_id') IS NULL
            ALTER TABLE ingestion_rules ADD verbex_index_id NVARCHAR(MAX) NULL;";

        internal static readonly string CreateCrawlPlansTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'crawl_plans')
            CREATE TABLE crawl_plans (
                id NVARCHAR(256) NOT NULL,
                tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default',
                name NVARCHAR(256) NOT NULL,
                repository_type NVARCHAR(256) NOT NULL DEFAULT 'Web',
                ingestion_settings_json NVARCHAR(MAX) NULL,
                repository_settings_json NVARCHAR(MAX) NULL,
                schedule_json NVARCHAR(MAX) NULL,
                filter_json NVARCHAR(MAX) NULL,
                process_additions BIT NOT NULL DEFAULT 1,
                process_updates BIT NOT NULL DEFAULT 1,
                process_deletions BIT NOT NULL DEFAULT 0,
                max_drain_tasks INT NOT NULL DEFAULT 8,
                retention_days INT NOT NULL DEFAULT 7,
                state NVARCHAR(256) NOT NULL DEFAULT 'Stopped',
                last_crawl_start_utc NVARCHAR(64) NULL,
                last_crawl_finish_utc NVARCHAR(64) NULL,
                last_crawl_success BIT NULL,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_crawl_plans PRIMARY KEY (id)
            );";

        internal static readonly string CreateCrawlOperationsTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'crawl_operations')
            CREATE TABLE crawl_operations (
                id NVARCHAR(256) NOT NULL,
                tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default',
                crawl_plan_id NVARCHAR(256) NOT NULL,
                state NVARCHAR(256) NOT NULL DEFAULT 'NotStarted',
                status_message NVARCHAR(MAX) NULL,
                objects_enumerated BIGINT NOT NULL DEFAULT 0,
                bytes_enumerated BIGINT NOT NULL DEFAULT 0,
                objects_added BIGINT NOT NULL DEFAULT 0,
                bytes_added BIGINT NOT NULL DEFAULT 0,
                objects_updated BIGINT NOT NULL DEFAULT 0,
                bytes_updated BIGINT NOT NULL DEFAULT 0,
                objects_deleted BIGINT NOT NULL DEFAULT 0,
                bytes_deleted BIGINT NOT NULL DEFAULT 0,
                objects_success BIGINT NOT NULL DEFAULT 0,
                bytes_success BIGINT NOT NULL DEFAULT 0,
                objects_failed BIGINT NOT NULL DEFAULT 0,
                bytes_failed BIGINT NOT NULL DEFAULT 0,
                enumeration_file NVARCHAR(MAX) NULL,
                start_utc NVARCHAR(64) NULL,
                start_enumeration_utc NVARCHAR(64) NULL,
                finish_enumeration_utc NVARCHAR(64) NULL,
                start_retrieval_utc NVARCHAR(64) NULL,
                finish_retrieval_utc NVARCHAR(64) NULL,
                finish_utc NVARCHAR(64) NULL,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_crawl_operations PRIMARY KEY (id)
            );";

        internal static readonly string CreateChatHistoryTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'chat_history')
            CREATE TABLE chat_history (
                id NVARCHAR(256) NOT NULL,
                trace_id NVARCHAR(256) NULL,
                request_history_id NVARCHAR(256) NULL,
                performance_schema_version INT NOT NULL DEFAULT 1,
                performance_json NVARCHAR(MAX) NULL,
                tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default',
                thread_id NVARCHAR(256) NOT NULL,
                assistant_id NVARCHAR(256) NOT NULL,
                collection_id NVARCHAR(256) NULL,
                user_message_utc NVARCHAR(64) NOT NULL,
                user_message NVARCHAR(MAX) NULL,
                retrieval_start_utc NVARCHAR(64) NULL,
                retrieval_duration_ms FLOAT NOT NULL DEFAULT 0,
                retrieval_gate_decision NVARCHAR(MAX) NULL,
                retrieval_gate_duration_ms FLOAT NOT NULL DEFAULT 0,
                query_rewrite_result NVARCHAR(MAX) NULL,
                query_rewrite_duration_ms FLOAT NOT NULL DEFAULT 0,
                rerank_duration_ms FLOAT NOT NULL DEFAULT 0,
                rerank_input_count INT NOT NULL DEFAULT 0,
                rerank_output_count INT NOT NULL DEFAULT 0,
                retrieval_context NVARCHAR(MAX) NULL,
                prompt_sent_utc NVARCHAR(64) NULL,
                prompt_tokens INT NOT NULL DEFAULT 0,
                endpoint_resolution_duration_ms FLOAT NOT NULL DEFAULT 0,
                compaction_duration_ms FLOAT NOT NULL DEFAULT 0,
                inference_connection_duration_ms FLOAT NOT NULL DEFAULT 0,
                time_to_first_token_ms FLOAT NOT NULL DEFAULT 0,
                time_to_last_token_ms FLOAT NOT NULL DEFAULT 0,
                completion_tokens INT NOT NULL DEFAULT 0,
                tokens_per_second_overall FLOAT NOT NULL DEFAULT 0,
                tokens_per_second_generation FLOAT NOT NULL DEFAULT 0,
                metadata_filter NVARCHAR(MAX) NULL,
                attached_document_ids_json NVARCHAR(MAX) NULL,
                attached_documents_json NVARCHAR(MAX) NULL,
                origin NVARCHAR(64) NULL,
                assistant_response NVARCHAR(MAX) NULL,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_chat_history PRIMARY KEY (id)
            );";

        internal static readonly string AddChatHistoryTraceIdColumn =
            @"IF COL_LENGTH('chat_history', 'trace_id') IS NULL
            ALTER TABLE chat_history ADD trace_id NVARCHAR(256) NULL;";

        internal static readonly string AddChatHistoryRequestHistoryIdColumn =
            @"IF COL_LENGTH('chat_history', 'request_history_id') IS NULL
            ALTER TABLE chat_history ADD request_history_id NVARCHAR(256) NULL;";

        internal static readonly string AddChatHistoryPerformanceSchemaVersionColumn =
            @"IF COL_LENGTH('chat_history', 'performance_schema_version') IS NULL
            ALTER TABLE chat_history ADD performance_schema_version INT NOT NULL DEFAULT 1;";

        internal static readonly string AddChatHistoryPerformanceJsonColumn =
            @"IF COL_LENGTH('chat_history', 'performance_json') IS NULL
            ALTER TABLE chat_history ADD performance_json NVARCHAR(MAX) NULL;";

        internal static readonly string AddChatHistoryAttachedDocumentIdsJsonColumn =
            @"IF COL_LENGTH('chat_history', 'attached_document_ids_json') IS NULL
            ALTER TABLE chat_history ADD attached_document_ids_json NVARCHAR(MAX) NULL;";

        internal static readonly string AddChatHistoryAttachedDocumentsJsonColumn =
            @"IF COL_LENGTH('chat_history', 'attached_documents_json') IS NULL
            ALTER TABLE chat_history ADD attached_documents_json NVARCHAR(MAX) NULL;";

        internal static readonly string CreateRequestHistoryTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'request_history')
            CREATE TABLE request_history (
                id NVARCHAR(256) NOT NULL,
                trace_id NVARCHAR(256) NULL,
                chat_history_id NVARCHAR(256) NULL,
                tenant_id NVARCHAR(256) NULL,
                user_id NVARCHAR(256) NULL,
                credential_id NVARCHAR(256) NULL,
                assistant_id NVARCHAR(256) NULL,
                thread_id NVARCHAR(256) NULL,
                principal_name NVARCHAR(MAX) NULL,
                request_type NVARCHAR(64) NOT NULL DEFAULT 'SystemApi',
                source_type NVARCHAR(64) NOT NULL DEFAULT 'api',
                http_method NVARCHAR(16) NOT NULL,
                route_template NVARCHAR(MAX) NULL,
                request_path NVARCHAR(MAX) NOT NULL,
                request_url NVARCHAR(MAX) NOT NULL,
                source_ip NVARCHAR(128) NULL,
                status_code INT NOT NULL DEFAULT 0,
                success BIT NOT NULL DEFAULT 0,
                duration_ms FLOAT NOT NULL DEFAULT 0,
                request_content_type NVARCHAR(256) NULL,
                response_content_type NVARCHAR(256) NULL,
                request_size_bytes BIGINT NOT NULL DEFAULT 0,
                response_size_bytes BIGINT NOT NULL DEFAULT 0,
                request_body_truncated BIT NOT NULL DEFAULT 0,
                response_body_truncated BIT NOT NULL DEFAULT 0,
                request_body_is_binary BIT NOT NULL DEFAULT 0,
                response_body_is_binary BIT NOT NULL DEFAULT 0,
                route_parameters_json NVARCHAR(MAX) NULL,
                query_parameters_json NVARCHAR(MAX) NULL,
                request_headers_json NVARCHAR(MAX) NULL,
                response_headers_json NVARCHAR(MAX) NULL,
                request_body NVARCHAR(MAX) NULL,
                response_body NVARCHAR(MAX) NULL,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_request_history PRIMARY KEY (id)
            );";

        internal static readonly string AddRequestHistoryTraceIdColumn =
            @"IF COL_LENGTH('request_history', 'trace_id') IS NULL
            ALTER TABLE request_history ADD trace_id NVARCHAR(256) NULL;";

        internal static readonly string AddRequestHistoryChatHistoryIdColumn =
            @"IF COL_LENGTH('request_history', 'chat_history_id') IS NULL
            ALTER TABLE request_history ADD chat_history_id NVARCHAR(256) NULL;";

        internal static readonly string CreateChatHistoryPerformanceEventsTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'chat_history_performance_events')
            CREATE TABLE chat_history_performance_events (
                id NVARCHAR(256) NOT NULL,
                tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default',
                assistant_id NVARCHAR(256) NULL,
                chat_history_id NVARCHAR(256) NOT NULL,
                request_history_id NVARCHAR(256) NULL,
                trace_id NVARCHAR(256) NULL,
                sequence_number INT NOT NULL DEFAULT 0,
                stage NVARCHAR(128) NOT NULL,
                phase NVARCHAR(128) NULL,
                kind NVARCHAR(128) NULL,
                endpoint_id NVARCHAR(256) NULL,
                endpoint_name NVARCHAR(MAX) NULL,
                endpoint_type NVARCHAR(128) NULL,
                provider NVARCHAR(128) NULL,
                api_format NVARCHAR(128) NULL,
                model NVARCHAR(450) NULL,
                started_utc NVARCHAR(64) NULL,
                finished_utc NVARCHAR(64) NULL,
                duration_ms FLOAT NOT NULL DEFAULT 0,
                success BIT NOT NULL DEFAULT 1,
                http_status_code INT NULL,
                error_type NVARCHAR(MAX) NULL,
                error_message NVARCHAR(MAX) NULL,
                input_tokens INT NULL,
                output_tokens INT NULL,
                total_tokens INT NULL,
                chunks_input INT NULL,
                chunks_output INT NULL,
                retrieval_query_count INT NULL,
                endpoint_limiter_wait_ms FLOAT NULL,
                request_to_headers_ms FLOAT NULL,
                headers_to_first_token_ms FLOAT NULL,
                first_token_to_last_token_ms FLOAT NULL,
                client_total_ms FLOAT NULL,
                provider_queue_ms FLOAT NULL,
                provider_load_ms FLOAT NULL,
                provider_prompt_eval_ms FLOAT NULL,
                provider_generation_ms FLOAT NULL,
                provider_total_ms FLOAT NULL,
                provider_tokens_per_second FLOAT NULL,
                provider_request_id NVARCHAR(MAX) NULL,
                metadata_json NVARCHAR(MAX) NULL,
                provider_metrics_json NVARCHAR(MAX) NULL,
                provider_raw_json NVARCHAR(MAX) NULL,
                created_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_chat_history_performance_events PRIMARY KEY (id)
            );";

        internal static readonly string AddChatHistoryPerformanceEventsAssistantIdColumn =
            @"IF COL_LENGTH('chat_history_performance_events', 'assistant_id') IS NULL
            ALTER TABLE chat_history_performance_events ADD assistant_id NVARCHAR(256) NULL;";

        internal static readonly string BackfillChatHistoryPerformanceEventsAssistantId =
            @"UPDATE e
            SET assistant_id = h.assistant_id
            FROM chat_history_performance_events e
            INNER JOIN chat_history h ON h.id = e.chat_history_id
            WHERE e.assistant_id IS NULL;";

        internal static readonly string CreateAssistantToolCallsTable =
            @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'assistant_tool_calls')
            CREATE TABLE assistant_tool_calls (
                id NVARCHAR(256) NOT NULL,
                tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default',
                assistant_id NVARCHAR(256) NOT NULL,
                chat_history_id NVARCHAR(256) NULL,
                request_history_id NVARCHAR(256) NULL,
                trace_id NVARCHAR(256) NULL,
                thread_id NVARCHAR(256) NULL,
                origin NVARCHAR(128) NULL,
                turn_index INT NOT NULL DEFAULT 0,
                iteration INT NOT NULL DEFAULT 0,
                sequence_number INT NOT NULL DEFAULT 0,
                provider_tool_call_id NVARCHAR(256) NULL,
                tool_name NVARCHAR(256) NOT NULL,
                arguments_json NVARCHAR(MAX) NULL,
                output_json NVARCHAR(MAX) NULL,
                result_summary_json NVARCHAR(MAX) NULL,
                success BIT NOT NULL DEFAULT 0,
                denied BIT NOT NULL DEFAULT 0,
                truncated BIT NOT NULL DEFAULT 0,
                output_characters INT NOT NULL DEFAULT 0,
                input_bytes INT NOT NULL DEFAULT 0,
                output_bytes INT NOT NULL DEFAULT 0,
                duration_ms FLOAT NOT NULL DEFAULT 0,
                error_type NVARCHAR(MAX) NULL,
                error_message NVARCHAR(MAX) NULL,
                provider NVARCHAR(128) NULL,
                model NVARCHAR(450) NULL,
                active BIT NOT NULL DEFAULT 1,
                started_utc NVARCHAR(64) NOT NULL,
                finished_utc NVARCHAR(64) NOT NULL,
                created_utc NVARCHAR(64) NOT NULL,
                last_update_utc NVARCHAR(64) NOT NULL,
                CONSTRAINT pk_assistant_tool_calls PRIMARY KEY (id)
            );";

        internal static readonly string AddAssistantToolCallsTurnIndexColumn =
            @"IF COL_LENGTH('assistant_tool_calls', 'turn_index') IS NULL
            ALTER TABLE assistant_tool_calls ADD turn_index INT NOT NULL DEFAULT 0;";

        internal static readonly string AddAssistantToolCallsResultSummaryJsonColumn =
            @"IF COL_LENGTH('assistant_tool_calls', 'result_summary_json') IS NULL
            ALTER TABLE assistant_tool_calls ADD result_summary_json NVARCHAR(MAX) NULL;";

        internal static readonly string AddAssistantToolCallsInputBytesColumn =
            @"IF COL_LENGTH('assistant_tool_calls', 'input_bytes') IS NULL
            ALTER TABLE assistant_tool_calls ADD input_bytes INT NOT NULL DEFAULT 0;";

        internal static readonly string AddAssistantToolCallsOutputBytesColumn =
            @"IF COL_LENGTH('assistant_tool_calls', 'output_bytes') IS NULL
            ALTER TABLE assistant_tool_calls ADD output_bytes INT NOT NULL DEFAULT 0;";

        internal static readonly string AddAssistantToolCallsErrorTypeColumn =
            @"IF COL_LENGTH('assistant_tool_calls', 'error_type') IS NULL
            ALTER TABLE assistant_tool_calls ADD error_type NVARCHAR(MAX) NULL;";

        internal static readonly string AddAssistantToolCallsProviderColumn =
            @"IF COL_LENGTH('assistant_tool_calls', 'provider') IS NULL
            ALTER TABLE assistant_tool_calls ADD provider NVARCHAR(128) NULL;";

        internal static readonly string AddAssistantToolCallsModelColumn =
            @"IF COL_LENGTH('assistant_tool_calls', 'model') IS NULL
            ALTER TABLE assistant_tool_calls ADD model NVARCHAR(450) NULL;";

        internal static readonly string AddAssistantToolCallsActiveColumn =
            @"IF COL_LENGTH('assistant_tool_calls', 'active') IS NULL
            ALTER TABLE assistant_tool_calls ADD active BIT NOT NULL DEFAULT 1;";

        internal static readonly string AddAssistantToolCallsLastUpdateUtcColumn =
            @"IF COL_LENGTH('assistant_tool_calls', 'last_update_utc') IS NULL
            ALTER TABLE assistant_tool_calls ADD last_update_utc NVARCHAR(64) NOT NULL DEFAULT '';";

        #endregion

        #region Indices

        internal static readonly string CreateTenantsNameIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_tenants_name')
            CREATE INDEX idx_tenants_name ON tenants (name);";

        internal static readonly string CreateTenantsCreatedUtcIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_tenants_created_utc')
            CREATE INDEX idx_tenants_created_utc ON tenants (created_utc);";

        internal static readonly string CreateUsersEmailIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_users_email')
            CREATE INDEX idx_users_email ON users (email);";

        internal static readonly string CreateUsersTenantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_users_tenant_id')
            CREATE INDEX idx_users_tenant_id ON users (tenant_id);";

        internal static readonly string CreateUsersTenantEmailIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_users_tenant_email')
            CREATE UNIQUE INDEX idx_users_tenant_email ON users (tenant_id, email);";

        internal static readonly string CreateCredentialsUserIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_credentials_user_id')
            CREATE INDEX idx_credentials_user_id ON credentials (user_id);";

        internal static readonly string CreateCredentialsBearerTokenIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_credentials_bearer_token')
            CREATE INDEX idx_credentials_bearer_token ON credentials (bearer_token);";

        internal static readonly string CreateCredentialsTenantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_credentials_tenant_id')
            CREATE INDEX idx_credentials_tenant_id ON credentials (tenant_id);";

        internal static readonly string CreateAssistantsUserIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistants_user_id')
            CREATE INDEX idx_assistants_user_id ON assistants (user_id);";

        internal static readonly string CreateAssistantsTenantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistants_tenant_id')
            CREATE INDEX idx_assistants_tenant_id ON assistants (tenant_id);";

        internal static readonly string CreateAssistantSettingsAssistantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_settings_assistant_id')
            CREATE INDEX idx_assistant_settings_assistant_id ON assistant_settings (assistant_id);";

        internal static readonly string CreateAssistantDocumentsTenantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_documents_tenant_id')
            CREATE INDEX idx_assistant_documents_tenant_id ON assistant_documents (tenant_id);";

        internal static readonly string CreateAssistantDocumentsIngestionRuleIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_documents_ingestion_rule_id')
            CREATE INDEX idx_assistant_documents_ingestion_rule_id ON assistant_documents (ingestion_rule_id);";

        internal static readonly string CreateAssistantFeedbackAssistantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_feedback_assistant_id')
            CREATE INDEX idx_assistant_feedback_assistant_id ON assistant_feedback (assistant_id);";

        internal static readonly string CreateAssistantFeedbackTenantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_feedback_tenant_id')
            CREATE INDEX idx_assistant_feedback_tenant_id ON assistant_feedback (tenant_id);";

        internal static readonly string CreateAssistantFeedbackTenantAssistantCreatedIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_feedback_tenant_assistant_created')
            CREATE INDEX idx_assistant_feedback_tenant_assistant_created ON assistant_feedback (tenant_id, assistant_id, created_utc);";

        internal static readonly string CreateIngestionRulesNameIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_ingestion_rules_name')
            CREATE INDEX idx_ingestion_rules_name ON ingestion_rules (name);";

        internal static readonly string CreateIngestionRulesTenantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_ingestion_rules_tenant_id')
            CREATE INDEX idx_ingestion_rules_tenant_id ON ingestion_rules (tenant_id);";

        internal static readonly string CreateChatHistoryAssistantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_assistant_id')
            CREATE INDEX idx_chat_history_assistant_id ON chat_history (assistant_id);";

        internal static readonly string CreateChatHistoryThreadIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_thread_id')
            CREATE INDEX idx_chat_history_thread_id ON chat_history (thread_id);";

        internal static readonly string CreateChatHistoryCreatedUtcIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_created_utc')
            CREATE INDEX idx_chat_history_created_utc ON chat_history (created_utc);";

        internal static readonly string CreateChatHistoryTenantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_tenant_id')
            CREATE INDEX idx_chat_history_tenant_id ON chat_history (tenant_id);";

        internal static readonly string CreateChatHistoryTraceIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_trace_id')
            CREATE INDEX idx_chat_history_trace_id ON chat_history (trace_id);";

        internal static readonly string CreateChatHistoryRequestHistoryIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_request_history_id')
            CREATE INDEX idx_chat_history_request_history_id ON chat_history (request_history_id);";

        internal static readonly string CreateRequestHistoryTenantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_tenant_id')
            CREATE INDEX idx_request_history_tenant_id ON request_history (tenant_id);";

        internal static readonly string CreateRequestHistoryUserIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_user_id')
            CREATE INDEX idx_request_history_user_id ON request_history (user_id);";

        internal static readonly string CreateRequestHistoryCredentialIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_credential_id')
            CREATE INDEX idx_request_history_credential_id ON request_history (credential_id);";

        internal static readonly string CreateRequestHistoryAssistantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_assistant_id')
            CREATE INDEX idx_request_history_assistant_id ON request_history (assistant_id);";

        internal static readonly string CreateRequestHistoryThreadIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_thread_id')
            CREATE INDEX idx_request_history_thread_id ON request_history (thread_id);";

        internal static readonly string CreateRequestHistoryStatusCodeIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_status_code')
            CREATE INDEX idx_request_history_status_code ON request_history (status_code);";

        internal static readonly string CreateRequestHistorySuccessIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_success')
            CREATE INDEX idx_request_history_success ON request_history (success);";

        internal static readonly string CreateRequestHistoryCreatedUtcIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_created_utc')
            CREATE INDEX idx_request_history_created_utc ON request_history (created_utc);";

        internal static readonly string CreateRequestHistoryPathIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_request_path')
            CREATE INDEX idx_request_history_request_path ON request_history (request_path);";

        internal static readonly string CreateRequestHistoryTraceIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_trace_id')
            CREATE INDEX idx_request_history_trace_id ON request_history (trace_id);";

        internal static readonly string CreateRequestHistoryChatHistoryIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_chat_history_id')
            CREATE INDEX idx_request_history_chat_history_id ON request_history (chat_history_id);";

        internal static readonly string CreateRequestHistoryTenantAssistantCreatedIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_tenant_assistant_created')
            CREATE INDEX idx_request_history_tenant_assistant_created ON request_history (tenant_id, assistant_id, created_utc);";

        internal static readonly string CreateRequestHistoryTenantAssistantSuccessCreatedIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_tenant_assistant_success_created')
            CREATE INDEX idx_request_history_tenant_assistant_success_created ON request_history (tenant_id, assistant_id, success, created_utc);";

        internal static readonly string CreateChatHistoryPerformanceEventsChatHistoryIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_chat_history_id')
            CREATE INDEX idx_chat_history_performance_events_chat_history_id ON chat_history_performance_events (chat_history_id);";

        internal static readonly string CreateChatHistoryPerformanceEventsAssistantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chpe_assistant_id')
            CREATE INDEX idx_chpe_assistant_id ON chat_history_performance_events (assistant_id);";

        internal static readonly string CreateChatHistoryPerformanceEventsRequestHistoryIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_request_history_id')
            CREATE INDEX idx_chat_history_performance_events_request_history_id ON chat_history_performance_events (request_history_id);";

        internal static readonly string CreateChatHistoryPerformanceEventsTraceIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_trace_id')
            CREATE INDEX idx_chat_history_performance_events_trace_id ON chat_history_performance_events (trace_id);";

        internal static readonly string CreateChatHistoryPerformanceEventsStageIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_stage')
            CREATE INDEX idx_chat_history_performance_events_stage ON chat_history_performance_events (stage);";

        internal static readonly string CreateChatHistoryPerformanceEventsStartedUtcIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_started_utc')
            CREATE INDEX idx_chat_history_performance_events_started_utc ON chat_history_performance_events (started_utc);";

        internal static readonly string CreateChatHistoryPerformanceEventsTenantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_tenant_id')
            CREATE INDEX idx_chat_history_performance_events_tenant_id ON chat_history_performance_events (tenant_id);";

        internal static readonly string CreateChatHistoryPerformanceEventsEndpointIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_endpoint_id')
            CREATE INDEX idx_chat_history_performance_events_endpoint_id ON chat_history_performance_events (endpoint_id);";

        internal static readonly string CreateChatHistoryPerformanceEventsProviderModelIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_provider_model')
            CREATE INDEX idx_chat_history_performance_events_provider_model ON chat_history_performance_events (provider, model);";

        internal static readonly string CreateChatHistoryPerformanceEventsCreatedUtcIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_created_utc')
            CREATE INDEX idx_chat_history_performance_events_created_utc ON chat_history_performance_events (created_utc);";

        internal static readonly string CreateChatHistoryPerformanceEventsDurationMsIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_duration_ms')
            CREATE INDEX idx_chat_history_performance_events_duration_ms ON chat_history_performance_events (duration_ms);";

        internal static readonly string CreateChatHistoryPerformanceEventsTenantCreatedIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_tenant_created')
            CREATE INDEX idx_chat_history_performance_events_tenant_created ON chat_history_performance_events (tenant_id, created_utc);";

        internal static readonly string CreateChatHistoryPerformanceEventsTenantAssistantCreatedIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chpe_tenant_assistant_created')
            CREATE INDEX idx_chpe_tenant_assistant_created ON chat_history_performance_events (tenant_id, assistant_id, created_utc);";

        internal static readonly string CreateChatHistoryPerformanceEventsTenantAssistantStageCreatedIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chpe_tenant_assistant_stage_created')
            CREATE INDEX idx_chpe_tenant_assistant_stage_created ON chat_history_performance_events (tenant_id, assistant_id, stage, created_utc);";

        internal static readonly string CreateChatHistoryPerformanceEventsTenantAssistantEndpointCreatedIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chpe_tenant_assistant_endpoint_created')
            CREATE INDEX idx_chpe_tenant_assistant_endpoint_created ON chat_history_performance_events (tenant_id, assistant_id, endpoint_id, created_utc);";

        internal static readonly string CreateAssistantToolCallsTenantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_tool_calls_tenant_id')
            CREATE INDEX idx_assistant_tool_calls_tenant_id ON assistant_tool_calls (tenant_id);";

        internal static readonly string CreateAssistantToolCallsAssistantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_tool_calls_assistant_id')
            CREATE INDEX idx_assistant_tool_calls_assistant_id ON assistant_tool_calls (assistant_id);";

        internal static readonly string CreateAssistantToolCallsThreadIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_tool_calls_thread_id')
            CREATE INDEX idx_assistant_tool_calls_thread_id ON assistant_tool_calls (thread_id);";

        internal static readonly string CreateAssistantToolCallsChatHistoryIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_tool_calls_chat_history_id')
            CREATE INDEX idx_assistant_tool_calls_chat_history_id ON assistant_tool_calls (chat_history_id);";

        internal static readonly string CreateAssistantToolCallsRequestHistoryIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_tool_calls_request_history_id')
            CREATE INDEX idx_assistant_tool_calls_request_history_id ON assistant_tool_calls (request_history_id);";

        internal static readonly string CreateAssistantToolCallsTraceIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_tool_calls_trace_id')
            CREATE INDEX idx_assistant_tool_calls_trace_id ON assistant_tool_calls (trace_id);";

        internal static readonly string CreateAssistantToolCallsToolNameIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_tool_calls_tool_name')
            CREATE INDEX idx_assistant_tool_calls_tool_name ON assistant_tool_calls (tool_name);";

        internal static readonly string CreateAssistantToolCallsSuccessIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_tool_calls_success')
            CREATE INDEX idx_assistant_tool_calls_success ON assistant_tool_calls (success);";

        internal static readonly string CreateAssistantToolCallsCreatedUtcIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_tool_calls_created_utc')
            CREATE INDEX idx_assistant_tool_calls_created_utc ON assistant_tool_calls (created_utc);";

        internal static readonly string CreateAssistantToolCallsTenantAssistantCreatedIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_atc_tenant_assistant_created')
            CREATE INDEX idx_atc_tenant_assistant_created ON assistant_tool_calls (tenant_id, assistant_id, created_utc);";

        internal static readonly string CreateAssistantToolCallsTenantAssistantToolCreatedIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_atc_tenant_assistant_tool_created')
            CREATE INDEX idx_atc_tenant_assistant_tool_created ON assistant_tool_calls (tenant_id, assistant_id, tool_name, created_utc);";

        internal static readonly string CreateCrawlPlansTenantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_crawl_plans_tenant_id')
            CREATE INDEX idx_crawl_plans_tenant_id ON crawl_plans (tenant_id);";

        internal static readonly string CreateCrawlPlansStateIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_crawl_plans_state')
            CREATE INDEX idx_crawl_plans_state ON crawl_plans (state);";

        internal static readonly string CreateCrawlOperationsTenantIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_crawl_operations_tenant_id')
            CREATE INDEX idx_crawl_operations_tenant_id ON crawl_operations (tenant_id);";

        internal static readonly string CreateCrawlOperationsCrawlPlanIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_crawl_operations_crawl_plan_id')
            CREATE INDEX idx_crawl_operations_crawl_plan_id ON crawl_operations (crawl_plan_id);";

        internal static readonly string CreateCrawlOperationsCreatedUtcIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_crawl_operations_created_utc')
            CREATE INDEX idx_crawl_operations_created_utc ON crawl_operations (created_utc);";

        internal static readonly string CreateAssistantDocumentsCrawlPlanIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_documents_crawl_plan_id')
            CREATE INDEX idx_assistant_documents_crawl_plan_id ON assistant_documents (crawl_plan_id);";

        internal static readonly string CreateAssistantDocumentsCrawlOperationIdIndex =
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_documents_crawl_operation_id')
            CREATE INDEX idx_assistant_documents_crawl_operation_id ON assistant_documents (crawl_operation_id);";

        #endregion
    }
}
