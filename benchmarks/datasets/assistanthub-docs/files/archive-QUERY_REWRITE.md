# Query Rewrite Feature Plan — v0.4.0

## Overview

Add a **query rewrite** capability to the RAG pipeline. When enabled, the user's prompt is sent to the configured inference endpoint with a rewrite prompt that asks the LLM to produce up to three semantically varied rephrasings of the original query. All resulting prompts (always including the original) are then used for retrieval against RecallDB, and the union of results is passed as context to the final inference call. This broadens recall by capturing synonyms, alternate phrasing, and conceptual restatements that a single query would miss.

This is a **breaking schema change** (no migration). A SQLite upgrade script for the production system is provided separately in the console output.

---

## Checklist

> Mark items: `[ ]` = pending, `[x]` = done, `[~]` = in progress, `[-]` = skipped

### 1. Model — `AssistantSettings.cs`

- [x] **1.1** Add `public bool EnableQueryRewrite { get; set; } = false;`
- [x] **1.2** Add `public string QueryRewritePrompt { get; set; }` with the default prompt (see Appendix A)
- [x] **1.3** Add both fields to `FromDataRow()`:
  - `obj.EnableQueryRewrite = DataTableHelper.GetBooleanValue(row, "enable_query_rewrite", false);`
  - `obj.QueryRewritePrompt = DataTableHelper.GetStringValue(row, "query_rewrite_prompt");`

### 2. Model — `ChatHistory.cs`

- [x] **2.1** Add `public string QueryRewriteResult { get; set; } = null;` — stores the newline-separated rewritten prompts returned by the LLM (null when feature disabled or not triggered)
- [x] **2.2** Add `public double QueryRewriteDurationMs { get; set; } = 0;` — timing of the rewrite LLM call
- [x] **2.3** Add both fields to `FromDataRow()`

### 3. Database — Schema (all four providers)

Files to update (CREATE TABLE + INSERT + UPDATE for `assistant_settings`; CREATE TABLE + INSERT + UPDATE for `chat_history`):

#### 3.1 `assistant_settings` table — add two columns

| Column | Type (SQLite) | Type (Pg) | Type (MySQL) | Type (SQL Server) | Default |
|---|---|---|---|---|---|
| `enable_query_rewrite` | `INTEGER NOT NULL DEFAULT 0` | `BOOLEAN NOT NULL DEFAULT FALSE` | `TINYINT(1) NOT NULL DEFAULT 0` | `BIT NOT NULL DEFAULT 0` | `0` / `false` |
| `query_rewrite_prompt` | `TEXT` | `TEXT` | `TEXT` | `NVARCHAR(MAX)` | `NULL` |

- [x] **3.1.1** `Sqlite/Queries/TableQueries.cs` — add columns to `assistant_settings` CREATE TABLE
- [x] **3.1.2** `Postgresql/Queries/TableQueries.cs` — same
- [x] **3.1.3** `Mysql/Queries/TableQueries.cs` — same
- [x] **3.1.4** `SqlServer/Queries/TableQueries.cs` — same

#### 3.2 `chat_history` table — add two columns

| Column | Type (SQLite) | Type (Pg) | Type (MySQL) | Type (SQL Server) | Default |
|---|---|---|---|---|---|
| `query_rewrite_result` | `TEXT` | `TEXT` | `TEXT` | `NVARCHAR(MAX)` | `NULL` |
| `query_rewrite_duration_ms` | `REAL NOT NULL DEFAULT 0` | `DOUBLE PRECISION NOT NULL DEFAULT 0` | `DOUBLE NOT NULL DEFAULT 0` | `FLOAT NOT NULL DEFAULT 0` | `0` |

- [x] **3.2.1** `Sqlite/Queries/TableQueries.cs` — add columns to `chat_history` CREATE TABLE
- [x] **3.2.2** `Postgresql/Queries/TableQueries.cs` — same
- [x] **3.2.3** `Mysql/Queries/TableQueries.cs` — same
- [x] **3.2.4** `SqlServer/Queries/TableQueries.cs` — same

#### 3.3 CRUD implementations — add new columns to INSERT and UPDATE

- [x] **3.3.1** `Sqlite/Implementations/AssistantSettingsMethods.cs` — INSERT + UPDATE
- [x] **3.3.2** `Postgresql/Implementations/AssistantSettingsMethods.cs` — INSERT + UPDATE
- [x] **3.3.3** `Mysql/Implementations/AssistantSettingsMethods.cs` — INSERT + UPDATE
- [x] **3.3.4** `SqlServer/Implementations/AssistantSettingsMethods.cs` — INSERT + UPDATE

- [x] **3.3.5** `Sqlite/Implementations/ChatHistoryMethods.cs` — INSERT + UPDATE
- [x] **3.3.6** `Postgresql/Implementations/ChatHistoryMethods.cs` — INSERT + UPDATE
- [x] **3.3.7** `Mysql/Implementations/ChatHistoryMethods.cs` — INSERT + UPDATE
- [x] **3.3.8** `SqlServer/Implementations/ChatHistoryMethods.cs` — INSERT + UPDATE

### 4. Docker SQLite databases — rebuild with new schema

- [x] **4.1** `docker/assistanthub/data/assistanthub.db` — recreate with updated schema (includes new columns, seeded default data)
- [x] **4.2** `docker/factory/assistanthub.db` — recreate with updated schema

### 5. Backend — `AssistantSettingsHandler.cs`

- [x] **5.1** In `PutSettingsAsync()`, propagate `EnableQueryRewrite` and `QueryRewritePrompt` from the deserialized request to the persisted settings object
- [x] **5.2** Validate that `QueryRewritePrompt`, if non-null/non-empty, contains the `{prompt}` placeholder; return 400 if missing

### 6. Backend — `ChatHandler.cs` (core feature logic)

- [x] **6.1** Add a static `_DefaultQueryRewritePrompt` string containing the default prompt (Appendix A) — used as fallback when `QueryRewritePrompt` is null/empty
- [x] **6.2** Add the query rewrite step **after** the retrieval gate and **before** retrieval:
  1. Guard: only run when `settings.EnableRag && settings.EnableQueryRewrite && shouldRetrieve && !String.IsNullOrEmpty(lastUserMessage)`
  2. Resolve inference endpoint (same pattern as retrieval gate — reuse the resolved provider/endpoint/apiKey)
  3. Build the rewrite prompt by replacing `{prompt}` in `settings.QueryRewritePrompt` (or `_DefaultQueryRewritePrompt`) with `lastUserMessage`
  4. Call `Inference.GenerateResponseAsync()` with the rewrite prompt as a system message, reasonable `MaxTokens` (e.g. 512), `Temperature = 0.7`, `TopP = 1.0`
  5. Parse the response: split on newlines, trim whitespace, remove empty lines → produces `List<string> retrievalQueries`
  6. If parsing fails or returns nothing, fall back to `new List<string> { lastUserMessage }`
  7. Record `queryRewriteResult` (newline-joined) and `queryRewriteDurationMs` for ChatHistory
- [x] **6.3** Modify the retrieval block to loop over `retrievalQueries`:
  - For each query string, call `Retrieval.RetrieveAsync()` with the same settings
  - Deduplicate returned chunks (by chunk content or document ID + position) to avoid feeding duplicates into context
  - Union all chunks, re-sort by score descending, and cap at `RetrievalTopK`
- [x] **6.4** Persist `QueryRewriteResult` and `QueryRewriteDurationMs` into the `ChatHistory` record

### 7. Frontend — `AssistantSettingsFormModal.jsx`

- [x] **7.1** Add an **Enable Query Rewrite** checkbox (follows the pattern of `EnableRetrievalGate` / `EnableCitations`)
  - Tooltip: `"When enabled, the user's prompt is rewritten into multiple semantically varied queries before retrieval, improving recall by capturing synonyms and alternate phrasing."`
- [x] **7.2** Conditionally render (when `EnableQueryRewrite` is checked) a **Query Rewrite Prompt** textarea (4-6 rows)
  - Tooltip: `"The prompt sent to the LLM to rewrite the user's query. Must contain the {prompt} placeholder, which is replaced with the user's message at runtime. The LLM should return a newline-separated list of prompts including the original."`
  - Placeholder text showing the default prompt so the user knows what they're customizing
  - Pre-populate with the default prompt if the field is empty/null
- [x] **7.3** Place these controls in the RAG Configuration section, logically grouped near the retrieval gate toggle

### 8. Tests — `AssistantSettingsTests.cs`

- [x] **8.1** Add `EnableQueryRewrite` and `QueryRewritePrompt` to the create-with-values test
- [x] **8.2** Add both fields to the update test
- [x] **8.3** Verify round-trip in the read test

### 9. Documentation — `REST_API.md`

- [x] **9.1** Add `EnableQueryRewrite` (boolean) and `QueryRewritePrompt` (string) to the assistant settings field table
- [x] **9.2** Document the `{prompt}` placeholder requirement
- [x] **9.3** Add `QueryRewriteResult` and `QueryRewriteDurationMs` to the chat history response documentation
- [x] **9.4** Update version references from v0.3.0 / v0.2.0 to v0.4.0

### 10. Documentation — `README.md`

- [x] **10.1** Add query rewrite to the feature list
- [x] **10.2** Update version references from v0.3.0 / v0.2.0 to v0.4.0

### 11. Documentation — `CHANGELOG.md`

- [x] **11.1** Add a `## Current Version: v0.4.0` section at the top; move the v0.3.0 content under a `## v0.3.0` heading
- [x] **11.2** Document the query rewrite feature, schema changes, and breaking change notice

### 12. Postman Collection — `postman/AssistantHub.postman_collection.json`

- [x] **12.1** Update the PUT assistant settings request body to include `EnableQueryRewrite` and `QueryRewritePrompt` fields
- [x] **12.2** Update any version references

### 13. Version Bump — Global sweep

Update **all** references from `v0.3.0` (and any remaining `v0.2.0`) to `v0.4.0` across:

- [x] **13.1** `README.md`
- [x] **13.2** `CHANGELOG.md`
- [x] **13.3** `REST_API.md`
- [x] **13.4** `MULTI_TENANT.md`
- [x] **13.5** `docker/compose.yaml`
- [x] **13.6** `src/AssistantHub.Core/Services/RetrievalService.cs` (if version referenced)
- [x] **13.7** `src/AssistantHub.Core/Services/IngestionService.cs` (if version referenced)
- [x] **13.8** `archive/CITATIONS.md`
- [x] **13.9** `archive/SUMMARIZATION.md`
- [x] **13.10** Any other files found by grep for `v0\.[23]\.0`

### 14. Migration Script — `migrations/002_upgrade_to_v0.4.0.sql`

- [x] **14.1** Create `migrations/002_upgrade_to_v0.4.0.sql` with ALTER TABLE statements for all four database providers (SQLite, PostgreSQL, MySQL, SQL Server) — mirrors the schema additions in Appendix B but covers all dialects
- [x] **14.2** Output the SQLite upgrade script to the console — see Appendix B

---

## Appendix A — Default Query Rewrite Prompt

```
Evaluate the following prompt.
Return up to three variants of this prompt using different words or phrasing to maximize retrieval accuracy.
If you are unable to rewrite this prompt, respond ONLY with the original prompt.
Always include the original prompt in the response.
Return nothing other than a newline-separated list of prompts.

Example #1 (positive example)
User prompt:
"How do I speed up my Postgres vector search? It's getting slow as my data grows."

LLM response:
"How do I speed up my Postgres vector search? It's getting slow as my data grows."
"How can I optimize pgvector similarity queries in Postgres (index type, HNSW/IVFFlat settings, query patterns, and maintenance like VACUUM/ANALYZE)?"
"Best practices for scaling Postgres vector retrieval with pgvector: schema design, indexing strategy, and query structure to reduce response time."
"pgvector performance tuning checklist: choosing distance function, index parameters, batch sizes, and filters without killing recall or speed."

Example #2 (negative example)
User prompt:
"Why is it doing that thing again?"

LLM response:
"Why is it doing that thing again?"

The prompt to evaluate is: {prompt}
```

## Appendix B — SQLite Production Upgrade Script

```sql
-- AssistantHub v0.3.0 → v0.4.0 upgrade script (SQLite)
-- Adds query rewrite columns to assistant_settings and chat_history

-- assistant_settings: add query rewrite fields
ALTER TABLE assistant_settings ADD COLUMN enable_query_rewrite INTEGER NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN query_rewrite_prompt TEXT;

-- chat_history: add query rewrite tracking fields
ALTER TABLE chat_history ADD COLUMN query_rewrite_result TEXT;
ALTER TABLE chat_history ADD COLUMN query_rewrite_duration_ms REAL NOT NULL DEFAULT 0;
```

---

## Execution Flow (updated)

```
User Chat Message
    |
[1] Retrieval Gate (if EnableRag && EnableRetrievalGate)
    |   LLM classifies: RETRIEVE or SKIP
    |
[2] Query Rewrite (if EnableRag && EnableQueryRewrite && shouldRetrieve)  <-- NEW
    |   LLM rewrites prompt into 1-4 query variants
    |
[3] Retrieval (if EnableRag && shouldRetrieve)
    |   For EACH rewritten query → RetrievalService.RetrieveAsync()
    |   Deduplicate + union + re-sort by score → cap at TopK
    |
[4] System Message Building
    |   Merge SystemPrompt + context chunks (+ citations if enabled)
    |
[5] Inference
    |   Final LLM call with full message list
    |
[6] History Persistence
    |   ChatHistory includes QueryRewriteResult, QueryRewriteDurationMs
```

---

## Design Decisions

1. **Prompt is user-editable** — Unlike the retrieval gate (hardcoded), the query rewrite prompt is stored in `assistant_settings` and editable via the dashboard. This lets users tune rewrite behavior for their domain.

2. **Default prompt provided** — When `QueryRewritePrompt` is null or empty, `_DefaultQueryRewritePrompt` in ChatHandler.cs is used. The dashboard pre-populates the textarea with this default.

3. **`{prompt}` placeholder required** — Validation in the PUT handler ensures the placeholder is present. The tooltip explains this requirement.

4. **Runs after retrieval gate, before retrieval** — If the retrieval gate says SKIP, query rewrite is also skipped (no point rewriting a query we won't retrieve against).

5. **Deduplication by content hash** — When multiple rewritten queries return overlapping chunks, duplicates are removed before context assembly. Chunks are compared by `DocumentId + Position` to catch exact duplicates while allowing the same document to contribute different chunks.

6. **Union capped at TopK** — After deduplication, results are re-sorted by score and trimmed to `RetrievalTopK` to avoid context window bloat.

7. **Fallback to original query** — If the LLM returns unparseable output or an error, retrieval proceeds with just the original user message.

8. **Same inference endpoint** — Query rewrite uses the same resolved inference endpoint as the retrieval gate and final completion. No separate endpoint configuration is needed.
