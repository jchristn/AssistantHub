-- Migration: Add chat_history table for tracking conversation history with timing metrics
-- Applies to: All database providers (SQLite, PostgreSQL, SQL Server, MySQL)
-- Note: The application handles these migrations automatically on startup.
--       This file is provided as a reference for manual database administration.

-- =============================================================================
-- 1. Create chat_history table
-- =============================================================================

-- SQLite / PostgreSQL
CREATE TABLE IF NOT EXISTS chat_history (
  id TEXT PRIMARY KEY,
  thread_id TEXT NOT NULL,
  assistant_id TEXT NOT NULL,
  collection_id TEXT,
  user_message_utc TEXT,
  user_message TEXT,
  retrieval_start_utc TEXT,
  retrieval_duration_ms REAL,
  retrieval_context TEXT,
  prompt_sent_utc TEXT,
  prompt_tokens INTEGER DEFAULT 0,
  time_to_first_token_ms REAL,
  time_to_last_token_ms REAL,
  assistant_response TEXT,
  created_utc TEXT NOT NULL,
  last_update_utc TEXT NOT NULL
);

-- SQL Server
-- IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'chat_history')
-- CREATE TABLE chat_history (
--   id NVARCHAR(256) NOT NULL,
--   thread_id NVARCHAR(256) NOT NULL,
--   assistant_id NVARCHAR(256) NOT NULL,
--   collection_id NVARCHAR(256) NULL,
--   user_message_utc NVARCHAR(64) NULL,
--   user_message NVARCHAR(MAX) NULL,
--   retrieval_start_utc NVARCHAR(64) NULL,
--   retrieval_duration_ms FLOAT NULL,
--   retrieval_context NVARCHAR(MAX) NULL,
--   prompt_sent_utc NVARCHAR(64) NULL,
--   prompt_tokens INT DEFAULT 0,
--   time_to_first_token_ms FLOAT NULL,
--   time_to_last_token_ms FLOAT NULL,
--   assistant_response NVARCHAR(MAX) NULL,
--   created_utc NVARCHAR(64) NOT NULL,
--   last_update_utc NVARCHAR(64) NOT NULL,
--   CONSTRAINT pk_chat_history PRIMARY KEY (id)
-- );

-- MySQL
-- CREATE TABLE IF NOT EXISTS `chat_history` (
--   `id` VARCHAR(256) NOT NULL,
--   `thread_id` VARCHAR(256) NOT NULL,
--   `assistant_id` VARCHAR(256) NOT NULL,
--   `collection_id` VARCHAR(256) NULL,
--   `user_message_utc` TEXT NULL,
--   `user_message` LONGTEXT NULL,
--   `retrieval_start_utc` TEXT NULL,
--   `retrieval_duration_ms` DOUBLE NULL,
--   `retrieval_context` LONGTEXT NULL,
--   `prompt_sent_utc` TEXT NULL,
--   `prompt_tokens` INT DEFAULT 0,
--   `time_to_first_token_ms` DOUBLE NULL,
--   `time_to_last_token_ms` DOUBLE NULL,
--   `assistant_response` LONGTEXT NULL,
--   `created_utc` TEXT NOT NULL,
--   `last_update_utc` TEXT NOT NULL,
--   PRIMARY KEY (`id`)
-- );

-- =============================================================================
-- 2. Create indices
-- =============================================================================

CREATE INDEX IF NOT EXISTS idx_chat_history_assistant_id ON chat_history (assistant_id);
CREATE INDEX IF NOT EXISTS idx_chat_history_thread_id ON chat_history (thread_id);
CREATE INDEX IF NOT EXISTS idx_chat_history_created_utc ON chat_history (created_utc);
