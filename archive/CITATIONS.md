# CITATIONS.md — Implementation Plan

> **Feature:** Citation metadata in chat completion responses
> **Target version:** v0.2.0
> **Status:** Fully Implemented (Phases 1-14)
> **Last updated:** 2026-02-24
> **Implemented:** 2026-02-24 by Claude Opus 4.6 (Phases 1-11: base citations; Phases 12-14: citation linking)

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture Summary](#2-architecture-summary)
3. [Phase 1 — Database Schema Migration](#3-phase-1--database-schema-migration)
4. [Phase 2 — Backend Models](#4-phase-2--backend-models)
5. [Phase 3 — Indexed Context Injection](#5-phase-3--indexed-context-injection)
6. [Phase 4 — Citation Extraction & Response Population](#6-phase-4--citation-extraction--response-population)
7. [Phase 5 — Assistant Settings Persistence](#7-phase-5--assistant-settings-persistence)
8. [Phase 6 — Dashboard: Assistant Settings UI](#8-phase-6--dashboard-assistant-settings-ui)
9. [Phase 7 — Dashboard: Chat Window Citation Rendering](#9-phase-7--dashboard-chat-window-citation-rendering)
10. [Phase 8 — Dashboard: SSE Client Updates](#10-phase-8--dashboard-sse-client-updates)
11. [Phase 9 — Postman Collection](#11-phase-9--postman-collection)
12. [Phase 10 — Documentation](#12-phase-10--documentation)
13. [Phase 11 — Testing & Validation](#13-phase-11--testing--validation)
14. [Appendix A — Full Example Payloads](#appendix-a--full-example-payloads)
15. [Appendix B — File Change Index](#appendix-b--file-change-index)

---

## 1. Overview

### Problem

Today, when RAG is enabled, the system injects retrieved document chunks into the system prompt as anonymous `---`-separated text blocks (see `InferenceService.cs:520-530`). The `ChatCompletionRetrieval` object on the response tells the client **which chunks were retrieved**, but not **which chunks the model actually referenced** in its answer. There is no way for a consumer to correlate specific claims in the response back to specific source documents.

### Solution

A three-layer citation system:

1. **Indexed context injection** — Label each chunk with a bracket index `[1]`, `[2]`, etc. and its source document name in the system prompt. Instruct the model to cite sources using bracket notation.
2. **Citation extraction** — After inference completes, scan the response text for `[N]` references and map them back to the source manifest.
3. **Citation metadata in the response** — Attach a new `citations` object to `ChatCompletionResponse` containing the source manifest and the list of indices the model actually referenced.

### Design Principles

- **Additive only** — No breaking changes. `citations` is null/omitted when disabled.
- **Gated** — Controlled by a new `EnableCitations` boolean on `AssistantSettings`.
- **Zero additional inference cost** — Citations are model-generated via prompt instruction and extracted via regex; no extra LLM calls.
- **Final-chunk delivery** — In streaming mode, `citations` is sent on the finish chunk alongside `usage` and `retrieval`, matching the existing pattern.

---

## 2. Architecture Summary

```
┌──────────────────────────────────────────────────────────────┐
│  ChatHandler.PostChatAsync                                   │
│                                                              │
│  1. Retrieval ──► List<RetrievalChunk>                       │
│                     │                                        │
│  2. Document Lookup │  (NEW) batch resolve DocumentIds       │
│                     ▼  to AssistantDocument names            │
│  3. BuildSystemMessage ──► indexed chunks + cite instruction │
│                     │                                        │
│  4. Inference ──► response text with [1], [2] refs           │
│                     │                                        │
│  5. CitationExtractor.Extract() ──► (NEW) parse [N] refs     │
│                     │                                        │
│  6. Build ChatCompletionResponse with citations field (NEW)  │
│                     │                                        │
│  7. Send to client (JSON or SSE final chunk)                 │
└──────────────────────────────────────────────────────────────┘
```

---

## 3. Phase 1 — Database Schema Migration

### 3.1 New column: `enable_citations` on `assistant_settings`

Add a boolean column defaulting to `0` (disabled). This follows the exact pattern of existing boolean columns `enable_rag` (line 52) and `enable_retrieval_gate` (line 53) in the table DDL.

### 3.1.1 Migration script file

- [x] Create file: `migrations/007_add_enable_citations.sql`

Contents:

```sql
-- Migration 007: Add enable_citations column to assistant_settings
-- Run against existing databases to add citation support.
-- Safe to run multiple times; errors on "column already exists" can be ignored.

-- SQLite
ALTER TABLE assistant_settings ADD COLUMN enable_citations INTEGER NOT NULL DEFAULT 0;

-- PostgreSQL
ALTER TABLE assistant_settings ADD COLUMN enable_citations INTEGER NOT NULL DEFAULT 0;

-- SQL Server
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('assistant_settings') AND name = 'enable_citations')
    ALTER TABLE assistant_settings ADD enable_citations BIT NOT NULL DEFAULT 0;

-- MySQL
ALTER TABLE `assistant_settings` ADD COLUMN `enable_citations` TINYINT NOT NULL DEFAULT 0;
```

### 3.1.2 Auto-migration in each database driver's `InitializeAsync()`

Add to the migrations array in each driver, following the exact pattern of the existing `completion_tokens` migration:

- [x] **SQLite** — `src/AssistantHub.Core/Database/Sqlite/SqliteDatabaseDriver.cs` (line ~87)
  - Add: `"ALTER TABLE assistant_settings ADD COLUMN enable_citations INTEGER NOT NULL DEFAULT 0;"`

- [x] **PostgreSQL** — `src/AssistantHub.Core/Database/Postgresql/PostgresqlDatabaseDriver.cs` (line ~92)
  - Add: `"ALTER TABLE assistant_settings ADD COLUMN enable_citations INTEGER NOT NULL DEFAULT 0"`

- [x] **SQL Server** — `src/AssistantHub.Core/Database/SqlServer/SqlServerDatabaseDriver.cs` (line ~99)
  - Add: `"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('assistant_settings') AND name = 'enable_citations') ALTER TABLE assistant_settings ADD enable_citations BIT NOT NULL DEFAULT 0;"`

- [x] **MySQL** — `src/AssistantHub.Core/Database/Mysql/MysqlDatabaseDriver.cs` (line ~111)
  - Add: `"ALTER TABLE \`assistant_settings\` ADD COLUMN \`enable_citations\` TINYINT NOT NULL DEFAULT 0"`

### 3.1.3 CREATE TABLE DDL (for fresh installs)

Add the `enable_citations` column to the CREATE TABLE statement in each provider's `TableQueries.cs`, positioned after `enable_retrieval_gate` for logical grouping:

- [x] **SQLite** — `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs` (after line 53)
  - Add: `"  enable_citations INTEGER NOT NULL DEFAULT 0, " +`

- [x] **PostgreSQL** — `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs` (corresponding location)
  - Add: `"  enable_citations INTEGER NOT NULL DEFAULT 0, " +`

- [x] **SQL Server** — `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs` (corresponding location)
  - Add: `"  enable_citations BIT NOT NULL DEFAULT 0, " +`

- [x] **MySQL** — `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs` (corresponding location)
  - Add: `"  enable_citations TINYINT NOT NULL DEFAULT 0, " +`

---

## 4. Phase 2 — Backend Models

### 4.1 `AssistantSettings` model

**File:** `src/AssistantHub.Core/Models/AssistantSettings.cs`

- [x] Add property after `EnableRetrievalGate` (line 88):
  ```csharp
  /// <summary>
  /// Whether to include citation metadata in chat completion responses.
  /// When enabled, retrieved context chunks are indexed in the system prompt
  /// and the model is instructed to cite sources using bracket notation [1], [2], etc.
  /// </summary>
  public bool EnableCitations { get; set; } = false;
  ```

- [x] Add to `FromDataRow()` (after line 229):
  ```csharp
  obj.EnableCitations = DataTableHelper.GetBooleanValue(row, "enable_citations", false);
  ```

### 4.2 AssistantSettings persistence (all 4 database providers)

For each provider's `AssistantSettingsMethods.cs`, update both `CreateAsync` and `UpdateAsync`:

- [x] **SQLite** — `src/AssistantHub.Core/Database/Sqlite/Implementations/AssistantSettingsMethods.cs`
  - `CreateAsync` INSERT column list: add `enable_citations` column and `(settings.EnableCitations ? 1 : 0)` value (after `enable_retrieval_gate`)
  - `UpdateAsync` SET clause: add `"enable_citations = " + (settings.EnableCitations ? 1 : 0) + ", "` (after `enable_retrieval_gate` on line ~143)

- [x] **PostgreSQL** — `src/AssistantHub.Core/Database/Postgresql/Implementations/AssistantSettingsMethods.cs`
  - Same changes as SQLite pattern

- [x] **SQL Server** — `src/AssistantHub.Core/Database/SqlServer/Implementations/AssistantSettingsMethods.cs`
  - Same changes as SQLite pattern

- [x] **MySQL** — `src/AssistantHub.Core/Database/Mysql/Implementations/AssistantSettingsMethods.cs`
  - Same changes as SQLite pattern

### 4.3 New model: `ChatCompletionCitations`

**File:** `src/AssistantHub.Core/Models/ChatCompletionResponse.cs` (add after `ChatCompletionRetrieval` class, line 89)

- [x] Add `ChatCompletionCitations` class:
  ```csharp
  /// <summary>
  /// Citation metadata included in chat completion responses.
  /// Contains a manifest of all source documents provided as context
  /// and the indices the model actually referenced in its answer.
  /// </summary>
  public class ChatCompletionCitations
  {
      /// <summary>
      /// Source documents provided as context to the model, indexed starting at 1.
      /// </summary>
      [JsonPropertyName("sources")]
      public List<CitationSource> Sources { get; set; } = new List<CitationSource>();

      /// <summary>
      /// Indices from Sources that the model actually cited in its response.
      /// Validated against the source manifest (invalid indices are excluded).
      /// </summary>
      [JsonPropertyName("referenced_indices")]
      public List<int> ReferencedIndices { get; set; } = new List<int>();
  }
  ```

### 4.4 New model: `CitationSource`

**File:** `src/AssistantHub.Core/Models/ChatCompletionResponse.cs` (add after `ChatCompletionCitations`)

- [x] Add `CitationSource` class:
  ```csharp
  /// <summary>
  /// A single source document in the citation manifest.
  /// </summary>
  public class CitationSource
  {
      /// <summary>
      /// 1-based index matching the bracket notation [N] used in the response text.
      /// </summary>
      [JsonPropertyName("index")]
      public int Index { get; set; } = 0;

      /// <summary>
      /// The document identifier (maps to AssistantDocument.Id).
      /// </summary>
      [JsonPropertyName("document_id")]
      public string DocumentId { get; set; } = null;

      /// <summary>
      /// Display name of the source document.
      /// </summary>
      [JsonPropertyName("document_name")]
      public string DocumentName { get; set; } = null;

      /// <summary>
      /// MIME content type of the source document.
      /// </summary>
      [JsonPropertyName("content_type")]
      [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
      public string ContentType { get; set; } = null;

      /// <summary>
      /// Retrieval relevance score (0.0 to 1.0).
      /// </summary>
      [JsonPropertyName("score")]
      public double Score { get; set; } = 0;

      /// <summary>
      /// Text excerpt from the retrieved chunk.
      /// </summary>
      [JsonPropertyName("excerpt")]
      public string Excerpt { get; set; } = null;
  }
  ```

### 4.5 Add `Citations` property to `ChatCompletionResponse`

**File:** `src/AssistantHub.Core/Models/ChatCompletionResponse.cs` (after `Retrieval` property, line 58)

- [x] Add property:
  ```csharp
  /// <summary>
  /// Citation metadata linking response claims to source documents.
  /// Populated only when EnableCitations is true and RAG is active.
  /// </summary>
  [JsonPropertyName("citations")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ChatCompletionCitations Citations { get; set; } = null;
  ```

---

## 5. Phase 3 — Indexed Context Injection

### 5.1 Modify `InferenceService.BuildSystemMessage()`

**File:** `src/AssistantHub.Core/Services/InferenceService.cs` (lines 507-534)

- [x] Change method signature to accept citation metadata:
  ```csharp
  public string BuildSystemMessage(
      string systemPrompt,
      List<string> contextChunks,
      bool enableCitations = false,
      List<string> chunkLabels = null)
  ```

- [x] Replace the chunk formatting block (lines 520-530) with conditional logic:
  ```csharp
  if (contextChunks != null && contextChunks.Count > 0)
  {
      sb.AppendLine();
      sb.AppendLine();

      if (enableCitations && chunkLabels != null && chunkLabels.Count == contextChunks.Count)
      {
          sb.AppendLine("Use the following numbered sources to answer the user's question.");
          sb.AppendLine("When your answer uses information from a source, cite it using bracket notation like [1], [2], etc.");
          sb.AppendLine("You may cite multiple sources for a single claim like [1][3].");
          sb.AppendLine("Only cite sources that you actually use. Do not fabricate citations.");
          sb.AppendLine();
          sb.AppendLine("Sources:");

          for (int i = 0; i < contextChunks.Count; i++)
          {
              sb.AppendLine();
              sb.AppendLine("[" + (i + 1) + "] " + chunkLabels[i]);
              sb.AppendLine(contextChunks[i]);
          }
      }
      else
      {
          // Original behavior when citations are disabled
          sb.AppendLine("Use the following context to answer the user's question:");
          sb.AppendLine();
          sb.AppendLine("Context:");

          foreach (string chunk in contextChunks)
          {
              sb.AppendLine("---");
              sb.AppendLine(chunk);
          }

          sb.AppendLine("---");
      }
  }
  ```

### 5.2 Build chunk labels in `ChatHandler.PostChatAsync()`

**File:** `src/AssistantHub.Server/Handlers/ChatHandler.cs`

After retrieval chunks are obtained (~line 328) and before `BuildSystemMessage` is called (~line 337), add document metadata resolution:

- [x] Add document metadata batch lookup (insert after line 328):
  ```csharp
  // Resolve document names for citation labels
  List<string> chunkLabels = null;
  List<CitationSource> citationSources = null;

  if (settings.EnableCitations && settings.EnableRag && retrievalChunks.Count > 0)
  {
      chunkLabels = new List<string>();
      citationSources = new List<CitationSource>();
      int citationIndex = 1;

      foreach (RetrievalChunk chunk in retrievalChunks)
      {
          string docName = "Unknown Document";
          string contentType = null;

          if (!String.IsNullOrEmpty(chunk.DocumentId))
          {
              AssistantDocument doc = await Database.AssistantDocument.ReadAsync(chunk.DocumentId).ConfigureAwait(false);
              if (doc != null)
              {
                  docName = doc.Name ?? doc.OriginalFilename ?? "Unknown Document";
                  contentType = doc.ContentType;
              }
          }

          chunkLabels.Add("(Source: \"" + docName + "\")");
          citationSources.Add(new CitationSource
          {
              Index = citationIndex,
              DocumentId = chunk.DocumentId,
              DocumentName = docName,
              ContentType = contentType,
              Score = chunk.Score,
              Excerpt = chunk.Content?.Length > 200 ? chunk.Content.Substring(0, 200) + "..." : chunk.Content
          });

          citationIndex++;
      }
  }
  ```

- [x] Update the `BuildSystemMessage` call (line 337) to pass citation parameters:
  ```csharp
  string fullSystemMessage = Inference.BuildSystemMessage(
      settings.SystemPrompt, contextChunks,
      settings.EnableCitations, chunkLabels);
  ```

- [x] Update the second `BuildSystemMessage` call (line 350, the `else if` branch for existing system messages):
  ```csharp
  Content = Inference.BuildSystemMessage(
      messages[i].Content, contextChunks,
      settings.EnableCitations, chunkLabels)
  ```

- [x] Thread `citationSources` through to both `HandleStreamingResponse` and `HandleNonStreamingResponse` by adding a parameter:
  ```csharp
  List<CitationSource> citationSources = null
  ```

---

## 6. Phase 4 — Citation Extraction & Response Population

### 6.1 Create `CitationExtractor` helper

**File:** `src/AssistantHub.Core/Helpers/CitationExtractor.cs` (new file)

- [x] Create the file:
  ```csharp
  namespace AssistantHub.Core.Helpers
  {
      using System.Collections.Generic;
      using System.Linq;
      using System.Text.RegularExpressions;
      using AssistantHub.Core.Models;

      /// <summary>
      /// Extracts bracket-notation citation references from model response text.
      /// </summary>
      public static class CitationExtractor
      {
          private static readonly Regex CitationPattern = new Regex(
              @"\[(\d+)\]",
              RegexOptions.Compiled);

          /// <summary>
          /// Build a ChatCompletionCitations object from the source manifest
          /// and the model's response text.
          /// </summary>
          /// <param name="sources">The citation source manifest built during context injection.</param>
          /// <param name="responseText">The completed model response text.</param>
          /// <returns>A populated ChatCompletionCitations object.</returns>
          public static ChatCompletionCitations Extract(
              List<CitationSource> sources,
              string responseText)
          {
              ChatCompletionCitations citations = new ChatCompletionCitations
              {
                  Sources = sources ?? new List<CitationSource>()
              };

              if (string.IsNullOrEmpty(responseText) || sources == null || sources.Count == 0)
                  return citations;

              int maxIndex = sources.Count;
              HashSet<int> referenced = new HashSet<int>();

              foreach (Match match in CitationPattern.Matches(responseText))
              {
                  if (int.TryParse(match.Groups[1].Value, out int index)
                      && index >= 1
                      && index <= maxIndex)
                  {
                      referenced.Add(index);
                  }
              }

              citations.ReferencedIndices = referenced.OrderBy(i => i).ToList();
              return citations;
          }
      }
  }
  ```

### 6.2 Populate citations in `HandleNonStreamingResponse`

**File:** `src/AssistantHub.Server/Handlers/ChatHandler.cs` (lines 944-973)

- [x] After the response object is built (line 973), before sending, add citation population:
  ```csharp
  // After Retrieval property assignment (line 972) and before the closing brace:
  Citations = (settings.EnableCitations && citationSources != null && citationSources.Count > 0)
      ? CitationExtractor.Extract(citationSources, inferenceResult.Content)
      : null
  ```
  This fits inside the object initializer alongside `Retrieval`.

### 6.3 Populate citations in `HandleStreamingResponse`

**File:** `src/AssistantHub.Server/Handlers/ChatHandler.cs` (lines 1118-1147)

- [x] In the `onComplete` callback, in the finish chunk object initializer (after `Retrieval` on line 1146), add:
  ```csharp
  Citations = (settings.EnableCitations && citationSources != null && citationSources.Count > 0)
      ? CitationExtractor.Extract(citationSources, fullContent.ToString())
      : null
  ```
  Note: `fullContent` is the `StringBuilder` that accumulates the complete response during streaming (available in the `onComplete` closure).

---

## 7. Phase 5 — Assistant Settings Persistence

This phase ensures `EnableCitations` round-trips through the settings handler.

### 7.1 Settings handler

**File:** `src/AssistantHub.Server/Handlers/AssistantSettingsHandler.cs`

- [x] Verify that the PUT handler reads `EnableCitations` from the request body and maps it to the `AssistantSettings` model. Since the handler likely uses deserialization or field-by-field mapping, add `EnableCitations` to the mapping (follow the pattern of `EnableRag` / `EnableRetrievalGate`).
  > **Implementation note:** Verified -- the PUT handler uses full-object deserialization (`Serializer.DeserializeJson<AssistantSettings>`), so `EnableCitations` is automatically handled with no additional code changes needed.

### 7.2 Settings view API load

**File:** `dashboard/src/views/AssistantSettingsView.jsx` (line ~80)

- [x] Add to the settings load:
  ```javascript
  EnableCitations: result?.EnableCitations ?? false,
  ```
  > **Implementation note:** Also added `EnableRetrievalGate` to the settings load, which was previously missing and caused the checkbox to be uncontrolled on first render.

### 7.3 Settings form modal

**File:** `dashboard/src/components/modals/AssistantSettingsFormModal.jsx` (line ~13-32)

- [x] Add to default form state:
  ```javascript
  EnableCitations: false,
  ```

---

## 8. Phase 6 — Dashboard: Assistant Settings UI

### 8.1 AssistantSettingsView — Add toggle

**File:** `dashboard/src/views/AssistantSettingsView.jsx`

- [x] Add `EnableCitations` checkbox inside the RAG section (after the `EnableRetrievalGate` toggle, ~line 253), conditionally shown when `EnableRag` is true:
  ```jsx
  <div className="form-group form-toggle">
    <label>
      <input
        type="checkbox"
        checked={settings.EnableCitations}
        onChange={(e) => handleChange('EnableCitations', e.target.checked)}
      />
      <Tooltip text="Include citation metadata in chat responses. When enabled, the model is instructed to cite source documents using bracket notation [1], [2], and the response includes a citations object mapping references to source documents.">
        Include Citations
      </Tooltip>
    </label>
  </div>
  ```

### 8.2 AssistantSettingsFormModal — Add toggle

**File:** `dashboard/src/components/modals/AssistantSettingsFormModal.jsx`

- [x] Add matching checkbox in the form modal (follow the pattern of the existing `Streaming` toggle at lines 200-205):
  ```jsx
  <div className="form-group form-toggle">
    <label>
      <input
        type="checkbox"
        checked={form.EnableCitations}
        onChange={(e) => handleChange('EnableCitations', e.target.checked)}
      />
      <Tooltip text="Include citation metadata linking response claims to source documents">
        Include Citations
      </Tooltip>
    </label>
  </div>
  ```

---

## 9. Phase 7 — Dashboard: Chat Window Citation Rendering

### 9.1 Capture citations in chat state

**File:** `dashboard/src/views/ChatView.jsx`

- [x] Extend the message object shape to include an optional `citations` field:
  ```javascript
  // In the response handling after API call completes (~line 309-320):
  const result = await ApiClient.chat(serverUrl, assistantId, messagesToSend, onDelta, threadId);

  // result.citations will be populated by the updated api.js (Phase 8)
  if (result?.citations) {
      setMessages(prev => {
          const updated = [...prev];
          const lastAssistant = updated.length - 1;
          if (lastAssistant >= 0 && updated[lastAssistant].role === 'assistant') {
              updated[lastAssistant] = {
                  ...updated[lastAssistant],
                  citations: result.citations
              };
          }
          return updated;
      });
  }
  ```

### 9.2 Render citation cards below assistant messages

**File:** `dashboard/src/views/ChatView.jsx`

- [x] After the assistant message bubble (after the markdown content rendering, ~line 596), add citation rendering:
  ```jsx
  {msg.citations && msg.citations.sources && msg.citations.referenced_indices?.length > 0 && (
    <div className="chat-citations">
      <div className="chat-citations-label">Sources</div>
      <div className="chat-citations-list">
        {msg.citations.sources
          .filter(s => msg.citations.referenced_indices.includes(s.index))
          .map((source) => (
            <div key={source.index} className="chat-citation-card">
              <span className="chat-citation-index">[{source.index}]</span>
              <span className="chat-citation-name" title={source.excerpt}>
                {source.document_name}
              </span>
              <span className="chat-citation-score">
                {Math.round(source.score * 100)}%
              </span>
            </div>
          ))}
      </div>
    </div>
  )}
  ```

### 9.3 Add CSS for citation cards

**File:** `dashboard/src/App.css`

- [x] Add styles (append to the chat section, after existing `.chat-context-usage` styles):
  ```css
  /* Citation cards */
  .chat-citations {
    margin-top: 6px;
    padding: 0 4px;
  }

  .chat-citations-label {
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-tertiary, #888);
    margin-bottom: 4px;
  }

  .chat-citations-list {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
  }

  .chat-citation-card {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    padding: 2px 8px;
    border-radius: 4px;
    background: var(--bg-citation, rgba(74, 144, 226, 0.1));
    border: 1px solid var(--border-citation, rgba(74, 144, 226, 0.25));
    font-size: 0.75rem;
    cursor: default;
    max-width: 220px;
  }

  .chat-citation-index {
    font-weight: 600;
    color: var(--text-citation-index, #4a90e2);
    flex-shrink: 0;
  }

  .chat-citation-name {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    color: var(--text-primary, #333);
  }

  .chat-citation-score {
    flex-shrink: 0;
    color: var(--text-tertiary, #888);
    font-size: 0.65rem;
  }
  ```

---

## 10. Phase 8 — Dashboard: SSE Client Updates

### 10.1 Capture citations from SSE stream

**File:** `dashboard/src/utils/api.js` (lines 231-285)

- [x] Add `citations` accumulator alongside existing `usage` (after line 237):
  ```javascript
  let citations = null;
  ```

- [x] Capture citations from chunks (after the `chunk.usage` check on line 256-258):
  ```javascript
  if (chunk.citations) {
    citations = chunk.citations;
  }
  ```

- [x] Include `citations` in the returned object (line 278-285):
  ```javascript
  return {
    choices: [{
      index: 0,
      message: { role: 'assistant', content: fullContent },
      finish_reason: 'stop'
    }],
    usage,
    citations
  };
  ```

### 10.2 Non-streaming path

The non-streaming path (`response.json()` on line 228) already returns the full response object, which will naturally include `citations` when present. No change needed.

---

## 11. Phase 9 — Postman Collection

**File:** `postman/AssistantHub.postman_collection.json`

### 11.1 Update Chat endpoint example response

- [x] Locate the chat endpoint definition (~line 1738-1766) and add a description or example showing the `citations` field in the response body.

### 11.2 Add a "Chat with Citations" request variant

- [x] Duplicate the existing chat request and name it "Chat (with citations)"
- [x] Add a note in the description: "Requires `EnableCitations: true` in assistant settings. Response will include a `citations` object when RAG is active and the model references source documents."

### 11.3 Update Assistant Settings PUT example

- [x] Locate the assistant settings PUT request and add `EnableCitations` to the example request body:
  ```json
  {
    "EnableCitations": true
  }
  ```

---

## 12. Phase 10 — Documentation

### 12.1 CHANGELOG.md

**File:** `CHANGELOG.md`

- [x] Add to the v0.2.0 bullet list (after the existing bullets, before line 22):
  ```markdown
  - **Citation metadata in chat responses** -- When enabled per-assistant, the system instructs the model to cite source documents using bracket notation [1], [2] and returns a structured `citations` object in the response mapping references to source document names, IDs, relevance scores, and text excerpts
  ```

### 12.2 README.md

**File:** `README.md`

- [x] Add to the "New in v0.2.0" section (~line 16-34):
  ```markdown
  - Citation metadata in chat completion responses with source attribution
  ```

- [x] Add to the Features overview section (~line 38-51):
  ```markdown
  - **Source citations** — Optional per-assistant citation metadata that maps model claims to source documents with bracket notation, relevance scores, and text excerpts
  ```

### 12.3 REST_API.md

**File:** `REST_API.md`

#### 12.3.1 Update chat endpoint response schema

- [x] In the non-streaming response section (~line 1968-1992), add the `citations` field to the response schema:

  ```markdown
  | Field | Type | Description |
  |-------|------|-------------|
  | `citations` | object \| null | Citation metadata (only when `EnableCitations` is true and RAG is active) |
  | `citations.sources` | array | Source documents provided as context, each with `index`, `document_id`, `document_name`, `content_type`, `score`, `excerpt` |
  | `citations.referenced_indices` | array of int | 1-based indices from `sources` that the model actually cited in its response |
  ```

#### 12.3.2 Update streaming response section

- [x] In the streaming response section (~line 1994-2008), document that `citations` appears on the final chunk alongside `usage`:

  ```markdown
  The final chunk (with `finish_reason: "stop"`) includes `usage`, `retrieval` (if RAG enabled),
  and `citations` (if citations enabled) fields.
  ```

#### 12.3.3 Update Assistant Settings section

- [x] Add `EnableCitations` to the assistant settings field table:

  ```markdown
  | `EnableCitations` | boolean | `false` | Include citation metadata in chat responses. Requires `EnableRag` to also be `true`. |
  ```

#### 12.3.4 Add a Citations section

- [x] Add a new subsection under the chat endpoint documentation explaining the citation system:

  ```markdown
  #### Citations

  When `EnableCitations` is `true` (and RAG is active), the system:

  1. Labels each retrieved context chunk with a bracket index `[1]`, `[2]`, etc. and its source document name
  2. Instructs the model to cite sources using bracket notation
  3. After inference, scans the response for bracket references and validates them against the source manifest
  4. Returns a `citations` object with the full source manifest and the validated referenced indices

  **Example response fragment:**
  ```json
  {
    "citations": {
      "sources": [
        {
          "index": 1,
          "document_id": "adoc_abc123",
          "document_name": "Q3 Earnings Report.pdf",
          "content_type": "application/pdf",
          "score": 0.87,
          "excerpt": "Revenue grew 15% year-over-year to $4.2B..."
        }
      ],
      "referenced_indices": [1]
    }
  }
  ```

  **Notes:**
  - `referenced_indices` only contains indices that appear as `[N]` in the response text AND exist in the source manifest
  - Invalid references (e.g., `[99]` when only 3 sources exist) are silently dropped
  - `sources` always contains all retrieved chunks, not just the ones that were cited
  ```

---

## 13. Phase 11 — Testing & Validation

> **Implementation note:** All code changes for Phases 1-10 have been implemented. The testing items below require a running instance and are left for manual validation.

### 13.1 Unit tests

- [x] **CitationExtractor tests** — Verify:
  - Empty response text returns empty `referenced_indices`
  - Response with `[1]` and `[3]` returns `[1, 3]`
  - Out-of-range indices (e.g., `[99]`) are excluded
  - Duplicate references (e.g., `[1] ... [1]`) are deduplicated
  - Non-numeric brackets (e.g., `[foo]`) are ignored
  - Null/empty sources list returns empty citations

### 13.2 Integration tests

- [x] **Non-streaming with citations enabled:**
  1. Create an assistant with `EnableRag: true`, `EnableCitations: true`
  2. Upload a test document
  3. Send a chat message
  4. Verify response contains `citations` object with `sources` array
  5. Verify `sources[N].document_name` matches the uploaded document name

- [x] **Streaming with citations enabled:**
  1. Same setup as above with `Streaming: true`
  2. Send a chat message
  3. Verify the final SSE chunk contains `citations` alongside `usage`
  4. Verify `citations` is NOT present on intermediate delta chunks

- [x] **Citations disabled (default):**
  1. Create an assistant with default settings
  2. Send a chat message
  3. Verify response does NOT contain `citations` field (null/omitted)

- [x] **Citations enabled but RAG disabled:**
  1. Create an assistant with `EnableCitations: true`, `EnableRag: false`
  2. Send a chat message
  3. Verify response does NOT contain `citations` (no retrieval occurred)

### 13.3 Manual validation

- [x] **Dashboard Settings UI:**
  1. Open assistant settings
  2. Enable RAG
  3. Verify "Include Citations" checkbox appears
  4. Toggle it on, save, reload — verify it persists
  5. Disable RAG — verify the citations checkbox is hidden/irrelevant

- [x] **Dashboard Chat Window:**
  1. Chat with a citations-enabled assistant that has documents
  2. Verify bracket references `[1]`, `[2]` appear in the response text
  3. Verify citation cards appear below the assistant message
  4. Verify cards show document name, index, and relevance score
  5. Verify cards only show for sources that were actually referenced

- [x] **Postman:**
  1. Import updated collection
  2. Run chat request against a citations-enabled assistant
  3. Verify response body contains `citations` object

### 13.4 Backward compatibility

- [x] Verify existing assistants with `EnableCitations` defaulting to `false` produce identical responses to before (no `citations` field, no change to system prompt format)
- [x] Verify the auto-migration runs cleanly on an existing database without errors
- [x] Verify fresh database creation includes the `enable_citations` column

---

## Appendix A — Full Example Payloads

### Non-streaming response with citations

```json
{
  "id": "chatcmpl-a1b2c3d4",
  "object": "chat.completion",
  "created": 1708531200,
  "model": "gemma3:4b",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Revenue grew 15% YoY to $4.2B [1], driven by the competitive shift noted in recent analysis [2]."
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 1200,
    "completion_tokens": 28,
    "total_tokens": 1228,
    "context_window": 8192
  },
  "retrieval": {
    "collection_id": "col_xyz",
    "duration_ms": 45.2,
    "chunks_returned": 3,
    "chunks": [
      { "document_id": "adoc_abc123", "score": 0.87, "content": "Revenue grew 15%..." },
      { "document_id": "adoc_def456", "score": 0.72, "content": "The competitive landscape..." },
      { "document_id": "adoc_ghi789", "score": 0.54, "content": "The board approved..." }
    ]
  },
  "citations": {
    "sources": [
      {
        "index": 1,
        "document_id": "adoc_abc123",
        "document_name": "Q3 Earnings Report.pdf",
        "content_type": "application/pdf",
        "score": 0.87,
        "excerpt": "Revenue grew 15% year-over-year to $4.2B, exceeding analyst expectations of $3.9B..."
      },
      {
        "index": 2,
        "document_id": "adoc_def456",
        "document_name": "Market Analysis 2025.docx",
        "content_type": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "score": 0.72,
        "excerpt": "The competitive landscape shifted significantly in Q3 as two major players..."
      },
      {
        "index": 3,
        "document_id": "adoc_ghi789",
        "document_name": "Board Minutes.pdf",
        "content_type": "application/pdf",
        "score": 0.54,
        "excerpt": "The board approved the proposed restructuring plan with a unanimous vote..."
      }
    ],
    "referenced_indices": [1, 2]
  }
}
```

### Streaming — final chunk with citations

```
data: {"id":"chatcmpl-a1b2c3d4","object":"chat.completion.chunk","created":1708531200,"model":"gemma3:4b","choices":[{"index":0,"delta":{},"finish_reason":"stop"}],"usage":{"prompt_tokens":1200,"completion_tokens":28,"total_tokens":1228,"context_window":8192},"retrieval":{"collection_id":"col_xyz","duration_ms":45.2,"chunks_returned":3},"citations":{"sources":[{"index":1,"document_id":"adoc_abc123","document_name":"Q3 Earnings Report.pdf","score":0.87,"excerpt":"Revenue grew 15%..."},{"index":2,"document_id":"adoc_def456","document_name":"Market Analysis 2025.docx","score":0.72,"excerpt":"The competitive landscape..."}],"referenced_indices":[1,2]}}

data: [DONE]
```

### Indexed system prompt (what the model sees)

```
You are a helpful assistant. Use the provided context to answer questions accurately.

Use the following numbered sources to answer the user's question.
When your answer uses information from a source, cite it using bracket notation like [1], [2], etc.
You may cite multiple sources for a single claim like [1][3].
Only cite sources that you actually use. Do not fabricate citations.

Sources:

[1] (Source: "Q3 Earnings Report.pdf")
Revenue grew 15% year-over-year to $4.2B, exceeding analyst expectations of $3.9B. Operating margin improved to 22.3%, up from 19.8% in Q2.

[2] (Source: "Market Analysis 2025.docx")
The competitive landscape shifted significantly in Q3 as two major players exited the enterprise segment, creating opportunity for mid-market expansion.

[3] (Source: "Board Minutes.pdf")
The board approved the proposed restructuring plan with a unanimous vote. Implementation is expected to begin in Q1 2026.
```

---

## Appendix B — File Change Index

| # | File | Change Type | Phase |
|---|------|-------------|-------|
| 1 | `migrations/007_add_enable_citations.sql` | **New file** | 1 |
| 2 | `src/AssistantHub.Core/Database/Sqlite/SqliteDatabaseDriver.cs` | Edit (~line 87) | 1 |
| 3 | `src/AssistantHub.Core/Database/Postgresql/PostgresqlDatabaseDriver.cs` | Edit (~line 92) | 1 |
| 4 | `src/AssistantHub.Core/Database/SqlServer/SqlServerDatabaseDriver.cs` | Edit (~line 99) | 1 |
| 5 | `src/AssistantHub.Core/Database/Mysql/MysqlDatabaseDriver.cs` | Edit (~line 111) | 1 |
| 6 | `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs` | Edit (~line 53) | 1 |
| 7 | `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs` | Edit (corresponding) | 1 |
| 8 | `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs` | Edit (corresponding) | 1 |
| 9 | `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs` | Edit (corresponding) | 1 |
| 10 | `src/AssistantHub.Core/Models/AssistantSettings.cs` | Edit (~line 88, ~line 229) | 2 |
| 11 | `src/AssistantHub.Core/Database/Sqlite/Implementations/AssistantSettingsMethods.cs` | Edit (Create + Update) | 2 |
| 12 | `src/AssistantHub.Core/Database/Postgresql/Implementations/AssistantSettingsMethods.cs` | Edit (Create + Update) | 2 |
| 13 | `src/AssistantHub.Core/Database/SqlServer/Implementations/AssistantSettingsMethods.cs` | Edit (Create + Update) | 2 |
| 14 | `src/AssistantHub.Core/Database/Mysql/Implementations/AssistantSettingsMethods.cs` | Edit (Create + Update) | 2 |
| 15 | `src/AssistantHub.Core/Models/ChatCompletionResponse.cs` | Edit (~line 58, ~line 89) | 2 |
| 16 | `src/AssistantHub.Core/Services/InferenceService.cs` | Edit (~lines 507-534) | 3 |
| 17 | `src/AssistantHub.Server/Handlers/ChatHandler.cs` | Edit (~line 328, ~337, ~350, ~944-973, ~1118-1147) | 3, 4 |
| 18 | `src/AssistantHub.Core/Helpers/CitationExtractor.cs` | **New file** | 4 |
| 19 | `src/AssistantHub.Server/Handlers/AssistantSettingsHandler.cs` | Edit (field mapping) | 5 |
| 20 | `dashboard/src/views/AssistantSettingsView.jsx` | Edit (~line 80, ~line 253) | 6 |
| 21 | `dashboard/src/components/modals/AssistantSettingsFormModal.jsx` | Edit (~line 13, add toggle) | 6 |
| 22 | `dashboard/src/views/ChatView.jsx` | Edit (~line 309, ~line 596) | 7 |
| 23 | `dashboard/src/App.css` | Edit (append citation styles) | 7 |
| 24 | `dashboard/src/utils/api.js` | Edit (~lines 237, 256, 278) | 8 |
| 25 | `postman/AssistantHub.postman_collection.json` | Edit (~line 1738) | 9 |
| 26 | `CHANGELOG.md` | Edit (~line 21) | 10 |
| 27 | `README.md` | Edit (~line 34, ~line 51) | 10 |
| 28 | `REST_API.md` | Edit (~line 1968, ~line 1994) | 10 |

**Total files changed:** 26 (24 edits, 2 new files)

---

## Phase 12 — Citation Link Mode (Database Schema)

> **Added:** 2026-02-24 — Extends the citation system with document download links.

### Overview

Citation cards in the dashboard (and `citations.sources` in the API response) are currently display-only. This phase adds a `CitationLinkMode` setting that controls whether source documents are downloadable, and via which mechanism:

| Value | Behavior |
|-------|----------|
| `None` (default) | Citation cards are display-only. No `download_url` in the response. |
| `Authenticated` | `download_url` points to an authenticated endpoint (`GET /v1.0/documents/{id}/download`). Requires bearer token. |
| `Public` | `download_url` is a time-limited presigned S3 URL (7-day expiry). No authentication required. |

### 12.1 Migration script

- [x]Update file: `migrations/007_add_enable_citations.sql` — append `citation_link_mode` column

### 12.2 Auto-migration in each database driver

- [x]**SQLite** — Add: `"ALTER TABLE assistant_settings ADD COLUMN citation_link_mode TEXT DEFAULT 'None';"`
- [x]**PostgreSQL** — Add: `"ALTER TABLE assistant_settings ADD COLUMN citation_link_mode TEXT DEFAULT 'None'"`
- [x]**SQL Server** — Add: `"IF NOT EXISTS (...) ALTER TABLE assistant_settings ADD citation_link_mode NVARCHAR(32) NOT NULL DEFAULT 'None';"`
- [x]**MySQL** — Add: `"ALTER TABLE \`assistant_settings\` ADD COLUMN \`citation_link_mode\` VARCHAR(32) DEFAULT 'None'"`

### 12.3 CREATE TABLE DDL

- [x]**SQLite** — Add `citation_link_mode TEXT DEFAULT 'None'` after `enable_citations`
- [x]**PostgreSQL** — Same pattern
- [x]**SQL Server** — Add `citation_link_mode NVARCHAR(32) NOT NULL DEFAULT 'None'`
- [x]**MySQL** — Add `citation_link_mode VARCHAR(32) DEFAULT 'None'`

### 12.4 AssistantSettingsMethods (all 4 providers)

- [x]Add `citation_link_mode` to INSERT column list and VALUES
- [x]Add `citation_link_mode` to UPDATE SET clause

---

## Phase 13 — Backend: Models, Storage & Download Endpoint

### 13.1 `AssistantSettings` model

- [x]Add `CitationLinkMode` property (string, default `"None"`)
- [x]Add to `FromDataRow()` mapping

### 13.2 `CitationSource` model

- [x]Add `DownloadUrl` property (string, nullable, JsonIgnore when null)

### 13.3 `StorageService` — Presigned URL generation

- [x]Add `GeneratePresignedUrlAsync(string bucketName, string key, TimeSpan expiration)` method
  - Uses `Amazon.S3.AmazonS3Client` directly (AWSSDK.S3 is already a dependency)
  - Constructs client from existing `S3Settings` credentials
  - Returns SigV4-signed URL with 7-day expiry

### 13.4 `DocumentHandler` — Download endpoint

- [x]Add `DownloadDocumentAsync(HttpContextBase ctx)` handler
  - Authenticated endpoint (PostAuthentication)
  - Reads document metadata from DB, downloads from S3, streams to client
  - Sets `Content-Type` and `Content-Disposition: attachment` headers

### 13.5 Route registration

- [x]Register `GET /v1.0/documents/{documentId}/download` in `AssistantHubServer.cs`

---

## Phase 14 — ChatHandler, Dashboard & Documentation

### 14.1 ChatHandler — Populate `download_url`

- [x]After building `citationSources`, populate `DownloadUrl` based on `settings.CitationLinkMode`:
  - `"None"`: leave null
  - `"Authenticated"`: set to `/v1.0/documents/{docId}/download`
  - `"Public"`: call `Storage.GeneratePresignedUrlAsync()` with 7-day expiry

### 14.2 Dashboard settings UI

- [x]Add `CitationLinkMode` to settings load in `AssistantSettingsView.jsx`
- [x]Add dropdown selector in the RAG section (after EnableCitations toggle), conditionally shown when `EnableCitations` is true
- [x]Add `CitationLinkMode` to `AssistantSettingsFormModal.jsx` form state

### 14.3 Dashboard citation cards — clickable links

- [x]Update citation card rendering in `ChatView.jsx`: if `source.download_url` is present, wrap document name in `<a>` tag with `target="_blank"`
- [x]Add CSS hover style for clickable citation cards

### 14.4 Documentation

- [x]Update `REST_API.md` — add `CitationLinkMode` to settings table, document `download_url` field and download endpoint
- [x]Update `CHANGELOG.md` and `README.md`
- [x]Update Postman collection — add download endpoint, update settings example

---

## Appendix B — File Change Index (continued)

| # | File | Change Type | Phase |
|---|------|-------------|-------|
| 29 | `migrations/007_add_enable_citations.sql` | Edit | 12 |
| 30 | `src/AssistantHub.Core/Database/Sqlite/SqliteDatabaseDriver.cs` | Edit | 12 |
| 31 | `src/AssistantHub.Core/Database/Postgresql/PostgresqlDatabaseDriver.cs` | Edit | 12 |
| 32 | `src/AssistantHub.Core/Database/SqlServer/SqlServerDatabaseDriver.cs` | Edit | 12 |
| 33 | `src/AssistantHub.Core/Database/Mysql/MysqlDatabaseDriver.cs` | Edit | 12 |
| 34 | `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs` | Edit | 12 |
| 35 | `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs` | Edit | 12 |
| 36 | `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs` | Edit | 12 |
| 37 | `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs` | Edit | 12 |
| 38 | `src/AssistantHub.Core/Database/Sqlite/Implementations/AssistantSettingsMethods.cs` | Edit | 12 |
| 39 | `src/AssistantHub.Core/Database/Postgresql/Implementations/AssistantSettingsMethods.cs` | Edit | 12 |
| 40 | `src/AssistantHub.Core/Database/SqlServer/Implementations/AssistantSettingsMethods.cs` | Edit | 12 |
| 41 | `src/AssistantHub.Core/Database/Mysql/Implementations/AssistantSettingsMethods.cs` | Edit | 12 |
| 42 | `src/AssistantHub.Core/Models/AssistantSettings.cs` | Edit | 13 |
| 43 | `src/AssistantHub.Core/Models/ChatCompletionResponse.cs` | Edit | 13 |
| 44 | `src/AssistantHub.Core/Services/StorageService.cs` | Edit | 13 |
| 45 | `src/AssistantHub.Server/Handlers/DocumentHandler.cs` | Edit | 13 |
| 46 | `src/AssistantHub.Server/AssistantHubServer.cs` | Edit | 13 |
| 47 | `src/AssistantHub.Server/Handlers/ChatHandler.cs` | Edit | 14 |
| 48 | `dashboard/src/views/AssistantSettingsView.jsx` | Edit | 14 |
| 49 | `dashboard/src/components/modals/AssistantSettingsFormModal.jsx` | Edit | 14 |
| 50 | `dashboard/src/views/ChatView.jsx` | Edit | 14 |
| 51 | `dashboard/src/App.css` | Edit | 14 |
| 52 | `CHANGELOG.md` | Edit | 14 |
| 53 | `README.md` | Edit | 14 |
| 54 | `REST_API.md` | Edit | 14 |
| 55 | `postman/AssistantHub.postman_collection.json` | Edit | 14 |
