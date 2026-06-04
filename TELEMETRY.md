# AssistantHub v0.12.0 Telemetry Plan

This plan covers the v0.12.0 work to capture provider-agnostic assistant performance telemetry, persist it with chat/request history, expose it through REST/SDK surfaces, and visualize it in the dashboard. It is written as an implementation checklist so progress can be annotated directly.

Implementation status: v0.12.0 telemetry has been implemented in the backend hot path, startup schemas, provider migration scripts, dashboard history/detail views, SDK models, REST API docs, OpenAPI, Postman, Docker compose tags, and shared test suites. Use the checklist below for follow-up hardening and future telemetry expansion.

Legend:

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete
- `Notes:` add implementation notes, PR references, or verification output under each item as work proceeds

## Goals

- [ ] Capture detailed assistant hot-path timings for each chat turn without making the schema depend on any single provider.
- [ ] Preserve existing `chat_history` timing columns for compatibility while adding richer v0.12.0 telemetry.
- [ ] Surface high-signal timing details in the dashboard so cold model load, upstream scheduling, prompt evaluation, retrieval fanout, rerank cost, and generation time can be separated.
- [ ] Link HTTP request history and assistant chat history by a stable trace/correlation identifier.
- [ ] Capture provider-native metrics when available, especially Ollama final stream metrics and OpenAI-compatible usage chunks, while showing unavailable metrics as null/empty rather than zero.
- [ ] Add migrations, startup schema creation, migration scripts, SDK types, REST docs, OpenAPI, Postman, README, CHANGELOG, docker compose tag updates, and tests.

## Non-Goals

- [ ] Do not add provider-specific top-level columns such as `ollama_load_duration_ms`.
- [ ] Do not require Ollama-specific behavior for OpenAI-compatible, Gemini, vLLM, or future providers.
- [ ] Do not write database rows per streamed token.
- [ ] Do not store API keys, authorization headers, raw credentials, or unredacted provider secret headers in telemetry.
- [ ] Do not break old chat history rows that do not have telemetry.

## Current State

- [ ] `chat_history` already stores coarse timing fields:
  - `retrieval_duration_ms`
  - `retrieval_gate_duration_ms`
  - `query_rewrite_duration_ms`
  - `rerank_duration_ms`
  - `endpoint_resolution_duration_ms`
  - `compaction_duration_ms`
  - `inference_connection_duration_ms`
  - `time_to_first_token_ms`
  - `time_to_last_token_ms`
  - `prompt_tokens`
  - `completion_tokens`
  - `tokens_per_second_overall`
  - `tokens_per_second_generation`
- [ ] `InferenceResult` currently carries success/content/error only.
- [ ] The dashboard `HistoryViewModal` derives prompt processing and generation time from coarse fields.
- [ ] Current `Connection` wording is misleading because it means request sent to upstream response headers, not just TCP connect.
- [ ] Provider-native metrics are not currently captured in `ChatHistory`, request history details, SDK models, REST docs, or dashboard details.

## Target Data Model

### Chat History Columns

- [ ] Add nullable `trace_id` to `chat_history`.
  - Purpose: correlate chat history, request history, logs, and telemetry events.
  - Notes:

- [ ] Add nullable `request_history_id` to `chat_history`.
  - Purpose: direct link from assistant history detail to captured HTTP request detail when available.
  - Notes:

- [ ] Add `performance_schema_version INTEGER NOT NULL DEFAULT 1` to `chat_history`.
  - Purpose: make `performance_json` evolvable.
  - Notes:

- [ ] Add nullable `performance_json` to `chat_history`.
  - Purpose: complete versioned telemetry payload for the chat turn.
  - Type guidance:
    - SQLite: `TEXT`
    - PostgreSQL: `JSONB` preferred, `TEXT` acceptable if existing driver patterns favor text
    - MySQL: `JSON` preferred, `LONGTEXT` acceptable if cross-version support is simpler
    - SQL Server: `NVARCHAR(MAX)`
  - Notes:

### Request History Columns

- [ ] Add nullable `trace_id` to `request_history`.
  - Purpose: correlate HTTP request capture to assistant telemetry.
  - Notes:

- [ ] Add nullable `chat_history_id` to `request_history`.
  - Purpose: direct link from request detail to assistant history detail when the request produced a chat turn.
  - Notes:

- [ ] Add nullable `performance_json` to `request_history` only if detail pages need embedded telemetry without a second lookup.
  - Recommendation: prefer storing authoritative telemetry on `chat_history` and linking by `trace_id`; duplicate only a small summary if needed.
  - Notes:

### Performance Event Table

- [ ] Add `chat_history_performance_events` for queryable/drill-down telemetry rows.
  - Recommended columns:
    - `id TEXT PRIMARY KEY`
    - `tenant_id TEXT NOT NULL`
    - `chat_history_id TEXT NOT NULL`
    - `request_history_id TEXT NULL`
    - `trace_id TEXT NULL`
    - `sequence_number INTEGER NOT NULL DEFAULT 0`
    - `stage TEXT NOT NULL`
    - `phase TEXT NULL`
    - `kind TEXT NOT NULL DEFAULT 'operation'`
    - `endpoint_id TEXT NULL`
    - `endpoint_name TEXT NULL`
    - `endpoint_type TEXT NULL`
    - `provider TEXT NULL`
    - `api_format TEXT NULL`
    - `model TEXT NULL`
    - `started_utc TEXT NULL`
    - `finished_utc TEXT NULL`
    - `duration_ms DOUBLE/REAL/FLOAT NOT NULL DEFAULT 0`
    - `success BOOLEAN/INTEGER/BIT NOT NULL DEFAULT true`
    - `http_status_code INTEGER NULL`
    - `error_type TEXT NULL`
    - `error_message TEXT NULL`
    - `input_tokens INTEGER NULL`
    - `output_tokens INTEGER NULL`
    - `total_tokens INTEGER NULL`
    - `chunks_input INTEGER NULL`
    - `chunks_output INTEGER NULL`
    - `retrieval_query_count INTEGER NULL`
    - `endpoint_limiter_wait_ms DOUBLE/REAL/FLOAT NULL`
    - `request_to_headers_ms DOUBLE/REAL/FLOAT NULL`
    - `headers_to_first_token_ms DOUBLE/REAL/FLOAT NULL`
    - `first_token_to_last_token_ms DOUBLE/REAL/FLOAT NULL`
    - `client_total_ms DOUBLE/REAL/FLOAT NULL`
    - `provider_queue_ms DOUBLE/REAL/FLOAT NULL`
    - `provider_load_ms DOUBLE/REAL/FLOAT NULL`
    - `provider_prompt_eval_ms DOUBLE/REAL/FLOAT NULL`
    - `provider_generation_ms DOUBLE/REAL/FLOAT NULL`
    - `provider_total_ms DOUBLE/REAL/FLOAT NULL`
    - `provider_tokens_per_second DOUBLE/REAL/FLOAT NULL`
    - `provider_request_id TEXT NULL`
    - `metadata_json TEXT/JSON NULL`
    - `provider_metrics_json TEXT/JSON NULL`
    - `provider_raw_json TEXT/JSON NULL`
    - `created_utc TEXT NOT NULL`
  - Notes:

### Required Indexes

- [ ] `idx_chat_history_trace_id` on `chat_history(trace_id)`.
- [ ] `idx_chat_history_request_history_id` on `chat_history(request_history_id)`.
- [ ] `idx_request_history_trace_id` on `request_history(trace_id)`.
- [ ] `idx_request_history_chat_history_id` on `request_history(chat_history_id)`.
- [ ] `idx_chpe_tenant_id` on `chat_history_performance_events(tenant_id)`.
- [ ] `idx_chpe_chat_history_id` on `chat_history_performance_events(chat_history_id)`.
- [ ] `idx_chpe_trace_id` on `chat_history_performance_events(trace_id)`.
- [ ] `idx_chpe_stage` on `chat_history_performance_events(stage)`.
- [ ] `idx_chpe_endpoint_id` on `chat_history_performance_events(endpoint_id)`.
- [ ] `idx_chpe_provider_model` on `chat_history_performance_events(provider, model)`.
- [ ] `idx_chpe_created_utc` on `chat_history_performance_events(created_utc)`.
- [ ] `idx_chpe_duration_ms` on `chat_history_performance_events(duration_ms)`.
- [ ] `idx_chpe_tenant_created` on `chat_history_performance_events(tenant_id, created_utc)`.
- [ ] Use MySQL prefix lengths where required for text columns, matching existing project patterns.

## Telemetry JSON Contract

- [ ] Add `AssistantPerformanceTelemetry` model.
- [ ] Add `AssistantPerformanceStage` model.
- [ ] Add `AssistantPerformanceClientTimings` model.
- [ ] Add `AssistantProviderMetrics` model.
- [ ] Add `AssistantTokenUsageTelemetry` model.
- [ ] Add `AssistantRetrievalTelemetry` model.
- [ ] Add `AssistantTelemetryHeader` or generic metadata model for safe provider/header fields.

Recommended shape:

```json
{
  "schemaVersion": 1,
  "traceId": "trace_...",
  "requestHistoryId": "req_...",
  "chatHistoryId": "chist_...",
  "wallTimeMs": 21170.0,
  "createdUtc": "2026-06-04T00:00:00Z",
  "stages": [
    {
      "name": "final_inference",
      "kind": "inference",
      "sequence": 90,
      "endpointId": "cep_...",
      "endpointName": "local-gpt-oss",
      "endpointType": "inference",
      "provider": "Ollama",
      "apiFormat": "Ollama",
      "model": "gpt-oss:120b",
      "startedUtc": "2026-06-04T00:00:10Z",
      "finishedUtc": "2026-06-04T00:00:24Z",
      "durationMs": 14771.0,
      "success": true,
      "clientTimings": {
        "endpointLimiterWaitMs": 0.0,
        "requestToHeadersMs": 2401.0,
        "headersToFirstTokenMs": 8058.0,
        "firstTokenToLastTokenMs": 4311.0,
        "totalMs": 14771.0
      },
      "tokens": {
        "input": 2215,
        "output": 229,
        "total": 2444
      },
      "providerMetrics": {
        "queueMs": null,
        "loadMs": 0.0,
        "promptEvalMs": 7900.0,
        "generationMs": 4300.0,
        "totalMs": 14700.0,
        "tokensPerSecond": 53.2,
        "requestId": null
      },
      "metadata": {
        "finishReason": "stop"
      },
      "providerRaw": {
        "ollama": {
          "load_duration": 0,
          "prompt_eval_duration": 7900000000,
          "eval_duration": 4300000000
        }
      }
    }
  ]
}
```

- [ ] Use null for unavailable provider metrics.
- [ ] Do not convert unavailable metrics to `0`.
- [ ] Include `schemaVersion` in every payload.
- [ ] Include units in field names, preferably `Ms`, `Bytes`, `Tokens`, or `Count`.
- [ ] Keep provider raw data under a provider-specific namespace.
- [ ] Redact provider raw data through an allowlist before persistence.

## Stage and Phase Taxonomy

- [ ] Use stable stage names:
  - `request_received`
  - `assistant_settings_load`
  - `endpoint_resolution`
  - `retrieval_gate`
  - `query_rewrite`
  - `retrieval`
  - `embedding`
  - `vector_search`
  - `rerank`
  - `context_assembly`
  - `compaction`
  - `final_inference`
  - `history_persist`
  - `response_flush`
- [ ] Use stable phase names for inference stages:
  - `endpoint_limiter_wait`
  - `request_to_headers`
  - `headers_to_first_token`
  - `first_token_to_last_token`
  - `provider_queue`
  - `provider_load`
  - `provider_prompt_eval`
  - `provider_generation`
  - `provider_total`
- [ ] Document every stage and phase in REST docs and dashboard tooltips.

## Backend Implementation

### Core Telemetry Types

- [ ] Add telemetry models under `src/AssistantHub.Core/Models`.
- [ ] Add a `TelemetryTrace` or `AssistantPerformanceTrace` collector type under `src/AssistantHub.Core/Services` or `Models`.
- [ ] Add helper methods to start/stop stages using `Stopwatch`.
- [ ] Add helper method to convert nanoseconds to milliseconds for Ollama.
- [ ] Add helper method to safely serialize telemetry JSON with existing serializer options.
- [ ] Add unit tests for serialization, null handling, ordering, and duration math.

### Inference Service

- [ ] Extend `InferenceResult` with a nullable `Telemetry` property.
- [ ] Add a provider-agnostic `InferenceCallTelemetry` model returned by every inference call.
- [ ] Capture client-side timings for every provider:
  - `startedUtc`
  - `requestToHeadersMs`
  - `headersToFirstTokenMs`
  - `firstTokenToLastTokenMs`
  - `clientTotalMs`
  - `httpStatusCode`
  - `providerRequestId`
- [ ] Capture endpoint concurrency limiter wait time around each inference endpoint acquisition.
- [ ] Ensure telemetry is captured for both streaming and non-streaming paths.
- [ ] Ensure cancellation and failures still produce a telemetry stage with `success=false` and error metadata.
- [ ] Ensure telemetry capture does not swallow cancellations.
- [ ] Ensure HTTP responses are disposed in all provider paths.

### Ollama Provider Metrics

- [ ] Extend Ollama stream line models to include final `done=true` metrics:
  - `total_duration`
  - `load_duration`
  - `prompt_eval_count`
  - `prompt_eval_duration`
  - `eval_count`
  - `eval_duration`
- [ ] Capture the same fields for non-streaming Ollama responses if returned.
- [ ] Convert Ollama nanoseconds to milliseconds in normalized provider metrics.
- [ ] Store original nanosecond values in `providerRaw.ollama`.
- [ ] Add parser tests using representative Ollama final stream JSON.
- [ ] Add tests asserting unavailable Ollama fields remain null.

### OpenAI-Compatible and vLLM Metrics

- [ ] For OpenAI-compatible streaming requests, request usage when supported with `stream_options.include_usage=true`.
- [ ] Parse final usage chunks when present.
- [ ] Treat absent final usage as valid and leave provider token metrics null.
- [ ] Capture safe headers:
  - `x-request-id`
  - `openai-processing-ms`
  - `server-timing`
  - rate-limit headers if already exposed and safe
- [ ] Do not fail requests if a backend rejects `stream_options.include_usage`; add a fallback behavior if necessary.
- [ ] Add tests for OpenAI-compatible stream chunks with and without final usage.
- [ ] Add tests for vLLM-like behavior where only client timings and usage are available.

### Gemini Metrics

- [ ] Capture Gemini usage metadata when present.
- [ ] Capture client timings even when provider-native timing is unavailable.
- [ ] Store provider raw data under `providerRaw.gemini` after allowlist filtering.
- [ ] Add tests for Gemini response usage metadata parsing.

### Assistant Chat Pipeline

- [ ] Generate a `trace_id` at the start of every chat request.
- [ ] Put `trace_id` into the HTTP context so `RequestHistoryCaptureService` can persist it.
- [ ] Carry `trace_id` through streaming and non-streaming chat paths.
- [ ] Track stage timings for:
  - assistant/settings load
  - endpoint resolution
  - retrieval gate inference
  - query rewrite inference
  - retrieval fanout
  - embedding calls
  - vector search calls
  - reranking inference
  - context assembly
  - compaction inference
  - final inference
  - chat history persistence
- [ ] Track retrieval fanout:
  - rewritten query count
  - retrieval call count
  - chunks before rerank
  - chunks after rerank
  - unique document count
  - neighbor count
- [ ] Track endpoint metadata for each LLM stage:
  - endpoint ID
  - endpoint name
  - endpoint type
  - provider
  - API format
  - model
  - configured `MaxConcurrentRequests`
- [ ] Continue populating legacy coarse `chat_history` columns.
- [ ] Populate `performance_json` from the full trace.
- [ ] Populate `chat_history_performance_events` from the same trace after the chat history ID is known.
- [ ] On streaming responses, persist final telemetry after the stream completes or fails.
- [ ] Ensure failed/canceled chat requests persist enough telemetry to debug the failure when a history row is created.

### Request History Capture

- [ ] Add `trace_id` capture to `RequestHistoryCaptureService`.
- [ ] Add `chat_history_id` capture/update when available.
- [ ] Fix or verify streaming request duration and response size capture so `/chat` history does not report misleading `0ms`/`0 bytes`.
- [ ] Add request history detail payload or linked metadata so the dashboard can navigate from request history to chat history telemetry.
- [ ] Add tests for request history to chat history correlation.

### Database Drivers

- [ ] Update SQLite `TableQueries.CreateTables()` and `CreateIndices()`.
- [ ] Add SQLite startup ensure-column logic for new `chat_history` and `request_history` columns.
- [ ] Update PostgreSQL `TableQueries` and startup initialization.
- [ ] Update MySQL `TableQueries` and startup ensure-column logic using `INFORMATION_SCHEMA`.
- [ ] Update SQL Server `TableQueries` and startup ensure-column logic using `COL_LENGTH`.
- [ ] Update all `ChatHistoryMethods.CreateAsync` implementations to insert new fields.
- [ ] Update all `ChatHistory.FromDataRow` and DB read paths to handle absent columns safely where practical.
- [ ] Add methods for `ChatHistoryPerformanceEvents` CRUD:
  - create single event
  - create batch events
  - list by chat history ID
  - delete by chat history ID
  - delete by retention cutoff
- [ ] Ensure deleting a `chat_history` row deletes associated performance events.
- [ ] Decide whether to enforce DB foreign keys or preserve current loose-reference style; document the choice.

## Migration Scripts

- [ ] Add `migrations/010_upgrade_to_v0.12.0.sql`.
- [ ] Add `migrations/010_upgrade_to_v0.12.0.sqlite.sql`.
- [ ] Add `migrations/010_upgrade_to_v0.12.0.postgresql.sql`.
- [ ] Add `migrations/010_upgrade_to_v0.12.0.mysql.sql`.
- [ ] Add `migrations/010_upgrade_to_v0.12.0.sqlserver.sql`.
- [ ] Each script must:
  - add `chat_history.trace_id`
  - add `chat_history.request_history_id`
  - add `chat_history.performance_schema_version`
  - add `chat_history.performance_json`
  - add `request_history.trace_id`
  - add `request_history.chat_history_id`
  - create `chat_history_performance_events`
  - create all indexes listed above
- [ ] Make scripts idempotent where the backend supports it.
- [ ] For SQLite, document that `ALTER TABLE ADD COLUMN` should only be run once unless startup migrations are used.
- [ ] For MySQL and SQL Server, use backend-specific existence checks.
- [ ] Add migration verification tests for every supported database type.

## REST API

- [ ] Update `GET /v1.0/history` response schema to include new summary/link fields:
  - `TraceId`
  - `RequestHistoryId`
  - `PerformanceSchemaVersion`
  - optionally omit `PerformanceJson` from list results if too large
- [ ] Update `GET /v1.0/history/{historyId}` to include detailed telemetry.
- [ ] Add optional endpoint if payload size requires separation:
  - `GET /v1.0/history/{historyId}/performance`
- [ ] Add optional endpoint to list queryable events:
  - `GET /v1.0/history/{historyId}/performance/events`
- [ ] Update `GET /v1.0/request-history/{requestId}` detail to include `TraceId`, `ChatHistoryId`, or linked assistant telemetry summary.
- [ ] Update error response behavior so missing telemetry returns an empty telemetry object or `404` only when the history row is missing.
- [ ] Update `openapi.json`.
- [ ] Update `REST_API.md` with examples.
- [ ] Add REST API tests for old rows without telemetry and new rows with telemetry.

## SDK Updates

### C# SDK

- [ ] Add telemetry models under `sdk/csharp/AssistantHub.Sdk/Models`.
- [ ] Update `ChatHistory` with:
  - `TraceId`
  - `RequestHistoryId`
  - `PerformanceSchemaVersion`
  - `PerformanceJson` or strongly typed `Performance`
- [ ] Update `RequestHistoryEntry` with:
  - `TraceId`
  - `ChatHistoryId`
- [ ] Add `GetHistoryPerformanceAsync` if a dedicated REST endpoint is added.
- [ ] Update SDK tests to parse and validate telemetry fields.
- [ ] Update `sdk/csharp/README.md` and `sdk/csharp/TESTING.md`.

### JavaScript SDK

- [ ] Add TypeScript telemetry interfaces in `sdk/js/src/types.ts`.
- [ ] Update `ChatHistory` and `RequestHistoryEntry` interfaces.
- [ ] Add client method for dedicated performance endpoint if added.
- [ ] Update package version to `0.12.0`.
- [ ] Update JS SDK tests to validate telemetry parsing.
- [ ] Update `sdk/js/README.md` and `sdk/js/TESTING.md`.

### Python SDK

- [ ] Add Pydantic telemetry models in `sdk/python/assistanthub_sdk/models.py`.
- [ ] Update `ChatHistory` and `RequestHistoryEntry`.
- [ ] Add sync/async methods for dedicated performance endpoint if added.
- [ ] Update package version to `0.12.0`.
- [ ] Update Python SDK tests to validate telemetry parsing.
- [ ] Update `sdk/python/README.md` and `sdk/python/TESTING.md`.

## Dashboard UX

### History List

- [ ] Add optional columns or compact indicators:
  - total wall time
  - final model
  - utility model summary
  - slowest stage
  - request/history trace link
- [ ] Add filters for assistant, thread, endpoint/model, and slow stage if backend supports it.
- [ ] Keep list rows dense and scannable.

### History Details Performance Section

- [ ] Rename `Connection` to `Request -> Headers`.
- [ ] Add tooltip explaining that `Request -> Headers` can include network, provider queue, model load, upstream scheduler wait, and provider work before headers.
- [ ] Replace the single flat timing list with grouped sections:
  - Overview
  - Pipeline timeline
  - Retrieval details
  - Inference calls
  - Provider metrics
  - Raw telemetry
- [ ] Keep legacy fallback rendering for rows without `performance_json`.
- [ ] Add a waterfall/timeline visualization:
  - fixed row heights
  - proportional bars
  - hover tooltip per stage
  - click to drill into stage
  - clear unavailable/null state
- [ ] Add stage drill-down panels for:
  - retrieval gate
  - query rewrite
  - retrieval fanout
  - rerank
  - compaction
  - final inference
- [ ] Show endpoint/model metadata per inference stage:
  - endpoint name
  - endpoint ID
  - provider
  - API format
  - model
  - max concurrency
  - limiter wait
- [ ] Show provider-native metrics when available:
  - provider load
  - provider queue
  - provider prompt eval
  - provider generation
  - provider total
  - provider request ID
- [ ] Show token metrics:
  - prompt/input tokens
  - completion/output tokens
  - total tokens
  - overall tokens/sec
  - generation tokens/sec
- [ ] Show retrieval fanout metrics:
  - query variants
  - retrieval calls
  - chunks returned
  - chunks before rerank
  - chunks after rerank
  - unique documents
  - neighbors
- [ ] Add a clear visual note when utility endpoints differ from final inference endpoint.
- [ ] Add raw JSON viewer with copy button for `performance_json`.
- [ ] Add responsive layout checks for desktop and mobile.

### Request History Details

- [ ] Show linked chat history ID when present.
- [ ] Show linked trace ID.
- [ ] Add a compact assistant telemetry summary for chat requests.
- [ ] Add navigation from request detail to chat history detail.

### Dashboard Tests

- [ ] Add or expand frontend test infrastructure if missing.
- [ ] Add component tests for telemetry parsing and fallback rendering.
- [ ] Add screenshot/visual checks for:
  - no telemetry
  - full Ollama telemetry
  - OpenAI-compatible telemetry with usage only
  - long model/endpoint names
  - mobile viewport
- [ ] Add regression test that no text overlaps in the performance section.

## Documentation

- [ ] Update `README.md`:
  - v0.12.0 telemetry overview
  - dashboard history performance section
  - provider support matrix
  - docker compose image tags
- [ ] Update `REST_API.md`:
  - new fields on chat history
  - new performance endpoint if added
  - request history correlation fields
  - example telemetry payload
  - field semantics and units
- [ ] Update `CHANGELOG.md` with v0.12.0 section.
- [ ] Update `TESTING.md` with new telemetry test suites and any frontend test runner.
- [ ] Update `MCP_API.md` if MCP history/request-history tools expose new fields.
- [ ] Update `docs/CLAUDE_MCP.md` if MCP tool descriptions change.
- [ ] Update SDK docs:
  - `sdk/csharp/README.md`
  - `sdk/js/README.md`
  - `sdk/python/README.md`
- [ ] Update SDK testing docs:
  - `sdk/csharp/TESTING.md`
  - `sdk/js/TESTING.md`
  - `sdk/python/TESTING.md`
- [ ] Update comments/XML docs for new public C# models and properties.

## Postman and OpenAPI

- [ ] Update `openapi.json`.
- [ ] Update `postman/AssistantHub.postman_collection.json`.
- [ ] Add or update Postman requests:
  - create chat request
  - get history item
  - get history performance detail if endpoint added
  - get request history detail with trace/chat link
- [ ] Add Postman tests asserting:
  - `TraceId` exists on new chat history
  - `PerformanceSchemaVersion` is `1`
  - `Performance` or `PerformanceJson` contains stages
  - request history detail links to chat history where available

## MCP Surface

- [ ] Review `src/AssistantHub.McpServer/Registrations/HistoryRegistrations.cs`.
- [ ] Review `src/AssistantHub.McpServer/Registrations/RequestHistoryRegistrations.cs`.
- [ ] Expose telemetry fields returned by REST through MCP tools.
- [ ] Add a dedicated MCP method for history performance if a dedicated REST endpoint is added.
- [ ] Update MCP tests in `Test.Shared/McpSuite.cs`.
- [ ] Update `MCP_API.md`.

## Configuration

- [ ] Add telemetry settings if needed:
  - `ChatHistory.EnablePerformanceTelemetry` default `true`
  - `ChatHistory.CaptureProviderRawTelemetry` default `true`
  - `ChatHistory.MaxPerformanceJsonBytes` default large enough for normal traces
  - `ChatHistory.CaptureProviderHeaders` default `true`
  - `ChatHistory.ProviderHeaderAllowlist`
- [ ] Ensure defaults do not leak secrets.
- [ ] Update config JSON examples in `docker/assistanthub/assistanthub.json`.
- [ ] Update any docker factory/default config assets if present.
- [ ] Update configuration dashboard if telemetry settings are user-editable.

## Versioning and Docker Assets

- [ ] Update `src/AssistantHub.Core/Constants.cs` to `0.12.0`.
- [ ] Update all `.csproj` versions to `0.12.0`:
  - `src/AssistantHub.Core/AssistantHub.Core.csproj`
  - `src/AssistantHub.Server/AssistantHub.Server.csproj`
  - `src/AssistantHub.McpServer/AssistantHub.McpServer.csproj`
  - `sdk/csharp/AssistantHub.Sdk/AssistantHub.Sdk.csproj`
  - test projects if versioned
- [ ] Update dashboard package version if present.
- [ ] Update JS SDK `package.json` and lockfile version to `0.12.0`.
- [ ] Update Python SDK `pyproject.toml` and package metadata to `0.12.0`.
- [ ] Update docker configuration software versions where present.
- [ ] Update `docker/compose.yaml` AssistantHub image tags:
  - `jchristn77/assistanthub-server:v0.12.0`
  - `jchristn77/assistanthub-mcp:v0.12.0`
  - `jchristn77/assistanthub-dashboard:v0.12.0`
- [ ] Verify build scripts still use optimized build contexts.
- [ ] Verify `docker/update.bat` and `docker/update.sh` continue to work.

## Testing Plan

### Shared .NET Test Coverage

- [ ] Add telemetry model tests in `src/Test.Shared/ModelSuite.cs`.
- [ ] Add telemetry service/helper tests in `src/Test.Shared/ServiceSuite.cs`.
- [ ] Add database tests for SQLite startup schema creation.
- [ ] Add migration script validation tests.
- [ ] Add parser tests for:
  - Ollama final stream metrics
  - Ollama non-streaming metrics
  - OpenAI-compatible final usage chunk
  - OpenAI-compatible response without usage
  - Gemini usage metadata
  - provider headers allowlist/redaction
- [ ] Add endpoint limiter wait telemetry test.
- [ ] Add failed inference telemetry test.
- [ ] Add canceled inference telemetry test.
- [ ] Add null/unavailable metrics test.

### API and Integration Tests

- [ ] Extend `src/Test.Shared/ApiSuite.cs`:
  - create chat request
  - read history
  - assert trace ID
  - assert performance schema version
  - assert performance stages
  - assert legacy fields still populated
- [ ] Extend `src/Test.Shared/IntegrationSuite.cs` with fake providers:
  - fake Ollama streaming provider
  - fake OpenAI-compatible streaming provider
  - fake Gemini provider if current test infrastructure supports it
- [ ] Assert request history and chat history are correlated.
- [ ] Assert streaming request history duration is non-zero.
- [ ] Assert older rows without telemetry deserialize correctly.

### Test Runners

- [ ] Ensure `src/Test.Automated` runs the new suites.
- [ ] Ensure `src/Test.Xunit` exposes the new suites.
- [ ] Ensure `src/Test.Nunit` exposes the new suites.
- [ ] Ensure runners start any required test services themselves.
- [ ] Update `run-tests.bat`, `run-tests.ps1`, and `run-tests.sh` if new frontend or SDK test steps become part of the standard run.

### SDK Tests

- [ ] Extend C# SDK tests for telemetry models and history detail parsing.
- [ ] Extend JS SDK tests for telemetry interfaces and parsing.
- [ ] Extend Python SDK tests for Pydantic telemetry models.
- [ ] Add fixture JSON covering full telemetry and no telemetry.

### Database Backend Coverage

- [ ] Add SQLite schema/migration verification.
- [ ] Add PostgreSQL schema/migration verification.
- [ ] Add MySQL schema/migration verification.
- [ ] Add SQL Server schema/migration verification.
- [ ] If full DB services are too heavy for default test runs, add an opt-in env var and document it:
  - `ASSISTANTHUB_TEST_DATABASES=sqlite,postgresql,mysql,sqlserver`

### Frontend Coverage

- [ ] Add dashboard unit/component tests if missing.
- [ ] Add telemetry timeline rendering tests.
- [ ] Add detail drill-down rendering tests.
- [ ] Add responsive screenshot checks for history detail modal.
- [ ] Add long endpoint/model name layout tests.

### Manual Verification

- [ ] Run release build.
- [ ] Run all .NET tests.
- [ ] Run SDK tests.
- [ ] Run dashboard tests.
- [ ] Start docker deployment.
- [ ] Send cold Ollama chat request.
- [ ] Send hot Ollama chat request.
- [ ] Verify `Request -> Headers`, provider load, prompt eval, and generation values are separated.
- [ ] Verify OpenAI-compatible backend still works when provider-native timings are unavailable.
- [ ] Verify dashboard falls back correctly for old history rows.

## Acceptance Criteria

- [ ] All supported database startup paths create the new schema on a fresh database.
- [ ] All migration scripts apply successfully to v0.11.0 databases.
- [ ] Existing v0.11.0 history rows can still be read and displayed.
- [ ] New chat history rows include trace ID and performance telemetry.
- [ ] Request history rows for chat requests can be correlated to chat history rows.
- [ ] Ollama load, prompt eval, generation, total duration, and eval counts are captured when available.
- [ ] OpenAI-compatible and vLLM-style backends continue to work without provider-native timings.
- [ ] Dashboard performance timing clearly separates client timings and provider-native metrics.
- [ ] Dashboard uses `Request -> Headers` instead of `Connection`.
- [ ] SDKs expose the new fields without breaking existing clients.
- [ ] REST docs, OpenAPI, Postman, README, CHANGELOG, MCP docs, and testing docs are updated.
- [ ] `docker/compose.yaml` references AssistantHub `v0.12.0` image tags.
- [ ] Release build succeeds.
- [ ] Automated tests pass across `Test.Automated`, `Test.Xunit`, and `Test.Nunit`.

## Implementation Order

- [ ] Phase 1: Data contracts and schema
  - telemetry models
  - database columns/table/indexes
  - migrations
  - startup schema creation
- [ ] Phase 2: Backend capture
  - trace ID propagation
  - inference telemetry
  - provider parsers
  - chat pipeline stages
  - request history correlation
- [ ] Phase 3: REST/SDK surfaces
  - history detail response
  - optional performance endpoint
  - OpenAPI
  - SDK models and methods
- [ ] Phase 4: Dashboard
  - performance section redesign
  - timeline/drill-down
  - request history link
  - fallback rendering
- [ ] Phase 5: Tests and docs
  - shared tests
  - API/integration tests
  - SDK tests
  - frontend tests
  - documentation and Postman
- [ ] Phase 6: Version and release assets
  - versions
  - docker compose tags
  - release build
  - final verification

## Open Decisions

- [ ] Decide whether `performance_json` should deserialize to a strongly typed `Performance` property in REST responses or remain a raw JSON string plus optional typed endpoint response.
- [ ] Decide whether `chat_history_performance_events` should be required in v0.12.0 or whether `performance_json` alone is enough for the first release.
- [ ] Decide whether dashboard should fetch full telemetry in the initial history detail response or lazily via `GET /history/{id}/performance`.
- [ ] Decide whether provider raw telemetry is enabled by default or only normalized metrics are enabled by default.
- [ ] Decide whether frontend test infrastructure should be Vitest/React Testing Library, Playwright, or both.
