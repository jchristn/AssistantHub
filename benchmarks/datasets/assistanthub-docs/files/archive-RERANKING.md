# Re-Ranking Feature Plan — v0.6.0

## Overview

Add an **LLM-based re-ranking** step to the RAG retrieval pipeline. After initial retrieval (vector, full-text, or hybrid) returns candidate chunks, a secondary LLM call scores each chunk's relevance to the original user query on a 0–10 scale. The chunks are then re-sorted by their re-ranking scores, and only the top results (after a configurable re-rank score threshold) are injected into the system prompt for final inference.

Re-ranking addresses the core weakness of embedding-based retrieval: cosine similarity captures semantic proximity but does not measure whether a chunk actually **answers** the user's question. An LLM re-ranker acts as a precision filter — it reads each chunk in context of the query and makes a relevance judgment that embeddings alone cannot.

### Current Retrieval Flow

```
User Chat Message
    |
[1] Retrieval Gate (if EnableRag && EnableRetrievalGate)
    |   LLM classifies: RETRIEVE or SKIP
    |
[2] Query Rewrite (if EnableRag && EnableQueryRewrite && shouldRetrieve)
    |   LLM rewrites prompt into 1-4 query variants
    |
[3] Retrieval (if EnableRag && shouldRetrieve)
    |   For EACH query → RetrievalService.RetrieveAsync()
    |   Deduplicate + union + re-sort by score → cap at TopK
    |
[4] System Message Building
    |   Merge SystemPrompt + context chunks (+ citations if enabled)
    |
[5] Inference
    |   Final LLM call with full message list
    |
[6] History Persistence
```

### Proposed Flow (with Re-Ranking)

```
User Chat Message
    |
[1] Retrieval Gate (if EnableRag && EnableRetrievalGate)
    |   LLM classifies: RETRIEVE or SKIP
    |
[2] Query Rewrite (if EnableRag && EnableQueryRewrite && shouldRetrieve)
    |   LLM rewrites prompt into 1-4 query variants
    |
[3] Retrieval (if EnableRag && shouldRetrieve)
    |   For EACH query → RetrievalService.RetrieveAsync()
    |   Deduplicate + union + re-sort by score → cap at TopK
    |
[4] Re-Rank (if EnableRag && EnableReranking && shouldRetrieve)       <-- NEW
    |   LLM scores each retrieved chunk for relevance (0-10)
    |   Filter by RerankerScoreThreshold → re-sort by rerank score
    |   Cap at RerankerTopK (or original TopK if not set)
    |
[5] System Message Building
    |   Merge SystemPrompt + context chunks (+ citations if enabled)
    |
[6] Inference
    |   Final LLM call with full message list
    |
[7] History Persistence
    |   ChatHistory includes RerankDurationMs, RerankResultCount
```

---

## Design Decisions

1. **LLM-based scoring, not a cross-encoder model** — The system already has inference infrastructure (Ollama, OpenAI-compatible endpoints). Using the same LLM for re-ranking avoids deploying a separate cross-encoder model and keeps the architecture simple. For production-scale deployments, a dedicated cross-encoder endpoint could be added later as an alternative re-ranking provider.

2. **Batch scoring in a single LLM call** — Rather than making one LLM call per chunk (which would be N calls for N chunks), all candidate chunks are presented in a single prompt with indexed labels. The LLM returns a JSON array of scores. This keeps latency proportional to one LLM call, not N.

3. **Configurable toggle** — Re-ranking is opt-in via `EnableReranking` on `AssistantSettings`. This mirrors the pattern used by `EnableRetrievalGate` and `EnableQueryRewrite`.

4. **Over-fetch then filter pattern** — When re-ranking is enabled, retrieval should fetch more candidates than the final desired count. A new `RerankerTopK` setting controls how many chunks survive re-ranking. The existing `RetrievalTopK` controls how many candidates are fetched initially. For example: retrieve 20 candidates, re-rank, keep top 5.

5. **Runs after retrieval, before system message building** — This is the natural insertion point: we have the candidate chunks and need to filter/reorder them before they become context.

6. **Same inference endpoint** — Re-ranking uses the same resolved inference endpoint as the retrieval gate, query rewrite, and final completion. No separate endpoint configuration is needed.

7. **Fallback on failure** — If the re-ranking LLM call fails or returns unparseable output, the pipeline falls back to the original retrieval ordering. Re-ranking never blocks chat completion.

8. **Score preserved for telemetry** — Both the original retrieval score and the re-rank score are preserved on `RetrievalChunk` so the dashboard can display both, enabling operators to evaluate re-ranking effectiveness.

---

## Checklist

> Mark items: `[ ]` = pending, `[x]` = done, `[~]` = in progress, `[-]` = skipped

### 1. Model — `RetrievalChunk.cs`

- [ ] **1.1** Add `public double? RerankScore { get; set; } = null;` — the LLM-assigned relevance score (0.0–10.0), null when re-ranking is disabled
  - File: `src/AssistantHub.Core/Models/RetrievalChunk.cs`
  - Add `[JsonPropertyName("rerank_score")]` attribute

### 2. Model — `AssistantSettings.cs`

- [ ] **2.1** Add `public bool EnableReranking { get; set; } = false;` — master toggle for re-ranking
- [ ] **2.2** Add `public int RerankerTopK` with validated backing field (min 1), default `5` — number of chunks to keep after re-ranking
- [ ] **2.3** Add `public double RerankerScoreThreshold` with validated backing field (0.0–10.0), default `3.0` — minimum re-rank score for a chunk to be included
- [ ] **2.4** Add `public string RerankPrompt { get; set; } = null;` — customizable re-ranking prompt template (null = use default)
- [ ] **2.5** Add all four fields to `FromDataRow()`:
  - `obj.EnableReranking = DataTableHelper.GetBooleanValue(row, "enable_reranking", false);`
  - `obj.RerankerTopK = DataTableHelper.GetIntValue(row, "reranker_top_k", 5);`
  - `obj.RerankerScoreThreshold = DataTableHelper.GetDoubleValue(row, "reranker_score_threshold", 3.0);`
  - `obj.RerankPrompt = DataTableHelper.GetStringValue(row, "rerank_prompt");`

### 3. Model — `ChatHistory.cs`

- [ ] **3.1** Add `public double RerankDurationMs { get; set; } = 0;` — timing of the re-rank LLM call
- [ ] **3.2** Add `public int RerankInputCount { get; set; } = 0;` — number of chunks sent to re-ranker
- [ ] **3.3** Add `public int RerankOutputCount { get; set; } = 0;` — number of chunks that survived re-ranking
- [ ] **3.4** Add all three fields to `FromDataRow()`:
  - `obj.RerankDurationMs = DataTableHelper.GetDoubleValue(row, "rerank_duration_ms");`
  - `obj.RerankInputCount = DataTableHelper.GetIntValue(row, "rerank_input_count");`
  - `obj.RerankOutputCount = DataTableHelper.GetIntValue(row, "rerank_output_count");`

### 4. Database — Schema (all four providers)

Files to update (CREATE TABLE + INSERT + UPDATE for `assistant_settings`; CREATE TABLE + INSERT + UPDATE for `chat_history`):

#### 4.1 `assistant_settings` table — add four columns

| Column | Type (SQLite) | Type (Pg) | Type (MySQL) | Type (SQL Server) | Default |
|---|---|---|---|---|---|
| `enable_reranking` | `INTEGER NOT NULL DEFAULT 0` | `BOOLEAN NOT NULL DEFAULT FALSE` | `TINYINT(1) NOT NULL DEFAULT 0` | `BIT NOT NULL DEFAULT 0` | `0` / `false` |
| `reranker_top_k` | `INTEGER NOT NULL DEFAULT 5` | `INTEGER NOT NULL DEFAULT 5` | `INT NOT NULL DEFAULT 5` | `INT NOT NULL DEFAULT 5` | `5` |
| `reranker_score_threshold` | `REAL NOT NULL DEFAULT 3.0` | `DOUBLE PRECISION NOT NULL DEFAULT 3.0` | `DOUBLE NOT NULL DEFAULT 3.0` | `FLOAT NOT NULL DEFAULT 3.0` | `3.0` |
| `rerank_prompt` | `TEXT` | `TEXT` | `TEXT` | `NVARCHAR(MAX)` | `NULL` |

- [ ] **4.1.1** `Sqlite/Queries/TableQueries.cs` — add columns to `assistant_settings` CREATE TABLE
- [ ] **4.1.2** `Postgresql/Queries/TableQueries.cs` — same
- [ ] **4.1.3** `Mysql/Queries/TableQueries.cs` — same
- [ ] **4.1.4** `SqlServer/Queries/TableQueries.cs` — same

#### 4.2 `chat_history` table — add three columns

| Column | Type (SQLite) | Type (Pg) | Type (MySQL) | Type (SQL Server) | Default |
|---|---|---|---|---|---|
| `rerank_duration_ms` | `REAL NOT NULL DEFAULT 0` | `DOUBLE PRECISION NOT NULL DEFAULT 0` | `DOUBLE NOT NULL DEFAULT 0` | `FLOAT NOT NULL DEFAULT 0` | `0` |
| `rerank_input_count` | `INTEGER NOT NULL DEFAULT 0` | `INTEGER NOT NULL DEFAULT 0` | `INT NOT NULL DEFAULT 0` | `INT NOT NULL DEFAULT 0` | `0` |
| `rerank_output_count` | `INTEGER NOT NULL DEFAULT 0` | `INTEGER NOT NULL DEFAULT 0` | `INT NOT NULL DEFAULT 0` | `INT NOT NULL DEFAULT 0` | `0` |

- [ ] **4.2.1** `Sqlite/Queries/TableQueries.cs` — add columns to `chat_history` CREATE TABLE
- [ ] **4.2.2** `Postgresql/Queries/TableQueries.cs` — same
- [ ] **4.2.3** `Mysql/Queries/TableQueries.cs` — same
- [ ] **4.2.4** `SqlServer/Queries/TableQueries.cs` — same

#### 4.3 CRUD implementations — add new columns to INSERT and UPDATE

- [ ] **4.3.1** `Sqlite/Implementations/AssistantSettingsMethods.cs` — INSERT + UPDATE
- [ ] **4.3.2** `Postgresql/Implementations/AssistantSettingsMethods.cs` — INSERT + UPDATE
- [ ] **4.3.3** `Mysql/Implementations/AssistantSettingsMethods.cs` — INSERT + UPDATE
- [ ] **4.3.4** `SqlServer/Implementations/AssistantSettingsMethods.cs` — INSERT + UPDATE

- [ ] **4.3.5** `Sqlite/Implementations/ChatHistoryMethods.cs` — INSERT + UPDATE
- [ ] **4.3.6** `Postgresql/Implementations/ChatHistoryMethods.cs` — INSERT + UPDATE
- [ ] **4.3.7** `Mysql/Implementations/ChatHistoryMethods.cs` — INSERT + UPDATE
- [ ] **4.3.8** `SqlServer/Implementations/ChatHistoryMethods.cs` — INSERT + UPDATE

### 5. Migration Script — `migrations/004_upgrade_to_v0.6.0.sql`

- [ ] **5.1** Create `migrations/004_upgrade_to_v0.6.0.sql` with ALTER TABLE statements for all four database providers (SQLite, PostgreSQL, MySQL, SQL Server)

```sql
-- Migration 004: Upgrade existing v0.5.0 database to v0.6.0
-- Adds re-ranking columns to assistant_settings and chat_history
-- Back up your database before running this migration.

------------------------------------------------------------------------
-- SQLite
------------------------------------------------------------------------
ALTER TABLE assistant_settings ADD COLUMN enable_reranking INTEGER NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN reranker_top_k INTEGER NOT NULL DEFAULT 5;
ALTER TABLE assistant_settings ADD COLUMN reranker_score_threshold REAL NOT NULL DEFAULT 3.0;
ALTER TABLE assistant_settings ADD COLUMN rerank_prompt TEXT;

ALTER TABLE chat_history ADD COLUMN rerank_duration_ms REAL NOT NULL DEFAULT 0;
ALTER TABLE chat_history ADD COLUMN rerank_input_count INTEGER NOT NULL DEFAULT 0;
ALTER TABLE chat_history ADD COLUMN rerank_output_count INTEGER NOT NULL DEFAULT 0;

------------------------------------------------------------------------
-- PostgreSQL
------------------------------------------------------------------------
-- ALTER TABLE assistant_settings ADD COLUMN enable_reranking BOOLEAN NOT NULL DEFAULT FALSE;
-- ALTER TABLE assistant_settings ADD COLUMN reranker_top_k INTEGER NOT NULL DEFAULT 5;
-- ALTER TABLE assistant_settings ADD COLUMN reranker_score_threshold DOUBLE PRECISION NOT NULL DEFAULT 3.0;
-- ALTER TABLE assistant_settings ADD COLUMN rerank_prompt TEXT;

-- ALTER TABLE chat_history ADD COLUMN rerank_duration_ms DOUBLE PRECISION NOT NULL DEFAULT 0;
-- ALTER TABLE chat_history ADD COLUMN rerank_input_count INTEGER NOT NULL DEFAULT 0;
-- ALTER TABLE chat_history ADD COLUMN rerank_output_count INTEGER NOT NULL DEFAULT 0;

------------------------------------------------------------------------
-- MySQL
------------------------------------------------------------------------
-- ALTER TABLE assistant_settings ADD COLUMN enable_reranking TINYINT(1) NOT NULL DEFAULT 0;
-- ALTER TABLE assistant_settings ADD COLUMN reranker_top_k INT NOT NULL DEFAULT 5;
-- ALTER TABLE assistant_settings ADD COLUMN reranker_score_threshold DOUBLE NOT NULL DEFAULT 3.0;
-- ALTER TABLE assistant_settings ADD COLUMN rerank_prompt TEXT;

-- ALTER TABLE chat_history ADD COLUMN rerank_duration_ms DOUBLE NOT NULL DEFAULT 0;
-- ALTER TABLE chat_history ADD COLUMN rerank_input_count INT NOT NULL DEFAULT 0;
-- ALTER TABLE chat_history ADD COLUMN rerank_output_count INT NOT NULL DEFAULT 0;

------------------------------------------------------------------------
-- SQL Server
------------------------------------------------------------------------
-- ALTER TABLE assistant_settings ADD enable_reranking BIT NOT NULL DEFAULT 0;
-- ALTER TABLE assistant_settings ADD reranker_top_k INT NOT NULL DEFAULT 5;
-- ALTER TABLE assistant_settings ADD reranker_score_threshold FLOAT NOT NULL DEFAULT 3.0;
-- ALTER TABLE assistant_settings ADD rerank_prompt NVARCHAR(MAX);

-- ALTER TABLE chat_history ADD rerank_duration_ms FLOAT NOT NULL DEFAULT 0;
-- ALTER TABLE chat_history ADD rerank_input_count INT NOT NULL DEFAULT 0;
-- ALTER TABLE chat_history ADD rerank_output_count INT NOT NULL DEFAULT 0;
```

### 6. Backend — `AssistantSettingsHandler.cs`

- [ ] **6.1** In `PutSettingsAsync()`, propagate `EnableReranking`, `RerankerTopK`, `RerankerScoreThreshold`, and `RerankPrompt` from the deserialized request to the persisted settings object
- [ ] **6.2** Validate that `RerankPrompt`, if non-null/non-empty, contains the `{query}` and `{chunks}` placeholders; return 400 if missing
- [ ] **6.3** Validate `RerankerTopK >= 1` and `0.0 <= RerankerScoreThreshold <= 10.0`

### 7. Backend — `ChatHandler.cs` (core feature logic)

#### 7.1 Default Re-Rank Prompt

- [ ] **7.1.1** Add a static `_DefaultRerankPrompt` string constant containing the default prompt (see Appendix A)

#### 7.2 Re-Ranking Step

- [ ] **7.2.1** Add the re-ranking step **after** retrieval deduplication/sorting and **before** system message building. Location: between the current retrieval block (lines ~384–438) and the context extraction (line ~441). Guard: only run when `settings.EnableRag && settings.EnableReranking && shouldRetrieve && retrievalChunks.Count > 0`

- [ ] **7.2.2** Implementation flow:
  1. Resolve inference endpoint (same pattern as retrieval gate — reuse the resolved provider/endpoint/apiKey from `settings.InferenceEndpointId`)
  2. Build the re-rank prompt:
     - Use `settings.RerankPrompt` if set, otherwise `_DefaultRerankPrompt`
     - Replace `{query}` with `lastUserMessage`
     - Replace `{chunks}` with a numbered list of chunk contents (truncated to ~500 chars each to keep prompt manageable)
  3. Call `Inference.GenerateResponseAsync()` with the re-rank prompt as a system message, `MaxTokens = 512`, `Temperature = 0.0`, `TopP = 1.0`
  4. Parse the JSON array response: expect `[{"index": 1, "score": 8.5}, ...]`
  5. Map scores back to `retrievalChunks` by index, setting `RerankScore` on each chunk
  6. Filter out chunks below `RerankerScoreThreshold`
  7. Re-sort by `RerankScore` descending
  8. Cap at `RerankerTopK`
  9. Record `rerankDurationMs`, `rerankInputCount`, `rerankOutputCount` for ChatHistory

- [ ] **7.2.3** Fallback on failure: if the LLM call fails, returns non-JSON, or returns fewer scores than chunks, log a warning and proceed with the original retrieval ordering (no chunks are dropped)

- [ ] **7.2.4** Persist `RerankDurationMs`, `RerankInputCount`, and `RerankOutputCount` into the `ChatHistory` record (both streaming and non-streaming paths)

### 8. Model — `ChatCompletionResponse.cs` (SSE telemetry)

- [ ] **8.1** Add re-ranking telemetry fields to `ChatCompletionRetrieval` so the frontend receives re-ranking data via SSE:
  ```csharp
  [JsonPropertyName("rerank_duration_ms")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public double RerankDurationMs { get; set; } = 0;

  [JsonPropertyName("rerank_input_count")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public int RerankInputCount { get; set; } = 0;

  [JsonPropertyName("rerank_output_count")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public int RerankOutputCount { get; set; } = 0;
  ```
- [ ] **8.2** In `ChatHandler.cs`, populate these fields on the `ChatCompletionRetrieval` object when re-ranking is performed (both streaming and non-streaming paths)

### 9. Frontend — `AssistantSettingsView.jsx`

- [ ] **9.1** Add an **Enable Re-Ranking** checkbox in the RAG Configuration section, after the Query Rewrite controls
  - Tooltip: `"When enabled, retrieved chunks are scored by an LLM for relevance to the query. Low-relevance chunks are filtered out before context injection, improving answer precision."`
  - Follow the existing pattern: `<input type="checkbox" checked={settings.EnableReranking} onChange={(e) => handleChange('EnableReranking', e.target.checked)} />`

- [ ] **9.2** Conditionally render (when `EnableReranking` is checked) the re-ranking configuration controls:
  - **Re-Ranker Top K** — number input, min 1, tooltip: `"Maximum number of chunks to keep after re-ranking. Should be less than or equal to Retrieval Top K."`
  - **Re-Ranker Score Threshold** — range input 0.0–10.0 with step 0.5, display current value with `<span className="range-value">{settings.RerankerScoreThreshold}</span>` (matching the existing pattern used by Temperature, TopP, RetrievalScoreThreshold, and TextWeight), tooltip: `"Minimum re-rank score (0-10) for a chunk to be included. Higher values mean stricter filtering."`
  - **Re-Rank Prompt** — textarea (4-6 rows), tooltip: `"The prompt sent to the LLM to score each chunk's relevance. Must contain {query} and {chunks} placeholders. Leave blank to use the built-in default."`

- [ ] **9.3** Add `EnableReranking`, `RerankerTopK`, `RerankerScoreThreshold`, and `RerankPrompt` to the settings state initialization in `loadSettings`:
  ```javascript
  EnableReranking: result?.EnableReranking ?? false,
  RerankerTopK: result?.RerankerTopK ?? 5,
  RerankerScoreThreshold: result?.RerankerScoreThreshold ?? 3.0,
  RerankPrompt: result?.RerankPrompt || '',
  ```

- [ ] **9.4** Add the new fields to the `handleSave` serialization with explicit type coercion (matching the existing pattern for `TextWeight` and `FullTextNormalization`):
  ```javascript
  RerankerTopK: parseInt(settings.RerankerTopK) || 5,
  RerankerScoreThreshold: parseFloat(settings.RerankerScoreThreshold) || 3.0,
  ```

### 10. Frontend — Chat Experience

- [ ] **10.1** In `ChatPanel.jsx` or `ChatDrawer.jsx`: if re-ranking telemetry is present in the SSE metadata, display it in the chat response metadata (alongside existing retrieval duration, retrieval gate decision, etc.)
  - Display: "Re-ranked: N→M chunks in Xms" (where N = input count, M = output count, X = duration)

- [ ] **10.2** In the history detail view (`HistoryViewModal.jsx`), add a **Re-Ranking `TimingBar`** to the Performance Timing section, placed between the existing "Retrieval" and "Endpoint Resolution" bars:
  ```jsx
  <TimingBar
    label="Re-Ranking"
    tooltip="LLM-based re-ranking — scores each retrieved chunk for relevance and filters low-scoring chunks"
    durationMs={history.RerankDurationMs}
    totalMs={totalPipelineMs}
    color="var(--timing-rerank, #ffa94d)"
  />
  ```

- [ ] **10.3** Update the `totalPipelineMs` calculation in `HistoryViewModal.jsx` to include re-ranking duration:
  ```javascript
  const totalPipelineMs =
    (history.RetrievalGateDurationMs || 0) +
    (history.QueryRewriteDurationMs || 0) +
    (history.RetrievalDurationMs || 0) +
    (history.RerankDurationMs || 0) +          // <-- NEW
    (history.EndpointResolutionDurationMs || 0) +
    (history.CompactionDurationMs || 0) +
    (history.TimeToLastTokenMs || 0);
  ```

- [ ] **10.4** In the `HistoryViewModal.jsx` individual chunk cards, display the `RerankScore` alongside the existing cosine `Score` when present:
  ```jsx
  {chunk.rerank_score != null && (
    <span className="history-chunk-score">Relevance: <strong>{chunk.rerank_score.toFixed(1)}/10</strong></span>
  )}
  ```

### 11. Frontend — Citation Score Display

- [ ] **11.1** If re-ranking is enabled, the `CitationSource` objects passed via SSE should include the `RerankScore` alongside the existing `Score` (cosine similarity). Update `CitationSource` class in `ChatHandler.cs` to include `RerankScore`:
  ```csharp
  public double? RerankScore { get; set; }
  ```

- [ ] **11.2** In the chat UI citation cards, display the re-rank score when present (e.g., "Relevance: 8.5/10") to give users confidence in source quality

### 12. Tests — `AssistantSettingsTests.cs`

- [ ] **12.1** Add `EnableReranking`, `RerankerTopK`, `RerankerScoreThreshold`, and `RerankPrompt` to the create-with-values test
- [ ] **12.2** Add all four fields to the update test
- [ ] **12.3** Verify round-trip in the read test

### 13. Tests — `ChatHistoryTests.cs`

- [ ] **13.1** Add `RerankDurationMs`, `RerankInputCount`, and `RerankOutputCount` to the create test
- [ ] **13.2** Verify round-trip in the read test

### 14. Documentation — `REST_API.md`

- [ ] **14.1** Add re-ranking fields to the **Assistant Settings** field table:

| Field | Type | Default | Description |
|---|---|---|---|
| `EnableReranking` | bool | `false` | Enable LLM-based re-ranking of retrieved chunks |
| `RerankerTopK` | int | `5` | Maximum chunks to keep after re-ranking |
| `RerankerScoreThreshold` | double | `3.0` | Minimum LLM relevance score (0-10) to retain a chunk |
| `RerankPrompt` | string | `null` | Custom re-ranking prompt (must contain `{query}` and `{chunks}` placeholders) |

- [ ] **14.2** Add re-ranking fields to the **Chat History** response documentation:

| Field | Type | Description |
|---|---|---|
| `RerankDurationMs` | double | Duration of the re-ranking LLM call in milliseconds |
| `RerankInputCount` | int | Number of chunks sent to the re-ranker |
| `RerankOutputCount` | int | Number of chunks that survived re-ranking |

- [ ] **14.3** Document the `{query}` and `{chunks}` placeholder requirements for `RerankPrompt`

- [ ] **14.4** Add a section explaining the re-ranking pipeline step and its interaction with existing retrieval features (search modes, query rewrite, retrieval gate, citations)

### 15. Documentation — `README.md`

- [ ] **15.1** Add re-ranking to the feature list in the RAG capabilities section
- [ ] **15.2** Add brief description explaining how re-ranking improves retrieval precision
- [ ] **15.3** Update version references from v0.5.0 to v0.6.0

### 16. Documentation — `CHANGELOG.md`

- [ ] **16.1** Add a `## Current Version: v0.6.0` section at the top; move v0.5.0 content under `## v0.5.0`
- [ ] **16.2** Document the re-ranking feature, new settings fields, chat history fields, and schema changes
- [ ] **16.3** Add breaking change notice for schema changes and reference migration script

### 17. Postman Collection — `postman/AssistantHub.postman_collection.json`

- [ ] **17.1** Update the PUT assistant settings request body to include `EnableReranking`, `RerankerTopK`, `RerankerScoreThreshold`, and `RerankPrompt` fields
- [ ] **17.2** Update any version references

### 18. OpenAPI Spec — `openapi.json`

- [ ] **18.1** Add `EnableReranking`, `RerankerTopK`, `RerankerScoreThreshold`, and `RerankPrompt` to the AssistantSettings schema
- [ ] **18.2** Add `RerankDurationMs`, `RerankInputCount`, and `RerankOutputCount` to the ChatHistory schema
- [ ] **18.3** Add `RerankScore` to the citation source schema

### 19. Docker — Rebuild SQLite databases

- [ ] **19.1** `docker/assistanthub/` — recreate SQLite database with updated schema (includes new columns, seeded default data)

### 20. Version Bump — Global sweep

Update **all** references from `v0.5.0` to `v0.6.0` across:

- [ ] **20.1** `README.md`
- [ ] **20.2** `CHANGELOG.md`
- [ ] **20.3** `REST_API.md`
- [ ] **20.4** `docker/compose.yaml`
- [ ] **20.5** Any other files found by grep for `v0\.5\.0`

---

## Appendix A — Default Re-Rank Prompt

```
You are a relevance judge. Given a user query and a numbered list of text chunks retrieved from a document collection, score each chunk's relevance to answering the query.

Score each chunk from 0 to 10:
- 0: Completely irrelevant, no connection to the query
- 1-3: Tangentially related but does not help answer the query
- 4-6: Somewhat relevant, contains related information but may not directly answer
- 7-8: Highly relevant, directly addresses the query
- 9-10: Perfect match, contains the exact information needed to answer

Respond with ONLY a JSON array of objects, each with "index" (the chunk number) and "score" (your relevance rating).

Example response:
[{"index": 1, "score": 8}, {"index": 2, "score": 3}, {"index": 3, "score": 7}]

User query:
{query}

Retrieved chunks:
{chunks}
```

## Appendix B — Re-Rank Prompt Chunk Formatting

When building the `{chunks}` replacement, each chunk is formatted as:

```
[1] <chunk text truncated to 500 chars>
[2] <chunk text truncated to 500 chars>
...
```

This keeps the total prompt size manageable. The 500-character truncation per chunk means that with the default `RetrievalTopK` of 10, the chunks section is at most ~5,000 characters (~1,500 tokens), keeping the full re-rank prompt well within typical context windows.

## Appendix C — Parsing Logic

The LLM response is parsed as follows:

1. Trim whitespace from the response
2. If the response starts with `` ```json `` or `` ``` ``, strip markdown code fences
3. Attempt `JsonSerializer.Deserialize<List<RerankResult>>(response)` where:
   ```csharp
   private class RerankResult
   {
       public int Index { get; set; }
       public double Score { get; set; }
   }
   ```
4. If deserialization succeeds and the list is non-empty:
   - Map each `RerankResult.Score` to the corresponding `RetrievalChunk` by `Index` (1-based)
   - Set `chunk.RerankScore = result.Score`
   - Filter chunks where `RerankScore >= settings.RerankerScoreThreshold`
   - Sort by `RerankScore` descending
   - Take top `settings.RerankerTopK`
5. If deserialization fails or the list is empty:
   - Log a warning
   - Fall through with the original retrieval ordering unchanged

---

## File Impact Summary

| File | Change Type | Description |
|---|---|---|
| `src/AssistantHub.Core/Models/RetrievalChunk.cs` | Modify | Add `RerankScore` property |
| `src/AssistantHub.Core/Models/AssistantSettings.cs` | Modify | Add 4 re-ranking settings + `FromDataRow()` |
| `src/AssistantHub.Core/Models/ChatHistory.cs` | Modify | Add 3 telemetry fields + `FromDataRow()` |
| `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs` | Modify | Schema columns |
| `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs` | Modify | Schema columns |
| `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs` | Modify | Schema columns |
| `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs` | Modify | Schema columns |
| `src/AssistantHub.Core/Database/Sqlite/Implementations/AssistantSettingsMethods.cs` | Modify | INSERT + UPDATE |
| `src/AssistantHub.Core/Database/Postgresql/Implementations/AssistantSettingsMethods.cs` | Modify | INSERT + UPDATE |
| `src/AssistantHub.Core/Database/Mysql/Implementations/AssistantSettingsMethods.cs` | Modify | INSERT + UPDATE |
| `src/AssistantHub.Core/Database/SqlServer/Implementations/AssistantSettingsMethods.cs` | Modify | INSERT + UPDATE |
| `src/AssistantHub.Core/Database/Sqlite/Implementations/ChatHistoryMethods.cs` | Modify | INSERT + UPDATE |
| `src/AssistantHub.Core/Database/Postgresql/Implementations/ChatHistoryMethods.cs` | Modify | INSERT + UPDATE |
| `src/AssistantHub.Core/Database/Mysql/Implementations/ChatHistoryMethods.cs` | Modify | INSERT + UPDATE |
| `src/AssistantHub.Core/Database/SqlServer/Implementations/ChatHistoryMethods.cs` | Modify | INSERT + UPDATE |
| `src/AssistantHub.Core/Models/ChatCompletionResponse.cs` | Modify | Add re-ranking telemetry to `ChatCompletionRetrieval`, add `RerankScore` to `CitationSource` |
| `src/AssistantHub.Server/Handlers/AssistantSettingsHandler.cs` | Modify | PUT validation + propagation |
| `src/AssistantHub.Server/Handlers/ChatHandler.cs` | Modify | Core re-ranking logic + populate SSE telemetry |
| `dashboard/src/views/AssistantSettingsView.jsx` | Modify | UI controls for re-ranking |
| `dashboard/src/components/ChatPanel.jsx` | Modify | Display re-rank telemetry |
| `dashboard/src/components/modals/HistoryViewModal.jsx` | Modify | Display re-rank metrics |
| `src/Test.Database/Tests/AssistantSettingsTests.cs` | Modify | Add re-ranking fields |
| `src/Test.Database/Tests/ChatHistoryTests.cs` | Modify | Add re-ranking fields |
| `migrations/004_upgrade_to_v0.6.0.sql` | Create | Migration script |
| `README.md` | Modify | Feature list + version |
| `REST_API.md` | Modify | API field docs |
| `CHANGELOG.md` | Modify | Release notes |
| `openapi.json` | Modify | Schema updates |
| `postman/AssistantHub.postman_collection.json` | Modify | Request bodies |
| `docker/assistanthub/` | Modify | Rebuild SQLite DB |

---

## Estimated Implementation Order

The recommended implementation sequence, based on dependencies:

1. **Models** (tasks 1–3) — no dependencies, foundation for everything else
2. **Database schema + CRUD** (task 4) — depends on models
3. **Migration script** (task 5) — depends on schema design
4. **Backend handler: settings** (task 6) — depends on models + CRUD
5. **Backend handler: chat re-ranking logic** (task 7) — depends on all above
6. **SSE telemetry model** (task 8) — depends on ChatHandler producing the data
7. **Frontend: settings UI** (task 9) — depends on backend settings handler
8. **Frontend: chat experience + history detail** (tasks 10–11) — depends on backend chat handler + SSE telemetry
9. **Tests** (tasks 12–13) — depends on models + CRUD
10. **Documentation** (tasks 14–16) — depends on feature being complete
11. **Postman + OpenAPI** (tasks 17–18) — depends on feature being complete
12. **Docker + version bump** (tasks 19–20) — final step
