-- Migration script for AssistantHub v0.12.0
-- SQL Server
-- Provider-agnostic assistant performance telemetry

IF COL_LENGTH('chat_history', 'trace_id') IS NULL
BEGIN
    ALTER TABLE chat_history ADD trace_id NVARCHAR(256) NULL;
END
GO

IF COL_LENGTH('chat_history', 'request_history_id') IS NULL
BEGIN
    ALTER TABLE chat_history ADD request_history_id NVARCHAR(256) NULL;
END
GO

IF COL_LENGTH('chat_history', 'performance_schema_version') IS NULL
BEGIN
    ALTER TABLE chat_history ADD performance_schema_version INT NOT NULL DEFAULT 1;
END
GO

IF COL_LENGTH('chat_history', 'performance_json') IS NULL
BEGIN
    ALTER TABLE chat_history ADD performance_json NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('request_history', 'trace_id') IS NULL
BEGIN
    ALTER TABLE request_history ADD trace_id NVARCHAR(256) NULL;
END
GO

IF COL_LENGTH('request_history', 'chat_history_id') IS NULL
BEGIN
    ALTER TABLE request_history ADD chat_history_id NVARCHAR(256) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'chat_history_performance_events')
BEGIN
    CREATE TABLE chat_history_performance_events (
        id NVARCHAR(256) NOT NULL,
        tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default',
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
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_trace_id')
    CREATE INDEX idx_chat_history_trace_id ON chat_history (trace_id);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_request_history_id')
    CREATE INDEX idx_chat_history_request_history_id ON chat_history (request_history_id);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_trace_id')
    CREATE INDEX idx_request_history_trace_id ON request_history (trace_id);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_chat_history_id')
    CREATE INDEX idx_request_history_chat_history_id ON request_history (chat_history_id);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_chat_history_id')
    CREATE INDEX idx_chat_history_performance_events_chat_history_id ON chat_history_performance_events (chat_history_id);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_request_history_id')
    CREATE INDEX idx_chat_history_performance_events_request_history_id ON chat_history_performance_events (request_history_id);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_trace_id')
    CREATE INDEX idx_chat_history_performance_events_trace_id ON chat_history_performance_events (trace_id);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_stage')
    CREATE INDEX idx_chat_history_performance_events_stage ON chat_history_performance_events (stage);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_started_utc')
    CREATE INDEX idx_chat_history_performance_events_started_utc ON chat_history_performance_events (started_utc);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_tenant_id')
    CREATE INDEX idx_chat_history_performance_events_tenant_id ON chat_history_performance_events (tenant_id);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_endpoint_id')
    CREATE INDEX idx_chat_history_performance_events_endpoint_id ON chat_history_performance_events (endpoint_id);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_provider_model')
    CREATE INDEX idx_chat_history_performance_events_provider_model ON chat_history_performance_events (provider, model);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_created_utc')
    CREATE INDEX idx_chat_history_performance_events_created_utc ON chat_history_performance_events (created_utc);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_duration_ms')
    CREATE INDEX idx_chat_history_performance_events_duration_ms ON chat_history_performance_events (duration_ms);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chat_history_performance_events_tenant_created')
    CREATE INDEX idx_chat_history_performance_events_tenant_created ON chat_history_performance_events (tenant_id, created_utc);
GO
