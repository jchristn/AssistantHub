# Neighbor Chunk Retrieval — Implementation Plan

> **Status: COMPLETED** — All phases implemented on 2026-02-24.

## Overview

RecallDB now supports an `IncludeNeighbors` parameter on search queries. When set to `N`, each matched chunk in the search results will have its `Neighbors` property populated with up to `N` chunks before and `N` chunks after the matched chunk's position within the same document. This provides surrounding context for each match, which is critical for RAG pipelines where isolated chunks lose meaning without their adjacent content.

**AssistantHub integration**: Expose a new per-assistant setting `RetrievalIncludeNeighbors` that controls how many neighboring chunks to request from RecallDB during retrieval. When building the system prompt, neighbor content is **merged** with the matched chunk's text to produce a single, seamless context block per match (neighbors before prepended, neighbors after appended).

**Non-goal**: Neighbors do not affect scoring, filtering, pagination, or citation indexing. They are a context-enrichment step only. Each matched chunk still maps to one citation source.

---

## Phase 1: Core Model — `AssistantSettings`

### 1.1 Add `RetrievalIncludeNeighbors` property

- [x] **File**: `src/AssistantHub.Core/Models/AssistantSettings.cs` — DONE
- [x] Add property with validated backing field (Math.Clamp 0–10) — DONE
- [x] Add private backing field: `private int _RetrievalIncludeNeighbors = 0;` — DONE
- [x] Add to `FromDataRow`: `obj.RetrievalIncludeNeighbors = DataTableHelper.GetIntValue(row, "retrieval_include_neighbors", 0);` — DONE

### 1.2 Add `IncludeNeighbors` to `RetrievalSearchOptions`

- [x] **File**: `src/AssistantHub.Core/Models/RetrievalSearchOptions.cs` — DONE
- [x] Add property: `public int IncludeNeighbors { get; set; } = 0;` — DONE

---

## Phase 2: Core Model — `RetrievalChunk`

### 2.1 Add `Neighbors` to `RetrievalChunk`

- [x] **File**: `src/AssistantHub.Core/Models/RetrievalChunk.cs` — DONE
- [x] Add `Neighbors` property (`List<RetrievalChunk>`, default `null`) — DONE

### 2.2 Add `MergedContent` helper property

- [x] **File**: `src/AssistantHub.Core/Models/RetrievalChunk.cs` — DONE
- [x] Add computed `[JsonIgnore]` property that interleaves neighbors + matched chunk by Position ascending, producing a seamless reading-order context block — DONE

### 2.3 Add `Position` to `RetrievalChunk` for precise ordering

- [x] **File**: `src/AssistantHub.Core/Models/RetrievalChunk.cs` — DONE
- [x] Add property: `public int? Position { get; set; } = null;` — DONE
- [x] `MergedContent` sorts all chunks (neighbors + match) by Position for true positional reading order — DONE

---

## Phase 3: Retrieval Service — Pass `IncludeNeighbors` to RecallDB

### 3.1 Update `SearchResult` inner class to capture neighbors

- [x] **File**: `src/AssistantHub.Core/Services/RetrievalService.cs` — DONE
- [x] Add `Neighbors` property (`List<SearchResult>`) to private `SearchResult` class — DONE
- [x] Add `Position` property (`int?`) to `SearchResult` — DONE

### 3.2 Update `BuildSearchBody` to include `IncludeNeighbors`

- [x] **File**: `src/AssistantHub.Core/Services/RetrievalService.cs` — DONE
- [x] Add `IncludeNeighbors` to all three search mode branches (Vector, FullText, Hybrid) — DONE
- [x] Add `IncludeNeighbors` to the hybrid-fallback vector-only body — DONE
- [x] Uses conditional: `options.IncludeNeighbors > 0 ? options.IncludeNeighbors : (int?)null` so null is omitted by JsonSerializer — DONE

### 3.3 Map neighbors into `RetrievalChunk` results

- [x] **File**: `src/AssistantHub.Core/Services/RetrievalService.cs` — DONE
- [x] In the results-mapping loop, map `result.Neighbors` into `RetrievalChunk.Neighbors` with DocumentId, Content, Position — DONE
- [x] Map `result.Position` into `RetrievalChunk.Position` — DONE

---

## Phase 4: Chat Handler — Use Merged Content in Prompt

### 4.1 Update streaming chat path (first `RetrieveAsync` call)

- [x] **File**: `src/AssistantHub.Server/Handlers/ChatHandler.cs` — DONE
- [x] Add `IncludeNeighbors = settings.RetrievalIncludeNeighbors` to the `RetrievalSearchOptions` — DONE

### 4.2 Update context chunk extraction to use merged content

- [x] **File**: `src/AssistantHub.Server/Handlers/ChatHandler.cs` — DONE
- [x] Changed `retrievalChunks.Select(c => c.Content)` to `retrievalChunks.Select(c => c.MergedContent)` — DONE

### 4.3 Update non-streaming chat path (second `RetrieveAsync` call)

- [x] **File**: `src/AssistantHub.Server/Handlers/ChatHandler.cs` — DONE
- [x] Add `IncludeNeighbors = settings.RetrievalIncludeNeighbors` to the `RetrievalSearchOptions` — DONE
- [x] Changed content extraction from `c.Content` to `c.MergedContent` — DONE

---

## Phase 5: Database Schema — Add Column

### 5.1 Update `TableQueries.cs` for all four database providers

- [x] **File**: `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs` — DONE (`retrieval_include_neighbors INTEGER NOT NULL DEFAULT 0`)
- [x] **File**: `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs` — DONE (`retrieval_include_neighbors INTEGER NOT NULL DEFAULT 0`)
- [x] **File**: `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs` — DONE (`retrieval_include_neighbors INT NOT NULL DEFAULT 0`)
- [x] **File**: `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs` — DONE (`retrieval_include_neighbors INT NOT NULL DEFAULT 0`)

### 5.2 Update `AssistantSettingsMethods.cs` for all four database providers

- [x] **File**: `src/AssistantHub.Core/Database/Sqlite/Implementations/AssistantSettingsMethods.cs` — DONE (INSERT + UPDATE)
- [x] **File**: `src/AssistantHub.Core/Database/Postgresql/Implementations/AssistantSettingsMethods.cs` — DONE (INSERT + UPDATE)
- [x] **File**: `src/AssistantHub.Core/Database/SqlServer/Implementations/AssistantSettingsMethods.cs` — DONE (INSERT + UPDATE)
- [x] **File**: `src/AssistantHub.Core/Database/Mysql/Implementations/AssistantSettingsMethods.cs` — DONE (INSERT + UPDATE)

### 5.3 Create migration file

- [x] **File**: `migrations/008_add_retrieval_include_neighbors.sql` — DONE (SQLite, PostgreSQL, SQL Server, MySQL)

---

## Phase 6: Settings Handler — Validation

### 6.1 Add validation/clamping in `PutSettingsAsync`

- [x] **File**: `src/AssistantHub.Server/Handlers/AssistantSettingsHandler.cs` — DONE
- [x] Added `updated.RetrievalIncludeNeighbors = Math.Clamp(updated.RetrievalIncludeNeighbors, 0, 10);` after the TextWeight clamp — DONE

---

## Phase 7: Dashboard — Settings UI

### 7.1 Add `RetrievalIncludeNeighbors` to settings state

- [x] **File**: `dashboard/src/views/AssistantSettingsView.jsx` — DONE
- [x] Added `RetrievalIncludeNeighbors: result?.RetrievalIncludeNeighbors ?? 0` to `loadSettings` — DONE

### 7.2 Add input field in the Retrieval (RAG) section

- [x] **File**: `dashboard/src/views/AssistantSettingsView.jsx` — DONE
- [x] Added numeric input (min 0, max 10) alongside Top K and Score Threshold in the same form-row — DONE
- [x] Includes tooltip explaining the feature — DONE

---

## Phase 8: Postman Collection

### 8.1 Update assistant settings request examples

- [x] **File**: `postman/AssistantHub.postman_collection.json` — DONE
- [x] Added `"RetrievalIncludeNeighbors": 2` to the "Update Assistant Settings" PUT request body — DONE

---

## Phase 9: Documentation

### 9.1 REST_API.md

- [x] **File**: `REST_API.md` — DONE
- [x] Added `RetrievalIncludeNeighbors` row to the Assistant Settings field reference table — DONE
- [x] Added `"RetrievalIncludeNeighbors": 0` to the GET response example — DONE
- [x] Added `"RetrievalIncludeNeighbors": 2` to the PUT request body example — DONE

### 9.2 README.md

- [x] **File**: `README.md` — DONE
- [x] Added feature bullet: "**Neighbor chunk retrieval** -- Optionally retrieve surrounding chunks for each search match to provide broader document context to the model, configurable per assistant (0–10 neighbors)" — DONE

---

## File Change Summary

| File | Change Type | Phase | Status |
|------|------------|-------|--------|
| `src/AssistantHub.Core/Models/AssistantSettings.cs` | Add property + FromDataRow | 1.1 | DONE |
| `src/AssistantHub.Core/Models/RetrievalSearchOptions.cs` | Add property | 1.2 | DONE |
| `src/AssistantHub.Core/Models/RetrievalChunk.cs` | Add Neighbors, MergedContent, Position | 2.1–2.3 | DONE |
| `src/AssistantHub.Core/Services/RetrievalService.cs` | Pass IncludeNeighbors, map neighbors | 3.1–3.3 | DONE |
| `src/AssistantHub.Server/Handlers/ChatHandler.cs` | Use MergedContent, pass setting | 4.1–4.3 | DONE |
| `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs` | Add column | 5.1 | DONE |
| `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs` | Add column | 5.1 | DONE |
| `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs` | Add column | 5.1 | DONE |
| `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs` | Add column | 5.1 | DONE |
| `src/AssistantHub.Core/Database/Sqlite/Implementations/AssistantSettingsMethods.cs` | Add to INSERT/UPDATE | 5.2 | DONE |
| `src/AssistantHub.Core/Database/Postgresql/Implementations/AssistantSettingsMethods.cs` | Add to INSERT/UPDATE | 5.2 | DONE |
| `src/AssistantHub.Core/Database/SqlServer/Implementations/AssistantSettingsMethods.cs` | Add to INSERT/UPDATE | 5.2 | DONE |
| `src/AssistantHub.Core/Database/Mysql/Implementations/AssistantSettingsMethods.cs` | Add to INSERT/UPDATE | 5.2 | DONE |
| `migrations/008_add_retrieval_include_neighbors.sql` | New migration file | 5.3 | DONE |
| `src/AssistantHub.Server/Handlers/AssistantSettingsHandler.cs` | Add validation | 6.1 | DONE |
| `dashboard/src/views/AssistantSettingsView.jsx` | Add UI input | 7.1–7.2 | DONE |
| `postman/AssistantHub.postman_collection.json` | Update examples | 8.1 | DONE |
| `REST_API.md` | Add field docs | 9.1 | DONE |
| `README.md` | Add feature mention | 9.2 | DONE |

## Notes

- **No breaking changes.** `RetrievalIncludeNeighbors` defaults to `0` (no neighbors). Existing API consumers and saved assistant settings are unaffected.
- **RecallDB compatibility.** The `IncludeNeighbors` parameter is ignored by older RecallDB versions that don't support it (unrecognized JSON fields are ignored). The `Neighbors` property on response documents will simply be null/absent.
- **Context window consideration.** When neighbors are enabled, each matched chunk's context block grows larger. With `IncludeNeighbors: 3` and 256-token chunks, each match could contribute up to 7 x 256 = 1,792 tokens. Combined with `RetrievalTopK: 10`, that's up to ~18K tokens of context. The existing conversation compaction mechanism in ChatHandler should handle this, but users should be aware of the context window budget.
- **Citation mapping is unchanged.** Each matched chunk still maps to one citation source. Neighbor content enriches the context block but does not create additional citations.
- **Deduplication consideration.** When two matched chunks from the same document are close together, their neighbor windows may overlap. The merged content for each match will include some of the same text. This is acceptable — the LLM sees slightly redundant context but each citation remains correctly scoped. RecallDB handles the deduplication at the query level (merging position ranges), so we don't fetch duplicate rows.
- **Build verified**: AssistantHub.Core and AssistantHub.Server compile successfully with 0 warnings, 0 errors.
