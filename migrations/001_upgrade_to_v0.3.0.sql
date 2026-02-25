-- Migration 001: Upgrade existing v0.2.0 database to v0.3.0
-- This is a BREAKING CHANGE migration for existing deployments.
-- Back up your database before running this migration.
--
-- What this migration does:
--   1. Creates the tenants table
--   2. Adds tenant_id columns to all entity tables
--   3. Adds is_tenant_admin column to users
--   4. Creates all new indices
--   5. Inserts the default tenant record
--   6. Sets tenant_id = 'default' on all existing rows
--   7. Promotes existing admins (is_admin = 1) to tenant admins
--   8. Inserts default admin user, credential, and ingestion rule if they don't exist

------------------------------------------------------------------------
-- SQLite
------------------------------------------------------------------------

-- 1. Create tenants table
CREATE TABLE IF NOT EXISTS tenants (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  active INTEGER NOT NULL DEFAULT 1,
  labels_json TEXT,
  tags_json TEXT,
  created_utc TEXT NOT NULL,
  last_update_utc TEXT NOT NULL
);

-- 2. Add tenant_id columns
ALTER TABLE users ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE users ADD COLUMN is_tenant_admin INTEGER NOT NULL DEFAULT 0;
ALTER TABLE credentials ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE assistants ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE assistant_documents ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE assistant_feedback ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE ingestion_rules ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE chat_history ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';

-- 3. Add other v0.3.0 columns that may be missing
ALTER TABLE chat_history ADD COLUMN completion_tokens INTEGER NOT NULL DEFAULT 0;
ALTER TABLE chat_history ADD COLUMN tokens_per_second_overall REAL NOT NULL DEFAULT 0;
ALTER TABLE chat_history ADD COLUMN tokens_per_second_generation REAL NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN enable_citations INTEGER NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN citation_link_mode TEXT DEFAULT 'None';
ALTER TABLE assistant_settings ADD COLUMN retrieval_include_neighbors INTEGER NOT NULL DEFAULT 0;

-- 3b. Add is_protected columns
ALTER TABLE tenants ADD COLUMN is_protected INTEGER NOT NULL DEFAULT 0;
ALTER TABLE users ADD COLUMN is_protected INTEGER NOT NULL DEFAULT 0;
ALTER TABLE credentials ADD COLUMN is_protected INTEGER NOT NULL DEFAULT 0;

-- 4. Create new indices
CREATE INDEX IF NOT EXISTS idx_tenants_name ON tenants(name);
CREATE INDEX IF NOT EXISTS idx_tenants_created_utc ON tenants(created_utc);
CREATE INDEX IF NOT EXISTS idx_users_tenant_id ON users(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_tenant_email ON users(tenant_id, email);
CREATE INDEX IF NOT EXISTS idx_credentials_tenant_id ON credentials(tenant_id);
CREATE INDEX IF NOT EXISTS idx_assistants_tenant_id ON assistants(tenant_id);
CREATE INDEX IF NOT EXISTS idx_assistant_documents_tenant_id ON assistant_documents(tenant_id);
CREATE INDEX IF NOT EXISTS idx_assistant_feedback_tenant_id ON assistant_feedback(tenant_id);
CREATE INDEX IF NOT EXISTS idx_ingestion_rules_tenant_id ON ingestion_rules(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_ingestion_rules_tenant_name ON ingestion_rules(tenant_id, name);
CREATE INDEX IF NOT EXISTS idx_chat_history_tenant_id ON chat_history(tenant_id);

-- 5. Insert default tenant
INSERT OR IGNORE INTO tenants (id, name, active, is_protected, created_utc, last_update_utc)
VALUES ('default', 'Default Tenant', 1, 1, datetime('now'), datetime('now'));

-- 6. Set tenant_id on all existing rows
UPDATE users SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE credentials SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistants SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistant_documents SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistant_feedback SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE ingestion_rules SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE chat_history SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;

-- 7. Promote existing admins to tenant admins
UPDATE users SET is_tenant_admin = 1 WHERE is_admin = 1 AND is_tenant_admin = 0;

-- 8. Insert default records (will not overwrite existing data due to OR IGNORE)
--    Default admin user (email: admin@assistanthub, password: password)
INSERT OR IGNORE INTO users (id, tenant_id, email, password_sha256, first_name, last_name, is_admin, is_tenant_admin, active, is_protected, created_utc, last_update_utc)
VALUES ('usr_default_admin', 'default', 'admin@assistanthub', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'Admin', 'User', 1, 1, 1, 1, datetime('now'), datetime('now'));

--    Default credential (bearer token: default)
INSERT OR IGNORE INTO credentials (id, tenant_id, user_id, name, bearer_token, active, is_protected, created_utc, last_update_utc)
VALUES ('cred_default_admin', 'default', 'usr_default_admin', 'Default admin credential', 'default', 1, 1, datetime('now'), datetime('now'));

--    Default ingestion rule
INSERT OR IGNORE INTO ingestion_rules (id, tenant_id, name, description, bucket, collection_name, collection_id, chunking_json, embedding_json, created_utc, last_update_utc)
VALUES ('ir_default', 'default', 'Default', 'Default ingestion rule', 'default', 'default', 'default', '{}', '{}', datetime('now'), datetime('now'));

------------------------------------------------------------------------
-- PostgreSQL (uncomment and run instead of the SQLite statements above)
------------------------------------------------------------------------
/*
CREATE TABLE IF NOT EXISTS tenants (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  active INTEGER NOT NULL DEFAULT 1,
  labels_json TEXT,
  tags_json TEXT,
  created_utc TIMESTAMP NOT NULL,
  last_update_utc TIMESTAMP NOT NULL
);

ALTER TABLE users ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE users ADD COLUMN is_tenant_admin INTEGER NOT NULL DEFAULT 0;
ALTER TABLE credentials ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE assistants ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE assistant_documents ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE assistant_feedback ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE ingestion_rules ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE chat_history ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE chat_history ADD COLUMN completion_tokens INTEGER NOT NULL DEFAULT 0;
ALTER TABLE chat_history ADD COLUMN tokens_per_second_overall DOUBLE PRECISION NOT NULL DEFAULT 0;
ALTER TABLE chat_history ADD COLUMN tokens_per_second_generation DOUBLE PRECISION NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN enable_citations INTEGER NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN citation_link_mode TEXT DEFAULT 'None';
ALTER TABLE assistant_settings ADD COLUMN retrieval_include_neighbors INTEGER NOT NULL DEFAULT 0;
ALTER TABLE tenants ADD COLUMN is_protected INTEGER NOT NULL DEFAULT 0;
ALTER TABLE users ADD COLUMN is_protected INTEGER NOT NULL DEFAULT 0;
ALTER TABLE credentials ADD COLUMN is_protected INTEGER NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS idx_tenants_name ON tenants(name);
CREATE INDEX IF NOT EXISTS idx_tenants_created_utc ON tenants(created_utc);
CREATE INDEX IF NOT EXISTS idx_users_tenant_id ON users(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_tenant_email ON users(tenant_id, email);
CREATE INDEX IF NOT EXISTS idx_credentials_tenant_id ON credentials(tenant_id);
CREATE INDEX IF NOT EXISTS idx_assistants_tenant_id ON assistants(tenant_id);
CREATE INDEX IF NOT EXISTS idx_assistant_documents_tenant_id ON assistant_documents(tenant_id);
CREATE INDEX IF NOT EXISTS idx_assistant_feedback_tenant_id ON assistant_feedback(tenant_id);
CREATE INDEX IF NOT EXISTS idx_ingestion_rules_tenant_id ON ingestion_rules(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_ingestion_rules_tenant_name ON ingestion_rules(tenant_id, name);
CREATE INDEX IF NOT EXISTS idx_chat_history_tenant_id ON chat_history(tenant_id);

INSERT INTO tenants (id, name, active, is_protected, created_utc, last_update_utc)
VALUES ('default', 'Default Tenant', TRUE, TRUE, NOW(), NOW()) ON CONFLICT (id) DO NOTHING;

UPDATE users SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE credentials SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistants SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistant_documents SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistant_feedback SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE ingestion_rules SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE chat_history SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;

UPDATE users SET is_tenant_admin = 1 WHERE is_admin = 1 AND is_tenant_admin = 0;

INSERT INTO users (id, tenant_id, email, password_sha256, first_name, last_name, is_admin, is_tenant_admin, active, is_protected, created_utc, last_update_utc)
VALUES ('usr_default_admin', 'default', 'admin@assistanthub', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'Admin', 'User', 1, 1, 1, 1, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO credentials (id, tenant_id, user_id, name, bearer_token, active, is_protected, created_utc, last_update_utc)
VALUES ('cred_default_admin', 'default', 'usr_default_admin', 'Default admin credential', 'default', 1, 1, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO ingestion_rules (id, tenant_id, name, description, bucket, collection_name, collection_id, chunking_json, embedding_json, created_utc, last_update_utc)
VALUES ('ir_default', 'default', 'Default', 'Default ingestion rule', 'default', 'default', 'default', '{}', '{}', NOW(), NOW())
ON CONFLICT (id) DO NOTHING;
*/

------------------------------------------------------------------------
-- SQL Server (uncomment and run instead of the SQLite statements above)
------------------------------------------------------------------------
/*
CREATE TABLE tenants (
  id NVARCHAR(256) PRIMARY KEY,
  name NVARCHAR(256) NOT NULL,
  active INT NOT NULL DEFAULT 1,
  labels_json NVARCHAR(MAX),
  tags_json NVARCHAR(MAX),
  created_utc DATETIME2 NOT NULL,
  last_update_utc DATETIME2 NOT NULL
);

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'tenant_id')
  ALTER TABLE users ADD tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'is_tenant_admin')
  ALTER TABLE users ADD is_tenant_admin INT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('credentials') AND name = 'tenant_id')
  ALTER TABLE credentials ADD tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('assistants') AND name = 'tenant_id')
  ALTER TABLE assistants ADD tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('assistant_documents') AND name = 'tenant_id')
  ALTER TABLE assistant_documents ADD tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('assistant_feedback') AND name = 'tenant_id')
  ALTER TABLE assistant_feedback ADD tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ingestion_rules') AND name = 'tenant_id')
  ALTER TABLE ingestion_rules ADD tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('chat_history') AND name = 'tenant_id')
  ALTER TABLE chat_history ADD tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('chat_history') AND name = 'completion_tokens')
  ALTER TABLE chat_history ADD completion_tokens INT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('chat_history') AND name = 'tokens_per_second_overall')
  ALTER TABLE chat_history ADD tokens_per_second_overall FLOAT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('chat_history') AND name = 'tokens_per_second_generation')
  ALTER TABLE chat_history ADD tokens_per_second_generation FLOAT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('assistant_settings') AND name = 'enable_citations')
  ALTER TABLE assistant_settings ADD enable_citations BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('assistant_settings') AND name = 'citation_link_mode')
  ALTER TABLE assistant_settings ADD citation_link_mode NVARCHAR(32) NOT NULL DEFAULT 'None';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('assistant_settings') AND name = 'retrieval_include_neighbors')
  ALTER TABLE assistant_settings ADD retrieval_include_neighbors INT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tenants') AND name = 'is_protected')
  ALTER TABLE tenants ADD is_protected INT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'is_protected')
  ALTER TABLE users ADD is_protected INT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('credentials') AND name = 'is_protected')
  ALTER TABLE credentials ADD is_protected INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM tenants WHERE id = 'default')
  INSERT INTO tenants (id, name, active, is_protected, created_utc, last_update_utc)
  VALUES ('default', 'Default Tenant', 1, 1, GETUTCDATE(), GETUTCDATE());

UPDATE users SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE credentials SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistants SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistant_documents SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistant_feedback SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE ingestion_rules SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE chat_history SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;

UPDATE users SET is_tenant_admin = 1 WHERE is_admin = 1 AND is_tenant_admin = 0;

IF NOT EXISTS (SELECT 1 FROM users WHERE id = 'usr_default_admin')
  INSERT INTO users (id, tenant_id, email, password_sha256, first_name, last_name, is_admin, is_tenant_admin, active, is_protected, created_utc, last_update_utc)
  VALUES ('usr_default_admin', 'default', 'admin@assistanthub', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'Admin', 'User', 1, 1, 1, 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM credentials WHERE id = 'cred_default_admin')
  INSERT INTO credentials (id, tenant_id, user_id, name, bearer_token, active, is_protected, created_utc, last_update_utc)
  VALUES ('cred_default_admin', 'default', 'usr_default_admin', 'Default admin credential', 'default', 1, 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM ingestion_rules WHERE id = 'ir_default')
  INSERT INTO ingestion_rules (id, tenant_id, name, description, bucket, collection_name, collection_id, chunking_json, embedding_json, created_utc, last_update_utc)
  VALUES ('ir_default', 'default', 'Default', 'Default ingestion rule', 'default', 'default', 'default', '{}', '{}', GETUTCDATE(), GETUTCDATE());
*/

------------------------------------------------------------------------
-- MySQL (uncomment and run instead of the SQLite statements above)
------------------------------------------------------------------------
/*
CREATE TABLE IF NOT EXISTS `tenants` (
  `id` VARCHAR(256) PRIMARY KEY,
  `name` VARCHAR(256) NOT NULL,
  `active` TINYINT NOT NULL DEFAULT 1,
  `labels_json` TEXT,
  `tags_json` TEXT,
  `created_utc` DATETIME NOT NULL,
  `last_update_utc` DATETIME NOT NULL
);

ALTER TABLE `users` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `users` ADD COLUMN `is_tenant_admin` TINYINT NOT NULL DEFAULT 0;
ALTER TABLE `credentials` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `assistants` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `assistant_documents` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `assistant_feedback` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `ingestion_rules` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `chat_history` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `chat_history` ADD COLUMN `completion_tokens` INT NOT NULL DEFAULT 0;
ALTER TABLE `chat_history` ADD COLUMN `tokens_per_second_overall` DOUBLE NOT NULL DEFAULT 0;
ALTER TABLE `chat_history` ADD COLUMN `tokens_per_second_generation` DOUBLE NOT NULL DEFAULT 0;
ALTER TABLE `assistant_settings` ADD COLUMN `enable_citations` TINYINT NOT NULL DEFAULT 0;
ALTER TABLE `assistant_settings` ADD COLUMN `citation_link_mode` VARCHAR(32) DEFAULT 'None';
ALTER TABLE `assistant_settings` ADD COLUMN `retrieval_include_neighbors` INT NOT NULL DEFAULT 0;
ALTER TABLE `tenants` ADD COLUMN `is_protected` INT NOT NULL DEFAULT 0;
ALTER TABLE `users` ADD COLUMN `is_protected` INT NOT NULL DEFAULT 0;
ALTER TABLE `credentials` ADD COLUMN `is_protected` INT NOT NULL DEFAULT 0;

INSERT IGNORE INTO `tenants` (`id`, `name`, `active`, `is_protected`, `created_utc`, `last_update_utc`)
VALUES ('default', 'Default Tenant', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

UPDATE `users` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;
UPDATE `credentials` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;
UPDATE `assistants` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;
UPDATE `assistant_documents` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;
UPDATE `assistant_feedback` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;
UPDATE `ingestion_rules` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;
UPDATE `chat_history` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;

UPDATE `users` SET `is_tenant_admin` = 1 WHERE `is_admin` = 1 AND `is_tenant_admin` = 0;

INSERT IGNORE INTO `users` (`id`, `tenant_id`, `email`, `password_sha256`, `first_name`, `last_name`, `is_admin`, `is_tenant_admin`, `active`, `is_protected`, `created_utc`, `last_update_utc`)
VALUES ('usr_default_admin', 'default', 'admin@assistanthub', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'Admin', 'User', 1, 1, 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

INSERT IGNORE INTO `credentials` (`id`, `tenant_id`, `user_id`, `name`, `bearer_token`, `active`, `is_protected`, `created_utc`, `last_update_utc`)
VALUES ('cred_default_admin', 'default', 'usr_default_admin', 'Default admin credential', 'default', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

INSERT IGNORE INTO `ingestion_rules` (`id`, `tenant_id`, `name`, `description`, `bucket`, `collection_name`, `collection_id`, `chunking_json`, `embedding_json`, `created_utc`, `last_update_utc`)
VALUES ('ir_default', 'default', 'Default', 'Default ingestion rule', 'default', 'default', 'default', '{}', '{}', UTC_TIMESTAMP(), UTC_TIMESTAMP());
*/
