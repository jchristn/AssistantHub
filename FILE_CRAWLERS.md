# AssistantHub v0.15.0 File-Server Crawler Plan

AssistantHub already has a real crawler abstraction. `WebRepositoryCrawler` derives from `CrawlerBase`, and `CrawlerFactory.Create(...)` returns `CrawlerBase` for the selected repository type. The scheduler, connectivity endpoint, ad-hoc enumeration endpoint, delta calculation, retention, document creation, storage upload, and ingestion handoff already flow through that base layer.

CIFS and NFS need one meaningful extension to that base layer: web crawling receives content bytes during enumeration, but file-server crawlers should enumerate metadata first and retrieve bytes only when a file is being processed. The implementation should add a lazy retrieval hook to `CrawlerBase`, then share most CIFS/NFS behavior in a file-server crawler base backed by `Blobject.Core.BlobClientBase`.

## Progress Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete
- `[!]` Blocked or needs a decision

## Current Findings

- [x] Confirmed `src/AssistantHub.Core/Services/Crawlers/WebRepositoryCrawler.cs` inherits `CrawlerBase`.
- [x] Confirmed `src/AssistantHub.Core/Services/Crawlers/CrawlerFactory.cs` returns `CrawlerBase`.
- [x] Confirmed `CrawlerBase` owns shared crawl lifecycle behavior: operation state transitions, enumeration files, delta calculation, filtering, add/update/delete processing, S3 upload, document creation, ingestion triggering, deletion cleanup, cancellation, and disposal.
- [x] Confirmed the existing settings converter is web-only: `CrawlRepositorySettingsConverter.Read(...)` always deserializes `WebCrawlRepositorySettings`.
- [x] Confirmed the dashboard Create Crawl Plan modal hardcodes `REPOSITORY_TYPES = ['Web']` and always renders/serializes web repository settings.
- [x] Confirmed View's CIFS/NFS crawlers use `Blobject.CIFS`, `Blobject.NFS`, and `Blobject.Core.BlobClientBase`.

## View Reference

Reference files:

- `C:\code\view\backend\core\csharp\View.Models\DataRepository.cs`
- `C:\code\view\backend\core\csharp\View.Models\DataRepositoryTypeEnum.cs`
- `C:\code\view\backend\core\csharp\View.Models\NfsVersionEnum.cs`
- `C:\code\view\backend\core\csharp\View.ConnectorServer\Classes\RepositoryCrawlerBase.cs`
- `C:\code\view\backend\core\csharp\View.ConnectorServer\Classes\CifsRepositoryCrawler.cs`
- `C:\code\view\backend\core\csharp\View.ConnectorServer\Classes\NfsRepositoryCrawler.cs`
- `C:\code\view\backend\core\csharp\View.ConnectorServer\Classes\RepositoryConnectivityService.cs`
- `C:\code\view\backend\core\csharp\View.ConnectorServer\Classes\RepositoryEnumerationService.cs`
- `C:\code\view\backend\core\csharp\View.ConnectorServer\Classes\Converters.cs`

View package versions found locally:

- `Blobject.CIFS` `5.0.14`
- `Blobject.NFS` `5.0.14`
- `Blobject.Core` `5.0.14`

AssistantHub already references `Blobject.Core` `5.0.18` and `Blobject.AmazonS3` `5.0.18`. Prefer adding `Blobject.CIFS` and `Blobject.NFS` at `5.0.18` if restore succeeds. If NuGet resolution fails, align all Blobject package references to the highest mutually available version and document the decision in the changelog.

## Repository Type Contract

Use a real enum everywhere. Do not add string literals in handlers, SDKs, or dashboard payload builders.

- [x] Add `RepositoryTypeEnum.CIFS` with `[EnumMember(Value = "CIFS")]`.
- [x] Add `RepositoryTypeEnum.NFS` with `[EnumMember(Value = "NFS")]`.
- [x] Keep `RepositoryTypeEnum.Web` unchanged for backward compatibility.
- [x] Add a centralized repository type descriptor/mapping in the dashboard with display labels:
  - `Web` -> `Web`
  - `CIFS` -> `CIFS File Server`
  - `NFS` -> `NFS File Server`
- [x] Ensure REST, OpenAPI, Postman, C# SDK, JavaScript/TypeScript SDK, Python SDK, and MCP documentation use the same enum values.
- [x] Keep the enum extensible by centralizing defaults, labels, settings class resolution, and factory construction per repository type.

## Repository Settings Contract

The crawl-plan API should continue storing repository settings as JSON in `crawl_plans.repository_settings_json`. No database columns are needed for CIFS/NFS because the settings are already polymorphic JSON.

### Shared Settings

- [x] Keep `CrawlRepositorySettings.RepositoryType`.
- [x] Add validation helpers so each derived settings type can verify required fields before a crawl starts.
- [x] Decide whether validation belongs in model methods, handler-level validation, or crawler constructors. Implemented model validation plus constructor guard clauses so API failures are clear and scheduled crawls fail with useful messages.

### Web Settings

Existing web settings stay unchanged:

- `AuthenticationType`
- `Username`
- `Password`
- `ApiKeyHeader`
- `ApiKeyValue`
- `BearerToken`
- `UserAgent`
- `StartUrl`
- `UseHeadlessBrowser`
- `FollowLinks`
- `FollowRedirects`
- `ExtractSitemapLinks`
- `RestrictToChildUrls`
- `RestrictToSubdomain`
- `RestrictToRootDomain`
- `IgnoreRobotsTxt`
- `MaxDepth`
- `MaxParallelTasks`
- `CrawlDelayMs`

### CIFS Settings

Fields must match View's `DataRepository` CIFS fields:

- [x] `RepositoryType`: `CIFS`
- [x] `CifsHostname`: string, required, View field `CifsHostname`
- [x] `CifsUsername`: string, required unless anonymous CIFS is explicitly supported and tested, View field `CifsUsername`
- [x] `CifsPassword`: string, required unless anonymous CIFS is explicitly supported and tested, View field `CifsPassword`
- [x] `CifsShareName`: string, required, View field `CifsShareName`
- [x] `IncludeSubdirectories`: bool, default `true`, View field `IncludeSubdirectories`

Implementation class:

- [x] Add `src/AssistantHub.Core/Models/CifsCrawlRepositorySettings.cs`.
- [x] Constructor sets `RepositoryType = RepositoryTypeEnum.CIFS`.
- [x] Guard hostname/share name against null or empty values.
- [x] Add XML comments for every public property.
- [x] Do not log `CifsPassword`.

### NFS Settings

Fields must match View's `DataRepository` NFS fields:

- [x] `RepositoryType`: `NFS`
- [x] `NfsHostname`: string, required, View field `NfsHostname`
- [x] `NfsUserId`: nullable int in API if possible, required before connecting, minimum `0`, View field `NfsUserId`
- [x] `NfsGroupId`: nullable int in API if possible, required before connecting, minimum `0`, View field `NfsGroupId`
- [x] `NfsShareName`: string, required, View field `NfsShareName`
- [x] `NfsVersion`: enum `V2`, `V3`, `V4`, default `V3`, View field `NfsVersion`
- [x] `IncludeSubdirectories`: bool, default `true`, View field `IncludeSubdirectories`

Implementation classes:

- [x] Add `src/AssistantHub.Core/Enums/NfsVersionEnum.cs`.
- [x] Add `src/AssistantHub.Core/Models/NfsCrawlRepositorySettings.cs`.
- [x] Constructor sets `RepositoryType = RepositoryTypeEnum.NFS`.
- [x] Guard hostname/share name/user ID/group ID before connecting.
- [x] Add an internal converter from AssistantHub `NfsVersionEnum` to `Blobject.NFS.NfsVersionEnum`, mirroring View's `Converters.ModelsNfsVersionToBlobjectNfsVersion(...)`.

## Backend Architecture

### Package References

- [x] Add `Blobject.CIFS` to `src/AssistantHub.Core/AssistantHub.Core.csproj`.
- [x] Add `Blobject.NFS` to `src/AssistantHub.Core/AssistantHub.Core.csproj`.
- [x] Keep package versions aligned with `Blobject.Core`.
- [x] Run restore/build after package changes.

### Crawler Base Changes

`CrawlerBase` is the right shared abstraction, but file servers need lazy content retrieval.

- [x] Add a protected virtual or abstract retrieval method, for example `protected virtual Task<byte[]> RetrieveDataAsync(CrawledObject obj, CancellationToken token = default)`.
- [x] Default implementation can return `obj.Data` or `Array.Empty<byte>()`; derived classes should override when bytes are not present during enumeration.
- [x] Update `ProcessAdditionAsync(...)` to retrieve bytes lazily before S3 upload and document creation.
- [x] Ensure retrieval byte length is used for upload/document size if enumeration metadata is missing or stale.
- [x] Keep delta comparison based on metadata where possible to avoid downloading every file.
- [x] Preserve web behavior by having `WebRepositoryCrawler` return `obj.Data`.
- [x] Add cancellation checks around lazy retrieval and upload.
- [x] Keep `IsSkipFile(...)` virtual; file-server crawlers should use the base skip list, while web keeps its current override.

### Shared File-Server Base

Add a crawler layer for Blobject-backed file servers.

- [x] Add `src/AssistantHub.Core/Services/Crawlers/FileServerRepositoryCrawlerBase.cs`.
- [x] Derive it from `CrawlerBase`.
- [x] Hold a lazily created `BlobClientBase` instance.
- [x] Implement `EnumerateAsync(...)` using `BlobClientBase.EnumerateAsync(...)`.
- [x] Implement `EnumerateContentsAsync(...)` using the same enumeration path with `skip` and `maxKeys`.
- [x] Implement `RetrieveDataAsync(...)` using `BlobClientBase.GetAsync(obj.Key, token)`.
- [x] Implement `ValidateConnectivityAsync(...)` using `BlobClientBase.ValidateConnectivity(token)`.
- [x] Add detailed connectivity status messages for hostname resolution, TCP port reachability, and share/export access failures.
- [x] Map Blobject metadata into `CrawledObject`:
  - `Key`
  - `IsFolder`
  - `ContentType`
  - `ContentLength`
  - `ETag`
  - `LastModifiedUtc` from `LastUpdateUtc` when present, otherwise created/access timestamps if available
  - hash fields if Blobject exposes them
- [x] Skip folders during processing through existing `CrawlerBase.StartAsync`.
- [x] Apply the existing `CrawlFilterSettings` prefix/suffix/size/content-type filter during processing; do not duplicate that logic unless needed for enumeration performance.
- [x] Implement `IncludeSubdirectories = false` either through Blobject settings/filter support or by filtering returned keys to the share root.
- [x] Dispose the Blobject client in the full dispose pattern.

### CIFS Crawler

- [x] Add `src/AssistantHub.Core/Services/Crawlers/CifsRepositoryCrawler.cs`.
- [x] Cast `crawlPlan.RepositorySettings` to `CifsCrawlRepositorySettings`.
- [x] Create `Blobject.CIFS.CifsSettings` with `CifsHostname`, `CifsUsername`, `CifsPassword`, and `CifsShareName`.
- [x] Create `Blobject.CIFS.CifsBlobClient`.
- [x] Pass the client and settings to `FileServerRepositoryCrawlerBase`.
- [x] Ensure error messages never include password.

### NFS Crawler

- [x] Add `src/AssistantHub.Core/Services/Crawlers/NfsRepositoryCrawler.cs`.
- [x] Cast `crawlPlan.RepositorySettings` to `NfsCrawlRepositorySettings`.
- [x] Default `NfsVersion` to `V3`.
- [x] Create `Blobject.NFS.NfsSettings` with `NfsHostname`, `NfsUserId`, `NfsGroupId`, `NfsShareName`, and converted `NfsVersion`.
- [x] Create `Blobject.NFS.NfsBlobClient`.
- [x] Pass the client and settings to `FileServerRepositoryCrawlerBase`.
- [x] Ensure nullable user/group IDs are validated before `.Value` access.

### Factory and Serialization

- [x] Update `CrawlerFactory.Create(...)` to return `CifsRepositoryCrawler` for `RepositoryTypeEnum.CIFS`.
- [x] Update `CrawlerFactory.Create(...)` to return `NfsRepositoryCrawler` for `RepositoryTypeEnum.NFS`.
- [x] Replace the web-only `CrawlRepositorySettingsConverter.Read(...)` with polymorphic deserialization based on the `RepositoryType` property in the JSON payload.
- [x] Update `CrawlPlan.FromDataRow(...)` to deserialize repository settings based on the stored `repository_type`.
- [x] Add a default settings factory so new plans with missing settings get the right derived settings object for their repository type.
- [x] Preserve backward compatibility for existing web crawl plans whose settings JSON omits `RepositoryType`.
- [x] Reject mismatches where `CrawlPlan.RepositoryType` and `RepositorySettings.RepositoryType` disagree, or normalize them consistently before persistence.

### API Handler Behavior

The saved-plan connectivity and enumeration routes remain unchanged. A draft connectivity route is required for the Create/Edit Crawl Plan modal because users need to validate the currently entered repository settings and credentials before the plan exists.

- [x] Keep `PUT /v1.0/crawlplans` and `PUT /v1.0/crawlplans/{id}` as the create/update surface.
- [x] Keep `POST /v1.0/crawlplans/{id}/connectivity`; it works through the existing factory path.
- [x] Add `POST /v1.0/crawlplans/connectivity` to validate a supplied draft `CrawlPlan` payload without persisting it.
- [x] Use the same crawler factory and `ValidateConnectivityAsync(...)` path for saved and draft connectivity checks.
- [x] Return a typed `CrawlConnectivityResult` with `Success` and `Message`.
- [x] For CIFS and NFS saved-plan connectivity, validate the configured share/export root with supplied credentials rather than only host-level reachability.
- [x] For CIFS and NFS saved/draft connectivity, return diagnostic messages identifying DNS, port, or credentials/share/export access as the likely failure area.
- [x] Keep `GET /v1.0/crawlplans/{id}/enumerate`; it works through the existing factory path.
- [x] Add clear 400 responses for invalid repository settings instead of allowing constructor failures to become 500s.
- [x] Avoid adding password/token logging in crawler and validation code.
- [x] Review JSON view behavior in the dashboard. Current admin JSON/API responses can include repository secrets; this is documented until a credential abstraction/redaction pass is added.

### Database and Migrations

- [x] Confirm no table schema changes are required because `repository_type` is already a string column and settings are JSON.
- [x] Confirm no migration is required for v0.15.0 because no crawl-plan table shape changes were needed.
- [x] Confirm fresh table creation SQL defaults remain `Web`.
- [x] Confirm all database drivers persist and read enum string values `CIFS` and `NFS`.

## Frontend Plan

The Create Crawl Plan modal should only require changes in the General and Repository Settings sections.

### General Section

- [x] Replace `REPOSITORY_TYPES = ['Web']` with a central descriptor list.
- [x] Add options with labels:
  - `Web`
  - `CIFS File Server`
  - `NFS File Server`
- [x] Store enum values in form state, not labels.
- [x] When repository type changes, reset `form.Repository` to defaults for the selected type.
- [x] Preserve existing web defaults for existing plans.
- [x] Keep all other modal sections unchanged unless a bug is uncovered.

### Repository Settings Section

- [x] Render web settings only when `RepositoryType === 'Web'`.
- [x] Render CIFS settings only when `RepositoryType === 'CIFS'`:
  - Hostname
  - Username
  - Password using `PasswordInput`
  - Share Name
  - Include Subdirectories toggle
- [x] Render NFS settings only when `RepositoryType === 'NFS'`:
  - Hostname
  - User ID number input, minimum `0`
  - Group ID number input, minimum `0`
  - Share Name
  - NFS Version select: `V2`, `V3`, `V4`
  - Include Subdirectories toggle
- [x] Serialize only the properties required for the selected repository type.
- [x] Include `RepositoryType` inside `RepositorySettings`.
- [x] Parse number fields carefully so `0` remains valid for NFS user/group IDs.
- [x] Keep the save button disabled until required fields for the selected repository type are present.
- [x] Add a Repository Settings `Test Connectivity` button that validates the current draft settings with the supplied credentials before the plan is saved.
- [x] Show connectivity success/failure feedback inline without exposing sensitive credential values.
- [x] Keep layout scoped to existing modal sections; validated by production dashboard build.

### Crawler List View

- [x] Update the Type column tooltip from web-only language to repository-type language.
- [x] Update the URL column to a Source column or render type-specific source text:
  - Web: `StartUrl`
  - CIFS: `//hostname/share`
  - NFS: `hostname:/share`
- [x] Keep row actions unchanged.
- [x] Normalize the enumeration modal so ad-hoc `GET /v1.0/crawlplans/{id}/enumerate` array responses populate the All Files section.
- [x] Surface API error `Description` text in the dashboard client so missing saved enumeration files are reported as `Enumeration file not found.` instead of a generic not-found message.

## SDK Plan

### C# SDK

- [x] Add `RepositoryTypeEnum.CIFS` and `RepositoryTypeEnum.NFS`.
- [x] Add `NfsVersionEnum`.
- [x] Add `CrawlRepositorySettings` base class if not already present in the SDK.
- [x] Change `CrawlPlan.RepositorySettings` from `WebCrawlRepositorySettings` to the base settings type.
- [x] Add polymorphic JSON converter for SDK crawl repository settings.
- [x] Add `CifsCrawlRepositorySettings`.
- [x] Add `NfsCrawlRepositorySettings`.
- [x] Update C# SDK README examples for Web, CIFS, and NFS crawl plans.
- [x] Update `sdk/csharp/Test.Sdk/Tests/CrawlPlanTests.cs` to create/read/update web, CIFS, and NFS plan payloads.

### JavaScript/TypeScript SDK

- [x] Add repository enum values to exported types.
- [x] Add `NfsVersion` type/enum.
- [x] Add interfaces for `CrawlRepositorySettings`, `WebCrawlRepositorySettings`, `CifsCrawlRepositorySettings`, and `NfsCrawlRepositorySettings`.
- [x] Type `CrawlPlan.RepositorySettings` as a union.
- [x] Update README examples with CIFS and NFS payloads.
- [x] Update SDK tests for web, CIFS, and NFS plan serialization.

### Python SDK

- [x] Add `RepositoryType.CIFS` and `RepositoryType.NFS`.
- [x] Add `NfsVersion`.
- [x] Add Pydantic models for CIFS and NFS repository settings.
- [x] Use a union or model validator for `CrawlPlan.repository_settings` so returned plans hydrate to the right settings type when possible.
- [x] Update Python README examples.
- [x] Update `sdk/python/test_sdk.py` crawl-plan tests for web, CIFS, and NFS.

## REST, OpenAPI, Postman, and MCP

### REST Documentation

- [x] Update `REST_API.md` Crawl Plans section.
- [x] Add repository type enum table with `Web`, `CIFS`, and `NFS`.
- [x] Add Web settings example.
- [x] Add CIFS settings example.
- [x] Add NFS settings example.
- [x] Document that repository passwords are stored in crawl-plan settings unless a future credential abstraction is added.
- [x] Document connectivity and enumeration behavior for file servers.
- [x] Document draft connectivity testing for Create/Edit modal workflows.

### OpenAPI

- [x] Update `openapi.json` repository type enum values.
- [x] Add schemas for `CrawlRepositorySettings`, `WebCrawlRepositorySettings`, `CifsCrawlRepositorySettings`, `NfsCrawlRepositorySettings`, and `NfsVersionEnum`.
- [x] Use `oneOf` for `RepositorySettings` if the generator and dashboard API Explorer handle it cleanly.
- [x] Add CIFS and NFS examples to crawl-plan create/update request bodies.
- [x] Add `POST /v1.0/crawlplans/connectivity` with `CrawlConnectivityResult`.
- [x] Verify `openapi.json` parses and add API-suite assertions for the crawl-plan schema and full backend/OpenAPI/Postman/REST route parity.

### API Explorer

- [x] Ensure API Explorer loads the full OpenAPI path set including `POST /v1.0/crawlplans/connectivity`.
- [x] Add a draft connectivity request template that includes repository settings for the selected crawler type.
- [x] Keep Swagger UI unauthenticated while authenticated API operations retain BearerAuth inside Swagger.

### Postman

- [x] Add collection variables for CIFS host, username, password, share name.
- [x] Add collection variables for NFS host, user ID, group ID, share name, and version.
- [x] Add or update requests:
  - Create Web Crawl Plan
  - Create CIFS Crawl Plan
  - Create NFS Crawl Plan
  - Test Draft Crawl Connectivity
  - Test Crawl Connectivity
  - Enumerate Crawl Contents
  - Start Crawl
  - Stop Crawl
- [x] Keep examples free of real credentials.

### MCP

- [x] Confirm MCP crawl-plan tools marshal the updated `CrawlPlan` model without route changes.
- [x] Update MCP docs for repository type enum values and settings payloads.
- [x] Add MCP tool examples for CIFS and NFS crawl-plan creation if examples are present for web.
- [ ] Add MCP tests if the test harness validates crawl-plan model payloads.

## Tests

### Model Tests

- [x] Update `ModelSuite` repository enum test from count `1` to include `Web`, `CIFS`, and `NFS`.
- [x] Add NFS version enum round-trip test.
- [x] Add default settings tests for `CifsCrawlRepositorySettings`.
- [x] Add default settings tests for `NfsCrawlRepositorySettings`.
- [x] Add JSON round-trip tests for all three repository settings types.
- [x] Add converter tests:
  - Web payload deserializes to `WebCrawlRepositorySettings`
  - CIFS payload deserializes to `CifsCrawlRepositorySettings`
  - NFS payload deserializes to `NfsCrawlRepositorySettings`
  - Missing/unknown repository type returns a clear validation failure

### Service Tests

- [x] Add `CrawlerFactory` tests for all repository types.
- [x] Add lazy Blobject client construction so factory tests do not touch the network.
- [x] Add fake-client tests proving file-server connectivity touches repository root metadata after Blobject host validation.
- [x] Add fake-client tests proving file-server connectivity messages include share/export and principal guidance without exposing passwords.
- [x] Add fake-client tests proving file-server enumeration sends non-null Blobject prefix/suffix filters for CIFS/NFS.
- [x] Add tests for Docker loopback hostname normalization.
- [ ] Test file-server base maps metadata to `CrawledObject`.
- [ ] Test `IncludeSubdirectories = false`.
- [ ] Test lazy retrieval is called only for additions/updates that pass filters.
- [ ] Test `CrawlerBase.ProcessAdditionAsync` handles data returned by `RetrieveDataAsync`.
- [ ] Test cancellation during enumeration and retrieval.
- [ ] Test password is not logged by crawler constructors or validation errors.

### API Tests

- [x] Add API create/read tests for CIFS crawl plans.
- [x] Add API create/read tests for NFS crawl plans.
- [x] Add invalid settings tests that expect 400.
- [x] Add a draft connectivity endpoint integration test that posts unsaved repository settings.
- [ ] Add API update tests for CIFS crawl plans once the integration `TestServer` route set includes crawl-plan update.
- [ ] Add API update tests for NFS crawl plans once the integration `TestServer` route set includes crawl-plan update.
- [ ] Add connectivity route tests using a fake crawler or injected factory seam if direct network dependencies are not suitable.
- [ ] Add enumerate route tests using fake metadata.

### Integration Tests

- [!] Decide whether to add optional Samba and NFS test fixtures in Docker Compose for integration tests.
- [x] If fixtures are added, keep them outside the default production compose stack.
- [ ] Add CI-gated integration tests only when the fixture is reliable on Windows and Linux runners.
- [ ] Validate CIFS connectivity against a test share.
- [ ] Validate NFS connectivity against a test export.
- [ ] Validate full crawl creates AssistantDocument records and ingestion input for at least one small file.

### Dashboard Tests

- [x] Build dashboard after modal changes.
- [ ] Add component/unit tests if the dashboard test stack supports them.
- [ ] Manually verify modal switching:
  - Web -> CIFS
  - CIFS -> NFS
  - NFS -> Web
  - Edit existing web plan
  - Edit existing CIFS plan
  - Edit existing NFS plan
- [ ] Verify required fields and numeric parsing.
- [ ] Verify password field remains masked.
- [ ] Verify desktop, tablet, and mobile modal layouts do not overlap or clip controls.

## Documentation

- [x] Update `README.md` once implementation is complete, replacing planning language with shipped feature language.
- [x] Update `CHANGELOG.md` with implementation details.
- [x] Update `REST_API.md`.
- [x] Update `MCP_API.md`.
- [x] Update `TESTING.md` with file-server integration test instructions and current no-fixture status.
- [x] Update SDK READMEs with CIFS and NFS examples.
- [x] Keep this plan as the active implementation/progress checklist for v0.15.0.

## Docker and Factory Assets

- [x] Bump active AssistantHub image tags in `docker/compose.yaml` to `v0.15.0`.
- [x] Bump `docker/assistanthub-mcp/assistanthub-mcp.json` `SoftwareVersion` to `v0.15.0`.
- [x] Confirm `docker/factory/` has no AssistantHub software-version field requiring a bump.
- [x] Confirm Docker config still mounts `crawl-enumerations`.
- [x] Document that CIFS/NFS crawlers connect over the network and do not require compose volume mounts for remote servers.
- [x] Add `host.docker.internal` host-gateway mapping for the local Docker server container.
- [x] Increase local Docker Postgres `max_connections` to 250 so Verbex indexing bursts do not exhaust non-superuser connection slots.
- [x] If integration fixtures are added, add separate optional compose files or scripts rather than changing the default production stack.
- [x] Run `docker compose -f docker/compose.yaml config --quiet`.

## Version Work

- [x] Update active product/package/runtime versions to `0.15.0`.
- [x] Update active Docker image tags to `v0.15.0`.
- [x] Update active REST/OpenAPI version examples to `0.15.0`.
- [x] Leave historical migration, archive, and completed release-plan references intact unless they incorrectly describe the current release.
- [x] Run a final version audit and document any remaining historical `0.14.0` references. Remaining active-tree hits are historical v0.14.0 release notes/backfill text, a completed search plan reference, and a React peer dependency constraint.

## Security and Operations

- [x] Avoid logging CIFS passwords.
- [x] Avoid logging bearer tokens, API keys, or web passwords while touching shared crawler code.
- [x] Review admin JSON views and API Explorer output for sensitive repository settings and document current raw-admin behavior.
- [x] Add operational guidance for network reachability from the AssistantHub server container to CIFS/NFS hosts.
- [x] Normalize CIFS/NFS loopback hostnames such as `localhost`, `127.0.0.1`, and `::1` to `host.docker.internal` when AssistantHub runs in Docker and the alias resolves.
- [x] Add troubleshooting notes for DNS, ports, share/export names, credentials, NFS UID/GID, and NFS version mismatches.
- [ ] Add timeout/cancellation guidance if Blobject exposes configurable timeouts.
- [x] Ensure failed file-server retrieval marks objects failed and persists enumeration failure state through existing `CrawlerBase` failure handling.
- [x] Ensure crawl operation success/failure counts wait for full document ingestion completion instead of counting only S3 upload/document creation.

## Acceptance Criteria

The v0.15.0 file-server crawler work is complete when all of the following are true:

- [x] Web crawler behavior is unchanged at the shared crawler architecture level.
- [~] CIFS crawl plans can be created, edited, draft-connectivity tested, validated, enumerated, started, stopped, and deleted. Code paths are implemented; live local-Docker CIFS enumeration was validated with a host share, while broader CIFS server validation remains manual.
- [~] NFS crawl plans can be created, edited, draft-connectivity tested, validated, enumerated, started, stopped, and deleted. Code paths are implemented; live NFS export validation is still pending.
- [x] Repository type is represented by enums in backend, dashboard descriptors, SDKs, OpenAPI, Postman examples, and docs.
- [x] Repository settings deserialize to the right concrete settings type for Web, CIFS, and NFS.
- [x] `CrawlerBase` supports lazy data retrieval without forcing web or file-server crawlers into duplicate processing code.
- [x] CIFS/NFS crawlers share common Blobject-backed file-server code.
- [x] Create Crawl Plan modal changes are limited to General and Repository Settings except for necessary source display polish in the crawler list.
- [~] Tests cover model serialization, factory behavior, API create/read/invalid payloads, SDK payloads, OpenAPI, and Postman. Deeper fake-client enumeration/lazy-retrieval and API update tests remain open.
- [x] OpenAPI, Postman, REST docs, MCP docs, SDK READMEs, README, CHANGELOG, Docker tags, and package versions are updated.
- [x] `dotnet build src/AssistantHub.sln --no-restore` passes after restore has been run for any new packages.
- [x] Dashboard build passes.
- [~] SDK build/compile commands pass for C#, JavaScript/TypeScript, and Python. Live SDK integration tests were updated but not run against a live AssistantHub server in this pass.
- [x] Docker compose config validation passes.

## Implementation Order

1. [x] Add backend enums/settings/converters and tests.
2. [x] Extend `CrawlerBase` with lazy retrieval and preserve web behavior.
3. [x] Add Blobject package references and file-server crawler base.
4. [x] Add CIFS and NFS crawler implementations.
5. [x] Register CIFS and NFS in `CrawlerFactory`.
6. [x] Add API validation and backend tests.
7. [x] Update dashboard General and Repository Settings sections.
8. [x] Update SDK models, enums, examples, and tests.
9. [x] Update REST, OpenAPI, Postman, MCP, README, CHANGELOG, and testing docs.
10. [x] Run build/test/docker validation.
11. [x] Run final version and sensitive-data audits.

## Open Decisions

- [!] Confirm whether CIFS anonymous access should be supported. Default implementation treats username/password as required unless the library behavior and product policy say otherwise.
- [!] Confirm whether to add optional Samba/NFS integration fixtures for CI or keep integration validation manual for v0.15.0.
- [!] Confirm whether admin JSON views should redact repository passwords now or follow the current web crawler pattern until a broader credentials feature is introduced.
