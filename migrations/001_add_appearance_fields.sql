-- Migration: Add appearance fields (title, logo_url, favicon_url) to assistant_settings
-- Run this against existing deployments before upgrading to the new version.

-- SQLite
ALTER TABLE assistant_settings ADD COLUMN title TEXT;
ALTER TABLE assistant_settings ADD COLUMN logo_url TEXT;
ALTER TABLE assistant_settings ADD COLUMN favicon_url TEXT;

-- PostgreSQL (same syntax)
-- ALTER TABLE assistant_settings ADD COLUMN title TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN logo_url TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN favicon_url TEXT;

-- SQL Server
-- ALTER TABLE assistant_settings ADD title NVARCHAR(MAX) NULL;
-- ALTER TABLE assistant_settings ADD logo_url NVARCHAR(MAX) NULL;
-- ALTER TABLE assistant_settings ADD favicon_url NVARCHAR(MAX) NULL;

-- MySQL
-- ALTER TABLE assistant_settings ADD COLUMN `title` TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN `logo_url` TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN `favicon_url` TEXT;
