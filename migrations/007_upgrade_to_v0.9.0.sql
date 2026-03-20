-- Migration script for AssistantHub v0.9.0
-- Slack assistant integration support
--
-- This script adds:
--   1. Slack configuration columns to assistant_settings
--   2. origin column to chat_history for transport observability
--
-- Run this script against your existing database before starting v0.9.0.

-- ============================================================================
-- SQLite
-- ============================================================================

ALTER TABLE assistant_settings ADD COLUMN enable_slack INTEGER NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN slack_app_token TEXT;
ALTER TABLE assistant_settings ADD COLUMN slack_bot_token TEXT;
ALTER TABLE assistant_settings ADD COLUMN slack_channel_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN slack_message_prefix TEXT;

ALTER TABLE chat_history ADD COLUMN origin TEXT;

-- ============================================================================
-- PostgreSQL (uncomment to use)
-- ============================================================================
-- ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS enable_slack BOOLEAN NOT NULL DEFAULT FALSE;
-- ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS slack_app_token TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS slack_bot_token TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS slack_channel_id TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS slack_message_prefix TEXT;
--
-- ALTER TABLE chat_history ADD COLUMN IF NOT EXISTS origin TEXT;

-- ============================================================================
-- MySQL (uncomment to use)
-- ============================================================================
-- ALTER TABLE assistant_settings ADD COLUMN enable_slack TINYINT(1) NOT NULL DEFAULT 0;
-- ALTER TABLE assistant_settings ADD COLUMN slack_app_token TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN slack_bot_token TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN slack_channel_id TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN slack_message_prefix TEXT;
--
-- ALTER TABLE chat_history ADD COLUMN origin VARCHAR(64);

-- ============================================================================
-- SQL Server (uncomment to use)
-- ============================================================================
-- ALTER TABLE assistant_settings ADD enable_slack BIT NOT NULL DEFAULT 0;
-- ALTER TABLE assistant_settings ADD slack_app_token NVARCHAR(MAX);
-- ALTER TABLE assistant_settings ADD slack_bot_token NVARCHAR(MAX);
-- ALTER TABLE assistant_settings ADD slack_channel_id NVARCHAR(MAX);
-- ALTER TABLE assistant_settings ADD slack_message_prefix NVARCHAR(MAX);
--
-- ALTER TABLE chat_history ADD origin NVARCHAR(64);
