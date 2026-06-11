-- Migration script for AssistantHub v0.16.0
-- MySQL
-- Optional assistant tool-routing inference endpoint and thinking exposure setting

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

CALL add_assistanthub_column_if_missing(
    'assistant_settings',
    'tool_routing_inference_endpoint_id',
    '`tool_routing_inference_endpoint_id` TEXT'
);

CALL add_assistanthub_column_if_missing(
    'assistant_settings',
    'expose_thinking',
    '`expose_thinking` TINYINT(1) NOT NULL DEFAULT 0'
);

DROP PROCEDURE add_assistanthub_column_if_missing;
