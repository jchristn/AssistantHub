# Metadata Filtering Implementation Plan

**Goal:** Allow chat conversations to be filtered to only retrieve documents matching specified labels (strings) and/or tags (key-value pairs). Filters can be configured as defaults on an assistant and/or supplied per-conversation at chat time.

**RecallDB support:** RecallDB's `SearchQuery` already supports `LabelFilter` (Required/Excluded lists) and `TagFilter` (`TagFilterSet` with Required/Excluded `TagCondition` lists supporting Equals, NotEquals, Contains, StartsWith, etc.). RecallDB also exposes distinct-values endpoints:
- `GET /v1.0/tenants/{tid}/collections/{cid}/labels/distinct` → `["label1", "label2", ...]`
- `GET /v1.0/tenants/{tid}/collections/{cid}/tags/distinct` → `["key1", "key2", ...]`

---

## Phase 1: Backend — Models & Database Schema

### 1.1 Add filter fields to `AssistantSettings` model
- [x]**File:** `src/AssistantHub.Core/Models/AssistantSettings.cs`
- [x]Add property `string RetrievalLabelFilter { get; set; }` — JSON-serialized label filter (stored as `{"Required":["a","b"],"Excluded":["c"]}`)
- [x]Add property `string RetrievalTagFilter { get; set; }` — JSON-serialized tag filter (stored as `{"Required":[{"Key":"k","Condition":"Equals","Value":"v"}],"Excluded":[...]}`)
- [x]Update `FromDataRow()` to read columns `retrieval_label_filter` and `retrieval_tag_filter`

### 1.2 Add filter fields to `ChatCompletionRequest` model
- [x]**File:** `src/AssistantHub.Core/Models/ChatCompletionRequest.cs`
- [x]Add property `ChatMetadataFilter MetadataFilter { get; set; }` with `[JsonPropertyName("metadata_filter")]`

### 1.3 Create `ChatMetadataFilter` model
- [x]**File:** `src/AssistantHub.Core/Models/ChatMetadataFilter.cs` (new file)
- [x]Properties:
  - `List<string> RequiredLabels` — labels that must be present
  - `List<string> ExcludedLabels` — labels that must NOT be present
  - `List<ChatTagCondition> RequiredTags` — tag conditions that must all match
  - `List<ChatTagCondition> ExcludedTags` — tag conditions that must NOT match

### 1.4 Create `ChatTagCondition` model
- [x]**File:** `src/AssistantHub.Core/Models/ChatTagCondition.cs` (new file)
- [x]Properties:
  - `string Key` — tag key
  - `string Condition` — condition type (Equals, NotEquals, Contains, StartsWith, EndsWith, IsNull, IsNotNull, GreaterThan, LessThan)
  - `string Value` — value to compare against

### 1.5 Update database schema — all 4 providers
- [x]**File:** `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs`
  - Add columns `retrieval_label_filter TEXT` and `retrieval_tag_filter TEXT` to `assistant_settings` CREATE TABLE
- [x]**File:** `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs` — same
- [x]**File:** `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs` — same
- [x]**File:** `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs` — same

### 1.6 Update `AssistantSettings` CRUD — all 4 providers
For each provider (Sqlite, Mysql, Postgresql, SqlServer):
- [x]**Insert** method: add `retrieval_label_filter` and `retrieval_tag_filter` parameters
- [x]**Update** method: add `retrieval_label_filter` and `retrieval_tag_filter` parameters
- [x]**Read/Enumerate**: columns are already picked up by `FromDataRow()` after 1.1

Affected files (8 total):
- [x]`src/AssistantHub.Core/Database/Sqlite/Implementations/AssistantSettingsMethods.cs`
- [x]`src/AssistantHub.Core/Database/Mysql/Implementations/AssistantSettingsMethods.cs`
- [x]`src/AssistantHub.Core/Database/Postgresql/Implementations/AssistantSettingsMethods.cs`
- [x]`src/AssistantHub.Core/Database/SqlServer/Implementations/AssistantSettingsMethods.cs`

---

## Phase 2: Backend — Retrieval Service

### 2.1 Add label/tag filter parameters to `RetrievalSearchOptions`
- [x]**File:** `src/AssistantHub.Core/Models/RetrievalSearchOptions.cs`
- [x]Add property `ChatMetadataFilter MetadataFilter { get; set; }` (nullable)

### 2.2 Update `RetrievalService.BuildSearchBody()` to include filters
- [x]**File:** `src/AssistantHub.Core/Services/RetrievalService.cs`
- [x]In all three branches (Vector, FullText, Hybrid), add `LabelFilter` and `TagFilter` properties to the anonymous search body object when `MetadataFilter` is non-null
- [x]Map `ChatMetadataFilter` → RecallDB's `LabelFilter` format (`Required`, `Excluded`)
- [x]Map `ChatTagCondition` list → RecallDB's `TagFilterSet` format (list of `TagCondition` with `Key`, `Condition`, `Value`)
- [x]Also include filters in the hybrid fallback vector-only body

---

## Phase 3: Backend — Chat Handler

### 3.1 Merge assistant-level and request-level filters in `PostChatAsync`
- [x]**File:** `src/AssistantHub.Server/Handlers/ChatHandler.cs`
- [x]After loading `AssistantSettings`, deserialize `RetrievalLabelFilter` and `RetrievalTagFilter` into a `ChatMetadataFilter`
- [x]If the `ChatCompletionRequest` also has a `MetadataFilter`, merge both:
  - `RequiredLabels`: union of both lists (deduplicated)
  - `ExcludedLabels`: union of both lists (deduplicated)
  - `RequiredTags`: concatenate both lists
  - `ExcludedTags`: concatenate both lists
- [x]Pass the merged `ChatMetadataFilter` into `RetrievalSearchOptions.MetadataFilter` when calling `RetrievalService.RetrieveAsync()`

### 3.2 Store effective filters in chat history
- [x]**File:** `src/AssistantHub.Core/Models/ChatHistory.cs`
  - Add property `string MetadataFilter { get; set; }` — JSON-serialized effective `ChatMetadataFilter` that was applied during retrieval (null when no filters active)
  - Update `FromDataRow()` to read column `metadata_filter`
- [x]**File:** `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs`
  - Add column `metadata_filter TEXT` to `chat_history` CREATE TABLE
- [x]**File:** `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs` — same
- [x]**File:** `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs` — same
- [x]**File:** `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs` — same
- [x]Update `ChatHistoryMethods` INSERT for all 4 providers to include `metadata_filter` parameter:
  - [x]`src/AssistantHub.Core/Database/Sqlite/Implementations/ChatHistoryMethods.cs`
  - [x]`src/AssistantHub.Core/Database/Mysql/Implementations/ChatHistoryMethods.cs`
  - [x]`src/AssistantHub.Core/Database/Postgresql/Implementations/ChatHistoryMethods.cs`
  - [x]`src/AssistantHub.Core/Database/SqlServer/Implementations/ChatHistoryMethods.cs`
- [x]**File:** `src/AssistantHub.Server/Handlers/ChatHandler.cs`
  - Update `WriteChatHistoryAsync()` signature to accept a `string metadataFilterJson` parameter
  - Serialize the effective merged `ChatMetadataFilter` to JSON and pass it to `WriteChatHistoryAsync()`
  - Update both call sites (streaming and non-streaming paths) to pass the filter JSON

### 3.3 Display filters in history view modal
- [x]**File:** `dashboard/src/components/modals/HistoryViewModal.jsx`
  - If `history.MetadataFilter` is non-null, parse JSON and display in a "Metadata Filters" collapsible section
  - Show required/excluded labels as chips
  - Show required/excluded tag conditions in a compact table (Key | Condition | Value)

### 3.4 Include active filters in chat response metadata
- [x]In the `ChatCompletionResponse` (or the metadata/diagnostics object), include the effective filters that were applied so the caller can confirm what was used

---

## Phase 4: Backend — Distinct Labels/Tags Proxy APIs

AssistantHub needs to proxy RecallDB's distinct endpoints so the dashboard and chat UI can discover available filter values.

### 4.1 Add proxy handler for distinct labels
- [x]**File:** `src/AssistantHub.Server/Handlers/CollectionHandler.cs` (or new `MetadataHandler.cs`)
- [x]Route: `GET /v1.0/collections/{collectionId}/labels/distinct`
- [x]Proxy to RecallDB: `GET /v1.0/tenants/{tenantId}/collections/{collectionId}/labels/distinct`
- [x]Return JSON array of unique label strings
- [x]Requires authenticated user (tenant-scoped)

### 4.2 Add proxy handler for distinct tag keys
- [x]Route: `GET /v1.0/collections/{collectionId}/tags/distinct`
- [x]Proxy to RecallDB: `GET /v1.0/tenants/{tenantId}/collections/{collectionId}/tags/distinct`
- [x]Return JSON array of unique tag key strings
- [x]Requires authenticated user (tenant-scoped)

### 4.3 Register routes in `AssistantHubServer`
- [x]**File:** `src/AssistantHub.Server/AssistantHubServer.cs`
- [x]Add route registrations for the two new GET endpoints

### 4.4 Add public distinct endpoints for chat UI
The standalone chat page does not have admin auth. Expose filtered-down versions scoped to the assistant's collection:
- [x]Route: `GET /v1.0/assistants/{assistantId}/labels/distinct`
- [x]Route: `GET /v1.0/assistants/{assistantId}/tags/distinct`
- [x]Looks up the assistant's `CollectionId` from settings, then proxies to RecallDB
- [x]No auth required (same as the public chat endpoint)

---

## Phase 5: Dashboard — Assistant Settings Configuration

### 5.1 Add metadata filter section to assistant settings form
- [x]**File:** `dashboard/src/views/AssistantSettingsView.jsx`
- [x]Add a "Retrieval Filters" collapsible section (visible when RAG is enabled)
- [x]**Required Labels:** multi-select/tag-input populated from `GET /v1.0/collections/{collectionId}/labels/distinct`
- [x]**Excluded Labels:** same pattern
- [x]**Required Tags:** repeatable row with Key (dropdown from distinct tag keys), Condition (dropdown: Equals, NotEquals, Contains, StartsWith, EndsWith, IsNull, IsNotNull, GreaterThan, LessThan), Value (text input)
- [x]**Excluded Tags:** same pattern
- [x]Serialize to JSON and save as `RetrievalLabelFilter` / `RetrievalTagFilter` on the assistant settings object

### 5.2 Handle empty collection gracefully
- [x]If no `CollectionId` is set on the assistant, show a note that filters require a collection to be assigned first
- [x]If the distinct API returns an empty array, allow freeform text entry for labels/tag keys

---

## Phase 6: Dashboard — Chat Experience

### 6.1 Add filter controls to `ChatPanel`
- [x]**File:** `dashboard/src/components/ChatPanel.jsx`
- [x]Add a collapsible "Filters" toolbar or sidebar panel in the chat header area
- [x]Show current active filters (labels and tags) as removable chips/badges
- [x]"Add Filter" button opens a popover/dropdown:
  - **Labels tab:** checkbox list populated from `GET /v1.0/assistants/{assistantId}/labels/distinct`, with search/filter
  - **Tags tab:** key dropdown (from distinct tag keys), condition dropdown, value input
- [x]Store active filters in component state
- [x]When sending a message, include active filters in the `metadata_filter` field of the `ChatCompletionRequest`

### 6.2 Merge with assistant defaults
- [x]On chat panel load, fetch assistant's public info (already done) — extend to include default filters
- [x]Pre-populate the filter UI with the assistant's default filters
- [x]User can add/remove filters within the chat session; changes are per-conversation only
- [x]Visual indicator when assistant-level default filters are active

### 6.3 Update `ChatDrawer` (dashboard embedded chat)
- [x]**File:** `dashboard/src/components/ChatDrawer.jsx`
- [x]Same filter capability as `ChatPanel`, or pass-through if `ChatDrawer` wraps `ChatPanel`

---

## Phase 7: OpenAI-Compatible API Support

### 7.1 Document the `metadata_filter` extension field
- [x]The `metadata_filter` field in `ChatCompletionRequest` is an AssistantHub extension to the OpenAI chat completion schema
- [x]OpenAI-compatible clients that don't know about it will simply omit it (null), which is fine

### 7.2 Support metadata_filter in Ollama API path
- [x]**File:** `src/AssistantHub.Server/Handlers/ChatHandler.cs`
- [x]Verify that the Ollama-compatible chat route also deserializes and applies `metadata_filter` from the request body (should work if both paths share the same `PostChatAsync` handler)

---

## Phase 8: Test Projects

### 8.1 Model tests
- [x]**File:** `src/Test.Models/Tests/CoreModelTests.cs` (or new test file)
- [x]Test `ChatMetadataFilter` serialization/deserialization round-trip
- [x]Test `ChatTagCondition` serialization/deserialization
- [x]Test `AssistantSettings` with filter fields populated and `FromDataRow()`

### 8.2 Database tests
- [x]**File:** `src/Test.Database/Tests/AssistantSettingsTests.cs`
- [x]Test CRUD for `AssistantSettings` with `RetrievalLabelFilter` and `RetrievalTagFilter` populated
- [x]Verify null filter fields work (backwards compatibility)
- [x]**File:** `src/Test.Database/Tests/ChatHistoryTests.cs`
- [x]Test CRUD for `ChatHistory` with `MetadataFilter` populated (JSON string)
- [x]Verify null `MetadataFilter` works (backwards compatibility)

### 8.3 Service tests
- [x]**File:** `src/Test.Services/` (new or existing test file)
- [x]Test filter merging logic: assistant-level only, request-level only, both merged
- [x]Test that empty/null filters produce no filter clause
- [x]Test `RetrievalService.BuildSearchBody()` includes `LabelFilter` and `TagFilter` when metadata filter is set

### 8.4 API tests
- [x]**File:** `src/Test.Api/Tests/` (new test file, e.g., `MetadataFilterTests.cs`)
- [x]Test `POST /v1.0/assistants/{id}/chat` with `metadata_filter` in request body
- [x]Test `GET /v1.0/collections/{id}/labels/distinct` returns expected format
- [x]Test `GET /v1.0/collections/{id}/tags/distinct` returns expected format
- [x]Test `GET /v1.0/assistants/{id}/labels/distinct` (public endpoint)
- [x]Test `GET /v1.0/assistants/{id}/tags/distinct` (public endpoint)

### 8.5 Integration tests
- [x]**File:** `src/Test.Integration/Tests/` (new test file, e.g., `MetadataFilterIntegrationTests.cs`)
- [x]End-to-end: upload documents with labels/tags → chat with filter → verify only filtered documents appear in context

---

## Phase 9: Documentation

### 9.1 Update REST_API.md
- [x]**File:** `REST_API.md`
- [x]Document `metadata_filter` field in `POST /v1.0/assistants/{assistantId}/chat` request body
- [x]Document new endpoints:
  - `GET /v1.0/collections/{collectionId}/labels/distinct`
  - `GET /v1.0/collections/{collectionId}/tags/distinct`
  - `GET /v1.0/assistants/{assistantId}/labels/distinct`
  - `GET /v1.0/assistants/{assistantId}/tags/distinct`
- [x]Document `RetrievalLabelFilter` and `RetrievalTagFilter` fields in assistant settings
- [x]Include request/response examples with filter objects

### 9.2 Update README.md
- [x]**File:** `README.md`
- [x]Add metadata filtering to the feature list
- [x]Brief description of label and tag filtering in the RAG/retrieval section
- [x]Link to REST_API.md for detailed API docs

### 9.3 Update CHANGELOG.md
- [x]**File:** `CHANGELOG.md`
- [x]Add entry for v0.7.0: metadata filtering feature

---

## Phase 10: Docker Assets

### 10.1 Update factory database
- [x]**File:** `docker/factory/assistanthub.db`
- [x]Regenerate the factory SQLite database with the updated schema (includes `retrieval_label_filter` and `retrieval_tag_filter` columns in `assistant_settings`, `metadata_filter` column in `chat_history`)

### 10.2 Update runtime database seed
- [x]**File:** `docker/assistanthub/data/assistanthub.db`
- [x]Same schema update as factory

### 10.3 Create migration script
- [x]**File:** `migrations/005_upgrade_to_v0.7.0.sql` (new file)
- [x]Follow the established pattern from `004_upgrade_to_v0.6.0.sql`: SQLite active, PostgreSQL/MySQL/SQL Server commented out
- [x]Contents:

```sql
-- Migration 005: Upgrade existing v0.6.0 database to v0.7.0
-- Adds metadata filtering columns to assistant_settings and chat_history
-- Back up your database before running this migration.

------------------------------------------------------------------------
-- SQLite
------------------------------------------------------------------------
ALTER TABLE assistant_settings ADD COLUMN retrieval_label_filter TEXT;
ALTER TABLE assistant_settings ADD COLUMN retrieval_tag_filter TEXT;

ALTER TABLE chat_history ADD COLUMN metadata_filter TEXT;

------------------------------------------------------------------------
-- PostgreSQL
------------------------------------------------------------------------
-- ALTER TABLE assistant_settings ADD COLUMN retrieval_label_filter TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN retrieval_tag_filter TEXT;

-- ALTER TABLE chat_history ADD COLUMN metadata_filter TEXT;

------------------------------------------------------------------------
-- MySQL
------------------------------------------------------------------------
-- ALTER TABLE assistant_settings ADD COLUMN retrieval_label_filter TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN retrieval_tag_filter TEXT;

-- ALTER TABLE chat_history ADD COLUMN metadata_filter TEXT;

------------------------------------------------------------------------
-- SQL Server
------------------------------------------------------------------------
-- ALTER TABLE assistant_settings ADD retrieval_label_filter NVARCHAR(MAX);
-- ALTER TABLE assistant_settings ADD retrieval_tag_filter NVARCHAR(MAX);

-- ALTER TABLE chat_history ADD metadata_filter NVARCHAR(MAX);
```

---

## Appendix A: Chat Request JSON Example

### POST /v1.0/assistants/{assistantId}/chat

```json
{
  "model": "gemma3:4b",
  "messages": [
    {
      "role": "user",
      "content": "What were the key findings in the Q4 2024 financial report?"
    }
  ],
  "metadata_filter": {
    "required_labels": ["finance", "quarterly-report"],
    "excluded_labels": ["draft"],
    "required_tags": [
      { "key": "department", "condition": "Equals", "value": "accounting" },
      { "key": "year", "condition": "GreaterThan", "value": "2023" }
    ],
    "excluded_tags": [
      { "key": "status", "condition": "Equals", "value": "archived" }
    ]
  },
  "stream": true,
  "temperature": 0.7,
  "max_tokens": 4096
}
```

**Behavior:**
- If the assistant also has default filters configured (e.g., `required_labels: ["internal"]`), they are merged with the request-level filters. The effective filter becomes `required_labels: ["finance", "quarterly-report", "internal"]`.
- Only documents in RecallDB that match ALL required labels, NONE of the excluded labels, ALL required tag conditions, and NONE of the excluded tag conditions will be returned during retrieval.
- The effective merged filter is stored in the `chat_history` record for auditing and is visible in the history view modal.

### Minimal request (no filters — backwards compatible)

```json
{
  "messages": [
    { "role": "user", "content": "Hello, how can you help me?" }
  ]
}
```

When `metadata_filter` is omitted or null, behavior is identical to today — no label/tag filtering is applied during retrieval.

---

## Appendix B: RecallDB Search Body Reference

The RecallDB search endpoint (`POST /v1.0/tenants/{tid}/collections/{cid}/search`) accepts the following filter structure within the `SearchQuery` body:

```json
{
  "Vector": { "SearchType": "CosineSimilarity", "Embeddings": [...] },
  "LabelFilter": {
    "Required": ["label1", "label2"],
    "Excluded": ["label3"]
  },
  "TagFilter": {
    "Required": [
      { "Key": "category", "Condition": "Equals", "Value": "finance" },
      { "Key": "year", "Condition": "GreaterThan", "Value": "2023" }
    ],
    "Excluded": [
      { "Key": "status", "Condition": "Equals", "Value": "draft" }
    ]
  },
  "MaxResults": 10
}
```

**TagCondition operators:** Equals, NotEquals, GreaterThan, LessThan, Contains, ContainsNot, StartsWith, EndsWith, IsNull, IsNotNull

---

## Summary of New/Modified Files

| File | Action |
|------|--------|
| `src/AssistantHub.Core/Models/ChatMetadataFilter.cs` | **New** |
| `src/AssistantHub.Core/Models/ChatTagCondition.cs` | **New** |
| `src/AssistantHub.Core/Models/AssistantSettings.cs` | Modified |
| `src/AssistantHub.Core/Models/ChatHistory.cs` | Modified |
| `src/AssistantHub.Core/Models/ChatCompletionRequest.cs` | Modified |
| `src/AssistantHub.Core/Models/RetrievalSearchOptions.cs` | Modified |
| `src/AssistantHub.Core/Services/RetrievalService.cs` | Modified |
| `src/AssistantHub.Core/Database/*/Queries/TableQueries.cs` (×4) | Modified (assistant_settings + chat_history) |
| `src/AssistantHub.Core/Database/*/Implementations/AssistantSettingsMethods.cs` (×4) | Modified |
| `src/AssistantHub.Core/Database/*/Implementations/ChatHistoryMethods.cs` (×4) | Modified |
| `src/AssistantHub.Server/Handlers/ChatHandler.cs` | Modified |
| `src/AssistantHub.Server/Handlers/CollectionHandler.cs` (or new handler) | Modified/New |
| `src/AssistantHub.Server/AssistantHubServer.cs` | Modified |
| `dashboard/src/views/AssistantSettingsView.jsx` | Modified |
| `dashboard/src/components/ChatPanel.jsx` | Modified |
| `dashboard/src/components/ChatDrawer.jsx` | Modified |
| `dashboard/src/components/modals/HistoryViewModal.jsx` | Modified |
| `src/Test.Models/Tests/CoreModelTests.cs` | Modified |
| `src/Test.Database/Tests/AssistantSettingsTests.cs` | Modified |
| `src/Test.Api/Tests/MetadataFilterTests.cs` | **New** |
| `src/Test.Integration/Tests/MetadataFilterIntegrationTests.cs` | **New** |
| `REST_API.md` | Modified |
| `README.md` | Modified |
| `CHANGELOG.md` | Modified |
| `migrations/005_upgrade_to_v0.7.0.sql` | **New** |
| `docker/factory/assistanthub.db` | Regenerated |
| `docker/assistanthub/data/assistanthub.db` | Regenerated |
