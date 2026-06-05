# Changelog

## Unreleased

### Added
- **AssistantHub MCP server**: Added standalone `AssistantHub.McpServer` with Voltaic HTTP, TCP, and WebSocket transports plus install support for Claude/Cursor MCP clients.
- **MCP management surface**: Added MCP tools for system/runtime inspection, authentication, tenants, users, credentials, buckets, bucket objects, collections, assistants, assistant settings, documents, ingestion rules, feedback, history, request history, endpoints, models, crawl plans, crawl operations, evaluation, and runtime configuration.
- **MCP integration coverage**: Added shared MCP host/test harness support and automated MCP transport, CRUD, configuration-redaction, request-history, and install-path coverage.
- **MCP docs and Docker assets**: Added `MCP_API.md`, `docs/CLAUDE_MCP.md`, `src/AssistantHub.McpServer/Dockerfile`, `build-mcp.bat`, and Docker compose/config assets for running the MCP server alongside the platform.
- **Docker status scripts**: Added `docker/status.bat` and `docker/status.sh` for a concise `docker ps -a` view of container ID, name, creation time, status, and ports.

### Changed
- **Docker PostgreSQL defaults**: The local Docker deployment now defaults AssistantHub, Less3, Partio, and RecallDB to a shared PostgreSQL/pgvector container with separate service databases and application roles, plus a one-shot `postgres-init` verifier before services start.
- **Archived PostgreSQL migration plan**: Moved the completed Docker PostgreSQL migration plan to `archive/POSTGRES_MIGRATION.md`.
- **SDK parity for MCP-backed routes**: C#, JavaScript, and Python SDKs now expose request-history APIs and align the eval judge-prompt/eval-results contracts used by the MCP server.
- **Testing documentation and runners**: Root test docs and wrappers document the Touchstone-backed `Test.Automated`, `Test.Xunit`, and `Test.Nunit` layout plus MCP-focused environment controls.

### Fixed
- **Request-history query parsing**: Request-history filter parsing now URL-decodes reserved characters before building filters, fixing MCP and REST summary/list queries that include encoded path or timestamp values.
- **Assistant analytics database portability**: Retained-chat analytics scoping now uses the active database driver's boolean formatting instead of a hard-coded literal, keeping the fix valid for SQLite, MySQL, PostgreSQL, and SQL Server.

## v0.13.0 - Assistant Analytics

### Added
- **Assistant Analytics dashboard**: Added `Assistants > Analytics` with assistant selector, independent last-hour/day/week/month chart ranges, overview metrics, request volume/outcome, latency percentiles, hot-path stage duration, provider timing, provider throughput, utility-call activity, retrieval fanout/chunk flow, endpoint/model usage, slowest requests, and feedback trend.
- **Assistant Analytics scoping**: Analytics now treats Assistant History as the ownership boundary and joins Request History only for supporting timing/status telemetry, so deleting assistant history removes those turns from analytics without deleting the request audit log.
- **Assistant analytics REST API**: Added authenticated `GET /v1.0/assistants/{assistantId}/analytics/overview`, `/timeseries`, `/stages`, `/endpoints`, `/slowest`, and `/feedback`.
- **Assistant-scoped telemetry indexing**: Added `assistant_id` to `chat_history_performance_events`, startup backfill, and assistant/time/stage/endpoint indexes for SQLite, PostgreSQL, MySQL, and SQL Server.
- **Migration scripts**: Added `migrations/011_upgrade_to_v0.13.0.*.sql` provider scripts for existing installations.
- **Archived implementation plan**: Moved the completed Assistant Analytics implementation plan to `archive/ASSISTANT_ANALYTICS.md`.
- **SDK/Postman/MCP coverage**: C#, JavaScript/TypeScript, Python, Postman, OpenAPI, and MCP now expose assistant analytics read APIs.
- **Analytics tests**: Added Touchstone shared coverage for analytics range capping, aggregation, endpoint summaries, slowest request detection, feedback analytics, and telemetry assistant ID projection.

### Changed
- Dashboard history and request-history filters can deep-link with `assistantId` so analytics drill-downs open directly to related entries.
- Docker compose image tags and package/product versions updated to `0.13.0`.

### Breaking
- Database schema changes require running the matching `migrations/011_upgrade_to_v0.13.0.*.sql` provider script for existing installations when startup migrations are not used.

## v0.12.0 - Assistant Performance Telemetry

### Added
- **Provider-agnostic chat telemetry**: Chat history now captures `TraceId`, `RequestHistoryId`, `PerformanceSchemaVersion`, and `PerformanceJson` with versioned assistant hot-path telemetry.
- **Queryable performance events**: Added `chat_history_performance_events` table and data access implementations for SQLite, PostgreSQL, MySQL, and SQL Server.
- **Request/history correlation**: Request history now stores `TraceId` and `ChatHistoryId` so HTTP request details can be correlated with assistant chat history and logs.
- **Inference timing detail**: Final inference telemetry captures endpoint limiter wait, request-to-headers, headers-to-first-token, first-token-to-last-token, total client time, HTTP status, endpoint/model metadata, token counts, and provider-native metrics when available.
- **Dashboard performance drill-down**: Assistant history details and request-history details show expanded stage timing, token, endpoint, and provider metric tables.
- **Migration scripts**: Added `migrations/010_upgrade_to_v0.12.0.*.sql` provider scripts for existing installations.

### Changed
- Chat history writes now persist telemetry and normalized performance events as part of the hot path instead of dropping timing detail after response completion.
- Request-history capture preserves preassigned request IDs and trace IDs for assistant chat requests.
- C#, JavaScript, and Python SDKs include the new history correlation fields and assistant telemetry DTOs.
- Docker compose image tags and package/product versions updated to `0.12.0`.

### Breaking
- Database schema changes require running the matching `migrations/010_upgrade_to_v0.12.0.*.sql` provider script for existing installations when startup migrations are not used.

## v0.11.0 - Assistant Utility Endpoint Routing

### Added
- **Specialized assistant inference endpoints**: Assistant settings now include optional `RetrievalGateInferenceEndpointId`, `QueryRewriteInferenceEndpointId`, and `RerankInferenceEndpointId` fields for routing RAG utility LLM calls to dedicated completion endpoints.
- **Dashboard endpoint selectors**: Assistant Settings exposes dropdowns for retrieval gate, query rewrite, and re-rank endpoints alongside the required response inference endpoint.
- **Schema migration**: Added startup migrations and standalone provider migration scripts for SQLite, PostgreSQL, MySQL, and SQL Server for the new assistant settings columns.

### Changed
- Retrieval gate, query rewrite, and re-ranking now honor their dedicated endpoint settings while falling back to `InferenceEndpointId` when unset.
- C#, JavaScript, and Python SDK models include the new assistant settings fields.
- .NET test projects now use Touchstone NuGet packages with shared suites, a console runner, xUnit adapter, and NUnit adapter.
- OpenAPI, Postman, REST API docs, and package/product versions updated to `0.11.0`.

### Breaking
- Database schema changes require running the matching `migrations/009_upgrade_to_v0.11.0.*.sql` provider script for existing installations when startup migrations are not used.

## v0.10.0 - API Explorer And Request History

### Added
- **HTTP request history subsystem**: AssistantHub now captures searchable request and response metadata for system APIs and assistant-facing APIs, including headers, bodies, status, duration, assistant ID, thread ID, and tenant context with redaction and truncation controls.
- **Dashboard Request History view**: New operator surface for filtering traffic, summarizing success and failure rates, inspecting request and response detail, and replaying captured calls into the explorer.
- **Dashboard API Explorer**: New live API explorer driven by runtime route metadata, with request editing, response inspection, code snippets, recent requests, and assistant-specific helper flows for public assistant APIs.
- **Runtime OpenAPI exposure**: AssistantHub now exposes `/openapi.json` from the running server for explorer consumption and route-surface verification.
- Migration script: `migrations/008_upgrade_to_v0.10.0.sql`

### Changed
- Docker image tags updated to `v0.10.0`
- Endpoint test workflows can now open directly in the shared API Explorer with prebuilt request presets
- The dashboard now includes `API Explorer` and `Request History` as built-in `Monitoring` tools for system and assistant API traffic
- Product and package versions updated to `0.10.0`

### Breaking
- Database schema changes require running `migrations/008_upgrade_to_v0.10.0.sql` for existing installations

## v0.9.0 - Slack Support Added

### Added
- **Per-assistant Slack integration**: Slack configuration now lives on assistant settings with `EnableSlack`, `SlackAppToken`, `SlackBotToken`, `SlackChannelId`, and `SlackMessagePrefix`
- **Slack connectivity verification**: New `POST /v1.0/assistants/{assistantId}/settings/slack/verify` endpoint and dashboard flow for testing draft Slack settings before saving
- **Slack worker runtime**: AssistantHub now starts one Socket Mode worker per Slack-enabled assistant, supports configured-channel traffic and direct messages, suppresses self-messages, and posts replies back into Slack threads
- **Shared chat execution service**: Non-streaming chat execution is now reusable by web chat and Slack so retrieval, compaction, inference, citations, and persistence stay aligned
- **Chat history origin tracking**: `chat_history.origin` records request source values such as `web` and `slack`
- Migration script: `migrations/007_upgrade_to_v0.9.0.sql`

### Changed
- Docker image tags updated to `v0.9.0`
- Assistant settings API, OpenAPI spec, Postman collection, and dashboard updated for Slack configuration and verification
- Slack responses are transport-shaped only at delivery time while canonical response text is persisted to history

### Breaking
- Database schema changes require running `migrations/007_upgrade_to_v0.9.0.sql` for existing installations

## v0.8.0

### Added
- **RAG Evaluation**: Automated evaluation framework for measuring RAG pipeline quality. Define expected facts per assistant, run evaluations that send questions through the inference pipeline, and use an LLM judge to score responses against expected facts with PASS/FAIL verdicts and reasoning.
- New models: `EvalFact` (prefix `ef_`), `EvalRun` (prefix `erun_`), `EvalResult` (prefix `eres_`), `FactVerdict`, `EvalStatusEnum`
- New database tables: `eval_facts`, `eval_runs`, `eval_results` with indexes for tenant, assistant, status, and temporal queries
- New assistant setting: `EvalJudgePrompt` — custom judge prompt template per assistant (supports `{QUESTION}`, `{RESPONSE}`, `{EXPECTED_FACT}` placeholders). Falls back to built-in default when not configured.
- New `EvalService` — orchestrates evaluation runs in the background, sends questions through inference, judges each expected fact, tracks progress, and stores results
- New `EvalHandler` with 13 API endpoints:
  - `PUT /v1.0/eval/facts` — create eval fact
  - `GET /v1.0/eval/facts` — list eval facts (with assistant filter)
  - `GET /v1.0/eval/facts/{factId}` — get eval fact
  - `PUT /v1.0/eval/facts/{factId}` — update eval fact
  - `DELETE /v1.0/eval/facts/{factId}` — delete eval fact
  - `POST /v1.0/eval/runs` — start eval run (with optional judge prompt override)
  - `GET /v1.0/eval/runs` — list eval runs
  - `GET /v1.0/eval/runs/{runId}` — get eval run
  - `DELETE /v1.0/eval/runs/{runId}` — delete eval run and results
  - `GET /v1.0/eval/runs/{runId}/results` — get all results for a run
  - `GET /v1.0/eval/runs/{runId}/stream` — SSE stream of run progress (2-second polling)
  - `GET /v1.0/eval/results/{resultId}` — get single eval result
  - `GET /v1.0/eval/judge-prompt/default` — get the built-in default judge prompt
- Dashboard: "Evaluation" page in the Chat (Assistants) sidebar section with:
  - Facts sub-tab: DataTable with category, question, expected facts count; create/edit modal with dynamic expected facts list; delete with confirmation
  - Runs sub-tab: DataTable with status badges, pass/fail counts, pass rate, duration; start new run modal with optional judge prompt override; SSE progress streaming modal; results modal with per-fact detail view showing LLM response and judge verdicts with reasoning
- OpenAPI spec updated with Evaluation tag and all 13 endpoint definitions plus schemas
- Migration script: `migrations/006_upgrade_to_v0.8.0.sql` (SQLite, PostgreSQL, MySQL, SQL Server)

### Changed
- Docker image tags updated to v0.8.0
- Database schema updated with `eval_judge_prompt` column on `assistant_settings` and three new eval tables
- `DatabaseDriverBase` extended with `EvalFact`, `EvalRun`, `EvalResult` entity method interfaces
- `SqliteDatabaseDriver` wired with eval method implementations
- `TableQueries` updated with eval table creation and index statements
- `Constants` updated with eval identifier prefixes and table names
- `IdGenerator` updated with `NewEvalFactId()`, `NewEvalRunId()`, `NewEvalResultId()` methods

### Breaking
- Database schema changes require running `migrations/006_upgrade_to_v0.8.0.sql` for existing installations

## v0.7.0

### Added
- **Metadata filtering for RAG retrieval**: Filter chat retrieval to only return documents matching specified labels (strings) and/or tags (key-value pairs with conditional operators). Filters can be configured as defaults on an assistant and/or supplied per-conversation at chat time.
- New assistant settings: `RetrievalLabelFilter`, `RetrievalTagFilter` — JSON-serialized default filters applied during every retrieval for that assistant
- New `metadata_filter` field on `ChatCompletionRequest` — per-request filter that is merged with assistant defaults
- Filter merging: assistant-level and request-level filters are unioned (required labels/tags combined, excluded labels/tags combined)
- Effective metadata filter stored in `ChatHistory.MetadataFilter` for auditing
- Metadata filter display in the History View modal
- New proxy API endpoints for discovering available filter values:
  - `GET /v1.0/collections/{collectionId}/labels/distinct` (admin, authenticated)
  - `GET /v1.0/collections/{collectionId}/tags/distinct` (admin, authenticated)
  - `GET /v1.0/assistants/{assistantId}/labels/distinct` (public, no auth)
  - `GET /v1.0/assistants/{assistantId}/tags/distinct` (public, no auth)
- Dashboard: Retrieval Filters section in assistant settings (JSON editor for label and tag filters)
- Dashboard: Collapsible metadata filter panel in ChatPanel for per-session filtering
- Migration script: `migrations/005_upgrade_to_v0.7.0.sql`

### Changed
- Docker image tags updated to v0.7.0
- Database schema updated with metadata filtering columns for `assistant_settings` and `chat_history` tables
- `RetrievalService.BuildSearchBody()` now uses Dictionary-based construction to support conditional `LabelFilter` and `TagFilter` properties
- `RetrievalSearchOptions` extended with `MetadataFilter` property
- Chat completion request body now accepts an optional `metadata_filter` object (AssistantHub extension to the OpenAI-compatible schema; omitting it preserves existing behavior)

### Breaking
- Database schema changes require running `migrations/005_upgrade_to_v0.7.0.sql` for existing installations

## v0.6.0

### Added
- **LLM-based re-ranking**: After initial retrieval, an LLM scores each chunk's relevance (0-10) to the user's query. Low-scoring chunks are filtered out and results are re-sorted by relevance before context injection.
- New assistant settings: `EnableReranking`, `RerankerTopK`, `RerankerScoreThreshold`, `RerankPrompt`
- New chat history telemetry: `RerankDurationMs`, `RerankInputCount`, `RerankOutputCount`
- Re-ranking TimingBar in history detail view
- Re-rank score display on citation cards and chunk details
- Migration script: `migrations/004_upgrade_to_v0.6.0.sql`

### Changed
- Docker image tags updated to v0.6.0
- Database schema updated with re-ranking columns for `assistant_settings` and `chat_history` tables

### Breaking
- Database schema changes require running `migrations/004_upgrade_to_v0.6.0.sql` for existing installations

## v0.5.0

### Native Crawlers

- **Web crawler engine** -- Built-in web crawling powered by CrawlSharp. Automatically discovers, retrieves, and ingests content from websites. Supports link following, sitemap extraction, robots.txt compliance, and configurable crawl depth, parallelism, and throttling
- **Crawl plans** -- Persistent crawler configurations that define what to crawl, how to crawl it, and what to do with the results. Each plan specifies a target URL, authentication, schedule, filters, ingestion rule, and processing options
- **Scheduled crawling** -- Automatic recurring crawls on configurable intervals (one-time, minutes, hours, days, weeks). Background scheduler service checks all plans every 60 seconds and launches crawls when due
- **Crawl operations** -- Each crawl execution is tracked as a separate operation with full lifecycle state (NotStarted, Starting, Enumerating, Retrieving, Success, Failed, Stopped, Canceled) and detailed counters for objects/bytes enumerated, added, updated, deleted, succeeded, and failed
- **Delta-based crawling** -- Crawl enumerations are persisted to disk as JSON files. Subsequent crawls compare against the previous enumeration to identify new, changed, deleted, and unchanged objects, processing only the delta
- **Document traceability** -- Crawled documents are linked back to their crawl plan and operation via `CrawlPlanId`, `CrawlOperationId`, and `SourceUrl` fields on `AssistantDocument`. Filter the Documents view by crawler to see all documents from a specific crawl plan
- **Web authentication** -- Support for None, Basic (username/password), API Key (custom header), and Bearer Token authentication when crawling protected sites
- **Crawl filters** -- Optional content type whitelist, object prefix/suffix matching, and minimum/maximum file size constraints to control which discovered resources are ingested
- **Configurable processing** -- Per-plan control over whether additions, updates, and deletions are processed. Configurable maximum concurrent drain tasks (1-64) for parallel ingestion
- **Operation retention** -- Per-plan retention period (0-14 days) with automatic cleanup of expired operations and their enumeration files by a background service running hourly
- **Startup recovery** -- The scheduler service detects any crawl plans left in a Running state from a previous unclean shutdown and resets them to Stopped with the last operation marked as Failed
- **On-demand controls** -- Start and stop crawls immediately via API or dashboard, independent of the schedule
- **Connectivity testing** -- Test crawl plan connectivity before running a full crawl. The API performs a single-page fetch against the configured URL and reports success/failure
- **Content enumeration** -- Preview what a crawl plan would discover without ingesting anything. The API returns the list of discovered resources with metadata
- **Crawl operations statistics** -- Aggregate and per-operation statistics including run counts, success/failure rates, runtime min/max/avg, total objects and bytes crawled, and next scheduled run time
- **Dashboard: Crawlers view** -- Full management UI for crawl plans with DataTable listing, create/edit form modal with collapsible sections (General, Ingestion, Repository Settings, Schedule, Filter, Processing, Retention), row actions (Start, Stop, Edit, View Operations, View JSON, Verify Connectivity, Enumerate Contents, Delete), and bulk delete
- **Dashboard: Operations modal** -- Statistics panel with aggregate metrics, operations table with status badges, per-operation actions (View Enumeration, Delete)
- **Dashboard: Enumeration viewer** -- Collapsible sections for All Files, New Files, Changed Files, Deleted Files, Successfully Crawled, and Failed, each with count/size summary and expandable file table
- **Dashboard: Documents integration** -- Crawler filter dropdown in Documents view, "Crawled" badge on crawler-produced documents, "View Crawl Operation" context menu action
- **Extensible crawler architecture** -- Abstract `CrawlerBase` class with `CrawlerFactory` pattern. Web is the first implementation; future repository types (S3, SFTP, etc.) can be added by implementing `CrawlerBase` and extending `RepositoryTypeEnum`
- **API endpoints** -- 16 new authenticated routes: CRUD for crawl plans (`/v1.0/crawlplans`), start/stop/connectivity/enumerate actions, and crawl operations sub-resource with statistics and enumeration file access
- **Database support** -- Full schema for all 4 database drivers (SQLite, PostgreSQL, MySQL, SQL Server) with new `crawl_plans` and `crawl_operations` tables plus indexes
- **Breaking change** -- v0.5.0 includes schema changes to add `crawl_plans` and `crawl_operations` tables and add `crawl_plan_id`, `crawl_operation_id`, `source_url` columns to `assistant_documents`. A migration script is provided at `migrations/003_upgrade_to_v0.5.0.sql`

## v0.4.0

### Query Rewrite

- **LLM-based query rewrite** -- Optionally rewrite user queries into multiple semantically varied phrasings before retrieval, broadening recall by capturing synonyms, alternate phrasing, and conceptual restatements that a single query would miss
- **Customizable rewrite prompt** -- User-editable prompt template with `{prompt}` placeholder; dashboard textarea with tooltips for easy customization. Falls back to a built-in default when not customized
- **Multi-query retrieval** -- All rewritten queries are sent to RecallDB independently; results are deduplicated by document ID and position, re-sorted by score, and capped at the configured Top K
- **Rewrite metrics in chat history** -- Query rewrite results and duration are persisted in chat history for auditability and performance monitoring
- **Breaking change** -- v0.4.0 includes schema changes to `assistant_settings` and `chat_history` tables. A migration script is provided at `migrations/002_upgrade_to_v0.4.0.sql`

## v0.3.0

### Multi-Tenant Platform

- **Full multi-tenancy** -- Row-level tenant isolation across all entities (users, credentials, assistants, documents, ingestion rules, chat history, feedback). Each tenant operates in complete isolation within a shared deployment
- **Tenant management** -- CRUD API and dashboard view for creating, updating, and deleting tenants (global admin only)
- **Auto-provisioning** -- New tenants are automatically provisioned with a RecallDB tenant, default collection, S3 bucket, admin user, credential, and ingestion rule
- **Tenant deletion cascade** -- Deleting a tenant cleanly removes all child rows, S3 buckets, the RecallDB tenant, and the tenant record
- **Three-tier authorization** -- Global Admin (admin API keys or users with `IsAdmin=true`), Tenant Admin (`IsTenantAdmin`), and Tenant User roles with appropriate access controls throughout all handlers
- **Per-tenant S3 bucket isolation** -- Each tenant's S3 buckets are prefixed with `{tenantId}_` (e.g. `ten_abc123_default`). Bucket and object operations enforce tenant bucket prefix for non-global-admin users. Auto-provisioning creates a default bucket per tenant; deprovisioning deletes all tenant buckets
- **Per-tenant processing logs** -- Processing log files are namespaced by tenant ID in subdirectories, with backward-compatible fallback for pre-v0.3.0 logs
- **Per-tenant RecallDB mapping** -- Each AssistantHub tenant maps 1:1 to its own RecallDB tenant for vector isolation
- **Tenant-scoped API routes** -- Users and credentials are accessed via `/v1.0/tenants/{tenantId}/users` and `/v1.0/tenants/{tenantId}/credentials`
- **WhoAmI endpoint** -- `GET /v1.0/whoami` returns the current authentication context including tenant, role, and user details
- **Login with tenant context** -- Email/password authentication accepts optional `TenantId` parameter; dashboard login form includes Tenant ID field
- **Dashboard tenant awareness** -- Topbar shows tenant name and role badge; sidebar conditionally shows admin-only sections; 6 data views show Tenant column for global admins
- **Tenants dashboard view** -- Full CRUD interface with provisioning result modal showing auto-generated credentials
- **Breaking change** -- v0.3.0 is a new deployment; existing v0.2.0 databases require the manual migration script (`migrations/001_upgrade_to_v0.3.0.sql`) which adds `tenant_id` columns, creates the default tenant, promotes existing admins to tenant admins, and inserts default records. Scripts provided for SQLite, PostgreSQL, SQL Server, and MySQL
- **Protected records** -- Default tenant, admin user, and credential are marked `IsProtected = true` and cannot be deleted via API (returns 403). Per-tenant provisioned users and credentials are also protected. Protected status is visible and editable in the dashboard. Deactivate protected records by setting `Active = false` instead
- **Default credentials** -- Fresh deployments are pre-seeded with a default tenant, admin user (`admin@assistanthub` / `password`), bearer token (`default`), and ingestion rule. First-run auto-provisioning creates these records if no tenants exist
- **Database test suite updated** -- All 9 test classes (including new TenantTests) explicitly set TenantId and use tenant-scoped enumeration

### Other Changes

- **Neighbor chunk retrieval** -- Optionally retrieve surrounding chunks for each search match to provide broader document context to the model, configurable per assistant (0-10 neighbors)
- Removed `RecallDb.TenantId` from configuration (now derived from authenticated user context)
- Added `AdminApiKeys` and `DefaultTenant` settings sections

## v0.2.0

- **Initial release**
- **Multi-assistant platform** -- Create and manage multiple AI assistants, each with independent configuration, personality, knowledge base, and appearance
- **Automated document ingestion pipeline** -- Upload documents (PDF, text, HTML, and more); automatic text extraction via DocumentAtom, chunking and embedding via Partio, and storage in RecallDB
- **Ingestion rules** -- Define reusable ingestion configurations specifying target S3 buckets, RecallDB collections, chunking strategies, optional summarization, and embedding settings
- **Flexible search modes** -- Vector (semantic similarity), full-text (keyword matching), and hybrid search with tunable scoring weights for optimal retrieval
- **LLM-based retrieval gate** -- Optional per-assistant retrieval gate that classifies whether each user message requires new document retrieval or can be answered from existing conversation context
- **Conversation compaction** -- Automatic summarization of older messages when the conversation approaches the context window limit, preserving conversation continuity
- **Streaming chat responses** -- Real-time Server-Sent Events (SSE) streaming for token-by-token response delivery
- **Configurable inference endpoints** -- Support for Ollama (local) and OpenAI (cloud) inference providers, with per-assistant endpoint overrides via managed Partio endpoints
- **Document summarization** -- Optional pre-chunking or post-chunking summarization of document content using configurable completion endpoints
- **Public chat API** -- Unauthenticated OpenAI-compatible chat endpoint for embedding assistants into external applications
- **Feedback collection** -- Thumbs-up/thumbs-down feedback and free-text comments on assistant responses for quality monitoring
- **Chat history and performance metrics** -- Per-turn history with detailed timing measurements: retrieval duration, time to first token, time to last token, tokens per second, compaction duration, and more
- **Browser-based dashboard** -- Full management UI for assistants, documents, ingestion rules, endpoints, feedback, history, collections, buckets, users, and live chat testing
- **Multi-tenant user management** -- Admin and standard user roles with per-user assistant ownership
- **Multiple database backends** -- SQLite (default), PostgreSQL, SQL Server, and MySQL for the application database
- **One-command Docker deployment** -- Fully orchestrated Docker Compose stack with health checks, dependency ordering, and persistent volumes
- **Citation metadata in chat responses** -- When enabled per-assistant, the system instructs the model to cite source documents using bracket notation [1], [2] and returns a structured `citations` object in the response mapping references to source document names, IDs, relevance scores, and text excerpts
- **Citation document linking** -- Configurable `CitationLinkMode` setting (`None`, `Authenticated`, `Public`) that populates `download_url` on citation sources. All downloads are server-proxied (no direct S3 exposure). Public mode provides unauthenticated download gated by the assistant setting. Citation cards in the dashboard are clickable when a download URL is available
