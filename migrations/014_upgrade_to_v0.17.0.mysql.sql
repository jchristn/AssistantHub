-- Migration script for AssistantHub v0.17.0
-- MySQL
-- Retrieval telemetry and answerability settings

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

CALL add_assistanthub_column_if_missing('assistant_settings', 'enable_answerability_check', '`enable_answerability_check` TINYINT(1) NOT NULL DEFAULT 0');
CALL add_assistanthub_column_if_missing('assistant_settings', 'answerability_inference_endpoint_id', '`answerability_inference_endpoint_id` TEXT');
CALL add_assistanthub_column_if_missing('assistant_settings', 'answerability_mode', '`answerability_mode` VARCHAR(32) DEFAULT ''LogOnly''');
CALL add_assistanthub_column_if_missing('assistant_settings', 'answerability_prompt', '`answerability_prompt` TEXT');

CALL add_assistanthub_column_if_missing('chat_history', 'query_class', '`query_class` VARCHAR(64)');
CALL add_assistanthub_column_if_missing('chat_history', 'answerability_decision', '`answerability_decision` VARCHAR(64)');
CALL add_assistanthub_column_if_missing('chat_history', 'answerability_reason', '`answerability_reason` TEXT');
CALL add_assistanthub_column_if_missing('chat_history', 'dropped_candidate_count', '`dropped_candidate_count` INT');
CALL add_assistanthub_column_if_missing('chat_history', 'dropped_candidate_summary_json', '`dropped_candidate_summary_json` LONGTEXT');
CALL add_assistanthub_column_if_missing('chat_history', 'final_citation_count', '`final_citation_count` INT');

DROP PROCEDURE add_assistanthub_column_if_missing;
