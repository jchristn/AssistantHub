-- Migration 007: Add enable_citations column to assistant_settings
-- Run against existing databases to add citation support.
-- Safe to run multiple times; errors on "column already exists" can be ignored.

-- SQLite
ALTER TABLE assistant_settings ADD COLUMN enable_citations INTEGER NOT NULL DEFAULT 0;

-- PostgreSQL
ALTER TABLE assistant_settings ADD COLUMN enable_citations INTEGER NOT NULL DEFAULT 0;

-- SQL Server
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('assistant_settings') AND name = 'enable_citations')
    ALTER TABLE assistant_settings ADD enable_citations BIT NOT NULL DEFAULT 0;

-- MySQL
ALTER TABLE `assistant_settings` ADD COLUMN `enable_citations` TINYINT NOT NULL DEFAULT 0;

-- citation_link_mode: controls document download linking in citations
-- Values: 'None' (default), 'Authenticated', 'Public'

-- SQLite
ALTER TABLE assistant_settings ADD COLUMN citation_link_mode TEXT DEFAULT 'None';

-- PostgreSQL
ALTER TABLE assistant_settings ADD COLUMN citation_link_mode TEXT DEFAULT 'None';

-- SQL Server
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('assistant_settings') AND name = 'citation_link_mode')
    ALTER TABLE assistant_settings ADD citation_link_mode NVARCHAR(32) NOT NULL DEFAULT 'None';

-- MySQL
ALTER TABLE `assistant_settings` ADD COLUMN `citation_link_mode` VARCHAR(32) DEFAULT 'None';
