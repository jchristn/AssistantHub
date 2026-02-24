-- Migration 008: Add retrieval_include_neighbors column to assistant_settings
-- Enables requesting neighboring chunks around each search match from RecallDB.
-- Safe to run multiple times; errors on "column already exists" can be ignored.

-- SQLite
ALTER TABLE assistant_settings ADD COLUMN retrieval_include_neighbors INTEGER NOT NULL DEFAULT 0;

-- PostgreSQL
ALTER TABLE assistant_settings ADD COLUMN retrieval_include_neighbors INTEGER NOT NULL DEFAULT 0;

-- SQL Server
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('assistant_settings') AND name = 'retrieval_include_neighbors')
    ALTER TABLE assistant_settings ADD retrieval_include_neighbors INT NOT NULL DEFAULT 0;

-- MySQL
ALTER TABLE `assistant_settings` ADD COLUMN `retrieval_include_neighbors` INT NOT NULL DEFAULT 0;
