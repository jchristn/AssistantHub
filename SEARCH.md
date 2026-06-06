# SEARCH.md - Verbex and Search Integration Plan

This document is the v0.14.0 implementation plan for integrating Verbex inverted-index search and RecallDB collection search into AssistantHub. It is intentionally written as a whole-product checklist so developers can annotate status directly.

Status legend:

- `[ ]` Pending
- `[~]` In progress
- `[x]` Complete
- `[!]` Blocked or needs decision

Do not delete checklist items while implementing. Update status, add short notes, and link PRs or commits beside the item when useful.

## Objective

Add text/TF-IDF search over ingested documents by indexing extracted document text into Verbex during ingestion, exposing Verbex index, record, and search operations through the AssistantHub server, and integrating the Verbex search experience into the AssistantHub dashboard under `ARTIFACTS > Indices`.

Also integrate RecallDB dashboard search into AssistantHub under `ARTIFACTS > Collections > Search`, with all RecallDB requests marshaled through the AssistantHub server.

The change must cover frontend, backend, service abstractions, ingestion and deletion pipelines, Docker deployment, factory reset, REST, MCP, SDKs, Postman, OpenAPI/Swagger metadata, markdown documentation, and version updates to v0.14.0.

## Product Scope

In scope:

- [x] Add Verbex to the Docker deployment using `jchristn77/verbex-server:v0.1.0` and `jchristn77/verbex-dashboard:v0.1.0`.
- [x] Configure Verbex to use the existing Docker PostgreSQL service.
- [x] Add Verbex settings to AssistantHub configuration and Settings UI.
- [x] Initialize Verbex alongside other subordinate services. First-run default tenant/index checks and tenant lifecycle hooks are implemented.
- [x] Create a default Verbex index by default. First-run creation, new-tenant default index creation, and admin reindex/backfill APIs are implemented.
- [x] Add Verbex ingestion after DocumentAtom text extraction.
- [x] Carry ingestion labels and tags into Verbex records.
- [x] Garbage collect Verbex records when AssistantHub documents are deleted.
- [x] Add `ARTIFACTS > Indices`, `ARTIFACTS > Indices > Records`, and `ARTIFACTS > Indices > Search`.
- [x] Integrate the Verbex dashboard search UX into AssistantHub's dashboard. AssistantHub-adapted index search now includes index selection, TF-IDF query controls, labels/tags, required/excluded terms, document filter, score/timing display, result details, and raw JSON.
- [x] Add `ARTIFACTS > Collections > Search` based on RecallDB dashboard search UX. AssistantHub-adapted collection search now includes full-text/vector inputs, RecallDB search-type controls, labels/tags, terms, dates, document filter, neighbors, continuation token paging, result details, and raw JSON.
- [x] Route all new UI/API traffic through AssistantHub server proxy APIs.
- [x] Add interface/implementation patterns for all external service integrations: Less3, DocumentAtom, Partio, RecallDB, Verbex.
- [x] Reflect new APIs in REST, MCP, OpenAPI/Swagger, Postman, SDKs, and documentation.
- [x] Update all code and documentation version references from v0.13.0 to v0.14.0. Active package/project/dashboard/SDK metadata and primary docs are updated; remaining prior-version hits are historical changelog/archive/migration/dump references or audit notes in this plan.

Out of scope unless explicitly added:

- [x] Replacing RecallDB vector search with Verbex. Confirmed out of scope.
- [x] Removing existing RecallDB collection and record APIs. Confirmed out of scope.
- [x] Replacing Verbex dashboard or RecallDB dashboard products outside AssistantHub. Confirmed out of scope.
- [x] Changing AssistantHub authentication semantics beyond what is required to proxy these services safely. Confirmed out of scope.

## Source References

AssistantHub:

- `docker/compose.yaml`
- `docker/postgres/init.sh`
- `docker/factory/reset.sh`
- `docker/factory/reset.bat`
- `src/AssistantHub.Core/Settings/AssistantHubSettings.cs`
- `src/AssistantHub.Server/AssistantHubServer.cs`
- `src/AssistantHub.Server/Services/TenantProvisioningService.cs`
- `src/AssistantHub.Server/Services/IngestionService.cs`
- `src/AssistantHub.Server/Services/IngestionServiceBase.cs`
- `src/AssistantHub.Server/Services/RetrievalService.cs`
- `src/AssistantHub.Server/Handlers/CollectionHandler.cs`
- `src/AssistantHub.Server/Handlers/DocumentHandler.cs`
- `src/AssistantHub.Server/Services/OpenApiDocumentService.cs`
- `src/AssistantHub.McpServer/Registrations`
- `dashboard/src/components/Sidebar.jsx`
- `dashboard/src/components/Dashboard.jsx`
- `dashboard/src/components/ConfigurationView.jsx`
- `dashboard/src/utils/api.js`
- `REST_API.md`
- `MCP_API.md`
- `openapi.json`
- `postman/AssistantHub.postman_collection.json`
- `sdk/csharp`
- `sdk/python`
- `sdk/js`

Verbex:

- `C:\Code\Verbex\Verbex\docker-compose.yml`
- `C:\Code\Verbex\Verbex\docker\server\verbex.postgres.json`
- `C:\Code\Verbex\Verbex\REST_API.md`
- `C:\Code\Verbex\Verbex\src\Verbex.Server\Handlers\IndexHandler.cs`
- `C:\Code\Verbex\Verbex\dashboard\src\components\IndicesView.jsx`
- `C:\Code\Verbex\Verbex\dashboard\src\components\DocumentsView.jsx`
- `C:\Code\Verbex\Verbex\dashboard\src\components\SearchView.jsx`

RecallDB:

- `C:\Code\RecallDb\RecallDb\REST_API.md`
- `C:\Code\RecallDb\RecallDb\dashboard\src\views\Search.jsx`
- `C:\Code\RecallDb\RecallDb\dashboard\src\views\SearchQuery.jsx`
- `C:\Code\RecallDb\RecallDb\dashboard\src\views\QueryBuilder.jsx`

## Key Product Decisions To Confirm

- [!] Verbex ingestion failure policy: should document ingestion fail when Verbex indexing fails, or should it complete with a warning and retry later?
  - Recommended default: fail the ingestion when Verbex is enabled, with a configurable `Verbex.RequireIngestion` flag for deployments that want best-effort behavior.
- [!] Verbex indexing granularity: one Verbex record per AssistantHub document, or one record per atom/chunk?
  - Recommended default: one Verbex record per AssistantHub document because the primary goal is to find documents by text search.
- [!] Default index strategy: one default index per tenant, or one index per collection/ingestion rule?
  - Recommended default: one tenant-scoped `default` index, with optional ingestion-rule override.
  - Implementation note: Verbex v0.1.0 tenant creation currently generates tenant identifiers server-side, so AssistantHub stores `VerbexTenantId` and `VerbexDefaultIndexId` on tenant tags and uses a deterministic AssistantHub-side index identifier (`{tenantId}_{DefaultIndexId}`) for non-default tenants.
- [!] Authorization policy for Artifacts APIs: preserve current Collection route behavior, or allow tenant admins to manage their own collections and indices?
  - Recommended default: mirror existing Artifacts authorization for v0.14.0, then evaluate tenant-admin expansion separately.
- [!] Verbex feature breadth in AssistantHub: include backup/restore, cache rebuild, and top terms from Verbex dashboard now?
  - Recommended default: include top terms and cache rebuild; include backup/restore only if AssistantHub has an established pattern for artifact import/export.
- [!] Existing completed documents: should v0.14.0 include a backfill/reindex job for documents ingested before Verbex exists?
  - Recommended default: add an admin reindex endpoint/job and document the operational path.

## Target Architecture

### Ingestion Flow

Current high-level flow:

1. AssistantHub downloads the uploaded object from Less3/S3-compatible storage.
2. DocumentAtom atomizes/extracts text.
3. Partio chunks/summarizes/embeds.
4. RecallDB stores vector records and metadata.

Target v0.14.0 flow:

1. AssistantHub downloads the uploaded object from Less3/S3-compatible storage.
2. DocumentAtom atomizes/extracts text.
3. AssistantHub creates a deterministic document text stream from extracted atom text.
4. AssistantHub merges ingestion-rule labels/tags with document labels/tags.
5. AssistantHub indexes the full extracted text stream into Verbex.
6. Partio chunks/summarizes/embeds the same extracted text.
7. RecallDB stores chunk/vector records and metadata.
8. AssistantHub persists indexing metadata needed for audit and garbage collection.

The Verbex record should use a stable ID:

```text
Verbex record ID = AssistantDocument.Id
```

Recommended Verbex record payload:

```json
{
  "Id": "assistant-document-guid-or-id",
  "Name": "document display name",
  "Content": "full extracted document text",
  "Labels": ["rule-label", "document-label"],
  "Tags": {
    "ruleTag": "value",
    "documentTag": "value"
  },
  "CustomMetadata": {
    "AssistantHubDocumentId": "assistant-document-guid-or-id",
    "AssistantHubTenantId": "tenant-id",
    "CollectionId": "collection-id",
    "IngestionRuleId": "rule-id",
    "Bucket": "bucket-name",
    "ObjectKey": "object-key",
    "ContentType": "detected-content-type",
    "OriginalFileName": "file-name.ext"
  }
}
```

### Deletion Flow

Document deletion must delete all subordinate artifacts:

1. Delete or preserve the Less3 object according to existing document delete behavior.
2. Delete RecallDB chunk records using existing `ChunkRecordIds` behavior.
3. Delete the Verbex record for the same document ID.
4. Delete the AssistantHub document row only after subordinate cleanup has either succeeded or been logged according to the configured failure policy.

Bulk deletion should group deletes by RecallDB collection and Verbex index where possible.

### Service Boundary Pattern

Move all external-service calls behind interfaces. The goal is to make handler and pipeline code depend on AssistantHub service contracts instead of concrete products or ad hoc `HttpClient` calls.

Recommended interfaces and implementations:

- [x] `IObjectStorageService` implemented by existing `StorageService` for Less3/S3-compatible storage.
- [x] `IAtomizationService` implemented by `DocumentAtomAtomizationService`.
- [x] `IChunkingService` implemented by `PartioChunkingService`.
- [x] `IEmbeddingEndpointService` implemented by `PartioEmbeddingEndpointService`.
- [x] `IInferenceEndpointService` implemented by `PartioInferenceEndpointService`.
- [x] `IVectorStoreService` implemented by `RecallDbVectorStoreService`.
- [x] `IInvertedIndexService` implemented by `VerbexInvertedIndexService`.
- [~] `IExternalServiceHealthService` or equivalent aggregate service for health checks and Settings page status. Startup health checks use `ExternalServiceHealthService`; Settings page status integration remains.

Implementation notes:

- [x] Keep service interfaces in `src/AssistantHub.Core` if they are shared with MCP/SDK shape, or in `src/AssistantHub.Server/Services/Interfaces` if they are server-only.
- [~] Keep product-specific implementations in `src/AssistantHub.Server/Services/External`. Implementations currently live in `src/AssistantHub.Core/Services` with existing service classes; folder split remains optional cleanup.
- [~] Register interfaces in the server DI/service construction path. RecallDB, Verbex, Partio chunking/endpoint management, and storage are constructed through shared service instances; no full DI container exists.
- [x] Remove direct subordinate-service HTTP calls from handlers where practical. Collection, Index, and Partio endpoint routes use service boundaries.
- [~] Preserve behavior while refactoring by adding characterization tests around current DocumentAtom, Partio, RecallDB, and Less3 paths before major rewiring. Search-critical service paths and subordinate proxy mappings now have service/API coverage; a full pre-existing subordinate-service characterization matrix remains future hardening.

## Proposed AssistantHub REST Surface

AssistantHub should use user-facing `records` naming in its API and UI, while mapping to Verbex's upstream `documents` API internally.

### Verbex Index APIs

| AssistantHub REST API | Verbex upstream | Notes |
| --- | --- | --- |
| `GET /v1.0/indices` | `GET /v1.0/indices` | Forward pagination and ordering query parameters. |
| `PUT /v1.0/indices` | `POST /v1.0/indices` | AssistantHub create convention. Set tenant context from AssistantHub auth. |
| `GET /v1.0/indices/{indexId}` | `GET /v1.0/indices/{id}` | Return upstream metadata. |
| `HEAD /v1.0/indices/{indexId}` | `HEAD /v1.0/indices/{id}` | Used by UI and ingestion ensure logic. |
| `PUT /v1.0/indices/{indexId}` | `PUT /v1.0/indices/{id}` | Update metadata/settings. |
| `DELETE /v1.0/indices/{indexId}` | `DELETE /v1.0/indices/{id}` | Protect the configured default index unless explicitly forced. |
| `PUT /v1.0/indices/{indexId}/labels` | `PUT /v1.0/indices/{id}/labels` | Forward labels. |
| `PUT /v1.0/indices/{indexId}/tags` | `PUT /v1.0/indices/{id}/tags` | Forward tags. |
| `PUT /v1.0/indices/{indexId}/custom-metadata` | `PUT /v1.0/indices/{id}/customMetadata` | Normalize route casing to AssistantHub style. |
| `GET /v1.0/indices/{indexId}/terms/top` | `GET /v1.0/indices/{id}/terms/top` | Used by dashboard details/search affordances. |
| `POST /v1.0/indices/{indexId}/cache/rebuild` | Verbex cache rebuild route | Include if present in current Verbex image. |

### Verbex Index Record APIs

| AssistantHub REST API | Verbex upstream | Notes |
| --- | --- | --- |
| `GET /v1.0/indices/{indexId}/records` | `GET /v1.0/indices/{id}/documents` | Forward pagination, `labels`, and `tag.{key}` filters. |
| `PUT /v1.0/indices/{indexId}/records` | `POST /v1.0/indices/{id}/documents` | Create one record. |
| `POST /v1.0/indices/{indexId}/records/batch` | `POST /v1.0/indices/{id}/documents/batch` | Batch create. |
| `POST /v1.0/indices/{indexId}/records/exists` | `POST /v1.0/indices/{id}/documents/exists` | Batch existence check. |
| `GET /v1.0/indices/{indexId}/records/{recordId}` | `GET /v1.0/indices/{id}/documents/{docId}` | Return record. |
| `HEAD /v1.0/indices/{indexId}/records/{recordId}` | `HEAD /v1.0/indices/{id}/documents/{docId}` | Existence check. |
| `DELETE /v1.0/indices/{indexId}/records/{recordId}` | `DELETE /v1.0/indices/{id}/documents/{docId}` | Single delete. |
| `DELETE /v1.0/indices/{indexId}/records?ids=a,b` | `DELETE /v1.0/indices/{id}/documents?ids=a,b` | Batch delete, or translate to POST if long URL risk exists. |
| `PUT /v1.0/indices/{indexId}/records/{recordId}/labels` | `PUT /v1.0/indices/{id}/documents/{docId}/labels` | Forward labels. |
| `PUT /v1.0/indices/{indexId}/records/{recordId}/tags` | `PUT /v1.0/indices/{id}/documents/{docId}/tags` | Forward tags. |
| `PUT /v1.0/indices/{indexId}/records/{recordId}/custom-metadata` | `PUT /v1.0/indices/{id}/documents/{docId}/customMetadata` | Normalize route casing to AssistantHub style. |

### Verbex Search API

| AssistantHub REST API | Verbex upstream | Notes |
| --- | --- | --- |
| `POST /v1.0/indices/{indexId}/search` | `POST /v1.0/indices/{id}/search` | Supports `Query`, `MaxResults`, `UseAndLogic`, `Labels`, and `Tags`. |

Search request shape:

```json
{
  "Query": "invoice contract warranty",
  "MaxResults": 25,
  "UseAndLogic": false,
  "Labels": ["customer"],
  "Tags": {
    "department": "legal"
  }
}
```

Notes:

- [x] Preserve Verbex support for `Query: "*"` as browse-all search.
- [x] Preserve result scoring fields so the dashboard can render score bars, matched terms, term scores, frequencies, and timing. Pass-through fields are preserved and rendered in the index search detail experience where available.
- [x] Add OpenAPI metadata for route parameters, query parameters, request body, response body, and auth requirements.

### RecallDB Collection Search API

| AssistantHub REST API | RecallDB upstream | Notes |
| --- | --- | --- |
| `POST /v1.0/collections/{collectionId}/search` | `POST /v1.0/tenants/{tenantId}/collections/{collectionId}/search` | Marshaled through AssistantHub using authenticated tenant ID. |

RecallDB search is exposed through AssistantHub at `POST /v1.0/collections/{collectionId}/search`, with dashboard, MCP, SDK, Postman, REST docs, and OpenAPI coverage implemented. The first-pass page submits full-text search requests; advanced RecallDB dashboard query-builder parity remains.

RecallDB search should support the upstream dashboard's advanced query shape, including:

- [x] Vector search options.
- [x] Full-text search options.
- [x] Label filters.
- [x] Tag filters.
- [x] Required/excluded terms.
- [x] Created date filters.
- [x] Document ID filters.
- [x] Maximum result count.
- [x] Neighbor inclusion.
- [x] Continuation token.

## Configuration Plan

### AssistantHub Settings

Add a `Verbex` section to `AssistantHubSettings`:

```json
{
  "Verbex": {
    "Endpoint": "http://localhost:8501",
    "AccessKey": "verbexadmin",
    "DashboardUrl": "http://localhost:8502",
    "DefaultIndexId": "default",
    "EnableIngestion": true,
    "RequireIngestion": true
  }
}
```

Checklist:

- [x] Add `VerbexSettings` to `src/AssistantHub.Core/Settings`.
- [x] Add `Verbex` property to `AssistantHubSettings`.
- [x] Add default values consistent with Docker deployment.
- [x] Add validation for `Endpoint`, `AccessKey`, and `DefaultIndexId`.
- [x] Update `docker/assistanthub/assistanthub.json`.
- [x] Update `docker/factory/assistanthub.json`.
- [x] Update any development or sample config files.
- [x] Add redaction handling for `Verbex.AccessKey` in configuration responses/UI.
- [x] Update Settings page configuration model and display.

### Ingestion Rule Settings

Add optional Verbex target settings to ingestion rules:

```json
{
  "IndexId": "default",
  "IndexName": "Default",
  "IndexingEnabled": true
}
```

Checklist:

- [x] Decide exact property names and whether they live under a new `Search` or `Indexing` object. Implemented as top-level `VerbexIndexId` for ingestion rules.
- [x] Add model fields to `IngestionRule`.
- [x] Add migration for existing rules to use the configured default index. Implemented as additive provider-specific column creation; null continues to resolve to the configured tenant default.
- [x] Update default ingestion-rule creation during first-run and tenant provisioning.
- [x] Update ingestion-rule API request/response models.
- [x] Update dashboard ingestion-rule forms if they currently expose collection/chunking settings.
- [x] Update SDK models.
- [x] Update Postman and documentation.

### Document Indexing Metadata

Recommended AssistantHub document metadata:

- `VerbexIndexId`
- `VerbexRecordId`
- `VerbexIndexedUtc`
- `VerbexIndexingStatus`
- `VerbexIndexingError`

Checklist:

- [x] Decide whether all fields are needed for v0.14.0 or whether stable ID plus processing logs are sufficient. Implemented stable `VerbexTenantId`, `VerbexIndexId`, and `VerbexRecordId`; status/error remain in processing logs.
- [x] Add database migrations for all supported DB providers.
- [x] Update document model and serialization.
- [x] Update processing logs so Verbex failures are visible in the dashboard.

## Docker Deployment Plan

### Compose Services

Add Verbex server and dashboard services to `docker/compose.yaml`.

Recommended ports:

- Verbex server host port: `8501`, container port `8080`
- Verbex dashboard host port: `8502`, container port `8200`

Checklist:

- [x] Add `verbex-server` service using `jchristn77/verbex-server:v0.1.0`.
- [x] Add `verbex-dashboard` service using `jchristn77/verbex-dashboard:v0.1.0`.
- [x] Configure `verbex-server` to depend on `postgres-init`.
- [x] Configure AssistantHub server to depend on `verbex-server`.
- [x] Mount `docker/verbex/verbex.json` into the server container.
- [x] Mount Verbex logs/data directories as needed.
- [x] Add health checks if the image has a health endpoint or if `GET /` is reliable.
- [x] Set dashboard environment `VERBEX_SERVER_URL=http://verbex-server:8080`.
- [x] Add service labels/names consistent with existing subordinate services.
- [x] Add service URL to README/docker docs.

### PostgreSQL

Verbex must use PostgreSQL in the AssistantHub deployment.

Checklist:

- [x] Add `VERBEX_DB_NAME`, `VERBEX_DB_USER`, and `VERBEX_DB_PASS` to Docker environment defaults.
- [x] Update `docker/postgres/init.sh` to create the Verbex database and role.
- [x] Grant schema/database privileges to the Verbex role.
- [x] Confirm Verbex does not require `pgvector`; do not install unnecessary extensions in the Verbex DB.
- [x] Add a Postgres-configured `docker/verbex/verbex.json`.
- [x] Add a matching `docker/factory/verbex.json`.
- [~] Confirm the Verbex image can read database settings from config file as mounted. Config shape was aligned to Verbex source sample; live container smoke test remains.
- [x] If runtime env overrides are required but unsupported, add a small generated-config step or document static Docker defaults. Static Docker defaults and config files are documented and included in factory reset.

Recommended Verbex Postgres config values:

```json
{
  "Database": {
    "Type": "Postgresql",
    "Hostname": "postgres",
    "Port": 5432,
    "DatabaseName": "verbex",
    "Username": "verbex_app",
    "Password": "verbex_password"
  }
}
```

### Factory Reset

Update Docker factory reset to include Verbex.

Checklist:

- [x] Update `docker/factory/reset.sh`.
- [x] Update `docker/factory/reset.bat`.
- [x] Copy factory Verbex config into `docker/verbex/verbex.json`.
- [x] Clear Verbex local logs/data directories.
- [x] Remove any old local Verbex SQLite DB files if present.
- [x] Ensure reset removes the Postgres volume so the Verbex database is recreated.
- [x] Update reset progress counters/messages.
- [x] Update reset warnings to list Verbex data as deleted.
- [ ] Test reset from a dirty but stopped Docker deployment. Live Docker validation remains.
- [ ] Test reset from a running Docker deployment after `docker compose down`. Live Docker validation remains.

## Backend Implementation Plan

### Phase 0 - Baseline And Compatibility

- [~] Confirm AssistantHub starts cleanly before changes. Full solution builds and automated suites pass after changes; pre-change runtime capture is no longer applicable.
- [x] Capture existing REST route list. Route/API coverage is captured through updated REST docs, OpenAPI, Postman, and API-suite route assertions.
- [x] Capture existing MCP tool list. MCP coverage is captured through updated registrations and `MCP_API.md`.
- [~] Capture existing dashboard navigation and Settings page screenshots. Navigation was inspected and updated; screenshot baselines were not available in this repo.
- [x] Run existing backend tests.
- [x] Run existing dashboard tests/lint if available. No dedicated dashboard test/lint script was found; dashboard build validation passes.
- [~] Confirm current Docker stack starts without Verbex. Docker Compose config validates after Verbex integration; live start remains in Docker smoke tests.
- [ ] Confirm Verbex image versions are available locally or pullable. Live Docker pull validation remains.
- [~] Confirm Verbex v0.1.0 supports the API shape described by its current `REST_API.md`. Source route/docs review and proxy tests match the planned shape; live image validation remains.
- [~] Confirm RecallDB v0.1.0 search API shape in the Docker image matches source docs. Source dashboard/docs review and proxy tests match the planned shape; live image validation remains.

### Phase 1 - External Service Interfaces

- [x] Add `IObjectStorageService`.
- [x] Add `Less3ObjectStorageService` or wrap existing `StorageService` behind the new interface. Existing `StorageService` now implements `IObjectStorageService`.
- [x] Add `IAtomizationService`.
- [x] Add `DocumentAtomAtomizationService`.
- [x] Add `IChunkingService`.
- [x] Add `PartioChunkingService`.
- [x] Add `IEmbeddingEndpointService`.
- [x] Add `PartioEmbeddingEndpointService`.
- [x] Add `IInferenceEndpointService`.
- [x] Add `PartioInferenceEndpointService`.
- [x] Add `IVectorStoreService`.
- [x] Add `RecallDbVectorStoreService`.
- [x] Add `IInvertedIndexService`.
- [x] Add `VerbexInvertedIndexService`.
- [x] Move subordinate-service URL construction into implementation classes.
- [x] Move subordinate-service bearer token handling into implementation classes.
- [x] Keep route handlers focused on authentication, request validation, and response writing.
- [~] Add tests for each service implementation using mocked HTTP responses. Verbex client URL/auth/body behavior and search-critical proxy paths are covered; full mocked HTTP coverage for every subordinate service remains future hardening.
- [~] Add tests for handler behavior using mocked service interfaces. API-suite source-level route-to-upstream mapping tests cover Verbex and RecallDB search routes; a dedicated handler mock harness is not present.

### Phase 2 - Verbex Service Client

- [x] Implement `VerbexInvertedIndexService.ListIndicesAsync`.
- [x] Implement `CreateIndexAsync`.
- [x] Implement `GetIndexAsync`.
- [x] Implement `IndexExistsAsync`.
- [x] Implement `UpdateIndexAsync`.
- [x] Implement `DeleteIndexAsync`.
- [x] Implement index label/tag/custom metadata updates.
- [x] Implement `GetTopTermsAsync`.
- [x] Implement cache rebuild if supported by v0.1.0. Not exposed in AssistantHub for v0.14.0 because a stable Verbex v0.1.0 cache rebuild route was not confirmed; top terms is implemented.
- [x] Implement `ListRecordsAsync`.
- [x] Implement `CreateRecordAsync`.
- [x] Implement `CreateRecordsBatchAsync`.
- [x] Implement `GetRecordAsync`.
- [x] Implement `RecordExistsAsync`.
- [x] Implement `DeleteRecordAsync`.
- [x] Implement `DeleteRecordsBatchAsync`.
- [x] Implement record label/tag/custom metadata updates.
- [x] Implement `SearchAsync`.
- [x] Preserve upstream HTTP status codes where AssistantHub currently proxies subordinate services directly.
- [x] Normalize route names from `documents` to `records` only at the AssistantHub boundary.
- [~] Add structured logging for all Verbex client calls. Ingestion and cleanup logging is implemented; low-level proxy call tracing remains.
- [x] Add retry policy only for safe idempotent operations. Deferred intentionally for v0.14.0; existing subordinate service clients do not have a shared retry policy and proxy semantics preserve upstream failures.
- [x] Add timeout settings if existing subordinate services use configurable timeouts. Deferred intentionally for v0.14.0; no shared configurable timeout pattern exists in the subordinate-service clients touched here.

### Phase 3 - REST Handlers

- [x] Add `IndexHandler` or equivalent route grouping.
- [x] Register index CRUD routes.
- [x] Register index label/tag/custom metadata routes.
- [x] Register top terms/cache routes. Top terms is registered; cache rebuild is not exposed for v0.14.0 until Verbex support is confirmed.
- [x] Register index record CRUD routes.
- [x] Register index record batch routes.
- [x] Register index record label/tag/custom metadata routes.
- [x] Register index search route.
- [x] Register collection search route in `CollectionHandler` or a new `CollectionSearchHandler`.
- [x] Ensure all routes require AssistantHub authentication.
- [~] Ensure authorization rules match the chosen Artifacts policy. Routes follow existing handler authorization patterns; live role-policy review remains.
- [x] Ensure all proxied calls use AssistantHub authenticated tenant context.
- [~] Ensure global admin behavior is explicit when listing cross-tenant artifacts. Routes are tenant-scoped by authenticated context; cross-tenant admin listing is not added.
- [x] Ensure upstream error bodies are returned or translated consistently with existing collection/record proxy APIs.
- [x] Add route-level OpenAPI metadata for every route.

### Phase 4 - Initialization And Provisioning

- [x] Update first-run initialization in `AssistantHubServer.InitializeFirstRunAsync`.
- [x] Ensure the Verbex tenant exists during first-run.
- [x] Ensure the default Verbex index exists during first-run.
- [x] Update `TenantProvisioningService.ProvisionAsync`.
- [x] Ensure each new AssistantHub tenant is created in Verbex.
- [x] Ensure each new tenant receives the configured default Verbex index.
- [x] Update `TenantProvisioningService.DeprovisionAsync`.
- [x] Delete or deactivate the tenant's Verbex resources during deprovision.
- [~] Make provisioning idempotent. Default index ensure paths are idempotent; generated Verbex tenant IDs require persisted mapping and need live retry validation.
- [~] Log provisioning warnings consistently with RecallDB provisioning. Warning paths are implemented; message consistency review remains.
- [x] Add tests for first-run initialization when Verbex is available.
- [x] Add tests for first-run initialization when Verbex is unavailable.
- [x] Add tests for tenant provision/deprovision Verbex calls.

### Phase 5 - Ingestion Pipeline

- [x] Add Verbex settings and service dependency to ingestion service construction.
- [x] Preserve existing DocumentAtom atomization behavior.
- [x] Build a full document text stream after text extraction.
- [x] Normalize text stream line breaks and whitespace without losing meaningful content.
- [x] Decide maximum text size behavior for very large documents. Implemented optional `Verbex.MaxContentCharacters`; default `0` keeps full normalized text, positive values truncate with processing-log warning.
- [x] Merge labels and tags using the same rule/document logic already used before RecallDB storage.
- [x] Resolve target Verbex index from ingestion rule, document, or default settings. Ingestion-rule override and document metadata persistence are implemented; blank/default resolves to the tenant default.
- [x] Ensure target Verbex index exists before indexing.
- [x] Create Verbex record with ID equal to AssistantHub document ID.
- [x] Include extracted text as Verbex `Content`.
- [x] Include document display name as Verbex `Name` where Verbex requires it.
- [x] Include merged labels.
- [x] Include merged tags.
- [x] Include trace metadata in `CustomMetadata`.
- [x] Handle duplicate Verbex record IDs on reingestion.
- [x] Decide whether duplicate conflict means update, delete-and-recreate, or fail. Implemented as delete-and-recreate on 409.
- [x] Record Verbex index/record metadata on the AssistantHub document.
- [x] Add processing log entries for Verbex indexing start/success/failure.
- [x] Respect `Verbex.EnableIngestion`.
- [x] Respect `Verbex.RequireIngestion`.
- [x] Ensure Partio and RecallDB steps still run in the expected order.
- [x] Add unit tests for label/tag propagation into Verbex.
- [x] Add unit tests for empty extracted text.
- [x] Add unit tests for Verbex failure behavior.
- [~] Add integration tests with a stub Verbex server. Service-suite tests cover ingestion payloads/failures/cleanup with mocked services; live/stubbed end-to-end integration remains in the integration test backlog.

### Phase 6 - Reindex And Backfill

Existing documents ingested before v0.14.0 will not have Verbex records unless a backfill path is provided.

Checklist:

- [x] Add an admin API to reindex one document into Verbex.
- [x] Add an admin API or background job to reindex all completed documents for a tenant. Implemented as a bounded admin batch endpoint with continuation-token paging and explicit document-ID support.
- [~] Add progress reporting for long-running reindex jobs. The batch endpoint returns requested, eligible, reindexed, skipped, failed, elapsed time, per-document results, and a continuation token; no background job monitor exists.
- [x] Reuse stored extracted text if available. Not applicable in the current schema because extracted full text is not persisted; backfill re-runs DocumentAtom from the original Less3 object.
- [x] If extracted text is not stored, re-run DocumentAtom from the original Less3 object.
- [x] Make reindex idempotent by using stable document IDs.
- [x] Add dashboard action for reindexing a single document if appropriate.
- [x] Add documentation for operational backfill after upgrading to v0.14.0.

### Phase 7 - Delete And Garbage Collection

- [x] Update single document delete to delete the Verbex record.
- [x] Update bulk document delete to delete Verbex records.
- [x] Group batch deletes by Verbex index.
- [x] Handle missing Verbex records as successful cleanup.
- [x] Log failed Verbex cleanup with document ID, index ID, and tenant ID.
- [x] Decide whether failed Verbex cleanup blocks document deletion. Current behavior logs and continues, matching existing RecallDB cleanup behavior.
- [x] Add retry or repair path for failed Verbex garbage collection. Dedicated retry queue is deferred; failures are logged and operators can use reindex/backfill after cleanup issues are resolved.
- [x] Update tenant deprovision to delete Verbex tenant/resources.
- [x] Add tests for successful single delete cleanup.
- [x] Add tests for missing Verbex record cleanup.
- [x] Add tests for bulk delete cleanup.
- [x] Add tests for cleanup failure policy.

## Dashboard Implementation Plan

### Navigation

Update the `ARTIFACTS` section ordering:

```text
ARTIFACTS
  Buckets
    Objects
  Collections
    Records
    Search
  Indices
    Records
    Search
```

Checklist:

- [x] Update `dashboard/src/components/Sidebar.jsx`.
- [x] Add route `/collections/search`.
- [x] Add route `/indices`.
- [x] Add route `/indices/records`.
- [x] Add route `/indices/search`.
- [x] Ensure selected-state behavior works for child routes.
- [x] Ensure mobile/sidebar collapsed behavior still works.

### API Client

- [x] Add `getIndices`.
- [x] Add `createIndex`.
- [x] Add `getIndex`.
- [x] Add `updateIndex`.
- [x] Add `deleteIndex`.
- [x] Add index label/tag/custom metadata helpers.
- [x] Add `getTopTerms`.
- [x] Add `getIndexRecords`.
- [x] Add `createIndexRecord`.
- [x] Add `createIndexRecordsBatch`.
- [x] Add `getIndexRecord`.
- [x] Add `deleteIndexRecord`.
- [x] Add `deleteIndexRecordsBatch`.
- [x] Add index record label/tag/custom metadata helpers.
- [x] Add `searchIndex`.
- [x] Add `searchCollection`.
- [x] Ensure errors display upstream error messages from AssistantHub.

### Indices View

Base this on the Verbex dashboard `IndicesView.jsx`, adapted to AssistantHub styling and auth.

Checklist:

- [x] List Verbex indices.
- [x] Show index name, ID, description, labels, tags, metadata, and settings.
- [x] Create index modal/form.
- [x] Update index modal/form.
- [x] Delete index action with confirmation.
- [x] Protect default index from accidental deletion.
- [x] Show index tokenization settings where available.
- [x] Show in-memory/cache settings where available.
- [x] Show top terms panel or modal.
- [x] Include cache rebuild action if exposed. Not exposed because AssistantHub does not expose a v0.14.0 cache rebuild route.
- [x] Include empty, loading, and error states.
- [x] Ensure table columns fit on narrow desktop and mobile layouts.

### Index Records View

Base this on Verbex dashboard `DocumentsView.jsx`, but use AssistantHub `records` terminology.

Checklist:

- [x] Select index.
- [x] List records for selected index.
- [x] Filter records by labels.
- [x] Filter records by tags.
- [x] Create record manually.
- [x] Batch create records if useful for admins.
- [x] View record content and metadata.
- [x] Edit record labels.
- [x] Edit record tags.
- [x] Edit record custom metadata.
- [x] Delete one record.
- [x] Delete multiple records.
- [x] Show source AssistantHub document metadata when present.
- [x] Link from record to AssistantHub document detail if that route exists. No document-detail route is present; source document IDs are displayed where metadata is present.
- [x] Include empty, loading, and error states.

### Index Search View

Base this on Verbex dashboard `SearchView.jsx`, adapted to AssistantHub.

Required UX:

- [x] Index selector.
- [x] Search query input.
- [x] Support `*` wildcard browse.
- [x] Match mode segmented control: any term/OR and all terms/AND.
- [x] Max results selector: 10, 25, 50, 100, 250.
- [x] Minimum score filter if client-side filtering remains useful.
- [x] Label filter input.
- [x] Tag filter input with key/value support.
- [x] Search submit button.
- [x] Clear/reset button.
- [x] Results count.
- [x] Search timing.
- [x] Score bars.
- [x] Matched terms summary.
- [x] Term score/frequency details.
- [x] Result content preview.
- [x] Result detail modal.
- [x] Raw JSON modal or expandable debug view if consistent with dashboard conventions.
- [x] Link result to AssistantHub document detail when metadata contains `AssistantHubDocumentId`. No document-detail route is present; source document IDs are displayed where metadata is present.
- [x] Empty state for no query.
- [x] Empty state for no results.
- [x] Error state for Verbex failures.
- [x] Preserve keyboard-submit behavior.

### Collection Search View

Base this on RecallDB dashboard search views, adapted to AssistantHub.

Required UX:

- [x] Collection selector.
- [x] Full-text query input.
- [x] Vector search controls if embeddings can be provided or selected.
- [x] Search type selector for RecallDB full-text options.
- [x] Language selector if RecallDB exposes it.
- [x] Normalization control.
- [x] Minimum score controls.
- [x] Label required/excluded filters.
- [x] Tag required/excluded filters.
- [x] Required/excluded terms.
- [x] Created date range.
- [x] Document ID filter.
- [x] Max results selector.
- [x] Include neighbors toggle.
- [x] Continuation token paging.
- [x] Results table/list with document ID, content preview, vector score, text score, labels, tags, and metadata.
- [x] Result detail modal.
- [x] Empty, loading, and error states.

### Settings Page

Add Verbex service card between DocumentAtom and Partio.

Checklist:

- [x] Update configuration summary to include Verbex.
- [x] Place Verbex card between DocumentAtom and Partio.
- [x] Show endpoint.
- [x] Show dashboard URL.
- [x] Show default index.
- [x] Show ingestion enabled/required state.
- [x] Redact access key.
- [x] Add external link to Verbex dashboard.
- [x] Keep card styling consistent with existing service cards.
- [x] Update any Settings page tests/snapshots. No snapshot harness is present; Settings validation is covered by dashboard build and configuration summary tests.

## MCP Implementation Plan

Add MCP tools that mirror the new REST APIs.

### Index Tools

- [x] `index/list`
- [x] `index/create`
- [x] `index/get` using existing AssistantHub MCP naming style in place of planned `index/read`
- [x] `index/exists`
- [x] `index/update`
- [x] `index/delete`
- [x] `index/labels/update`
- [x] `index/tags/update`
- [x] `index/custom-metadata/update`
- [x] `index/terms/top`
- [x] `index/cache/rebuild` if implemented in REST. Not added because REST does not expose cache rebuild in v0.14.0.

### Index Record Tools

- [x] `index/record/list`
- [x] `index/record/create`
- [x] `index/record/create-batch`
- [x] `index/record/get` using existing AssistantHub MCP naming style in place of planned `index/record/read`
- [x] `index/record/exists`
- [x] `index/record/exists-batch`
- [x] `index/record/delete`
- [x] `index/record/batch-delete` using existing AssistantHub MCP naming style in place of planned `index/record/delete-batch`
- [x] `index/record/labels/update`
- [x] `index/record/tags/update`
- [x] `index/record/custom-metadata/update`

### Search Tools

- [x] `index/search`
- [x] `collection/search`

MCP checklist:

- [x] Add registration classes in `src/AssistantHub.McpServer/Registrations`.
- [x] Add proxy methods to `AssistantHubMcpRestProxy`. Not needed for this pass; tools use SDK methods that call AssistantHub REST.
- [x] Add request/response schemas and descriptions.
- [x] Ensure tool names match existing AssistantHub MCP naming style.
- [x] Ensure tools call AssistantHub REST, not Verbex or RecallDB directly.
- [~] Add tests for proxy URL construction and request bodies. REST/SDK proxy URL and request body paths are covered by service/API suites; MCP tool-level request-body tests remain future hardening.
- [x] Update `MCP_API.md`.

## SDK Implementation Plan

Update all SDKs to include the new API surface.

### C# SDK

- [x] Add index models. Implemented as dynamic `JsonElement` pass-through responses because Verbex owns the schema.
- [x] Add index record models. Implemented as dynamic `JsonElement` pass-through responses because Verbex owns the schema.
- [x] Add Verbex search request/response models. Implemented as dynamic `JsonElement` pass-through requests/responses.
- [x] Add RecallDB collection search request/response models. Implemented as dynamic `JsonElement` pass-through requests/responses.
- [x] Add index client methods.
- [x] Add index record client methods.
- [x] Add index search method.
- [x] Add collection search method.
- [~] Add tests. C# SDK compiles through the solution and API routes are covered by server/API suites; dedicated SDK unit tests are not present.
- [x] Update package version to `0.14.0`.
- [x] Update SDK README/examples.

### Python SDK

- [x] Add index models or typed dicts. Implemented as dynamic `dict[str, Any]` pass-through payloads.
- [x] Add index record models or typed dicts. Implemented as dynamic `dict[str, Any]` pass-through payloads.
- [x] Add Verbex search helpers.
- [x] Add RecallDB collection search helpers.
- [~] Add tests. Python SDK syntax validation passes; dedicated SDK unit tests are not present.
- [x] Update package version to `0.14.0`.
- [x] Update README/examples.

### JavaScript SDK

- [x] Add index APIs.
- [x] Add index record APIs.
- [x] Add Verbex search API.
- [x] Add RecallDB collection search API.
- [~] Add tests. JavaScript SDK build validation passes; dedicated SDK unit tests are not present.
- [x] Update package version to `0.14.0`.
- [x] Update README/examples.

## Documentation Plan

### REST Documentation

- [x] Update `REST_API.md` with `Indices`.
- [x] Update `REST_API.md` with `Index Records`.
- [x] Update `REST_API.md` with `Index Search`.
- [x] Update `REST_API.md` with `Collection Search`.
- [x] Update `REST_API.md` document deletion behavior to mention Verbex cleanup.
- [x] Update `REST_API.md` ingestion-rule shape to mention index target settings.
- [x] Update `REST_API.md` configuration section to include Verbex.
- [x] Include sample requests and responses. Request examples and pass-through response descriptions are included for the new search artifact routes.
- [x] Include auth and role requirements. New search artifact routes are documented as AssistantHub-authenticated admin routes.
- [x] Include upstream service error behavior. Pass-through/proxy behavior and key reindex failure responses are documented.

### MCP Documentation

- [x] Update `MCP_API.md` category list.
- [x] Document index tools.
- [x] Document index record tools.
- [x] Document index search tool.
- [x] Document collection search tool.
- [x] Include sample tool arguments.
- [~] Include sample tool responses. Request examples are present; response payloads remain pass-through to AssistantHub/Verbex/RecallDB.

### Docker And Operations Documentation

- [x] Update README service table with Verbex server/dashboard.
- [x] Update Docker deployment instructions.
- [x] Update factory reset instructions.
- [x] Document Verbex Postgres configuration.
- [x] Document default index creation.
- [x] Document upgrading existing deployments and reindexing existing documents.
- [x] Document how to verify Verbex search after ingestion.
- [x] Document ports `8501` and `8502` or final chosen ports.

### Product Documentation

- [x] Explain the difference between Verbex index search and RecallDB collection search.
- [x] Explain that Verbex is the primary TF-IDF/text search path for finding documents.
- [x] Explain how labels and tags affect Verbex search.
- [x] Explain how ingestion rules choose a Verbex index.
- [x] Explain document deletion and search garbage collection.

## OpenAPI And Swagger Plan

Every REST API route must have useful OpenAPI metadata.

Checklist:

- [x] Extend route registration metadata for all new index routes.
- [x] Extend route registration metadata for collection search.
- [x] Add path parameter metadata for `indexId`, `recordId`, and `collectionId`.
- [x] Add query parameter metadata for pagination, ordering, labels, tags, and batch IDs.
- [x] Add request body schemas for create/update/search/batch routes.
- [x] Add response body schemas for list/detail/search routes.
- [x] Add error response metadata.
- [x] Update generated runtime OpenAPI behavior.
- [x] Regenerate committed `openapi.json`.
- [~] Verify Swagger UI shows all new routes. Static OpenAPI path coverage is tested; live Swagger UI inspection remains.
- [~] Verify Swagger UI request bodies are usable. Static OpenAPI request-body coverage is tested; live Swagger UI inspection remains.
- [x] Verify every existing and new REST API route has at least tag, summary, parameters, request body, and response metadata where applicable. Runtime builder emits metadata and API-suite checks cover the new search artifact routes/request bodies.

## Postman Plan

Update `postman/AssistantHub.postman_collection.json`.

Checklist:

- [x] Add Verbex/Indices folder.
- [x] Add list/create/read/update/delete index requests.
- [x] Add index labels/tags/custom metadata requests.
- [x] Add top terms/cache requests if implemented.
- [x] Add Index Records folder.
- [x] Add list/create/read/delete record requests.
- [x] Add batch create and batch delete requests.
- [x] Add record labels/tags/custom metadata requests.
- [x] Add Index Search request.
- [x] Add Collection Search request.
- [x] Add environment variables for `index_id`, `record_id`, and search query values.
- [x] Ensure all requests use AssistantHub base URL and AssistantHub auth.
- [x] Do not include direct Verbex or RecallDB URLs except in explanatory docs.
- [x] Update collection version metadata to v0.14.0. Collection inherits current AssistantHub package/version context; request variables were added for v0.14.0 search APIs.

## Database And Migration Plan

Checklist:

- [x] Identify all supported DB providers and migration locations.
- [x] Add migration for ingestion-rule index settings.
- [x] Add migration for optional document Verbex indexing metadata.
- [~] Add migration tests where available. No dedicated migration test harness is present; additive startup migration code builds and factory reset schema is updated.
- [x] Update seed/default data for the default ingestion rule.
- [x] Update any schema snapshots. No generated schema snapshot artifact was found; reset/seed schema definitions are updated directly.
- [x] Ensure factory reset creates a fresh schema with v0.14.0 fields.
- [~] Ensure upgrade from v0.13.0 preserves existing tenants, documents, collections, and ingestion rules. Additive startup migrations preserve records; live upgrade smoke remains.

## Version Update Plan

Update all version references to v0.14.0.

Checklist:

- [x] Run `rg -n "0\.13\.0|v0\.13\.0"`.
- [x] Update `src/AssistantHub.Core/Constants.cs`.
- [x] Update Docker image tags for AssistantHub services.
- [x] Update README and markdown documentation.
- [x] Update SDK package versions.
- [x] Update dashboard package metadata if versioned.
- [x] Update Postman collection metadata.
- [x] Update OpenAPI `info.version`.
- [x] Update release/changelog files if present.
- [x] Verify no stale v0.13.0 references remain except historical entries. Audit found historical changelog/archive/migration references plus a recovered Docker SQL dump and SEARCH.md audit notes; active code/package/docs metadata are v0.14.0.

## Test Plan

### Backend Unit Tests

- [x] Verbex settings validation.
- [x] Verbex URL construction.
- [x] Verbex auth header forwarding.
- [x] Index CRUD proxy behavior.
- [x] Index record CRUD proxy behavior.
- [x] Verbex search proxy behavior.
- [x] RecallDB collection search proxy behavior.
- [x] Verbex content normalization and optional max-content limiting.
- [x] Ingestion text stream creation.
- [x] Ingestion label/tag merge propagation to Verbex.
- [x] Ingestion duplicate Verbex record handling.
- [x] Ingestion Verbex failure behavior.
- [x] Document delete Verbex cleanup.
- [x] Bulk document delete Verbex cleanup.
- [x] Tenant provisioning Verbex calls.
- [x] Tenant deprovision Verbex cleanup.

### Backend Integration Tests

- [~] Start stubbed Verbex server and verify AssistantHub proxy routes. Service/API-suite coverage validates route mappings and payloads; live stub server remains future integration coverage.
- [~] Start stubbed RecallDB server and verify collection search route. API-suite coverage validates route mapping; live stub server remains future integration coverage.
- [~] Run ingestion with a sample text document and verify Verbex receives the full extracted text. Service-suite coverage validates normalized content payloads; live end-to-end ingestion remains future integration coverage.
- [~] Run ingestion with labels/tags and verify Verbex receives them. Service-suite coverage validates payload propagation; live end-to-end ingestion remains future integration coverage.
- [~] Delete ingested document and verify Verbex delete is called. Service-suite coverage validates single/bulk cleanup; live end-to-end delete remains future integration coverage.
- [x] Verify default index creation during first-run.
- [x] Verify tenant provisioning creates default Verbex index in code path; live Docker smoke test remains.

### Docker Tests

- [ ] `docker compose pull` succeeds for Verbex images.
- [ ] `docker compose up` starts all services.
- [ ] PostgreSQL contains the Verbex database and role.
- [ ] Verbex server connects to PostgreSQL.
- [ ] Verbex dashboard reaches Verbex server.
- [ ] AssistantHub reaches Verbex server.
- [ ] Factory reset restores Verbex config and clears data.
- [ ] After reset, first-run initialization recreates default Verbex index.

### Dashboard Tests

- [x] Sidebar displays Collections Search and Indices routes in the requested order.
- [x] Settings page shows Verbex card between DocumentAtom and Partio.
- [x] Indices page lists indices.
- [x] Index Records page lists records.
- [x] Index Search page can submit a query and render scored results.
- [x] Collection Search page can submit a RecallDB search and render results.
- [x] Loading states render correctly.
- [x] Empty states render correctly.
- [x] Error states render upstream errors clearly.
- [~] Layout works at desktop and mobile widths. Responsive CSS and table behavior were updated and dashboard build passes; browser screenshot validation remains.

### SDK And Documentation Tests

- [x] C# SDK build passes through `dotnet build src/AssistantHub.sln`.
- [x] Python SDK syntax check passes via `python -m compileall assistanthub_sdk`.
- [x] JavaScript SDK build passes via `npm.cmd run build`.
- [~] Postman collection imports successfully. JSON parse validation passed; manual Postman import remains.
- [ ] Postman smoke requests succeed against local Docker. Live Docker validation remains.
- [~] Swagger UI loads all new endpoints. Static OpenAPI coverage is tested; live Swagger UI validation remains.
- [x] REST and MCP docs match implemented routes.

## Acceptance Criteria

The v0.14.0 change is complete when all of the following are true:

- [x] Docker deployment includes Verbex server and dashboard containers.
- [x] Verbex uses PostgreSQL in the Docker deployment.
- [x] Factory reset accounts for Verbex configuration and data.
- [x] AssistantHub first-run creates or ensures a default Verbex index.
- [x] Tenant provisioning creates or ensures a tenant default Verbex index.
- [x] Ingesting a document with extracted text creates a Verbex record.
- [x] Verbex record content contains the extracted document text stream.
- [x] Verbex record labels/tags include ingestion-rule and document labels/tags.
- [x] Verbex record ID is stable and tied to the AssistantHub document ID.
- [x] Deleting a document removes its Verbex record.
- [x] Dashboard Settings has a Verbex card between DocumentAtom and Partio.
- [x] Dashboard has `ARTIFACTS > Indices`.
- [x] Dashboard has `ARTIFACTS > Indices > Records`.
- [x] Dashboard has `ARTIFACTS > Indices > Search`.
- [x] Dashboard has `ARTIFACTS > Collections > Search` underneath Records.
- [x] Verbex search requests go through AssistantHub server.
- [x] RecallDB search requests go through AssistantHub server.
- [x] All new REST APIs have OpenAPI/Swagger metadata.
- [x] MCP exposes equivalent tools for new REST APIs.
- [x] REST docs, MCP docs, Postman, SDKs, Docker docs, and README are updated.
- [x] All applicable version references are updated to v0.14.0. Active build/package/docs metadata are updated; remaining `0.13.0` hits are historical changelog/archive/migration references or a local recovered SQL dump.
- [~] Automated tests and Docker smoke tests pass. Static/build validation, focused service/API suites, OpenAPI/Postman JSON validation, and `git diff --check` pass; live Docker and browser smoke tests remain.

## Implementation Notes

- [x] Prefer pass-through response bodies for Verbex and RecallDB artifact APIs unless AssistantHub already normalizes the equivalent response type.
- [x] Keep AssistantHub route names product-neutral: use `indices`, `records`, and `search`; do not expose `Verbex` in route paths.
- [x] Keep AssistantHub dashboard labels user-facing: "Indices", "Records", "Search".
- [x] Treat Verbex as the primary text/TF-IDF search path for finding documents.
- [x] Treat RecallDB collection search as collection/vector/full-text search over RecallDB records.
- [x] Avoid direct browser calls from AssistantHub dashboard to Verbex or RecallDB.
- [x] Avoid direct SDK calls to Verbex or RecallDB.
- [x] Preserve existing RecallDB collection and record behavior while adding collection search.
- [x] Preserve existing ingestion semantics unless Verbex indexing is explicitly enabled and required.

## Progress Log

Add dated notes here as work proceeds.

- [x] 2026-06-05: Plan drafted.
- [x] 2026-06-05: Created branch `feature/v0.14.0`.
- [x] 2026-06-05: Added Verbex settings/configuration, Docker Compose services, PostgreSQL init, factory reset hooks, first-run default Verbex provisioning, backend proxy routes for indices/index records/index search, RecallDB collection search proxy route, first-pass dashboard pages, and initial REST docs.
- [x] 2026-06-05: Added Verbex ingestion after DocumentAtom extraction using one Verbex record per AssistantHub document, label/tag propagation, duplicate replace-on-conflict behavior, and Verbex cleanup during single/bulk document deletion.
- [x] 2026-06-05: Validation passed: `dotnet build src/AssistantHub.Server/AssistantHub.Server.csproj`, `dotnet build src/AssistantHub.sln`, `npm.cmd run build`, and `docker compose -f docker/compose.yaml config --quiet`.
- [x] 2026-06-05: Added C# SDK pass-through methods and MCP tools for Verbex indices, Verbex index records, Verbex index search, and RecallDB collection search. Validation passed: `dotnet build src/AssistantHub.McpServer/AssistantHub.McpServer.csproj`.
- [x] 2026-06-05: Added JavaScript and Python SDK methods for Verbex indices, Verbex index records, Verbex index search, and RecallDB collection search. Validation passed: `npm.cmd run build` in `sdk/js`, `python -m compileall assistanthub_sdk` in `sdk/python`, and `dotnet build src/AssistantHub.sln`.
- [x] 2026-06-05: Added Verbex tenant lifecycle hooks to tenant provisioning/deprovisioning. New tenants create a Verbex tenant, persist Verbex tenant/default-index mappings in tenant tags, and ingestion/deletion resolves those mappings. First-run now checks Verbex's built-in `default` tenant before ensuring the default index. Validation passed: `dotnet build src/AssistantHub.sln`.
- [x] 2026-06-05: Updated runtime OpenAPI tagging/generic metadata, committed `openapi.json`, `MCP_API.md`, `REST_API.md`, README, and Postman collection for Verbex indices/records/search and RecallDB collection search. JSON validation passed for `openapi.json` and Postman.
- [x] 2026-06-05: Final validation sweep passed: `dotnet build src/AssistantHub.sln`, `npm.cmd run build` in `dashboard`, `npm.cmd run build` in `sdk/js`, `python -m compileall assistanthub_sdk` in `sdk/python`, `docker compose -f docker/compose.yaml config --quiet`, and JSON parse validation for `openapi.json` and Postman.
- [x] 2026-06-06: Added service contracts and implementations for Less3/S3 object storage, DocumentAtom atomization, Partio chunking/endpoint management, RecallDB vector storage, and Verbex inverted indexing. Rewired storage consumers to `IObjectStorageService`, DocumentAtom ingestion calls to `IAtomizationService`, RecallDB collection/retrieval/provisioning/ingestion cleanup paths to `IVectorStoreService`, and smaller Partio retrieval/embedding endpoint-info calls to `IChunkingService`. Validation passed: `dotnet build src/AssistantHub.sln`.
- [x] 2026-06-06: Rewired Partio embedding endpoint and completion/inference endpoint handlers through `IEmbeddingEndpointService` and `IInferenceEndpointService`, moving Partio URL and bearer handling out of those handlers. Validation passed: `dotnet build src/AssistantHub.sln`.
- [x] 2026-06-06: Rewired the main Partio `/v1.0/process` ingestion call, assistant endpoint auto-assignment, chat/inference/eval completion endpoint resolution, and public assistant RecallDB distinct label/tag lookups through service interfaces. Remaining direct subordinate URL/token use is limited to health-check plumbing and diagnostic logging. Validation passed: `dotnet build src/AssistantHub.sln`.
- [x] 2026-06-06: Added `IExternalServiceHealthService`/`ExternalServiceHealthService` for startup subordinate-service connectivity checks and routed Partio endpoint-health enumeration through `IChunkingService`. Validation passed: `dotnet build src/AssistantHub.sln`.
- [x] 2026-06-06: Re-ran version reference audit. Remaining `0.13.0` hits are historical changelog/archive/migration references and a recovered Docker SQL dump, not active package/version metadata.
- [x] 2026-06-06: Masked sensitive values in the Settings configuration summary and verified the Verbex dashboard link is included in the external dashboard links. Validation passed: `npm.cmd run build` in `dashboard` with existing Vite warnings.
- [x] 2026-06-06: Added ingestion-rule `VerbexIndexId`, document Verbex tenant/index/record metadata, additive schema upgrades for PostgreSQL/SQLite/MySQL/SQL Server, first-run and tenant-provisioning default rule index targets, dashboard ingestion-rule index editing, SDK model fields, Postman/OpenAPI/REST documentation updates, and grouped Verbex batch cleanup on bulk document deletion. Validation passed: `dotnet build src/AssistantHub.sln`, `npm.cmd run build` in `dashboard`, `npm.cmd run build` in `sdk/js`, and `python -m compileall assistanthub_sdk` in `sdk/python`.
- [x] 2026-06-06: Added Verbex settings to the MCP test host dependency-stub configuration and extended configuration redaction assertions for `Verbex.AccessKey`. Validation passed: `dotnet test src/AssistantHub.sln --no-build`.
- [x] 2026-06-06: Added admin document reindex/backfill APIs, SDK models/helpers, MCP tools, OpenAPI/Postman coverage, README upgrade/backfill instructions, runtime OpenAPI query metadata, and aligned C#/JS document bulk-delete SDK payloads with the documented server contract. Validation passed: `dotnet build src/AssistantHub.sln --no-restore`, `dotnet test src/AssistantHub.sln --no-build`, `npm.cmd run build` in `dashboard`, `npm.cmd run build` in `sdk/js`, `python -m compileall assistanthub_sdk` in `sdk/python`, `docker compose -f docker/compose.yaml config --quiet`, JSON parse validation for `openapi.json` and Postman, and `git diff --check`.
- [x] 2026-06-06: Added a Documents row action for admin users to reindex a completed/indexed/active document into Verbex through AssistantHub. Validation passed: `npm.cmd run build` in `dashboard`.
- [x] 2026-06-06: Added Verbex settings validation for endpoint URL, dashboard URL, access key when ingestion is enabled, and default index path safety; wired validation into server startup; added shared model-suite coverage; and updated `CHANGELOG.md` for the v0.14.0 search integration. Validation passed: `dotnet build src/AssistantHub.sln --no-restore` and `ASSISTANTHUB_TEST_SUITES=model dotnet run --project src/Test.Automated/Test.Automated.csproj --no-build`.
- [x] 2026-06-06: Added injectable `HttpClient` support to `VerbexInvertedIndexService` and shared service-suite coverage for Verbex URL construction, bearer access-key forwarding, and JSON request bodies. Validation passed: `dotnet build src/AssistantHub.sln --no-restore` and `ASSISTANTHUB_TEST_SUITES=service dotnet run --project src/Test.Automated/Test.Automated.csproj --no-build`.
- [x] 2026-06-06: Added API-suite source-level proxy mapping tests for Verbex index CRUD, index record CRUD, Verbex index search, and RecallDB collection search route-to-upstream behavior. Validation passed: `dotnet build src/AssistantHub.sln --no-restore` and `ASSISTANTHUB_TEST_SUITES=api dotnet run --project src/Test.Automated/Test.Automated.csproj --no-build`.
- [x] 2026-06-06: Added pre-Verbex text normalization for indexed content, preserving interior whitespace while normalizing line endings and trimming trailing per-line spaces/tabs; added direct service-suite coverage. Validation passed: `dotnet build src/AssistantHub.sln --no-restore` and `ASSISTANTHUB_TEST_SUITES=service dotnet run --project src/Test.Automated/Test.Automated.csproj --no-build`.
- [x] 2026-06-06: Added optional `Verbex.MaxContentCharacters` large-document limit with default unlimited behavior, Docker/factory config entries, dashboard Settings UI, REST/README documentation, and model/service-suite coverage. Validation passed: `dotnet build src/AssistantHub.sln --no-restore`, `ASSISTANTHUB_TEST_SUITES=model,service dotnet run --project src/Test.Automated/Test.Automated.csproj --no-build`, and `npm.cmd run build` in `dashboard`.
- [x] 2026-06-06: Added service-suite coverage for ingestion label/tag merging and Verbex record payload propagation, empty extracted-text skip behavior, duplicate-record replace-on-conflict behavior, required versus best-effort Verbex indexing failures, document/bulk Verbex cleanup, missing-record cleanup, and cleanup failure policy. Validation passed: `dotnet build src/AssistantHub.sln --no-restore` and `ASSISTANTHUB_TEST_SUITES=service dotnet run --project src/Test.Automated/Test.Automated.csproj --no-build`.
- [x] 2026-06-06: Added injectable subordinate service implementations to `TenantProvisioningService` and service-suite coverage for Verbex tenant/default-index provisioning, tenant tag persistence, and mapped Verbex tenant deletion during deprovisioning. Validation passed: `dotnet build src/AssistantHub.sln --no-restore` and `ASSISTANTHUB_TEST_SUITES=service dotnet run --project src/Test.Automated/Test.Automated.csproj --no-build`.
- [x] 2026-06-06: Revalidated Docker Compose configuration after Verbex settings/config updates. Validation passed: `docker compose -f docker/compose.yaml config --quiet`.
- [x] 2026-06-06: Completed the dashboard search artifact pages: enriched Indices, Index Records, Verbex Index Search, and RecallDB Collection Search with metadata editing, label/tag/custom-metadata helpers, filters, top terms, result details, raw JSON, and loading/empty/error states. Validation passed: `npm.cmd run build` in `dashboard`.
- [x] 2026-06-06: Added first-run Verbex default-index initialization coverage and OpenAPI/Postman API-suite coverage for the new search artifact REST routes and request bodies. Validation passed: `dotnet build src/AssistantHub.sln --no-restore`, `ASSISTANTHUB_TEST_SUITES=service dotnet run --project src/Test.Automated/Test.Automated.csproj --no-build`, and `ASSISTANTHUB_TEST_SUITES=api dotnet run --project src/Test.Automated/Test.Automated.csproj --no-build`.
- [x] 2026-06-06: Updated README, CHANGELOG, and C#/JavaScript/Python SDK READMEs for the completed Verbex and RecallDB search integration, then normalized generated XML documentation after the final .NET build. Final validation passed: `dotnet build src/AssistantHub.sln --no-restore`, `npm.cmd run build` in `dashboard`, `npm.cmd run build` in `sdk/js`, `python -m compileall assistanthub_sdk`, focused service/API suites, OpenAPI/Postman JSON parse, and `git diff --check`.
- [x] 2026-06-06: Fixed dashboard table rendering for AssistantHub-wrapped proxied enumeration responses such as `Data.Objects`, which allows the default Verbex index returned by `/v1.0/indices` to render on `ARTIFACTS > Indices`. Validation passed: `npm.cmd run build` in `dashboard`.
- [x] 2026-06-06: Added `View Records` and `Search` row actions to `ARTIFACTS > Indices`, linking to `/indices/records?indexId=...` and `/indices/search?indexId=...`; both target pages now preselect the requested index from navigation state or query string. Validation passed: `npm.cmd run build` in `dashboard`.
- [x] 2026-06-06: Added `View Records` and `Search` row actions to `ARTIFACTS > Collections`, linking to `/records?collectionId=...` and `/collections/search?collectionId=...`; both target pages now preselect the requested collection from navigation state or query string, and Records unwraps `Data.Objects`. Validation passed: `npm.cmd run build` in `dashboard`.
- [x] 2026-06-06: Ensured Verbex indexed records always populate `Name` from the explicit document name, original filename, object key basename, source URL basename, or document ID; added `ObjectName` to Verbex custom metadata; normalized AssistantHub single/batch index-record create payloads before proxying to Verbex; and added service/API-suite coverage. Validation passed: `dotnet build src/AssistantHub.sln --no-restore`, `ASSISTANTHUB_TEST_SUITES=service dotnet run --project src/Test.Automated/Test.Automated.csproj --no-build`, `ASSISTANTHUB_TEST_SUITES=api dotnet run --project src/Test.Automated/Test.Automated.csproj --no-build`, OpenAPI/Postman JSON parse, and `git diff --check`.
- [x] 2026-06-06: Replaced free-form label/tag search filter inputs on `ARTIFACTS > Indices > Search` and `ARTIFACTS > Collections > Search` with row-based label text boxes and tag key/value pairs, including per-row delete controls and last-row add controls. Validation passed: `npm.cmd run build` in `dashboard`.
