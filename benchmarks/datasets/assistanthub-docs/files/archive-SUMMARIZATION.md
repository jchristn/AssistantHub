# AssistantHub — Partio v0.4.0 Summarization Integration Plan

**Status:** Planned
**Date:** 2026-02-19
**Last Updated:** 2026-02-19

---

## Table of Contents

1. [Overview](#1-overview)
2. [AssistantHub.Core — Models](#2-assistanthubcore--models)
3. [AssistantHub.Core — Database Layer](#3-assistanthubcore--database-layer)
4. [AssistantHub.Core — Settings](#4-assistanthubcore--settings)
5. [AssistantHub.Core — Ingestion Pipeline](#5-assistanthubcore--ingestion-pipeline)
6. [AssistantHub.Core — Retrieval Pipeline](#6-assistanthubcore--retrieval-pipeline)
7. [AssistantHub.Core — Validation](#7-assistanthubcore--validation)
8. [AssistantHub.Server — Partio Endpoint Proxy APIs](#8-assistanthubserver--partio-endpoint-proxy-apis)
9. [AssistantHub.Server — Ingestion Rule Routes](#9-assistanthubserver--ingestion-rule-routes)
10. [AssistantHub.Server — First-Run Setup](#10-assistanthubserver--first-run-setup)
11. [Dashboard — Endpoint Views (Inference & Embedding)](#11-dashboard--endpoint-views-inference--embedding)
12. [Dashboard — Ingestion Rule Form](#12-dashboard--ingestion-rule-form)
13. [Dashboard — Other UI Touchpoints](#13-dashboard--other-ui-touchpoints)
14. [Dashboard — API Client](#14-dashboard--api-client)
15. [Dashboard — Navigation & Routing](#15-dashboard--navigation--routing)
16. [Database Migrations & Factory Reset](#16-database-migrations--factory-reset)
17. [Docker Compose](#17-docker-compose)
18. [REST API Documentation](#18-rest-api-documentation)
19. [README](#19-readme)
20. [Task Checklist](#20-task-checklist)

---

## 1. Overview

### What

Partio v0.4.0 introduces **summarization** as an optional processing step before chunking and embedding, and introduces **completion endpoints** as a new endpoint type alongside embedding endpoints. AssistantHub must integrate this by:

1. Adding an `IngestionSummarizationConfig` model (parallel to `IngestionChunkingConfig` and `IngestionEmbeddingConfig`).
2. Adding a `Summarization` property to `IngestionRule` (positioned before `Chunking`).
3. Updating **both** `IngestionService` and `RetrievalService` to use the new v0.4.0 routes.
4. Updating the response models (`ChunkingResponse` / `ChunkWithEmbedding`) to match Partio v0.4.0's `SemanticCellResponse` / `ChunkResult` shape.
5. Adding **proxy APIs** in AssistantHub.Server for managing Partio embedding and completion (inference) endpoints.
6. Adding **dashboard views** for Inference Endpoints and Embedding Endpoints under the Configuration section.
7. Updating the dashboard ingestion rule form to include a collapsible "Summarization" section with all 9 config fields.
8. Adding server-side validation of `IngestionSummarizationConfig`.
9. Updating all database layers, migrations, factory reset files, documentation, and docker configuration.

### Pipeline Change (Partio-side)

**v0.1.0:** `Upload cells → Chunk → Embed`
**v0.4.0:** `Upload cells → (Summarize if configured) → Chunk → Embed`

When summarization is enabled, summaries are injected as **child cells** with `Type = "Summary"` in the response hierarchy. They are not a separate field on chunks — they appear as child `SemanticCellResponse` nodes with their own `Chunks` and `Text`.

### Partio Route Changes (Breaking)

AssistantHub must update **all** Partio API calls (in both `IngestionService` and `RetrievalService`) to the new v0.4.0 routes:

| Old Route (v0.1.0) | New Route (v0.4.0) |
|---|---|
| `POST /v1.0/endpoints/{id}/process` | `POST /v1.0/process` |
| `POST /v1.0/endpoints/{id}/process/batch` | `POST /v1.0/process/batch` |

The embedding endpoint ID (`_ChunkingSettings.EndpointId`) moves from the URL path into `EmbeddingConfiguration.EmbeddingEndpointId` in the request body.

**Note:** AssistantHub currently only uses the single `/process` route (not batch). The batch route exists in Partio but is not called from AssistantHub at this time.

### Partio Endpoint Management (New)

Partio v0.4.0 exposes CRUD APIs for both endpoint types:

| Endpoint Type | Partio Routes | Purpose |
|---|---|---|
| Embedding | `PUT/GET/DELETE /v1.0/endpoints/embedding/{id}`, `POST /v1.0/endpoints/embedding/enumerate` | Manage embedding model endpoints (e.g., Ollama all-minilm) |
| Completion | `PUT/GET/DELETE /v1.0/endpoints/completion/{id}`, `POST /v1.0/endpoints/completion/enumerate` | Manage inference/completion endpoints for summarization |

AssistantHub will proxy these APIs and expose them in the dashboard.

### Key Design Decisions

1. The `CompletionEndpointId` (the Partio-side inference endpoint used for summarization) is stored **per ingestion rule** inside the new `IngestionSummarizationConfig`. This allows different rules to use different LLMs for summarization.
2. Authentication to Partio remains **Bearer token** (`Authorization: Bearer {AccessKey}`).
3. The `Order` field in Partio's `SummarizationConfiguration` is a **traversal order** (`TopDown` = root-to-leaves, `BottomUp` = leaves-to-root) for walking the cell hierarchy during summarization. It is named `Order` in Partio's model and we will match that naming. The UI label should read "Processing Order" for clarity.

---

## 2. AssistantHub.Core — Models

### 2.1 New Model: `IngestionSummarizationConfig`

- [ ] **File:** `src/AssistantHub.Core/Models/IngestionSummarizationConfig.cs` (new)
- [ ] Follow the pattern of `IngestionChunkingConfig` and `IngestionEmbeddingConfig`
- [ ] Properties (derived from Partio's `SummarizationConfiguration`):

```
Property                  Type       Default       Notes
────────────────────────  ─────────  ────────────  ──────────────────────
CompletionEndpointId      string     (required)    ID of a Partio completion endpoint
Order                     string     "BottomUp"    "TopDown" or "BottomUp" (traversal order)
SummarizationPrompt       string     null          Custom prompt template (null = Partio default)
MaxSummaryTokens          int        1024          Min: 128
MinCellLength             int        128           Min: 0
MaxParallelTasks          int        4             Min: 1
MaxRetriesPerSummary      int        3             Min: 0; retries per cell
MaxRetries                int        9             Min: 0; global failure limit (circuit breaker)
TimeoutMs                 int        30000         Min: 100
```

- [ ] Use `string` for `Order` (not an enum) to stay consistent with how `IngestionChunkingConfig.Strategy` is a string, avoiding tight coupling to Partio enums.

### 2.2 Update Model: `IngestionRule`

- [ ] **File:** `src/AssistantHub.Core/Models/IngestionRule.cs`
- [ ] **Add property** before the `Chunking` property:

```csharp
/// <summary>
/// Summarization configuration.
/// </summary>
public IngestionSummarizationConfig Summarization { get; set; } = null;
```

- [ ] **Update `FromDataRow`**: Add deserialization of `summarization_json` column (same pattern as `chunking_json`):

```csharp
string summarizationJson = DataTableHelper.GetStringValue(row, "summarization_json");
if (!String.IsNullOrEmpty(summarizationJson))
{
    try { obj.Summarization = JsonSerializer.Deserialize<IngestionSummarizationConfig>(summarizationJson, _JsonOptions); }
    catch { }
}
```

### 2.3 Update Response Models: `ChunkingResponse` and `ChunkWithEmbedding`

- [ ] **File:** `src/AssistantHub.Core/Services/IngestionService.cs` (private inner classes at bottom, ~line 784)

Partio v0.4.0 returns a `SemanticCellResponse` with a hierarchical structure. The existing flat `ChunkingResponse { List<ChunkWithEmbedding> Chunks }` must be updated.

**Old models (v0.1.0):**
```csharp
private class ChunkingResponse
{
    public List<ChunkWithEmbedding> Chunks { get; set; } = null;
}

private class ChunkWithEmbedding
{
    public string Text { get; set; } = null;
    public List<float> Embeddings { get; set; } = null;
}
```

**New models (v0.4.0):**
```csharp
private class SemanticCellResponse
{
    public Guid GUID { get; set; }
    public Guid? ParentGUID { get; set; }
    public string Type { get; set; } = null;
    public string Text { get; set; } = null;
    public List<ChunkResult> Chunks { get; set; } = null;
    public List<SemanticCellResponse> Children { get; set; } = null;
}

private class ChunkResult
{
    public Guid CellGUID { get; set; }
    public string Text { get; set; } = null;
    public List<string> Labels { get; set; } = null;
    public Dictionary<string, string> Tags { get; set; } = null;
    public List<float> Embeddings { get; set; } = null;
}
```

- [ ] **Update chunk extraction logic** — The response is now hierarchical. After deserialization, flatten all `Chunks` from the root cell and all `Children` (recursively) into a single `List<ChunkResult>`:

```csharp
private List<ChunkResult> FlattenChunks(SemanticCellResponse cell)
{
    List<ChunkResult> all = new List<ChunkResult>();
    if (cell.Chunks != null)
        all.AddRange(cell.Chunks);
    if (cell.Children != null)
    {
        foreach (SemanticCellResponse child in cell.Children)
            all.AddRange(FlattenChunks(child));
    }
    return all;
}
```

- [ ] **Update `StoreEmbeddingAsync`** — Change parameter type from `ChunkWithEmbedding` to `ChunkResult`. The property names (`Text`, `Embeddings`) are the same so the body is unchanged.

### 2.4 Update Response Models in `RetrievalService`

- [ ] **File:** `src/AssistantHub.Core/Services/RetrievalService.cs` (private inner classes at bottom, ~line 224)

**Old models:**
```csharp
private class ProcessResponse
{
    public List<ProcessChunk> Chunks { get; set; } = null;
}

private class ProcessChunk
{
    public string Text { get; set; } = null;
    public List<double> Embeddings { get; set; } = null;
}
```

**New models:**
```csharp
private class ProcessResponse
{
    public Guid GUID { get; set; }
    public string Type { get; set; } = null;
    public string Text { get; set; } = null;
    public List<ProcessChunk> Chunks { get; set; } = null;
    public List<ProcessResponse> Children { get; set; } = null;
}

private class ProcessChunk
{
    public Guid CellGUID { get; set; }
    public string Text { get; set; } = null;
    public List<double> Embeddings { get; set; } = null;
}
```

- [ ] The existing logic takes `Chunks[0].Embeddings` from the response. Since the query text is a simple string with no children, the first chunk on the root cell will still be the correct embedding. **No logic change needed** beyond updating the model shape.

---

## 3. AssistantHub.Core — Database Layer

### 3.1 Update: All Four Table Creation Queries

Add the `summarization_json` column to the `ingestion_rules` table in all four database providers:

- [ ] **SQLite:** `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs` — Add `summarization_json TEXT` column after `atomization_json`
- [ ] **PostgreSQL:** `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs` — Same
- [ ] **MySQL:** `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs` — Same
- [ ] **SQL Server:** `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs` — Same (as `NVARCHAR(MAX) NULL`)

### 3.2 Update: All Four IngestionRuleMethods Implementations

Update `CreateAsync` and `UpdateAsync`. (`ReadAsync`, `EnumerateAsync`, `ExistsAsync` require no changes — they use `SELECT *` and `FromDataRow` handles the new column.)

- [ ] **SQLite:** `src/AssistantHub.Core/Database/Sqlite/Implementations/IngestionRuleMethods.cs`
  - `CreateAsync` INSERT: add `summarization_json` to the column list and values
  - `UpdateAsync` UPDATE SET: add `summarization_json = ...`
- [ ] **PostgreSQL:** `src/AssistantHub.Core/Database/Postgresql/Implementations/IngestionRuleMethods.cs` — Same changes
- [ ] **MySQL:** `src/AssistantHub.Core/Database/Mysql/Implementations/IngestionRuleMethods.cs` — Same changes
- [ ] **SQL Server:** `src/AssistantHub.Core/Database/SqlServer/Implementations/IngestionRuleMethods.cs` — Same changes

Each uses the same pattern as `chunking_json`:
```csharp
_Driver.FormatNullableString(Serializer.SerializeJson(rule.Summarization))
```

---

## 4. AssistantHub.Core — Settings

### 4.1 Update: `ChunkingSettings`

- [ ] **File:** `src/AssistantHub.Core/Settings/ChunkingSettings.cs`
- [ ] The `EndpointId` property still exists and is used, but it will now be passed in the request body as `EmbeddingConfiguration.EmbeddingEndpointId` instead of the URL path. **No new properties needed** — just note the usage change in `IngestionService` and `RetrievalService`.

---

## 5. AssistantHub.Core — Ingestion Pipeline

### 5.1 Update: `IngestionService.ChunkAndEmbedContentAsync`

- [ ] **File:** `src/AssistantHub.Core/Services/IngestionService.cs`

**Route change** — Update the Partio URL from the old v0.1.0 route to v0.4.0:

```csharp
// OLD:
string url = _ChunkingSettings.Endpoint.TrimEnd('/') + "/v1.0/endpoints/" + _ChunkingSettings.EndpointId + "/process";

// NEW:
string url = _ChunkingSettings.Endpoint.TrimEnd('/') + "/v1.0/process";
```

**EmbeddingEndpointId** — Always include `EmbeddingConfiguration` with the endpoint ID in the request body:

```csharp
Dictionary<string, object> embedConfig = new Dictionary<string, object>();
embedConfig["EmbeddingEndpointId"] = _ChunkingSettings.EndpointId;

if (rule?.Embedding != null)
{
    if (!String.IsNullOrEmpty(rule.Embedding.EmbeddingEndpointId))
        embedConfig["EmbeddingEndpointId"] = rule.Embedding.EmbeddingEndpointId;
    embedConfig["L2Normalization"] = rule.Embedding.L2Normalization;
}

requestBody["EmbeddingConfiguration"] = embedConfig;
```

**Summarization** — If the ingestion rule has a `Summarization` config, include `SummarizationConfiguration` in the request body:

```csharp
if (rule?.Summarization != null)
{
    Dictionary<string, object> sumConfig = new Dictionary<string, object>();
    sumConfig["CompletionEndpointId"] = rule.Summarization.CompletionEndpointId;
    sumConfig["Order"] = rule.Summarization.Order ?? "BottomUp";
    sumConfig["MaxSummaryTokens"] = rule.Summarization.MaxSummaryTokens;
    sumConfig["MinCellLength"] = rule.Summarization.MinCellLength;
    sumConfig["MaxParallelTasks"] = rule.Summarization.MaxParallelTasks;
    sumConfig["MaxRetriesPerSummary"] = rule.Summarization.MaxRetriesPerSummary;
    sumConfig["MaxRetries"] = rule.Summarization.MaxRetries;
    sumConfig["TimeoutMs"] = rule.Summarization.TimeoutMs;
    if (!String.IsNullOrEmpty(rule.Summarization.SummarizationPrompt))
        sumConfig["SummarizationPrompt"] = rule.Summarization.SummarizationPrompt;
    requestBody["SummarizationConfiguration"] = sumConfig;
}
```

**Response deserialization** — Update to use the new `SemanticCellResponse` model and `FlattenChunks`:

```csharp
// OLD:
ChunkingResponse chunkResult = JsonSerializer.Deserialize<ChunkingResponse>(responseBody, _JsonOptions);
return chunkResult?.Chunks;

// NEW:
SemanticCellResponse cellResult = JsonSerializer.Deserialize<SemanticCellResponse>(responseBody, _JsonOptions);
if (cellResult == null) return null;
return FlattenChunks(cellResult);
```

### 5.2 Update: Processing Log Messages

- [ ] **File:** `src/AssistantHub.Core/Services/IngestionService.cs`
- [ ] Update the step logging to mention summarization when it's enabled:

```csharp
if (rule?.Summarization != null)
{
    string sumParams = "order: " + (rule.Summarization.Order ?? "BottomUp")
        + ", completionEndpoint: " + rule.Summarization.CompletionEndpointId
        + ", maxTokens: " + rule.Summarization.MaxSummaryTokens;
    await _ProcessingLog.LogAsync(documentId, "INFO", "Summarization enabled — " + sumParams);
}
```

- [ ] Update the step 8 status message from "Chunking and embedding content." to "Processing content." (since it now potentially includes summarization).

---

## 6. AssistantHub.Core — Retrieval Pipeline

### 6.1 Update: `RetrievalService.EmbedQueryAsync`

- [ ] **File:** `src/AssistantHub.Core/Services/RetrievalService.cs`

**Route change** — Same URL update as `IngestionService`:

```csharp
// OLD (~line 139):
string url = _ChunkingSettings.Endpoint.TrimEnd('/') + "/v1.0/endpoints/"
    + _ChunkingSettings.EndpointId + "/process";

// NEW:
string url = _ChunkingSettings.Endpoint.TrimEnd('/') + "/v1.0/process";
```

**EmbeddingEndpointId** — Add `EmbeddingEndpointId` to the request body. The current request sends a minimal `{ Type, Text }` object. Update to include the endpoint ID:

```csharp
// OLD:
object requestBody = new { Type = "Text", Text = query };

// NEW:
object requestBody = new
{
    Type = "Text",
    Text = query,
    EmbeddingConfiguration = new { EmbeddingEndpointId = _ChunkingSettings.EndpointId }
};
```

- [ ] The response parsing (`processResult.Chunks[0].Embeddings`) remains correct — for a simple text query with no children, the first chunk on the root cell contains the embedding.

---

## 7. AssistantHub.Core — Validation

### 7.1 New: `IngestionSummarizationConfig` Validation

- [ ] Add a validation method (either on the model itself or in the handler) that runs before persisting an ingestion rule. Fail early with a clear error message.

```csharp
public static List<string> Validate(IngestionSummarizationConfig config)
{
    List<string> errors = new List<string>();
    if (config == null) return errors;

    if (String.IsNullOrWhiteSpace(config.CompletionEndpointId))
        errors.Add("CompletionEndpointId is required when summarization is enabled.");

    if (!String.IsNullOrEmpty(config.Order)
        && config.Order != "TopDown" && config.Order != "BottomUp")
        errors.Add("Order must be 'TopDown' or 'BottomUp'.");

    if (config.MaxSummaryTokens < 128)
        errors.Add("MaxSummaryTokens must be >= 128.");

    if (config.MinCellLength < 0)
        errors.Add("MinCellLength must be >= 0.");

    if (config.MaxParallelTasks < 1)
        errors.Add("MaxParallelTasks must be >= 1.");

    if (config.MaxRetriesPerSummary < 0)
        errors.Add("MaxRetriesPerSummary must be >= 0.");

    if (config.MaxRetries < 0)
        errors.Add("MaxRetries must be >= 0.");

    if (config.TimeoutMs < 100)
        errors.Add("TimeoutMs must be >= 100.");

    // Validate prompt template placeholders if custom prompt is provided
    if (!String.IsNullOrEmpty(config.SummarizationPrompt))
    {
        // Warn if none of the supported placeholders are present
        if (!config.SummarizationPrompt.Contains("{content}"))
            errors.Add("SummarizationPrompt should contain the {content} placeholder.");
    }

    return errors;
}
```

- [ ] Call this validation in the `IngestionRuleHandler` before `CreateAsync` / `UpdateAsync`. Return 400 with the error list if validation fails.

---

## 8. AssistantHub.Server — Partio Endpoint Proxy APIs

### 8.1 New Handler: `EmbeddingEndpointHandler`

- [ ] **File:** `src/AssistantHub.Server/Handlers/EmbeddingEndpointHandler.cs` (new)
- [ ] Follow the `CollectionHandler` proxy pattern: extend `HandlerBase`, use a static `HttpClient`, proxy requests to Partio.
- [ ] Target Partio base URL: `Settings.Chunking.Endpoint` (same as the existing Partio connection).
- [ ] Auth to Partio: `Authorization: Bearer {Settings.Chunking.AccessKey}`.
- [ ] All methods require admin access (`IsAdmin(ctx)` guard).

**Routes to proxy:**

| AssistantHub Route | HTTP Method | Proxies To (Partio) | Handler Method |
|---|---|---|---|
| `PUT /v1.0/endpoints/embedding` | PUT | `PUT /v1.0/endpoints/embedding` | `CreateEmbeddingEndpointAsync` |
| `POST /v1.0/endpoints/embedding/enumerate` | POST | `POST /v1.0/endpoints/embedding/enumerate` | `EnumerateEmbeddingEndpointsAsync` |
| `GET /v1.0/endpoints/embedding/{endpointId}` | GET | `GET /v1.0/endpoints/embedding/{endpointId}` | `GetEmbeddingEndpointAsync` |
| `PUT /v1.0/endpoints/embedding/{endpointId}` | PUT | `PUT /v1.0/endpoints/embedding/{endpointId}` | `UpdateEmbeddingEndpointAsync` |
| `DELETE /v1.0/endpoints/embedding/{endpointId}` | DELETE | `DELETE /v1.0/endpoints/embedding/{endpointId}` | `DeleteEmbeddingEndpointAsync` |
| `HEAD /v1.0/endpoints/embedding/{endpointId}` | HEAD | `HEAD /v1.0/endpoints/embedding/{endpointId}` | `HeadEmbeddingEndpointAsync` |
| `GET /v1.0/endpoints/embedding/{endpointId}/health` | GET | `GET /v1.0/endpoints/embedding/{endpointId}/health` | `GetEmbeddingEndpointHealthAsync` |

Each handler method follows the same pattern:

```csharp
public async Task CreateEmbeddingEndpointAsync(HttpContextBase ctx)
{
    if (ctx == null) throw new ArgumentNullException(nameof(ctx));
    try
    {
        if (!IsAdmin(ctx))
        {
            ctx.Response.StatusCode = 403;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(Serializer.SerializeJson(
                new ApiErrorResponse(Enums.ApiErrorEnum.AuthorizationFailed)));
            return;
        }

        string body = ctx.Request.DataAsString;
        string partioUrl = Settings.Chunking.Endpoint.TrimEnd('/')
            + "/v1.0/endpoints/embedding";

        HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Put, partioUrl);
        req.Headers.Add("Authorization", "Bearer " + Settings.Chunking.AccessKey);
        if (!String.IsNullOrEmpty(body))
            req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        HttpResponseMessage resp = await _HttpClient.SendAsync(req);
        string respBody = await resp.Content.ReadAsStringAsync();

        ctx.Response.StatusCode = (int)resp.StatusCode;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.Send(respBody);
    }
    catch (Exception e)
    {
        Logging.Warn(_Header + "exception in CreateEmbeddingEndpointAsync: " + e.Message);
        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.Send(Serializer.SerializeJson(
            new ApiErrorResponse(Enums.ApiErrorEnum.InternalError)));
    }
}
```

### 8.2 New Handler: `CompletionEndpointHandler`

- [ ] **File:** `src/AssistantHub.Server/Handlers/CompletionEndpointHandler.cs` (new)
- [ ] Identical pattern to `EmbeddingEndpointHandler`, but proxies to `/v1.0/endpoints/completion` routes.

**Routes to proxy:**

| AssistantHub Route | HTTP Method | Proxies To (Partio) | Handler Method |
|---|---|---|---|
| `PUT /v1.0/endpoints/completion` | PUT | `PUT /v1.0/endpoints/completion` | `CreateCompletionEndpointAsync` |
| `POST /v1.0/endpoints/completion/enumerate` | POST | `POST /v1.0/endpoints/completion/enumerate` | `EnumerateCompletionEndpointsAsync` |
| `GET /v1.0/endpoints/completion/{endpointId}` | GET | `GET /v1.0/endpoints/completion/{endpointId}` | `GetCompletionEndpointAsync` |
| `PUT /v1.0/endpoints/completion/{endpointId}` | PUT | `PUT /v1.0/endpoints/completion/{endpointId}` | `UpdateCompletionEndpointAsync` |
| `DELETE /v1.0/endpoints/completion/{endpointId}` | DELETE | `DELETE /v1.0/endpoints/completion/{endpointId}` | `DeleteCompletionEndpointAsync` |
| `HEAD /v1.0/endpoints/completion/{endpointId}` | HEAD | `HEAD /v1.0/endpoints/completion/{endpointId}` | `HeadCompletionEndpointAsync` |
| `GET /v1.0/endpoints/completion/{endpointId}/health` | GET | `GET /v1.0/endpoints/completion/{endpointId}/health` | `GetCompletionEndpointHealthAsync` |

### 8.3 Register Routes in `AssistantHubServer.cs`

- [ ] **File:** `src/AssistantHub.Server/AssistantHubServer.cs` — `InitializeWebserver()` method

```csharp
// Instantiate handlers
EmbeddingEndpointHandler embeddingEndpointHandler = new EmbeddingEndpointHandler(
    _Database, _Logging, _Settings, _Authentication,
    _Storage, _Ingestion, _Retrieval, _Inference);
CompletionEndpointHandler completionEndpointHandler = new CompletionEndpointHandler(
    _Database, _Logging, _Settings, _Authentication,
    _Storage, _Ingestion, _Retrieval, _Inference);

// Embedding endpoint routes (static for create/enumerate, parameter for CRUD by ID)
_Server.Routes.PostAuthentication.Static.Add(
    WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/endpoints/embedding",
    embeddingEndpointHandler.CreateEmbeddingEndpointAsync);
_Server.Routes.PostAuthentication.Static.Add(
    WatsonWebserver.Core.HttpMethod.POST, "/v1.0/endpoints/embedding/enumerate",
    embeddingEndpointHandler.EnumerateEmbeddingEndpointsAsync);

// NOTE: Health route with /health suffix MUST be registered BEFORE the /{endpointId} parameter route
_Server.Routes.PostAuthentication.Parameter.Add(
    WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/embedding/{endpointId}/health",
    embeddingEndpointHandler.GetEmbeddingEndpointHealthAsync);
_Server.Routes.PostAuthentication.Parameter.Add(
    WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/embedding/{endpointId}",
    embeddingEndpointHandler.GetEmbeddingEndpointAsync);
_Server.Routes.PostAuthentication.Parameter.Add(
    WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/endpoints/embedding/{endpointId}",
    embeddingEndpointHandler.UpdateEmbeddingEndpointAsync);
_Server.Routes.PostAuthentication.Parameter.Add(
    WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/endpoints/embedding/{endpointId}",
    embeddingEndpointHandler.DeleteEmbeddingEndpointAsync);
_Server.Routes.PostAuthentication.Parameter.Add(
    WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/endpoints/embedding/{endpointId}",
    embeddingEndpointHandler.HeadEmbeddingEndpointAsync);

// Completion endpoint routes (same pattern)
_Server.Routes.PostAuthentication.Static.Add(
    WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/endpoints/completion",
    completionEndpointHandler.CreateCompletionEndpointAsync);
_Server.Routes.PostAuthentication.Static.Add(
    WatsonWebserver.Core.HttpMethod.POST, "/v1.0/endpoints/completion/enumerate",
    completionEndpointHandler.EnumerateCompletionEndpointsAsync);

_Server.Routes.PostAuthentication.Parameter.Add(
    WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/completion/{endpointId}/health",
    completionEndpointHandler.GetCompletionEndpointHealthAsync);
_Server.Routes.PostAuthentication.Parameter.Add(
    WatsonWebserver.Core.HttpMethod.GET, "/v1.0/endpoints/completion/{endpointId}",
    completionEndpointHandler.GetCompletionEndpointAsync);
_Server.Routes.PostAuthentication.Parameter.Add(
    WatsonWebserver.Core.HttpMethod.PUT, "/v1.0/endpoints/completion/{endpointId}",
    completionEndpointHandler.UpdateCompletionEndpointAsync);
_Server.Routes.PostAuthentication.Parameter.Add(
    WatsonWebserver.Core.HttpMethod.DELETE, "/v1.0/endpoints/completion/{endpointId}",
    completionEndpointHandler.DeleteCompletionEndpointAsync);
_Server.Routes.PostAuthentication.Parameter.Add(
    WatsonWebserver.Core.HttpMethod.HEAD, "/v1.0/endpoints/completion/{endpointId}",
    completionEndpointHandler.HeadCompletionEndpointAsync);
```

### 8.4 Partio Endpoint Models Reference

For reference, the Partio endpoint models have these fields:

**EmbeddingEndpoint:**
```
Property                        Type                          Default
──────────────────────────────  ────────────────────────────  ─────────
Id                              string                        (assigned by Partio)
TenantId                        string                        (assigned by Partio)
Model                           string                        (required)
Endpoint                        string                        (required)
ApiFormat                       string                        (required, e.g. "Ollama")
ApiKey                          string                        null
Active                          bool                          true
EnableRequestHistory            bool                          true
HealthCheckEnabled              bool                          false
HealthCheckUrl                  string                        null
HealthCheckMethod               string                        "GET"
HealthCheckIntervalMs           int                           5000
HealthCheckTimeoutMs            int                           2000
HealthCheckExpectedStatusCode   int                           200
HealthyThreshold                int                           3
UnhealthyThreshold              int                           3
HealthCheckUseAuth              bool                          false
Labels                          List<string>                  null
Tags                            Dictionary<string, string>    null
CreatedUtc                      DateTime                      (auto)
LastUpdateUtc                   DateTime                      (auto)
```

**CompletionEndpoint** — Same fields as EmbeddingEndpoint, plus:
```
Name                            string                        null (human-readable name)
```

And different health check defaults: `HealthCheckEnabled = true`, `HealthCheckIntervalMs = 30000`, `HealthCheckTimeoutMs = 5000`.

---

## 9. AssistantHub.Server — Ingestion Rule Routes

### 9.1 Add Validation to Ingestion Rule Handler

- [ ] **File:** `src/AssistantHub.Server/Handlers/IngestionRuleHandler.cs`
- [ ] In `CreateAsync` and `UpdateAsync`, after deserializing the `IngestionRule` from the request body, validate the `Summarization` config if present:

```csharp
if (rule.Summarization != null)
{
    List<string> errors = IngestionSummarizationConfig.Validate(rule.Summarization);
    if (errors.Count > 0)
    {
        ctx.Response.StatusCode = 400;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.Send(Serializer.SerializeJson(
            new ApiErrorResponse(Enums.ApiErrorEnum.BadRequest, String.Join("; ", errors))));
        return;
    }
}
```

### 9.2 No Other Route Changes Needed

The ingestion rule CRUD routes (`/v1.0/ingestion-rules`) remain unchanged. The `IngestionRuleHandler` deserializes the full `IngestionRule` object from JSON, which will automatically pick up the new `Summarization` property. The handler delegates to `Database.IngestionRule.CreateAsync` / `UpdateAsync` which serialize all properties.

---

## 10. AssistantHub.Server — First-Run Setup

### 10.1 Default Ingestion Rule

- [ ] **File:** `src/AssistantHub.Server/AssistantHubServer.cs`
- [ ] The default ingestion rule created on first run does not need a Summarization config (it should remain `null` / disabled by default). **No changes needed.**

---

## 11. Dashboard — Endpoint Views (Inference & Embedding)

### 11.1 New View: `EmbeddingEndpointsView.jsx`

- [ ] **File:** `dashboard/src/views/EmbeddingEndpointsView.jsx` (new)
- [ ] Follow the `IngestionRulesView.jsx` pattern exactly: DataTable, CRUD modals, bulk delete.

**DataTable columns:**

```javascript
const columns = [
  {
    key: 'Id',
    label: 'ID',
    tooltip: 'Unique endpoint identifier',
    filterable: true,
    render: (row) => <CopyableId id={row.Id} />
  },
  {
    key: 'Model',
    label: 'Model',
    tooltip: 'Model name (e.g. all-minilm)',
    filterable: true
  },
  {
    key: 'Endpoint',
    label: 'Endpoint',
    tooltip: 'Target API URL',
    filterable: true
  },
  {
    key: 'ApiFormat',
    label: 'Format',
    tooltip: 'API format (e.g. Ollama)',
    filterable: true
  },
  {
    key: 'Active',
    label: 'Active',
    tooltip: 'Whether the endpoint is active',
    render: (row) => row.Active ? 'Yes' : 'No'
  },
  {
    key: 'CreatedUtc',
    label: 'Created',
    tooltip: 'When the endpoint was created',
    render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : ''
  }
];
```

**Data fetching** — Uses `api.enumerateEmbeddingEndpoints(params)` (see Section 14).

**Row actions:** Edit, View JSON, Delete.

### 11.2 New Modal: `EmbeddingEndpointFormModal.jsx`

- [ ] **File:** `dashboard/src/components/modals/EmbeddingEndpointFormModal.jsx` (new)
- [ ] Form fields:
  - **Model** (text, required) — e.g. `all-minilm`
  - **Endpoint** (text, required) — e.g. `http://ollama:11434`
  - **API Format** (select: `Ollama`, or other supported formats) — required
  - **API Key** (text, optional) — masked/password type
  - **Active** (toggle, default true)
- [ ] Advanced/collapsible section for health check settings:
  - **Health Check Enabled** (toggle, default false)
  - **Health Check URL** (text)
  - **Health Check Method** (select: GET, POST, HEAD)
  - **Health Check Interval (ms)** (number, default 5000, min 1000)
  - **Health Check Timeout (ms)** (number, default 2000, min 100)
  - **Expected Status Code** (number, default 200)
  - **Healthy Threshold** (number, default 3, min 1)
  - **Unhealthy Threshold** (number, default 3, min 1)

### 11.3 New View: `InferenceEndpointsView.jsx`

- [ ] **File:** `dashboard/src/views/InferenceEndpointsView.jsx` (new)
- [ ] Same pattern as `EmbeddingEndpointsView.jsx`, but for completion endpoints.

**DataTable columns:**

```javascript
const columns = [
  {
    key: 'Id',
    label: 'ID',
    tooltip: 'Unique endpoint identifier',
    filterable: true,
    render: (row) => <CopyableId id={row.Id} />
  },
  {
    key: 'Name',
    label: 'Name',
    tooltip: 'Human-readable endpoint name',
    filterable: true
  },
  {
    key: 'Model',
    label: 'Model',
    tooltip: 'Model name',
    filterable: true
  },
  {
    key: 'Endpoint',
    label: 'Endpoint',
    tooltip: 'Target API URL',
    filterable: true
  },
  {
    key: 'ApiFormat',
    label: 'Format',
    tooltip: 'API format (e.g. Ollama)',
    filterable: true
  },
  {
    key: 'Active',
    label: 'Active',
    tooltip: 'Whether the endpoint is active',
    render: (row) => row.Active ? 'Yes' : 'No'
  },
  {
    key: 'CreatedUtc',
    label: 'Created',
    tooltip: 'When the endpoint was created',
    render: (row) => row.CreatedUtc ? new Date(row.CreatedUtc).toLocaleString() : ''
  }
];
```

### 11.4 New Modal: `InferenceEndpointFormModal.jsx`

- [ ] **File:** `dashboard/src/components/modals/InferenceEndpointFormModal.jsx` (new)
- [ ] Same fields as `EmbeddingEndpointFormModal.jsx`, plus:
  - **Name** (text, optional) — human-readable name
- [ ] Health check defaults differ: `HealthCheckEnabled = true`, `HealthCheckIntervalMs = 30000`, `HealthCheckTimeoutMs = 5000`.

---

## 12. Dashboard — Ingestion Rule Form

### 12.1 Update: `IngestionRuleFormModal.jsx`

- [ ] **File:** `dashboard/src/components/modals/IngestionRuleFormModal.jsx`

**Add default summarization config:**

```javascript
const ORDER_OPTIONS = ['BottomUp', 'TopDown'];

const defaultSummarization = {
  CompletionEndpointId: '',
  Order: 'BottomUp',
  SummarizationPrompt: '',
  MaxSummaryTokens: 1024,
  MinCellLength: 128,
  MaxParallelTasks: 4,
  MaxRetriesPerSummary: 3,
  MaxRetries: 9,
  TimeoutMs: 30000
};
```

**Add form state:**

```javascript
// In the useState initializer, add:
SummarizationEnabled: !!rule?.Summarization,
Summarization: rule?.Summarization
  ? { ...defaultSummarization, ...rule.Summarization }
  : { ...defaultSummarization }
```

**Add state variables:**

```javascript
const [summarizationOpen, setSummarizationOpen] = useState(false);
```

**Add change handler:**

```javascript
const handleSummarizationChange = (field, value) => {
  setForm(prev => ({
    ...prev,
    Summarization: { ...prev.Summarization, [field]: value }
  }));
};
```

**Add Summarization section in JSX** — Insert before the Chunking collapsible section. Follow the same collapsible pattern as Chunking and Embedding. All 9 config fields must be rendered:

```jsx
{/* Summarization (collapsible) */}
<div className="form-group">
  <div className="form-toggle" style={{ marginBottom: '0.5rem' }}>
    <label className="toggle-switch">
      <input
        type="checkbox"
        checked={form.SummarizationEnabled}
        onChange={(e) => handleChange('SummarizationEnabled', e.target.checked)}
      />
      <span className="toggle-slider"></span>
    </label>
    <button
      type="button"
      style={collapsibleButtonStyle}
      onClick={() => setSummarizationOpen(prev => !prev)}
    >
      {summarizationOpen ? '▾' : '▸'} Summarization
    </button>
  </div>
  {form.SummarizationEnabled && summarizationOpen && (
    <div style={{ marginTop: '0.5rem' }}>
      {/* CompletionEndpointId */}
      <div className="form-group">
        <label>Completion Endpoint ID</label>
        <input
          type="text"
          value={form.Summarization.CompletionEndpointId}
          onChange={(e) => handleSummarizationChange('CompletionEndpointId', e.target.value)}
          placeholder="Partio completion endpoint ID (e.g. cep_...)"
          required
        />
      </div>

      {/* Order */}
      <div className="form-group">
        <label>Processing Order</label>
        <select
          value={form.Summarization.Order}
          onChange={(e) => handleSummarizationChange('Order', e.target.value)}
        >
          {ORDER_OPTIONS.map(opt => (
            <option key={opt} value={opt}>{opt}</option>
          ))}
        </select>
        <small style={{ color: '#888', display: 'block', marginTop: '0.25rem' }}>
          BottomUp: summarize leaves first (children before parents).
          TopDown: summarize root first (parents before children).
        </small>
      </div>

      {/* MaxSummaryTokens */}
      <div className="form-group">
        <label>Max Summary Tokens</label>
        <input
          type="number"
          value={form.Summarization.MaxSummaryTokens}
          onChange={(e) => handleSummarizationChange('MaxSummaryTokens', e.target.value)}
          min="128"
        />
      </div>

      {/* MinCellLength */}
      <div className="form-group">
        <label>Min Cell Length</label>
        <input
          type="number"
          value={form.Summarization.MinCellLength}
          onChange={(e) => handleSummarizationChange('MinCellLength', e.target.value)}
          min="0"
        />
        <small style={{ color: '#888', display: 'block', marginTop: '0.25rem' }}>
          Cells with content shorter than this (in characters) are skipped.
        </small>
      </div>

      {/* MaxParallelTasks */}
      <div className="form-group">
        <label>Max Parallel Tasks</label>
        <input
          type="number"
          value={form.Summarization.MaxParallelTasks}
          onChange={(e) => handleSummarizationChange('MaxParallelTasks', e.target.value)}
          min="1"
        />
      </div>

      {/* MaxRetriesPerSummary */}
      <div className="form-group">
        <label>Max Retries Per Summary</label>
        <input
          type="number"
          value={form.Summarization.MaxRetriesPerSummary}
          onChange={(e) => handleSummarizationChange('MaxRetriesPerSummary', e.target.value)}
          min="0"
        />
        <small style={{ color: '#888', display: 'block', marginTop: '0.25rem' }}>
          Max retry attempts for an individual cell before giving up on that cell.
        </small>
      </div>

      {/* MaxRetries */}
      <div className="form-group">
        <label>Max Retries (Global)</label>
        <input
          type="number"
          value={form.Summarization.MaxRetries}
          onChange={(e) => handleSummarizationChange('MaxRetries', e.target.value)}
          min="0"
        />
        <small style={{ color: '#888', display: 'block', marginTop: '0.25rem' }}>
          Global failure limit across all cells. When reached, the entire request fails (circuit breaker).
        </small>
      </div>

      {/* TimeoutMs */}
      <div className="form-group">
        <label>Timeout (ms)</label>
        <input
          type="number"
          value={form.Summarization.TimeoutMs}
          onChange={(e) => handleSummarizationChange('TimeoutMs', e.target.value)}
          min="100"
        />
      </div>

      {/* SummarizationPrompt */}
      <div className="form-group">
        <label>Custom Prompt</label>
        <textarea
          value={form.Summarization.SummarizationPrompt}
          onChange={(e) => handleSummarizationChange('SummarizationPrompt', e.target.value)}
          rows={4}
          placeholder="Optional. Uses Partio default prompt if empty."
        />
        <small style={{ color: '#888', display: 'block', marginTop: '0.25rem' }}>
          Supported placeholders: <code>{'{content}'}</code> (cell text),
          <code>{'{tokens}'}</code> (max summary tokens),
          <code>{'{context}'}</code> (context from adjacent cells, or "(none)").
          Default: "Summarize the following content in at most {'{tokens}'} tokens..."
        </small>
      </div>
    </div>
  )}
</div>
```

**Update `handleSubmit`** — Include `Summarization` in the submitted data only if enabled, with all 9 fields:

```javascript
Summarization: form.SummarizationEnabled ? {
  CompletionEndpointId: form.Summarization.CompletionEndpointId,
  Order: form.Summarization.Order || 'BottomUp',
  MaxSummaryTokens: parseInt(form.Summarization.MaxSummaryTokens) || 1024,
  MinCellLength: parseInt(form.Summarization.MinCellLength) || 128,
  MaxParallelTasks: parseInt(form.Summarization.MaxParallelTasks) || 4,
  MaxRetriesPerSummary: parseInt(form.Summarization.MaxRetriesPerSummary) || 3,
  MaxRetries: parseInt(form.Summarization.MaxRetries) || 9,
  TimeoutMs: parseInt(form.Summarization.TimeoutMs) || 30000,
  SummarizationPrompt: form.Summarization.SummarizationPrompt || undefined
} : undefined
```

---

## 13. Dashboard — Other UI Touchpoints

### 13.1 Update: `IngestionRulesView.jsx`

- [ ] **File:** `dashboard/src/views/IngestionRulesView.jsx`
- [ ] Add a "Summarization" column to the DataTable showing whether summarization is enabled:

```javascript
{
  key: 'Summarization',
  label: 'Summarization',
  tooltip: 'Whether summarization is enabled for this rule',
  render: (row) => row.Summarization ? 'Enabled' : '—'
}
```

### 13.2 No Other Existing View Changes

The `DocumentsView`, `DocumentUploadModal`, `DropRuleModal`, `useUploadQueue` components reference ingestion rules by ID but do not interact with the rule's config details. They require no changes.

---

## 14. Dashboard — API Client

### 14.1 Add Endpoint API Methods

- [ ] **File:** `dashboard/src/utils/api.js`

Add methods for embedding endpoints:

```javascript
// Embedding Endpoints
createEmbeddingEndpoint(endpoint) {
  return this.request('PUT', '/v1.0/endpoints/embedding', endpoint);
}
enumerateEmbeddingEndpoints(params) {
  return this.request('POST', '/v1.0/endpoints/embedding/enumerate', params || {});
}
getEmbeddingEndpoint(id) {
  return this.request('GET', `/v1.0/endpoints/embedding/${id}`);
}
updateEmbeddingEndpoint(id, endpoint) {
  return this.request('PUT', `/v1.0/endpoints/embedding/${id}`, endpoint);
}
deleteEmbeddingEndpoint(id) {
  return this.request('DELETE', `/v1.0/endpoints/embedding/${id}`);
}
```

Add methods for completion (inference) endpoints:

```javascript
// Completion (Inference) Endpoints
createCompletionEndpoint(endpoint) {
  return this.request('PUT', '/v1.0/endpoints/completion', endpoint);
}
enumerateCompletionEndpoints(params) {
  return this.request('POST', '/v1.0/endpoints/completion/enumerate', params || {});
}
getCompletionEndpoint(id) {
  return this.request('GET', `/v1.0/endpoints/completion/${id}`);
}
updateCompletionEndpoint(id, endpoint) {
  return this.request('PUT', `/v1.0/endpoints/completion/${id}`, endpoint);
}
deleteCompletionEndpoint(id) {
  return this.request('DELETE', `/v1.0/endpoints/completion/${id}`);
}
```

**Note:** The enumerate endpoints use POST (not GET) because Partio's enumerate routes accept a request body with pagination parameters.

### 14.2 DataTable fetchData Adapter

The `DataTable` component calls `fetchData({ maxResults, continuationToken, ordering })` which expects GET-style pagination. The Partio enumerate routes use POST with a body. The view components will need to wrap the API call:

```javascript
const fetchData = useCallback(async (params) => {
  const body = {};
  if (params.maxResults) body.MaxResults = params.maxResults;
  if (params.continuationToken) body.ContinuationToken = params.continuationToken;
  if (params.ordering) body.Ordering = params.ordering;
  return await api.enumerateEmbeddingEndpoints(body);
}, [serverUrl, credential]);
```

---

## 15. Dashboard — Navigation & Routing

### 15.1 Update: `Sidebar.jsx`

- [ ] **File:** `dashboard/src/components/Sidebar.jsx`
- [ ] Add "Endpoints" as a top-level item under the "Configuration" section, with "Embedding" and "Inference" as sub-items:

```javascript
// In the 'Configuration' section's items array, add BEFORE 'Ingestion Rules':
{
  path: '/endpoints/embedding',
  label: 'Endpoints',
  icon: <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"
    fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <circle cx="12" cy="12" r="3"/><path d="M12 2v4m0 12v4M2 12h4m12 0h4"/>
  </svg>
},
{
  path: '/endpoints/embedding',
  label: 'Embedding',
  sub: true,
  icon: <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"
    fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="4 17 10 11 4 5"/><line x1="12" y1="19" x2="20" y2="19"/>
  </svg>
},
{
  path: '/endpoints/inference',
  label: 'Inference',
  sub: true,
  icon: <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"
    fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M12 2a4 4 0 0 0-4 4c0 2 2 3 2 6h4c0-3 2-4 2-6a4 4 0 0 0-4-4z"/>
    <line x1="10" y1="16" x2="14" y2="16"/><line x1="10" y1="19" x2="14" y2="19"/>
  </svg>
},
```

### 15.2 Update: `Dashboard.jsx`

- [ ] **File:** `dashboard/src/components/Dashboard.jsx`
- [ ] Import the new views and add routes:

```jsx
import EmbeddingEndpointsView from '../views/EmbeddingEndpointsView';
import InferenceEndpointsView from '../views/InferenceEndpointsView';

// Inside <Routes>, add (admin-only):
{isAdmin && <Route path="/endpoints/embedding" element={<EmbeddingEndpointsView />} />}
{isAdmin && <Route path="/endpoints/inference" element={<InferenceEndpointsView />} />}
```

---

## 16. Database Migrations & Factory Reset

### 16.1 New Migration File

- [ ] **File:** `migrations/004_add_summarization_to_ingestion_rules.sql` (new)

```sql
-- Migration: Add summarization_json column to ingestion_rules
-- Applies to: All database providers
-- This is a breaking change. For fresh installs, the column is created by TableQueries.cs.
-- For existing databases, run the appropriate ALTER TABLE statement below.

-- SQLite
ALTER TABLE ingestion_rules ADD COLUMN summarization_json TEXT;

-- PostgreSQL
-- ALTER TABLE ingestion_rules ADD COLUMN IF NOT EXISTS summarization_json TEXT;

-- SQL Server
-- IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ingestion_rules') AND name = 'summarization_json')
--   ALTER TABLE ingestion_rules ADD summarization_json NVARCHAR(MAX) NULL;

-- MySQL
-- ALTER TABLE `ingestion_rules` ADD COLUMN `summarization_json` TEXT;
```

### 16.2 Update Factory Database Files

There is no auto-migration mechanism. Breaking changes are acceptable. The factory reset files must be updated so that `reset.bat` / `reset.sh` restore a compatible state.

- [ ] **File:** `docker/factory/assistanthub.db` — Regenerate after the `summarization_json` column is added to the `ingestion_rules` table in `TableQueries.cs`. Steps:
  1. Delete the existing `docker/assistanthub/data/assistanthub.db`
  2. Start the server (it will create a fresh DB with the new schema via `CREATE TABLE IF NOT EXISTS`)
  3. Copy the new DB to `docker/factory/assistanthub.db`

- [ ] **File:** `docker/factory/partio.db` — Regenerate to include the `completion_endpoints` table (v0.4.0 schema). The current factory copy is missing this table. Steps:
  1. Delete the existing `docker/partio/data/partio.db`
  2. Start the Partio v0.4.0 container (it will create a fresh DB with the new schema)
  3. Copy the new DB to `docker/factory/partio.db`

---

## 17. Docker Compose

### 17.1 Verify Partio Version

- [ ] **File:** `docker/compose.yaml`
- [ ] Partio is already set to `v0.4.0` (`jchristn77/partio-server:v0.4.0` and `jchristn77/partio-dashboard:v0.4.0`). **No changes needed.**

### 17.2 Verify Partio Configuration

- [ ] **File:** `docker/partio/partio.json`
- [ ] Ensure the Partio configuration file is compatible with v0.4.0. The existing `DefaultEmbeddingEndpoints` array should still work. If Partio v0.4.0 introduces new top-level config fields, update accordingly.

---

## 18. REST API Documentation

### 18.1 Update: Ingestion Rules Section

- [ ] **File:** `REST_API.md`
- [ ] Update the Ingestion Rules description from "processed, chunked, and embedded" to "processed, summarized, chunked, and embedded."
- [ ] Update the `PUT /v1.0/ingestion-rules` request/response JSON example to include the new `Summarization` property:

```json
{
  "Name": "Knowledge Base Documents",
  "Description": "Process PDF and text documents for the support knowledge base.",
  "Bucket": "kb-documents",
  "CollectionId": "collection-uuid-here",
  "CollectionName": "knowledge-base",
  "Labels": ["support", "knowledge-base"],
  "Tags": { "department": "engineering", "priority": "high" },
  "Summarization": {
    "CompletionEndpointId": "cep_abc123",
    "Order": "BottomUp",
    "MaxSummaryTokens": 1024,
    "MinCellLength": 128,
    "MaxParallelTasks": 4,
    "MaxRetriesPerSummary": 3,
    "MaxRetries": 9,
    "TimeoutMs": 30000,
    "SummarizationPrompt": null
  },
  "Chunking": {
    "Strategy": "FixedTokenCount",
    "FixedTokenCount": 256,
    "OverlapCount": 32
  },
  "Embedding": {
    "EmbeddingEndpointId": null,
    "L2Normalization": false
  }
}
```

- [ ] Add a note that `Summarization` is optional; when null/omitted, the summarization step is skipped.
- [ ] Document each `IngestionSummarizationConfig` property with type, default, and constraints.
- [ ] Add a brief "Summarization" subsection explaining:
  - The pipeline order: Summarize → Chunk → Embed
  - TopDown vs BottomUp processing order (TopDown = root-to-leaves, BottomUp = leaves-to-root)
  - The `CompletionEndpointId` references a Partio completion endpoint (manageable via the Endpoints section in the dashboard)
  - The prompt template placeholders: `{content}` (cell text), `{tokens}` (max summary tokens as integer), `{context}` (adjacent cell context or "(none)")
  - Default prompt: `"Summarize the following content in at most {tokens} tokens.\n\nContent:\n{content}\n\nContext:\n{context}"`

### 18.2 Add: Endpoint Proxy API Documentation

- [ ] **File:** `REST_API.md`
- [ ] Add new sections for Embedding Endpoints and Completion Endpoints, documenting all proxy routes.
- [ ] Include request/response examples showing the Partio endpoint model fields.

---

## 19. README

### 19.1 Update Feature Description

- [ ] **File:** `README.md`
- [ ] Update the "Ingestion Rules" feature bullet to mention summarization:

```
- **Ingestion Rules** -- Define reusable ingestion configurations that specify target S3 buckets, RecallDB collections, summarization strategies, chunking strategies, and embedding settings.
```

- [ ] Update the "Documents" feature bullet:

```
- **Documents** -- Upload documents (PDF, text, HTML, and more) to build a knowledge base for each assistant. Documents are automatically summarized (if configured), chunked, embedded, and indexed.
```

- [ ] Add a feature bullet for endpoint management:

```
- **Endpoint Management** -- Configure and manage Partio embedding and inference endpoints directly from the dashboard.
```

---

## 20. Task Checklist

### Phase 1: Core Models
- [ ] 2.1 — Create `IngestionSummarizationConfig` model
- [ ] 2.2 — Add `Summarization` property to `IngestionRule` model
- [ ] 2.2 — Add `summarization_json` deserialization to `IngestionRule.FromDataRow`
- [ ] 2.3 — Update `ChunkingResponse`/`ChunkWithEmbedding` → `SemanticCellResponse`/`ChunkResult` in `IngestionService`
- [ ] 2.3 — Add `FlattenChunks` helper method in `IngestionService`
- [ ] 2.4 — Update `ProcessResponse`/`ProcessChunk` response models in `RetrievalService`

### Phase 2: Database Layer
- [ ] 3.1 — Update `TableQueries.cs` in all 4 providers (add `summarization_json` column)
- [ ] 3.2 — Update `IngestionRuleMethods.CreateAsync` in all 4 providers
- [ ] 3.2 — Update `IngestionRuleMethods.UpdateAsync` in all 4 providers

### Phase 3: Ingestion & Retrieval Pipeline
- [ ] 5.1 — Update Partio URL in `IngestionService` from v0.1.0 to v0.4.0 (`/v1.0/process`)
- [ ] 5.1 — Add `EmbeddingEndpointId` to `EmbeddingConfiguration` in `IngestionService` request body
- [ ] 5.1 — Add `SummarizationConfiguration` to request body when `rule.Summarization` is present
- [ ] 5.1 — Update response deserialization to use `SemanticCellResponse` + `FlattenChunks`
- [ ] 5.2 — Update processing log messages
- [ ] 6.1 — Update Partio URL in `RetrievalService` from v0.1.0 to v0.4.0 (`/v1.0/process`)
- [ ] 6.1 — Add `EmbeddingEndpointId` to `RetrievalService` request body

### Phase 4: Validation
- [ ] 7.1 — Add `IngestionSummarizationConfig.Validate()` method
- [ ] 9.1 — Wire validation into `IngestionRuleHandler` create/update

### Phase 5: Partio Endpoint Proxy APIs
- [ ] 8.1 — Create `EmbeddingEndpointHandler.cs` (7 handler methods)
- [ ] 8.2 — Create `CompletionEndpointHandler.cs` (7 handler methods)
- [ ] 8.3 — Register all endpoint proxy routes in `AssistantHubServer.cs`

### Phase 6: Dashboard — Endpoint Views
- [ ] 11.1 — Create `EmbeddingEndpointsView.jsx`
- [ ] 11.2 — Create `EmbeddingEndpointFormModal.jsx`
- [ ] 11.3 — Create `InferenceEndpointsView.jsx`
- [ ] 11.4 — Create `InferenceEndpointFormModal.jsx`
- [ ] 14.1 — Add embedding endpoint API methods to `api.js`
- [ ] 14.1 — Add completion endpoint API methods to `api.js`
- [ ] 15.1 — Add Endpoints navigation items to `Sidebar.jsx`
- [ ] 15.2 — Add endpoint view routes to `Dashboard.jsx`

### Phase 7: Dashboard — Summarization in Ingestion Rules
- [ ] 12.1 — Add "Summarization" collapsible section to `IngestionRuleFormModal.jsx` (all 9 fields)
- [ ] 13.1 — Add Summarization column to `IngestionRulesView.jsx`

### Phase 8: Database Migration & Factory Reset
- [ ] 16.1 — Create `migrations/004_add_summarization_to_ingestion_rules.sql`
- [ ] 16.2 — Regenerate `docker/factory/assistanthub.db` with new schema
- [ ] 16.2 — Regenerate `docker/factory/partio.db` with `completion_endpoints` table

### Phase 9: Documentation
- [ ] 17.2 — Verify `docker/partio/partio.json` compatibility with v0.4.0
- [ ] 18.1 — Update `REST_API.md` with Summarization property and documentation
- [ ] 18.2 — Add Endpoint Proxy API documentation to `REST_API.md`
- [ ] 19.1 — Update `README.md` feature descriptions

---

*End of plan.*
