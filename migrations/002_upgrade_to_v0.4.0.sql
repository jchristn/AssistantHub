-- Migration 002: Upgrade existing v0.3.0 database to v0.4.0
-- Adds query rewrite columns to assistant_settings and chat_history.
-- Back up your database before running this migration.
--
-- What this migration does:
--   1. Adds enable_query_rewrite and query_rewrite_prompt to assistant_settings
--   2. Adds query_rewrite_result and query_rewrite_duration_ms to chat_history

------------------------------------------------------------------------
-- SQLite
------------------------------------------------------------------------

ALTER TABLE assistant_settings ADD COLUMN enable_query_rewrite INTEGER NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN query_rewrite_prompt TEXT;

ALTER TABLE chat_history ADD COLUMN query_rewrite_result TEXT;
ALTER TABLE chat_history ADD COLUMN query_rewrite_duration_ms REAL NOT NULL DEFAULT 0;

------------------------------------------------------------------------
-- PostgreSQL
------------------------------------------------------------------------

-- ALTER TABLE assistant_settings ADD COLUMN enable_query_rewrite BOOLEAN NOT NULL DEFAULT FALSE;
-- ALTER TABLE assistant_settings ADD COLUMN query_rewrite_prompt TEXT;

-- ALTER TABLE chat_history ADD COLUMN query_rewrite_result TEXT;
-- ALTER TABLE chat_history ADD COLUMN query_rewrite_duration_ms DOUBLE PRECISION NOT NULL DEFAULT 0;

------------------------------------------------------------------------
-- MySQL
------------------------------------------------------------------------

-- ALTER TABLE assistant_settings ADD COLUMN enable_query_rewrite TINYINT(1) NOT NULL DEFAULT 0;
-- ALTER TABLE assistant_settings ADD COLUMN query_rewrite_prompt TEXT;

-- ALTER TABLE chat_history ADD COLUMN query_rewrite_result TEXT;
-- ALTER TABLE chat_history ADD COLUMN query_rewrite_duration_ms DOUBLE NOT NULL DEFAULT 0;

------------------------------------------------------------------------
-- SQL Server
------------------------------------------------------------------------

-- ALTER TABLE assistant_settings ADD enable_query_rewrite BIT NOT NULL DEFAULT 0;
-- ALTER TABLE assistant_settings ADD query_rewrite_prompt NVARCHAR(MAX) NULL;

-- ALTER TABLE chat_history ADD query_rewrite_result NVARCHAR(MAX) NULL;
-- ALTER TABLE chat_history ADD query_rewrite_duration_ms FLOAT NOT NULL DEFAULT 0;
