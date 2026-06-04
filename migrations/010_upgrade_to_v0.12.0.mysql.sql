-- Migration script for AssistantHub v0.12.0
-- MySQL
-- Provider-agnostic assistant performance telemetry

DELIMITER //

CREATE PROCEDURE add_assistanthub_column_if_missing(
    IN table_name_value VARCHAR(64),
    IN column_name_value VARCHAR(64),
    IN column_definition_value TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = table_name_value
          AND COLUMN_NAME = column_name_value
    ) THEN
        SET @add_column_sql = CONCAT('ALTER TABLE `', table_name_value, '` ADD COLUMN ', column_definition_value);
        PREPARE add_column_statement FROM @add_column_sql;
        EXECUTE add_column_statement;
        DEALLOCATE PREPARE add_column_statement;
    END IF;
END//

DELIMITER ;

CALL add_assistanthub_column_if_missing('chat_history', 'trace_id', '`trace_id` VARCHAR(256)');
CALL add_assistanthub_column_if_missing('chat_history', 'request_history_id', '`request_history_id` VARCHAR(256)');
CALL add_assistanthub_column_if_missing('chat_history', 'performance_schema_version', '`performance_schema_version` INT NOT NULL DEFAULT 1');
CALL add_assistanthub_column_if_missing('chat_history', 'performance_json', '`performance_json` LONGTEXT');
CALL add_assistanthub_column_if_missing('request_history', 'trace_id', '`trace_id` VARCHAR(256)');
CALL add_assistanthub_column_if_missing('request_history', 'chat_history_id', '`chat_history_id` VARCHAR(256)');

DROP PROCEDURE add_assistanthub_column_if_missing;

CREATE TABLE IF NOT EXISTS `chat_history_performance_events` (
  `id` VARCHAR(256) NOT NULL,
  `tenant_id` VARCHAR(256) NOT NULL DEFAULT 'default',
  `chat_history_id` VARCHAR(256) NOT NULL,
  `request_history_id` VARCHAR(256),
  `trace_id` VARCHAR(256),
  `sequence_number` INT NOT NULL DEFAULT 0,
  `stage` VARCHAR(128) NOT NULL,
  `phase` VARCHAR(128),
  `kind` VARCHAR(128),
  `endpoint_id` VARCHAR(256),
  `endpoint_name` TEXT,
  `endpoint_type` VARCHAR(128),
  `provider` VARCHAR(128),
  `api_format` VARCHAR(128),
  `model` TEXT,
  `started_utc` TEXT,
  `finished_utc` TEXT,
  `duration_ms` DOUBLE NOT NULL DEFAULT 0,
  `success` TINYINT(1) NOT NULL DEFAULT 1,
  `http_status_code` INT,
  `error_type` TEXT,
  `error_message` LONGTEXT,
  `input_tokens` INT,
  `output_tokens` INT,
  `total_tokens` INT,
  `chunks_input` INT,
  `chunks_output` INT,
  `retrieval_query_count` INT,
  `endpoint_limiter_wait_ms` DOUBLE,
  `request_to_headers_ms` DOUBLE,
  `headers_to_first_token_ms` DOUBLE,
  `first_token_to_last_token_ms` DOUBLE,
  `client_total_ms` DOUBLE,
  `provider_queue_ms` DOUBLE,
  `provider_load_ms` DOUBLE,
  `provider_prompt_eval_ms` DOUBLE,
  `provider_generation_ms` DOUBLE,
  `provider_total_ms` DOUBLE,
  `provider_tokens_per_second` DOUBLE,
  `provider_request_id` TEXT,
  `metadata_json` LONGTEXT,
  `provider_metrics_json` LONGTEXT,
  `provider_raw_json` LONGTEXT,
  `created_utc` TEXT NOT NULL,
  PRIMARY KEY (`id`)
);

CREATE INDEX idx_chat_history_trace_id ON `chat_history` (`trace_id`);
CREATE INDEX idx_chat_history_request_history_id ON `chat_history` (`request_history_id`);
CREATE INDEX idx_request_history_trace_id ON `request_history` (`trace_id`);
CREATE INDEX idx_request_history_chat_history_id ON `request_history` (`chat_history_id`);
CREATE INDEX idx_chat_history_performance_events_chat_history_id ON `chat_history_performance_events` (`chat_history_id`);
CREATE INDEX idx_chat_history_performance_events_request_history_id ON `chat_history_performance_events` (`request_history_id`);
CREATE INDEX idx_chat_history_performance_events_trace_id ON `chat_history_performance_events` (`trace_id`);
CREATE INDEX idx_chat_history_performance_events_stage ON `chat_history_performance_events` (`stage`);
CREATE INDEX idx_chat_history_performance_events_started_utc ON `chat_history_performance_events` (`started_utc`(191));
CREATE INDEX idx_chat_history_performance_events_tenant_id ON `chat_history_performance_events` (`tenant_id`);
CREATE INDEX idx_chat_history_performance_events_endpoint_id ON `chat_history_performance_events` (`endpoint_id`);
CREATE INDEX idx_chat_history_performance_events_provider_model ON `chat_history_performance_events` (`provider`, `model`(191));
CREATE INDEX idx_chat_history_performance_events_created_utc ON `chat_history_performance_events` (`created_utc`(191));
CREATE INDEX idx_chat_history_performance_events_duration_ms ON `chat_history_performance_events` (`duration_ms`);
CREATE INDEX idx_chat_history_performance_events_tenant_created ON `chat_history_performance_events` (`tenant_id`, `created_utc`(191));
