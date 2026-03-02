# AssistantHub Testing Plan

> Systematic test coverage from lowest layer to highest.
> Mark each item: `[ ]` pending, `[~]` in progress, `[x]` complete, `[-]` skipped (with reason).

---

## Table of Contents

1. [Test Infrastructure](#1-test-infrastructure)
2. [Layer 1 — Models & Enums](#2-layer-1--models--enums)
3. [Layer 2 — Database Drivers](#3-layer-2--database-drivers)
4. [Layer 3 — Core Services (Unit)](#4-layer-3--core-services-unit)
5. [Layer 4 — Authentication & Authorization](#5-layer-4--authentication--authorization)
6. [Layer 5 — API Handlers (Unit)](#6-layer-5--api-handlers-unit)
7. [Layer 6 — API Integration (HTTP)](#7-layer-6--api-integration-http)
8. [Layer 7 — Background Services](#8-layer-7--background-services)
9. [Running Tests](#9-running-tests)

---

## 1. Test Infrastructure

### 1.1 Shared test harness — `Test.Common`

The existing `Test.Database` project contains `TestRunner`, `TestResult`, and `AssertHelper`. These must be
extracted into a shared class library so every test project uses the **same** harness with **identical** output
behavior. The `Test.Common` namespace replaces the current `Test.Database` namespace for these three files.

**Required output contract — every test project must produce this exact format:**

```
==========================================================
  AssistantHub <ProjectName> Test Suite
==========================================================
  <optional context lines, e.g. database type>
==========================================================

<SuiteName>
  PASS  <TestName> (1.2ms)
  FAIL  <TestName> (0.8ms)
         <error message>
  PASS  <TestName> (3.4ms)

<SuiteName>
  PASS  <TestName> (0.5ms)
  ...

================================================================================
TEST SUMMARY
================================================================================
  Total:   42
  Passed:  40
  Failed:  2
  Runtime: 312.7ms

FAILED TESTS:
  FAIL  <TestName>
         <error message>
  FAIL  <TestName>
         <error message>

OVERALL: FAIL
```

Rules:
- Every individual test prints `PASS` (green) or `FAIL` (red) with the test name and runtime in milliseconds.
- Failed tests print the error message indented on the next line (dark yellow).
- After all tests complete, `PrintSummary(totalRuntimeMs)` prints the summary block.
- The summary lists total/passed/failed counts, total runtime, enumerates every failed test, and prints
  `OVERALL: PASS` (green) or `OVERALL: FAIL` (red).
- Exit code: `0` if all tests passed, `1` if any test failed.
- Each test project is a **console application** (`<OutputType>Exe</OutputType>`, `net10.0`).
- Each test project has a `Program.cs` with `static async Task<int> Main(string[] args)` that instantiates
  `TestRunner`, calls each test suite's `RunAllAsync`, calls `runner.PrintSummary(...)`, and returns the exit code.

### 1.2 Projects

| Project | Type | Purpose | References |
|---------|------|---------|------------|
| `Test.Common` | **Class library** (new) | `TestRunner`, `TestResult`, `AssertHelper` | — |
| `Test.Database` | Console app (existing) | Database driver CRUD against real databases | AssistantHub.Core, Test.Common |
| `Test.Models` | Console app (new) | Model validation, defaults, serialization, enums | AssistantHub.Core, Test.Common |
| `Test.Services` | Console app (new) | Core service logic with mocked HTTP and DB | AssistantHub.Core, Test.Common |
| `Test.Api` | Console app (new) | Handler/endpoint tests with mocked services | AssistantHub.Core, AssistantHub.Server, Test.Common |
| `Test.Integration` | Console app (new) | Full HTTP integration tests against in-process server | AssistantHub.Server, Test.Common |

### 1.3 Infrastructure tasks

- [ ] Create `Test.Common` class library project
- [ ] Move `TestRunner.cs`, `TestResult.cs`, `AssertHelper.cs` from `Test.Database` into `Test.Common` (namespace `Test.Common`)
- [ ] Update `Test.Database` to reference `Test.Common` instead of owning those files; update `using` statements
- [ ] Verify `Test.Database` still compiles and produces identical output after the move
- [ ] Create `Test.Models` console app project referencing `AssistantHub.Core` + `Test.Common`
- [ ] Create `Test.Services` console app project referencing `AssistantHub.Core` + `Test.Common`
- [ ] Create `Test.Api` console app project referencing `AssistantHub.Core` + `AssistantHub.Server` + `Test.Common`
- [ ] Create `Test.Integration` console app project referencing `AssistantHub.Server` + `Test.Common`
- [ ] Add all new projects to `AssistantHub.sln`
- [ ] Each new project has `Program.cs` following the pattern below
- [ ] Extend `AssertHelper` with collection assertions: `HasCount`, `Contains`, `IsEmpty`, `AllMatch`
- [ ] Create `MockHttpMessageHandler` utility in `Test.Common` for stubbing HTTP calls to external services
- [ ] Create `MockDatabaseDriver` in `Test.Common` implementing `DatabaseDriverBase` with in-memory dictionaries
- [ ] Create `TestFixture` base class in `Test.Common` with common setup (logging suppression, cancellation tokens)
- [ ] Add a top-level test runner script (`run-tests.sh` / `run-tests.ps1`) that executes all test projects sequentially, propagates exit codes, and prints a cross-project summary

### 1.4 Program.cs template (every new test project must follow this)

```csharp
namespace Test.ProjectName
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Common;

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("  AssistantHub <ProjectName> Test Suite");
            Console.WriteLine("==========================================================");
            Console.WriteLine();

            TestRunner runner = new TestRunner();
            CancellationToken token = CancellationToken.None;
            Stopwatch totalStopwatch = Stopwatch.StartNew();

            try
            {
                // Run test suites in dependency order
                await SuiteA.RunAllAsync(runner, token);
                await SuiteB.RunAllAsync(runner, token);
                // ...
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unhandled exception during test execution: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }

            totalStopwatch.Stop();
            runner.PrintSummary(totalStopwatch.Elapsed.TotalMilliseconds);

            foreach (TestResult r in runner.Results)
            {
                if (!r.Passed) return 1;
            }

            return 0;
        }
    }
}
```

### 1.5 Test suite class template (every test suite must follow this)

```csharp
namespace Test.ProjectName.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Common;

    public static class ExampleTests
    {
        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("ExampleTests");

            await runner.RunTestAsync("Example: thing works", async (ct) =>
            {
                // Arrange
                // Act
                // Assert using AssertHelper
                AssertHelper.AreEqual(expected, actual, "description");
            }, token);

            await runner.RunTestAsync("Example: bad input fails", async (ct) =>
            {
                // ...
            }, token);
        }
    }
}
```

---

## 2. Layer 1 — Models & Enums

**Project:** `Test.Models`

Tests that models serialize/deserialize correctly, have proper defaults, and that enums cover all expected values.

### 2.1 Enum coverage

- [ ] `DatabaseTypeEnum` — all 4 values parse from string
- [ ] `DocumentStatusEnum` — all values round-trip through JSON serialization
- [ ] `InferenceProviderEnum` — Ollama, OpenAI
- [ ] `FeedbackRatingEnum` — all values
- [ ] `ApiErrorEnum` — all values exist and are distinct
- [ ] `ScheduleIntervalEnum` — all values
- [ ] `CrawlOperationStateEnum` — all states
- [ ] `CrawlPlanStateEnum` — all states
- [ ] `EnumerationOrderEnum` — CreatedAscending, CreatedDescending
- [ ] `RepositoryTypeEnum` — WebRepository
- [ ] `WebAuthTypeEnum` — None, Basic, ApiKey, Bearer
- [ ] `SummarizationOrderEnum` — all values

### 2.2 Core domain models

For each model: verify default constructor, JSON round-trip, nullable fields, ID prefix generation.

- [ ] `TenantMetadata` — defaults, JSON serialization, Id prefix `ten_`
- [ ] `UserMaster` — defaults, `SetPassword()` / `VerifyPassword()` correctness, password never serialized in JSON
- [ ] `Credential` — defaults, Id prefix `cred_`, bearer token generation
- [ ] `Assistant` — defaults, Id prefix
- [ ] `AssistantSettings` — defaults, nested config objects serialize correctly
- [ ] `AssistantDocument` — defaults, `DocumentStatusEnum` transitions
- [ ] `AssistantFeedback` — defaults, rating enum
- [ ] `ChatHistory` — defaults, Id prefix
- [ ] `IngestionRule` — defaults, Id prefix
- [ ] `CrawlPlan` — defaults, nested `CrawlFilterSettings` / `CrawlIngestionSettings`
- [ ] `CrawlOperation` — defaults, state enum

### 2.3 API contract models

- [ ] `ChatCompletionRequest` — required fields present after deserialization
- [ ] `ChatCompletionResponse` / `ChatCompletionChoice` / `ChatCompletionUsage` — OpenAI-compatible structure
- [ ] `AuthenticateRequest` / `AuthenticateResult` — round-trip
- [ ] `ApiErrorResponse` — error enum + message populated
- [ ] `EnumerationQuery` — default `MaxResults`, ordering
- [ ] `EnumerationResult<T>` — `EndOfResults`, `ContinuationToken`, `TotalRecords`, `RecordsRemaining`

### 2.4 Settings models

- [ ] `AssistantHubSettings` — loads from JSON, all sections non-null with defaults
- [ ] `DatabaseSettings` — sensible defaults per database type
- [ ] `WebserverSettings` — default hostname/port
- [ ] `S3Settings` — endpoint URL format
- [ ] `InferenceSettings` — provider defaults
- [ ] `RecallDbSettings` — endpoint + bearer token
- [ ] `ChunkingSettings` / `EmbeddingsSettings` — endpoint + bearer token
- [ ] `DocumentAtomSettings` — endpoint
- [ ] `LoggingSettings` — console/file/syslog defaults
- [ ] `CrawlSettings` — schedule defaults

---

## 3. Layer 2 — Database Drivers

**Project:** `Test.Database` (existing)

### 3.1 Current coverage (existing — verify passing)

- [ ] `TenantTests` — CRUD, enumerate, paginate, order, filter
- [ ] `UserTests` — CRUD, enumerate, paginate, password hashing
- [ ] `CredentialTests` — CRUD, enumerate, bearer token lookup
- [ ] `AssistantTests` — CRUD, enumerate, paginate, order
- [ ] `AssistantSettingsTests` — CRUD, per-assistant settings
- [ ] `AssistantDocumentTests` — CRUD, enumerate, filter by assistant
- [ ] `AssistantFeedbackTests` — CRUD, enumerate, filter by assistant
- [ ] `IngestionRuleTests` — CRUD, enumerate
- [ ] `ChatHistoryTests` — CRUD, enumerate, filter by thread

### 3.2 Missing coverage to add

- [ ] `CrawlPlanTests` — CRUD, enumerate, filter by tenant
- [ ] `CrawlOperationTests` — CRUD, enumerate, filter by crawl plan
- [ ] Cross-driver parity — run all test suites against all 4 database types in CI:
  - [ ] SQLite
  - [ ] PostgreSQL
  - [ ] MySQL
  - [ ] SQL Server
- [ ] Concurrency / race condition tests — parallel create/update on same record
- [ ] Boundary tests — max string lengths, empty strings, special characters (unicode, SQL injection attempts)
- [ ] `EnumerationQuery` edge cases:
  - [ ] `MaxResults = 0` (should default or error)
  - [ ] `MaxResults = 1` through full dataset
  - [ ] Invalid `ContinuationToken` (should return empty or error)
  - [ ] `ContinuationToken` from wrong entity type
- [ ] Tenant isolation — verify that records from tenant A are never returned when querying as tenant B
- [ ] Cascade behavior — deleting a tenant cascades (or prevents) delete of child records
- [ ] `ExistsAsync` for every entity type
- [ ] `ReadByNameAsync` / `ReadByEmailAsync` with non-existent values returns null

---

## 4. Layer 3 — Core Services (Unit)

**Project:** `Test.Services`

All external HTTP calls stubbed with `MockHttpMessageHandler`. Database calls use `MockDatabaseDriver`.

### 4.1 AuthenticationService

- [ ] `AuthenticateBearerAsync` — valid bearer token returns `AuthContext` with correct user/tenant/credential
- [ ] `AuthenticateBearerAsync` — admin API key returns admin `AuthContext`
- [ ] `AuthenticateBearerAsync` — invalid token returns null
- [ ] `AuthenticateBearerAsync` — inactive credential returns null
- [ ] `AuthenticateBearerAsync` — inactive user returns null
- [ ] `AuthenticateBearerAsync` — inactive tenant returns null
- [ ] `AuthenticateByEmailPasswordAsync` — correct email + password returns result
- [ ] `AuthenticateByEmailPasswordAsync` — wrong password returns null
- [ ] `AuthenticateByEmailPasswordAsync` — non-existent email returns null
- [ ] `AuthenticateByEmailPasswordAsync` — default tenant ID used when not specified
- [ ] Password is redacted in returned user object

### 4.2 StorageService

- [ ] Constructor with valid S3 settings succeeds
- [ ] `UploadAsync` to default bucket — calls S3 client correctly
- [ ] `UploadAsync` to specific bucket — creates/caches bucket client
- [ ] `DownloadAsync` — returns byte array from S3
- [ ] `DownloadAsync` — non-existent key returns null
- [ ] `DeleteAsync` — calls S3 delete
- [ ] Bucket client caching — second call to same bucket reuses client

### 4.3 IngestionService

- [ ] `ProcessDocumentAsync` — happy path: file downloaded, extracted, chunked, embedded, indexed
- [ ] Pipeline step ordering — DocumentAtom called before chunking, chunking before embedding
- [ ] DocumentAtom failure — document status set to Failed, error logged
- [ ] Chunking failure — document status set to Failed
- [ ] Embedding failure — document status set to Failed
- [ ] RecallDB indexing failure — document status set to Failed
- [ ] Status progression — Pending → Processing → Completed on success
- [ ] Status progression — Pending → Processing → Failed on error
- [ ] ProcessingLog entries created for each step
- [ ] Cancellation token respected — mid-pipeline cancellation stops processing
- [ ] Empty document (0 bytes) — handled gracefully
- [ ] Summarization step — invoked when configured, skipped when not

### 4.4 RetrievalService

- [ ] `RetrieveAsync` — embeds query then searches RecallDB, returns chunks
- [ ] Vector search mode — only embedding endpoint called
- [ ] Full-text search mode — embedding skipped, full-text query sent
- [ ] Hybrid search mode — both vector and full-text results merged
- [ ] `topK` parameter respected
- [ ] `scoreThreshold` filters low-scoring results
- [ ] Neighbor chunk retrieval — adjacent chunks fetched when configured
- [ ] Deduplication — duplicate chunks from overlapping results removed
- [ ] Empty results — returns empty list, no exception
- [ ] RecallDB unreachable — returns null or empty, logs warning

### 4.5 InferenceService

- [ ] `ListModelsAsync` — Ollama: calls `/api/tags`, returns model list
- [ ] `ListModelsAsync` — OpenAI: calls `/v1/models`, returns model list
- [ ] `PullModelAsync` — Ollama: sends POST to `/api/pull`
- [ ] `PullModelAsync` — OpenAI: returns error (not supported)
- [ ] Provider routing — correct endpoint called based on `InferenceProviderEnum`
- [ ] Bearer token / API key included in request headers
- [ ] Timeout handling — service unreachable returns null

### 4.6 TenantProvisioningService

- [ ] Provisions S3 bucket with correct naming convention (`{tenantId}_default`)
- [ ] Provisions RecallDB collection
- [ ] Creates default user and credential
- [ ] Idempotent — re-provisioning existing tenant does not duplicate resources

### 4.7 ProcessingLogService

- [ ] `WriteAsync` — creates log file in correct directory
- [ ] `ReadAsync` — reads back written log entries
- [ ] `CleanupAsync` — removes logs older than retention period
- [ ] Directory auto-creation when missing

### 4.8 Crawler services

- [ ] `CrawlerFactory` — returns `WebRepositoryCrawler` for `RepositoryTypeEnum.WebRepository`
- [ ] `CrawlerFactory` — unknown type throws or returns null
- [ ] `WebRepositoryCrawler.EnumerateAsync` — yields `CrawledObject` items
- [ ] Delta detection — previously-seen hashes produce no output
- [ ] New/changed content — different hash yields crawled object
- [ ] Filter settings respected — excluded URL patterns skipped
- [ ] `CrawlEnumeration` — tracks state correctly

---

## 5. Layer 4 — Authentication & Authorization

**Project:** `Test.Api`

Tests the authentication middleware and authorization checks in handler base methods.

### 5.1 Bearer token extraction

- [ ] Token from `Authorization: Bearer <token>` header
- [ ] Token from `?token=<token>` query parameter
- [ ] Missing token — 401 response
- [ ] Empty/whitespace token — 401 response
- [ ] Malformed `Authorization` header (no "Bearer" prefix) — 401

### 5.2 Role-based authorization

- [ ] Global admin can access any tenant's resources
- [ ] Tenant admin can access own tenant's resources
- [ ] Tenant admin cannot access another tenant's resources — 403
- [ ] Regular user can access own resources
- [ ] Regular user cannot access other users' resources in same tenant — 403
- [ ] `RequireGlobalAdmin` — non-admin gets 403
- [ ] `RequireAdmin` — regular user gets 403
- [ ] `ValidateTenantAccess` — mismatched tenant returns 403
- [ ] `EnforceTenantOwnership` — record from different tenant returns 403

### 5.3 AuthContext propagation

- [ ] Authenticated request populates `ctx.Metadata["authContext"]`
- [ ] `GetUser(ctx)` returns correct `UserMaster`
- [ ] `IsAdmin(ctx)` returns correct boolean
- [ ] Backward-compat metadata keys (`user`, `isAdmin`) also set

---

## 6. Layer 5 — API Handlers (Unit)

**Project:** `Test.Api`

Each handler tested with mocked services. Verify request validation, response codes, and response body structure.

### 6.1 TenantHandler

- [ ] `GET /v1.0/tenants` — returns paginated list, respects `maxResults` / `continuationToken`
- [ ] `GET /v1.0/tenants/{id}` — returns tenant by ID
- [ ] `GET /v1.0/tenants/{id}` — non-existent ID returns 404
- [ ] `PUT /v1.0/tenants` — creates tenant, returns 201 with `ten_` prefixed ID
- [ ] `PUT /v1.0/tenants` — missing name returns 400
- [ ] `POST /v1.0/tenants/{id}` — updates tenant
- [ ] `DELETE /v1.0/tenants/{id}` — deletes tenant
- [ ] `DELETE /v1.0/tenants/{id}` — non-existent returns 404
- [ ] Admin-only access enforced

### 6.2 UserHandler

- [ ] `GET /v1.0/users` — paginated list
- [ ] `GET /v1.0/users/{id}` — returns user
- [ ] `PUT /v1.0/users` — creates user with password hash
- [ ] `PUT /v1.0/users` — duplicate email returns 409
- [ ] `POST /v1.0/users/{id}` — updates user
- [ ] `DELETE /v1.0/users/{id}` — deletes user
- [ ] Password not included in response body
- [ ] Tenant scoping — users only see users in their tenant

### 6.3 CredentialHandler

- [ ] `GET /v1.0/credentials` — paginated list
- [ ] `PUT /v1.0/credentials` — creates credential with generated bearer token
- [ ] `DELETE /v1.0/credentials/{id}` — deletes credential
- [ ] Tenant scoping enforced

### 6.4 AssistantHandler

- [ ] `GET /v1.0/assistants` — paginated list
- [ ] `GET /v1.0/assistants/{id}` — returns assistant
- [ ] `PUT /v1.0/assistants` — creates assistant, sets tenant/user from auth context
- [ ] `PUT /v1.0/assistants` — missing name returns 400
- [ ] `POST /v1.0/assistants/{id}` — updates assistant
- [ ] `DELETE /v1.0/assistants/{id}` — deletes assistant
- [ ] `HEAD /v1.0/assistants/{id}` — exists check returns 200 or 404
- [ ] Tenant scoping enforced

### 6.5 AssistantSettingsHandler

- [ ] `GET /v1.0/assistants/{id}/settings` — returns settings
- [ ] `PUT /v1.0/assistants/{id}/settings` — creates/updates settings
- [ ] Settings bound to correct assistant ID

### 6.6 DocumentHandler

- [ ] `GET /v1.0/documents` — paginated list, filter by assistant
- [ ] `PUT /v1.0/documents` — upload file, triggers ingestion
- [ ] `DELETE /v1.0/documents/{id}` — deletes document + S3 object + RecallDB data
- [ ] `HEAD /v1.0/documents/{id}` — exists check
- [ ] File size validation (if applicable)
- [ ] Content-type detection

### 6.7 ChatHandler

- [ ] `PUT /v1.0/assistants/{id}/chat` — returns SSE stream
- [ ] Chat with retrieval — retrieval service called, context injected
- [ ] Chat without retrieval — direct LLM call
- [ ] Missing assistant ID — 404
- [ ] Inactive assistant — 400 or 404
- [ ] Chat history saved after completion
- [ ] Query rewriting — when enabled, multiple query variants generated
- [ ] Retrieval gate — when enabled, decides whether to retrieve

### 6.8 HistoryHandler

- [ ] `GET /v1.0/history` — paginated list, filter by assistant/thread
- [ ] `GET /v1.0/history/{id}` — returns single record
- [ ] `DELETE /v1.0/history/{id}` — deletes record

### 6.9 FeedbackHandler

- [ ] `GET /v1.0/feedback` — paginated list, filter by assistant
- [ ] `PUT /v1.0/feedback` — creates feedback with rating
- [ ] `DELETE /v1.0/feedback/{id}` — deletes feedback

### 6.10 IngestionRuleHandler

- [ ] `GET /v1.0/ingestion-rules` — paginated list
- [ ] `PUT /v1.0/ingestion-rules` — creates rule
- [ ] `POST /v1.0/ingestion-rules/{id}` — updates rule
- [ ] `DELETE /v1.0/ingestion-rules/{id}` — deletes rule

### 6.11 CollectionHandler

- [ ] `GET /v1.0/collections` — lists RecallDB collections
- [ ] Collection operations proxy to RecallDB correctly

### 6.12 BucketHandler

- [ ] `GET /v1.0/buckets` — lists S3 buckets
- [ ] Bucket operations proxy to S3 correctly

### 6.13 EmbeddingEndpointHandler

- [ ] `GET /v1.0/embedding-endpoints` — lists configured endpoints
- [ ] CRUD operations for embedding endpoint configuration

### 6.14 CompletionEndpointHandler

- [ ] `GET /v1.0/completion-endpoints` — lists configured endpoints
- [ ] CRUD operations for completion endpoint configuration

### 6.15 InferenceHandler

- [ ] `GET /v1.0/models` — lists available models from inference provider
- [ ] Model pull/delete operations (Ollama only)

### 6.16 ConfigurationHandler

- [ ] `GET /v1.0/configuration` — returns current config (admin only)
- [ ] Sensitive fields (passwords, API keys) redacted in response

### 6.17 CrawlPlanHandler

- [ ] `GET /v1.0/crawl-plans` — paginated list
- [ ] `PUT /v1.0/crawl-plans` — creates crawl plan
- [ ] `POST /v1.0/crawl-plans/{id}` — updates crawl plan
- [ ] `DELETE /v1.0/crawl-plans/{id}` — deletes crawl plan
- [ ] Tenant scoping enforced

### 6.18 CrawlOperationHandler

- [ ] `GET /v1.0/crawl-operations` — paginated list, filter by crawl plan
- [ ] `GET /v1.0/crawl-operations/{id}` — returns operation
- [ ] Operation state transitions validated

### 6.19 AuthenticateHandler

- [ ] `POST /v1.0/authenticate` — valid email/password returns token
- [ ] `POST /v1.0/authenticate` — invalid credentials returns 401
- [ ] `GET /v1.0/whoami` — returns authenticated user info

### 6.20 RootHandler

- [ ] `GET /` — returns server info / health check

---

## 7. Layer 6 — API Integration (HTTP)

**Project:** `Test.Integration`

End-to-end tests that start an in-process server with SQLite and mock external services, then make real HTTP requests.

### 7.1 Server lifecycle

- [ ] Server starts and responds to `GET /`
- [ ] Server initializes SQLite database
- [ ] Server creates default tenant/user/credential on first run

### 7.2 Authentication flow

- [ ] `POST /v1.0/authenticate` with email/password → receive bearer token
- [ ] Subsequent requests with bearer token succeed
- [ ] Requests without token return 401
- [ ] Requests with expired/invalid token return 401

### 7.3 CRUD lifecycle tests

Complete create → read → update → enumerate → delete cycle for each entity:

- [ ] Tenant lifecycle
- [ ] User lifecycle
- [ ] Credential lifecycle
- [ ] Assistant lifecycle
- [ ] Assistant settings lifecycle
- [ ] Document lifecycle (upload → status → delete)
- [ ] Feedback lifecycle
- [ ] Ingestion rule lifecycle
- [ ] Chat history lifecycle
- [ ] Crawl plan lifecycle
- [ ] Crawl operation lifecycle

### 7.4 Pagination

- [ ] Create N records, paginate with `maxResults=1`, verify all pages reached
- [ ] `continuationToken` from one page works on next request
- [ ] `EndOfResults=true` on final page
- [ ] `TotalRecords` accurate across pages

### 7.5 Multi-tenant isolation

- [ ] Create two tenants with separate users and credentials
- [ ] Tenant A's user cannot see Tenant B's assistants
- [ ] Tenant A's user cannot see Tenant B's documents
- [ ] Global admin can see both tenants' resources

### 7.6 Error handling

- [ ] Malformed JSON body → 400
- [ ] Non-existent endpoint → 404
- [ ] Method not allowed → 405
- [ ] Request to entity in wrong tenant → 403

---

## 8. Layer 7 — Background Services

**Project:** `Test.Services` (or `Test.Integration` for timer-based tests)

### 8.1 EndpointHealthCheckService

- [ ] Healthy endpoint → status reported as healthy
- [ ] Unreachable endpoint → status reported as unhealthy
- [ ] Intermittent failure → status transitions correctly
- [ ] Check interval respected (no excessive polling)

### 8.2 CrawlSchedulerService

- [ ] Crawl plan with schedule triggers at correct interval
- [ ] Disabled crawl plan does not trigger
- [ ] Concurrent crawl prevention — second trigger while first is running is skipped

### 8.3 CrawlOperationCleanupService

- [ ] Stale enumeration directories older than threshold are cleaned up
- [ ] Active enumeration directories are not removed

---

## 9. Running Tests

### Quick reference — each project runs independently

```bash
# Layer 1 — Models (no infrastructure required)
cd src/Test.Models
dotnet run
# Exit code: 0 = all passed, 1 = any failed

# Layer 2 — Database, SQLite (no infrastructure required)
cd src/Test.Database
dotnet run -- --type sqlite
# Exit code: 0 = all passed, 1 = any failed

# Layer 2 — Database, PostgreSQL (requires running instance)
cd src/Test.Database
dotnet run -- --type postgres --host 127.0.0.1 --user postgres --pass <pw> --name testdb

# Layer 3 — Services (mocked, no infrastructure required)
cd src/Test.Services
dotnet run
# Exit code: 0 = all passed, 1 = any failed

# Layer 4+5 — API Handlers (mocked, no infrastructure required)
cd src/Test.Api
dotnet run
# Exit code: 0 = all passed, 1 = any failed

# Layer 6 — Integration (starts in-process server with SQLite, mocked external services)
cd src/Test.Integration
dotnet run
# Exit code: 0 = all passed, 1 = any failed
```

### run-tests script

The `run-tests.sh` (and `run-tests.ps1`) script runs every test project sequentially, captures each exit
code, and prints a final cross-project summary:

```
==============================================================
  CROSS-PROJECT TEST SUMMARY
==============================================================
  PASS  Test.Models          (312ms)
  PASS  Test.Database        (1204ms)
  FAIL  Test.Services        (876ms)
  PASS  Test.Api             (543ms)
  PASS  Test.Integration     (2108ms)
--------------------------------------------------------------
  Total runtime: 5043ms
  OVERALL: FAIL (1 project failed)
==============================================================
```

Exit code: `0` only if every project returned `0`.

### CI pipeline checklist

- [ ] `Test.Models` runs on every PR (no dependencies)
- [ ] `Test.Database --type sqlite` runs on every PR
- [ ] `Test.Services` runs on every PR (mocked)
- [ ] `Test.Api` runs on every PR (mocked)
- [ ] `Test.Integration` runs on every PR (SQLite + mock external services)
- [ ] `Test.Database --type postgres` runs nightly (requires PostgreSQL service container)
- [ ] `Test.Database --type mysql` runs nightly (requires MySQL service container)
- [ ] `Test.Database --type sqlserver` runs nightly (requires SQL Server service container)

### Output contract verification

- [ ] Every test project compiles as a console application (`<OutputType>Exe</OutputType>`)
- [ ] Every test project returns exit code `0` on all-pass, `1` on any failure
- [ ] Every test project prints per-test `PASS`/`FAIL` with name and runtime in milliseconds
- [ ] Every test project prints `TEST SUMMARY` with total/passed/failed counts and total runtime
- [ ] Every test project enumerates all failed tests with error messages in the summary
- [ ] Every test project prints `OVERALL: PASS` or `OVERALL: FAIL` as the final line
- [ ] `run-tests.sh` / `run-tests.ps1` prints the cross-project summary shown above

---

## Progress Summary

| Layer | Project | Tests | Status |
|-------|---------|-------|--------|
| 1 — Models & Enums | Test.Models | ~40 | `[ ] Not started` |
| 2 — Database Drivers | Test.Database | ~200 existing + ~50 new | `[~] Partial (9 suites exist, 2 missing)` |
| 3 — Core Services | Test.Services | ~60 | `[ ] Not started` |
| 4 — Auth & Authz | Test.Api | ~15 | `[ ] Not started` |
| 5 — API Handlers | Test.Api | ~80 | `[ ] Not started` |
| 6 — API Integration | Test.Integration | ~30 | `[ ] Not started` |
| 7 — Background Services | Test.Services | ~10 | `[ ] Not started` |
| **Total** | | **~485** | |
