-- Migration script for AssistantHub v0.13.0
-- MySQL
-- Assistant analytics indexes and performance-event assistant backfill

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

CALL add_assistanthub_column_if_missing('chat_history_performance_events', 'assistant_id', '`assistant_id` VARCHAR(256)');

DROP PROCEDURE add_assistanthub_column_if_missing;

UPDATE `chat_history_performance_events` e
JOIN `chat_history` h ON h.`id` = e.`chat_history_id`
SET e.`assistant_id` = h.`assistant_id`
WHERE e.`assistant_id` IS NULL;

CREATE INDEX idx_assistant_feedback_tenant_assistant_created ON `assistant_feedback` (`tenant_id`, `assistant_id`, `created_utc`(191));
CREATE INDEX idx_request_history_tenant_assistant_created ON `request_history` (`tenant_id`, `assistant_id`, `created_utc`(191));
CREATE INDEX idx_request_history_tenant_assistant_success_created ON `request_history` (`tenant_id`, `assistant_id`, `success`, `created_utc`(191));
CREATE INDEX idx_chpe_assistant_id ON `chat_history_performance_events` (`assistant_id`);
CREATE INDEX idx_chpe_tenant_assistant_created ON `chat_history_performance_events` (`tenant_id`, `assistant_id`, `created_utc`(191));
CREATE INDEX idx_chpe_tenant_assistant_stage_created ON `chat_history_performance_events` (`tenant_id`, `assistant_id`, `stage`, `created_utc`(191));
CREATE INDEX idx_chpe_tenant_assistant_endpoint_created ON `chat_history_performance_events` (`tenant_id`, `assistant_id`, `endpoint_id`, `created_utc`(191));
