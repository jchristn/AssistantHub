-- Migration: Add summarization_json column to ingestion_rules table
-- Applies to: All database providers (SQLite, PostgreSQL, SQL Server, MySQL)
-- Note: The application handles these migrations automatically on startup.
--       This file is provided as a reference for manual database administration.

-- =============================================================================
-- 1. Add summarization_json column to ingestion_rules
-- =============================================================================

-- SQLite (errors are swallowed at app level if column already exists)
ALTER TABLE ingestion_rules ADD COLUMN summarization_json TEXT;

-- PostgreSQL
-- ALTER TABLE ingestion_rules ADD COLUMN IF NOT EXISTS summarization_json TEXT;

-- SQL Server
-- IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ingestion_rules') AND name = 'summarization_json')
--   ALTER TABLE ingestion_rules ADD summarization_json NVARCHAR(MAX) NULL;

-- MySQL
-- ALTER TABLE `ingestion_rules` ADD COLUMN `summarization_json` TEXT;
