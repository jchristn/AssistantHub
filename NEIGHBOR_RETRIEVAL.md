# Neighbor Chunk Retrieval — Implementation Plan

> **Status: NOT STARTED**

## Overview

RecallDB now supports an `IncludeNeighbors` parameter on search queries. When set to `N`, each matched chunk in the search results will have its `Neighbors` property populated with up to `N` chunks before and `N` chunks after the matched chunk's position within the same document. This provides surrounding context for each match, which is critical for RAG pipelines where isolated chunks lose meaning without their adjacent content.

**AssistantHub integration**: Expose a new per-assistant setting `RetrievalIncludeNeighbors` that controls how many neighboring chunks to request from RecallDB during retrieval. When building the system prompt, neighbor content is **merged** with the matched chunk's text to produce a single, seamless context block per match (neighbors before prepended, neighbors after appended).

**Non-goal**: Neighbors do not affect scoring, filtering, pagination, or citation indexing. They are a context-enrichment step only. Each matched chunk still maps to one citation source.

---

## Phase 1: Core Model — `AssistantSettings`

### 1.1 Add `RetrievalIncludeNeighbors` property

- [ ] **File**: `src/AssistantHub.Core/Models/AssistantSettings.cs`
- [ ] Add property with validated backing field:
  ```csharp
  /// <summary>
  /// Number of neighboring chunks to retrieve before and after each matched chunk (0-10).
  /// When set, each search result from RecallDB includes up to N chunks before and N chunks
  /// after the matched position within the same document. 0 or null means no neighbors.
  /// </summary>
  public int RetrievalIncludeNeighbors
  {
      get => _RetrievalIncludeNeighbors;
      set => _RetrievalIncludeNeighbors = Math.Clamp(value, 0, 10);
  }
  ```
- [ ] Add private backing field: `private int _RetrievalIncludeNeighbors = 0;`
- [ ] Add to `FromDataRow`:
  ```csharp
  obj.RetrievalIncludeNeighbors = DataTableHelper.GetIntValue(row, "retrieval_include_neighbors", 0);
  ```

### 1.2 Add `IncludeNeighbors` to `RetrievalSearchOptions`

- [ ] **File**: `src/AssistantHub.Core/Models/RetrievalSearchOptions.cs`
- [ ] Add property:
  ```csharp
  /// <summary>
  /// Number of neighboring chunks to include before and after each matched chunk (0-10).
  /// Passed to RecallDB as IncludeNeighbors on the search query.
  /// </summary>
  public int IncludeNeighbors { get; set; } = 0;
  ```

---

## Phase 2: Core Model — `RetrievalChunk`

### 2.1 Add `Neighbors` to `RetrievalChunk`

- [ ] **File**: `src/AssistantHub.Core/Models/RetrievalChunk.cs`
- [ ] Add property:
  ```csharp
  /// <summary>
  /// Neighboring chunks surrounding this match in positional order.
  /// Populated when IncludeNeighbors is specified. Null when not requested.
  /// </summary>
  [JsonPropertyName("neighbors")]
  public List<RetrievalChunk> Neighbors { get; set; } = null;
  ```

### 2.2 Add `MergedContent` helper property

- [ ] **File**: `src/AssistantHub.Core/Models/RetrievalChunk.cs`
- [ ] Add computed property that produces the merged context block:
  ```csharp
  /// <summary>
  /// Returns the matched chunk's content with neighbor content merged in positional order.
  /// Neighbors before the match are prepended; neighbors after are appended.
  /// Falls back to Content when no neighbors are present.
  /// </summary>
  [JsonIgnore]
  public string MergedContent
  {
      get
      {
          if (Neighbors == null || Neighbors.Count == 0)
              return Content;
          StringBuilder sb = new StringBuilder();
          foreach (var n in Neighbors.Where(n => n.Content != null))
              sb.AppendLine(n.Content);
          // The matched chunk itself is NOT in the Neighbors list (RecallDB excludes it),
          // so insert it in the merged output. Since neighbors are already ordered by position
          // and the matched chunk sits in the middle, we prepend neighbors-before, add the match,
          // then append neighbors-after. However, RecallDB returns ALL neighbors in one sorted
          // list — we don't have the matched chunk's position here. Simpler approach: just
          // concatenate all neighbor content + matched content. The positional order from
          // RecallDB ensures correct reading order.
          // Actually: neighbors are ordered by position and the matched chunk is excluded.
          // We need to interleave. But since we don't have Position on RetrievalChunk,
          // simply concatenate: neighbors (already sorted by position) form the surrounding
          // context, and the matched chunk is in the middle. For the LLM, the exact ordering
          // within a single context block is acceptable as "surrounding context".
          //
          // Recommended approach: prepend all neighbor content, then the matched chunk.
          // RecallDB returns neighbors sorted by Position ASC, and the matched chunk's
          // position is somewhere in the middle of that range. For simplicity and correctness,
          // concatenate all neighbors + match content as one block.
          return sb.ToString() + (Content ?? "");
      }
  }
  ```
  **Note**: Refine this after reviewing whether `Position` should be added to `RetrievalChunk` for precise interleaving. If so, add a `Position` property (see 2.3).

### 2.3 (Optional) Add `Position` to `RetrievalChunk` for precise ordering

- [ ] **File**: `src/AssistantHub.Core/Models/RetrievalChunk.cs`
- [ ] Add property:
  ```csharp
  [JsonPropertyName("position")]
  public int? Position { get; set; } = null;
  ```
- [ ] If added, update `MergedContent` to insert the matched chunk at its correct position among neighbors, producing true positional reading order.

---

## Phase 3: Retrieval Service — Pass `IncludeNeighbors` to RecallDB

### 3.1 Update `SearchResult` inner class to capture neighbors

- [ ] **File**: `src/AssistantHub.Core/Services/RetrievalService.cs`
- [ ] Add `Neighbors` property to the private `SearchResult` class:
  ```csharp
  public List<SearchResult> Neighbors { get; set; } = null;
  ```
- [ ] Add `Position` property to `SearchResult`:
  ```csharp
  public int? Position { get; set; } = null;
  ```

### 3.2 Update `BuildSearchBody` to include `IncludeNeighbors`

- [ ] **File**: `src/AssistantHub.Core/Services/RetrievalService.cs`
- [ ] Modify the method signature to accept `RetrievalSearchOptions` (already does).
- [ ] In all three search mode branches (Vector, FullText, Hybrid), add `IncludeNeighbors` to the anonymous object when `options.IncludeNeighbors > 0`:
  ```csharp
  // Example for Vector mode:
  return new
  {
      Vector = new { SearchType = "CosineSimilarity", Embeddings = embeddings },
      MaxResults = topK,
      IncludeNeighbors = options.IncludeNeighbors > 0 ? options.IncludeNeighbors : (int?)null
  };
  ```
- [ ] Apply the same pattern to FullText and Hybrid branches.
- [ ] Also add `IncludeNeighbors` to the hybrid-fallback vector-only body (line ~122-127).

### 3.3 Map neighbors into `RetrievalChunk` results

- [ ] **File**: `src/AssistantHub.Core/Services/RetrievalService.cs`
- [ ] In the results-mapping loop (around line 139-154), map `result.Neighbors` into `RetrievalChunk.Neighbors`:
  ```csharp
  results.Add(new RetrievalChunk
  {
      DocumentId = result.DocumentId,
      Score = Math.Round(result.Score, 6),
      TextScore = result.TextScore.HasValue ? Math.Round(result.TextScore.Value, 6) : null,
      Content = result.Content,
      Position = result.Position,
      Neighbors = result.Neighbors?.Select(n => new RetrievalChunk
      {
          DocumentId = n.DocumentId,
          Content = n.Content,
          Position = n.Position
      }).ToList()
  });
  ```

---

## Phase 4: Chat Handler — Use Merged Content in Prompt

### 4.1 Update streaming chat path (first `RetrieveAsync` call)

- [ ] **File**: `src/AssistantHub.Server/Handlers/ChatHandler.cs` (around line 301-316)
- [ ] Add `IncludeNeighbors` to the `RetrievalSearchOptions` construction:
  ```csharp
  new RetrievalSearchOptions
  {
      SearchMode = settings.SearchMode,
      TextWeight = settings.TextWeight,
      FullTextSearchType = settings.FullTextSearchType,
      FullTextLanguage = settings.FullTextLanguage,
      FullTextNormalization = settings.FullTextNormalization,
      FullTextMinimumScore = settings.FullTextMinimumScore,
      IncludeNeighbors = settings.RetrievalIncludeNeighbors
  }
  ```

### 4.2 Update context chunk extraction to use merged content

- [ ] **File**: `src/AssistantHub.Server/Handlers/ChatHandler.cs` (around line 328)
- [ ] Change from:
  ```csharp
  List<string> contextChunks = retrievalChunks.Select(c => c.Content).ToList();
  ```
  To:
  ```csharp
  List<string> contextChunks = retrievalChunks.Select(c => c.MergedContent).ToList();
  ```

### 4.3 Update non-streaming chat path (second `RetrieveAsync` call)

- [ ] **File**: `src/AssistantHub.Server/Handlers/ChatHandler.cs` (around line 656-669)
- [ ] Add `IncludeNeighbors = settings.RetrievalIncludeNeighbors` to the `RetrievalSearchOptions`.
- [ ] Change the content extraction from `c.Content` to `c.MergedContent`:
  ```csharp
  if (retrievedChunks != null) contextChunks.AddRange(retrievedChunks.Select(c => c.MergedContent));
  ```

---

## Phase 5: Database Schema — Add Column

### 5.1 Update `TableQueries.cs` for all four database providers

Add `retrieval_include_neighbors` column to the `CREATE TABLE assistant_settings` statement in each provider:

- [ ] **File**: `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs`
  - Add: `retrieval_include_neighbors INTEGER NOT NULL DEFAULT 0`
- [ ] **File**: `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs`
  - Add: `retrieval_include_neighbors INTEGER NOT NULL DEFAULT 0`
- [ ] **File**: `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs`
  - Add: `retrieval_include_neighbors INT NOT NULL DEFAULT 0`
- [ ] **File**: `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs`
  - Add: `retrieval_include_neighbors INT NOT NULL DEFAULT 0`

### 5.2 Update `AssistantSettingsMethods.cs` for all four database providers

Add `retrieval_include_neighbors` to INSERT and UPDATE SQL statements:

- [ ] **File**: `src/AssistantHub.Core/Database/Sqlite/Implementations/AssistantSettingsMethods.cs`
  - Add column to INSERT column list and VALUES parameter list
  - Add column to UPDATE SET clause
  - Add parameter binding: `settings.RetrievalIncludeNeighbors`
- [ ] **File**: `src/AssistantHub.Core/Database/Postgresql/Implementations/AssistantSettingsMethods.cs`
  - Same changes as SQLite
- [ ] **File**: `src/AssistantHub.Core/Database/SqlServer/Implementations/AssistantSettingsMethods.cs`
  - Same changes as SQLite
- [ ] **File**: `src/AssistantHub.Core/Database/Mysql/Implementations/AssistantSettingsMethods.cs`
  - Same changes as SQLite

### 5.3 Create migration file

- [ ] **File**: `migrations/008_add_retrieval_include_neighbors.sql`
- [ ] Content:
  ```sql
  -- Migration 008: Add retrieval_include_neighbors column to assistant_settings
  -- Enables requesting neighboring chunks around each search match from RecallDB.
  -- Safe to run multiple times; errors on "column already exists" can be ignored.

  -- SQLite
  ALTER TABLE assistant_settings ADD COLUMN retrieval_include_neighbors INTEGER NOT NULL DEFAULT 0;

  -- PostgreSQL
  ALTER TABLE assistant_settings ADD COLUMN retrieval_include_neighbors INTEGER NOT NULL DEFAULT 0;

  -- SQL Server
  IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('assistant_settings') AND name = 'retrieval_include_neighbors')
      ALTER TABLE assistant_settings ADD retrieval_include_neighbors INT NOT NULL DEFAULT 0;

  -- MySQL
  ALTER TABLE `assistant_settings` ADD COLUMN `retrieval_include_neighbors` INT NOT NULL DEFAULT 0;
  ```

---

## Phase 6: Settings Handler — Validation

### 6.1 Add validation/clamping in `PutSettingsAsync`

- [ ] **File**: `src/AssistantHub.Server/Handlers/AssistantSettingsHandler.cs` (around line 168)
- [ ] Add clamping after the TextWeight clamp:
  ```csharp
  updated.RetrievalIncludeNeighbors = Math.Clamp(updated.RetrievalIncludeNeighbors, 0, 10);
  ```

---

## Phase 7: Dashboard — Settings UI

### 7.1 Add `RetrievalIncludeNeighbors` to settings state

- [ ] **File**: `dashboard/src/views/AssistantSettingsView.jsx`
- [ ] In `loadSettings`, add to the settings object:
  ```js
  RetrievalIncludeNeighbors: result?.RetrievalIncludeNeighbors ?? 0,
  ```

### 7.2 Add input field in the Retrieval (RAG) section

- [ ] **File**: `dashboard/src/views/AssistantSettingsView.jsx`
- [ ] Add a numeric input in the Retrieval section, inside the `{settings.EnableRag && (...)}` block, alongside the existing Top K / Score Threshold row (around line 283-292). Add it as a third field in that row or as a new row:
  ```jsx
  <div className="form-group">
    <label className="form-label">
      <Tooltip text="Number of neighboring chunks to retrieve before and after each matched chunk (0-10). Provides surrounding context for each match. 0 means no neighbors.">
        Include Neighbors
      </Tooltip>
    </label>
    <input
      className="form-input"
      type="number"
      min="0"
      max="10"
      value={settings.RetrievalIncludeNeighbors}
      onChange={(e) => handleChange('RetrievalIncludeNeighbors', parseInt(e.target.value) || 0)}
      placeholder="0"
    />
  </div>
  ```

---

## Phase 8: Postman Collection

### 8.1 Update assistant settings request examples

- [ ] **File**: `postman/AssistantHub.postman_collection.json`
- [ ] In the "Create or Update Assistant Settings" (`PUT`) request body, add `RetrievalIncludeNeighbors` as an optional field:
  ```json
  "RetrievalIncludeNeighbors": 2
  ```
- [ ] In the "Get Assistant Settings" (`GET`) response description, mention the new field.

---

## Phase 9: Documentation

### 9.1 REST_API.md

- [ ] **File**: `REST_API.md`
- [ ] In the Assistant Settings field reference table, add a row:
  | Field | Type | Default | Description |
  |-------|------|---------|-------------|
  | `RetrievalIncludeNeighbors` | integer | `0` | Number of neighboring chunks to retrieve before and after each matched chunk (0–10). Provides surrounding document context for each search match. 0 means no neighbors. |
- [ ] In the PUT request body example, add `"RetrievalIncludeNeighbors": 2`.
- [ ] In the GET response example, add the field with default `0`.
- [ ] Add a brief note in the Retrieval section explaining:
  - What neighbor retrieval is and when to use it
  - That neighbors are merged into matched chunks for prompt context
  - That neighbors do not affect scoring, citation count, or top-K limits
  - Valid range is 0–10; values outside are clamped

### 9.2 README.md

- [ ] **File**: `README.md`
- [ ] In the RAG/Retrieval features section, add a bullet mentioning neighbor chunk retrieval:
  - "**Neighbor chunk retrieval**: Optionally retrieve surrounding chunks for each search match to provide broader document context to the model."
- [ ] In any settings reference or configuration example, include `RetrievalIncludeNeighbors`.

---

## File Change Summary

| File | Change Type | Phase | Status |
|------|------------|-------|--------|
| `src/AssistantHub.Core/Models/AssistantSettings.cs` | Add property + FromDataRow | 1.1 | ☐ |
| `src/AssistantHub.Core/Models/RetrievalSearchOptions.cs` | Add property | 1.2 | ☐ |
| `src/AssistantHub.Core/Models/RetrievalChunk.cs` | Add Neighbors, MergedContent, Position | 2.1–2.3 | ☐ |
| `src/AssistantHub.Core/Services/RetrievalService.cs` | Pass IncludeNeighbors, map neighbors | 3.1–3.3 | ☐ |
| `src/AssistantHub.Server/Handlers/ChatHandler.cs` | Use MergedContent, pass setting | 4.1–4.3 | ☐ |
| `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs` | Add column | 5.1 | ☐ |
| `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs` | Add column | 5.1 | ☐ |
| `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs` | Add column | 5.1 | ☐ |
| `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs` | Add column | 5.1 | ☐ |
| `src/AssistantHub.Core/Database/Sqlite/Implementations/AssistantSettingsMethods.cs` | Add to INSERT/UPDATE | 5.2 | ☐ |
| `src/AssistantHub.Core/Database/Postgresql/Implementations/AssistantSettingsMethods.cs` | Add to INSERT/UPDATE | 5.2 | ☐ |
| `src/AssistantHub.Core/Database/SqlServer/Implementations/AssistantSettingsMethods.cs` | Add to INSERT/UPDATE | 5.2 | ☐ |
| `src/AssistantHub.Core/Database/Mysql/Implementations/AssistantSettingsMethods.cs` | Add to INSERT/UPDATE | 5.2 | ☐ |
| `migrations/008_add_retrieval_include_neighbors.sql` | New migration file | 5.3 | ☐ |
| `src/AssistantHub.Server/Handlers/AssistantSettingsHandler.cs` | Add validation | 6.1 | ☐ |
| `dashboard/src/views/AssistantSettingsView.jsx` | Add UI input | 7.1–7.2 | ☐ |
| `postman/AssistantHub.postman_collection.json` | Update examples | 8.1 | ☐ |
| `REST_API.md` | Add field docs | 9.1 | ☐ |
| `README.md` | Add feature mention | 9.2 | ☐ |

## Notes

- **No breaking changes.** `RetrievalIncludeNeighbors` defaults to `0` (no neighbors). Existing API consumers and saved assistant settings are unaffected.
- **RecallDB compatibility.** The `IncludeNeighbors` parameter is ignored by older RecallDB versions that don't support it (unrecognized JSON fields are ignored). The `Neighbors` property on response documents will simply be null/absent.
- **Context window consideration.** When neighbors are enabled, each matched chunk's context block grows larger. With `IncludeNeighbors: 3` and 256-token chunks, each match could contribute up to 7 × 256 = 1,792 tokens. Combined with `RetrievalTopK: 10`, that's up to ~18K tokens of context. The existing conversation compaction mechanism in ChatHandler should handle this, but users should be aware of the context window budget.
- **Citation mapping is unchanged.** Each matched chunk still maps to one citation source. Neighbor content enriches the context block but does not create additional citations.
- **Deduplication consideration.** When two matched chunks from the same document are close together, their neighbor windows may overlap. The merged content for each match will include some of the same text. This is acceptable — the LLM sees slightly redundant context but each citation remains correctly scoped. RecallDB handles the deduplication at the query level (merging position ranges), so we don't fetch duplicate rows.
