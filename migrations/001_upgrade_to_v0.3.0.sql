-- Migration 001: Upgrade existing v0.2.0 database to v0.3.0
-- This is a BREAKING CHANGE migration for existing deployments.
-- Back up your database before running this migration.
--
-- What this migration does:
--   1. Creates the tenants table (with is_protected column)
--   2. Adds tenant_id, is_tenant_admin, and is_protected columns to entity tables
--   3. Creates all new indices for tenant isolation
--   4. Inserts the default tenant record
--   5. Sets tenant_id = 'default' on all existing rows
--   6. Promotes existing admins (is_admin = 1) to tenant admins
--   7. Inserts default admin user, credential, and ingestion rule if they don't exist

------------------------------------------------------------------------
-- SQLite
------------------------------------------------------------------------

-- 1. Create tenants table
CREATE TABLE IF NOT EXISTS tenants (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  active INTEGER NOT NULL DEFAULT 1,
  is_protected INTEGER NOT NULL DEFAULT 0,
  labels_json TEXT,
  tags_json TEXT,
  created_utc TEXT NOT NULL,
  last_update_utc TEXT NOT NULL
);

-- 2. Add new columns to existing tables
ALTER TABLE users ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE users ADD COLUMN is_tenant_admin INTEGER NOT NULL DEFAULT 0;
ALTER TABLE users ADD COLUMN is_protected INTEGER NOT NULL DEFAULT 0;
ALTER TABLE credentials ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE credentials ADD COLUMN is_protected INTEGER NOT NULL DEFAULT 0;
ALTER TABLE assistants ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE assistant_documents ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE assistant_feedback ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE ingestion_rules ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE chat_history ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';

-- 3. Create new indices
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

-- 4. Insert default tenant
INSERT OR IGNORE INTO tenants (id, name, active, is_protected, created_utc, last_update_utc)
VALUES ('default', 'Default Tenant', 1, 1, datetime('now'), datetime('now'));

-- 5. Set tenant_id on all existing rows
UPDATE users SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE credentials SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistants SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistant_documents SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistant_feedback SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE ingestion_rules SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE chat_history SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;

-- 6. Promote existing admins to tenant admins
UPDATE users SET is_tenant_admin = 1 WHERE is_admin = 1 AND is_tenant_admin = 0;

-- 7. Insert default records (will not overwrite existing data due to OR IGNORE)
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
-- 1. Create tenants table
CREATE TABLE IF NOT EXISTS tenants (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  active INTEGER NOT NULL DEFAULT 1,
  is_protected INTEGER NOT NULL DEFAULT 0,
  labels_json TEXT,
  tags_json TEXT,
  created_utc TEXT NOT NULL,
  last_update_utc TEXT NOT NULL
);

-- 2. Add new columns to existing tables
ALTER TABLE users ADD COLUMN IF NOT EXISTS tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE users ADD COLUMN IF NOT EXISTS is_tenant_admin INTEGER NOT NULL DEFAULT 0;
ALTER TABLE users ADD COLUMN IF NOT EXISTS is_protected INTEGER NOT NULL DEFAULT 0;
ALTER TABLE credentials ADD COLUMN IF NOT EXISTS tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE credentials ADD COLUMN IF NOT EXISTS is_protected INTEGER NOT NULL DEFAULT 0;
ALTER TABLE assistants ADD COLUMN IF NOT EXISTS tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE assistant_documents ADD COLUMN IF NOT EXISTS tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE assistant_feedback ADD COLUMN IF NOT EXISTS tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE ingestion_rules ADD COLUMN IF NOT EXISTS tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE chat_history ADD COLUMN IF NOT EXISTS tenant_id TEXT NOT NULL DEFAULT 'default';

-- 3. Create new indices
CREATE INDEX IF NOT EXISTS idx_tenants_name ON tenants(name);
CREATE INDEX IF NOT EXISTS idx_tenants_created_utc ON tenants(created_utc);
CREATE INDEX IF NOT EXISTS idx_users_tenant_id ON users(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_tenant_email ON users(tenant_id, email);
CREATE INDEX IF NOT EXISTS idx_credentials_tenant_id ON credentials(tenant_id);
CREATE INDEX IF NOT EXISTS idx_assistants_tenant_id ON assistants(tenant_id);
CREATE INDEX IF NOT EXISTS idx_assistant_documents_tenant_id ON assistant_documents(tenant_id);
CREATE INDEX IF NOT EXISTS idx_assistant_feedback_tenant_id ON assistant_feedback(tenant_id);
CREATE INDEX IF NOT EXISTS idx_ingestion_rules_tenant_id ON ingestion_rules(tenant_id);
CREATE INDEX IF NOT EXISTS idx_chat_history_tenant_id ON chat_history(tenant_id);

-- 4. Insert default tenant
INSERT INTO tenants (id, name, active, is_protected, created_utc, last_update_utc)
VALUES ('default', 'Default Tenant', 1, 1, NOW()::TEXT, NOW()::TEXT)
ON CONFLICT (id) DO NOTHING;

-- 5. Set tenant_id on all existing rows
UPDATE users SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE credentials SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistants SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistant_documents SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistant_feedback SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE ingestion_rules SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE chat_history SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;

-- 6. Promote existing admins to tenant admins
UPDATE users SET is_tenant_admin = 1 WHERE is_admin = 1 AND is_tenant_admin = 0;

-- 7. Insert default records
INSERT INTO users (id, tenant_id, email, password_sha256, first_name, last_name, is_admin, is_tenant_admin, active, is_protected, created_utc, last_update_utc)
VALUES ('usr_default_admin', 'default', 'admin@assistanthub', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'Admin', 'User', 1, 1, 1, 1, NOW()::TEXT, NOW()::TEXT)
ON CONFLICT (id) DO NOTHING;

INSERT INTO credentials (id, tenant_id, user_id, name, bearer_token, active, is_protected, created_utc, last_update_utc)
VALUES ('cred_default_admin', 'default', 'usr_default_admin', 'Default admin credential', 'default', 1, 1, NOW()::TEXT, NOW()::TEXT)
ON CONFLICT (id) DO NOTHING;

INSERT INTO ingestion_rules (id, tenant_id, name, description, bucket, collection_name, collection_id, chunking_json, embedding_json, created_utc, last_update_utc)
VALUES ('ir_default', 'default', 'Default', 'Default ingestion rule', 'default', 'default', 'default', '{}', '{}', NOW()::TEXT, NOW()::TEXT)
ON CONFLICT (id) DO NOTHING;
*/

------------------------------------------------------------------------
-- SQL Server (uncomment and run instead of the SQLite statements above)
------------------------------------------------------------------------
/*
-- 1. Create tenants table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tenants')
CREATE TABLE tenants (
  id NVARCHAR(256) NOT NULL PRIMARY KEY,
  name NVARCHAR(256) NOT NULL,
  active INT NOT NULL DEFAULT 1,
  is_protected INT NOT NULL DEFAULT 0,
  labels_json NVARCHAR(MAX) NULL,
  tags_json NVARCHAR(MAX) NULL,
  created_utc NVARCHAR(64) NOT NULL,
  last_update_utc NVARCHAR(64) NOT NULL
);

-- 2. Add new columns to existing tables
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'tenant_id')
  ALTER TABLE users ADD tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'is_tenant_admin')
  ALTER TABLE users ADD is_tenant_admin INT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'is_protected')
  ALTER TABLE users ADD is_protected INT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('credentials') AND name = 'tenant_id')
  ALTER TABLE credentials ADD tenant_id NVARCHAR(256) NOT NULL DEFAULT 'default';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('credentials') AND name = 'is_protected')
  ALTER TABLE credentials ADD is_protected INT NOT NULL DEFAULT 0;
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

-- 3. Create new indices
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_tenants_name')
  CREATE INDEX idx_tenants_name ON tenants(name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_tenants_created_utc')
  CREATE INDEX idx_tenants_created_utc ON tenants(created_utc);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_users_tenant_id')
  CREATE INDEX idx_users_tenant_id ON users(tenant_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_users_tenant_email')
  CREATE UNIQUE INDEX idx_users_tenant_email ON users(tenant_id, email);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_credentials_tenant_id')
  CREATE INDEX idx_credentials_tenant_id ON credentials(tenant_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_assistants_tenant_id')
  CREATE INDEX idx_assistants_tenant_id ON assistants(tenant_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_assistant_documents_tenant_id')
  CREATE INDEX idx_assistant_documents_tenant_id ON assistant_documents(tenant_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_assistant_feedback_tenant_id')
  CREATE INDEX idx_assistant_feedback_tenant_id ON assistant_feedback(tenant_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_ingestion_rules_tenant_id')
  CREATE INDEX idx_ingestion_rules_tenant_id ON ingestion_rules(tenant_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_chat_history_tenant_id')
  CREATE INDEX idx_chat_history_tenant_id ON chat_history(tenant_id);

-- 4. Insert default tenant
IF NOT EXISTS (SELECT 1 FROM tenants WHERE id = 'default')
  INSERT INTO tenants (id, name, active, is_protected, created_utc, last_update_utc)
  VALUES ('default', 'Default Tenant', 1, 1, CONVERT(NVARCHAR(64), GETUTCDATE(), 126), CONVERT(NVARCHAR(64), GETUTCDATE(), 126));

-- 5. Set tenant_id on all existing rows
UPDATE users SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE credentials SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistants SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistant_documents SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE assistant_feedback SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE ingestion_rules SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;
UPDATE chat_history SET tenant_id = 'default' WHERE tenant_id = '' OR tenant_id IS NULL;

-- 6. Promote existing admins to tenant admins
UPDATE users SET is_tenant_admin = 1 WHERE is_admin = 1 AND is_tenant_admin = 0;

-- 7. Insert default records
IF NOT EXISTS (SELECT 1 FROM users WHERE id = 'usr_default_admin')
  INSERT INTO users (id, tenant_id, email, password_sha256, first_name, last_name, is_admin, is_tenant_admin, active, is_protected, created_utc, last_update_utc)
  VALUES ('usr_default_admin', 'default', 'admin@assistanthub', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'Admin', 'User', 1, 1, 1, 1, CONVERT(NVARCHAR(64), GETUTCDATE(), 126), CONVERT(NVARCHAR(64), GETUTCDATE(), 126));

IF NOT EXISTS (SELECT 1 FROM credentials WHERE id = 'cred_default_admin')
  INSERT INTO credentials (id, tenant_id, user_id, name, bearer_token, active, is_protected, created_utc, last_update_utc)
  VALUES ('cred_default_admin', 'default', 'usr_default_admin', 'Default admin credential', 'default', 1, 1, CONVERT(NVARCHAR(64), GETUTCDATE(), 126), CONVERT(NVARCHAR(64), GETUTCDATE(), 126));

IF NOT EXISTS (SELECT 1 FROM ingestion_rules WHERE id = 'ir_default')
  INSERT INTO ingestion_rules (id, tenant_id, name, description, bucket, collection_name, collection_id, chunking_json, embedding_json, created_utc, last_update_utc)
  VALUES ('ir_default', 'default', 'Default', 'Default ingestion rule', 'default', 'default', 'default', '{}', '{}', CONVERT(NVARCHAR(64), GETUTCDATE(), 126), CONVERT(NVARCHAR(64), GETUTCDATE(), 126));
*/

------------------------------------------------------------------------
-- MySQL (uncomment and run instead of the SQLite statements above)
------------------------------------------------------------------------
/*
-- 1. Create tenants table
CREATE TABLE IF NOT EXISTS `tenants` (
  `id` VARCHAR(256) NOT NULL,
  `name` VARCHAR(256) NOT NULL,
  `active` INT NOT NULL DEFAULT 1,
  `is_protected` INT NOT NULL DEFAULT 0,
  `labels_json` TEXT,
  `tags_json` TEXT,
  `created_utc` TEXT NOT NULL,
  `last_update_utc` TEXT NOT NULL,
  PRIMARY KEY (`id`)
);

-- 2. Add new columns to existing tables
--    Note: MySQL does not support IF NOT EXISTS for ALTER TABLE ADD COLUMN.
--    If a column already exists, the statement will error. Skip any that fail.
ALTER TABLE `users` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `users` ADD COLUMN `is_tenant_admin` TINYINT NOT NULL DEFAULT 0;
ALTER TABLE `users` ADD COLUMN `is_protected` INT NOT NULL DEFAULT 0;
ALTER TABLE `credentials` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `credentials` ADD COLUMN `is_protected` INT NOT NULL DEFAULT 0;
ALTER TABLE `assistants` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `assistant_documents` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `assistant_feedback` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `ingestion_rules` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';
ALTER TABLE `chat_history` ADD COLUMN `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default';

-- 3. Create new indices
--    Note: VARCHAR(256) columns do not need prefix lengths.
--    TEXT columns need (191) prefix for indexing in MySQL.
CREATE INDEX idx_tenants_name ON `tenants`(`name`);
CREATE INDEX idx_tenants_created_utc ON `tenants`(`created_utc`(191));
CREATE INDEX idx_users_tenant_id ON `users`(`tenant_id`);
CREATE UNIQUE INDEX idx_users_tenant_email ON `users`(`tenant_id`, `email`);
CREATE INDEX idx_credentials_tenant_id ON `credentials`(`tenant_id`);
CREATE INDEX idx_assistants_tenant_id ON `assistants`(`tenant_id`);
CREATE INDEX idx_assistant_documents_tenant_id ON `assistant_documents`(`tenant_id`);
CREATE INDEX idx_assistant_feedback_tenant_id ON `assistant_feedback`(`tenant_id`);
CREATE INDEX idx_ingestion_rules_tenant_id ON `ingestion_rules`(`tenant_id`);
CREATE INDEX idx_chat_history_tenant_id ON `chat_history`(`tenant_id`);

-- 4. Insert default tenant
INSERT IGNORE INTO `tenants` (`id`, `name`, `active`, `is_protected`, `created_utc`, `last_update_utc`)
VALUES ('default', 'Default Tenant', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- 5. Set tenant_id on all existing rows
UPDATE `users` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;
UPDATE `credentials` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;
UPDATE `assistants` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;
UPDATE `assistant_documents` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;
UPDATE `assistant_feedback` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;
UPDATE `ingestion_rules` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;
UPDATE `chat_history` SET `tenant_id` = 'default' WHERE `tenant_id` = '' OR `tenant_id` IS NULL;

-- 6. Promote existing admins to tenant admins
UPDATE `users` SET `is_tenant_admin` = 1 WHERE `is_admin` = 1 AND `is_tenant_admin` = 0;

-- 7. Insert default records
INSERT IGNORE INTO `users` (`id`, `tenant_id`, `email`, `password_sha256`, `first_name`, `last_name`, `is_admin`, `is_tenant_admin`, `active`, `is_protected`, `created_utc`, `last_update_utc`)
VALUES ('usr_default_admin', 'default', 'admin@assistanthub', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'Admin', 'User', 1, 1, 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

INSERT IGNORE INTO `credentials` (`id`, `tenant_id`, `user_id`, `name`, `bearer_token`, `active`, `is_protected`, `created_utc`, `last_update_utc`)
VALUES ('cred_default_admin', 'default', 'usr_default_admin', 'Default admin credential', 'default', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

INSERT IGNORE INTO `ingestion_rules` (`id`, `tenant_id`, `name`, `description`, `bucket`, `collection_name`, `collection_id`, `chunking_json`, `embedding_json`, `created_utc`, `last_update_utc`)
VALUES ('ir_default', 'default', 'Default', 'Default ingestion rule', 'default', 'default', 'default', '{}', '{}', UTC_TIMESTAMP(), UTC_TIMESTAMP());
*/
