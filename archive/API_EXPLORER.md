# API Explorer + Request History Integration Plan

This document describes the end-to-end work needed to bring the request-history and API-explorer capabilities exposed in `C:\Code\lattice`, `C:\Code\verbex`, and `C:\Code\litegraph\litegraph` into AssistantHub.

It is written to be used as a working implementation checklist. Developers should mark items complete, add links to PRs, and annotate decisions inline.

## Objective

Build two operator capabilities inside AssistantHub:

- A system API explorer for AssistantHub management and operational APIs.
- An assistant API explorer for calling assistants themselves, including public assistant APIs, thread lifecycle, chat streaming, compaction, generation, feedback, and assistant-specific helper calls.

Build one shared foundation behind both:

- A first-class HTTP request-history subsystem with searchable request/response metadata, detail views, retention, redaction, and summary reporting.

## Current State

- AssistantHub does not currently expose live OpenAPI metadata from the server. Route registration in `src/AssistantHub.Server/AssistantHubServer.cs` is manual and does not use `UseOpenApi()`.
- AssistantHub does not currently persist generic HTTP request history. `PostRouting` only writes log lines.
- AssistantHub already has chat-turn history in `src/AssistantHub.Core/Models/ChatHistory.cs` and `src/AssistantHub.Server/Handlers/HistoryHandler.cs`, but that is conversation telemetry, not generic HTTP/API request history.
- AssistantHub already has narrow endpoint-testing capability for Partio-backed embedding and completion endpoints through:
  - `src/AssistantHub.Server/Handlers/CompletionEndpointHandler.cs`
  - `src/AssistantHub.Server/Handlers/EmbeddingEndpointHandler.cs`
  - `dashboard/src/components/modals/InferenceEndpointTestModal.jsx`
  - `dashboard/src/components/modals/EmbeddingEndpointTestModal.jsx`
- The dashboard currently has no API Explorer page, no Request History page for HTTP/API traffic, and no raw request execution path in `dashboard/src/utils/api.js`.
- There is a committed `openapi.json` file at repo root, but it is not obviously authoritative relative to the current registered route surface.

## Reference Patterns To Reuse

- Lattice:
  - OpenAPI-driven explorer UI.
  - Request-history filters, summaries, detail view, and local explorer request replay history.
  - Central request capture with path exclusions and redaction.
- Verbex:
  - `UseOpenApi()`-based server exposure.
  - Clean request-history route surface.
  - Metadata/detail split for request-history persistence.
- LiteGraph:
  - Stronger explorer UX, grouped operations, request templates, code snippets, and recent requests.
  - Request-history chart and detail modal.
  - Good pattern for assistant-specific starter templates.

## Recommended Decisions

- Recommendation: treat system API explorer and assistant API explorer as two modes over one shared request-execution engine.
- Recommendation: add a new generic request-history subsystem instead of expanding `ChatHistory`.
- Recommendation: expose live `/openapi.json` from the running server and keep the committed `openapi.json` only as a generated artifact, not as the source of truth.
- Recommendation: keep request-history persistence split between entry metadata and entry detail to avoid making list queries heavy.
- Recommendation: reuse the existing endpoint-test flows as specialized explorer presets, then retire the separate modal-only implementation once parity is reached.

## Scope

In scope:

- Live OpenAPI exposure for AssistantHub system APIs.
- Dashboard Request History page for HTTP/API traffic.
- Dashboard API Explorer page for system APIs.
- Dashboard assistant API explorer flows for:
  - `GET /v1.0/assistants/{assistantId}/public`
  - `POST /v1.0/assistants/{assistantId}/threads`
  - `GET /v1.0/assistants/{assistantId}/threads/{threadId}/history`
  - `POST /v1.0/assistants/{assistantId}/chat`
  - `POST /v1.0/assistants/{assistantId}/compact`
  - `POST /v1.0/assistants/{assistantId}/generate`
  - `POST /v1.0/assistants/{assistantId}/feedback`
  - Assistant document download and distinct labels/tags helpers where useful
- Searchable request history for both management APIs and assistant-facing APIs.
- Retention, redaction, truncation, and access control.
- Migration of current endpoint test modals into the broader explorer experience.

Out of scope for the first delivery:

- General-purpose arbitrary outbound API testing to non-AssistantHub targets.
- Persisting full streaming event payloads token-by-token.
- Full API consumer portal or external developer documentation site.
- Replacing existing chat history UX.

## Assumptions And Decision Gates

These are not blocking for plan creation, but they must be resolved during implementation.

- [ ] Decision: system request history visibility.
  Recommendation: global admins can view all tenants, tenant admins are limited to their tenant, non-admin assistant owners do not get generic system API request-history access.
- [ ] Decision: assistant API explorer visibility.
  Recommendation: global admins and tenant admins get it first; assistant-owner access can be added later if needed.
- [ ] Decision: assistant request-history visibility.
  Recommendation: global admins and tenant admins get tenant-scoped access first; if assistant owners need it, limit them to assistants they own.
- [ ] Decision: anonymous public assistant requests captured in history.
  Recommendation: yes, but tag them as unauthenticated/public and redact aggressively.
- [ ] Decision: committed `openapi.json` workflow.
  Recommendation: generate it from the live route metadata in CI or a local maintenance script after the runtime endpoint is trustworthy.

## Target Architecture

### 1. Shared Request-History Backbone

- New generic request-history models and database methods under `src/AssistantHub.Core`.
- New request-history settings under `AssistantHubSettings`.
- Central request capture in server request pipeline, not per-handler ad hoc logging.
- Request capture must support:
  - tenant ID
  - user ID
  - credential ID
  - auth mode
  - route path template when known
  - raw path
  - method
  - query string
  - request headers snapshot with redaction
  - request body with truncation/redaction
  - response headers snapshot with redaction
  - response body with truncation/redaction
  - status code
  - duration
  - correlation or trace ID if available
  - assistant ID and thread ID when present
  - source classification such as `dashboard`, `api`, `public-assistant`, `internal-proxy`

### 2. Live OpenAPI Surface

- AssistantHub server should emit a truthful runtime OpenAPI document that describes the registered routes, auth expectations, tags, parameters, and bodies.
- The system API explorer should consume the live document, not a stale file.
- Assistant-facing APIs may need either:
  - inclusion in the same spec with a distinct tag group and security metadata, or
  - a companion assistant-focused derived model in the dashboard for flows that need richer execution behavior than standard OpenAPI can describe.

### 3. Dashboard Explorer Shell

- One new dashboard page for Request History.
- One new dashboard page for API Explorer.
- One shared request executor in `dashboard/src/utils/api.js` for:
  - arbitrary method/path/query/header/body execution
  - auth inheritance from dashboard login
  - optional unauthenticated public assistant execution
  - SSE handling for assistant chat and eval streams
  - response header inspection
  - download-safe handling for binary responses

### 4. Assistant-Specific Explorer Layer

- Build assistant operation templates on top of the shared explorer engine.
- Reuse the existing `ChatPanel` fetch and SSE patterns rather than building separate streaming logic from scratch.
- Support:
  - assistant selection
  - public assistant discovery
  - thread creation and reuse
  - `X-Thread-ID`
  - metadata filters
  - streaming transcript display
  - compact and generate shortcuts
  - feedback submission
  - code snippets for common assistant call patterns

## Implementation Plan

## Phase 0: Truthful Spec And Integration Design

- [ ] Inventory the actual registered route surface in `src/AssistantHub.Server/AssistantHubServer.cs`.
- [ ] Compare the actual route surface to the committed `openapi.json` and document mismatches.
- [ ] Decide whether to adopt Watson `UseOpenApi()` directly for all routes or create a thin route-registration wrapper that stores both Watson handlers and OpenAPI metadata.
- [ ] Define route tags for the future explorer.
  Recommended initial tags: `Authentication`, `Tenants`, `Users`, `Credentials`, `Buckets`, `Bucket Objects`, `Collections`, `Collection Records`, `Assistants`, `Assistant Settings`, `Assistant Public APIs`, `Ingestion Rules`, `Documents`, `Feedback`, `Chat History`, `Request History`, `Models`, `Configuration`, `Crawlers`, `Evaluation`, `Endpoints`.
- [ ] Decide which routes are intentionally excluded from request history.
  Recommendation: exclude request-history endpoints themselves and internal health/noise paths if later added.
- [ ] Decide which routes require special response handling in the explorer.
  Minimum list: assistant chat SSE, eval run stream, binary downloads, file upload, object upload.
- [ ] Write down the data-retention and redaction defaults for request history before implementation starts.

Acceptance criteria:

- [ ] There is a single written route inventory and mismatch list.
- [ ] The team has chosen the OpenAPI metadata strategy.
- [ ] The team has chosen the request-history visibility model.

## Phase 1: Backend Request-History Domain Model

- [ ] Add request-history constants to `src/AssistantHub.Core/Constants.cs`.
- [ ] Add new models under `src/AssistantHub.Core/Models`:
  - `RequestHistoryEntry`
  - `RequestHistoryDetail`
  - `RequestHistorySearchFilter`
  - `RequestHistorySearchResult`
  - `RequestHistorySummaryResult`
  - `RequestHistorySummaryBucket`
- [ ] Add new interfaces under `src/AssistantHub.Core/Database/Interfaces`.
- [ ] Extend `src/AssistantHub.Core/Database/DatabaseDriverBase.cs` with request-history accessors.
- [ ] Implement request-history methods for every supported database provider already used by AssistantHub.
  Minimum: SQLite, PostgreSQL, MySQL.
- [ ] Choose the storage model.
  Recommendation: metadata table plus detail table, following the Verbex pattern.
- [ ] Add indexes for:
  - created time
  - tenant ID
  - user ID
  - status code
  - success
  - path
  - assistant ID
  - thread ID
- [ ] Add request-history settings under `src/AssistantHub.Core/Settings`, for example:
  - `Enabled`
  - `RetentionDays`
  - `MaxRequestBodyBytes`
  - `MaxResponseBodyBytes`
  - `CaptureHeaders`
  - `CaptureBodies`
  - `RedactedHeaders`
  - `RedactedJsonFields`
  - `IncludeUnauthenticatedAssistantTraffic`
- [ ] Wire the new settings into `AssistantHubSettings`.

Acceptance criteria:

- [ ] The database layer can create, read, search, summarize, delete, and bulk-delete request-history entries.
- [ ] The subsystem has tenant-aware filtering and retention support.
- [ ] The subsystem can store assistant IDs and thread IDs when available.

## Phase 2: Backend Request Capture In The HTTP Pipeline

- [ ] Implement a request-history service in `src/AssistantHub.Server/Services`.
- [ ] Move request capture to the server request pipeline rather than individual handlers.
- [ ] Replace the current `PostRouting` log-only behavior in `src/AssistantHub.Server/AssistantHubServer.cs` with:
  - existing log line emission
  - asynchronous request-history persistence
- [ ] Add robust capture helpers for:
  - request body extraction
  - response body capture
  - header redaction
  - JSON-field redaction
  - truncation markers
  - exclusion rules
- [ ] Ensure authenticated requests use `AuthContext` from request metadata.
- [ ] Ensure public assistant requests still capture:
  - assistant ID
  - thread ID
  - source IP if already available
  - public/unauthenticated classification
- [ ] Add path-template classification if route metadata is available.
- [ ] Add exclusions for request-history endpoints to avoid recursion/noise.
- [ ] Add cleanup scheduling, similar to existing chat-history cleanup, but isolated in its own lifecycle path.

Acceptance criteria:

- [ ] Every eligible AssistantHub HTTP request generates a searchable history entry.
- [ ] Sensitive headers and body fields are redacted.
- [ ] Large bodies are truncated safely.
- [ ] Disabled request-history mode returns empty or unavailable behavior without breaking callers.

## Phase 3: Request-History API Surface

- [ ] Add new request-history handler(s) under `src/AssistantHub.Server/Handlers`.
- [ ] Add routes for:
  - `GET /v1.0/requesthistory`
  - `GET /v1.0/requesthistory/summary`
  - `GET /v1.0/requesthistory/{requestId}`
  - `GET /v1.0/requesthistory/{requestId}/detail`
  - `DELETE /v1.0/requesthistory/{requestId}`
  - `DELETE /v1.0/requesthistory/bulk`
- [ ] Match the behavior expected in the `C:\Code\claude` backend guidelines:
  - empty or disabled-safe behavior
  - retention-respecting access
  - tenant-aware filtering
  - detail endpoint separation
- [ ] Support filters for:
  - start/end time
  - method
  - path
  - status code
  - success
  - tenant ID
  - user ID
  - assistant ID
  - thread ID
  - text search
- [ ] Enforce authorization correctly for global admins, tenant admins, and any later assistant-owner scope.

Acceptance criteria:

- [ ] The server exposes a full request-history route surface.
- [ ] List queries are fast and do not require loading full request/response bodies.
- [ ] Detail queries return the redacted/truncated request and response payloads.

## Phase 4: Live OpenAPI Exposure

- [ ] Introduce live OpenAPI generation on the AssistantHub server.
- [ ] Add route metadata for all registered routes.
- [ ] Mark auth expectations in the spec:
  - unauthenticated public assistant routes
  - bearer-token protected routes
  - admin-only routes
  - tenant-admin routes where relevant
- [ ] Ensure request/response schemas are emitted for:
  - current management APIs
  - request-history routes
  - assistant public APIs
  - endpoint test routes while they still exist
- [ ] Expose `/openapi.json` from the running server.
- [ ] Decide whether Swagger UI is enabled in dev only, or omitted entirely from product scope.
- [ ] Add a script or CI step to regenerate the committed `openapi.json` from the live server once the runtime spec is correct.

Acceptance criteria:

- [ ] The runtime `/openapi.json` matches the actual registered route surface.
- [ ] The dashboard can depend on the runtime spec.
- [ ] The committed spec, if retained, is generated rather than hand-maintained.

## Phase 5: Dashboard Request History Page

- [ ] Add a new dashboard page under `dashboard/src/views` or a new page structure consistent with the current repo.
- [ ] Add navigation entry in `dashboard/src/components/Sidebar.jsx`.
- [ ] Add route wiring in `dashboard/src/components/Dashboard.jsx`.
- [ ] Extend `dashboard/src/utils/api.js` with request-history client methods.
- [ ] Build the request-history list experience with:
  - filters
  - summary counts
  - success/error indicators
  - method and path columns
  - tenant/assistant/thread filters as appropriate
  - delete and bulk-delete actions
- [ ] Build a detail modal with:
  - request metadata
  - response metadata
  - redacted headers
  - request body
  - response body
  - copy helpers
- [ ] Add a summary chart modeled after the reference repos.
- [ ] Persist useful UI state where it helps operator workflows.

Acceptance criteria:

- [ ] Operators can search request history and inspect full details from the dashboard.
- [ ] The page scales to large history tables without loading request bodies in the grid response.
- [ ] Error traffic is easy to isolate.

## Phase 6: Dashboard System API Explorer

- [ ] Add an API Explorer page and navigation entry.
- [ ] Extend `dashboard/src/utils/api.js` with a shared raw executor that supports:
  - arbitrary method
  - path
  - query params
  - headers
  - JSON body
  - multipart/file uploads where needed
  - binary/download-safe behavior
  - response headers and body inspection
- [ ] Add client methods to load the live OpenAPI document.
- [ ] Build an OpenAPI flattener and grouped operation model for the dashboard.
- [ ] Build request editors from the spec:
  - path params
  - query params
  - headers
  - body template
- [ ] Add response rendering with:
  - status code
  - elapsed time
  - response headers
  - JSON pretty-print
  - raw text fallback
- [ ] Add code snippet generation.
- [ ] Add local recent-request history.
- [ ] Add destructive-action confirmation prompts.

Acceptance criteria:

- [ ] A logged-in operator can browse AssistantHub APIs from the live spec and execute them from the dashboard.
- [ ] The explorer inherits dashboard auth and shows accurate results.
- [ ] Recent requests and code snippets improve repeatability.

## Phase 7: Assistant API Explorer

- [ ] Add an assistant mode to the explorer or a dedicated assistant explorer page.
  Recommendation: keep it in the main explorer with a top-level mode switch.
- [ ] Build assistant operation templates for the public assistant endpoints.
- [ ] Support assistant selection from current tenant data.
- [ ] Support thread lifecycle:
  - create thread
  - paste existing thread ID
  - store recent thread IDs per assistant locally
- [ ] Support `POST /chat` with both non-streaming and SSE streaming display.
- [ ] Reuse the streaming logic patterns already present in `dashboard/src/components/ChatPanel.jsx`.
- [ ] Support metadata-filter editing for assistant chat requests.
- [ ] Support `POST /compact`, `POST /generate`, and feedback submission.
- [ ] Support helper operations for labels/tags distinct endpoints.
- [ ] Surface request-history correlation where possible.
  Example: if assistant chat or proxy calls later emit a request-history ID, show it in the explorer response panel.
- [ ] Add assistant-specific code snippets for:
  - thread creation
  - chat with `X-Thread-ID`
  - compact
  - generate
  - feedback

Acceptance criteria:

- [ ] Operators can exercise assistant APIs end-to-end without leaving the dashboard.
- [ ] Streaming assistant chat is inspectable in real time.
- [ ] Assistant-specific request setup is materially easier than using the generic system explorer alone.

## Phase 8: Absorb Existing Endpoint Test Modals

- [ ] Decide whether the current embedding and inference endpoint test modals remain temporarily as shortcuts.
- [ ] Refactor the request and response models so the modals can reuse the shared explorer execution engine instead of custom code.
- [ ] Add preset templates in the explorer for:
  - completion endpoint smoke test
  - embedding endpoint smoke test
- [ ] Preserve the useful upstream-call and `RequestHistoryId` display already exposed by Partio explorer responses.
- [ ] Remove duplicated UI logic from:
  - `dashboard/src/components/modals/InferenceEndpointTestModal.jsx`
  - `dashboard/src/components/modals/EmbeddingEndpointTestModal.jsx`
  after feature parity is achieved.

Acceptance criteria:

- [ ] The existing endpoint-test capability survives the migration.
- [ ] The product no longer carries a separate one-off testing UI path once parity is reached.

## Phase 9: Assistant Execution Rail Cleanup

- [ ] Review duplicate chat-history write logic in:
  - `src/AssistantHub.Server/Services/AssistantChatService.cs`
  - `src/AssistantHub.Server/Handlers/ChatHandler.cs`
- [ ] Consolidate assistant-turn persistence and observability hooks behind one clear execution path.
- [ ] Decide whether assistant responses should later emit a lightweight correlation ID that maps chat history and HTTP request history together.
- [ ] Ensure any future assistant explorer or request-history enhancements hook into the consolidated path only once.

Acceptance criteria:

- [ ] Assistant execution observability is not split across duplicate persistence code.
- [ ] Future explorer/history features can correlate assistant traffic reliably.

## Phase 10: Tests, Docs, And Rollout

- [ ] Add backend tests for:
  - request-history persistence
  - search filters
  - summary aggregation
  - redaction
  - truncation
  - disabled mode
  - tenant authorization
- [ ] Add frontend tests for:
  - explorer operation parsing
  - request template generation
  - request-history table behavior
  - detail modal rendering
  - assistant SSE handling
- [ ] Add manual verification scripts or checklists for:
  - system API explorer
  - assistant chat streaming
  - request-history capture for authenticated and unauthenticated assistant traffic
  - endpoint test migration
- [ ] Document operator behavior and privacy posture.
- [ ] Add upgrade notes if a database migration is required for existing installations.

Acceptance criteria:

- [ ] The feature ships with backend coverage, dashboard coverage, and an operator-facing verification checklist.
- [ ] Existing installations have a clear migration path.

## Risks

- The current committed `openapi.json` may be far enough from reality that trying to build the explorer on top of it first will create rework.
- Request/response body capture can create privacy and storage risks if redaction is weak or defaults are too permissive.
- Public assistant APIs and SSE flows are not well represented by basic CRUD explorer assumptions; they need dedicated execution behavior.
- The dashboard currently mixes admin and non-admin flows; access rules must be explicit before exposing request-history or assistant explorer pages broadly.
- Large uploads and downloads can distort request-history storage and explorer UX unless they are summarized rather than fully buffered.

## Initial Delivery Recommendation

If the work needs to be sliced into the smallest valuable first release, ship in this order:

- [ ] Phase 0
- [ ] Phase 1
- [ ] Phase 2
- [ ] Phase 3
- [ ] Phase 4
- [ ] Phase 5
- [ ] Phase 6

Then add assistant-specific explorer work:

- [ ] Phase 7
- [ ] Phase 8
- [ ] Phase 9
- [ ] Phase 10

## Notes

- Chat history remains a separate feature. Do not force generic HTTP request history into `ChatHistory`.
- The assistant explorer should share infrastructure with the system explorer, but not be constrained to generic CRUD UX. Assistant chat, thread lifecycle, and SSE are first-class requirements.
- The existing Partio endpoint test flows are useful seed functionality. Treat them as an on-ramp to the full explorer, not as the long-term product shape.
