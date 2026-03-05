-- Migration script for AssistantHub v0.8.0
-- RAG Evaluation support
--
-- This script adds:
--   1. eval_facts table - stores evaluation questions and expected facts per assistant
--   2. eval_runs table - tracks evaluation run execution and results
--   3. eval_results table - stores per-fact evaluation outcomes with verdicts
--   4. eval_judge_prompt column on assistant_settings - custom judge prompt per assistant
--   5. Indexes for efficient querying
--
-- Run this script against your existing database before starting v0.8.0.

-- ============================================================================
-- SQLite
-- ============================================================================

-- Add eval_judge_prompt to assistant_settings
ALTER TABLE assistant_settings ADD COLUMN eval_judge_prompt TEXT;

-- Create eval_facts table
CREATE TABLE IF NOT EXISTS eval_facts (
  id TEXT PRIMARY KEY,
  tenant_id TEXT NOT NULL DEFAULT 'default',
  assistant_id TEXT NOT NULL,
  category TEXT,
  question TEXT,
  expected_facts TEXT,
  created_utc TEXT NOT NULL,
  last_update_utc TEXT NOT NULL
);

-- Create eval_runs table
CREATE TABLE IF NOT EXISTS eval_runs (
  id TEXT PRIMARY KEY,
  tenant_id TEXT NOT NULL DEFAULT 'default',
  assistant_id TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'Pending',
  total_facts INTEGER NOT NULL DEFAULT 0,
  facts_evaluated INTEGER NOT NULL DEFAULT 0,
  facts_passed INTEGER NOT NULL DEFAULT 0,
  facts_failed INTEGER NOT NULL DEFAULT 0,
  pass_rate REAL NOT NULL DEFAULT 0,
  judge_prompt TEXT,
  started_utc TEXT,
  completed_utc TEXT,
  created_utc TEXT NOT NULL
);

-- Create eval_results table
CREATE TABLE IF NOT EXISTS eval_results (
  id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL,
  fact_id TEXT NOT NULL,
  question TEXT,
  expected_facts TEXT,
  llm_response TEXT,
  fact_verdicts TEXT,
  overall_pass INTEGER NOT NULL DEFAULT 0,
  duration_ms INTEGER NOT NULL DEFAULT 0,
  created_utc TEXT NOT NULL
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_eval_facts_tenant_id ON eval_facts(tenant_id);
CREATE INDEX IF NOT EXISTS idx_eval_facts_assistant_id ON eval_facts(assistant_id);
CREATE INDEX IF NOT EXISTS idx_eval_facts_category ON eval_facts(category);
CREATE INDEX IF NOT EXISTS idx_eval_facts_created_utc ON eval_facts(created_utc);
CREATE INDEX IF NOT EXISTS idx_eval_runs_tenant_id ON eval_runs(tenant_id);
CREATE INDEX IF NOT EXISTS idx_eval_runs_assistant_id ON eval_runs(assistant_id);
CREATE INDEX IF NOT EXISTS idx_eval_runs_status ON eval_runs(status);
CREATE INDEX IF NOT EXISTS idx_eval_runs_created_utc ON eval_runs(created_utc);
CREATE INDEX IF NOT EXISTS idx_eval_results_run_id ON eval_results(run_id);
CREATE INDEX IF NOT EXISTS idx_eval_results_fact_id ON eval_results(fact_id);

-- ============================================================================
-- PostgreSQL (uncomment to use)
-- ============================================================================
-- ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS eval_judge_prompt TEXT;
--
-- CREATE TABLE IF NOT EXISTS eval_facts (
--   id TEXT PRIMARY KEY,
--   tenant_id TEXT NOT NULL DEFAULT 'default',
--   assistant_id TEXT NOT NULL,
--   category TEXT,
--   question TEXT,
--   expected_facts TEXT,
--   created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
--   last_update_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
-- );
--
-- CREATE TABLE IF NOT EXISTS eval_runs (
--   id TEXT PRIMARY KEY,
--   tenant_id TEXT NOT NULL DEFAULT 'default',
--   assistant_id TEXT NOT NULL,
--   status TEXT NOT NULL DEFAULT 'Pending',
--   total_facts INTEGER NOT NULL DEFAULT 0,
--   facts_evaluated INTEGER NOT NULL DEFAULT 0,
--   facts_passed INTEGER NOT NULL DEFAULT 0,
--   facts_failed INTEGER NOT NULL DEFAULT 0,
--   pass_rate DOUBLE PRECISION NOT NULL DEFAULT 0,
--   judge_prompt TEXT,
--   started_utc TIMESTAMPTZ,
--   completed_utc TIMESTAMPTZ,
--   created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
-- );
--
-- CREATE TABLE IF NOT EXISTS eval_results (
--   id TEXT PRIMARY KEY,
--   run_id TEXT NOT NULL,
--   fact_id TEXT NOT NULL,
--   question TEXT,
--   expected_facts TEXT,
--   llm_response TEXT,
--   fact_verdicts TEXT,
--   overall_pass BOOLEAN NOT NULL DEFAULT FALSE,
--   duration_ms BIGINT NOT NULL DEFAULT 0,
--   created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
-- );
--
-- CREATE INDEX IF NOT EXISTS idx_eval_facts_tenant_id ON eval_facts(tenant_id);
-- CREATE INDEX IF NOT EXISTS idx_eval_facts_assistant_id ON eval_facts(assistant_id);
-- CREATE INDEX IF NOT EXISTS idx_eval_facts_category ON eval_facts(category);
-- CREATE INDEX IF NOT EXISTS idx_eval_facts_created_utc ON eval_facts(created_utc);
-- CREATE INDEX IF NOT EXISTS idx_eval_runs_tenant_id ON eval_runs(tenant_id);
-- CREATE INDEX IF NOT EXISTS idx_eval_runs_assistant_id ON eval_runs(assistant_id);
-- CREATE INDEX IF NOT EXISTS idx_eval_runs_status ON eval_runs(status);
-- CREATE INDEX IF NOT EXISTS idx_eval_runs_created_utc ON eval_runs(created_utc);
-- CREATE INDEX IF NOT EXISTS idx_eval_results_run_id ON eval_results(run_id);
-- CREATE INDEX IF NOT EXISTS idx_eval_results_fact_id ON eval_results(fact_id);

-- ============================================================================
-- MySQL (uncomment to use)
-- ============================================================================
-- ALTER TABLE assistant_settings ADD COLUMN eval_judge_prompt TEXT;
--
-- CREATE TABLE IF NOT EXISTS eval_facts (
--   id VARCHAR(64) PRIMARY KEY,
--   tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
--   assistant_id VARCHAR(64) NOT NULL,
--   category TEXT,
--   question TEXT,
--   expected_facts TEXT,
--   created_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
--   last_update_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
-- );
--
-- CREATE TABLE IF NOT EXISTS eval_runs (
--   id VARCHAR(64) PRIMARY KEY,
--   tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
--   assistant_id VARCHAR(64) NOT NULL,
--   status VARCHAR(32) NOT NULL DEFAULT 'Pending',
--   total_facts INT NOT NULL DEFAULT 0,
--   facts_evaluated INT NOT NULL DEFAULT 0,
--   facts_passed INT NOT NULL DEFAULT 0,
--   facts_failed INT NOT NULL DEFAULT 0,
--   pass_rate DOUBLE NOT NULL DEFAULT 0,
--   judge_prompt TEXT,
--   started_utc DATETIME,
--   completed_utc DATETIME,
--   created_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
-- );
--
-- CREATE TABLE IF NOT EXISTS eval_results (
--   id VARCHAR(64) PRIMARY KEY,
--   run_id VARCHAR(64) NOT NULL,
--   fact_id VARCHAR(64) NOT NULL,
--   question TEXT,
--   expected_facts TEXT,
--   llm_response TEXT,
--   fact_verdicts TEXT,
--   overall_pass TINYINT NOT NULL DEFAULT 0,
--   duration_ms BIGINT NOT NULL DEFAULT 0,
--   created_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
-- );
--
-- CREATE INDEX idx_eval_facts_tenant_id ON eval_facts(tenant_id);
-- CREATE INDEX idx_eval_facts_assistant_id ON eval_facts(assistant_id);
-- CREATE INDEX idx_eval_facts_category ON eval_facts(category(255));
-- CREATE INDEX idx_eval_facts_created_utc ON eval_facts(created_utc);
-- CREATE INDEX idx_eval_runs_tenant_id ON eval_runs(tenant_id);
-- CREATE INDEX idx_eval_runs_assistant_id ON eval_runs(assistant_id);
-- CREATE INDEX idx_eval_runs_status ON eval_runs(status);
-- CREATE INDEX idx_eval_runs_created_utc ON eval_runs(created_utc);
-- CREATE INDEX idx_eval_results_run_id ON eval_results(run_id);
-- CREATE INDEX idx_eval_results_fact_id ON eval_results(fact_id);

-- ============================================================================
-- SQL Server (uncomment to use)
-- ============================================================================
-- ALTER TABLE assistant_settings ADD eval_judge_prompt NVARCHAR(MAX);
--
-- IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'eval_facts')
-- CREATE TABLE eval_facts (
--   id NVARCHAR(64) PRIMARY KEY,
--   tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
--   assistant_id NVARCHAR(64) NOT NULL,
--   category NVARCHAR(MAX),
--   question NVARCHAR(MAX),
--   expected_facts NVARCHAR(MAX),
--   created_utc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
--   last_update_utc DATETIME2 NOT NULL DEFAULT GETUTCDATE()
-- );
--
-- IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'eval_runs')
-- CREATE TABLE eval_runs (
--   id NVARCHAR(64) PRIMARY KEY,
--   tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
--   assistant_id NVARCHAR(64) NOT NULL,
--   status NVARCHAR(32) NOT NULL DEFAULT 'Pending',
--   total_facts INT NOT NULL DEFAULT 0,
--   facts_evaluated INT NOT NULL DEFAULT 0,
--   facts_passed INT NOT NULL DEFAULT 0,
--   facts_failed INT NOT NULL DEFAULT 0,
--   pass_rate FLOAT NOT NULL DEFAULT 0,
--   judge_prompt NVARCHAR(MAX),
--   started_utc DATETIME2,
--   completed_utc DATETIME2,
--   created_utc DATETIME2 NOT NULL DEFAULT GETUTCDATE()
-- );
--
-- IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'eval_results')
-- CREATE TABLE eval_results (
--   id NVARCHAR(64) PRIMARY KEY,
--   run_id NVARCHAR(64) NOT NULL,
--   fact_id NVARCHAR(64) NOT NULL,
--   question NVARCHAR(MAX),
--   expected_facts NVARCHAR(MAX),
--   llm_response NVARCHAR(MAX),
--   fact_verdicts NVARCHAR(MAX),
--   overall_pass BIT NOT NULL DEFAULT 0,
--   duration_ms BIGINT NOT NULL DEFAULT 0,
--   created_utc DATETIME2 NOT NULL DEFAULT GETUTCDATE()
-- );
--
-- CREATE INDEX idx_eval_facts_tenant_id ON eval_facts(tenant_id);
-- CREATE INDEX idx_eval_facts_assistant_id ON eval_facts(assistant_id);
-- CREATE INDEX idx_eval_facts_created_utc ON eval_facts(created_utc);
-- CREATE INDEX idx_eval_runs_tenant_id ON eval_runs(tenant_id);
-- CREATE INDEX idx_eval_runs_assistant_id ON eval_runs(assistant_id);
-- CREATE INDEX idx_eval_runs_status ON eval_runs(status);
-- CREATE INDEX idx_eval_runs_created_utc ON eval_runs(created_utc);
-- CREATE INDEX idx_eval_results_run_id ON eval_results(run_id);
-- CREATE INDEX idx_eval_results_fact_id ON eval_results(fact_id);
