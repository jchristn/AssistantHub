-- Migration 006: Add completion_tokens and tokens-per-second metrics to chat_history
-- These columns support TPS (tokens per second) calculations for both overall and generation-only throughput.

-- SQLite / PostgreSQL:
ALTER TABLE chat_history ADD COLUMN completion_tokens INTEGER DEFAULT 0;
ALTER TABLE chat_history ADD COLUMN tokens_per_second_overall REAL DEFAULT 0;
ALTER TABLE chat_history ADD COLUMN tokens_per_second_generation REAL DEFAULT 0;

-- SQL Server:
-- ALTER TABLE chat_history ADD completion_tokens INT DEFAULT 0;
-- ALTER TABLE chat_history ADD tokens_per_second_overall FLOAT DEFAULT 0;
-- ALTER TABLE chat_history ADD tokens_per_second_generation FLOAT DEFAULT 0;

-- MySQL:
-- ALTER TABLE `chat_history` ADD COLUMN `completion_tokens` INT DEFAULT 0;
-- ALTER TABLE `chat_history` ADD COLUMN `tokens_per_second_overall` DOUBLE DEFAULT 0;
-- ALTER TABLE `chat_history` ADD COLUMN `tokens_per_second_generation` DOUBLE DEFAULT 0;
