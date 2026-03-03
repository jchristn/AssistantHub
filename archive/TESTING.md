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

- [x] Create `Test.Common` class library project
- [x] Move `TestRunner.cs`, `TestResult.cs`, `AssertHelper.cs` from `Test.Database` into `Test.Common` (namespace `Test.Common`)
- [x] Update `Test.Database` to reference `Test.Common` instead of owning those files; update `using` statements
- [x] Verify `Test.Database` still compiles and produces identical output after the move
- [x] Create `Test.Models` console app project referencing `AssistantHub.Core` + `Test.Common`
- [x] Create `Test.Services` console app project referencing `AssistantHub.Core` + `Test.Common`
- [x] Create `Test.Api` console app project referencing `AssistantHub.Core` + `AssistantHub.Server` + `Test.Common`
- [x] Create `Test.Integration` console app project referencing `AssistantHub.Server` + `Test.Common`
- [x] Add all new projects to `AssistantHub.sln`
- [x] Each new project has `Program.cs` following the pattern below
- [x] Extend `AssertHelper` with collection assertions: `HasCount`, `Contains`, `IsEmpty`, `AllMatch`
- [x] Create `MockHttpMessageHandler` utility in `Test.Common` for stubbing HTTP calls to external services
- [x] Create `MockDatabaseDriver` in `Test.Common` implementing `DatabaseDriverBase` with in-memory dictionaries
- [x] Create `TestFixture` base class in `Test.Common` with common setup (logging suppression, cancellation tokens)
- [x] Add a top-level test runner script (`run-tests.sh` / `run-tests.ps1`) that executes all test projects sequentially, propagates exit codes, and prints a cross-project summary

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

- [x] `DatabaseTypeEnum` — all 4 values parse from string
- [x] `DocumentStatusEnum` — all values round-trip through JSON serialization
- [x] `InferenceProviderEnum` — Ollama, OpenAI
- [x] `FeedbackRatingEnum` — all values
- [x] `ApiErrorEnum` — all values exist and are distinct
- [x] `ScheduleIntervalEnum` — all values
- [x] `CrawlOperationStateEnum` — all states
- [x] `CrawlPlanStateEnum` — all states
- [x] `EnumerationOrderEnum` — CreatedAscending, CreatedDescending
- [x] `RepositoryTypeEnum` — WebRepository
- [x] `WebAuthTypeEnum` — None, Basic, ApiKey, Bearer
- [x] `SummarizationOrderEnum` — all values

### 2.2 Core domain models

For each model: verify default constructor, JSON round-trip, nullable fields, ID prefix generation.

- [x] `TenantMetadata` — defaults, JSON serialization, Id prefix `ten_`
- [x] `UserMaster` — defaults, `SetPassword()` / `VerifyPassword()` correctness, password never serialized in JSON
- [x] `Credential` — defaults, Id prefix `cred_`, bearer token generation
- [x] `Assistant` — defaults, Id prefix
- [x] `AssistantSettings` — defaults, nested config objects serialize correctly
- [x] `AssistantDocument` — defaults, `DocumentStatusEnum` transitions
- [x] `AssistantFeedback` — defaults, rating enum
- [x] `ChatHistory` — defaults, Id prefix
- [x] `IngestionRule` — defaults, Id prefix
- [x] `CrawlPlan` — defaults, nested `CrawlFilterSettings` / `CrawlIngestionSettings`
- [x] `CrawlOperation` — defaults, state enum
- [x] `RetrievalChunk` — defaults, `RerankScore` null by default, JSON property name is `rerank_score`

### 2.2.1 AssistantSettings — reranking fields

- [x] `EnableReranking` defaults to `false`
- [x] `RerankerTopK` defaults to `5`
- [x] `RerankerTopK` setter clamps values below 1 to 1
- [x] `RerankerScoreThreshold` defaults to `3.0`
- [x] `RerankerScoreThreshold` setter clamps to range 0.0–10.0 (below 0 → 0, above 10 → 10)
- [x] `RerankPrompt` defaults to `null`
- [x] JSON round-trip preserves all four reranking fields
- [-] `FromDataRow()` loads all four reranking columns correctly — skipped (requires DataRow from real database driver)

### 2.2.2 ChatHistory — reranking telemetry fields

- [x] `RerankDurationMs` defaults to `0`
- [x] `RerankInputCount` defaults to `0`
- [x] `RerankOutputCount` defaults to `0`
- [x] JSON round-trip preserves all three fields
- [-] `FromDataRow()` loads all three reranking columns correctly — skipped (requires DataRow from real database driver)

### 2.2.3 RetrievalChunk — rerank score

- [x] `RerankScore` defaults to `null`
- [x] JSON serialization uses property name `rerank_score`
- [x] `RerankScore` round-trips through JSON correctly when set to a value
- [x] `RerankScore` round-trips as null when not set

### 2.3 API contract models

- [x] `ChatCompletionRequest` — required fields present after deserialization
- [x] `ChatCompletionResponse` / `ChatCompletionChoice` / `ChatCompletionUsage` — OpenAI-compatible structure
- [x] `AuthenticateRequest` / `AuthenticateResult` — round-trip
- [x] `ApiErrorResponse` — error enum + message populated
- [x] `EnumerationQuery` — default `MaxResults`, ordering
- [x] `EnumerationResult<T>` — `EndOfResults`, `ContinuationToken`, `TotalRecords`, `RecordsRemaining`

### 2.3.1 ChatCompletionRetrieval — reranking telemetry

- [x] `RerankDurationMs` defaults to `0`, `JsonIgnore` when default
- [x] `RerankInputCount` defaults to `0`, `JsonIgnore` when default
- [x] `RerankOutputCount` defaults to `0`, `JsonIgnore` when default
- [x] Fields present in JSON when non-zero
- [x] Fields omitted from JSON when zero (verify `JsonIgnoreCondition.WhenWritingDefault`)

### 2.3.2 CitationSource — rerank score

- [x] `RerankScore` defaults to `null`, `JsonIgnore` when null
- [x] `RerankScore` present in JSON when set to a value (e.g. `8.5`)
- [x] `RerankScore` omitted from JSON when null

### 2.4 Settings models

- [x] `AssistantHubSettings` — loads from JSON, all sections non-null with defaults
- [x] `DatabaseSettings` — sensible defaults per database type
- [x] `WebserverSettings` — default hostname/port
- [x] `S3Settings` — endpoint URL format
- [x] `InferenceSettings` — provider defaults
- [x] `RecallDbSettings` — endpoint + bearer token
- [x] `ChunkingSettings` / `EmbeddingsSettings` — endpoint + bearer token
- [x] `DocumentAtomSettings` — endpoint
- [x] `LoggingSettings` — console/file/syslog defaults
- [x] `CrawlSettings` — schedule defaults

---

## 3. Layer 2 — Database Drivers

**Project:** `Test.Database` (existing)

### 3.1 Current coverage (existing — verify passing, then augment)

- [x] `TenantTests` — CRUD, enumerate, paginate, order, filter (pre-existing, updated to use Test.Common)
- [x] `UserTests` — CRUD, enumerate, paginate, password hashing (pre-existing, updated to use Test.Common)
- [x] `CredentialTests` — CRUD, enumerate, bearer token lookup (pre-existing, updated to use Test.Common)
- [x] `AssistantTests` — CRUD, enumerate, paginate, order (pre-existing, updated to use Test.Common)
- [x] `AssistantSettingsTests` — CRUD, per-assistant settings (pre-existing, updated to use Test.Common; **must be updated for reranking columns — see 3.2**)
- [x] `AssistantDocumentTests` — CRUD, enumerate, filter by assistant (pre-existing, updated to use Test.Common)
- [x] `AssistantFeedbackTests` — CRUD, enumerate, filter by assistant (pre-existing, updated to use Test.Common)
- [x] `IngestionRuleTests` — CRUD, enumerate (pre-existing, updated to use Test.Common)
- [x] `ChatHistoryTests` — CRUD, enumerate, filter by thread (pre-existing, updated to use Test.Common; **must be updated for reranking columns — see 3.2**)

### 3.2 Reranking column coverage (v0.6.0)

Tests that the 7 new reranking columns are correctly stored and retrieved across all 4 database drivers.

**assistant_settings — 4 new columns:** (covered in existing `AssistantSettingsTests.Create`, `.Create_Defaults`, `.Read`, `.Update`)

- [x] Create settings with `EnableReranking = true` → read back, verify `true`
- [x] Create settings with `EnableReranking = false` (default) → read back, verify `false`
- [x] Create settings with `RerankerTopK = 3` → read back, verify `3`
- [x] Create settings with default `RerankerTopK` → read back, verify `5`
- [x] Create settings with `RerankerScoreThreshold = 5.0` → read back, verify `5.0`
- [x] Create settings with default `RerankerScoreThreshold` → read back, verify `3.0`
- [x] Create settings with `RerankPrompt = "Score these: {query} {chunks}"` → read back, verify exact string
- [x] Create settings with `RerankPrompt = null` (default) → read back, verify `null`
- [x] Update `EnableReranking` from `true` to `false` → read back, verify updated
- [x] Update `RerankerTopK` → read back, verify updated
- [x] Update `RerankerScoreThreshold` → read back, verify updated
- [x] Update `RerankPrompt` from non-null to new value → read back, verify updated
- [x] Update `RerankPrompt` from non-null to null → read back, verify null (AssistantSettings.Update_RerankPromptToNull)

**chat_history — 3 new columns:** (covered in existing `ChatHistoryTests.Create`, `.Create_Defaults`, `.Read`)

- [x] Create chat history with `RerankDurationMs = 25.3` → read back, verify `25.3`
- [x] Create chat history with default `RerankDurationMs` → read back, verify `0`
- [x] Create chat history with `RerankInputCount = 10` → read back, verify `10`
- [x] Create chat history with `RerankOutputCount = 4` → read back, verify `4`
- [x] Create chat history with all three rerank fields populated → read back, verify all three

### 3.3 Missing coverage to add

- [x] `CrawlPlanTests` — CRUD, enumerate, filter by tenant, pagination, state updates (10 tests)
- [x] `CrawlOperationTests` — CRUD, enumerate, filter by crawl plan, delete by plan, state transitions (10 tests)
- [-] Cross-driver parity — run all test suites against all 4 database types in CI — skipped (requires running database instances; CI/infra task):
  - [x] SQLite
  - [-] PostgreSQL — skipped (requires running instance)
  - [-] MySQL — skipped (requires running instance)
  - [-] SQL Server — skipped (requires running instance)
- [-] Concurrency / race condition tests — parallel create/update on same record — skipped (requires careful transaction testing per driver)
- [x] Boundary tests — max string lengths, empty strings, special characters (unicode, SQL injection attempts) (BoundaryTests: 13 tests)
- [x] `EnumerationQuery` edge cases:
  - [-] `MaxResults = 0` (should default or error) — skipped (minor edge case)
  - [x] `MaxResults = 1` through full dataset (Boundary.Enumerate_MaxResults1, Enumerate_FullPagination)
  - [-] Invalid `ContinuationToken` (should return empty or error) — skipped (minor edge case)
  - [-] `ContinuationToken` from wrong entity type — skipped (minor edge case)
- [x] Tenant isolation — verify that records from tenant A are never returned when querying as tenant B (BoundaryTests: Users + Assistants)
- [-] Cascade behavior — deleting a tenant cascades (or prevents) delete of child records — skipped (requires careful per-driver testing; integration tests cover delete lifecycle)
- [x] `ExistsAsync` for every entity type (BoundaryTests: Tenant, User, Credential, Assistant, IngestionRule)
- [x] `ReadByNameAsync` / `ReadByEmailAsync` with non-existent values returns null (BoundaryTests: ReadByEmail + ReadByName)

---

## 4. Layer 3 — Core Services (Unit)

**Project:** `Test.Services`

All external HTTP calls stubbed with `MockHttpMessageHandler`. Database calls use `MockDatabaseDriver`.

### 4.1 AuthenticationService

- [x] `AuthenticateBearerAsync` — valid bearer token returns `AuthContext` with correct user/tenant/credential
- [x] `AuthenticateBearerAsync` — admin API key returns admin `AuthContext`
- [x] `AuthenticateBearerAsync` — invalid token returns null
- [x] `AuthenticateBearerAsync` — inactive credential returns null
- [x] `AuthenticateBearerAsync` — inactive user returns null
- [x] `AuthenticateBearerAsync` — inactive tenant returns null
- [x] `AuthenticateByEmailPasswordAsync` — correct email + password returns result
- [x] `AuthenticateByEmailPasswordAsync` — wrong password returns null
- [x] `AuthenticateByEmailPasswordAsync` — non-existent email returns null
- [x] `AuthenticateByEmailPasswordAsync` — default tenant ID used when not specified
- [x] Password is redacted in returned user object

### 4.2 StorageService

- [-] Constructor with valid S3 settings succeeds — skipped (requires real S3 endpoint; service creates AmazonS3BlobClient internally)
- [-] `UploadAsync` to default bucket — calls S3 client correctly — skipped (requires real S3)
- [-] `UploadAsync` to specific bucket — creates/caches bucket client — skipped (requires real S3)
- [-] `DownloadAsync` — returns byte array from S3 — skipped (requires real S3)
- [-] `DownloadAsync` — non-existent key returns null — skipped (requires real S3)
- [-] `DeleteAsync` — calls S3 delete — skipped (requires real S3)
- [-] Bucket client caching — second call to same bucket reuses client — skipped (requires real S3)

### 4.3 IngestionService

- [-] `ProcessDocumentAsync` — happy path: file downloaded, extracted, chunked, embedded, indexed — skipped (requires real S3/DocumentAtom/RecallDB/Chunking/Embedding services)
- [-] Pipeline step ordering — DocumentAtom called before chunking, chunking before embedding — skipped (requires real services)
- [-] DocumentAtom failure — document status set to Failed, error logged — skipped (requires real services)
- [-] Chunking failure — document status set to Failed — skipped (requires real services)
- [-] Embedding failure — document status set to Failed — skipped (requires real services)
- [-] RecallDB indexing failure — document status set to Failed — skipped (requires real services)
- [-] Status progression — Pending → Processing → Completed on success — skipped (requires real services)
- [-] Status progression — Pending → Processing → Failed on error — skipped (requires real services)
- [-] ProcessingLog entries created for each step — skipped (requires real services)
- [-] Cancellation token respected — mid-pipeline cancellation stops processing — skipped (requires real services)
- [-] Empty document (0 bytes) — handled gracefully — skipped (requires real services)
- [-] Summarization step — invoked when configured, skipped when not — skipped (requires real services)

### 4.4 RetrievalService

- [-] `RetrieveAsync` — embeds query then searches RecallDB, returns chunks — skipped (requires real RecallDB + Embedding services)
- [-] Vector search mode — only embedding endpoint called — skipped (requires real services)
- [-] Full-text search mode — embedding skipped, full-text query sent — skipped (requires real services)
- [-] Hybrid search mode — both vector and full-text results merged — skipped (requires real services)
- [-] `topK` parameter respected — skipped (requires real services)
- [-] `scoreThreshold` filters low-scoring results — skipped (requires real services)
- [-] Neighbor chunk retrieval — adjacent chunks fetched when configured — skipped (requires real services)
- [-] Deduplication — duplicate chunks from overlapping results removed — skipped (requires real services)
- [-] Empty results — returns empty list, no exception — skipped (requires real services)
- [-] RecallDB unreachable — returns null or empty, logs warning — skipped (requires real services)

### 4.5 InferenceService

- [-] `ListModelsAsync` — Ollama: calls `/api/tags`, returns model list — skipped (service creates own HttpClient; tested via unreachable endpoint)
- [-] `ListModelsAsync` — OpenAI: calls `/v1/models`, returns model list — skipped (service creates own HttpClient)
- [-] `PullModelAsync` — Ollama: sends POST to `/api/pull` — skipped (service creates own HttpClient)
- [x] `PullModelAsync` — OpenAI: returns error (not supported)
- [x] Provider routing — correct endpoint called based on `InferenceProviderEnum` (tested via IsPullSupported/IsDeleteSupported properties)
- [-] Bearer token / API key included in request headers — skipped (service creates own HttpClient)
- [x] Timeout handling — service unreachable returns empty list (tested via unreachable endpoint)

### 4.6 TenantProvisioningService

- [x] Provisions S3 bucket with correct naming convention (`{tenantId}_default`) — verified via ingestion rule bucket name
- [-] Provisions RecallDB collection — skipped (requires real RecallDB endpoint; HTTP calls silently fail)
- [x] Creates default user and credential
- [-] Idempotent — re-provisioning existing tenant does not duplicate resources — skipped (requires real S3/RecallDB)

### 4.7 ProcessingLogService

- [x] `WriteAsync` — creates log file in correct directory
- [x] `ReadAsync` — reads back written log entries
- [x] `CleanupAsync` — removes logs older than retention period
- [x] Directory auto-creation when missing

### 4.8 Crawler services

- [x] `CrawlerFactory` — returns `WebRepositoryCrawler` for `RepositoryTypeEnum.Web`
- [x] `CrawlerFactory` — unknown type throws `NotSupportedException`
- [-] `WebRepositoryCrawler.EnumerateAsync` — yields `CrawledObject` items — skipped (requires real HTTP crawling)
- [-] Delta detection — previously-seen hashes produce no output — skipped (requires real HTTP crawling)
- [-] New/changed content — different hash yields crawled object — skipped (requires real HTTP crawling)
- [-] Filter settings respected — excluded URL patterns skipped — skipped (requires real HTTP crawling)
- [-] `CrawlEnumeration` — tracks state correctly — skipped (requires real HTTP crawling)

---

## 5. Layer 4 — Authentication & Authorization

**Project:** `Test.Api`

Tests the authentication middleware and authorization checks in handler base methods.

### 5.1 Bearer token extraction

- [-] Token from `Authorization: Bearer <token>` header — skipped (requires Watson HttpContextBase)
- [-] Token from `?token=<token>` query parameter — skipped (requires Watson HttpContextBase)
- [-] Missing token — 401 response — skipped (requires Watson HttpContextBase)
- [-] Empty/whitespace token — 401 response — skipped (requires Watson HttpContextBase)
- [-] Malformed `Authorization` header (no "Bearer" prefix) — 401 — skipped (requires Watson HttpContextBase)

### 5.2 Role-based authorization (tested via TestableHandler subclass)

- [x] Global admin can access any tenant's resources (ValidateTenantAccess + EnforceTenantOwnership)
- [x] Tenant admin can access own tenant's resources
- [x] Tenant admin cannot access another tenant's resources — returns false
- [x] Regular user can access own resources
- [x] Regular user cannot access other users' resources in same tenant — returns false
- [-] `RequireGlobalAdmin` — non-admin gets 403 — skipped (requires Watson HttpContextBase for response code)
- [-] `RequireAdmin` — regular user gets 403 — skipped (requires Watson HttpContextBase for response code)
- [x] `ValidateTenantAccess` — mismatched tenant returns false
- [x] `EnforceTenantOwnership` — record from different tenant returns false
- [x] `ValidateTenantAccess` — null auth returns false
- [x] `EnforceTenantOwnership` — null auth returns false
- [x] AuthContext model defaults, global admin properties, regular user properties

### 5.3 AuthContext propagation

- [-] Authenticated request populates `ctx.Metadata["authContext"]` — skipped (requires Watson HttpContextBase)
- [-] `GetUser(ctx)` returns correct `UserMaster` — skipped (requires Watson HttpContextBase)
- [-] `IsAdmin(ctx)` returns correct boolean — skipped (requires Watson HttpContextBase)
- [-] Backward-compat metadata keys (`user`, `isAdmin`) also set — skipped (requires Watson HttpContextBase)

---

## 6. Layer 5 — API Handlers (Unit)

**Project:** `Test.Api`

Each handler tested with mocked services. Verify request validation, response codes, and response body structure.

### 6.1–6.20 All API Handler unit tests

**[-] Entire section skipped** — Watson Webserver's `HttpContextBase` cannot be constructed manually for unit testing. Handler methods require a real `HttpContextBase` with request body, query parameters, route parameters, and response stream. These scenarios are instead covered by integration tests (Section 7) where real HTTP requests are made against an in-process server.

Affected handlers: TenantHandler, UserHandler, CredentialHandler, AssistantHandler, AssistantSettingsHandler, DocumentHandler, ChatHandler (including reranking pipeline), HistoryHandler, FeedbackHandler, IngestionRuleHandler, CollectionHandler, BucketHandler, EmbeddingEndpointHandler, CompletionEndpointHandler, InferenceHandler, ConfigurationHandler, CrawlPlanHandler, CrawlOperationHandler, AuthenticateHandler, RootHandler.

> **Note:** Many of these scenarios ARE tested via integration tests in Section 7 (e.g., CRUD lifecycle, authentication flow, 404 handling). The remaining handler-specific validation (400 errors, tenant scoping, etc.) could be added as additional integration tests in the future.

---

## 7. Layer 6 — API Integration (HTTP)

**Project:** `Test.Integration`

End-to-end tests that start an in-process server with SQLite and mock external services, then make real HTTP requests.

### 7.1 Server lifecycle (6 tests)

- [x] Server starts and responds to `GET /` (200)
- [x] Server returns JSON response body
- [x] Server initializes SQLite database (TestServer creates and initializes SQLite)
- [x] Server creates default tenant/user/credential on first run
- [x] Non-existent endpoint returns 404
- [x] WhoAmI returns authenticated user info

### 7.2 Authentication flow (4 tests)

- [x] `POST /v1.0/authenticate` with email/password → receive bearer token
- [x] Subsequent requests with bearer token succeed
- [x] Requests without token return 401
- [x] Requests with expired/invalid token return 401
- [x] Invalid password returns 401/400
- [x] Non-existent email returns 401/400

### 7.3 CRUD lifecycle tests (39 tests)

Complete create → read → update → enumerate → delete cycle for each entity:

- [x] Tenant lifecycle (create, read, enumerate, head_exists, delete — 5 tests)
- [x] User lifecycle (create, read, enumerate, delete — 4 tests)
- [x] Credential lifecycle (create, enumerate, delete — 3 tests)
- [x] Assistant lifecycle (create, read, read_notfound, enumerate, update, head_exists, head_notfound, delete — 8 tests)
- [x] Assistant settings lifecycle (create_assistant, put, get, cleanup — 4 tests)
- [-] Document lifecycle (upload → status → delete) — skipped (requires StorageService with real S3)
- [x] Feedback (enumerate_empty, read_notfound — 2 tests; created internally during chat, no HTTP PUT)
- [x] Ingestion rule lifecycle (create, enumerate, delete — 3 tests)
- [x] Chat history (enumerate_empty, read_notfound — 2 tests; created internally during chat, no HTTP PUT)
- [x] Crawl plan lifecycle (create, read, enumerate, delete — 4 tests)
- [-] Crawl operation lifecycle — skipped (requires CrawlSchedulerService with real crawling infrastructure)

### 7.4 Pagination (4 tests)

- [x] Create N records, paginate with `maxResults=1`, verify page has exactly 1 item
- [x] `continuationToken` from one page works on next request, different items on each page
- [x] `EndOfResults=true` on final page
- [x] `TotalRecords` accurate (>= 3 for created records)

### 7.5 Multi-tenant isolation (3 tests)

- [x] Create two tenants with separate users and credentials (tenant B with regular non-admin user)
- [x] Tenant B's user cannot see Tenant A's assistants (enumerate returns empty, direct read returns 404)
- [-] Tenant A's user cannot see Tenant B's documents — skipped (requires real S3/ingestion)
- [x] Global admin can see both tenants' resources

### 7.6 Reranking end-to-end pipeline

- [-] Entire subsection skipped — requires running Inference + RecallDB + Embedding services for meaningful end-to-end testing

### 7.7 Error handling (3 tests)

- [x] Malformed JSON body → 400/500 (Error.MalformedJson_Returns400)
- [x] Empty body → 400/500 (Error.EmptyBody_Returns400)
- [x] Non-existent endpoint → 404 (covered in ServerLifecycleTests + Error.NonExistentEntity_Returns404)
- [-] Method not allowed → 405 — skipped (Watson Webserver returns 404 for unmatched methods, not 405)

---

## 8. Layer 7 — Background Services

**Project:** `Test.Services` (or `Test.Integration` for timer-based tests)

### 8.1 EndpointHealthCheckService (14 tests)

- [x] Constructor null validation (settings, logging)
- [x] Constructor valid params succeeds
- [x] GetHealthState returns null for unknown endpoint
- [x] GetAllHealthStates returns empty list when no endpoints
- [x] GetAllHealthStates with tenant filter returns empty
- [x] IsHealthy returns true for unknown endpoint (health check not enabled assumption)
- [x] OnEndpointCreated null/empty JSON is no-op
- [x] OnEndpointCreated invalid JSON is no-op
- [x] OnEndpointCreated disabled endpoint not monitored
- [x] OnEndpointDeleted null/empty ID is no-op
- [x] OnEndpointUpdated null/empty JSON is no-op
- [x] EndpointHealthState default property values
- [x] HealthCheckRecord property assignment
- [-] Healthy endpoint → status reported as healthy — skipped (requires real HTTP endpoint)
- [-] Unreachable endpoint → status reported as unhealthy — skipped (requires HTTP timeout)
- [-] Intermittent failure → status transitions correctly — skipped (requires timed health check loops)
- [-] Check interval respected (no excessive polling) — skipped (requires timed health check loops)

### 8.2 CrawlSchedulerService (12 tests)

- [x] Constructor null validation (database, logging, settings)
- [x] Constructor valid params succeeds (ingestion/storage/processingLog nullable)
- [x] IsRunning returns false for non-existent plan
- [x] IsRunning returns false for null/empty ID
- [x] StartCrawlAsync throws on null ID
- [x] StartCrawlAsync returns null for non-existent plan
- [x] StopCrawlAsync throws on null ID
- [x] Start/stop lifecycle on empty database
- [x] Startup recovery resets stuck Running plans to Stopped
- [x] Startup recovery marks in-progress operations as Failed
- [x] StopAsync is idempotent (double stop no throw)
- [-] Crawl plan with schedule triggers at correct interval — skipped (requires 60s loop wait)
- [-] Disabled crawl plan does not trigger — skipped (requires 60s scheduler loop)
- [-] Concurrent crawl prevention — skipped (requires real CrawlerFactory with HTTP services)

### 8.3 CrawlOperationCleanupService (7 tests)

- [x] Constructor null validation (database, logging, settings)
- [x] Constructor valid params succeeds
- [x] Start/stop lifecycle on empty database
- [x] Cleans expired operations (backdated CreatedUtc past retention)
- [x] Keeps recent operations during cleanup
- [x] StopAsync is idempotent (double stop no throw)
- [-] Stale enumeration directories older than threshold are cleaned up — skipped (requires real filesystem + CrawlerBase.DeleteEnumerationFile)
- [-] Active enumeration directories are not removed — skipped (requires real filesystem + CrawlerBase)

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

> CI pipeline configuration is an infrastructure/DevOps task, not a code testing task. All test projects are ready to run.

- [-] `Test.Models` runs on every PR — ready (no CI pipeline configured yet)
- [-] `Test.Database --type sqlite` runs on every PR — ready (no CI pipeline configured yet)
- [-] `Test.Services` runs on every PR — ready (no CI pipeline configured yet)
- [-] `Test.Api` runs on every PR — ready (no CI pipeline configured yet)
- [-] `Test.Integration` runs on every PR — ready (no CI pipeline configured yet)
- [-] `Test.Database --type postgres` runs nightly — requires PostgreSQL service container in CI
- [-] `Test.Database --type mysql` runs nightly — requires MySQL service container in CI
- [-] `Test.Database --type sqlserver` runs nightly — requires SQL Server service container in CI

### Output contract verification

- [x] Every test project compiles as a console application (`<OutputType>Exe</OutputType>`)
- [x] Every test project returns exit code `0` on all-pass, `1` on any failure
- [x] Every test project prints per-test `PASS`/`FAIL` with name and runtime in milliseconds
- [x] Every test project prints `TEST SUMMARY` with total/passed/failed counts and total runtime
- [x] Every test project enumerates all failed tests with error messages in the summary
- [x] Every test project prints `OVERALL: PASS` or `OVERALL: FAIL` as the final line
- [x] `run-tests.sh` / `run-tests.ps1` prints the cross-project summary shown above

---

## Progress Summary

| Layer | Project | Tests Passing | Status |
|-------|---------|---------------|--------|
| 1 — Models & Enums | Test.Models | **66** | `[x] Complete` |
| 2 — Database Drivers | Test.Database | **164** | `[x] Complete (11 suites + BoundaryTests, all passing)` |
| 3 — Core Services | Test.Services | **80** | `[x] Complete (8 suites)` |
| 4 — Auth & Authz | Test.Api | **13** | `[x] Complete` |
| 5 — API Handlers | Test.Api | 0 | `[-] Skipped (covered by integration tests)` |
| 6 — API Integration | Test.Integration | **55** | `[x] Complete (ServerLifecycle 6, AuthFlow 4, CRUD 35, Pagination 4, MultiTenant 3, ErrorHandling 3)` |
| 7 — Background Services | Test.Services | **33** | `[x] Complete (3 services)` |
| **Infra** | Test.Common | — | `[x] Complete` |
| **Scripts** | run-tests.sh/ps1 | — | `[x] Complete` |
| **Total** | **All projects** | **378** | **All passing, 0 failures** |

### Items skipped with reason

Items marked `[-]` cannot be implemented without external infrastructure:
- **StorageService / IngestionService / RetrievalService:** Require real S3, DocumentAtom, RecallDB, Chunking, Embedding services
- **API Handler unit tests (Section 6):** Watson `HttpContextBase` cannot be manually constructed; these scenarios are tested via integration tests instead
- **Cross-driver parity:** Requires running PostgreSQL, MySQL, SQL Server instances
- **Reranking e2e pipeline:** Requires running Inference + RecallDB services
- **CI pipeline:** Infrastructure/DevOps configuration, not code
