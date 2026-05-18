-- Migration script for AssistantHub v0.10.0
-- Request history and API explorer support
--
-- This script adds:
--   1. request_history table for HTTP request/response observability
--   2. supporting request_history indexes for filtering and retention cleanup
--
-- Run this script against your existing database before starting v0.10.0.

-- ============================================================================
-- SQLite
-- ============================================================================

CREATE TABLE IF NOT EXISTS request_history (
  id TEXT PRIMARY KEY,
  tenant_id TEXT,
  user_id TEXT,
  credential_id TEXT,
  assistant_id TEXT,
  thread_id TEXT,
  principal_name TEXT,
  request_type TEXT NOT NULL DEFAULT 'SystemApi',
  source_type TEXT NOT NULL DEFAULT 'api',
  http_method TEXT NOT NULL,
  route_template TEXT,
  request_path TEXT NOT NULL,
  request_url TEXT NOT NULL,
  source_ip TEXT,
  status_code INTEGER NOT NULL DEFAULT 0,
  success INTEGER NOT NULL DEFAULT 0,
  duration_ms REAL NOT NULL DEFAULT 0,
  request_content_type TEXT,
  response_content_type TEXT,
  request_size_bytes INTEGER NOT NULL DEFAULT 0,
  response_size_bytes INTEGER NOT NULL DEFAULT 0,
  request_body_truncated INTEGER NOT NULL DEFAULT 0,
  response_body_truncated INTEGER NOT NULL DEFAULT 0,
  request_body_is_binary INTEGER NOT NULL DEFAULT 0,
  response_body_is_binary INTEGER NOT NULL DEFAULT 0,
  route_parameters_json TEXT,
  query_parameters_json TEXT,
  request_headers_json TEXT,
  response_headers_json TEXT,
  request_body TEXT,
  response_body TEXT,
  created_utc TEXT NOT NULL,
  last_update_utc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_request_history_tenant_id ON request_history(tenant_id);
CREATE INDEX IF NOT EXISTS idx_request_history_user_id ON request_history(user_id);
CREATE INDEX IF NOT EXISTS idx_request_history_credential_id ON request_history(credential_id);
CREATE INDEX IF NOT EXISTS idx_request_history_assistant_id ON request_history(assistant_id);
CREATE INDEX IF NOT EXISTS idx_request_history_thread_id ON request_history(thread_id);
CREATE INDEX IF NOT EXISTS idx_request_history_status_code ON request_history(status_code);
CREATE INDEX IF NOT EXISTS idx_request_history_success ON request_history(success);
CREATE INDEX IF NOT EXISTS idx_request_history_created_utc ON request_history(created_utc);
CREATE INDEX IF NOT EXISTS idx_request_history_request_path ON request_history(request_path);

-- ============================================================================
-- PostgreSQL (uncomment to use)
-- ============================================================================
-- CREATE TABLE IF NOT EXISTS request_history (
--   id TEXT PRIMARY KEY,
--   tenant_id TEXT,
--   user_id TEXT,
--   credential_id TEXT,
--   assistant_id TEXT,
--   thread_id TEXT,
--   principal_name TEXT,
--   request_type TEXT NOT NULL DEFAULT 'SystemApi',
--   source_type TEXT NOT NULL DEFAULT 'api',
--   http_method TEXT NOT NULL,
--   route_template TEXT,
--   request_path TEXT NOT NULL,
--   request_url TEXT NOT NULL,
--   source_ip TEXT,
--   status_code INTEGER NOT NULL DEFAULT 0,
--   success BOOLEAN NOT NULL DEFAULT FALSE,
--   duration_ms DOUBLE PRECISION NOT NULL DEFAULT 0,
--   request_content_type TEXT,
--   response_content_type TEXT,
--   request_size_bytes BIGINT NOT NULL DEFAULT 0,
--   response_size_bytes BIGINT NOT NULL DEFAULT 0,
--   request_body_truncated BOOLEAN NOT NULL DEFAULT FALSE,
--   response_body_truncated BOOLEAN NOT NULL DEFAULT FALSE,
--   request_body_is_binary BOOLEAN NOT NULL DEFAULT FALSE,
--   response_body_is_binary BOOLEAN NOT NULL DEFAULT FALSE,
--   route_parameters_json TEXT,
--   query_parameters_json TEXT,
--   request_headers_json TEXT,
--   response_headers_json TEXT,
--   request_body TEXT,
--   response_body TEXT,
--   created_utc TIMESTAMP NOT NULL,
--   last_update_utc TIMESTAMP NOT NULL
-- );
--
-- CREATE INDEX IF NOT EXISTS idx_request_history_tenant_id ON request_history(tenant_id);
-- CREATE INDEX IF NOT EXISTS idx_request_history_user_id ON request_history(user_id);
-- CREATE INDEX IF NOT EXISTS idx_request_history_credential_id ON request_history(credential_id);
-- CREATE INDEX IF NOT EXISTS idx_request_history_assistant_id ON request_history(assistant_id);
-- CREATE INDEX IF NOT EXISTS idx_request_history_thread_id ON request_history(thread_id);
-- CREATE INDEX IF NOT EXISTS idx_request_history_status_code ON request_history(status_code);
-- CREATE INDEX IF NOT EXISTS idx_request_history_success ON request_history(success);
-- CREATE INDEX IF NOT EXISTS idx_request_history_created_utc ON request_history(created_utc);
-- CREATE INDEX IF NOT EXISTS idx_request_history_request_path ON request_history(request_path);

-- ============================================================================
-- MySQL (uncomment to use)
-- ============================================================================
-- CREATE TABLE IF NOT EXISTS request_history (
--   id VARCHAR(64) PRIMARY KEY,
--   tenant_id TEXT,
--   user_id TEXT,
--   credential_id TEXT,
--   assistant_id TEXT,
--   thread_id TEXT,
--   principal_name TEXT,
--   request_type VARCHAR(64) NOT NULL DEFAULT 'SystemApi',
--   source_type VARCHAR(64) NOT NULL DEFAULT 'api',
--   http_method VARCHAR(16) NOT NULL,
--   route_template TEXT,
--   request_path TEXT NOT NULL,
--   request_url TEXT NOT NULL,
--   source_ip TEXT,
--   status_code INT NOT NULL DEFAULT 0,
--   success TINYINT(1) NOT NULL DEFAULT 0,
--   duration_ms DOUBLE NOT NULL DEFAULT 0,
--   request_content_type TEXT,
--   response_content_type TEXT,
--   request_size_bytes BIGINT NOT NULL DEFAULT 0,
--   response_size_bytes BIGINT NOT NULL DEFAULT 0,
--   request_body_truncated TINYINT(1) NOT NULL DEFAULT 0,
--   response_body_truncated TINYINT(1) NOT NULL DEFAULT 0,
--   request_body_is_binary TINYINT(1) NOT NULL DEFAULT 0,
--   response_body_is_binary TINYINT(1) NOT NULL DEFAULT 0,
--   route_parameters_json LONGTEXT,
--   query_parameters_json LONGTEXT,
--   request_headers_json LONGTEXT,
--   response_headers_json LONGTEXT,
--   request_body LONGTEXT,
--   response_body LONGTEXT,
--   created_utc DATETIME NOT NULL,
--   last_update_utc DATETIME NOT NULL,
--   INDEX idx_request_history_tenant_id (tenant_id(255)),
--   INDEX idx_request_history_user_id (user_id(255)),
--   INDEX idx_request_history_credential_id (credential_id(255)),
--   INDEX idx_request_history_assistant_id (assistant_id(255)),
--   INDEX idx_request_history_thread_id (thread_id(255)),
--   INDEX idx_request_history_status_code (status_code),
--   INDEX idx_request_history_success (success),
--   INDEX idx_request_history_created_utc (created_utc),
--   INDEX idx_request_history_request_path (request_path(255))
-- );

-- ============================================================================
-- SQL Server (uncomment to use)
-- ============================================================================
-- IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='request_history' AND xtype='U')
-- CREATE TABLE request_history (
--   id NVARCHAR(64) PRIMARY KEY,
--   tenant_id NVARCHAR(64) NULL,
--   user_id NVARCHAR(64) NULL,
--   credential_id NVARCHAR(64) NULL,
--   assistant_id NVARCHAR(64) NULL,
--   thread_id NVARCHAR(64) NULL,
--   principal_name NVARCHAR(256) NULL,
--   request_type NVARCHAR(64) NOT NULL DEFAULT 'SystemApi',
--   source_type NVARCHAR(64) NOT NULL DEFAULT 'api',
--   http_method NVARCHAR(16) NOT NULL,
--   route_template NVARCHAR(MAX) NULL,
--   request_path NVARCHAR(MAX) NOT NULL,
--   request_url NVARCHAR(MAX) NOT NULL,
--   source_ip NVARCHAR(128) NULL,
--   status_code INT NOT NULL DEFAULT 0,
--   success BIT NOT NULL DEFAULT 0,
--   duration_ms FLOAT NOT NULL DEFAULT 0,
--   request_content_type NVARCHAR(256) NULL,
--   response_content_type NVARCHAR(256) NULL,
--   request_size_bytes BIGINT NOT NULL DEFAULT 0,
--   response_size_bytes BIGINT NOT NULL DEFAULT 0,
--   request_body_truncated BIT NOT NULL DEFAULT 0,
--   response_body_truncated BIT NOT NULL DEFAULT 0,
--   request_body_is_binary BIT NOT NULL DEFAULT 0,
--   response_body_is_binary BIT NOT NULL DEFAULT 0,
--   route_parameters_json NVARCHAR(MAX) NULL,
--   query_parameters_json NVARCHAR(MAX) NULL,
--   request_headers_json NVARCHAR(MAX) NULL,
--   response_headers_json NVARCHAR(MAX) NULL,
--   request_body NVARCHAR(MAX) NULL,
--   response_body NVARCHAR(MAX) NULL,
--   created_utc DATETIME2 NOT NULL,
--   last_update_utc DATETIME2 NOT NULL
-- );
--
-- CREATE INDEX idx_request_history_tenant_id ON request_history(tenant_id);
-- CREATE INDEX idx_request_history_user_id ON request_history(user_id);
-- CREATE INDEX idx_request_history_credential_id ON request_history(credential_id);
-- CREATE INDEX idx_request_history_assistant_id ON request_history(assistant_id);
-- CREATE INDEX idx_request_history_thread_id ON request_history(thread_id);
-- CREATE INDEX idx_request_history_status_code ON request_history(status_code);
-- CREATE INDEX idx_request_history_success ON request_history(success);
-- CREATE INDEX idx_request_history_created_utc ON request_history(created_utc);
-- CREATE INDEX idx_request_history_request_path ON request_history(request_path);
