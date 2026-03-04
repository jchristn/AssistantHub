-- Migration 005: Upgrade existing v0.6.0 database to v0.7.0
-- Adds metadata filtering columns to assistant_settings and chat_history
-- Back up your database before running this migration.

------------------------------------------------------------------------
-- SQLite
------------------------------------------------------------------------
ALTER TABLE assistant_settings ADD COLUMN retrieval_label_filter TEXT;
ALTER TABLE assistant_settings ADD COLUMN retrieval_tag_filter TEXT;

ALTER TABLE chat_history ADD COLUMN metadata_filter TEXT;

------------------------------------------------------------------------
-- PostgreSQL
------------------------------------------------------------------------
-- ALTER TABLE assistant_settings ADD COLUMN retrieval_label_filter TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN retrieval_tag_filter TEXT;

-- ALTER TABLE chat_history ADD COLUMN metadata_filter TEXT;

------------------------------------------------------------------------
-- MySQL
------------------------------------------------------------------------
-- ALTER TABLE assistant_settings ADD COLUMN retrieval_label_filter TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN retrieval_tag_filter TEXT;

-- ALTER TABLE chat_history ADD COLUMN metadata_filter TEXT;

------------------------------------------------------------------------
-- SQL Server
------------------------------------------------------------------------
-- ALTER TABLE assistant_settings ADD retrieval_label_filter NVARCHAR(MAX);
-- ALTER TABLE assistant_settings ADD retrieval_tag_filter NVARCHAR(MAX);

-- ALTER TABLE chat_history ADD metadata_filter NVARCHAR(MAX);
