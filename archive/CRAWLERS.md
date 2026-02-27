# Crawlers Implementation Plan

> **Status: COMPLETE** — All sections implemented and build verified (0 errors). Implemented 2026-02-26.

This document describes the implementation plan for adding native crawler support to AssistantHub. Each task is annotated with a checkbox for tracking completion.

---

## Table of Contents

1. [Data Model](#1-data-model)
2. [Enums](#2-enums)
3. [Constants & ID Generation](#3-constants--id-generation)
4. [Database Layer](#4-database-layer)
5. [Crawler Engine](#5-crawler-engine)
6. [Background Services](#6-background-services)
7. [API Handlers](#7-api-handlers)
8. [Dashboard](#8-dashboard)
9. [Docker & Infrastructure](#9-docker--infrastructure)
10. [Migration Script](#10-migration-script)

---

## 1. Data Model

### 1.1 CrawlPlan Model

- [x]Create `src/AssistantHub.Core/Models/CrawlPlan.cs`

Represents a crawler configuration. Consolidates View's separate CrawlPlan, CrawlSchedule, CrawlFilter, and DataRepository into a single object with nested sub-objects stored as JSON columns.

```
Properties:
├── Id                  string, prefix "cplan_", K-sortable, not null
├── TenantId            string, not null, default "default"
├── Name                string, not null, default "My crawl plan"
├── RepositoryType      RepositoryTypeEnum, not null, default Web
├── IngestionSettings   CrawlIngestionSettings (sub-object, stored as JSON)
│   ├── IngestionRuleId string, nullable
│   ├── StoreInS3       bool, default true
│   └── S3BucketName    string, nullable
├── RepositorySettings  CrawlRepositorySettings (sub-object, stored as JSON)
│   └── (see §1.2)
├── Schedule            CrawlScheduleSettings (sub-object, stored as JSON)
│   ├── IntervalType    ScheduleIntervalEnum (OneTime, Minutes, Hours, Days, Weeks)
│   └── IntervalValue   int, default 24, clamped 1–10080
├── Filter              CrawlFilterSettings (sub-object, stored as JSON)
│   ├── ObjectPrefix    string, nullable
│   ├── ObjectSuffix    string, nullable
│   ├── AllowedContentTypes  List<string>, nullable
│   ├── MinimumSize     long, default 0, clamped ≥ 0
│   └── MaximumSize     long, nullable, clamped ≥ 0 when set
├── ProcessAdditions    bool, default true
├── ProcessUpdates      bool, default true
├── ProcessDeletions    bool, default false
├── MaxDrainTasks       int, default 8, clamped 1–64
├── RetentionDays       int, default 7, clamped 0–14
├── State               CrawlPlanStateEnum (Stopped, Running), default Stopped
├── LastCrawlStartUtc   DateTime?, nullable
├── LastCrawlFinishUtc  DateTime?, nullable
├── LastCrawlSuccess    bool?, nullable
├── CreatedUtc          DateTime, default UtcNow
└── LastUpdateUtc       DateTime, default UtcNow
```

Code style requirements:
- Private backing fields named `_LikeThis`
- Setter validation with null checks and value clamping
- XML documentation with default values and ranges
- `FromDataRow(DataRow)` static factory method
- No `var`, no tuples

### 1.2 CrawlRepositorySettings (Web)

- [x]Create `src/AssistantHub.Core/Models/CrawlRepositorySettings.cs`

Base class for repository-type-specific settings. Initially only web is implemented.

Base class should have a property called `RepositoryType` which is an enum containing only `Web` initially.

```
CrawlRepositorySettings (base, abstract or marker)

WebCrawlRepositorySettings : CrawlRepositorySettings
├── AuthenticationType      WebAuthTypeEnum (None, Basic, ApiKey, BearerToken), default None
├── Username                string, nullable (Basic auth)
├── Password                string, nullable (Basic auth)
├── ApiKeyHeader            string, nullable (ApiKey auth)
├── ApiKeyValue             string, nullable (ApiKey auth)
├── BearerToken             string, nullable (BearerToken auth)
├── UserAgent               string, default "assistanthub-crawler"
├── StartUrl                string, not null
├── UseHeadlessBrowser      bool, default false
├── FollowLinks             bool, default true
├── FollowRedirects         bool, default true
├── ExtractSitemapLinks     bool, default true
├── RestrictToChildUrls     bool, default true
├── RestrictToSubdomain     bool, default false
├── RestrictToRootDomain    bool, default true
├── IgnoreRobotsTxt         bool, default false
├── MaxDepth                int, default 5, clamped 1–100
├── MaxParallelTasks        int, default 8, clamped 1–64
└── CrawlDelayMs            int, default 100, clamped 0–60000
```

Defaults aligned with View's DataRepository. `UseHeadlessBrowser` defaults to false (Playwright adds ~100MB to the image and is significantly slower; enable only for JavaScript-heavy sites). `RestrictToChildUrls` defaults to true for safety (prevents unbounded crawling). `CrawlDelayMs` defaults to 100ms to be a respectful crawler.

### 1.3 CrawlIngestionSettings

- [x]Create `src/AssistantHub.Core/Models/CrawlIngestionSettings.cs`

```
├── IngestionRuleId     string, nullable
├── StoreInS3           bool, default true
└── S3BucketName        string, nullable
```

### 1.4 CrawlScheduleSettings

- [x]Create `src/AssistantHub.Core/Models/CrawlScheduleSettings.cs`

```
├── IntervalType    ScheduleIntervalEnum, default Hours
└── IntervalValue   int, default 24, clamped 1–10080
```

### 1.5 CrawlFilterSettings

- [x]Create `src/AssistantHub.Core/Models/CrawlFilterSettings.cs`

```
├── ObjectPrefix            string, nullable
├── ObjectSuffix            string, nullable
├── AllowedContentTypes     List<string>, nullable
├── MinimumSize             long, default 0, clamped ≥ 0
└── MaximumSize             long, nullable, clamped ≥ 0 when set
```

### 1.6 Document Traceability

- [x]Add columns to `AssistantDocument` model to link documents back to crawl operations:

```
├── CrawlPlanId         string, nullable  — which crawler produced this document
├── CrawlOperationId    string, nullable  — which specific crawl run produced this document
├── SourceUrl           string, nullable  — the original URL/key from the crawl
```

These fields enable:
- Querying "was this file ingested successfully?" by looking up the document record and its `Status` / processing log
- Querying "when was this file crawled?" by following `CrawlOperationId` back to the operation's `StartUtc`
- Filtering the Documents table by `CrawlPlanId` to see all documents from a specific crawler
- The existing `ProcessingLogService` already provides per-document processing logs, so crawled documents inherit that capability automatically

Database changes: add `crawl_plan_id`, `crawl_operation_id`, and `source_url` nullable text columns to the `assistant_documents` table.

Use appropriate column types per database to facilitate creation of indexes for `crawl_plan_id` and `crawl_operation_id`.

### 1.7 CrawlOperation Model

- [x]Create `src/AssistantHub.Core/Models/CrawlOperation.cs`

Represents a single crawl execution linked to a CrawlPlan.

```
Properties:
├── Id                  string, prefix "cop_", K-sortable, not null
├── TenantId            string, not null, default "default"
├── CrawlPlanId         string, not null
├── State               CrawlOperationStateEnum, default NotStarted
├── StatusMessage       string, nullable
├── ObjectsEnumerated   long, default 0
├── BytesEnumerated     long, default 0
├── ObjectsAdded        long, default 0
├── BytesAdded          long, default 0
├── ObjectsUpdated      long, default 0
├── BytesUpdated        long, default 0
├── ObjectsDeleted      long, default 0
├── BytesDeleted        long, default 0
├── ObjectsSuccess      long, default 0
├── BytesSuccess        long, default 0
├── ObjectsFailed       long, default 0
├── BytesFailed         long, default 0
├── EnumerationFile         string, nullable (path to enumeration JSON on disk)
├── StartUtc                DateTime?, nullable
├── StartEnumerationUtc     DateTime?, nullable
├── FinishEnumerationUtc    DateTime?, nullable
├── StartRetrievalUtc       DateTime?, nullable
├── FinishRetrievalUtc      DateTime?, nullable
├── FinishUtc               DateTime?, nullable
├── CreatedUtc              DateTime, default UtcNow
└── LastUpdateUtc           DateTime, default UtcNow
```

Any property with default 0 above should be clamped to greater than or equal to 0.

Static factory: `FromDataRow(DataRow)`.

---

## 2. Enums

- [x]Create `src/AssistantHub.Core/Enums/RepositoryTypeEnum.cs`

```csharp
public enum RepositoryTypeEnum { Web }
// Future: AmazonS3, AzureBlob, File, CIFS, NFS, Dropbox, Box, Notion, Git
```

- [x]Create `src/AssistantHub.Core/Enums/ScheduleIntervalEnum.cs`

```csharp
public enum ScheduleIntervalEnum { OneTime, Minutes, Hours, Days, Weeks }
```

- [x]Create `src/AssistantHub.Core/Enums/CrawlPlanStateEnum.cs`

```csharp
public enum CrawlPlanStateEnum { Stopped, Running }
```

- [x]Create `src/AssistantHub.Core/Enums/CrawlOperationStateEnum.cs`

```csharp
public enum CrawlOperationStateEnum { NotStarted, Starting, Enumerating, Retrieving, Success, Failed, Stopped, Canceled }
```

- [x]Create `src/AssistantHub.Core/Enums/WebAuthTypeEnum.cs`

```csharp
public enum WebAuthTypeEnum { None, Basic, ApiKey, BearerToken }
```

---

## 3. Constants & ID Generation

- [x]Add to `Constants.cs`:

```csharp
public static string CrawlPlanIdentifierPrefix = "cplan_";
public static string CrawlOperationIdentifierPrefix = "cop_";
public static string CrawlPlansTable = "crawl_plans";
public static string CrawlOperationsTable = "crawl_operations";
```

- [x]Add to `IdGenerator.cs`:

```csharp
public static string NewCrawlPlanId()
public static string NewCrawlOperationId()
```

---

## 4. Database Layer

### 4.1 Interface

- [x]Create `src/AssistantHub.Core/Database/Interfaces/ICrawlPlanMethods.cs`

```csharp
Task<CrawlPlan> CreateAsync(CrawlPlan plan, CancellationToken token = default);
Task<CrawlPlan> ReadAsync(string id, CancellationToken token = default);
Task<CrawlPlan> UpdateAsync(CrawlPlan plan, CancellationToken token = default);
Task UpdateStateAsync(string id, CrawlPlanStateEnum state, CancellationToken token = default);
Task DeleteAsync(string id, CancellationToken token = default);
Task<bool> ExistsAsync(string id, CancellationToken token = default);
Task<EnumerationResult<CrawlPlan>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default);
```

- [x]Create `src/AssistantHub.Core/Database/Interfaces/ICrawlOperationMethods.cs`

```csharp
Task<CrawlOperation> CreateAsync(CrawlOperation operation, CancellationToken token = default);
Task<CrawlOperation> ReadAsync(string id, CancellationToken token = default);
Task<CrawlOperation> UpdateAsync(CrawlOperation operation, CancellationToken token = default);
Task DeleteAsync(string id, CancellationToken token = default);
Task<bool> ExistsAsync(string id, CancellationToken token = default);
Task<EnumerationResult<CrawlOperation>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default);
Task<EnumerationResult<CrawlOperation>> EnumerateByCrawlPlanAsync(string crawlPlanId, EnumerationQuery query, CancellationToken token = default);
Task DeleteByCrawlPlanAsync(string crawlPlanId, CancellationToken token = default);
Task DeleteExpiredAsync(string crawlPlanId, int retentionDays, CancellationToken token = default);
```

### 4.2 DatabaseDriverBase Update

- [x]Add properties to `DatabaseDriverBase.cs`:

```csharp
public ICrawlPlanMethods CrawlPlan { get; protected set; }
public ICrawlOperationMethods CrawlOperation { get; protected set; }
```

### 4.3 AssistantDocument Table Changes

- [x]Add columns to `assistant_documents` table:

```sql
ALTER TABLE assistant_documents ADD COLUMN crawl_plan_id TEXT;
ALTER TABLE assistant_documents ADD COLUMN crawl_operation_id TEXT;
ALTER TABLE assistant_documents ADD COLUMN source_url TEXT;
```

- [x]Add index:

```sql
CREATE INDEX IF NOT EXISTS idx_assistant_documents_crawl_plan_id ON assistant_documents(crawl_plan_id);
CREATE INDEX IF NOT EXISTS idx_assistant_documents_crawl_operation_id ON assistant_documents(crawl_operation_id);
```

- [x]Update `AssistantDocument.cs` model to include `CrawlPlanId`, `CrawlOperationId`, and `SourceUrl` properties
- [x]Update `AssistantDocumentMethods.cs` (all database drivers) to include new columns in CREATE, READ, UPDATE queries
- [x]Update `AssistantDocument.FromDataRow()` to read the new columns

### 4.4 SQLite Table Definitions (New Tables)

- [x]Add to `TableQueries.cs` `CreateTables()`:

```sql
CREATE TABLE IF NOT EXISTS crawl_plans (
  id TEXT PRIMARY KEY,
  tenant_id TEXT NOT NULL DEFAULT 'default',
  name TEXT NOT NULL,
  repository_type TEXT NOT NULL DEFAULT 'Web',
  ingestion_settings_json TEXT,
  repository_settings_json TEXT,
  schedule_json TEXT,
  filter_json TEXT,
  process_additions INTEGER NOT NULL DEFAULT 1,
  process_updates INTEGER NOT NULL DEFAULT 1,
  process_deletions INTEGER NOT NULL DEFAULT 0,
  max_drain_tasks INTEGER NOT NULL DEFAULT 8,
  retention_days INTEGER NOT NULL DEFAULT 7,
  state TEXT NOT NULL DEFAULT 'Stopped',
  last_crawl_start_utc TEXT,
  last_crawl_finish_utc TEXT,
  last_crawl_success INTEGER,
  created_utc TEXT NOT NULL,
  last_update_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS crawl_operations (
  id TEXT PRIMARY KEY,
  tenant_id TEXT NOT NULL DEFAULT 'default',
  crawl_plan_id TEXT NOT NULL,
  state TEXT NOT NULL DEFAULT 'NotStarted',
  status_message TEXT,
  objects_enumerated INTEGER NOT NULL DEFAULT 0,
  bytes_enumerated INTEGER NOT NULL DEFAULT 0,
  objects_added INTEGER NOT NULL DEFAULT 0,
  bytes_added INTEGER NOT NULL DEFAULT 0,
  objects_updated INTEGER NOT NULL DEFAULT 0,
  bytes_updated INTEGER NOT NULL DEFAULT 0,
  objects_deleted INTEGER NOT NULL DEFAULT 0,
  bytes_deleted INTEGER NOT NULL DEFAULT 0,
  objects_success INTEGER NOT NULL DEFAULT 0,
  bytes_success INTEGER NOT NULL DEFAULT 0,
  objects_failed INTEGER NOT NULL DEFAULT 0,
  bytes_failed INTEGER NOT NULL DEFAULT 0,
  enumeration_file TEXT,
  start_utc TEXT,
  start_enumeration_utc TEXT,
  finish_enumeration_utc TEXT,
  start_retrieval_utc TEXT,
  finish_retrieval_utc TEXT,
  finish_utc TEXT,
  created_utc TEXT NOT NULL,
  last_update_utc TEXT NOT NULL
);
```

- [x]Add indices to `CreateIndices()`:

```sql
CREATE INDEX IF NOT EXISTS idx_crawl_plans_tenant_id ON crawl_plans(tenant_id);
CREATE INDEX IF NOT EXISTS idx_crawl_plans_state ON crawl_plans(state);
CREATE INDEX IF NOT EXISTS idx_crawl_operations_tenant_id ON crawl_operations(tenant_id);
CREATE INDEX IF NOT EXISTS idx_crawl_operations_crawl_plan_id ON crawl_operations(crawl_plan_id);
CREATE INDEX IF NOT EXISTS idx_crawl_operations_created_utc ON crawl_operations(created_utc);
```

### 4.5 SQLite Implementations

- [x]Create `src/AssistantHub.Core/Database/Sqlite/Implementations/CrawlPlanMethods.cs`

Follow the pattern from `AssistantDocumentMethods.cs`. Sub-objects (ingestion settings, repository settings, schedule, filter) are serialized to/from JSON using `Serializer.SerializeJson()` / `Serializer.DeserializeJson<T>()` and stored in `*_json` TEXT columns.

- [x]Create `src/AssistantHub.Core/Database/Sqlite/Implementations/CrawlOperationMethods.cs`

Follow the same pattern. Include `EnumerateByCrawlPlanAsync` with WHERE clause on `crawl_plan_id`. Include `DeleteExpiredAsync` which deletes operations where `created_utc < (now - retentionDays)`.

### 4.6 Wire up in SqliteDatabaseDriver

- [x]In `SqliteDatabaseDriver.cs` constructor, add:

```csharp
CrawlPlan = new CrawlPlanMethods(this, _Settings, _Logging);
CrawlOperation = new CrawlOperationMethods(this, _Settings, _Logging);
```

### 4.7 Other Database Drivers (Postgresql, MySQL, SqlServer)

- [x]Add equivalent table creation and implementations for PostgreSQL
- [x]Add equivalent table creation and implementations for MySQL
- [x]Add equivalent table creation and implementations for SQL Server

Follow same patterns as SQLite but with appropriate SQL dialect differences (e.g., BOOLEAN for PostgreSQL, TINYINT(1) for MySQL, BIT for SQL Server).

---

## 5. Crawler Engine

### 5.1 NuGet Dependency

- [x]Add `CrawlSharp` NuGet package reference to `AssistantHub.Core.csproj`

```xml
<PackageReference Include="CrawlSharp" Version="1.0.16" />
```

Or whatever the latest version is.

### 5.2 CrawlerBase

- [x]Create `src/AssistantHub.Core/Services/Crawlers/CrawlerBase.cs`

Abstract base class for all crawler types.

```csharp
public abstract class CrawlerBase : IDisposable
```

**Constructor parameters:**
- `LoggingModule logging`
- `DatabaseDriverBase database`
- `CrawlPlan crawlPlan`
- `CrawlOperation crawlOperation`
- `IngestionService ingestion` (nullable)
- `StorageService storage` (nullable)
- `CancellationToken token`

**Abstract methods:**
```csharp
public abstract IAsyncEnumerable<CrawledObject> EnumerateAsync(CancellationToken token = default);
public abstract Task<bool> ValidateConnectivityAsync(CancellationToken token = default);
public abstract Task<List<CrawledObject>> EnumerateContentsAsync(int maxKeys = 100, int skip = 0, CancellationToken token = default);
```

**CrawledObject** - internal data class:
```csharp
public class CrawledObject
{
    public string Key { get; set; }          // URL or path
    public string ContentType { get; set; }
    public long ContentLength { get; set; }
    public byte[] Data { get; set; }
    public string MD5Hash { get; set; }
    public string SHA1Hash { get; set; }
    public string SHA256Hash { get; set; }
    public string ETag { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
    public string DocumentId { get; set; }   // populated after ingestion, for enumeration file
    public bool IsFolder { get; set; }
}
```

#### 5.2.1 Skip File Lists

The base class includes hardcoded skip lists for system/temporary files. These are applied during enumeration before any object is added to the processing queue. Web crawlers can override `IsSkipFile()` to no-op since these are primarily relevant to file-based crawlers (File, CIFS, NFS, S3).

```csharp
internal static List<string> SkipFilenames = new List<string>
{
    ".", "..", ".ds_store", "desktop.ini", "thumbs.db",
    ".directory", ".gitignore", ".gitattributes", ".localized"
};

internal static List<string> SkipPrefixes = new List<string> { "~", ".dropbox" };
internal static List<string> SkipSuffixes = new List<string> { ".swp", ".tmp", ".bak" };

protected virtual bool IsSkipFile(string filename)
```

Checks: filename matches any entry in `SkipFilenames` (case-insensitive), or starts with any `SkipPrefixes`, or ends with any `SkipSuffixes`.

#### 5.2.2 Filter Application

`MatchesFilter(CrawledObject obj, CrawlFilterSettings filter)` is applied **before queueing** objects for processing (not after). This prevents wasted retrieval/ingestion effort on objects that will be discarded.

Check order:
1. `ContentLength` must be > 0
2. If `filter.ObjectPrefix` is set, `obj.Key` must start with it (case-insensitive)
3. If `filter.ObjectSuffix` is set, `obj.Key` must end with it (case-insensitive)
4. If `filter.AllowedContentTypes` is set and non-empty, `obj.ContentType` must match one entry (case-insensitive)
5. If `filter.MinimumSize` > 0, `obj.ContentLength` must be ≥ `filter.MinimumSize`
6. If `filter.MaximumSize` is not null, `obj.ContentLength` must be ≤ `filter.MaximumSize`

#### 5.2.3 Delta Detection Algorithm

`BuildEnumerationDelta(List<CrawledObject> current, List<CrawledObject> previous)` compares two enumerations to produce Added, Changed, Deleted, and Unchanged lists.

**Step 1: Classify by key**
- Build a dictionary from previous enumeration keyed by `Key`
- For each item in current: if key exists in previous → candidate for Changed/Unchanged; if not → Added
- Any key in previous not in current → Deleted

**Step 2: Detect changes (for items present in both)**

Compare in precedence order — first mismatch means Changed:
1. `ContentLength` differs → **Changed**
2. Both have `ETag` and values differ → **Changed**
3. Both have `SHA256Hash` and values differ → **Changed**
4. Both have `SHA1Hash` and values differ → **Changed**
5. Both have `MD5Hash` and values differ → **Changed**
6. Both have `LastModifiedUtc` and values differ (compare formatted as `"yyyy-MM-ddTHH:mm:ss.ffffff"` for microsecond precision) → **Changed**
7. Otherwise → **Unchanged**

**Step 3: Re-process previously failed items**

Items that were in `previous.Failed` and are NOT already in the current Added or Changed lists are re-added to the Added list. This ensures failed items get another ingestion attempt on the next crawl.

#### 5.2.4 Task Parallelism and Drain Strategy

The base class uses a `SemaphoreSlim` initialized to `CrawlPlan.MaxDrainTasks` to control concurrent processing tasks. Each object ingestion runs as a `Task.Run()` that acquires the semaphore before starting work and releases it on completion.

Processing is phased with drain between each phase:
1. Queue all **addition** tasks → wait for all to complete
2. Queue all **update** tasks → wait for all to complete
3. Queue all **deletion** tasks → wait for all to complete

Drain is implemented by waiting on all outstanding tasks with a polling interval of 5000ms. If tasks are still running after the phase, the drain loop logs progress (running count, queued count) every iteration.

#### 5.2.5 Thread Safety

Mutations to shared state during parallel processing are protected by a lock:

```csharp
private readonly object _EnumerationLock = new object();
```

The following operations are performed under `_EnumerationLock`:
- Adding items to Success/Failed lists in the enumeration
- Incrementing `ObjectsSuccess`, `BytesSuccess`, `ObjectsFailed`, `BytesFailed` counters on the `CrawlOperation`

The lock is held **briefly** — only for list/counter mutation, never during I/O or network calls.

Database writes (`CrawlOperation.UpdateAsync()`) to persist operation statistics should be batched — update the DB record after each phase completes rather than per-object to avoid write contention on SQLite's single-writer constraint.

#### 5.2.6 Concrete `Start()` Method Lifecycle

```
try
{
    1.  Set CrawlPlan.State = Running, CrawlOperation.State = Starting
        Set CrawlOperation.StartUtc = UtcNow
        Persist both to database

    --- ENUMERATION PHASE ---
    2.  Set CrawlOperation.State = Enumerating
        Set CrawlOperation.StartEnumerationUtc = UtcNow
    3.  Call EnumerateAsync(), collect all objects into currentEnumeration
        Apply IsSkipFile() during collection — skip matching objects
    4.  Set CrawlOperation.FinishEnumerationUtc = UtcNow
        Record ObjectsEnumerated, BytesEnumerated
    5.  Load previous enumeration file (if exists); empty enumeration on first run
    6.  Call BuildEnumerationDelta() → produces Added, Changed, Deleted, Unchanged lists

    --- RETRIEVAL / PROCESSING PHASE ---
    7.  Set CrawlOperation.State = Retrieving
        Set CrawlOperation.StartRetrievalUtc = UtcNow

    8.  If ProcessAdditions enabled:
        For each object in Added that passes MatchesFilter():
          Queue task (bounded by SemaphoreSlim):
            a. Upload file bytes to S3 via StorageService
            b. Create AssistantDocument record with:
               - IngestionRuleId from CrawlIngestionSettings
               - CrawlPlanId, CrawlOperationId, SourceUrl
               - Name, OriginalFilename, ContentType, SizeBytes
               - BucketName and CollectionId from the ingestion rule
            c. Fire-and-forget IngestionService.ProcessDocumentAsync()
            d. Lock: add to Success list, increment counters
            e. On exception: lock: add to Failed list, increment failure counters
        DRAIN — wait for all addition tasks to complete

    9.  If ProcessUpdates enabled:
        For each object in Changed that passes MatchesFilter():
          Queue task:
            a. Find existing AssistantDocument by SourceUrl + CrawlPlanId
            b. Delete existing document via cleanup pipeline (see §5.2.7)
            c. Upload new bytes, create new AssistantDocument, trigger ingestion
            d. Lock: update counters
        DRAIN — wait for all update tasks to complete

    10. If ProcessDeletions enabled:
        For each object in Deleted:
          Queue task:
            a. Find existing AssistantDocument by SourceUrl + CrawlPlanId
            b. Delete via cleanup pipeline (see §5.2.7)
            c. Lock: increment deletion counters
        DRAIN — wait for all deletion tasks to complete

    11. Set CrawlOperation.FinishRetrievalUtc = UtcNow

    --- FINALIZATION ---
    12. Mark operation Success (if no failures) or Failed (if any failures)
    13. Set CrawlOperation.FinishUtc = UtcNow
    14. Update CrawlPlan: LastCrawlStartUtc, LastCrawlFinishUtc, LastCrawlSuccess
}
catch (OperationCanceledException)
{
    Mark CrawlOperation.State = Canceled
    Log cancellation
}
catch (Exception ex)
{
    Mark CrawlOperation.State = Failed
    Set CrawlOperation.StatusMessage = ex.Message
    Log exception
}
finally
{
    // Wait for any remaining in-flight tasks (up to 60 seconds)
    // Save enumeration file to disk (strip Data field to save space)
    // Reset CrawlPlan.State = Stopped
    // Persist final state to database
    // Set currentEnumeration = null to free memory
}
```

**Key design point:** Crawled documents are first-class `AssistantDocument` records. They go through the same ingestion pipeline, appear in the Documents table, have processing logs, and can be queried/filtered. The `CrawlPlanId` and `CrawlOperationId` fields provide traceability back to the crawler and specific crawl run.

#### 5.2.7 Document Cleanup Pipeline

When a crawled document needs to be removed (due to update or deletion), the cleanup pipeline performs cascading deletion:

1. **Delete RecallDB embeddings** — Read the document's `ChunkRecordIds` (JSON array of record IDs). For each record ID, call RecallDB to delete the vector record from the collection specified by `CollectionId`.
2. **Delete S3 object** — Call `StorageService.DeleteObjectAsync()` using the document's `BucketName` and `S3Key`.
3. **Delete processing log** — Call `ProcessingLogService` to remove the log file for the document.
4. **Delete database record** — Call `Database.AssistantDocument.DeleteAsync()` to remove the document record.

This same cleanup pipeline should also be used when a document is deleted manually via the API or dashboard (currently not implemented — this is a prerequisite enhancement). Implement as a method on `IngestionService` or as a standalone `DocumentCleanupService`:

```csharp
public async Task CleanupDocumentAsync(string documentId, CancellationToken token = default)
```

- [x]Create document cleanup method (used by both manual deletion and crawler updates/deletions)

#### 5.2.8 Enumeration File Format

Enumeration files are stored at `{EnumerationDirectory}/{crawlPlanId}/{operationId}.json`.

The file contains a serialized `CrawlEnumeration` object:

```json
{
  "AllFiles": [
    { "Key": "https://example.com/page1", "ContentType": "text/html", "ContentLength": 4096, "MD5Hash": "...", "SHA256Hash": "...", "LastModifiedUtc": "2026-02-25T12:00:00.000000", "DocumentId": "adoc_..." },
    ...
  ],
  "Added": [ ... ],
  "Changed": [ ... ],
  "Deleted": [ ... ],
  "Unchanged": [ ... ],
  "Success": [ ... ],
  "Failed": [ ... ],
  "Statistics": {
    "TotalCount": 150, "TotalBytes": 5242880,
    "AddedCount": 10, "AddedBytes": 102400,
    "ChangedCount": 5, "ChangedBytes": 51200,
    "DeletedCount": 2, "DeletedBytes": 20480,
    "SuccessCount": 13, "SuccessBytes": 133120,
    "FailedCount": 2, "FailedBytes": 20480
  }
}
```

**The `Data` (byte[]) field is set to null before serialization** to avoid storing full document content on disk. Only metadata is preserved. This is done via a copy/strip step before writing (following View's `Copy(false)` pattern).

**Helper methods:**
- `SaveEnumerationFile(string directory, string crawlPlanId, string operationId, CrawlEnumeration enumeration)` — creates directory if needed, strips Data, serializes to JSON
- `LoadEnumerationFile(string directory, string crawlPlanId)` — loads the most recent enumeration file for a plan (by operationId sort order)
- `DeleteEnumerationFile(string path)` — removes a specific enumeration file (used by retention cleanup)

### 5.3 WebCrawler Implementation

- [x]Create `src/AssistantHub.Core/Services/Crawlers/WebRepositoryCrawler.cs`

```csharp
public class WebRepositoryCrawler : CrawlerBase
```

**Implementation details:**
- Construct `CrawlSharp.Web.Settings` from `WebCrawlRepositorySettings`:
  - Map all boolean flags, auth settings, user agent, start URL, depth, parallelism
  - Set `crawler.Delay` to `CrawlDelayMs` after construction (CrawlSharp exposes this as a property on the WebCrawler instance, not on Settings)
- Instantiate `CrawlSharp.Web.WebCrawler`
- Wire `crawler.Logger` to logging
- Wire `crawler.Exception` to logging (error level)
- Override `IsSkipFile()` to return `false` (skip lists are irrelevant for web URLs)

**EnumerateAsync():**
- Iterate `crawler.CrawlAsync()`, yield `CrawledObject` for each `WebResource`
- **Only yield objects with HTTP status 200–399** (success + redirects)
- Skip 4xx and 5xx responses (log as warnings)
- **Data is already fetched** — `WebResource.Data` contains the response body from the crawl; there is no separate retrieval step for web crawlers. The `Data` field on the yielded `CrawledObject` is populated directly from `WebResource.Data`.
- Map: `WebResource.Url` → `Key`, `WebResource.ContentType` → `ContentType`, `WebResource.ContentLength` → `ContentLength`, `WebResource.MD5Hash` → `MD5Hash`, `WebResource.SHA1Hash` → `SHA1Hash`, `WebResource.SHA256Hash` → `SHA256Hash`, `WebResource.ETag` → `ETag`, `WebResource.LastModified` → `LastModifiedUtc`

**ValidateConnectivityAsync():**
- Create a `WebCrawler` with the same settings but `FollowLinks = false`, `MaxCrawlDepth = 0`
- Attempt to fetch only the start URL
- Return true if HTTP status is 200–399, false otherwise

**EnumerateContentsAsync():**
- Similar to `EnumerateAsync()` but with skip/take pagination applied to results
- Dispose of `WebCrawler` when done

### 5.4 CrawlerFactory

- [x]Create `src/AssistantHub.Core/Services/Crawlers/CrawlerFactory.cs`

Factory method to instantiate the correct crawler based on `RepositoryTypeEnum`:

```csharp
public static CrawlerBase Create(RepositoryTypeEnum type, ...) => type switch
{
    RepositoryTypeEnum.Web => new WebRepositoryCrawler(...),
    _ => throw new NotSupportedException($"Repository type {type} is not supported.")
};
```

---

## 6. Background Services

### 6.1 CrawlSchedulerService

- [x]Create `src/AssistantHub.Server/Services/CrawlSchedulerService.cs`

Background service that manages crawl scheduling.

**Behavior:**
- On startup: query all crawl plans
- For any plan that was `Running` when the server stopped, reset state to `Stopped` and mark the last operation as `Failed` (unclean shutdown recovery)
- Every 60 seconds: check all crawl plans to see if any are due to run
  - A plan is due if: `State == Stopped` AND (`LastCrawlStartUtc` is null OR `LastCrawlStartUtc + interval < DateTime.UtcNow`)
  - Skip `OneTime` plans that have already run (`LastCrawlFinishUtc` is not null)
- When a plan is due:
  - Create a `CrawlOperation` record
  - Instantiate the crawler via `CrawlerFactory`
  - Run `crawler.Start()` on a background thread
  - Track running crawlers in a `ConcurrentDictionary<string, (CrawlerBase, CancellationTokenSource)>`
- Expose methods for manual start/stop:
  - `StartCrawlAsync(string crawlPlanId)` - manually trigger a crawl
  - `StopCrawlAsync(string crawlPlanId)` - cancel running crawl via CancellationToken

### 6.2 CrawlOperationCleanupService

- [x]Create `src/AssistantHub.Server/Services/CrawlOperationCleanupService.cs`

Background service for retention cleanup.

**Behavior:**
- On startup: run immediately
- Then every 1 hour
- For each crawl plan: delete operations older than `RetentionDays`
  - Delete database records via `CrawlOperation.DeleteExpiredAsync()`
  - Delete associated enumeration files from disk

### 6.3 Wire up in AssistantHubServer.cs

- [x]Add `_CrawlSchedulerService` and `_CrawlOperationCleanupService` private members
- [x]Initialize in `InitializeServices()` or a new `StartCrawlServicesAsync()` method
- [x]Start both services during server startup (after `StartChatHistoryCleanup()`)
- [x]Cancel on shutdown via `_TokenSource`

---

## 7. API Handlers

### 7.1 CrawlPlanHandler

- [x]Create `src/AssistantHub.Server/Handlers/CrawlPlanHandler.cs`

Extends `HandlerBase`. Needs additional constructor parameter for `CrawlSchedulerService`.

**Endpoints:**

| Method | Route | Description |
|--------|-------|-------------|
| PUT | `/v1.0/crawlplans` | Create crawl plan |
| GET | `/v1.0/crawlplans` | List crawl plans (paginated enumeration) |
| GET | `/v1.0/crawlplans/{id}` | Get crawl plan by ID |
| PUT | `/v1.0/crawlplans/{id}` | Update crawl plan |
| DELETE | `/v1.0/crawlplans/{id}` | Delete crawl plan (and all operations) |
| POST | `/v1.0/crawlplans/{id}/start` | Start a crawl |
| POST | `/v1.0/crawlplans/{id}/stop` | Stop a crawl |
| POST | `/v1.0/crawlplans/{id}/connectivity` | Test repository connectivity |
| GET | `/v1.0/crawlplans/{id}/enumerate` | Enumerate repository contents |
| HEAD | `/v1.0/crawlplans/{id}` | Check existence |

**Authorization:** Require auth. Enforce tenant ownership on all operations. Start/stop/create/edit/delete require admin.

### 7.2 CrawlOperationHandler

- [x]Create `src/AssistantHub.Server/Handlers/CrawlOperationHandler.cs`

**Endpoints:**

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/v1.0/crawlplans/{planId}/operations` | List operations for a plan (paginated enumeration) |
| GET | `/v1.0/crawlplans/{planId}/operations/statistics` | Get aggregate stats for a plan |
| GET | `/v1.0/crawlplans/{planId}/operations/{id}` | Get operation by ID |
| GET | `/v1.0/crawlplans/{planId}/operations/{id}/statistics` | Get stats for a given operation by ID |
| DELETE | `/v1.0/crawlplans/{planId}/operations/{id}` | Delete operation |
| GET | `/v1.0/crawlplans/{planId}/operations/{id}/enumeration` | Get enumeration file contents |

**Statistics endpoint response:**
```json
{
  "LastRun": "2026-02-25T12:00:00Z",
  "NextRun": "2026-02-26T12:00:00Z",
  "FailedRunCount": 2,
  "SuccessfulRunCount": 15,
  "MinRuntimeMs": 12000,
  "MaxRuntimeMs": 300000,
  "AvgRuntimeMs": 45000,
  "MinObjectCount": 10,
  "MaxObjectCount": 500,
  "AvgObjectCount": 150,
  "MinBytesCrawled": 1024,
  "MaxBytesCrawled": 5242880,
  "AvgBytesCrawled": 524288
}
```

### 7.3 Route Registration

- [x]In `AssistantHubServer.cs` `InitializeWebserver()`, register all new routes

Follow the existing pattern (e.g., how `DocumentHandler` routes are registered). Register parameterized routes for crawl plans and operations.

---

## 8. Dashboard

### 8.1 API Client Extensions

- [x]Add crawler methods to `dashboard/src/utils/api.js`:

```javascript
// Crawl Plans
getCrawlPlans(params)               // GET /v1.0/crawlplans
getCrawlPlan(id)                     // GET /v1.0/crawlplans/{id}
createCrawlPlan(plan)                // PUT /v1.0/crawlplans
updateCrawlPlan(id, plan)            // PUT /v1.0/crawlplans/{id}
deleteCrawlPlan(id)                  // DELETE /v1.0/crawlplans/{id}
startCrawl(id)                       // POST /v1.0/crawlplans/{id}/start
stopCrawl(id)                        // POST /v1.0/crawlplans/{id}/stop
testCrawlConnectivity(id)            // POST /v1.0/crawlplans/{id}/connectivity
enumerateCrawlContents(id, params)   // GET /v1.0/crawlplans/{id}/enumerate

// Crawl Operations
getCrawlOperations(planId, params)           // GET /v1.0/crawlplans/{planId}/operations
getCrawlOperationStatistics(planId)          // GET /v1.0/crawlplans/{planId}/operations/statistics
getCrawlOperation(planId, id)                // GET /v1.0/crawlplans/{planId}/operations/{id}
deleteCrawlOperation(planId, id)             // DELETE /v1.0/crawlplans/{planId}/operations/{id}
getCrawlOperationEnumeration(planId, id)     // GET /v1.0/crawlplans/{planId}/operations/{id}/enumeration
```

### 8.2 Sidebar Update

- [x]In `dashboard/src/components/Sidebar.jsx`, add Crawlers item under the Ingestion section:

```javascript
{
  label: 'Ingestion',
  adminOnly: false,
  items: [
    { path: '/documents', label: 'Documents', icon: ... },
    { path: '/crawlers', label: 'Crawlers', sub: true, icon: <svg>...</svg> },
  ],
},
```

Use a spider/globe icon appropriate for crawling.

### 8.3 Routing

- [x]In `dashboard/src/components/Dashboard.jsx`, add route:

```javascript
<Route path="/crawlers" element={<CrawlersView />} />
```

### 8.4 CrawlersView

- [x]Create `dashboard/src/views/CrawlersView.jsx`

Main crawler management page.

**Layout:** Uses `DataTable` component (same pattern as `DocumentsView`).

**Table columns:**
| Column | Source |
|--------|--------|
| ID | `Id` (with CopyableId) |
| Name | `Name` |
| Type | `RepositoryType` |
| URL | `RepositorySettings.StartUrl` (for web) |
| State | `State` (badge: green=Running, gray=Stopped) |
| Interval | `Schedule.IntervalValue` + `Schedule.IntervalType` (e.g., "24 Hours") |
| Last Crawl | `LastCrawlStartUtc` (formatted relative timestamp) |
| Result | `LastCrawlSuccess` (badge: green=Success, red=Failed, gray=N/A) |
| Actions | Context menu |

**Actions context menu (per row):**
- Edit — open `CrawlPlanFormModal`
- Start — call `api.startCrawl(id)`, refresh (show only if state === 'Stopped')
- Stop — call `api.stopCrawl(id)`, refresh (show only if state === 'Running')
- View Operations — open `CrawlOperationsModal`
- View JSON — open `JsonViewModal`
- Verify Connectivity — call `api.testCrawlConnectivity(id)`, show success/fail alert
- Enumerate Contents — call `api.enumerateCrawlContents(id)`, show results in modal
- Delete — confirm then call `api.deleteCrawlPlan(id)`, refresh

**Header:** "Crawlers" title with "Create Crawler" button.

### 8.5 CrawlPlanFormModal (Create/Edit)

- [x]Create `dashboard/src/components/modals/CrawlPlanFormModal.jsx`

Large modal for creating/editing a crawl plan.

**Sections (collapsible):**

1. **General**
   - Name (text input)
   - Repository Type (dropdown, currently only "Web")

2. **Ingestion**
   - Ingestion Rule (dropdown, populated from `api.getIngestionRules()`)
   - Store in S3 (checkbox)
   - S3 Bucket Name (text input, shown if Store in S3 checked)

3. **Repository Settings** (shown based on selected repository type)
   - For Web:
     - Start URL (text input, required)
     - Authentication Type (dropdown: None, Basic, API Key, Bearer Token)
     - Auth fields (shown conditionally based on auth type):
       - Basic: Username, Password
       - API Key: Header Name, Key Value
       - Bearer Token: Token
     - User Agent (text input, default "assistanthub-crawler")
     - Use Headless Browser (checkbox, default unchecked)
     - Follow Links (checkbox, default checked)
     - Follow Redirects (checkbox, default checked)
     - Extract Sitemap Links (checkbox, default checked)
     - Restrict to Child URLs (checkbox, default checked)
     - Restrict to Subdomain (checkbox)
     - Restrict to Root Domain (checkbox, default checked)
     - Ignore robots.txt (checkbox)
     - Max Depth (number input, default 5)
     - Max Parallel Tasks (number input, default 8)
     - Crawl Delay (ms) (number input, default 100)

4. **Schedule**
   - Interval Type (dropdown: OneTime, Minutes, Hours, Days, Weeks)
   - Interval Value (number input, default 24)

5. **Filter**
   - Object Prefix (text input)
   - Object Suffix (text input)
   - Allowed Content Types (tag input / comma-separated text)
   - Minimum Size (bytes, number input)
   - Maximum Size (bytes, number input)

6. **Processing**
   - Process Additions (checkbox, default checked)
   - Process Updates (checkbox, default checked)
   - Process Deletions (checkbox, default unchecked)
   - Max Concurrent Tasks (number input, default 8, min 1, max 64)

7. **Retention**
   - Retention Days (number input, default 7, min 0, max 14)

**Buttons:** Save / Cancel

### 8.6 CrawlOperationsModal

- [x]Create `dashboard/src/components/modals/CrawlOperationsModal.jsx`

Large modal (nearly full-screen, similar to existing large modals) for viewing crawl operations.

**Top section — Statistics panel:**

Uses the `/statistics` endpoint to show:
- Last Run / Next Run (computed from last run + interval)
- Failed Runs / Successful Runs counts
- Min / Max / Avg Runtime
- Min / Max / Avg Object Count
- Min / Max / Avg Bytes Crawled

Display as a grid of stat cards.

**Table section:**

| Column | Source |
|--------|--------|
| ID | `Id` (CopyableId) |
| Start | `StartUtc` (formatted) |
| End | `FinishUtc` (formatted) |
| Status | `State` (badge) |
| Objects | `ObjectsEnumerated` |
| Bytes | `BytesEnumerated` (formatted) |
| Actions | Context menu |

**Actions per operation:**
- View Enumeration — open `CrawlEnumerationModal`
- Delete — confirm then call `api.deleteCrawlOperation(planId, id)`, refresh

### 8.7 CrawlEnumerationModal

- [x]Create `dashboard/src/components/modals/CrawlEnumerationModal.jsx`

Modal that displays the enumeration file contents from a crawl operation.

Fetches data from `api.getCrawlOperationEnumeration(planId, opId)`.

**Layout — sections with stats and file lists:**

- **All Files** — total count, total bytes, expandable file list
- **New Files** (added) — count, bytes, file list
- **Changed Files** (updated) — count, bytes, file list
- **Deleted Files** — count, bytes, file list
- **Successfully Crawled** — count, bytes, file list
- **Failed** — count, bytes, file list

Each file list entry shows: key/URL, content type, size, last modified.

### 8.8 DocumentsView Updates (Traceability)

- [x]Update `dashboard/src/views/DocumentsView.jsx`:
  - Add optional filter dropdown for "Crawler" that filters documents by `crawl_plan_id`
  - When a document has `CrawlPlanId` set, show a "Crawled" badge in the table
  - In the row context menu, add "View Crawl Operation" option (opens the operation that produced this document) — only shown when `CrawlOperationId` is set
  - The existing "View Processing Logs" action already works for crawled documents since they go through the standard ingestion pipeline

This enables the user to answer:
1. **"Was this file ingested successfully?"** — Look at the document's `Status` column (Completed = success, Failed = failure). Click "View Processing Logs" for the detailed processing log.
2. **"When was this file crawled?"** — The `CrawlOperationId` links to the operation, which has `StartUtc`. Shown in the document detail/JSON view. The "View Crawl Operation" action navigates directly to the operation.

### 8.9 CrawlOperationsModal Enhancements (Traceability)

- [x]In the operations modal, add a "View Documents" action per operation:
  - Opens a filtered view of the Documents table showing only documents with `crawl_operation_id` matching the selected operation
  - This shows all files crawled in that run, their ingestion status, and provides access to processing logs

### 8.10 CSS Styles

- [x]Add styles to `dashboard/src/App.css` for:
  - Crawler-specific badges (Running/Stopped, Success/Failed)
  - Statistics grid/cards in the operations modal
  - Collapsible sections in the form modal
  - Full-screen modal variant for operations view
  - Enumeration details styling

Follow existing patterns in App.css for status badges, modals, and data tables.

---

## 9. Docker & Infrastructure

### 9.1 Compose Volume for Enumerations

- [x]Add enumeration volume mount to `docker/compose.yaml` under `assistanthub-server`:

```yaml
volumes:
  - ./assistanthub/assistanthub.json:/app/assistanthub.json
  - ./assistanthub/data/:/app/data/
  - ./assistanthub/logs/:/app/logs/
  - ./assistanthub/processing-logs/:/app/processing-logs/
  - ./assistanthub/crawl-enumerations/:/app/crawl-enumerations/   # NEW
```

### 9.2 Settings Update

- [x]Add crawl settings to `AssistantHubSettings.cs`:

```csharp
/// <summary>
/// Crawl settings.
/// </summary>
public CrawlSettings Crawl { get; set; } = new CrawlSettings();
```

- [x]Create `src/AssistantHub.Core/Settings/CrawlSettings.cs`:

```csharp
public class CrawlSettings
{
    /// <summary>
    /// Directory for storing crawl enumeration files.
    /// Default: ./crawl-enumerations/
    /// </summary>
    public string EnumerationDirectory { get; set; } = "./crawl-enumerations/";
}
```

### 9.3 Factory Reset

- [x]Update `docker/factory/reset.sh` — add cleanup of crawl enumerations:

```bash
rm -rf "$DOCKER_DIR/assistanthub/crawl-enumerations/"*
echo "        Cleared crawl enumerations"
```

- [x]Update `docker/factory/reset.bat` — equivalent cleanup

---

## 10. Migration Script

- [x]Create `migrations/003_add_crawlers.sql`

Provide CREATE TABLE statements for all 4 database types (SQLite active, others commented out) following the pattern established in `002_upgrade_to_v0.4.0.sql`.

```sql
-- Migration 003: Add crawler tables and document traceability columns.
-- Adds crawl_plans and crawl_operations tables.
-- Adds crawl_plan_id, crawl_operation_id, source_url to assistant_documents.
-- Back up your database before running this migration.

--------------------------------------------------------------------
-- SQLite
--------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS crawl_plans ( ... );
CREATE TABLE IF NOT EXISTS crawl_operations ( ... );

ALTER TABLE assistant_documents ADD COLUMN crawl_plan_id TEXT;
ALTER TABLE assistant_documents ADD COLUMN crawl_operation_id TEXT;
ALTER TABLE assistant_documents ADD COLUMN source_url TEXT;

CREATE INDEX IF NOT EXISTS idx_crawl_plans_tenant_id ON crawl_plans(tenant_id);
CREATE INDEX IF NOT EXISTS idx_crawl_operations_crawl_plan_id ON crawl_operations(crawl_plan_id);
CREATE INDEX IF NOT EXISTS idx_assistant_documents_crawl_plan_id ON assistant_documents(crawl_plan_id);
CREATE INDEX IF NOT EXISTS idx_assistant_documents_crawl_operation_id ON assistant_documents(crawl_operation_id);
-- ... remaining indices

--------------------------------------------------------------------
-- PostgreSQL (commented out)
--------------------------------------------------------------------

--------------------------------------------------------------------
-- MySQL (commented out)
--------------------------------------------------------------------

--------------------------------------------------------------------
-- SQL Server (commented out)
--------------------------------------------------------------------
```

---

## Implementation Order

Recommended order of implementation:

1. **Enums** (§2) — no dependencies
2. **Constants & ID Generation** (§3) — no dependencies
3. **Data Models** (§1) — depends on enums
4. **Database Layer** (§4) — depends on models
5. **Crawler Engine** (§5) — depends on models + database
6. **Background Services** (§6) — depends on crawler engine
7. **API Handlers** (§7) — depends on database + services
8. **Dashboard** (§8) — depends on API
9. **Docker & Infrastructure** (§9) — can be done in parallel with any step
10. **Migration Script** (§10) — matches database layer

---

## File Summary

### New Files (Backend — ~20 files)

| File | Purpose |
|------|---------|
| `Enums/RepositoryTypeEnum.cs` | Repository types |
| `Enums/ScheduleIntervalEnum.cs` | Schedule intervals |
| `Enums/CrawlPlanStateEnum.cs` | Plan states |
| `Enums/CrawlOperationStateEnum.cs` | Operation states |
| `Enums/WebAuthTypeEnum.cs` | Web auth types |
| `Models/CrawlPlan.cs` | Crawl plan model |
| `Models/CrawlOperation.cs` | Crawl operation model |
| `Models/CrawlRepositorySettings.cs` | Base + WebCrawlRepositorySettings |
| `Models/CrawlIngestionSettings.cs` | Ingestion sub-object |
| `Models/CrawlScheduleSettings.cs` | Schedule sub-object |
| `Models/CrawlFilterSettings.cs` | Filter sub-object |
| `Database/Interfaces/ICrawlPlanMethods.cs` | Plan DB interface |
| `Database/Interfaces/ICrawlOperationMethods.cs` | Operation DB interface |
| `Database/Sqlite/Implementations/CrawlPlanMethods.cs` | SQLite plan impl |
| `Database/Sqlite/Implementations/CrawlOperationMethods.cs` | SQLite operation impl |
| `Services/Crawlers/CrawlerBase.cs` | Base crawler class |
| `Services/Crawlers/CrawledObject.cs` | Crawled item data class |
| `Services/Crawlers/CrawlEnumeration.cs` | Enumeration delta model |
| `Services/Crawlers/WebRepositoryCrawler.cs` | Web crawler impl |
| `Services/Crawlers/CrawlerFactory.cs` | Crawler factory |
| `Settings/CrawlSettings.cs` | Crawl configuration |

### New Files (Server — ~4 files)

| File | Purpose |
|------|---------|
| `Handlers/CrawlPlanHandler.cs` | Crawl plan API |
| `Handlers/CrawlOperationHandler.cs` | Crawl operation API |
| `Services/CrawlSchedulerService.cs` | Background scheduler |
| `Services/CrawlOperationCleanupService.cs` | Retention cleanup |

### New Files (Dashboard — ~4 files)

| File | Purpose |
|------|---------|
| `views/CrawlersView.jsx` | Main crawlers page |
| `components/modals/CrawlPlanFormModal.jsx` | Create/edit modal |
| `components/modals/CrawlOperationsModal.jsx` | Operations viewer |
| `components/modals/CrawlEnumerationModal.jsx` | Enumeration details |

### Modified Files

| File | Change |
|------|--------|
| `Constants.cs` | Add crawler prefixes and table names |
| `IdGenerator.cs` | Add crawl plan/operation ID generators |
| `AssistantDocument.cs` | Add CrawlPlanId, CrawlOperationId, SourceUrl properties |
| `AssistantDocumentMethods.cs` | Include new columns in all CRUD queries (all DB drivers) |
| `DatabaseDriverBase.cs` | Add CrawlPlan/CrawlOperation properties |
| `SqliteDatabaseDriver.cs` | Wire up new implementations |
| `TableQueries.cs` | Add CREATE TABLE/INDEX statements, ALTER assistant_documents |
| `IngestionService.cs` | Add `CleanupDocumentAsync()` for cascading document deletion |
| `DocumentHandler.cs` | Wire DELETE endpoint to use `CleanupDocumentAsync()` |
| `AssistantHubSettings.cs` | Add CrawlSettings |
| `AssistantHubServer.cs` | Initialize services, register routes |
| `Sidebar.jsx` | Add Crawlers menu item |
| `Dashboard.jsx` | Add /crawlers route |
| `DocumentsView.jsx` | Add crawler filter, crawled badge, crawl operation link |
| `api.js` | Add crawler API methods |
| `App.css` | Add crawler-related styles |
| `docker/compose.yaml` | Add enumeration volume |
| `docker/factory/reset.sh` | Clear enumerations |
| `docker/factory/reset.bat` | Clear enumerations |

### New Migration

| File | Purpose |
|------|--------|
| `migrations/003_add_crawlers.sql` | Add tables for existing installations |
