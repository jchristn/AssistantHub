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

-- Hybrid fusion, recency and prompt context order settings
CALL add_assistanthub_column_if_missing('assistant_settings', 'fusion_strategy', '`fusion_strategy` VARCHAR(32) DEFAULT ''Rrf''');
CALL add_assistanthub_column_if_missing('assistant_settings', 'rrf_k', '`rrf_k` INT NOT NULL DEFAULT 60');
CALL add_assistanthub_column_if_missing('assistant_settings', 'fusion_candidate_pool', '`fusion_candidate_pool` INT NULL');
CALL add_assistanthub_column_if_missing('assistant_settings', 'recency_weight', '`recency_weight` DOUBLE NOT NULL DEFAULT 0');
CALL add_assistanthub_column_if_missing('assistant_settings', 'context_order', '`context_order` VARCHAR(32) DEFAULT ''Score''');

-- Reranking, conversation rewrite, supersession, duplicate detection and eval judge settings
CALL add_assistanthub_column_if_missing('assistant_settings', 'eval_judge_inference_endpoint_id', '`eval_judge_inference_endpoint_id` TEXT');
CALL add_assistanthub_column_if_missing('assistant_settings', 'embedding_task_prefixes', '`embedding_task_prefixes` TINYINT(1) NOT NULL DEFAULT 0');
CALL add_assistanthub_column_if_missing('assistant_settings', 'enable_conversation_rewrite', '`enable_conversation_rewrite` TINYINT(1) NOT NULL DEFAULT 0');
CALL add_assistanthub_column_if_missing('assistant_settings', 'conversation_rewrite_prompt', '`conversation_rewrite_prompt` TEXT');
CALL add_assistanthub_column_if_missing('assistant_settings', 'reranker_type', '`reranker_type` VARCHAR(32) DEFAULT ''Llm''');
CALL add_assistanthub_column_if_missing('assistant_settings', 'rerank_endpoint_id', '`rerank_endpoint_id` TEXT');
CALL add_assistanthub_column_if_missing('assistant_settings', 'rerank_candidate_count', '`rerank_candidate_count` INT NOT NULL DEFAULT 20');
CALL add_assistanthub_column_if_missing('assistant_settings', 'rerank_min_score', '`rerank_min_score` DOUBLE NULL');
CALL add_assistanthub_column_if_missing('assistant_settings', 'supersession_mode', '`supersession_mode` VARCHAR(32) DEFAULT ''Demote''');
CALL add_assistanthub_column_if_missing('assistant_documents', 'supersedes_json', '`supersedes_json` TEXT');
CALL add_assistanthub_column_if_missing('assistant_documents', 'superseded_by', '`superseded_by` TEXT');
CALL add_assistanthub_column_if_missing('assistant_documents', 'content_sha256', '`content_sha256` TEXT');
CALL add_assistanthub_column_if_missing('assistant_documents', 'near_duplicates_json', '`near_duplicates_json` TEXT');

DROP PROCEDURE add_assistanthub_column_if_missing;
