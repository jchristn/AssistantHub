-- Migration 003: Upgrade existing v0.4.0 database to v0.5.0
-- Adds crawl_plans and crawl_operations tables, and crawl traceability
-- columns to assistant_documents.
-- Back up your database before running this migration.
--
-- What this migration does:
--   1. Creates the crawl_plans table
--   2. Creates the crawl_operations table
--   3. Adds crawl_plan_id, crawl_operation_id, source_url to assistant_documents
--   4. Creates indexes for efficient querying

------------------------------------------------------------------------
-- SQLite
------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS crawl_plans (
  id TEXT PRIMARY KEY,
  tenant_id TEXT NOT NULL DEFAULT 'default',
  name TEXT NOT NULL,
  repository_type TEXT NOT NULL DEFAULT 'Web',
  ingestion_settings_json TEXT,
  repository_settings_json TEXT,
  schedule_json TEXT,
  filter_json TEXT,
  process_additions INTEGER NOT NULL DEFAULT 1,
  process_updates INTEGER NOT NULL DEFAULT 1,
  process_deletions INTEGER NOT NULL DEFAULT 0,
  max_drain_tasks INTEGER NOT NULL DEFAULT 8,
  retention_days INTEGER NOT NULL DEFAULT 7,
  state TEXT NOT NULL DEFAULT 'Stopped',
  last_crawl_start_utc TEXT,
  last_crawl_finish_utc TEXT,
  last_crawl_success INTEGER,
  created_utc TEXT NOT NULL,
  last_update_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS crawl_operations (
  id TEXT PRIMARY KEY,
  tenant_id TEXT NOT NULL DEFAULT 'default',
  crawl_plan_id TEXT NOT NULL,
  state TEXT NOT NULL DEFAULT 'NotStarted',
  status_message TEXT,
  objects_enumerated INTEGER NOT NULL DEFAULT 0,
  bytes_enumerated INTEGER NOT NULL DEFAULT 0,
  objects_added INTEGER NOT NULL DEFAULT 0,
  bytes_added INTEGER NOT NULL DEFAULT 0,
  objects_updated INTEGER NOT NULL DEFAULT 0,
  bytes_updated INTEGER NOT NULL DEFAULT 0,
  objects_deleted INTEGER NOT NULL DEFAULT 0,
  bytes_deleted INTEGER NOT NULL DEFAULT 0,
  objects_success INTEGER NOT NULL DEFAULT 0,
  bytes_success INTEGER NOT NULL DEFAULT 0,
  objects_failed INTEGER NOT NULL DEFAULT 0,
  bytes_failed INTEGER NOT NULL DEFAULT 0,
  enumeration_file TEXT,
  start_utc TEXT,
  start_enumeration_utc TEXT,
  finish_enumeration_utc TEXT,
  start_retrieval_utc TEXT,
  finish_retrieval_utc TEXT,
  finish_utc TEXT,
  created_utc TEXT NOT NULL,
  last_update_utc TEXT NOT NULL
);

ALTER TABLE assistant_documents ADD COLUMN crawl_plan_id TEXT;
ALTER TABLE assistant_documents ADD COLUMN crawl_operation_id TEXT;
ALTER TABLE assistant_documents ADD COLUMN source_url TEXT;

CREATE INDEX IF NOT EXISTS idx_crawl_plans_tenant_id ON crawl_plans(tenant_id);
CREATE INDEX IF NOT EXISTS idx_crawl_plans_state ON crawl_plans(state);
CREATE INDEX IF NOT EXISTS idx_crawl_operations_tenant_id ON crawl_operations(tenant_id);
CREATE INDEX IF NOT EXISTS idx_crawl_operations_crawl_plan_id ON crawl_operations(crawl_plan_id);
CREATE INDEX IF NOT EXISTS idx_crawl_operations_created_utc ON crawl_operations(created_utc);
CREATE INDEX IF NOT EXISTS idx_assistant_documents_crawl_plan_id ON assistant_documents(crawl_plan_id);
CREATE INDEX IF NOT EXISTS idx_assistant_documents_crawl_operation_id ON assistant_documents(crawl_operation_id);

------------------------------------------------------------------------
-- PostgreSQL
------------------------------------------------------------------------

-- CREATE TABLE IF NOT EXISTS crawl_plans (
--   id TEXT PRIMARY KEY,
--   tenant_id TEXT NOT NULL DEFAULT 'default',
--   name TEXT NOT NULL,
--   repository_type TEXT NOT NULL DEFAULT 'Web',
--   ingestion_settings_json TEXT,
--   repository_settings_json TEXT,
--   schedule_json TEXT,
--   filter_json TEXT,
--   process_additions BOOLEAN NOT NULL DEFAULT TRUE,
--   process_updates BOOLEAN NOT NULL DEFAULT TRUE,
--   process_deletions BOOLEAN NOT NULL DEFAULT FALSE,
--   max_drain_tasks INTEGER NOT NULL DEFAULT 8,
--   retention_days INTEGER NOT NULL DEFAULT 7,
--   state TEXT NOT NULL DEFAULT 'Stopped',
--   last_crawl_start_utc TIMESTAMPTZ,
--   last_crawl_finish_utc TIMESTAMPTZ,
--   last_crawl_success BOOLEAN,
--   created_utc TIMESTAMPTZ NOT NULL,
--   last_update_utc TIMESTAMPTZ NOT NULL
-- );

-- CREATE TABLE IF NOT EXISTS crawl_operations (
--   id TEXT PRIMARY KEY,
--   tenant_id TEXT NOT NULL DEFAULT 'default',
--   crawl_plan_id TEXT NOT NULL,
--   state TEXT NOT NULL DEFAULT 'NotStarted',
--   status_message TEXT,
--   objects_enumerated BIGINT NOT NULL DEFAULT 0,
--   bytes_enumerated BIGINT NOT NULL DEFAULT 0,
--   objects_added BIGINT NOT NULL DEFAULT 0,
--   bytes_added BIGINT NOT NULL DEFAULT 0,
--   objects_updated BIGINT NOT NULL DEFAULT 0,
--   bytes_updated BIGINT NOT NULL DEFAULT 0,
--   objects_deleted BIGINT NOT NULL DEFAULT 0,
--   bytes_deleted BIGINT NOT NULL DEFAULT 0,
--   objects_success BIGINT NOT NULL DEFAULT 0,
--   bytes_success BIGINT NOT NULL DEFAULT 0,
--   objects_failed BIGINT NOT NULL DEFAULT 0,
--   bytes_failed BIGINT NOT NULL DEFAULT 0,
--   enumeration_file TEXT,
--   start_utc TIMESTAMPTZ,
--   start_enumeration_utc TIMESTAMPTZ,
--   finish_enumeration_utc TIMESTAMPTZ,
--   start_retrieval_utc TIMESTAMPTZ,
--   finish_retrieval_utc TIMESTAMPTZ,
--   finish_utc TIMESTAMPTZ,
--   created_utc TIMESTAMPTZ NOT NULL,
--   last_update_utc TIMESTAMPTZ NOT NULL
-- );

-- ALTER TABLE assistant_documents ADD COLUMN crawl_plan_id TEXT;
-- ALTER TABLE assistant_documents ADD COLUMN crawl_operation_id TEXT;
-- ALTER TABLE assistant_documents ADD COLUMN source_url TEXT;

-- CREATE INDEX IF NOT EXISTS idx_crawl_plans_tenant_id ON crawl_plans(tenant_id);
-- CREATE INDEX IF NOT EXISTS idx_crawl_plans_state ON crawl_plans(state);
-- CREATE INDEX IF NOT EXISTS idx_crawl_operations_tenant_id ON crawl_operations(tenant_id);
-- CREATE INDEX IF NOT EXISTS idx_crawl_operations_crawl_plan_id ON crawl_operations(crawl_plan_id);
-- CREATE INDEX IF NOT EXISTS idx_crawl_operations_created_utc ON crawl_operations(created_utc);
-- CREATE INDEX IF NOT EXISTS idx_assistant_documents_crawl_plan_id ON assistant_documents(crawl_plan_id);
-- CREATE INDEX IF NOT EXISTS idx_assistant_documents_crawl_operation_id ON assistant_documents(crawl_operation_id);

------------------------------------------------------------------------
-- MySQL
------------------------------------------------------------------------

-- CREATE TABLE IF NOT EXISTS crawl_plans (
--   id VARCHAR(64) PRIMARY KEY,
--   tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
--   name VARCHAR(256) NOT NULL,
--   repository_type VARCHAR(64) NOT NULL DEFAULT 'Web',
--   ingestion_settings_json TEXT,
--   repository_settings_json TEXT,
--   schedule_json TEXT,
--   filter_json TEXT,
--   process_additions TINYINT(1) NOT NULL DEFAULT 1,
--   process_updates TINYINT(1) NOT NULL DEFAULT 1,
--   process_deletions TINYINT(1) NOT NULL DEFAULT 0,
--   max_drain_tasks INT NOT NULL DEFAULT 8,
--   retention_days INT NOT NULL DEFAULT 7,
--   state VARCHAR(64) NOT NULL DEFAULT 'Stopped',
--   last_crawl_start_utc DATETIME(6),
--   last_crawl_finish_utc DATETIME(6),
--   last_crawl_success TINYINT(1),
--   created_utc DATETIME(6) NOT NULL,
--   last_update_utc DATETIME(6) NOT NULL
-- );

-- CREATE TABLE IF NOT EXISTS crawl_operations (
--   id VARCHAR(64) PRIMARY KEY,
--   tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
--   crawl_plan_id VARCHAR(64) NOT NULL,
--   state VARCHAR(64) NOT NULL DEFAULT 'NotStarted',
--   status_message TEXT,
--   objects_enumerated BIGINT NOT NULL DEFAULT 0,
--   bytes_enumerated BIGINT NOT NULL DEFAULT 0,
--   objects_added BIGINT NOT NULL DEFAULT 0,
--   bytes_added BIGINT NOT NULL DEFAULT 0,
--   objects_updated BIGINT NOT NULL DEFAULT 0,
--   bytes_updated BIGINT NOT NULL DEFAULT 0,
--   objects_deleted BIGINT NOT NULL DEFAULT 0,
--   bytes_deleted BIGINT NOT NULL DEFAULT 0,
--   objects_success BIGINT NOT NULL DEFAULT 0,
--   bytes_success BIGINT NOT NULL DEFAULT 0,
--   objects_failed BIGINT NOT NULL DEFAULT 0,
--   bytes_failed BIGINT NOT NULL DEFAULT 0,
--   enumeration_file TEXT,
--   start_utc DATETIME(6),
--   start_enumeration_utc DATETIME(6),
--   finish_enumeration_utc DATETIME(6),
--   start_retrieval_utc DATETIME(6),
--   finish_retrieval_utc DATETIME(6),
--   finish_utc DATETIME(6),
--   created_utc DATETIME(6) NOT NULL,
--   last_update_utc DATETIME(6) NOT NULL
-- );

-- ALTER TABLE assistant_documents ADD COLUMN crawl_plan_id VARCHAR(64);
-- ALTER TABLE assistant_documents ADD COLUMN crawl_operation_id VARCHAR(64);
-- ALTER TABLE assistant_documents ADD COLUMN source_url TEXT;

-- CREATE INDEX idx_crawl_plans_tenant_id ON crawl_plans(tenant_id);
-- CREATE INDEX idx_crawl_plans_state ON crawl_plans(state);
-- CREATE INDEX idx_crawl_operations_tenant_id ON crawl_operations(tenant_id);
-- CREATE INDEX idx_crawl_operations_crawl_plan_id ON crawl_operations(crawl_plan_id);
-- CREATE INDEX idx_crawl_operations_created_utc ON crawl_operations(created_utc);
-- CREATE INDEX idx_assistant_documents_crawl_plan_id ON assistant_documents(crawl_plan_id);
-- CREATE INDEX idx_assistant_documents_crawl_operation_id ON assistant_documents(crawl_operation_id);

------------------------------------------------------------------------
-- SQL Server
------------------------------------------------------------------------

-- IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'crawl_plans')
-- CREATE TABLE crawl_plans (
--   id NVARCHAR(64) PRIMARY KEY,
--   tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
--   name NVARCHAR(256) NOT NULL,
--   repository_type NVARCHAR(64) NOT NULL DEFAULT 'Web',
--   ingestion_settings_json NVARCHAR(MAX),
--   repository_settings_json NVARCHAR(MAX),
--   schedule_json NVARCHAR(MAX),
--   filter_json NVARCHAR(MAX),
--   process_additions BIT NOT NULL DEFAULT 1,
--   process_updates BIT NOT NULL DEFAULT 1,
--   process_deletions BIT NOT NULL DEFAULT 0,
--   max_drain_tasks INT NOT NULL DEFAULT 8,
--   retention_days INT NOT NULL DEFAULT 7,
--   state NVARCHAR(64) NOT NULL DEFAULT 'Stopped',
--   last_crawl_start_utc DATETIME2,
--   last_crawl_finish_utc DATETIME2,
--   last_crawl_success BIT,
--   created_utc DATETIME2 NOT NULL,
--   last_update_utc DATETIME2 NOT NULL
-- );

-- IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'crawl_operations')
-- CREATE TABLE crawl_operations (
--   id NVARCHAR(64) PRIMARY KEY,
--   tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
--   crawl_plan_id NVARCHAR(64) NOT NULL,
--   state NVARCHAR(64) NOT NULL DEFAULT 'NotStarted',
--   status_message NVARCHAR(MAX),
--   objects_enumerated BIGINT NOT NULL DEFAULT 0,
--   bytes_enumerated BIGINT NOT NULL DEFAULT 0,
--   objects_added BIGINT NOT NULL DEFAULT 0,
--   bytes_added BIGINT NOT NULL DEFAULT 0,
--   objects_updated BIGINT NOT NULL DEFAULT 0,
--   bytes_updated BIGINT NOT NULL DEFAULT 0,
--   objects_deleted BIGINT NOT NULL DEFAULT 0,
--   bytes_deleted BIGINT NOT NULL DEFAULT 0,
--   objects_success BIGINT NOT NULL DEFAULT 0,
--   bytes_success BIGINT NOT NULL DEFAULT 0,
--   objects_failed BIGINT NOT NULL DEFAULT 0,
--   bytes_failed BIGINT NOT NULL DEFAULT 0,
--   enumeration_file NVARCHAR(MAX),
--   start_utc DATETIME2,
--   start_enumeration_utc DATETIME2,
--   finish_enumeration_utc DATETIME2,
--   start_retrieval_utc DATETIME2,
--   finish_retrieval_utc DATETIME2,
--   finish_utc DATETIME2,
--   created_utc DATETIME2 NOT NULL,
--   last_update_utc DATETIME2 NOT NULL
-- );

-- ALTER TABLE assistant_documents ADD crawl_plan_id NVARCHAR(64) NULL;
-- ALTER TABLE assistant_documents ADD crawl_operation_id NVARCHAR(64) NULL;
-- ALTER TABLE assistant_documents ADD source_url NVARCHAR(MAX) NULL;

-- CREATE INDEX idx_crawl_plans_tenant_id ON crawl_plans(tenant_id);
-- CREATE INDEX idx_crawl_plans_state ON crawl_plans(state);
-- CREATE INDEX idx_crawl_operations_tenant_id ON crawl_operations(tenant_id);
-- CREATE INDEX idx_crawl_operations_crawl_plan_id ON crawl_operations(crawl_plan_id);
-- CREATE INDEX idx_crawl_operations_created_utc ON crawl_operations(created_utc);
-- CREATE INDEX idx_assistant_documents_crawl_plan_id ON assistant_documents(crawl_plan_id);
-- CREATE INDEX idx_assistant_documents_crawl_operation_id ON assistant_documents(crawl_operation_id);
