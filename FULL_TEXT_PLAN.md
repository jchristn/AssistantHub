# Full-Text Search Integration Plan

This plan integrates RecallDB's full-text and hybrid search capabilities into AssistantHub, allowing users to configure search mode per-assistant via the dashboard. It also fixes default assistant creation to auto-assign configured endpoints instead of "Use server default".

Breaking changes are acceptable. No backward compatibility is required.

**Status Legend:** `[ ]` Pending · `[~]` In Progress · `[x]` Complete

---

## Phase 1: Backend Model Changes

### 1.1 Update `AssistantSettings` Model
`[ ]` **File:** `src/AssistantHub.Core/Models/AssistantSettings.cs`

Add the following properties to the `AssistantSettings` class:

```csharp
/// <summary>
/// Search mode for retrieval: Vector, FullText, or Hybrid.
/// </summary>
public string SearchMode { get; set; } = "Vector";

/// <summary>
/// Weight of full-text score in hybrid mode (0.0 to 1.0).
/// Formula: Score = (1.0 - TextWeight) * vectorScore + TextWeight * textScore.
/// Only applies when SearchMode is "Hybrid".
/// </summary>
public double TextWeight { get; set; } = 0.3;

/// <summary>
/// Full-text ranking function: "TsRank" (term frequency) or "TsRankCd" (cover density, rewards proximity).
/// </summary>
public string FullTextSearchType { get; set; } = "TsRank";

/// <summary>
/// PostgreSQL text search language configuration.
/// Controls stemming and stop words.
/// </summary>
public string FullTextLanguage { get; set; } = "english";

/// <summary>
/// Full-text score normalization bitmask. 32 = normalized 0-1 (recommended for hybrid).
/// </summary>
public int FullTextNormalization { get; set; } = 32;

/// <summary>
/// Minimum full-text score threshold. Documents with TextScore below this are excluded.
/// Null means no threshold.
/// </summary>
public double? FullTextMinimumScore { get; set; } = null;
```

**Validation rules:**
- `SearchMode` must be one of: `"Vector"`, `"FullText"`, `"Hybrid"` (case-insensitive)
- `TextWeight` clamped to `[0.0, 1.0]`
- `FullTextSearchType` must be one of: `"TsRank"`, `"TsRankCd"`
- `FullTextNormalization` must be one of: `0`, `1`, `2`, `32`

### 1.2 Create `RetrievalSearchOptions` Parameter Object
`[ ]` **File:** `src/AssistantHub.Core/Models/RetrievalSearchOptions.cs` (new file)

```csharp
public class RetrievalSearchOptions
{
    public string SearchMode { get; set; } = "Vector";
    public double TextWeight { get; set; } = 0.3;
    public string FullTextSearchType { get; set; } = "TsRank";
    public string FullTextLanguage { get; set; } = "english";
    public int FullTextNormalization { get; set; } = 32;
    public double? FullTextMinimumScore { get; set; } = null;
}
```

---

## Phase 2: Database Schema — All Providers

### 2.1 Update Database Schema
`[ ]` **Files:**
- `src/AssistantHub.Core/Database/Sqlite/Implementations/AssistantSettingsMethods.cs`
- `src/AssistantHub.Core/Database/Mysql/Implementations/AssistantSettingsMethods.cs` (if exists)
- `src/AssistantHub.Core/Database/Postgresql/Implementations/AssistantSettingsMethods.cs` (if exists)
- `src/AssistantHub.Core/Database/SqlServer/Implementations/AssistantSettingsMethods.cs` (if exists)

Add columns to the `assistant_settings` table:

```sql
search_mode TEXT DEFAULT 'Vector'
text_weight REAL DEFAULT 0.3
fulltext_search_type TEXT DEFAULT 'TsRank'
fulltext_language TEXT DEFAULT 'english'
fulltext_normalization INTEGER DEFAULT 32
fulltext_minimum_score REAL DEFAULT NULL
```

Update all CRUD methods (Create, Read, Update) to include the new columns in:
- INSERT statements
- SELECT statements
- UPDATE statements
- Object mapping (reader to model)

### 2.2 Update Factory Database
`[ ]` **File:** `docker/factory/assistanthub.db`

Rebuild the factory SQLite database to include the new columns in the `assistant_settings` table schema. This database serves as the clean-slate template used by `docker/factory/reset.sh` and `docker/factory/reset.bat`. The new columns must be present so that factory resets produce a database compatible with the updated code.

---

## Phase 3: Retrieval Service Changes

### 3.1 Change `RetrievalService.RetrieveAsync()` Signature
`[ ]` **File:** `src/AssistantHub.Core/Services/RetrievalService.cs`

Replace the current method signature with one accepting `RetrievalSearchOptions`:

```csharp
public async Task<List<RetrievalChunk>> RetrieveAsync(
    string collectionId,
    string query,
    int topK,
    double scoreThreshold,
    CancellationToken token = default,
    string embeddingEndpointId = null,
    RetrievalSearchOptions searchOptions = null)
```

Default `searchOptions` to a new `RetrievalSearchOptions()` instance if null.

### 3.2 Skip Embedding Step for FullText-Only Mode
`[ ]` **File:** `src/AssistantHub.Core/Services/RetrievalService.cs`

When `searchOptions.SearchMode == "FullText"`, skip the embedding step entirely — no call to Partio `/v1.0/process`, no embedding endpoint needed, faster execution:

```csharp
List<double> queryEmbeddings = null;

if (!searchOptions.SearchMode.Equals("FullText", StringComparison.OrdinalIgnoreCase))
{
    queryEmbeddings = await EmbedQueryAsync(query, token, embeddingEndpointId).ConfigureAwait(false);
}
```

### 3.3 Modify RecallDB Search Request Construction
`[ ]` **File:** `src/AssistantHub.Core/Services/RetrievalService.cs`

Replace the current vector-only request body construction with conditional logic based on `searchOptions.SearchMode`:

**Vector mode** (current behavior):
```json
{ "Vector": { "SearchType": "CosineSimilarity", "Embeddings": [...] }, "MaxResults": N }
```

**FullText mode** (new):
```json
{
  "FullText": {
    "Query": "<user message>",
    "SearchType": "<TsRank|TsRankCd>",
    "Language": "<language>",
    "Normalization": <norm>,
    "MinimumScore": <threshold|null>
  },
  "MaxResults": N
}
```

**Hybrid mode** (new):
```json
{
  "Vector": { "SearchType": "CosineSimilarity", "Embeddings": [...] },
  "FullText": {
    "Query": "<user message>",
    "SearchType": "<TsRank|TsRankCd>",
    "Language": "<language>",
    "Normalization": <norm>,
    "TextWeight": <weight>,
    "MinimumScore": <threshold|null>
  },
  "MaxResults": N
}
```

Extract the HTTP call to RecallDB into a helper method (`ExecuteSearchAsync`) to avoid code duplication between the initial search and fallback.

### 3.4 Implement Hybrid Fallback to Vector-Only
`[ ]` **File:** `src/AssistantHub.Core/Services/RetrievalService.cs`

When `searchMode == "Hybrid"` and the initial search returns 0 results, automatically retry with vector-only (embeddings are already computed):

```csharp
if (searchOptions.SearchMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase) && results.Count == 0)
{
    Logging.Info(_Header + "hybrid search returned 0 results, falling back to vector-only");
    results = await ExecuteSearchAsync(collectionId, vectorOnlyBody, token).ConfigureAwait(false);
}
```

### 3.5 Handle `TextScore` in Response Parsing
`[ ]` **Files:**
- `src/AssistantHub.Core/Models/RetrievalChunk.cs`
- `src/AssistantHub.Core/Services/RetrievalService.cs`

Add `TextScore` to `RetrievalChunk`:

```csharp
/// <summary>
/// Full-text relevance score component (null in vector-only mode).
/// </summary>
public double? TextScore { get; set; }
```

Parse `TextScore` from the RecallDB search response alongside existing `Score` and `Distance` fields.

---

## Phase 4: Chat Handler Integration

### 4.1 Pass Search Options to RetrievalService
`[ ]` **File:** `src/AssistantHub.Server/Handlers/ChatHandler.cs`

Update the call to `Retrieval.RetrieveAsync()` (around line 192) to pass `RetrievalSearchOptions` built from `AssistantSettings`:

```csharp
List<RetrievalChunk> retrieved = await Retrieval.RetrieveAsync(
    settings.CollectionId,
    lastUserMessage,
    settings.RetrievalTopK,
    settings.RetrievalScoreThreshold,
    default,
    settings.EmbeddingEndpointId,
    new RetrievalSearchOptions
    {
        SearchMode = settings.SearchMode,
        TextWeight = settings.TextWeight,
        FullTextSearchType = settings.FullTextSearchType,
        FullTextLanguage = settings.FullTextLanguage,
        FullTextNormalization = settings.FullTextNormalization,
        FullTextMinimumScore = settings.FullTextMinimumScore
    }
).ConfigureAwait(false);
```

### 4.2 Validate SearchMode at Settings Update Time
`[ ]` **File:** `src/AssistantHub.Server/Handlers/AssistantSettingsHandler.cs`

Add validation when settings are saved (PUT endpoint):

```csharp
string[] validSearchModes = { "Vector", "FullText", "Hybrid" };
if (!string.IsNullOrEmpty(settings.SearchMode) &&
    !validSearchModes.Contains(settings.SearchMode, StringComparer.OrdinalIgnoreCase))
{
    // Return 400 Bad Request
}

settings.TextWeight = Math.Clamp(settings.TextWeight, 0.0, 1.0);

string[] validSearchTypes = { "TsRank", "TsRankCd" };
if (!string.IsNullOrEmpty(settings.FullTextSearchType) &&
    !validSearchTypes.Contains(settings.FullTextSearchType, StringComparer.OrdinalIgnoreCase))
{
    settings.FullTextSearchType = "TsRank";
}
```

---

## Phase 5: Default Assistant Settings — Auto-Assign Endpoints

### 5.1 Auto-Assign First Available Endpoints on Assistant Creation
`[ ]` **File:** `src/AssistantHub.Server/Handlers/AssistantHandler.cs`

Currently (lines 75-102), new assistants are created with `InferenceEndpointId` and `EmbeddingEndpointId` left as `null`, resulting in "Use server default". Fix this to query Partio for available endpoints and assign the first one found.

After the existing RAG auto-assignment block, add:

```csharp
// Auto-assign first available inference endpoint
try
{
    string completionUrl = Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/completion/enumerate";
    using (HttpClient client = new HttpClient())
    {
        if (!String.IsNullOrEmpty(Settings.Chunking.AccessKey))
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Settings.Chunking.AccessKey);

        var enumBody = new { MaxResults = 1 };
        var content = new StringContent(
            JsonSerializer.Serialize(enumBody),
            Encoding.UTF8, "application/json");
        var response = await client.PostAsync(completionUrl, content).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<JsonElement>(body);
            if (result.TryGetProperty("Objects", out var objects) && objects.GetArrayLength() > 0)
            {
                string endpointId = objects[0].GetProperty("Id").GetString();
                if (!String.IsNullOrEmpty(endpointId))
                    settings.InferenceEndpointId = endpointId;
            }
        }
    }
}
catch (Exception ex)
{
    Logging.Warn(_Header + "unable to auto-assign inference endpoint: " + ex.Message);
}

// Auto-assign first available embedding endpoint
try
{
    string embeddingUrl = Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/embedding/enumerate";
    using (HttpClient client = new HttpClient())
    {
        if (!String.IsNullOrEmpty(Settings.Chunking.AccessKey))
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Settings.Chunking.AccessKey);

        var enumBody = new { MaxResults = 1 };
        var content = new StringContent(
            JsonSerializer.Serialize(enumBody),
            Encoding.UTF8, "application/json");
        var response = await client.PostAsync(embeddingUrl, content).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<JsonElement>(body);
            if (result.TryGetProperty("Objects", out var objects) && objects.GetArrayLength() > 0)
            {
                string endpointId = objects[0].GetProperty("Id").GetString();
                if (!String.IsNullOrEmpty(endpointId))
                    settings.EmbeddingEndpointId = endpointId;
            }
        }
    }
}
catch (Exception ex)
{
    Logging.Warn(_Header + "unable to auto-assign embedding endpoint: " + ex.Message);
}
```

Consider extracting the Partio enumerate call into a shared helper. If Partio is unreachable, the endpoints remain null and server defaults still work.

---

## Phase 6: Dashboard UI Changes

### 6.1 Add Search Mode to Settings Form
`[ ]` **File:** `dashboard/src/components/modals/AssistantSettingsFormModal.jsx`

Add a new "Search Mode" section within the Retrieval (RAG) settings area, visible when RAG is enabled.

**New form fields (initial state defaults):**
```javascript
SearchMode: settings?.SearchMode || 'Vector',
TextWeight: settings?.TextWeight ?? 0.3,
FullTextSearchType: settings?.FullTextSearchType || 'TsRank',
FullTextLanguage: settings?.FullTextLanguage || 'english',
FullTextNormalization: settings?.FullTextNormalization ?? 32,
FullTextMinimumScore: settings?.FullTextMinimumScore ?? '',
```

**UI Layout (inside RAG section, after Score Threshold):**

```
Search Mode
┌──────────────────────────────────────────┐
│  [Vector ▾]  [FullText]  [Hybrid]        │  ← Radio group or select
└──────────────────────────────────────────┘

── Shown only when SearchMode is "Hybrid" ──

Text Weight                          0.3
┌──────────────────────────────────────────┐
│  ├──────●──────────────────────────────┤  │  ← Slider 0.0-1.0, step 0.05
│  Vector-heavy          Text-heavy        │
└──────────────────────────────────────────┘

── Shown when SearchMode is "Hybrid" or "FullText" ──

Full-Text Ranking       [TsRank ▾]
                         TsRank
                         TsRankCd

Language                [english ▾]
                         english
                         simple
                         spanish
                         french
                         german

Minimum Text Score      [________] (optional, 0.0-1.0)
```

### 6.2 Conditionally Show/Hide Fields
`[ ]` **File:** `dashboard/src/components/modals/AssistantSettingsFormModal.jsx`

- **SearchMode = "Vector"**: Hide all full-text fields (TextWeight, FullTextSearchType, Language, Normalization, MinimumScore). Current behavior.
- **SearchMode = "FullText"**: Show FullTextSearchType, Language, MinimumScore. Hide TextWeight (not relevant). Hide EmbeddingEndpoint selector (not needed for fulltext-only).
- **SearchMode = "Hybrid"**: Show all full-text fields including TextWeight.

### 6.3 Add Tooltips
`[ ]` **File:** `dashboard/src/components/modals/AssistantSettingsFormModal.jsx`

| Field | Tooltip |
|-------|---------|
| Search Mode | How documents are retrieved: Vector (semantic similarity), FullText (keyword matching), or Hybrid (both combined). |
| Text Weight | Balance between vector and text scoring in hybrid mode. 0.0 = pure vector, 1.0 = pure text. Recommended: 0.3 for quality embeddings. |
| Full-Text Ranking | TsRank: standard term frequency scoring. TsRankCd: cover density, rewards terms appearing close together. |
| Language | Text search language for stemming and stop words. Use "simple" to disable stemming. |
| Minimum Text Score | Documents with text relevance below this threshold are excluded. Leave empty for no threshold. |

### 6.4 Include New Fields in Settings Save Payload
`[ ]` **File:** `dashboard/src/components/modals/AssistantSettingsFormModal.jsx`

Ensure the `handleSave` function includes the new fields in the PUT request body. Handle type conversions:
- `TextWeight` -> parse as float
- `FullTextNormalization` -> parse as int
- `FullTextMinimumScore` -> parse as float or null if empty

---

## Phase 7: Docker Configuration

### 7.1 Verify RecallDB Version Supports Full-Text Search
`[ ]` **File:** `docker/compose.yaml`

Verify the `recalldb-server` image tag in `compose.yaml` is a version that includes full-text search support (GIN tsvector index auto-creation). If not, update the image tag to the required version.

### 7.2 Update Factory Reset Scripts
`[ ]` **Files:**
- `docker/factory/reset.sh`
- `docker/factory/reset.bat`

No changes expected to the reset scripts themselves since they copy the factory database as-is. The updated `docker/factory/assistanthub.db` (Phase 2.2) handles schema changes.

### 7.3 Verify docker/assistanthub Configuration
`[ ]` **File:** `docker/assistanthub/assistanthub.json` (if exists)

No changes needed to `assistanthub.json`. Full-text search settings are per-assistant (stored in the database), not server-wide configuration. However, verify that no server-wide config blocks the new functionality.

---

## Phase 8: Documentation Updates

### 8.1 Update `REST_API.md` — Assistant Settings Section
`[ ]` **File:** `REST_API.md`

**Update the `GET /v1.0/assistants/{assistantId}/settings` response example** (around line 1290) to include the new fields:

```json
{
  "Id": "aset_abc123...",
  "AssistantId": "asst_abc123...",
  "Temperature": 0.7,
  "TopP": 1.0,
  "SystemPrompt": "You are a helpful assistant...",
  "MaxTokens": 4096,
  "ContextWindow": 8192,
  "Model": "gpt-4o",
  "EnableRag": true,
  "CollectionId": "collection-uuid",
  "RetrievalTopK": 5,
  "RetrievalScoreThreshold": 0.7,
  "SearchMode": "Hybrid",
  "TextWeight": 0.3,
  "FullTextSearchType": "TsRank",
  "FullTextLanguage": "english",
  "FullTextNormalization": 32,
  "FullTextMinimumScore": null,
  "InferenceEndpointId": "ep_abc123...",
  "EmbeddingEndpointId": "ep_def456...",
  "Title": "My Support Bot",
  "LogoUrl": "https://example.com/logo.png",
  "FaviconUrl": "https://example.com/favicon.ico",
  "Streaming": true,
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Add rows to the Field Descriptions table** (around line 1316):

| Field | Type | Description |
|-------|------|-------------|
| `SearchMode` | string | Search mode for RAG retrieval: `Vector` (semantic similarity), `FullText` (keyword matching), or `Hybrid` (both combined). Default `Vector`. |
| `TextWeight` | double | Weight of full-text score in hybrid mode (0.0 to 1.0). Formula: `Score = (1 - TextWeight) * vectorScore + TextWeight * textScore`. Default `0.3`. |
| `FullTextSearchType` | string | Full-text ranking function: `TsRank` (term frequency) or `TsRankCd` (cover density, rewards term proximity). Default `TsRank`. |
| `FullTextLanguage` | string | PostgreSQL text search language for stemming and stop words. Values: `english`, `simple`, `spanish`, `french`, `german`. Default `english`. |
| `FullTextNormalization` | int | Score normalization bitmask. `32` = normalized 0-1 (recommended). `0` = raw scores. Default `32`. |
| `FullTextMinimumScore` | double? | Minimum full-text relevance threshold. Documents below this TextScore are excluded. Null = no threshold. |

**Update the `PUT /v1.0/assistants/{assistantId}/settings` request example** (around line 1347) to include the new fields in the request body.

### 8.2 Update `CHAT_DATA_FLOW.md` — Phase 4 RAG Retrieval
`[ ]` **File:** `CHAT_DATA_FLOW.md`

**Update the `RetrieveAsync` call** (around line 344) to show the new `RetrievalSearchOptions` parameter:

```csharp
List<RetrievalChunk> retrieved = await Retrieval.RetrieveAsync(
    settings.CollectionId,
    lastUserMessage,
    settings.RetrievalTopK,
    settings.RetrievalScoreThreshold,
    default,
    settings.EmbeddingEndpointId,
    new RetrievalSearchOptions
    {
        SearchMode = settings.SearchMode,
        TextWeight = settings.TextWeight,
        FullTextSearchType = settings.FullTextSearchType,
        FullTextLanguage = settings.FullTextLanguage,
        FullTextNormalization = settings.FullTextNormalization,
        FullTextMinimumScore = settings.FullTextMinimumScore
    }
);
```

**Update Step 4.1** (around line 357): Add a note that the embedding step is skipped when `SearchMode == "FullText"`.

**Update Step 4.2** (around line 428): Replace the section title from "Vector Search in RecallDB" to "Search in RecallDB" and show all three request body variants (vector, fulltext, hybrid).

**Update the RecallDB capabilities table** (around line 469): Change the "Used by AssistantHub?" column to reflect that `FullText` fields are now used when SearchMode is FullText or Hybrid.

| Field | Used by AssistantHub? |
|-------|----------------------|
| `FullText.Query` | Yes — when SearchMode is FullText or Hybrid |
| `FullText.SearchType` | Yes — configurable via FullTextSearchType |
| `FullText.Language` | Yes — configurable via FullTextLanguage |
| `FullText.Normalization` | Yes — configurable via FullTextNormalization |
| `FullText.TextWeight` | Yes — configurable via TextWeight (Hybrid only) |
| `FullText.MinimumScore` | Yes — configurable via FullTextMinimumScore |

**Remove the paragraph** (line 486) that says "AssistantHub only uses vector search" and replace with a description of the three search modes now supported.

**Add a note about hybrid fallback**: When hybrid returns 0 results, the system automatically retries with vector-only search.

**Update the AssistantSettings table** (around line 299) to include the new fields:

| Field | Default | Description |
|-------|---------|-------------|
| `SearchMode` | `"Vector"` | RAG search mode: Vector, FullText, or Hybrid |
| `TextWeight` | `0.3` | Full-text weight in hybrid scoring |
| `FullTextSearchType` | `"TsRank"` | Ranking function |
| `FullTextLanguage` | `"english"` | Text search language |
| `FullTextNormalization` | `32` | Score normalization |
| `FullTextMinimumScore` | `null` | Minimum text score threshold |

### 8.3 Update `README.md` — Features and Data Flow
`[ ]` **File:** `README.md`

**Update the Features section** (around line 16):

Change:
```
- **Embeddings** -- Leverages pgvector and RecallDb for vector storage and similarity search, enabling accurate context retrieval from your document corpus.
```
To:
```
- **Search** -- Leverages pgvector and RecallDb for vector, full-text, and hybrid search. Configure per-assistant search modes with tunable scoring weights for optimal retrieval from your document corpus.
```

**Update the chat data flow summary** (around line 247):

Change:
```
2. The server queries RecallDb for relevant document chunks using vector similarity search.
```
To:
```
2. The server queries RecallDb for relevant document chunks using the assistant's configured search mode (vector, full-text, or hybrid).
```

**Update the services table** (around line 44): Change RecallDB description:

Change:
```
| recalldb-server        | 8401  | Vector database service            |
```
To:
```
| recalldb-server        | 8401  | Vector and full-text search service |
```

### 8.4 Update XML Documentation
`[ ]` **File:** `src/AssistantHub.Core/AssistantHub.Core.xml`

Auto-generated from XML doc comments on rebuild. No manual edits needed. Rebuild the project after Phase 1 to regenerate.

---

## Phase 9: Testing and Verification

### 9.1 Backend Smoke Tests
`[ ]` Verify the following scenarios end-to-end:

1. **Vector mode**
   - Create assistant -> settings have `SearchMode=Vector`
   - Chat with RAG enabled -> retrieval uses vector-only search
   - No `FullText` object in RecallDB request

2. **FullText mode**
   - Set `SearchMode=FullText` on an assistant
   - Chat -> retrieval skips embedding, sends only `FullText` query to RecallDB
   - No embedding endpoint is called
   - Results ranked by text relevance

3. **Hybrid mode**
   - Set `SearchMode=Hybrid`, `TextWeight=0.3`
   - Chat -> retrieval generates embeddings AND sends `FullText` query
   - Results ranked by blended score
   - `TextScore` populated in response

4. **Hybrid fallback**
   - Set `SearchMode=Hybrid` with a very specific text query that matches 0 documents
   - Verify fallback to vector-only search
   - Results returned from vector search

5. **Default endpoint assignment**
   - Create a new assistant when completion/embedding endpoints exist in Partio
   - Verify `InferenceEndpointId` and `EmbeddingEndpointId` are populated (not null)
   - Verify the assistant works with the assigned endpoints

6. **Default endpoint when none exist**
   - Create a new assistant when no endpoints are configured in Partio
   - Verify `InferenceEndpointId` and `EmbeddingEndpointId` remain null
   - Verify the assistant still works using server defaults

### 9.2 Dashboard UI Tests
`[ ]` Verify the following in the dashboard:

1. Settings form loads with new search mode fields
2. Search Mode selector shows/hides appropriate fields
3. TextWeight slider works and displays value
4. Full-text options visible for FullText and Hybrid modes
5. Embedding Endpoint selector hidden for FullText mode
6. Settings save and reload correctly with new fields
7. New assistants show assigned endpoints instead of "Use server default" (when endpoints exist)

### 9.3 Docker Full-Stack Test
`[ ]` Verify with `docker compose up -d`:

1. Factory reset (`docker/factory/reset.sh`) produces a clean database with new schema
2. All services start without errors
3. Create assistant via dashboard -> settings include search mode fields
4. Chat works with each search mode (vector, fulltext, hybrid)
5. RecallDB receives correctly structured search requests

### 9.4 Documentation Review
`[ ]` Verify all documentation is accurate:

1. `REST_API.md` response examples match actual API responses
2. `CHAT_DATA_FLOW.md` accurately describes the updated retrieval flow
3. `README.md` features list reflects full-text search capability

---

## Implementation Order

Recommended execution sequence:

1. **Phase 1** (Model + parameter object) — Foundation, must be first
2. **Phase 2** (DB schema + factory DB) — Must follow Phase 1
3. **Phase 5** (Default endpoints) — Independent of search mode, can be done early
4. **Phase 3** (RetrievalService) — Core search logic
5. **Phase 4** (ChatHandler) — Wires Phase 3 to the API
6. **Phase 6** (Dashboard UI) — Can start after Phase 2 schema is stable
7. **Phase 7** (Docker) — Verify images and config
8. **Phase 8** (Documentation) — Update REST_API.md, CHAT_DATA_FLOW.md, README.md
9. **Phase 9** (Testing) — After all phases complete

Phases 5 and 6 can be developed in parallel with Phases 3-4.

---

## Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Default SearchMode | `"Vector"` | Safe default for new assistants |
| Default TextWeight | `0.3` | 70% vector / 30% text — good for quality embeddings per RecallDB docs |
| Hybrid fallback | Auto-fallback to vector-only | Prevents empty results when text terms are too restrictive |
| Embedding skip for FullText | Yes | No need to call Partio for embeddings in fulltext-only mode |
| FullText normalization default | `32` | Required for meaningful hybrid blending (normalizes to 0-1 range) |
| Endpoint auto-assignment | First available from Partio | Prevents "Use server default" when endpoints are configured |
| Parameter object | `RetrievalSearchOptions` class | Keeps `RetrieveAsync` signature clean as search options grow |
| Breaking changes | Allowed | Factory DB rebuilt, docs updated, no migration path needed |

---

## Files Changed Summary

| Area | Files |
|------|-------|
| **Backend Models** | `src/AssistantHub.Core/Models/AssistantSettings.cs`, `src/AssistantHub.Core/Models/RetrievalSearchOptions.cs` (new), `src/AssistantHub.Core/Models/RetrievalChunk.cs` |
| **Database** | `src/AssistantHub.Core/Database/*/Implementations/AssistantSettingsMethods.cs` (all providers) |
| **Services** | `src/AssistantHub.Core/Services/RetrievalService.cs` |
| **Handlers** | `src/AssistantHub.Server/Handlers/ChatHandler.cs`, `src/AssistantHub.Server/Handlers/AssistantSettingsHandler.cs`, `src/AssistantHub.Server/Handlers/AssistantHandler.cs` |
| **Dashboard** | `dashboard/src/components/modals/AssistantSettingsFormModal.jsx` |
| **Docker** | `docker/compose.yaml` (verify image), `docker/factory/assistanthub.db` (rebuild) |
| **Documentation** | `REST_API.md`, `CHAT_DATA_FLOW.md`, `README.md` |
| **Auto-generated** | `src/AssistantHub.Core/AssistantHub.Core.xml` (rebuild) |
