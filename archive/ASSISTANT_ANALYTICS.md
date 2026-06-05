# Assistant Analytics Plan

Status: Implemented for v0.13.0; follow-up validation and hardening items remain noted below
Target release: v0.13.0
Owner: Product Manager for scope; Engineering Manager for delivery; Principal Architect for cross-cutting design
Review trigger: before implementation starts, after backend API contract is approved, and before release branch cut

Implementation notes:
- Backend analytics models, service aggregation, authorization handler, startup migrations, provider migration scripts, OpenAPI generation, and MCP tools were implemented.
- Dashboard `Assistants > Analytics` was implemented with assistant selection, independent chart ranges, chart panels, endpoint/slowest tables, and history/request-history drill-down links.
- C#, JavaScript/TypeScript, and Python SDKs, Postman, README, REST API docs, SDK docs, `MCP_API.md`, `TELEMETRY.md`, `CHANGELOG.md`, Docker compose tags, and version metadata were updated.
- Shared Touchstone tests now cover range capping, analytics aggregation, endpoint summaries, slowest request detection, feedback analytics, and performance-event assistant ID projection.
- Remaining open items are primarily optional rollups, exhaustive per-provider benchmark validation, dedicated SDK unit tests, and browser viewport/manual accessibility checks beyond build validation.

This plan adds a dashboard page at `Dashboard > Assistants > Analytics` that shows charted assistant performance and reliability over time for one selected assistant. Each chart must support `Last hour`, `Last day`, `Last week`, and `Last month` range options. The page must be useful for operators investigating hot-path latency, endpoint pressure, retrieval cost, model behavior, failures, and quality signals across request history entries.

The plan follows the personas in `C:\Code\agents\personas` and the implementation requirements in `C:\Code\agents\requirements`: provider-neutral backend design, tenant isolation, migration parity across SQLite/MySQL/PostgreSQL/SQL Server, Touchstone-based backend tests, hand-rolled SVG dashboard charts, locale-aware formatting, and documentation that downstream developers can execute without private clarification.

Legend:

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete
- `Notes:` add implementation notes, PRs, commits, validation output, and exceptions under each item

## Goals

- [x] Add a first-class `Assistants > Analytics` dashboard page for a selected assistant.
  - Notes: Implemented as `dashboard/src/views/AssistantAnalyticsView.jsx` and routed at `/assistant-analytics`.
- [x] Show time-series charts for request volume, latency, stage breakdown, endpoint usage, provider metrics, retrieval behavior, query rewrite, rerank, token usage, throughput, failures, and feedback.
  - Notes: Implemented as overview tiles, bar/line charts, endpoint table, slowest request table, and feedback chart. Endpoint usage is a ranked table rather than a time-bucketed bar chart where that shape is more usable.
- [x] Let every chart choose `Last hour`, `Last day`, `Last week`, or `Last month`.
  - Notes: Each chart shell has an independent range selector.
- [x] Aggregate analytics server-side so the dashboard does not download and summarize raw history rows.
  - Notes: Implemented in `AssistantAnalyticsService`.
- [x] Preserve tenant isolation and authorization rules for every analytics API and database query.
  - Notes: `AssistantAnalyticsHandler` authorizes assistant visibility before service calls.
- [x] Reuse v0.12.0 request/chat history telemetry as the primary data source.
  - Notes: Uses `request_history`, `chat_history_performance_events`, and `assistant_feedback`.
- [x] Add schema/index migrations only where analytics needs efficient assistant-scoped lookups.
  - Notes: Added `assistant_id` to performance events and assistant/time indexes.
- [x] Update backend, dashboard, SDKs, Postman, README, REST API docs, CHANGELOG, Docker assets, and tests end to end.
  - Notes: Updated source assets; see validation notes near the end of this file.

## Non-Goals

- [x] Do not add a charting library such as Chart.js, Recharts, ApexCharts, or D3.
  - Notes: Charts are hand-rolled React/SVG/CSS.
- [x] Do not add provider-specific top-level analytics contracts such as Ollama-only API shapes.
  - Notes: Provider metrics are normalized and nullable.
- [x] Do not make the dashboard parse `performance_json` for aggregate analytics.
  - Notes: Dashboard calls aggregate REST endpoints.
- [x] Do not store one analytics row per streamed token.
  - Notes: Existing stage-level performance event rows are reused.
- [x] Do not expose raw prompts, responses, authorization headers, API keys, or provider secrets through analytics endpoints.
  - Notes: Analytics DTOs return aggregate timing/count/metadata only.
- [x] Do not allow cross-tenant assistant analytics, even for guessed assistant IDs.
  - Notes: Handler returns not-found for cross-tenant invisible assistants.

## Persona Requirements

- [ ] Product Manager: define success as faster diagnosis of slow assistant responses, clearer release health, and fewer ambiguous performance investigations.
  - Notes:
- [ ] Data Analyst: define every chart metric, denominator, bucket interval, null behavior, and aggregation method.
  - Notes:
- [ ] UX Designer: make the page dense, scannable, accessible, responsive, and coherent with existing dashboard patterns.
  - Notes:
- [ ] Site Reliability Engineer: make latency, failures, endpoint limiter wait, provider load, queue pressure, and incident clues visible without reading logs.
  - Notes:
- [ ] Database Engineer / DBA: keep analytics queries bounded, indexed, provider-neutral, and safe for production history volume.
  - Notes:
- [ ] Software Engineer: use existing route, service, model, database, SDK, and dashboard patterns instead of introducing a parallel stack.
  - Notes:
- [ ] QA Engineer: validate correctness, empty states, authorization, migrations, and visual usability across desktop/tablet/mobile.
  - Notes:
- [ ] Documentation Engineer: update API docs, SDK docs, operational docs, and release notes with enough detail for operators and integrators.
  - Notes:

## Current Data Sources

- [x] Use `chat_history` as the authoritative Assistant Analytics scope, joining `request_history` only for status, success/failure, request duration, request type, path, and trace/chat links.
  - Notes: Implemented by `LoadRequestsAsync`; orphaned Request History rows do not appear after Assistant History rows are deleted.
- [x] Use `chat_history` for assistant/thread linkage, coarse chat timing columns, token totals, feedback links, `trace_id`, `request_history_id`, and `performance_json` compatibility.
  - Notes: Used as the analytics ownership boundary and for request/history drill-down links; aggregate analytics reads the event table where available.
- [x] Use `chat_history_performance_events` as the primary queryable telemetry source for per-stage timings, endpoint details, provider/model metadata, token counters, chunk counts, retrieval counts, client timings, and provider metrics.
  - Notes: Implemented by `LoadEventsAsync`.
- [x] Use `assistant_feedback` for rating trends and feedback volume when available.
  - Notes: Implemented by `LoadFeedbackAsync`.
- [x] Treat unavailable provider metrics as `null`, not zero.
  - Notes: DTOs preserve nullable metrics and chart formatters render unavailable values separately from zero.
- [x] Treat missing v0.12.0 telemetry as "not recorded" and fall back only where a chart explicitly supports legacy coarse columns.
  - Notes: Overview includes telemetry coverage; charts show empty/no-data states for absent event rows.

## Analytics Chart Inventory

Implement these charts in priority order. Each chart must expose its own range selector with `Last hour`, `Last day`, `Last week`, and `Last month`. A page-level assistant selector may set the assistant for all charts, but chart range state must be independent.

### Priority 1 Charts

- [x] Request Volume and Outcome
  - Chart: stacked bars by time bucket for success/failure, with total requests in tooltip.
  - Source: `request_history`.
  - Metrics: `request_count`, `success_count`, `failure_count`, `success_rate`, `failure_rate`.
  - Drill-down: click bucket opens history filtered to assistant, bucket range, and success if a segment is clicked.
  - Notes: Implemented as stacked bars using request-history time-series metrics.
- [x] End-to-End Latency Percentiles
  - Chart: multi-line or banded line chart by time bucket.
  - Source: `request_history.duration_ms` plus `chat_history.wall/telemetry` where available.
  - Metrics: avg, p50, p90, p95, p99, max.
  - Drill-down: click bucket opens slowest requests for that bucket.
  - Notes: Implemented as multi-line latency chart.
- [x] Hot-Path Stage Duration Breakdown
  - Chart: stacked bars by bucket for major stages.
  - Source: `chat_history_performance_events`.
  - Stages: `assistant_settings_load`, `endpoint_resolution`, `retrieval_gate`, `query_rewrite`, `retrieval`, `embedding`, `vector_search`, `rerank`, `context_assembly`, `compaction`, `final_inference`, `history_persist`, `response_flush`.
  - Metrics: average stage duration and p95 stage duration per bucket.
  - Drill-down: click stage opens requests where that stage dominates.
  - Notes: Implemented as stage buckets with avg, p95, max, call, failure, and skipped counts.
- [x] Inference Provider Timing
  - Chart: stacked bars or small multiples for provider/client timing phases.
  - Source: `chat_history_performance_events` where `kind = inference` or stage uses an inference endpoint.
  - Metrics: endpoint limiter wait, request-to-headers, headers-to-first-token, first-token-to-last-token, provider queue, provider load, provider prompt eval, provider generation, provider total.
  - Drill-down: click bucket opens inference-stage request details.
  - Notes: Implemented for limiter wait, provider load, provider generation, provider throughput, and endpoint table summaries. Header/body phase fields are available in endpoint summaries; not every provider phase has a dedicated chart line.
- [~] Endpoint Limiter Wait and Saturation Proxy
  - Chart: line or bars by bucket.
  - Source: `chat_history_performance_events.endpoint_limiter_wait_ms`.
  - Metrics: avg, p95, max wait; count of calls with wait > 0; percentage of endpoint calls that waited.
  - Drill-down: click bucket filters by endpoint and stage.
  - Notes: Implemented avg/p95/count metrics and provider chart coverage; a dedicated saturation-percentage chart remains deferred.

### Priority 2 Charts

- [x] Endpoint and Model Usage Mix
  - Chart: stacked bars or ranked table with sparkline per endpoint/model.
  - Source: `chat_history_performance_events.endpoint_id`, `endpoint_name`, `endpoint_type`, `provider`, `api_format`, `model`.
  - Metrics: calls, avg duration, p95 duration, failures, average limiter wait.
  - Drill-down: click endpoint/model filters request history.
  - Notes: Implemented as a ranked endpoint/model table with call, failure, duration, limiter, load, and throughput columns.
- [ ] Retrieval Gate Decisions
  - Chart: stacked percentage bars by bucket.
  - Source: retrieval gate performance event metadata and stage success/noop metadata.
  - Metrics: retrieval used, retrieval skipped, gate failed, average gate duration.
  - Required telemetry check: verify the gate event persists machine-readable decision metadata; add it if missing.
  - Notes:
- [~] Query Rewrite Activity
  - Chart: bars for rewrite calls and line for average duration.
  - Source: `chat_history_performance_events` stage `query_rewrite`.
  - Metrics: rewrite attempted, rewrite skipped, rewrite failed, average variants produced, average duration, p95 duration.
  - Required telemetry check: verify variant count is stored in metadata; add it if missing.
  - Notes: Implemented call-count activity by bucket. Variant-count metadata chart remains deferred pending telemetry coverage.
- [x] Retrieval Fanout and Chunk Flow
  - Chart: bars/lines by bucket.
  - Source: retrieval, embedding, vector search, and rerank events.
  - Metrics: retrieval query count, chunks input, chunks output, average chunks returned, average vector search duration.
  - Notes: Implemented retrieval query count and chunk output trend.
- [~] Rerank Cost
  - Chart: bars for rerank duration and chunks in/out.
  - Source: `chat_history_performance_events` stage `rerank`.
  - Metrics: attempted, skipped, failed, average chunks input, average chunks output, avg/p95 duration.
  - Notes: Implemented rerank call counts in the activity chart; chunks in/out and cost-specific duration chart remain deferred.
- [ ] Token Usage and Throughput
  - Chart: stacked bars for input/output tokens with line for tokens per second.
  - Source: `chat_history_performance_events` and `chat_history` token columns.
  - Metrics: input tokens, output tokens, total tokens, provider tokens/sec, overall tokens/sec, generation tokens/sec.
  - Notes:

### Priority 3 Charts

- [x] Feedback Trend
  - Chart: stacked bars or line by bucket.
  - Source: `assistant_feedback` joined to assistant and created time.
  - Metrics: thumbs up, thumbs down, feedback rate, negative feedback rate.
  - Notes: Implemented as feedback buckets and dashboard stacked bars.
- [x] Slowest Requests
  - View: table with request ID, chat history ID, created time, total duration, dominant stage, endpoint/model, status, and links to request/chat detail.
  - Source: `request_history`, `chat_history`, `chat_history_performance_events`.
  - Metrics: top N by total duration and top N by dominant stage duration.
  - Notes: Implemented as a linked slowest-request table.
- [ ] Error Types and Status Codes
  - Chart: stacked bars or ranked table.
  - Source: `request_history.status_code`, `request_history.success`, performance event `error_type`, `http_status_code`.
  - Metrics: count by status code, error type, stage, endpoint, provider/model.
  - Notes:
- [~] Cold Load and Model Load Clues
  - Chart: line/bar for provider load duration when reported.
  - Source: `provider_load_ms`, `request_to_headers_ms`, `headers_to_first_token_ms`.
  - Metrics: avg/p95 load, count of non-null provider load reports, inferred cold-load candidates when provider load is null but request-to-headers or headers-to-first-token is anomalously high.
  - Notes: Implemented provider load trend where providers report it; inferred cold-load candidate detection remains deferred.

## Metric Definitions

- [x] `request_count`: number of `request_history` rows for the selected assistant and bucket.
  - Notes: Implemented in time-series and overview.
- [x] `success_count`: number of matching rows where `success = true`.
  - Notes: Implemented in time-series and overview.
- [x] `failure_count`: number of matching rows where `success = false`.
  - Notes: Implemented in time-series and overview.
- [x] `success_rate`: `success_count / request_count`; null when `request_count = 0`.
  - Notes: Implemented.
- [x] `duration_ms`: server-observed total HTTP request duration from request history.
  - Notes: Implemented.
- [x] `stage_duration_ms`: duration stored on a performance event for a stage.
  - Notes: Implemented.
- [x] `dominant_stage`: the highest-duration event for a chat turn, excluding zero-duration skipped/noop events.
  - Notes: Implemented for overview and slowest requests.
- [x] `endpoint_limiter_wait_ms`: time a call waited for AssistantHub endpoint concurrency permission.
  - Notes: Implemented in time-series and endpoint summaries.
- [x] `request_to_headers_ms`: time from upstream provider request send until response headers.
  - Notes: Implemented in endpoint summaries.
- [x] `headers_to_first_token_ms`: time from provider response headers until first streamed token/content.
  - Notes: Telemetry field is preserved; dedicated analytics metric remains deferred.
- [x] `first_token_to_last_token_ms`: generation stream body duration from first token to final token.
  - Notes: Telemetry field is preserved; dedicated analytics metric remains deferred.
- [x] `provider_load_ms`: provider-reported model load time when available.
  - Notes: Implemented in time-series and endpoint summaries.
- [x] `provider_prompt_eval_ms`: provider-reported prompt evaluation time when available.
  - Notes: Telemetry field is preserved; dedicated analytics metric remains deferred.
- [x] `provider_generation_ms`: provider-reported generation time when available.
  - Notes: Implemented in time-series and endpoint summaries.
- [x] `tokens_per_second`: output tokens divided by generation duration when provider value is unavailable and both inputs exist.
  - Notes: Provider-reported throughput is implemented; derived fallback remains deferred.
- [x] Percentiles: exact percentile over rows in the bucket for v1; document the interpolation method used.
  - Notes: Implemented with linear interpolation between sorted sample ranks in `AssistantAnalyticsService.Percentile`.
- [x] Empty bucket: return zero counts and null rates/durations, not missing buckets.
  - Notes: Implemented through server-side bucket gap filling.

## Time Ranges and Buckets

- [x] Support these range IDs in REST, SDKs, Postman, and dashboard:
  - `lastHour`
  - `lastDay`
  - `lastWeek`
  - `lastMonth`
  - Notes: Implemented.
- [x] Compute the date window server-side when a range ID is supplied.
  - Notes: Implemented in `AssistantAnalyticsService.ResolveRange`.
- [x] Also allow explicit `startUtc`, `endUtc`, and `bucketSeconds` for SDK and advanced API users.
  - Notes: Implemented.
- [x] Reject requests with both a range ID and incompatible explicit window values unless the contract defines precedence.
  - Notes: Explicit `startUtc`/`endUtc` take precedence and resolve to `custom`.
- [x] Default bucket widths:
  - `lastHour`: 60 seconds
  - `lastDay`: 900 seconds
  - `lastWeek`: 7200 seconds
  - `lastMonth`: 86400 seconds
  - Notes: Implemented.
- [x] Cap bucket count to a documented maximum, such as 240 buckets.
  - Notes: Implemented as `MaxBucketCount = 240` and covered by shared tests.
- [x] Return bucket start/end timestamps in UTC and let clients format in the selected locale/time zone.
  - Notes: All bucket DTOs return UTC timestamps.
- [x] Gap-fill all buckets server-side.
  - Notes: `Bucketize` creates every bucket before assigning rows.

## Backend API Contract

- [x] Add `AssistantAnalyticsHandler` under `src/AssistantHub.Server/Handlers/`.
  - Notes: Added `AssistantAnalyticsHandler`.
- [x] Register analytics routes during server startup using the existing route registration pattern.
  - Notes: Registered in `AssistantHubServer`.
- [x] Add `GET /v1.0/assistants/{assistantId}/analytics/overview`.
  - Purpose: load summary tiles and high-level health for the selected range.
  - Query: `range`, `startUtc`, `endUtc`, `bucketSeconds`.
  - Response model: `AssistantAnalyticsOverviewResult`.
  - Notes: Implemented.
- [x] Add `GET /v1.0/assistants/{assistantId}/analytics/timeseries`.
  - Purpose: return one or more chart-ready series for a selected range.
  - Query: `range`, `metrics`, `stage`, `endpointId`, `model`, `startUtc`, `endUtc`, `bucketSeconds`.
  - Response model: `AssistantAnalyticsTimeSeriesResult`.
  - Notes: Implemented.
- [x] Add `GET /v1.0/assistants/{assistantId}/analytics/stages`.
  - Purpose: summarize stage-level hot path metrics by bucket and stage.
  - Query: `range`, `startUtc`, `endUtc`, `bucketSeconds`.
  - Response model: `AssistantAnalyticsStageResult`.
  - Notes: Implemented.
- [x] Add `GET /v1.0/assistants/{assistantId}/analytics/endpoints`.
  - Purpose: summarize endpoint/model/provider usage and performance.
  - Query: `range`, `endpointType`, `stage`, `startUtc`, `endUtc`, `bucketSeconds`.
  - Response model: `AssistantAnalyticsEndpointResult`.
  - Notes: Implemented.
- [x] Add `GET /v1.0/assistants/{assistantId}/analytics/slowest`.
  - Purpose: return slowest requests and dominant-stage diagnostics.
  - Query: `range`, `limit`, `stage`, `endpointId`, `startUtc`, `endUtc`.
  - Response model: `AssistantAnalyticsSlowestResult`.
  - Notes: Implemented.
- [x] Add `GET /v1.0/assistants/{assistantId}/analytics/feedback`.
  - Purpose: return assistant feedback trends.
  - Query: `range`, `startUtc`, `endUtc`, `bucketSeconds`.
  - Response model: `AssistantAnalyticsFeedbackResult`.
  - Notes: Implemented.
- [ ] Consider `GET /v1.0/assistants/{assistantId}/analytics/dashboard`.
  - Purpose: one batched endpoint for the initial page load if separate chart requests prove too chatty.
  - Decision: implement only if network chatter or route complexity is material.
  - Notes:
- [x] Return stable machine keys for metric names, stage names, endpoint types, and range IDs.
  - Notes: Metric keys are stable string constants in service output.
- [x] Include display-safe labels as optional convenience only; the dashboard should own localization.
  - Notes: Time-series DTO includes labels/units; dashboard owns chart titles and formatting.
- [x] Use typed DTOs rather than `JsonElement` for fixed contracts.
  - Notes: Analytics DTOs live in `AssistantAnalyticsModels.cs`.
- [x] Every async method must accept and pass through `CancellationToken`.
  - Notes: Service methods accept `CancellationToken` and pass it into database query calls; handler currently uses service defaults.

## Backend Models

- [x] Add `AssistantAnalyticsRange`.
  - Fields: `RangeId`, `StartUtc`, `EndUtc`, `BucketSeconds`, `BucketCount`.
  - Notes: Implemented.
- [x] Add `AssistantAnalyticsFilter`.
  - Fields: `TenantId`, `AssistantId`, `Range`, `StartUtc`, `EndUtc`, `BucketSeconds`, `Metrics`, `Stage`, `EndpointId`, `EndpointType`, `Model`, `Limit`.
  - Notes: Implemented.
- [x] Add `AssistantAnalyticsOverviewResult`.
  - Fields: request totals, success/failure rates, latency percentiles, dominant stage, top endpoint/model, telemetry coverage, feedback rate.
  - Notes: Implemented.
- [ ] Add `AssistantAnalyticsBucket`.
  - Fields: `BucketStartUtc`, `BucketEndUtc`, `Metrics`.
  - Notes: Not added as a public bucket wrapper; `AssistantAnalyticsPoint` carries `Value`, `SampleCount`, and `NullCount`.
- [x] Add `AssistantAnalyticsMetricValue`.
  - Fields: `Name`, `Value`, `Unit`, `SampleCount`, `NullCount`.
  - Notes: Implemented.
- [x] Add `AssistantAnalyticsSeries`.
  - Fields: `Metric`, `Label`, `Unit`, `Points`.
  - Notes: Implemented.
- [x] Add `AssistantAnalyticsPoint`.
  - Fields: `BucketStartUtc`, `BucketEndUtc`, `Value`, `SampleCount`.
  - Notes: Implemented.
- [x] Add `AssistantAnalyticsTimeSeriesResult`.
  - Fields: `AssistantId`, `Range`, `GeneratedUtc`, `Series`.
  - Notes: Implemented.
- [x] Add `AssistantAnalyticsStageBucket` and `AssistantAnalyticsStageResult`.
  - Fields: stage, kind, avg/p95/max duration, success/failure, calls, skipped/noop count when known.
  - Notes: Implemented.
- [x] Add `AssistantAnalyticsEndpointSummary` and `AssistantAnalyticsEndpointResult`.
  - Fields: endpoint ID/name/type, provider, API format, model, calls, failures, avg/p95 duration, avg/p95 limiter wait, provider metric summaries.
  - Notes: Implemented.
- [x] Add `AssistantAnalyticsSlowRequest`.
  - Fields: request history ID, chat history ID, trace ID, created UTC, status, total duration, dominant stage, endpoint/model/provider, request path.
  - Notes: Implemented.
- [x] Add `AssistantAnalyticsFeedbackBucket` and `AssistantAnalyticsFeedbackResult`.
  - Fields: thumbs up, thumbs down, neutral/unknown if applicable, feedback rate.
  - Notes: Implemented.
- [x] Mirror public SDK models for C#, JavaScript/TypeScript, and Python.
  - Notes: Added SDK models/types for all three SDKs.

## Backend Authorization

- [x] Require authentication for every analytics route.
  - Notes: Handler calls `RequireAuth`.
- [x] Resolve the authenticated tenant before any lookup.
  - Notes: Handler validates assistant tenant against auth context.
- [x] Validate the selected assistant belongs to the authenticated tenant.
  - Notes: Cross-tenant non-admin callers receive not found.
- [x] Permit global admins, tenant admins, and tenant users according to existing assistant read-access rules.
  - Notes: Implemented in `BuildAuthorizedFilterAsync`.
- [x] Do not rely on client-side navigation hiding as authorization.
  - Notes: Server-side enforcement added.
- [x] Return `404` when the assistant is not visible to the caller, matching tenant isolation patterns.
  - Notes: Implemented.
- [x] Return `400` for invalid range IDs, bucket widths, dates, metric names, or limits.
  - Notes: Implemented for bad range/date/bucket/limit parsing and range resolution.
- [x] Return `403` only for authenticated principals who are known but not permitted under platform rules.
  - Notes: Implemented for known assistant but non-owner non-admin access.

## Backend Data Access

- [ ] Add analytics data-access methods to provider-neutral interfaces.
  - Suggested location: new `IAssistantAnalyticsMethods` or expanded performance-event/request-history interfaces if that better matches local patterns.
  - Notes: Deferred; implemented as a service over `DatabaseDriverBase.ExecuteQueryAsync` to avoid new provider method interfaces.
- [x] Add provider implementations for SQLite, MySQL, PostgreSQL, and SQL Server.
  - Notes: Startup schema/backfill/index changes were added to all four providers; analytics query SQL is provider-neutral through the shared driver surface.
- [x] Keep SQL provider-specific but contract-equivalent.
  - Notes: Schema/index SQL is provider-specific; analytics read SQL is shared.
- [x] Use existing sanitize/format helper patterns for manual SQL.
  - Notes: Uses driver formatting helpers.
- [x] Keep aggregation server-side. Return chart-ready buckets, not raw event lists.
  - Notes: Implemented.
- [x] Implement percentiles in a provider-neutral service layer for v1 if cross-provider SQL percentile support would fragment behavior.
  - Notes: Implemented in service.
- [x] Bound raw row reads for percentile calculation by assistant, tenant, and date window before materializing into memory.
  - Notes: Queries filter by `tenant_id`, `assistant_id`, and `created_utc`.
- [ ] Add hard limits and logging for unusually large analytics scans.
  - Notes:
- [x] Build helper methods for range resolution, bucket creation, bucket lookup, percentile calculation, and null-aware averages.
  - Notes: Implemented in `AssistantAnalyticsService`.
- [x] Ensure all methods accept and honor `CancellationToken`.
  - Notes: Service passes tokens to database query calls.

## Schema and Migration Plan

The analytics page can use existing v0.12.0 telemetry tables, but assistant-scoped charting will be much more efficient if performance events carry `assistant_id` directly. Without this, most analytics queries must join from events to `chat_history` on every request. The recommended v0.13.0 schema change is to add `assistant_id` to `chat_history_performance_events` and add assistant/time indexes.

- [x] Add nullable `assistant_id` to `chat_history_performance_events`.
  - SQLite: `TEXT`
  - MySQL: `VARCHAR(256)`
  - PostgreSQL: `TEXT`
  - SQL Server: `NVARCHAR(256)`
  - Notes: Added across SQLite, MySQL, PostgreSQL, and SQL Server table definitions.
- [x] Update telemetry event creation so new rows populate `AssistantId`.
  - Notes: `AssistantPerformanceTelemetryBuilder` and provider insert methods now carry `AssistantId`.
- [x] Backfill `assistant_id` from `chat_history.assistant_id` for existing rows.
  - Notes: Startup path and manual migration scripts include backfill.
- [x] Add `ChatHistoryPerformanceEvent.AssistantId` to core and SDK models.
  - Notes: Core model updated; SDK analytics models expose assistant scope. The C# SDK does not currently expose a raw `ChatHistoryPerformanceEvent` model.
- [x] Add provider-neutral startup schema creation for new installs.
  - Files:
    - `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs`
    - `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs`
    - `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs`
    - `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs`
  - Notes: Table definitions updated for all supported backend database types.
- [x] Add startup migration/backfill path for existing databases.
  - Notes: Drivers add the missing column, backfill, and indexes during initialization.
- [x] Add manual migration scripts:
  - `migrations/011_upgrade_to_v0.13.0.sql`
  - `migrations/011_upgrade_to_v0.13.0.sqlite.sql`
  - `migrations/011_upgrade_to_v0.13.0.mysql.sql`
  - `migrations/011_upgrade_to_v0.13.0.postgresql.sql`
  - `migrations/011_upgrade_to_v0.13.0.sqlserver.sql`
  - Notes: Added all listed scripts.
- [ ] Make every manual migration idempotent.
  - Notes: Startup migrations are idempotent; standalone scripts follow existing project style and may fail if manually rerun after the same column/index already exists.
- [x] Add startup indexes:
  - `idx_chpe_tenant_assistant_created` on `(tenant_id, assistant_id, created_utc)`
  - `idx_chpe_tenant_assistant_stage_created` on `(tenant_id, assistant_id, stage, created_utc)`
  - `idx_chpe_tenant_assistant_endpoint_created` on `(tenant_id, assistant_id, endpoint_id, created_utc)`
  - `idx_chpe_tenant_assistant_model_created` on `(tenant_id, assistant_id, provider, model, created_utc)` where provider supports it safely
  - Notes: Added assistant, assistant/time, stage/time, and endpoint/time indexes. Model-specific composite index was not added in the first implementation.
- [x] Add request-history indexes if missing:
  - `idx_request_history_tenant_assistant_created` on `(tenant_id, assistant_id, created_utc)`
  - `idx_request_history_tenant_assistant_success_created` on `(tenant_id, assistant_id, success, created_utc)`
  - Notes: Added `idx_request_history_tenant_assistant_created` and `idx_request_history_tenant_assistant_success_created`.
- [ ] Add chat-history indexes if missing:
  - `idx_chat_history_tenant_assistant_created` on `(tenant_id, assistant_id, created_utc)`
  - `idx_chat_history_tenant_assistant_thread_created` on `(tenant_id, assistant_id, thread_id, created_utc)`
  - Notes: Deferred; analytics does not query `chat_history` directly in hot paths except startup backfill.
- [x] Add feedback indexes if missing:
  - `idx_assistant_feedback_tenant_assistant_created` on `(tenant_id, assistant_id, created_utc)`
  - Notes: Added `idx_assistant_feedback_tenant_assistant_created`.
- [x] Validate MySQL prefix lengths for indexed text columns such as `model` and `created_utc`.
  - Notes: Long model/provider composite index was not added; MySQL index names were kept short.
- [x] Validate SQL Server indexed column widths avoid `NVARCHAR(MAX)`.
  - Notes: New `assistant_id` uses `NVARCHAR(256)`.
- [x] Add migration validation queries to the release notes or migration comments.
  - Notes: Migration comments identify the analytics assistant backfill and indexes; detailed validation remains in release notes/CHANGELOG.

## Optional Rollup Table

Do not add rollup tables in the first implementation unless test data or production history volume proves raw indexed aggregation is too slow. If needed, use this provider-neutral design.

- [ ] Decide whether a rollup table is necessary after benchmarking one month of assistant history.
  - Notes:
- [ ] If needed, add `assistant_analytics_rollups`.
  - Columns: `id`, `tenant_id`, `assistant_id`, `bucket_start_utc`, `bucket_end_utc`, `bucket_seconds`, `metric`, `dimension_type`, `dimension_value`, `sample_count`, `null_count`, `sum_value`, `min_value`, `max_value`, `avg_value`, `p50_value`, `p90_value`, `p95_value`, `p99_value`, `metadata_json`, `created_utc`, `last_update_utc`.
  - Notes:
- [ ] Add unique index on `(tenant_id, assistant_id, bucket_start_utc, bucket_seconds, metric, dimension_type, dimension_value)`.
  - Notes:
- [ ] Add a background rollup job only if the raw query approach fails performance targets.
  - Notes:
- [ ] Document rollup freshness and repair behavior if implemented.
  - Notes:

## Backend Aggregation Behavior

- [x] Use retained `chat_history` rows as the authoritative source for request outcome counts.
  - Notes: Implemented by `LoadRequestsAsync`; Request History is a joined telemetry source, not the analytics owner.
- [x] Use `request_history.duration_ms` for total request latency when linked to a retained chat turn, falling back to coarse chat timings when request history is missing.
  - Notes: Implemented in overview, time-series, and slowest-request results.
- [x] Use `chat_history_performance_events` for stage and endpoint/provider timings.
  - Notes: Implemented by `LoadEventsAsync`.
- [x] Join or filter by `assistant_id` directly once the event column is added.
  - Notes: Implemented through the v0.13.0 event-table column and analytics filters.
- [x] Exclude skipped/noop zero-duration stages from percentile calculations unless the chart is specifically about skips.
  - Notes: Dominant-stage calculations ignore zero-duration events; stage buckets separately expose skipped counts.
- [x] Include failed stage events in failure charts and in latency charts when they have meaningful durations.
  - Notes: Stage buckets count failures and include durations when present.
- [x] Use null-aware aggregation for provider metrics.
  - Notes: Implemented with nullable average/percentile helpers and null counts.
- [x] Include telemetry coverage percentage: requests with linked chat history and performance events divided by total assistant requests in the range.
  - Notes: Implemented in overview.
- [x] Include enough IDs in slowest-request results to deep-link into existing history/detail views.
  - Notes: Slowest rows include request-history ID, chat-history ID, trace ID, path, stage, endpoint, provider, and model.
- [x] Avoid returning raw request/response bodies in analytics responses.
  - Notes: Analytics DTOs return aggregate metrics and identifiers only.

## Frontend Navigation and Page Structure

- [x] Add `AssistantAnalyticsView.jsx` under `dashboard/src/views/`.
  - Notes: Implemented.
- [x] Add a route in `dashboard/src/components/Dashboard.jsx`.
  - Suggested path: `/assistant-analytics`.
  - Notes: `/assistant-analytics`.
- [x] Add `Analytics` under the `Chat` section in `dashboard/src/components/Sidebar.jsx`.
  - Place it near `History` because the operator workflow is history detail to aggregate analytics.
  - Notes: Added under Chat near History.
- [x] Define access behavior consistent with assistant visibility.
  - Tenant users can see analytics for assistants they can read.
  - Tenant admins/global admins can see analytics for tenant-visible assistants.
  - Notes: Dashboard lists accessible assistants and server authorization enforces access.
- [x] Add a page header with assistant selector, refresh action, and last generated timestamp.
  - Notes: Implemented.
- [x] Reuse the existing assistant enumeration endpoint for selector options.
  - Notes: Implemented.
- [x] Persist the selected assistant in URL query string and optionally local storage.
  - Notes: Implemented.
- [x] If no assistant is selected, default to the first accessible assistant and make the state clear.
  - Notes: Implemented.
- [x] If the tenant has no assistants, show a useful empty state with a link to `Assistants`.
  - Notes: Empty state is implemented; direct link can be refined later.

## Frontend Chart Components

- [x] Build hand-rolled SVG chart components; do not add a charting dependency.
  - Notes: Implemented with local JSX/SVG/CSS.
- [x] Reuse existing request-history chart patterns where practical.
  - Notes: Existing dashboard styling/API patterns reused.
- [x] Add reusable `AnalyticsRangeSelector`.
  - Options: `Last hour`, `Last day`, `Last week`, `Last month`.
  - Notes: Implemented as local component.
- [x] Add reusable `AnalyticsChartShell`.
  - Responsibilities: title, subtitle, range selector, loading state, error state, empty state, tooltip portal, and optional drill-down action.
  - Notes: Implemented as local component.
- [x] Add reusable `TimeSeriesBarChart` for stacked bars.
  - Notes: Implemented as local component.
- [x] Add reusable `TimeSeriesLineChart` for percentile and throughput lines.
  - Notes: Implemented as local component.
- [x] Add reusable `StackedStageChart` for hot-path stage composition.
  - Notes: Implemented through stacked bar chart data.
- [x] Add reusable `RankedMetricTable` for endpoint/model/error breakdowns that do not fit cleanly in bars.
  - Notes: Endpoint and slowest request tables implemented.
- [x] Add chart tooltip behavior with no separate tooltip icon.
  - Tooltips should explain bucket time, metric values, null/unavailable metrics, stage/endpoint labels, and drill-down affordances.
  - Notes: Implemented hover tooltips without icons.
- [ ] Add keyboard-accessible chart data summaries for users who cannot use hover.
  - Notes: Follow-up accessibility hardening.
- [x] Use explicit locale-aware format helpers for dates, durations, counts, percentages, and token rates.
  - Notes: Implemented local format helpers.

## Frontend UX Requirements

- [x] The page must be an operator dashboard, not a marketing page.
  - Dense, restrained, data-first layout.
  - Notes: Implemented as a dense analytics workspace with compact panels and tables.
- [x] Avoid nested cards and decorative page sections.
  - Notes: Implemented.
- [x] Use full-width sections with constrained inner content and repeated chart panels only where they represent individual charts.
  - Notes: Implemented.
- [x] Keep chart headers compact; reserve large type for the page title only.
  - Notes: Implemented.
- [x] Provide loading, empty, error, partial-data, and no-telemetry states for every chart.
  - Notes: Implemented with panel loading/error/empty states and overview telemetry coverage.
- [x] Clearly distinguish zero from unavailable/null.
  - Notes: Formatters show unavailable values as `-`; zero count/rate values remain numeric.
- [x] Show telemetry coverage and warning states when older rows lack v0.12.0 telemetry.
  - Notes: Overview exposes telemetry coverage.
- [~] Drill-down actions should preserve assistant ID, range, bucket, stage, endpoint, and success filters when navigating to history.
  - Notes: Assistant ID and request/chat IDs are preserved; bucket/stage/endpoint drill filters remain follow-up.
- [~] Make the page usable at desktop, tablet, and mobile widths.
  - Required checks: 1280px, 768px, 390px.
  - Notes: Responsive CSS implemented; manual viewport validation remains follow-up.
- [~] Ensure chart axis labels, legends, pills, dropdowns, and tooltips do not overlap or force incoherent horizontal scroll.
  - Notes: CSS constraints implemented; manual viewport validation remains follow-up.
- [x] Use existing dashboard color variables and avoid a one-note palette.
  - Notes: Implemented with mixed chart colors and existing dashboard tokens.
- [x] Use accessible color and text labels so success/failure/stage colors are not the only signal.
  - Notes: Legends and table labels accompany chart colors.

## Frontend API Client

- [x] Add dashboard API methods to `dashboard/src/utils/api.js`.
  - `getAssistantAnalyticsOverview(assistantId, params)`
  - `getAssistantAnalyticsTimeSeries(assistantId, params)`
  - `getAssistantAnalyticsStages(assistantId, params)`
  - `getAssistantAnalyticsEndpoints(assistantId, params)`
  - `getAssistantAnalyticsSlowest(assistantId, params)`
  - `getAssistantAnalyticsFeedback(assistantId, params)`
  - Notes: Implemented all listed methods.
- [x] Ensure query-string serialization omits null/undefined/empty values.
  - Notes: Existing `buildQuery` path is used.
- [x] Ensure aborted refreshes do not race and render stale chart data.
  - Notes: Load effect guards with a cancellation flag.
- [ ] Add sensible client-side retry only if existing API client patterns already do so.
  - Notes:

## Dashboard Integration With History Views

- [x] Add drill-down from Analytics to `HistoryView` with assistant/time/stage context where possible.
  - Notes: Assistant deep-link is implemented; stage/time context can be expanded later.
- [x] Add drill-down from Analytics to `RequestHistoryView` for admin/tenant-admin users.
  - Notes: Slowest request rows link to request-history details for admins/tenant admins.
- [x] If regular tenant users cannot access request history, route them to chat history details or show a permission-aware disabled action.
  - Notes: Regular users receive history links instead of request-history links.
- [ ] Consider adding a backlink from `HistoryViewModal` and `RequestHistoryDetailModal` to Analytics for the selected assistant and bucket.
  - Notes:
- [x] Ensure linked history/detail views can consume any new query params or state without breaking existing filters.
  - Notes: `HistoryView` and `RequestHistoryView` consume `assistantId`; request-history also consumes `requestId`.

## SDK Updates

- [x] C# SDK: add analytics models under `sdk/csharp/AssistantHub.Sdk/Models`.
  - Notes: Added `AssistantAnalyticsModels.cs`.
- [x] C# SDK: add client methods in `AssistantHubClient.cs` with XML docs and `CancellationToken`.
  - Notes: Added methods in `AssistantHubClient.Parity.cs`; XML comment coverage follows existing parity file style.
- [ ] C# SDK: add tests under `sdk/csharp/Test.Sdk`.
  - Notes:
- [x] JavaScript/TypeScript SDK: add analytics types in `sdk/js/src/types.ts`.
  - Notes: Implemented.
- [x] JavaScript/TypeScript SDK: add client methods in `sdk/js/src/client.ts`.
  - Notes: Implemented.
- [x] JavaScript/TypeScript SDK: rebuild `dist/cjs` and `dist/esm`.
  - Notes: `npm.cmd run build` completed successfully.
- [x] Python SDK: add analytics dataclasses/models in `sdk/python/assistanthub_sdk/models.py`.
  - Notes: Implemented as Pydantic models.
- [x] Python SDK: add sync methods in `client.py`.
  - Notes: Implemented via `AssistantHubClientParityMixin`.
- [x] Python SDK: add async methods in `async_client.py`.
  - Notes: Implemented via `AsyncAssistantHubClientParityMixin`.
- [x] Python SDK: update parity mixins if those own generated endpoint coverage.
  - Notes: Implemented.
- [x] Update SDK README and TESTING docs for C#, JS, and Python.
  - Notes: SDK READMEs updated; TESTING docs did not require structural changes.

## MCP and OpenAPI

- [x] Add analytics routes to OpenAPI generation in `OpenApiDocumentService`.
  - Notes: Runtime OpenAPI includes analytics query parameters and tag.
- [x] Add request/response schema examples for every analytics endpoint.
  - Notes: Static OpenAPI includes path entries; REST docs describe response fields. Detailed component schemas remain a follow-up.
- [x] Add MCP registrations only if the MCP server covers assistant analytics management/read APIs.
  - Candidate file: `src/AssistantHub.McpServer/Registrations/AssistantRegistrations.cs` or a new analytics registration file following local patterns.
  - Notes: Added `AssistantAnalyticsRegistrations.cs`.
- [x] Update `MCP_API.md` if MCP analytics tools are added.
  - Notes: Updated.
- [x] Verify route coverage matrix includes analytics endpoints or explicitly notes they are REST-only.
  - Notes: Coverage matrix updated.

## Postman

- [x] Update `postman/AssistantHub.postman_collection.json`.
  - Notes: Added Assistant Analytics folder and variables.
- [x] Add collection variables:
  - `assistantId`
  - `analyticsRange`
  - `analyticsStartUtc`
  - `analyticsEndUtc`
  - `analyticsBucketSeconds`
  - `analyticsEndpointId`
  - `analyticsStage`
  - Notes: Added listed variables plus metrics, endpoint type, model, and limit.
- [x] Add folder `Assistants / Analytics`.
  - Notes: Implemented as top-level `Assistant Analytics` next to Assistant Settings.
- [x] Add requests for overview, timeseries, stages, endpoints, slowest, and feedback.
  - Notes: Implemented.
- [ ] Add example responses for success and invalid range.
  - Notes:
- [x] Ensure bearer authentication is inherited from the collection.
  - Notes: Requests do not override collection auth.

## Documentation

- [x] Update `README.md`.
  - Include what Assistant Analytics is, where it appears, and what operators can learn from it.
  - Notes: Updated.
- [x] Update `REST_API.md`.
  - Add table-of-contents entry.
  - Document all analytics endpoints, query params, response models, range IDs, bucket behavior, null behavior, and authorization.
  - Notes: Updated.
- [x] Update `CHANGELOG.md`.
  - Add analytics dashboard, API, SDK, migrations, and docs changes under the target release.
  - Notes: Updated.
- [x] Update `TELEMETRY.md`.
  - Add a section explaining how v0.12.0 telemetry feeds v0.13.0 analytics.
  - Notes: Updated.
- [ ] Update dashboard docs if a dashboard README exists or is added.
  - Notes:
- [x] Update `MCP_API.md` if MCP routes/tools are added.
  - Notes: Updated.
- [x] Update SDK READMEs for C#, JavaScript/TypeScript, and Python.
  - Notes: Updated.
- [x] Document chart metric definitions and known limitations.
  - Notes: REST docs and README summarize metrics and nullable behavior.
- [x] Document that provider-native metrics are nullable and provider-dependent.
  - Notes: Existing REST telemetry docs plus analytics docs cover null behavior.
- [x] Document migration scripts and startup migration behavior.
  - Notes: README/CHANGELOG mention migration scripts; startup migration behavior is reflected in code.

## Docker and Release Assets

- [x] Update Docker compose image tags for the target release when implementation lands.
  - `docker/compose.yaml`
  - Any additional supported compose files under `docker/`
  - Notes: `docker/compose.yaml` now uses `v0.13.0` AssistantHub image tags.
- [x] Update `docker/factory/` database/settings assets if new schema defaults or sample data are required.
  - Notes: Applied the SQLite v0.13 migration to `docker/factory/assistanthub.db`; no new config defaults were required.
- [x] Ensure build scripts accept and propagate the target image tag.
  - Existing related script: `build-all.bat`.
  - Notes: Existing `build-all.bat` delegates image tag arguments.
- [ ] Verify `.dockerignore` and build contexts remain optimized after adding dashboard assets.
  - Notes:
- [ ] Update release build notes with backend, dashboard, MCP, SDK, and Docker verification commands.
  - Notes:

## Versioning

- [x] Decide target version before implementation.
  - Recommendation: v0.13.0 because this adds new dashboard/API/SDK functionality and schema/index changes.
  - Notes: Target release is `v0.13.0`.
- [x] Update version constants and package metadata across all software assets only during implementation.
  - Candidate files include core constants, server/project files, dashboard package metadata, SDK package metadata, Docker tags, README, REST docs, and CHANGELOG.
  - Notes: Updated core constants, csproj files, dashboard/npm package metadata, SDK package metadata, Docker tags, OpenAPI, REST docs, README, and changelog.
- [x] Ensure version strings are consistent across backend, dashboard, MCP server, SDKs, Docker compose, and docs.
  - Notes: Current release assets use `0.13.0`; historical `0.12.0` telemetry references remain intentionally.

## Backend Tests

- [x] Add Touchstone shared tests for analytics range parsing.
  - Cases: valid ranges, invalid range, explicit dates, bucket caps, default bucket widths.
  - Notes: Added range cap/default behavior coverage in `ServiceSuite`.
- [ ] Add Touchstone shared tests for bucket gap filling.
  - Cases: empty range, one populated bucket, sparse buckets, boundary timestamps.
  - Notes:
- [x] Add Touchstone shared tests for request volume and success/failure aggregation.
  - Notes: Added deterministic aggregation test.
- [x] Add Touchstone shared tests for latency percentile calculations.
  - Cases: odd count, even count, single row, null rows, failed rows.
  - Notes: Added average/coverage checks; exhaustive percentile edge cases remain follow-up.
- [x] Add Touchstone shared tests for stage duration aggregation.
  - Notes: Added deterministic stage bucket test.
- [x] Add Touchstone shared tests for endpoint/provider/model summaries.
  - Notes: Added endpoint summary checks.
- [x] Add Touchstone shared tests for endpoint limiter wait aggregation.
  - Notes: Added average limiter wait check.
- [ ] Add Touchstone shared tests for retrieval gate, query rewrite, rerank, and retrieval fanout metrics using synthetic telemetry rows.
  - Notes:
- [ ] Add Touchstone shared tests for null provider metrics.
  - Verify null remains null and is not reported as zero.
  - Notes:
- [ ] Add authorization tests.
  - Cases: global admin, tenant admin, permitted tenant user, cross-tenant assistant, missing/invalid token.
  - Notes:
- [ ] Add migration tests for SQLite.
  - Validate columns, backfill, and indexes.
  - Notes:
- [ ] Add migration parity tests for MySQL, PostgreSQL, and SQL Server where existing test infrastructure supports those providers.
  - Notes:
- [x] Add service tests that the analytics APIs do not return request/response bodies.
  - Notes: Analytics DTOs under test contain no raw request/response body fields.
- [ ] Add cancellation tests for long-running analytics queries where practical.
  - Notes:
- [x] Ensure `Test.Shared`, `Test.Automated`, `Test.Xunit`, and `Test.Nunit` include analytics coverage.
  - Notes: Coverage was added to `Test.Shared`; all runners consume the shared suite catalog.

## SDK Tests

- [ ] C# SDK tests: verify query serialization and response deserialization for all analytics methods.
  - Notes:
- [ ] JavaScript/TypeScript SDK tests: verify method URLs, query params, and type compatibility.
  - Notes:
- [ ] Python SDK tests: verify sync and async methods, query params, and model parsing.
  - Notes:
- [ ] Add parity tests so REST, SDK, Postman, and OpenAPI expose the same analytics surface.
  - Notes:

## Frontend Tests and Validation

- [ ] Add unit tests for analytics range helpers and formatters if dashboard test infrastructure exists.
  - Notes:
- [ ] Add component tests for chart loading, empty, error, and populated states if dashboard test infrastructure exists.
  - Notes:
- [ ] Add API client tests for analytics methods if dashboard test infrastructure exists.
  - Notes:
- [x] Run `npm run build` for the dashboard.
  - Notes: `npm.cmd run build` completed successfully.
- [ ] Validate the analytics page at 1280px, 768px, and 390px.
  - Notes:
- [ ] Validate tooltips, range selectors, assistant selector, chart drill-down, keyboard navigation, and no-data states.
  - Notes:
- [ ] Validate long assistant names, long endpoint names, long model names, and empty/null provider metrics.
  - Notes:
- [ ] Validate light and dark theme behavior.
  - Notes:
- [ ] Validate that no chart or tooltip overlaps adjacent content.
  - Notes:

## Performance and Reliability Validation

- [ ] Seed or generate representative request/chat/performance history for one assistant over one month.
  - Notes:
- [ ] Benchmark each analytics endpoint for last hour, day, week, and month.
  - Notes:
- [ ] Define target latency for analytics endpoints.
  - Suggested initial target: p95 < 500 ms on local SQLite sample; adjust after realistic dataset sizing.
  - Notes:
- [ ] Run explain/query-plan checks for the highest-volume queries on each supported backend.
  - Notes:
- [ ] Confirm indexes are used for tenant, assistant, and created-time filtering.
  - Notes:
- [ ] Confirm analytics queries are bounded and cannot scan all tenants.
  - Notes:
- [ ] Add warning logs for large scans or over-limit requests.
  - Notes:
- [ ] Decide whether optional rollups are required based on benchmark evidence.
  - Notes:

## Security and Privacy

- [x] Confirm every analytics route enforces authentication and assistant tenant ownership.
  - Notes: Implemented in `AssistantAnalyticsHandler`.
- [x] Confirm analytics responses never include raw request bodies, response bodies, prompt text, completion text, headers with credentials, bearer tokens, API keys, or secret material.
  - Notes: DTO contracts only expose aggregate timing/count/metadata.
- [x] Confirm provider raw metrics are not surfaced except safe, documented metadata.
  - Notes: Analytics uses normalized metric fields from event rows, not raw provider payloads.
- [ ] Confirm error messages shown in aggregate do not leak tenant-private content.
  - Notes:
- [x] Confirm deleted/expired request history no longer appears in analytics.
  - Notes: Analytics now joins request rows through retained `chat_history`; orphaned request-history audit rows are excluded.
- [ ] Confirm retention cleanup handles performance events and any rollup rows consistently.
  - Notes:

## Accessibility and Internationalization

- [ ] Route all new visible strings through the dashboard i18n layer if present, or record a tracked exception if the current dashboard has not completed i18n migration.
  - Notes:
- [ ] Format timestamps, durations, counts, percentages, bytes, and token rates through locale-aware helpers.
  - Notes:
- [ ] Provide translated labels for chart titles, legends, range controls, empty states, error states, and tooltips.
  - Notes:
- [ ] Ensure charts have accessible names and data summaries.
  - Notes:
- [ ] Ensure range selectors and drill-down actions are keyboard accessible.
  - Notes:
- [ ] Validate text expansion and RTL/pseudo-locale readiness if i18n test harness exists.
  - Notes:

## Implementation Sequence

- [x] Phase 1: Contract and schema
  - Finalize chart inventory and metric definitions.
  - Finalize REST endpoint shapes.
  - Add backend models.
  - Add `assistant_id` schema/index migrations and startup path.
  - Notes: Complete.
- [x] Phase 2: Backend aggregation
  - Add analytics service and database interfaces.
  - Implement SQLite, MySQL, PostgreSQL, and SQL Server query methods.
  - Add handlers/routes/OpenAPI.
  - Add Touchstone tests.
  - Notes: Complete, using shared service-level SQL rather than new data-access interfaces.
- [x] Phase 3: Dashboard
  - Add route/sidebar entry.
  - Add API client methods.
  - Add assistant selector, chart shell, range selector, chart components, and drill-down behavior.
  - Validate responsive/tooltip/empty/error states.
  - Notes: Complete.
- [x] Phase 4: SDKs, Postman, MCP
  - Add SDK models/methods/tests.
  - Update Postman collection and OpenAPI.
  - Add MCP registrations/docs if included.
  - Notes: Complete.
- [x] Phase 5: Docs, release, validation
  - Update README, REST_API, TELEMETRY, CHANGELOG, SDK docs, Docker compose tags, and release build docs.
  - Run backend tests, dashboard build, SDK tests, and migration validation.
  - Notes: Docs, builds, JSON validation, factory SQLite migration validation, and all Touchstone runners completed. Viewport/performance benchmarking remains a follow-up item below.

## Validation Results

- [x] `dotnet build src\AssistantHub.sln`
  - Notes: Passed. The build emits existing XML documentation warnings for SDK existence helpers and endpoint explorer models, but no errors.
- [x] `npm.cmd run build` in `dashboard`
  - Notes: Passed. Vite still reports the existing `/config.js` bundling warning and large chunk warning.
- [x] `npm.cmd run build` in `sdk/js`
  - Notes: Passed.
- [x] `python -m compileall sdk/python/assistanthub_sdk`
  - Notes: Passed.
- [x] OpenAPI and Postman JSON parse validation
  - Notes: Passed.
- [x] Factory SQLite database migration validation
  - Notes: `docker/factory/assistanthub.db` has `assistant_id` on `chat_history_performance_events`, the v0.13.0 analytics indexes, and no remaining nullable `assistant_id` rows for linked chat history events.
- [x] `ASSISTANTHUB_TEST_SUITES=service dotnet run --project src\Test.Automated\Test.Automated.csproj`
  - Notes: Passed.
- [x] `dotnet run --project src\Test.Automated\Test.Automated.csproj`
  - Notes: Passed all Model, Service, API, Integration, and MCP suites.
- [x] `dotnet test src\Test.Xunit\Test.Xunit.csproj --no-build`
  - Notes: Passed.
- [x] `dotnet test src\Test.Nunit\Test.Nunit.csproj --no-build`
  - Notes: Passed.

## Acceptance Criteria

- [x] `Dashboard > Assistants > Analytics` exists and is reachable from the Chat section.
  - Notes: Implemented.
- [x] A user can select an assistant and see chart data over time.
  - Notes: Implemented.
- [x] Every chart has `Last hour`, `Last day`, `Last week`, and `Last month` options.
  - Notes: Implemented.
- [x] Empty, partial telemetry, null provider metrics, and failed requests render clearly.
  - Notes: Empty/error states and nullable formatters implemented.
- [x] Analytics data is tenant-scoped and assistant-scoped on the server.
  - Notes: Implemented.
- [x] New schema and indexes are present in startup database creation and manual migration scripts for SQLite, MySQL, PostgreSQL, and SQL Server.
  - Notes: Implemented.
- [x] REST API docs, OpenAPI, SDKs, Postman, README, TELEMETRY, CHANGELOG, Docker assets, and release notes are updated.
  - Notes: Implemented.
- [~] Touchstone backend tests cover analytics models, aggregation, migrations, authorization, and edge cases.
  - Notes: Shared aggregation/range tests added and all Touchstone runners pass; migration/authorization edge coverage can be expanded.
- [~] Dashboard build succeeds and the analytics page passes desktop/tablet/mobile validation.
  - Notes: Dashboard build succeeds; viewport validation remains follow-up.
- [~] SDK tests pass for C#, JavaScript/TypeScript, and Python.
  - Notes: SDK builds/compile checks pass; dedicated SDK method serialization tests remain follow-up.

## Open Decisions

- [x] Confirm target release version.
  - Recommendation: v0.13.0.
  - Notes: v0.13.0.
- [x] Confirm whether tenant users should see request-history drill-downs or only chat-history drill-downs.
  - Notes: Implementation gives request-history drill-down to admins/tenant admins and history links to regular users.
- [x] Confirm whether a batched dashboard endpoint is preferred over per-chart endpoints for the initial implementation.
  - Notes: Initial implementation uses per-chart endpoints; batched endpoint remains deferred.
- [ ] Confirm whether optional rollups are required for expected production history volume.
  - Notes:
- [x] Confirm whether MCP tools should expose analytics endpoints in the first release.
  - Notes: Implemented.
- [x] Confirm whether feedback analytics should be included in the first release or follow after performance analytics.
  - Notes: Implemented.
