# Tokens Per Second & History Modal Improvements

## Overview

Three changes to the dashboard's request history feature:

1. **Tokens-per-second metrics** — compute and store two TPS values: overall (prompt sent → last token) and generation-only (first token → last token), plus the completion token count they depend on
2. **Identifiers layout** — display each identifier on its own row instead of all on one row
3. **Modal sizing** — allow the history details modal to consume 95% of viewport width and height

---

## Prerequisites

Three new fields must be added to the `chat_history` table and computed/persisted from the chat handler:

| New Column | Type | Description |
|---|---|---|
| `completion_tokens` | INTEGER | Estimated token count for the assistant's response |
| `tokens_per_second_overall` | REAL | CompletionTokens / (TimeToLastTokenMs / 1000) — end-to-end TPS from prompt sent to last token |
| `tokens_per_second_generation` | REAL | CompletionTokens / ((TimeToLastTokenMs - TimeToFirstTokenMs) / 1000) — TPS during the generation phase only (first token → last token) |

All three values are **computed on the backend** at write time and **stored in the database** so they are available via the REST API. The frontend **also computes them** from the stored fields for display, providing redundancy and allowing recalculation if the underlying timing fields are present but TPS was not yet stored (e.g., pre-migration history records).

---

## Implementation Plan

### Phase 1: Database — Add New Columns

#### Task 1.1: Create migration file `migrations/006_add_completion_tokens_and_tps.sql`

- [x] Create `migrations/006_add_completion_tokens_and_tps.sql`
- [x] Add `ALTER TABLE` statements for all four database providers:

  **SQLite / PostgreSQL:**
  ```sql
  ALTER TABLE chat_history ADD COLUMN completion_tokens INTEGER DEFAULT 0;
  ALTER TABLE chat_history ADD COLUMN tokens_per_second_overall REAL DEFAULT 0;
  ALTER TABLE chat_history ADD COLUMN tokens_per_second_generation REAL DEFAULT 0;
  ```

  **SQL Server (commented):**
  ```sql
  -- ALTER TABLE chat_history ADD completion_tokens INT DEFAULT 0;
  -- ALTER TABLE chat_history ADD tokens_per_second_overall FLOAT DEFAULT 0;
  -- ALTER TABLE chat_history ADD tokens_per_second_generation FLOAT DEFAULT 0;
  ```

  **MySQL (commented):**
  ```sql
  -- ALTER TABLE `chat_history` ADD COLUMN `completion_tokens` INT DEFAULT 0;
  -- ALTER TABLE `chat_history` ADD COLUMN `tokens_per_second_overall` DOUBLE DEFAULT 0;
  -- ALTER TABLE `chat_history` ADD COLUMN `tokens_per_second_generation` DOUBLE DEFAULT 0;
  ```

#### Task 1.2: Add auto-migration logic in each database driver

The application runs migrations on startup. Each driver has a method that checks for missing columns and adds them.

- [x] **SQLite** — `src/AssistantHub.Core/Database/Sqlite/SqliteDriver.cs`
  - In the migration/column-check section, add checks for three columns on `chat_history`:
    - `completion_tokens` — `INTEGER DEFAULT 0`
    - `tokens_per_second_overall` — `REAL DEFAULT 0`
    - `tokens_per_second_generation` — `REAL DEFAULT 0`
- [x] **PostgreSQL** — `src/AssistantHub.Core/Database/Postgresql/PostgresqlDriver.cs`
  - Same pattern: check for and add all three columns.
- [x] **SQL Server** — `src/AssistantHub.Core/Database/SqlServer/SqlServerDriver.cs`
  - Same pattern: `completion_tokens` as `INT`, TPS columns as `FLOAT`.
- [x] **MySQL** — `src/AssistantHub.Core/Database/Mysql/MysqlDriver.cs`
  - Same pattern: `completion_tokens` as `INT`, TPS columns as `DOUBLE`.

---

### Phase 2: Backend Model — Add New Properties

#### Task 2.1: Update `ChatHistory` model

- [x] **File:** `src/AssistantHub.Core/Models/ChatHistory.cs`
- [x] Add three properties after `PromptTokens` (around line 79):
  ```csharp
  /// <summary>
  /// Estimated completion (response) token count from the model.
  /// </summary>
  public int CompletionTokens { get; set; } = 0;

  /// <summary>
  /// Tokens per second (overall): CompletionTokens / (TimeToLastTokenMs / 1000).
  /// Measures end-to-end generation throughput from prompt sent to last token.
  /// </summary>
  public double TokensPerSecondOverall { get; set; } = 0;

  /// <summary>
  /// Tokens per second (generation only): CompletionTokens / ((TimeToLastTokenMs - TimeToFirstTokenMs) / 1000).
  /// Measures pure token generation throughput excluding prompt processing.
  /// </summary>
  public double TokensPerSecondGeneration { get; set; } = 0;
  ```
- [x] Update `FromDataRow` method (around line 160) to read the new columns:
  ```csharp
  obj.CompletionTokens = DataTableHelper.GetIntValue(row, "completion_tokens");
  obj.TokensPerSecondOverall = DataTableHelper.GetDoubleValue(row, "tokens_per_second_overall");
  obj.TokensPerSecondGeneration = DataTableHelper.GetDoubleValue(row, "tokens_per_second_generation");
  ```

---

### Phase 3: Database CRUD — Add New Columns to INSERT Queries

Each database provider has a `ChatHistoryMethods.cs` with the `CreateAsync` method that builds the INSERT query.

- [x] **SQLite** — `src/AssistantHub.Core/Database/Sqlite/Implementations/ChatHistoryMethods.cs`
  - Add `completion_tokens`, `tokens_per_second_overall`, `tokens_per_second_generation` to the column list (after `prompt_tokens`) in the INSERT statement
  - Add `history.CompletionTokens`, `_Driver.FormatDouble(history.TokensPerSecondOverall)`, `_Driver.FormatDouble(history.TokensPerSecondGeneration)` to the VALUES list
- [x] **PostgreSQL** — `src/AssistantHub.Core/Database/Postgresql/Implementations/ChatHistoryMethods.cs`
  - Same change as SQLite
- [x] **SQL Server** — `src/AssistantHub.Core/Database/SqlServer/Implementations/ChatHistoryMethods.cs`
  - Same change as SQLite
- [x] **MySQL** — `src/AssistantHub.Core/Database/Mysql/Implementations/ChatHistoryMethods.cs`
  - Same change as SQLite

---

### Phase 4: Chat Handler — Compute and Pass All Telemetry to History Writer

#### Task 4.1: Update `WriteChatHistoryAsync` signature and body

- [x] **File:** `src/AssistantHub.Server/Handlers/ChatHandler.cs`
- [x] Add `int completionTokens` parameter to `WriteChatHistoryAsync` (line ~1200)
- [x] In the method body, after setting all fields on the `ChatHistory` object (after line ~1228), compute and set the TPS values:
  ```csharp
  history.CompletionTokens = completionTokens;

  // Compute tokens per second (overall): completion tokens / TTLT in seconds
  if (completionTokens > 0 && timeToLastTokenMs > 0)
      history.TokensPerSecondOverall = Math.Round(completionTokens / (timeToLastTokenMs / 1000.0), 2);

  // Compute tokens per second (generation only): completion tokens / (TTLT - TTFT) in seconds
  double generationMs = timeToLastTokenMs - timeToFirstTokenMs;
  if (completionTokens > 0 && generationMs > 0)
      history.TokensPerSecondGeneration = Math.Round(completionTokens / (generationMs / 1000.0), 2);
  ```

#### Task 4.2: Update non-streaming call site (line ~858)

- [x] In the non-streaming handler, compute completion tokens before calling `WriteChatHistoryAsync`:
  ```csharp
  int completionTokens = EstimateTokenCount(inferenceResult.Content);
  ```
- [x] Pass `completionTokens` as the new argument to `WriteChatHistoryAsync`

#### Task 4.3: Update streaming call site (line ~976)

- [x] In the streaming `onComplete` callback, compute completion tokens before calling `WriteChatHistoryAsync`:
  ```csharp
  int completionTokens = EstimateTokenCount(fullContent);
  ```
  Note: `EstimateTokenCount(fullContent)` is already computed a few lines later as `finishCompletionTokens`; move or reuse this value.
- [x] Pass `completionTokens` as the new argument to `WriteChatHistoryAsync`

---

### Phase 5: Frontend — Tokens Per Second Display (Redundant Calculation)

The frontend also computes TPS from the stored timing/token fields. This provides a fallback for pre-migration records that have timing data but no stored TPS, and serves as a visual cross-check.

#### Task 5.1: Add TPS computations in `HistoryViewModal.jsx`

- [x] **File:** `dashboard/src/components/modals/HistoryViewModal.jsx`
- [x] Add a helper function at the top of the file:
  ```jsx
  function formatTps(tps) {
    if (!tps || tps <= 0 || !isFinite(tps)) return 'N/A';
    return tps.toFixed(1) + ' tok/s';
  }
  ```
- [x] Inside the `HistoryViewModal` component, after the existing `tokenGenMs` computation (line ~60), add:
  ```jsx
  // Use backend-stored TPS if available, otherwise compute from raw fields
  const overallTps = history.TokensPerSecondOverall > 0
    ? history.TokensPerSecondOverall
    : (history.CompletionTokens > 0 && history.TimeToLastTokenMs > 0
        ? (history.CompletionTokens / (history.TimeToLastTokenMs / 1000))
        : 0);

  const generationTps = history.TokensPerSecondGeneration > 0
    ? history.TokensPerSecondGeneration
    : (history.CompletionTokens > 0 && tokenGenMs > 0
        ? (history.CompletionTokens / (tokenGenMs / 1000))
        : 0);
  ```

#### Task 5.2: Display TPS metrics in the summary metrics row

- [x] In the `history-metrics-row` section (lines ~139-156), add three new metric items after the existing "Prompt Tokens" metric:
  ```jsx
  <div className="history-metric">
    <span className="history-metric-label">
      <Tooltip text="Estimated completion tokens in the assistant's response">Completion Tokens</Tooltip>
    </span>
    <span className="history-metric-value">
      {history.CompletionTokens > 0 ? `~${history.CompletionTokens.toLocaleString()}` : 'N/A'}
    </span>
  </div>
  <div className="history-metric">
    <span className="history-metric-label">
      <Tooltip text="Tokens per second — completion tokens divided by total time from prompt sent to last token (TTLT)">TPS (Overall)</Tooltip>
    </span>
    <span className="history-metric-value">{formatTps(overallTps)}</span>
  </div>
  <div className="history-metric">
    <span className="history-metric-label">
      <Tooltip text="Tokens per second — completion tokens divided by generation time (first token to last token)">TPS (Generation)</Tooltip>
    </span>
    <span className="history-metric-value">{formatTps(generationTps)}</span>
  </div>
  ```

---

### Phase 6: Frontend — Identifiers on Separate Rows

#### Task 6.1: Update CSS for identifiers layout

- [x] **File:** `dashboard/src/App.css`
- [x] Modify `.history-ids-row` (line ~2385) to stack vertically:
  ```css
  .history-ids-row {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    padding: 0.5rem 0 0.75rem;
    border-bottom: 1px solid var(--border-color);
    margin-bottom: 1rem;
  }
  ```
  Key change: replace `flex-wrap: wrap` with `flex-direction: column` and reduce gap from `0.75rem 1.5rem` to `0.4rem`.

#### Task 6.2: No JSX changes needed

The existing JSX structure (each identifier is already in its own `<div className="history-id-item">`) will automatically stack vertically with the CSS change above.

---

### Phase 7: Frontend — Modal 95% Viewport Sizing

#### Task 7.1: Add a new CSS class for full-screen modal

- [x] **File:** `dashboard/src/App.css`
- [x] Add a new modifier class after `.modal-container.extra-wide` (line ~669):
  ```css
  .modal-container.fullscreen {
    max-width: 95vw;
    max-height: 95vh;
    width: 95vw;
  }
  ```

#### Task 7.2: Update Modal component to accept `fullscreen` prop

- [x] **File:** `dashboard/src/components/Modal.jsx`
- [x] Add `fullscreen` to the destructured props (line 3):
  ```jsx
  function Modal({ title, onClose, children, footer, wide, extraWide, fullscreen, className }) {
  ```
- [x] Update the className logic (line 19):
  ```jsx
  <div className={`modal-container ${fullscreen ? 'fullscreen' : extraWide ? 'extra-wide' : wide ? 'wide' : ''}${className ? ' ' + className : ''}`}>
  ```

#### Task 7.3: Use `fullscreen` in HistoryViewModal

- [x] **File:** `dashboard/src/components/modals/HistoryViewModal.jsx`
- [x] Change `extraWide` to `fullscreen` on the Modal usage (line ~63):
  ```jsx
  <Modal title="History Details" onClose={onClose} fullscreen footer={
  ```

---

### Phase 8: Docker / Factory Database Files

#### Task 8.1: Update factory SQLite database

- [x] **File:** `docker/factory/assistanthub.db`
- [x] Run the migration against the factory database to add all three new columns:
  ```bash
  sqlite3 docker/factory/assistanthub.db "ALTER TABLE chat_history ADD COLUMN completion_tokens INTEGER DEFAULT 0;"
  sqlite3 docker/factory/assistanthub.db "ALTER TABLE chat_history ADD COLUMN tokens_per_second_overall REAL DEFAULT 0;"
  sqlite3 docker/factory/assistanthub.db "ALTER TABLE chat_history ADD COLUMN tokens_per_second_generation REAL DEFAULT 0;"
  ```
- [x] Verify with: `sqlite3 docker/factory/assistanthub.db ".schema chat_history"`

#### Task 8.2: Update live/dev SQLite database (if applicable)

- [x] If `docker/assistanthub/data/` contains a live `assistanthub.db`, the application's auto-migration will handle it on next startup. No manual action needed, but verify after restarting.

---

### Phase 9: Documentation Updates

#### Task 9.1: Update `REST_API.md`

- [x] **File:** `REST_API.md`
- [x] In the `GET /v1.0/history/{historyId}` response example (line ~1612), add new fields after `"PromptTokens": 1250,`:
  ```json
  "CompletionTokens": 87,
  "TokensPerSecondOverall": 97.65,
  "TokensPerSecondGeneration": 145.00,
  ```
- [x] In the field descriptions table (line ~1636), add three rows after `PromptTokens`:

  | Field | Type | Description |
  |---|---|---|
  | `CompletionTokens` | int | Estimated completion token count from the model's response. |
  | `TokensPerSecondOverall` | double | Tokens per second (overall): CompletionTokens / (TimeToLastTokenMs / 1000). End-to-end throughput from prompt sent to last token. |
  | `TokensPerSecondGeneration` | double | Tokens per second (generation only): CompletionTokens / ((TimeToLastTokenMs - TimeToFirstTokenMs) / 1000). Pure generation throughput excluding prompt processing. |

#### Task 9.2: Update `openapi.json`

- [x] **File:** `openapi.json`
- [x] Add all three new fields to the `ChatHistory` schema:
  - `CompletionTokens` — integer, default 0
  - `TokensPerSecondOverall` — number (double), default 0
  - `TokensPerSecondGeneration` — number (double), default 0

#### Task 9.3: Update `README.md` (if applicable)

- [x] **File:** `README.md`
- [x] If the README mentions history fields or dashboard features, add a mention of tokens-per-second metrics and completion tokens tracking. If it doesn't go into this level of detail, no change needed.

---

### Phase 10: Postman Collection Update

#### Task 10.1: Update Postman collection response examples

- [x] **File:** `postman/` (check for collection JSON files)
- [x] If the Postman collection includes example responses for history endpoints, add `CompletionTokens`, `TokensPerSecondOverall`, and `TokensPerSecondGeneration` fields to those examples.

---

## Summary of Files to Modify

| # | File | Change |
|---|------|--------|
| 1 | `migrations/006_add_completion_tokens_and_tps.sql` | **NEW** — Migration script (3 new columns) |
| 2 | `src/AssistantHub.Core/Database/Sqlite/SqliteDriver.cs` | Auto-migration for 3 new columns |
| 3 | `src/AssistantHub.Core/Database/Postgresql/PostgresqlDriver.cs` | Auto-migration for 3 new columns |
| 4 | `src/AssistantHub.Core/Database/SqlServer/SqlServerDriver.cs` | Auto-migration for 3 new columns |
| 5 | `src/AssistantHub.Core/Database/Mysql/MysqlDriver.cs` | Auto-migration for 3 new columns |
| 6 | `src/AssistantHub.Core/Models/ChatHistory.cs` | Add `CompletionTokens`, `TokensPerSecondOverall`, `TokensPerSecondGeneration` properties + `FromDataRow` |
| 7 | `src/AssistantHub.Core/Database/Sqlite/Implementations/ChatHistoryMethods.cs` | Add 3 columns to INSERT |
| 8 | `src/AssistantHub.Core/Database/Postgresql/Implementations/ChatHistoryMethods.cs` | Add 3 columns to INSERT |
| 9 | `src/AssistantHub.Core/Database/SqlServer/Implementations/ChatHistoryMethods.cs` | Add 3 columns to INSERT |
| 10 | `src/AssistantHub.Core/Database/Mysql/Implementations/ChatHistoryMethods.cs` | Add 3 columns to INSERT |
| 11 | `src/AssistantHub.Server/Handlers/ChatHandler.cs` | Compute completion tokens + TPS, pass to history writer |
| 12 | `dashboard/src/components/modals/HistoryViewModal.jsx` | TPS display (with frontend fallback calc), use `fullscreen` |
| 13 | `dashboard/src/components/Modal.jsx` | Add `fullscreen` prop |
| 14 | `dashboard/src/App.css` | Identifiers vertical layout + fullscreen modal class |
| 15 | `docker/factory/assistanthub.db` | Run migration to add 3 columns |
| 16 | `REST_API.md` | Add 3 new fields to docs |
| 17 | `openapi.json` | Add 3 new fields to schema |
| 18 | `postman/` collection file(s) | Update example responses |

---

## Data Flow Summary

```
Chat Request
  └─► ChatHandler (streaming or non-streaming)
        ├─► EstimateTokenCount(response) → completionTokens
        └─► WriteChatHistoryAsync(... completionTokens)
              ├─► history.CompletionTokens = completionTokens
              ├─► history.TokensPerSecondOverall = completionTokens / (TTLT_ms / 1000)
              ├─► history.TokensPerSecondGeneration = completionTokens / ((TTLT_ms - TTFT_ms) / 1000)
              └─► Database INSERT (all 3 new fields persisted)

Dashboard → GET /v1.0/history/{id}
  └─► Returns all fields including CompletionTokens, TokensPerSecondOverall, TokensPerSecondGeneration
        └─► HistoryViewModal
              ├─► Uses stored TPS values if > 0
              └─► Falls back to computing from CompletionTokens + timing fields (for pre-migration records)
```

---

## Testing Checklist

- [x] Application starts without errors (auto-migration runs successfully for all 3 new columns)
- [x] New chat requests populate `completion_tokens`, `tokens_per_second_overall`, and `tokens_per_second_generation` in the database
- [x] Verify stored TPS values match expected calculations: manually check `completion_tokens / (time_to_last_token_ms / 1000)` and `completion_tokens / ((time_to_last_token_ms - time_to_first_token_ms) / 1000)`
- [x] Existing history records show `CompletionTokens: 0` and TPS displays "N/A" (graceful fallback)
- [x] History details modal shows identifiers each on their own row
- [x] History details modal consumes ~95% of viewport width and height
- [x] TPS (Overall) and TPS (Generation) metrics display correct values in the modal
- [x] Completion Tokens metric displays in the summary row
- [x] Non-streaming responses correctly persist all three new fields
- [x] Streaming responses correctly persist all three new fields
- [x] All four database providers (SQLite, PostgreSQL, SQL Server, MySQL) work with the new columns
- [x] REST API `GET /v1.0/history/{id}` response includes `CompletionTokens`, `TokensPerSecondOverall`, `TokensPerSecondGeneration` in JSON output
- [x] Factory database includes the three new columns for fresh deployments
- [x] Frontend fallback calculation works for records where TPS columns are 0 but timing/token data exists
