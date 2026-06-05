-- Migration script for AssistantHub v0.13.0
-- SQL Server
-- Assistant analytics indexes and performance-event assistant backfill

IF COL_LENGTH('chat_history_performance_events', 'assistant_id') IS NULL
BEGIN
    ALTER TABLE chat_history_performance_events ADD assistant_id NVARCHAR(256) NULL;
END
GO

UPDATE e
SET assistant_id = h.assistant_id
FROM chat_history_performance_events e
INNER JOIN chat_history h ON h.id = e.chat_history_id
WHERE e.assistant_id IS NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_assistant_feedback_tenant_assistant_created')
    CREATE INDEX idx_assistant_feedback_tenant_assistant_created ON assistant_feedback (tenant_id, assistant_id, created_utc);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_tenant_assistant_created')
    CREATE INDEX idx_request_history_tenant_assistant_created ON request_history (tenant_id, assistant_id, created_utc);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_request_history_tenant_assistant_success_created')
    CREATE INDEX idx_request_history_tenant_assistant_success_created ON request_history (tenant_id, assistant_id, success, created_utc);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chpe_assistant_id')
    CREATE INDEX idx_chpe_assistant_id ON chat_history_performance_events (assistant_id);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chpe_tenant_assistant_created')
    CREATE INDEX idx_chpe_tenant_assistant_created ON chat_history_performance_events (tenant_id, assistant_id, created_utc);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chpe_tenant_assistant_stage_created')
    CREATE INDEX idx_chpe_tenant_assistant_stage_created ON chat_history_performance_events (tenant_id, assistant_id, stage, created_utc);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_chpe_tenant_assistant_endpoint_created')
    CREATE INDEX idx_chpe_tenant_assistant_endpoint_created ON chat_history_performance_events (tenant_id, assistant_id, endpoint_id, created_utc);
GO
