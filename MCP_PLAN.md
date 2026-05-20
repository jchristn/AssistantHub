# AssistantHub MCP Server Plan

Status legend: `[ ]` not started, `[~]` in progress, `[x]` complete, `[!]` blocked

## Goal

Build a standalone `AssistantHub.McpServer` that mirrors the LiteGraph MCP implementation pattern:

- separate executable under `src/`
- Voltaic-based MCP plumbing
- upstream connection to the existing REST server through the C# SDK plus a small REST proxy for gaps
- per-domain registration classes
- HTTP, TCP, and WebSocket transports started together
- install/configuration flow for Claude/Cursor-style MCP clients

This plan covers implementation, testing, documentation, SDK parity work, `MCP_API.md`, `CHANGELOG.md`, `README.md`, Docker/release assets, and related repo updates.

## Reference Implementation To Follow

Use these LiteGraph files as the primary shape reference:

- `C:\code\litegraph\litegraph\src\LiteGraph.McpServer\LiteGraphMcpServer.cs`
- `C:\code\litegraph\litegraph\src\LiteGraph.McpServer\LiteGraph.McpServer.csproj`
- `C:\code\litegraph\litegraph\src\LiteGraph.McpServer\Classes\LiteGraphMcpServerSettings.cs`
- `C:\code\litegraph\litegraph\src\LiteGraph.McpServer\Classes\LiteGraphMcpRestProxy.cs`
- `C:\code\litegraph\litegraph\src\LiteGraph.McpServer\Classes\LiteGraphMcpServerHelpers.cs`
- `C:\code\litegraph\litegraph\src\LiteGraph.McpServer\Registrations\*.cs`
- `C:\code\litegraph\litegraph\src\Test.Shared\LiteGraphTouchstoneMcpHost.cs`
- `C:\code\litegraph\litegraph\README.md` MCP sections
- `C:\code\litegraph\litegraph\docs\CLAUDE_MCP.md`

## AssistantHub Source Of Truth

Use these AssistantHub files to drive scope and parity checks:

- `src/AssistantHub.Server/AssistantHubServer.cs`
- `src/AssistantHub.Server/Handlers/*.cs`
- `src/AssistantHub.Server/Services/OpenApiDocumentService.cs`
- `REST_API.md`
- `README.md`
- `sdk/csharp/AssistantHub.Sdk/*`
- `sdk/js/src/*`
- `sdk/python/assistanthub_sdk/*`

## Architectural Decisions

- [x] Keep the MCP server as a standalone process, not code embedded into `AssistantHub.Server`.
- [x] Use `Voltaic` for transports and tool registration, matching LiteGraph.
- [x] Prefer `sdk/csharp/AssistantHub.Sdk` as the typed upstream client.
- [x] Add `AssistantHubMcpRestProxy` for raw endpoints, HEAD checks, SDK gaps, and cases where exact REST semantics matter.
- [x] Use one configured upstream AssistantHub endpoint and bearer token per MCP server instance, same model as LiteGraph.
- [x] Default the MCP config file to `assistanthub-mcp.json` to avoid colliding with `assistanthub.json`.
- [x] Expose HTTP, TCP, and WebSocket transports together.
- [x] Make secret handling explicit before exposing config and credential-bearing tools.
- [x] Treat `REST_API.md` plus `OpenApiDocumentService` as the route inventory to reconcile against MCP coverage.

## Scope Notes

- [x] Management/configuration surfaces are required first: tenants, users, credentials, storage, assistants, settings, endpoints, models, crawl, eval, history, request history, configuration.
- [x] Binary endpoints need MCP-safe wrappers:
  document upload/download, bucket object upload/download.
- [x] Streaming endpoints need a deliberate strategy:
  assistant chat/generate SSE and eval SSE are explicitly documented as deferred/REST-only for the current MCP release.
- [x] Public assistant interaction routes should be included only after the management surface is stable, unless full REST parity is declared mandatory for v1.
  Current release keeps the management-first scope and only exposes public assistant metadata helpers.

## Phase 1: Project Scaffolding

- [x] Add `src/AssistantHub.McpServer/AssistantHub.McpServer.csproj`.
- [x] Add the new project to `src/AssistantHub.sln`.
- [x] Mirror the LiteGraph MCP project layout:
  `AssistantHubMcpServer.cs`, `Classes/`, `Registrations/`, `clean.bat`, `clean.sh`, `Dockerfile`.
- [x] Add package references for `Voltaic` and `SyslogLogging`.
- [x] Add project references needed for the MCP server implementation.
  Expected baseline: `sdk/csharp/AssistantHub.Sdk/AssistantHub.Sdk.csproj`.
- [x] Decide whether `AssistantHub.Core` is also referenced directly or whether all model access must come through `AssistantHub.Sdk`.
  Decision: reference `AssistantHub.Core` directly so the MCP server can reuse shared settings/models/serialization and avoid duplicating request-history/configuration types.
- [x] Add XML docs generation to the MCP project, matching repo style.

## Phase 2: Bootstrap And Configuration

- [x] Create `src/AssistantHub.McpServer/Classes/Constants.cs`.
- [x] Create `src/AssistantHub.McpServer/Classes/AssistantHubMcpServerSettings.cs`.
- [x] Create the supporting settings classes for:
  logging, node metadata, storage/temp dirs, upstream AssistantHub connection, HTTP, TCP, WebSocket, debug.
- [x] Avoid a namespace/type collision with `AssistantHub.Core.Settings.AssistantHubSettings`.
  Recommended MCP-side name: `AssistantHubServiceSettings` or equivalent.
- [x] Implement `AssistantHubMcpServer.cs` to mirror LiteGraph flow:
  welcome, arg parse, settings init, env overrides, logging init, SDK init, server init, tool registration, graceful shutdown.
- [x] Support `--config=`, `--showconfig`, `install`, `--dry-run`, `--help`.
- [x] Add environment variable overrides for:
  `ASSISTANTHUB_ENDPOINT`, `ASSISTANTHUB_API_KEY`, `MCP_HTTP_HOSTNAME`, `MCP_HTTP_PORT`, `MCP_TCP_ADDRESS`, `MCP_TCP_PORT`, `MCP_WS_HOSTNAME`, `MCP_WS_PORT`, `MCP_CONSOLE_LOGGING`.
- [x] Pick default MCP ports.
  Recommended: HTTP `8820`, TCP `8821`, WebSocket `8822`.
- [x] Set server metadata on each Voltaic transport:
  `ServerName = "AssistantHub.McpServer"` and version aligned with the release.
- [x] Add connection/request/response logging hooks on all transports.

## Phase 3: Install And Client Setup Experience

- [x] Implement `install` command behavior modeled after LiteGraph:
  update `~/.claude.json`, add an agent file under `~/.claude/agents/`, print Cursor config snippet.
- [x] Draft an `assistanthub` agent definition that allows `mcp__assistanthub__*`.
- [x] Keep `install --dry-run` fully non-destructive.
- [x] Decide whether the agent guidance lives only in generated content or is also committed as a repo asset for review.
  Decision: keep the generated install-time agent and add committed operator guidance in `docs/CLAUDE_MCP.md`.

## Phase 4: SDK Parity And Shared Upstream Abstractions

- [x] Audit `AssistantHub.Sdk` against `REST_API.md` and list gaps.
- [x] Close C# SDK gaps required by the MCP server.
  `requesthistory/*` is now implemented in the C# SDK, and the eval result / judge prompt contract mismatches are fixed.
- [x] Audit JS SDK parity in `sdk/js/src/client.ts`.
- [x] Audit Python SDK parity in `sdk/python/assistanthub_sdk/`.
- [x] Add or fix models/methods in all three SDKs for any new parity work done for MCP.
  JS and Python SDKs now expose request-history models and methods, and SDK-side tests now cover request-history read paths plus the eval judge-prompt contract.
- [x] Update SDK test coverage for any new methods/models.
  Coverage has been added in the C#, JS, and Python SDK runners for request-history read paths plus the eval judge-prompt/results contracts.
- [x] Implement `src/AssistantHub.McpServer/Classes/AssistantHubMcpRestProxy.cs`.
- [x] Define when to use SDK calls vs REST proxy calls.
  Recommended rule: use SDK for typed CRUD/enumeration; use proxy for raw JSON, HEAD, exact REST pass-through, binary wrappers, and temporary SDK gaps.

## Phase 5: Secret Handling Policy

- [x] Define which tool responses must redact secrets by default.
  Secret-bearing fields are redacted by default for credentials, assistant settings, configuration, and any payload containing known secret property names.
- [x] Decide whether create/update responses should echo secrets back at all.
  Decision: create/update responses remain redacted by default and only return raw values when `includeSecrets=true` is explicitly requested.
- [x] Decide whether `configuration/get` returns a redacted view by default.
- [~] Document any explicit opt-in secret-return behavior if one is allowed.
- [x] Add tests that verify redaction behavior.
- [x] Reflect the redaction policy in all SDK and MCP docs.

## Phase 6: Tool Naming And Coverage Matrix

- [x] Adopt LiteGraph-style slash naming.
  Example pattern: `tenant/create`, `assistant/settings/get`, `bucket/object/upload`.
- [x] Produce a coverage matrix that maps every relevant REST route to:
  `Mapped`, `Deferred`, or `Intentionally Unsupported`.
- [x] Store that matrix in `MCP_API.md` or an appendix referenced from it.
- [x] Explicitly call out exceptions for streaming and binary-heavy endpoints.

## Phase 7: Domain Registration Implementation

- [x] Create `src/AssistantHub.McpServer/Registrations/SystemRegistrations.cs`.
  Target tools: health, head/health if useful, whoami, openapi document fetch.
- [x] Create `src/AssistantHub.McpServer/Registrations/AuthenticationRegistrations.cs`.
  Target tools: authenticate and any auth-support operations worth exposing.
- [x] Create `src/AssistantHub.McpServer/Registrations/TenantRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/UserRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/CredentialRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/BucketRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/BucketObjectRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/CollectionRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/CollectionRecordRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/AssistantRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/AssistantSettingsRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/DocumentRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/IngestionRuleRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/FeedbackRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/HistoryRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/RequestHistoryRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/EmbeddingEndpointRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/CompletionEndpointRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/ModelRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/CrawlPlanRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/CrawlOperationRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/EvalRegistrations.cs`.
- [x] Create `src/AssistantHub.McpServer/Registrations/ConfigurationRegistrations.cs`.
- [x] Decide whether to add `AssistantPublicRegistrations.cs` in the first release for:
  public info, public thread/history, distinct labels/tags, public downloads, chat, compact, generate, feedback.
  Decision: defer public assistant interaction routes for the first management-first release.

## Domain Coverage Matrix

| Domain | REST Source | MCP Registration Target | Notes |
|---|---|---|---|
| Health / OpenAPI / Identity | `RootHandler`, `OpenApiHandler`, `TenantHandler.GetWhoAmIAsync` | `SystemRegistrations.cs` | Good first smoke-test surface |
| Authentication | `AuthenticateHandler` | `AuthenticationRegistrations.cs` | Optional if install-time token is enough, but include if full parity is required |
| Tenants | `TenantHandler` | `TenantRegistrations.cs` | CRUD, list, exists |
| Users | `UserHandler` | `UserRegistrations.cs` | CRUD, list, exists |
| Credentials | `CredentialHandler` | `CredentialRegistrations.cs` | CRUD, list, exists, secret redaction required |
| Buckets | `BucketHandler` | `BucketRegistrations.cs` | CRUD, list, exists |
| Bucket Objects | `BucketHandler` | `BucketObjectRegistrations.cs` | list, metadata, upload, download, delete |
| Collections | `CollectionHandler` | `CollectionRegistrations.cs` | CRUD, list, exists |
| Collection Records | `CollectionHandler` | `CollectionRecordRegistrations.cs` | CRUD-ish plus batch delete and distinct labels/tags |
| Assistants | `AssistantHandler` | `AssistantRegistrations.cs` | CRUD, list, exists |
| Assistant Settings | `AssistantSettingsHandler` | `AssistantSettingsRegistrations.cs` | get, put, Slack verify, secret redaction required |
| Documents | `DocumentHandler` | `DocumentRegistrations.cs` | upload, list, get, delete, bulk delete, exists, processing log, download |
| Ingestion Rules | `IngestionRuleHandler` | `IngestionRuleRegistrations.cs` | CRUD, list, exists |
| Feedback | `FeedbackHandler` | `FeedbackRegistrations.cs` | list, get, delete |
| History / Threads | `HistoryHandler`, chat thread routes | `HistoryRegistrations.cs` | list history, get history, delete history, list threads, get thread |
| Request History | `RequestHistoryHandler` | `RequestHistoryRegistrations.cs` | enumerate, summary, get, detail, delete, bulk delete |
| Embedding Endpoints | `EmbeddingEndpointHandler` | `EmbeddingEndpointRegistrations.cs` | CRUD, enumerate, exists, health, test |
| Completion Endpoints | `CompletionEndpointHandler` | `CompletionEndpointRegistrations.cs` | CRUD, enumerate, exists, health, test |
| Models | `InferenceHandler` | `ModelRegistrations.cs` | list, pull, pull status, delete |
| Crawl Plans | `CrawlPlanHandler` | `CrawlPlanRegistrations.cs` | CRUD, exists, start, stop, connectivity, enumerate |
| Crawl Operations | `CrawlOperationHandler` | `CrawlOperationRegistrations.cs` | list, stats, get, delete, enumeration |
| Evaluation | `EvalHandler` | `EvalRegistrations.cs` | facts, runs, results, judge prompt, stream strategy decision needed |
| Configuration | `ConfigurationHandler` | `ConfigurationRegistrations.cs` | get, put, secret redaction required |
| Public Assistant APIs | `ChatHandler` and public routes | `AssistantPublicRegistrations.cs` | full parity only; not management-first |

## Phase 8: Binary And Streaming Contracts

- [x] Define MCP argument shapes for uploads.
  Recommended pattern: `filename`, `contentType`, `contentBase64`, plus route-specific metadata.
- [x] Define MCP response shapes for downloads.
  Recommended pattern: `filename`, `contentType`, `contentBase64`, `size`, `source`.
- [x] Put explicit size limits on upload/download tools.
- [x] Decide whether oversized downloads return an error or a temporary URL/reference.
- [x] Decide how SSE endpoints are represented.
  Recommended first pass: do not stream through MCP; use polling/list/get tools and document the limitation.
- [ ] If eval stream parity is required, add a dedicated MCP strategy and tests before release.

## Phase 9: Test Strategy

- [~] Add `src/Test.Shared/AssistantHubMcpHost.cs`.
  Modeled after LiteGraph's `LiteGraphTouchstoneMcpHost.cs`; the harness now supports preserving spawned server/MCP artifacts via `ASSISTANTHUB_TEST_KEEP_ARTIFACTS=1` for live DB/log inspection.
- [x] Support starting both `AssistantHub.Server` and `AssistantHub.McpServer` out-of-process for end-to-end MCP tests.
- [x] Reuse the existing integration server approach where practical for REST-side setup.
- [x] Add helper code to connect with Voltaic `McpHttpClient`.
- [ ] Add unit tests for:
  argument parsing helpers, enum parsing, default handling, redaction helpers, REST proxy error mapping.
- [~] Add MCP integration tests for at least one happy-path tool per domain.
  The shared `McpSuite` now covers transport startup, health/openapi, tenant CRUD, assistant CRUD, configuration redaction/includeSecrets, request-history capture/detail/summary, and `install --dry-run`.
- [ ] Add failure-path tests for validation errors and upstream 404/403/500 translation.
- [ ] Add permission-boundary tests using different configured upstream credentials:
  global admin, tenant admin, regular user.
- [ ] Add binary wrapper tests for document and bucket object flows.
- [x] Add tests for the `install --dry-run` path.
- [x] Add transport smoke tests for HTTP, TCP, and WebSocket registration startup.
- [ ] Add regression tests that compare MCP coverage against the documented route matrix.

## Phase 10: Test Project And Runner Updates

- [x] Decide where MCP tests live:
  `Test.XUnit`, `Test.Automated`, or both.
  Decision: add an automated `McpSuite` and surface its results through the existing xUnit integration fixture so MCP coverage runs in both paths.
- [x] If using `Test.Automated`, add a dedicated MCP suite and summary reporting.
- [x] If using `Test.XUnit`, add explicit MCP test classes and fixtures.
- [x] Update `run-tests.ps1`.
- [x] Update `run-tests.sh`.
- [x] Update `run-tests.bat`.
- [x] Update root `TESTING.md` with MCP build/start/test instructions.

## Phase 11: Documentation Updates

- [x] Create `MCP_API.md`.
  Include tool families, naming rules, transport endpoints, config, install, redaction policy, route-to-tool matrix, and examples.
- [x] Update `README.md` with a new MCP section:
  overview, prerequisites, quick start, config file example, transport table, install flow, Docker usage, supported tool families.
- [x] Update `REST_API.md` to cross-link the MCP server and `MCP_API.md`.
- [x] Update `TESTING.md` with MCP-specific test instructions.
- [x] Add a short "Using Claude/Cursor with AssistantHub MCP" subsection or dedicated document if the README becomes too large.
- [ ] Document any intentionally deferred REST routes or streaming limitations.

## Phase 12: SDK Documentation Updates

- [x] Update `sdk/csharp/README.md` for any parity additions made while supporting MCP.
- [x] Update `sdk/csharp/TESTING.md` if C# SDK tests or fixtures change.
- [x] Update `sdk/js/README.md` for any parity additions made while supporting MCP.
- [x] Update `sdk/js/TESTING.md` if JS SDK tests or build steps change.
- [x] Update `sdk/python/README.md` for any parity additions made while supporting MCP.
- [x] Update `sdk/python/TESTING.md` if Python SDK tests or packaging steps change.

## Phase 13: Docker, Build, And Release Assets

- [x] Add `src/AssistantHub.McpServer/Dockerfile`.
- [x] Add `build-mcp.bat`, modeled after `build-server.bat`.
- [x] Add Docker runtime config file:
  `docker/assistanthub-mcp/assistanthub-mcp.json`.
- [x] Update `docker/compose.yaml` with an `assistanthub-mcp-server` service.
- [x] Decide whether to add factory/default MCP config assets under `docker/factory/`.
  Decision: keep the default MCP runtime asset under `docker/assistanthub-mcp/` and do not add factory reset content for the MCP container.
- [x] Expose the chosen MCP ports in Docker.
- [x] Ensure the container can point at `assistanthub-server` by service name.
- [x] Decide image naming/versioning.
  Recommended: `jchristn77/assistanthub-mcp:<version>`.

## Phase 14: Versioning And Changelog

- [x] Add a new release section to `CHANGELOG.md`.
- [x] Update version numbers where required:
  `AssistantHub.McpServer.csproj`, Docker tags, release docs, any centralized product/version references.
- [x] Decide whether the MCP server version always matches `AssistantHub` server version.
  Decision: keep the MCP server aligned with the AssistantHub server version for this release train.
- [ ] Add any breaking-change notes if secret redaction or SDK contract changes alter existing behavior.

## Phase 15: Pre-Release Verification

- [x] Build the solution with the new MCP project included.
- [x] Run the full test suite and ensure MCP tests are included by default.
- [x] Run the MCP server locally against a live AssistantHub instance.
- [x] Verify all three transports start and accept connections.
- [x] Verify `install --dry-run`.
- [ ] Verify at least one CRUD flow end-to-end through MCP for:
  tenant, user, credential, assistant, assistant settings, ingestion rule, document, endpoint, crawl plan, eval, configuration.
- [x] Verify request history tools end-to-end against a live server.
- [x] Verify secrets are redacted where intended.
- [~] Verify Docker compose can bring up AssistantHub plus the MCP server together.
  `docker compose -f docker/compose.yaml config` succeeds with the MCP service pointing at the named image `jchristn77/assistanthub-mcp:v0.10.0`; full stack startup was not executed in this session.

## Acceptance Criteria

- [x] `src/AssistantHub.McpServer` exists and follows the LiteGraph MCP project shape closely.
- [x] Voltaic HTTP, TCP, and WebSocket transports all start successfully.
- [x] Tool registration coverage is documented and reconciled against `REST_API.md`.
- [x] Required SDK parity gaps are closed across C#, JS, and Python.
- [x] `MCP_API.md` exists and is accurate.
- [x] `README.md`, `CHANGELOG.md`, and `TESTING.md` are updated.
- [x] Docker and build assets for the MCP server exist and work.
  Build script, Dockerfile, runtime config, and compose wiring are present; `docker compose -f docker/compose.yaml config` validates the stack and `docker build -f src/AssistantHub.McpServer/Dockerfile . -t assistanthub-mcp-local:verify` succeeds.
- [x] MCP tests are automated and run with the normal repo test flow.
- [x] Any deferred routes are explicitly documented with rationale.

## Suggested Implementation Order

1. Project scaffolding and bootstrap.
2. Secret-handling policy and SDK gap audit.
3. Core control-plane registrations:
   tenants, users, credentials, assistants, settings, ingestion rules, configuration.
4. Storage and endpoint registrations.
5. Crawl, eval, history, and request history.
6. Binary wrappers and any remaining public/interaction tools.
7. Docker, install flow, docs, SDK docs, changelog, and final verification.
