-- Migration: Add ingestion_rules table and new columns to assistant_documents
-- Applies to: All database providers (SQLite, PostgreSQL, SQL Server, MySQL)
-- Note: The application handles these migrations automatically on startup.
--       This file is provided as a reference for manual database administration.

-- =============================================================================
-- 1. Create ingestion_rules table
-- =============================================================================

-- SQLite / PostgreSQL
CREATE TABLE IF NOT EXISTS ingestion_rules (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  description TEXT,
  bucket TEXT NOT NULL,
  collection_name TEXT NOT NULL,
  collection_id TEXT,
  labels_json TEXT,
  tags_json TEXT,
  atomization_json TEXT,
  chunking_json TEXT,
  embedding_json TEXT,
  created_utc TEXT NOT NULL,
  last_update_utc TEXT NOT NULL
);

-- SQL Server
-- IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ingestion_rules')
-- CREATE TABLE ingestion_rules (
--   id NVARCHAR(256) NOT NULL,
--   name NVARCHAR(MAX) NOT NULL,
--   description NVARCHAR(MAX) NULL,
--   bucket NVARCHAR(MAX) NOT NULL,
--   collection_name NVARCHAR(MAX) NOT NULL,
--   collection_id NVARCHAR(MAX) NULL,
--   labels_json NVARCHAR(MAX) NULL,
--   tags_json NVARCHAR(MAX) NULL,
--   atomization_json NVARCHAR(MAX) NULL,
--   chunking_json NVARCHAR(MAX) NULL,
--   embedding_json NVARCHAR(MAX) NULL,
--   created_utc NVARCHAR(64) NOT NULL,
--   last_update_utc NVARCHAR(64) NOT NULL,
--   CONSTRAINT pk_ingestion_rules PRIMARY KEY (id)
-- );

-- MySQL
-- CREATE TABLE IF NOT EXISTS `ingestion_rules` (
--   `id` VARCHAR(256) NOT NULL,
--   `name` TEXT NOT NULL,
--   `description` TEXT,
--   `bucket` TEXT NOT NULL,
--   `collection_name` TEXT NOT NULL,
--   `collection_id` TEXT,
--   `labels_json` TEXT,
--   `tags_json` TEXT,
--   `atomization_json` TEXT,
--   `chunking_json` TEXT,
--   `embedding_json` TEXT,
--   `created_utc` TEXT NOT NULL,
--   `last_update_utc` TEXT NOT NULL,
--   PRIMARY KEY (`id`)
-- );

-- =============================================================================
-- 2. Add new columns to assistant_documents
-- =============================================================================

-- SQLite (no IF NOT EXISTS for ALTER TABLE; errors are swallowed at app level)
ALTER TABLE assistant_documents ADD COLUMN ingestion_rule_id TEXT;
ALTER TABLE assistant_documents ADD COLUMN bucket_name TEXT;
ALTER TABLE assistant_documents ADD COLUMN collection_id TEXT;
ALTER TABLE assistant_documents ADD COLUMN labels_json TEXT;
ALTER TABLE assistant_documents ADD COLUMN tags_json TEXT;
ALTER TABLE assistant_documents ADD COLUMN chunk_record_ids TEXT;

-- PostgreSQL (uses ADD COLUMN IF NOT EXISTS)
-- ALTER TABLE assistant_documents ADD COLUMN IF NOT EXISTS ingestion_rule_id TEXT;
-- ALTER TABLE assistant_documents ADD COLUMN IF NOT EXISTS bucket_name TEXT;
-- ALTER TABLE assistant_documents ADD COLUMN IF NOT EXISTS collection_id TEXT;
-- ALTER TABLE assistant_documents ADD COLUMN IF NOT EXISTS labels_json TEXT;
-- ALTER TABLE assistant_documents ADD COLUMN IF NOT EXISTS tags_json TEXT;
-- ALTER TABLE assistant_documents ADD COLUMN IF NOT EXISTS chunk_record_ids TEXT;

-- =============================================================================
-- 3. Create indices
-- =============================================================================

CREATE INDEX IF NOT EXISTS idx_ingestion_rules_name ON ingestion_rules (name);
CREATE INDEX IF NOT EXISTS idx_assistant_documents_ingestion_rule_id ON assistant_documents (ingestion_rule_id);
