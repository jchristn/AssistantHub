# AssistantHub Multi-Tenant Migration Plan

**Version:** v0.4.0 → v0.4.0
**Status:** Implementation Complete
**Breaking Changes:** Yes (migration guide included)

### Implementation Progress

| Phase | Status |
|-------|--------|
| 1. Core Infrastructure (Models, IdGenerator, Constants) | Complete |
| 2. Database Layer (all 4 drivers, tenants table, tenant_id columns) | Complete |
| 3. Authentication & Authorization (AuthContext, HandlerBase, AuthenticationService) | Complete |
| 4. API Handlers (all handlers updated with tenant scoping) | Complete |
| 5. Services (RetrievalService, IngestionService, TenantProvisioningService) | Complete |
| 6. First-Run Init & Migration (data migrations in all 4 drivers) | Complete |
| 7. Frontend Dashboard (AuthContext, TenantsView, Sidebar, Topbar, API client) | Complete |
| 8-9. Docker, Postman, Documentation (config updates) | Complete |
| 10. Test.Database (TenantTests + all 8 test classes updated with TenantId) | Complete |
| 11. S3 tenant prefix isolation (DocumentHandler, BucketHandler) | Complete |
| 12. ProcessingLogService tenant namespacing | Complete |
| 13. Tenant deletion cascade (DeprovisionAsync) | Complete |
| 14. Frontend Login TenantId + view scoping (6 views with Tenant column) | Complete |

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture Decisions](#2-architecture-decisions)
3. [Database Schema Changes](#3-database-schema-changes)
4. [Backend: Core Models](#4-backend-core-models)
5. [Backend: Authentication & Authorization](#5-backend-authentication--authorization)
6. [Backend: Database Layer](#6-backend-database-layer)
7. [Backend: Services](#7-backend-services)
8. [Backend: API Handlers](#8-backend-api-handlers)
9. [Backend: Configuration](#9-backend-configuration)
10. [Frontend: Dashboard](#10-frontend-dashboard)
11. [External Service Integration](#11-external-service-integration)
12. [API Contract Changes](#12-api-contract-changes)
13. [Tenant Administration Experience](#13-tenant-administration-experience)
14. [Migration Script (v0.4.0 → v0.4.0)](#14-migration-script-v020--v030)
15. [Docker & Deployment](#15-docker--deployment)
16. [Postman Collection](#16-postman-collection)
17. [Documentation](#17-documentation)
18. [Testing Plan](#18-testing-plan)
19. [Implementation Order](#19-implementation-order)

---

## 1. Overview

### 1.1 Goal

Convert AssistantHub from a single-tenant platform to a multi-tenant platform, enabling multiple isolated organizations to share a single deployment. Follow the proven patterns established in RecallDB, Partio, and Verbex.

### 1.2 Tenancy Model

- **Row-level isolation** — all tenants share the same database schema; tenant scoping enforced via `tenant_id` columns and application-layer filtering
- **Shared infrastructure** — single deployment serves all tenants (S3, RecallDB, Ollama, etc.)
- **Per-tenant RecallDB tenants** — each AssistantHub tenant maps to its own RecallDB tenant for vector isolation
- **Per-tenant S3 prefix isolation** — S3 objects prefixed by tenant ID to prevent cross-tenant access

### 1.3 Reference Implementations

| Project | Key Pattern Adopted |
|---------|-------------------|
| RecallDB | TenantMetadata model, AuthenticationResult with tenant context, URL-path tenant routing for admin APIs, default tenant auto-creation |
| Partio | AuthContext propagation, `RequireAdmin` pattern, per-tenant endpoint scoping, auto-provisioning on tenant creation |
| Verbex | Cascade delete behavior, composite unique indexes, `IsTenantAdmin` role, labels/tags metadata |

---

## 2. Architecture Decisions

### 2.1 Tenant Identification

- [x] Use k-sortable IDs with `ten_` prefix (consistent with RecallDB/Partio/Verbex)
- [x] Add `IdGenerator.NewTenantId()` method
- [x] Default tenant ID: `"default"` (for migration of existing data)

### 2.2 Authorization Levels

| Role | Scope | Description |
|------|-------|-------------|
| **Global Admin** | All tenants | Authenticated via admin API keys in config, or any user with `IsAdmin=true`. Can manage all tenants, users, and resources. |
| **Tenant Admin** | Single tenant | `is_tenant_admin = true`. Can manage users, credentials, assistants, ingestion rules, and all resources within their tenant. |
| **Tenant User** | Single tenant | Standard user. Can create/manage their own assistants and documents within their tenant. |

- [x] Add `AdminApiKeys` list to settings (new config key)
- [x] Add `is_tenant_admin` field to `UserMaster` as `IsTenantAdmin`
- [x] Global admins bypass tenant scoping checks

### 2.3 Tenant Context Propagation

- [x] Create `AuthContext` class (replaces current ad-hoc auth result)
- [x] Store `AuthContext` in request metadata (as Partio/Verbex do)
- [x] Every handler extracts `AuthContext` and uses `TenantId` for scoping
- [x] Bearer token → credential → user → tenant chain for resolution

### 2.4 API URL Design

Admin/tenant management routes use explicit tenant ID in path:
```
/v1.0/tenants
/v1.0/tenants/{tenantId}
/v1.0/tenants/{tenantId}/users
/v1.0/tenants/{tenantId}/credentials
```

All other resource routes derive tenant from authenticated context (no tenant in URL):
```
/v1.0/assistants          → scoped to auth.TenantId
/v1.0/documents           → scoped to auth.TenantId
/v1.0/ingestion-rules     → scoped to auth.TenantId
/v1.0/history             → scoped to auth.TenantId
/v1.0/feedback            → scoped to auth.TenantId
```

Public chat routes remain unchanged (assistant ID resolves to tenant implicitly):
```
POST /v1.0/assistants/{assistantId}/chat
```

---

## 3. Database Schema Changes

### 3.1 New Table: `tenants`

- [x] Create `tenants` table across all four database drivers (SQLite, PostgreSQL, SQL Server, MySQL)

```sql
CREATE TABLE IF NOT EXISTS tenants (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    active INTEGER NOT NULL DEFAULT 1,
    labels_json TEXT,
    tags_json TEXT,
    created_utc TEXT NOT NULL,
    last_update_utc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_tenants_name ON tenants(name);
CREATE INDEX IF NOT EXISTS idx_tenants_created_utc ON tenants(created_utc);
```

### 3.2 Add `tenant_id` Column to Existing Tables

Each table below needs a `tenant_id TEXT NOT NULL` column added, with existing rows backfilled to `'default'`.

- [x] **`users`** — add `tenant_id`, add `is_tenant_admin INTEGER NOT NULL DEFAULT 0`
  - [x] Add index: `idx_users_tenant_id ON users(tenant_id)`
  - [x] Add composite unique: `idx_users_tenant_email ON users(tenant_id, email)`
  - [x] Drop existing `idx_users_email` (email no longer globally unique)

- [x] **`credentials`** — add `tenant_id`
  - [x] Add index: `idx_credentials_tenant_id ON credentials(tenant_id)`

- [x] **`assistants`** — add `tenant_id`
  - [x] Add index: `idx_assistants_tenant_id ON assistants(tenant_id)`

- [x] **`assistant_settings`** — no direct `tenant_id` (scoped via `assistant_id` → `assistants.tenant_id`)
  - [x] No schema change needed (joins through assistant)

- [x] **`assistant_documents`** — add `tenant_id`
  - [x] Add index: `idx_assistant_documents_tenant_id ON assistant_documents(tenant_id)`

- [x] **`assistant_feedback`** — add `tenant_id`
  - [x] Add index: `idx_assistant_feedback_tenant_id ON assistant_feedback(tenant_id)`

- [x] **`ingestion_rules`** — add `tenant_id`
  - [x] Add index: `idx_ingestion_rules_tenant_id ON ingestion_rules(tenant_id)`
  - [x] Add composite unique: `idx_ingestion_rules_tenant_name ON ingestion_rules(tenant_id, name)`

- [x] **`chat_history`** — add `tenant_id`
  - [x] Add index: `idx_chat_history_tenant_id ON chat_history(tenant_id)`

### 3.3 Migration SQL File

- [x] Create `migrations/009_add_multi_tenancy.sql` with all of the above
- [x] Migration must be idempotent and support all four database types
- [x] Backfill all existing rows with `tenant_id = 'default'`
- [x] Insert default tenant row: `INSERT INTO tenants (id, name, active, created_utc, last_update_utc) VALUES ('default', 'Default Tenant', 1, ...)`

### 3.4 Migration Queries in Code

For each database driver (SQLite, PostgreSQL, SQL Server, MySQL):

- [x] `Sqlite/Queries/TableQueries.cs` — add `CreateTenantsTable`, update all `CREATE TABLE` statements
- [x] `Postgresql/Queries/TableQueries.cs` — same
- [x] `Sqlserver/Queries/TableQueries.cs` — same
- [x] `Mysql/Queries/TableQueries.cs` — same
- [x] Add migration detection in `DatabaseDriverBase` to apply `009_add_multi_tenancy` on startup

---

## 4. Backend: Core Models

### 4.1 New Model: `TenantMetadata`

- [x] Create `src/AssistantHub.Core/Models/TenantMetadata.cs`

```csharp
public class TenantMetadata
{
    public string Id { get; set; }                          // ten_ prefix, k-sortable
    public string Name { get; set; }                        // Display name
    public bool Active { get; set; } = true;                // Soft disable
    public List<string> Labels { get; set; }                // Categorization
    public Dictionary<string, string> Tags { get; set; }    // Key-value metadata
    public DateTime CreatedUtc { get; set; }
    public DateTime LastUpdateUtc { get; set; }
}
```

### 4.2 New Model: `AuthContext`

- [x] Create `src/AssistantHub.Core/Models/AuthContext.cs`

```csharp
public class AuthContext
{
    public bool IsAuthenticated { get; set; }
    public bool IsGlobalAdmin { get; set; }
    public bool IsTenantAdmin { get; set; }
    public string TenantId { get; set; }
    public string UserId { get; set; }
    public string CredentialId { get; set; }
    public string Email { get; set; }
    public TenantMetadata Tenant { get; set; }
    public UserMaster User { get; set; }
}
```

### 4.3 Update Existing Models

- [x] **`UserMaster.cs`** — add `TenantId` (string), add `IsTenantAdmin` (bool, default false)
- [x] **`Credential.cs`** — add `TenantId` (string)
- [x] **`Assistant.cs`** — add `TenantId` (string)
- [x] **`AssistantDocument.cs`** — add `TenantId` (string)
- [x] **`AssistantFeedback.cs`** — add `TenantId` (string)
- [x] **`IngestionRule.cs`** — add `TenantId` (string)
- [x] **`ChatHistory.cs`** — add `TenantId` (string)

### 4.4 ID Generator

- [x] Add `NewTenantId()` to `IdGenerator.cs` returning `ten_` prefixed k-sortable ID

---

## 5. Backend: Authentication & Authorization

### 5.1 Refactor `AuthenticationService`

- [x] Update `AuthenticateBearerAsync` to return `AuthContext` instead of current result
- [x] Add admin API key check: if bearer token matches any entry in `Settings.AdminApiKeys`, return `AuthContext { IsGlobalAdmin = true }`
- [x] For credential-based auth: load credential → load user → load tenant → validate all are active → populate full `AuthContext` with `IsGlobalAdmin = user.IsAdmin`
- [x] Add `AuthenticateEmailPasswordAsync(string tenantId, string email, string password)` returning `AuthContext` with `IsGlobalAdmin = user.IsAdmin`
- [x] Users with `IsAdmin=true` are treated as global admins (cross-tenant access) in addition to API key holders

### 5.2 Authentication Flow

The authentication middleware runs on every request to a `PostAuthentication` route. It extracts the bearer token from the `Authorization` header and resolves it to an `AuthContext`, which is stored in `ctx.Metadata` for all downstream handlers to use.

```
HTTP Request Received (PostAuthentication route)
  │
  ├── No Authorization header? → 401 Unauthorized
  │
  └── Extract bearer token from "Bearer {token}"
       │
       ├── Token matches any entry in Settings.AdminApiKeys?
       │   → ctx.Metadata = AuthContext {
       │       IsAuthenticated = true,
       │       IsGlobalAdmin = true,
       │       IsTenantAdmin = false,
       │       TenantId = null,     // Global admin has no tenant scope
       │       UserId = null,
       │     }
       │
       └── Look up token in credentials table (ReadByBearerTokenAsync)
            │
            ├── Credential not found → 401 Unauthorized
            ├── Credential.Active == false → 401 Unauthorized
            │
            └── Load user by credential.UserId (ReadByIdAsync)
                 │
                 ├── User not found → 401 Unauthorized
                 ├── User.Active == false → 401 Unauthorized
                 │
                 └── Load tenant by credential.TenantId (ReadByIdAsync)
                      │
                      ├── Tenant not found → 401 Unauthorized
                      ├── Tenant.Active == false → 401 Unauthorized
                      │
                      └── ctx.Metadata = AuthContext {
                            IsAuthenticated = true,
                            IsGlobalAdmin = user.IsAdmin,
                            IsTenantAdmin = user.IsTenantAdmin,
                            TenantId = tenant.Id,
                            UserId = user.Id,
                            CredentialId = credential.Id,
                            Email = user.Email,
                            Tenant = tenant,
                            User = user,
                          }
```

**Key invariant:** After authentication, every handler can rely on `AuthContext` being populated. If `auth.IsGlobalAdmin == false`, then `auth.TenantId` is guaranteed to be non-null and refers to an active tenant. Handlers use this to enforce tenant-scoped access.

**Email/password authentication** (`POST /v1.0/authenticate` with `TenantId`, `Email`, `Password`):
- [x] Look up user by `(tenantId, email)` composite key
- [x] Verify password hash matches
- [x] Load tenant, validate active
- [x] Generate or look up credential for response token
- [x] Return same `AuthContext` structure

### 5.3 Authorization Helpers

- [x] Create helper methods in server (or a shared utility):

```csharp
// Require any authenticated user; throws 401 if not authenticated
static AuthContext RequireAuth(HttpContextBase ctx);

// Require global admin; throws 401 if not authenticated, 403 if not global admin
static AuthContext RequireGlobalAdmin(HttpContextBase ctx);

// Require global admin OR tenant admin; throws 401/403 accordingly
static AuthContext RequireAdmin(HttpContextBase ctx);

// Validate caller can access the given tenant; returns true for global admin or matching tenant
static bool ValidateTenantAccess(AuthContext auth, string tenantId);

// Validate a loaded record belongs to the caller's tenant; throws 403 if mismatch
// Global admins bypass this check
static void EnforceTenantOwnership(AuthContext auth, string recordTenantId, string recordId);
```

### 5.4 Authorization Enforcement on ID-Based Lookups

**Critical rule:** When a non-global-admin user requests a record by ID in the URL (e.g. `GET /v1.0/assistants/{assistantId}`), the handler MUST:

1. Load the record from the database by ID
2. If the record is not found, return `404`
3. **Check that `record.TenantId == auth.TenantId`** — if not, return `403 Forbidden` (or `404` to avoid leaking existence)
4. Only then proceed with the operation

Global admins (`auth.IsGlobalAdmin == true`) bypass step 3 and can access records in any tenant.

- [x] Implement `EnforceTenantOwnership(auth, recordTenantId, recordId)` helper
- [x] Apply to ALL ID-based read, update, and delete operations across all handlers

**Enforcement pattern (pseudocode):**

```csharp
async Task<object> GetAssistantAsync(HttpContextBase ctx)
{
    AuthContext auth = RequireAuth(ctx);
    string assistantId = ctx.Request.Url.Parameters["assistantId"];

    Assistant assistant = await _Database.Assistant.ReadByIdAsync(assistantId);
    if (assistant == null) return NotFound();

    // TENANT CONSISTENCY CHECK — non-global-admins can only access their own tenant's records
    if (!auth.IsGlobalAdmin && assistant.TenantId != auth.TenantId)
        return Forbidden();  // or NotFound() to avoid leaking existence

    return assistant;
}
```

**This pattern applies to every handler that accepts a resource ID in the URL:**

| Handler | ID Parameter(s) | Record(s) to Check |
|---------|----------------|-------------------|
| `AssistantHandler` | `{assistantId}` | `assistant.TenantId` |
| `AssistantSettingsHandler` | `{assistantId}` | Load assistant first, check `assistant.TenantId` |
| `DocumentHandler` | `{documentId}` | `document.TenantId` |
| `IngestionRuleHandler` | `{ruleId}` | `rule.TenantId` |
| `FeedbackHandler` | `{feedbackId}` | `feedback.TenantId` |
| `HistoryHandler` | `{historyId}` | `history.TenantId` |
| `UserHandler` | `{tenantId}`, `{userId}` | `ValidateTenantAccess(auth, tenantId)` on URL path + `user.TenantId` on record |
| `CredentialHandler` | `{tenantId}`, `{credentialId}` | `ValidateTenantAccess(auth, tenantId)` on URL path + `credential.TenantId` on record |
| `CollectionHandler` | `{collectionId}` | Scoped via RecallDB tenant routing (implicit) |
| `BucketHandler` | `{name}` | Scoped via S3 tenant prefix (implicit) |
| `ChatHandler` (public) | `{assistantId}` | No auth, but assistant lookup resolves tenant for downstream scoping |

**For list/enumerate operations**, tenant scoping is enforced at the query level (WHERE clause), not post-load. This prevents data leakage and is more efficient:

```csharp
async Task<object> ListAssistantsAsync(HttpContextBase ctx)
{
    AuthContext auth = RequireAuth(ctx);

    // Tenant scoping happens in the query — only returns records matching tenant
    var results = await _Database.Assistant.EnumerateAsync(auth.TenantId, enumRequest);
    return results;
}
```

**For tenant-path routes** (`/v1.0/tenants/{tenantId}/users`), there is a double check:

1. Validate `tenantId` from URL matches `auth.TenantId` (or caller is global admin)
2. When loading a specific record by ID, also verify the record's `TenantId` matches the URL `tenantId`

```csharp
async Task<object> GetUserAsync(HttpContextBase ctx)
{
    AuthContext auth = RequireAdmin(ctx);
    string tenantId = ctx.Request.Url.Parameters["tenantId"];
    string userId = ctx.Request.Url.Parameters["userId"];

    // CHECK 1: Can this caller access this tenant at all?
    if (!ValidateTenantAccess(auth, tenantId))
        return Forbidden();

    UserMaster user = await _Database.User.ReadByIdAsync(userId);
    if (user == null) return NotFound();

    // CHECK 2: Does the loaded record actually belong to this tenant?
    // Prevents accessing a record via a tenant URL it doesn't belong to
    if (user.TenantId != tenantId)
        return NotFound();

    return UserMaster.Redact(user);
}
```

### 5.5 `/v1.0/authenticate` Endpoint Update

- [x] Accept optional `TenantId` field for email/password login
- [x] Return `TenantId`, `TenantName`, `IsGlobalAdmin`, `IsTenantAdmin` in response
- [x] Bearer token auth auto-resolves tenant from credential

### 5.5 Add `/v1.0/whoami` Endpoint

- [x] New endpoint returning current auth context (role, tenant, user info)
- [x] Used by dashboard to determine UI capabilities

---

## 6. Backend: Database Layer

### 6.1 New Interface: `ITenantMethods`

- [x] Create `src/AssistantHub.Core/Database/Interfaces/ITenantMethods.cs`

```csharp
public interface ITenantMethods
{
    Task<TenantMetadata> CreateAsync(TenantMetadata tenant);
    Task<TenantMetadata> ReadByIdAsync(string id);
    Task<TenantMetadata> ReadByNameAsync(string name);
    Task<TenantMetadata> UpdateAsync(TenantMetadata tenant);
    Task DeleteByIdAsync(string id);
    Task<bool> ExistsByIdAsync(string id);
    Task<EnumerationResult<TenantMetadata>> EnumerateAsync(EnumerationRequest request);
}
```

### 6.2 Implement `ITenantMethods` for Each Database Driver

- [x] `Sqlite/Implementations/TenantMethods.cs`
- [x] `Postgresql/Implementations/TenantMethods.cs`
- [x] `Sqlserver/Implementations/TenantMethods.cs`
- [x] `Mysql/Implementations/TenantMethods.cs`

### 6.3 Update Existing Database Interfaces

Add `tenantId` parameter to all query/enumerate methods:

- [x] **`IUserMethods`**
  - [x] `EnumerateAsync(string tenantId, ...)` — filter by tenant
  - [x] `ReadByEmailAsync(string tenantId, string email)` — composite lookup
  - [x] `CreateAsync(UserMaster user)` — user.TenantId must be set
  - [x] All read/update/delete methods validate tenant ownership

- [x] **`ICredentialMethods`**
  - [x] `EnumerateAsync(string tenantId, ...)`
  - [x] `ReadByBearerTokenAsync(string token)` — returns credential with TenantId
  - [x] `CreateAsync(Credential cred)` — cred.TenantId must be set

- [x] **`IAssistantMethods`**
  - [x] `EnumerateAsync(string tenantId, ...)` — filter by tenant (not just user_id)
  - [x] `CreateAsync(Assistant asst)` — asst.TenantId must be set
  - [x] Read/update/delete validate tenant ownership

- [x] **`IAssistantSettingsMethods`**
  - [x] No direct tenant changes (scoped via assistant_id join)
  - [x] Ensure reads validate the parent assistant's tenant

- [x] **`IDocumentMethods`**
  - [x] `EnumerateAsync(string tenantId, ...)`
  - [x] `CreateAsync(AssistantDocument doc)` — doc.TenantId must be set

- [x] **`IIngestionRuleMethods`**
  - [x] `EnumerateAsync(string tenantId, ...)`
  - [x] `CreateAsync(IngestionRule rule)` — rule.TenantId must be set
  - [x] `ReadByNameAsync(string tenantId, string name)` — composite lookup

- [x] **`IChatHistoryMethods`**
  - [x] `EnumerateAsync(string tenantId, ...)`

- [x] **`IFeedbackMethods`**
  - [x] `EnumerateAsync(string tenantId, ...)`

### 6.4 Update All Four Database Driver Implementations

For each interface change above, update the implementation in all four drivers:

- [x] SQLite implementations (8 method files)
- [x] PostgreSQL implementations (8 method files)
- [x] SQL Server implementations (8 method files)
- [x] MySQL implementations (8 method files)

### 6.5 Add `Tenant` Property to `DatabaseDriverBase`

- [x] Add `public ITenantMethods Tenant { get; }` to the base class
- [x] Initialize in each driver's constructor

### 6.6 SQL Query Updates

Every `SELECT`, `UPDATE`, `DELETE` query that touches a tenant-scoped table must include `WHERE tenant_id = @tenantId`:

- [x] Audit and update all SQL queries in all four drivers
- [x] Add `tenant_id` to all `INSERT` statements
- [x] Update `DataTableHelper` mappings to include `tenant_id`

---

## 7. Backend: Services

### 7.1 `AuthenticationService` Refactor

(Covered in Section 5 above)

### 7.2 `StorageService` (S3) Updates

- [x] Per-tenant bucket isolation: each tenant gets buckets prefixed with `{tenantId}_` (e.g. `ten_abc123_default`)
- [x] Auto-provision tenant S3 bucket during tenant creation
- [x] Auto-delete tenant S3 buckets (with objects) during tenant deletion
- [x] Enforce bucket name prefix `{tenantId}_` for non-global-admin users in BucketHandler
- [x] Enforce bucket name prefix on ingestion rule create/update
- [x] Ensure no cross-tenant S3 access is possible

### 7.3 `IngestionService` Updates

- [x] Accept `tenantId` parameter in all ingestion operations
- [x] Pass tenant-specific RecallDB tenant ID when storing vectors
- [x] Use tenant-prefixed bucket names for document storage
- [x] Update `ProcessDocumentAsync` to propagate tenant context

### 7.4 `RetrievalService` Updates

- [x] Accept `tenantId` parameter for all search operations
- [x] Use tenant-specific RecallDB tenant ID when querying vectors
- [x] Validate collection belongs to tenant before searching

### 7.5 `InferenceService` Updates

- [x] No direct tenant changes needed (inference is stateless)
- [x] Ensure endpoint resolution validates tenant ownership if endpoints become tenant-scoped (see Section 11)

### 7.6 `ProcessingLogService` Updates

- [x] Namespace processing log directories by tenant: `{logDir}/{tenantId}/{documentId}/`
- [x] Update log retrieval to include tenant path

### 7.7 New Service: `TenantProvisioningService`

- [x] Create `src/AssistantHub.Core/Services/TenantProvisioningService.cs`
- [x] On tenant creation, automatically provision:
  - [x] RecallDB tenant via RecallDB API (`PUT /v1.0/tenants`)
  - [x] Default admin user (`admin@{tenantName}`, password: `password`)
  - [x] Default credential (auto-generated bearer token)
  - [x] Default ingestion rule (using system-configured S3 bucket and RecallDB collection)
- [x] On tenant deletion:
  - [x] Delete RecallDB tenant (`DELETE /v1.0/tenants/{id}?force=true`)
  - [x] Clean up S3 objects with tenant prefix
  - [x] Delete all tenant rows from all tables (cascade)

---

## 8. Backend: API Handlers

### 8.1 New Handler: `TenantHandler`

- [x] Create `src/AssistantHub.Server/Handlers/TenantHandler.cs`

| Route | Method | Auth | Description |
|-------|--------|------|-------------|
| `PUT /v1.0/tenants` | PUT | Global Admin | Create tenant (with auto-provisioning) |
| `GET /v1.0/tenants` | GET | Any Authenticated | List tenants (see authorization details below) |
| `POST /v1.0/tenants/enumerate` | POST | Any Authenticated | Enumerate tenants with pagination (see authorization details below) |
| `GET /v1.0/tenants/{id}` | GET | Any Authenticated | Get tenant details (see authorization details below) |
| `PUT /v1.0/tenants/{id}` | PUT | Global Admin | Update tenant |
| `DELETE /v1.0/tenants/{id}` | DELETE | Global Admin | Delete tenant (cascade) |
| `HEAD /v1.0/tenants/{id}` | HEAD | Any Authenticated | Check tenant exists (see authorization details below) |

**Tenant list/enumerate authorization (matches RecallDB and Verbex patterns):**

Both `GET /v1.0/tenants` and `POST /v1.0/tenants/enumerate` use the same enumeration facilities as other entity types (`EnumerationRequest` / `EnumerationResult`), but the data returned varies by role:

| Role | Behavior |
|------|----------|
| **Global Admin** | Full enumeration of all tenants with pagination, ordering, continuation tokens |
| **Tenant Admin** | Returns `EnumerationResult` with exactly one object: their own tenant. `EndOfResults = true`, `TotalRecords = 1`. |
| **Regular User** | Same as tenant admin: returns `EnumerationResult` with exactly one object: their own tenant. |

- [x] Any authenticated user can call these endpoints (no `RequireAdmin` gate)
- [x] Global admin: delegate to `_Database.Tenant.EnumerateAsync(enumRequest)` — returns full paginated results
- [x] Non-global-admin: build an `EnumerationResult` with `auth.Tenant` as the single object — no database query needed since the tenant is already loaded in `AuthContext`

```csharp
async Task<object> ListTenantsAsync(HttpContextBase ctx)
{
    AuthContext auth = RequireAuth(ctx);

    if (auth.IsGlobalAdmin)
    {
        // Full enumeration with pagination
        EnumerationResult<TenantMetadata> result = await _Database.Tenant.EnumerateAsync(enumRequest);
        return result;
    }
    else
    {
        // Return only the caller's own tenant
        return new EnumerationResult<TenantMetadata>
        {
            MaxResults = 1,
            EndOfResults = true,
            TotalRecords = 1,
            RecordsRemaining = 0,
            Objects = new List<TenantMetadata> { auth.Tenant }
        };
    }
}
```

**Tenant get-by-ID authorization:**

`GET /v1.0/tenants/{id}` and `HEAD /v1.0/tenants/{id}` are accessible to any authenticated user, but non-global-admins can only access their own tenant:

| Role | Behavior |
|------|----------|
| **Global Admin** | Can read any tenant by ID |
| **Tenant Admin** | Can read their own tenant only (`auth.TenantId == id`); returns `403` for other tenants |
| **Regular User** | Same as tenant admin: can read their own tenant only |

- [x] Any authenticated user can call these endpoints
- [x] `ValidateTenantAccess(auth, id)` — global admin passes; non-global-admin passes only if `auth.TenantId == id`
- [x] If access denied, return `403 Forbidden`
- [x] If tenant not found, return `404 Not Found`

```csharp
async Task<object> GetTenantAsync(HttpContextBase ctx)
{
    AuthContext auth = RequireAuth(ctx);
    string tenantId = ctx.Request.Url.Parameters["id"];

    if (!ValidateTenantAccess(auth, tenantId))
        return Forbidden();

    TenantMetadata tenant = await _Database.Tenant.ReadByIdAsync(tenantId);
    if (tenant == null) return NotFound();

    return tenant;
}
```

**Tenant create/update/delete authorization:**

These remain global-admin-only (`RequireGlobalAdmin`). No tenant admin or regular user can create, modify, or delete tenants.

### 8.2 Update `AuthenticationHandler`

- [x] Refactor to produce `AuthContext` and store in `ctx.Metadata`
- [x] Add admin API key validation
- [x] Add tenant existence and active check
- [x] Support `TenantId` field in email/password auth requests

### 8.3 Update `UserHandler`

- [x] Move routes under tenant path: `GET /v1.0/tenants/{tenantId}/users`
- [x] **List:** `ValidateTenantAccess(auth, tenantId)` on URL path; enumerate scoped by `tenantId`
- [x] **Get by ID:** `ValidateTenantAccess(auth, tenantId)` on URL path; load user; verify `user.TenantId == tenantId`; return `404` if mismatch
- [x] **Create:** `ValidateTenantAccess(auth, tenantId)`; set `user.TenantId = tenantId` before insert
- [x] **Update by ID:** `ValidateTenantAccess(auth, tenantId)`; load user; verify `user.TenantId == tenantId`; apply changes
- [x] **Delete by ID:** `ValidateTenantAccess(auth, tenantId)`; load user; verify `user.TenantId == tenantId`; delete
- [x] Global admin can specify any `tenantId`; tenant admin only their own
- [x] Add `IsTenantAdmin` field to create/update requests

### 8.4 Update `CredentialHandler`

- [x] Move routes under tenant path: `GET /v1.0/tenants/{tenantId}/credentials`
- [x] **List:** `ValidateTenantAccess(auth, tenantId)` on URL path; enumerate scoped by `tenantId`
- [x] **Get by ID:** `ValidateTenantAccess(auth, tenantId)`; load credential; verify `credential.TenantId == tenantId`; return `404` if mismatch
- [x] **Create:** `ValidateTenantAccess(auth, tenantId)`; set `credential.TenantId = tenantId`
- [x] **Update by ID:** `ValidateTenantAccess(auth, tenantId)`; load credential; verify `credential.TenantId == tenantId`; apply
- [x] **Delete by ID:** `ValidateTenantAccess(auth, tenantId)`; load credential; verify `credential.TenantId == tenantId`; delete

### 8.5 Update `AssistantHandler`

- [x] Extract `tenantId` from `AuthContext` (not URL)
- [x] **List:** Enumerate with `auth.TenantId`; additionally filter by `user_id` for non-admin users (tenant admins see all)
- [x] **Get by ID:** Load assistant; `EnforceTenantOwnership(auth, assistant.TenantId, assistantId)` — return `403`/`404` if tenant mismatch for non-global-admins
- [x] **Create:** Set `assistant.TenantId = auth.TenantId`; set `assistant.UserId = auth.UserId`
- [x] **Update by ID:** Load assistant; `EnforceTenantOwnership(auth, assistant.TenantId, assistantId)`; additionally verify `auth.UserId == assistant.UserId` or caller is admin; apply changes
- [x] **Delete by ID:** Load assistant; `EnforceTenantOwnership(auth, assistant.TenantId, assistantId)`; additionally verify `auth.UserId == assistant.UserId` or caller is admin; delete

### 8.6 Update `AssistantSettingsHandler`

- [x] **Get/Update settings:** Load parent assistant first; `EnforceTenantOwnership(auth, assistant.TenantId, assistantId)` — if tenant mismatch, return `403`/`404`
- [x] No direct `tenant_id` on settings (inherited from assistant)
- [x] Verify `auth.UserId == assistant.UserId` or caller is admin before allowing changes

### 8.7 Update `DocumentHandler`

- [x] Extract `tenantId` from `AuthContext`
- [x] **List:** Enumerate with `auth.TenantId`
- [x] **Get by ID:** Load document; `EnforceTenantOwnership(auth, document.TenantId, documentId)`
- [x] **Create:** Set `document.TenantId = auth.TenantId`
- [x] **Delete by ID:** Load document; `EnforceTenantOwnership(auth, document.TenantId, documentId)`; then delete
- [x] **Download by ID:** Load document; `EnforceTenantOwnership(auth, document.TenantId, documentId)`; then serve file
- [x] **Processing log:** Load document; `EnforceTenantOwnership(auth, document.TenantId, documentId)`; then return log

### 8.8 Update `IngestionRuleHandler`

- [x] Extract `tenantId` from `AuthContext`
- [x] **List:** Enumerate with `auth.TenantId`
- [x] **Get by ID:** Load rule; `EnforceTenantOwnership(auth, rule.TenantId, ruleId)`
- [x] **Create:** Set `rule.TenantId = auth.TenantId`
- [x] **Update by ID:** Load rule; `EnforceTenantOwnership(auth, rule.TenantId, ruleId)`; apply changes
- [x] **Delete by ID:** Load rule; `EnforceTenantOwnership(auth, rule.TenantId, ruleId)`; delete

### 8.9 Update `ChatHandler` (Public Chat)

Public chat endpoints are unauthenticated, so tenant context is derived from the assistant:

- [x] **All public routes** (`/v1.0/assistants/{assistantId}/chat`, `/feedback`, `/threads`, etc.):
  - Load assistant by `assistantId`
  - Resolve `tenantId = assistant.TenantId`
  - Use this tenant ID for all downstream operations (RecallDB queries, history logging, etc.)
- [x] Set `chatHistory.TenantId = assistant.TenantId` when logging
- [x] Set `feedback.TenantId = assistant.TenantId` on public feedback submission
- [x] **Public document download** (`/v1.0/assistants/{assistantId}/documents/{documentId}/download`):
  - Load assistant; resolve tenant
  - Load document; verify `document.TenantId == assistant.TenantId` — prevents cross-tenant document access via crafted URLs

### 8.10 Update `FeedbackHandler`

- [x] Extract `tenantId` from `AuthContext`
- [x] **List:** Enumerate with `auth.TenantId`
- [x] **Get by ID:** Load feedback; `EnforceTenantOwnership(auth, feedback.TenantId, feedbackId)`
- [x] **Delete by ID:** Load feedback; `EnforceTenantOwnership(auth, feedback.TenantId, feedbackId)`; delete

### 8.11 Update `HistoryHandler`

- [x] Extract `tenantId` from `AuthContext`
- [x] **List history:** Enumerate with `auth.TenantId`
- [x] **Get by ID:** Load history; `EnforceTenantOwnership(auth, history.TenantId, historyId)`
- [x] **Delete by ID:** Load history; `EnforceTenantOwnership(auth, history.TenantId, historyId)`; delete
- [x] **List threads:** Enumerate threads filtered by `auth.TenantId`

### 8.12 Update `CollectionHandler`

- [x] Route all RecallDB requests through tenant's RecallDB tenant ID (`auth.TenantId`) instead of hardcoded `"default"`
- [x] **List:** Use `GET /v1.0/tenants/{auth.TenantId}/collections` against RecallDB
- [x] **Get by ID / Create / Update / Delete:** Use `{auth.TenantId}` in RecallDB URL path — RecallDB enforces its own tenant isolation
- [x] Global admin: can specify a different tenant ID to access another tenant's collections

### 8.13 Update `BucketHandler`

- [x] **Bucket-level operations** (create, list, get, delete, head): Enforce `{auth.TenantId}_` bucket name prefix for non-global-admin users
- [x] **Create bucket:** Auto-prefix with `{auth.TenantId}_` for non-global-admin; require admin role
- [x] **List buckets:** Filter to only buckets starting with `{auth.TenantId}_` for non-global-admin
- [x] **Object-level operations** (list, get, download, upload, delete): Enforce bucket name starts with `{auth.TenantId}_` for non-global-admin

### 8.14 Update `EmbeddingEndpointHandler` and `CompletionEndpointHandler`

- [x] **Decision:** Are endpoints global (shared) or per-tenant?
  - **Recommended:** Keep endpoints global (configured by global admin), any tenant can use them
  - Alternative: Per-tenant endpoints (as in Partio) — more isolation but more management overhead
- [x] If global: no tenant_id on endpoints, but validate endpoint exists before use; CRUD restricted to global admin
- [x] If per-tenant: add `tenant_id` to endpoint tables; all ID-based operations must `EnforceTenantOwnership`

### 8.15 Update `ConfigurationHandler`

- [x] Keep as global-admin-only (system-level configuration)
- [x] `RequireGlobalAdmin(ctx)` on all operations
- [x] No tenant scoping needed

### 8.16 Update `InferenceHandler` (Models)

- [x] Keep as global (Ollama models are shared infrastructure)
- [x] No tenant scoping needed

### 8.17 Register All New Routes

- [x] Add tenant routes in `AssistantHubServer.cs`
- [x] Update user/credential routes to new paths
- [x] Add `/v1.0/whoami` route
- [x] Update authentication middleware

---

## 9. Backend: Configuration

### 9.1 Update `assistanthub.json` Schema

- [x] Add `AdminApiKeys` array (list of global admin bearer tokens)
- [x] Remove hardcoded `RecallDb.TenantId` — tenant ID now dynamic per tenant
- [x] Add `DefaultTenant` section for first-run provisioning

```json
{
  "AdminApiKeys": ["assistanthubadmin"],
  "RecallDb": {
    "Endpoint": "http://recalldb-server:8600",
    "AccessKey": "recalldbadmin"
  },
  "DefaultTenant": {
    "Name": "Default Tenant",
    "AdminEmail": "admin@assistanthub",
    "AdminPassword": "password"
  }
}
```

### 9.2 Update Settings Classes

- [x] Add `AdminApiKeys` property to `ServerSettings` (or top-level settings)
- [x] Remove `TenantId` from `RecallDbSettings`
- [x] Add `DefaultTenantSettings` class with `Name`, `AdminEmail`, `AdminPassword`

### 9.3 First-Run Initialization (Default Tenant & Records)

The current `InitializeFirstRunAsync()` in `AssistantHubServer.cs` (lines 174–256) checks `userCount > 0` and, if no users exist, creates a default admin user, credential, ingestion rule, and RecallDB collection — all with hardcoded values and no tenant concept.

This must be completely reworked to be tenant-aware. The new logic checks `tenantCount` instead of `userCount`, and all created records include `tenant_id = 'default'`.

#### 9.3.1 Trigger Condition

- [x] Change first-run check from `User.GetCountAsync() == 0` to `Tenant.GetCountAsync() == 0`
- [x] If tenants exist, skip first-run setup entirely

#### 9.3.2 Step 1: Create Default Tenant

- [x] Insert into `tenants` table:

| Field | Value |
|-------|-------|
| `id` | `"default"` |
| `name` | `Settings.DefaultTenant.Name` (default: `"Default Tenant"`) |
| `active` | `true` |
| `labels_json` | `null` |
| `tags_json` | `null` |
| `created_utc` | `DateTime.UtcNow` |
| `last_update_utc` | `DateTime.UtcNow` |

#### 9.3.3 Step 2: Create Default Admin User

- [x] Insert into `users` table:

| Field | Value |
|-------|-------|
| `id` | `IdGenerator.NewUserId()` (e.g. `usr_...`) |
| `tenant_id` | `"default"` |
| `email` | `Settings.DefaultTenant.AdminEmail` (default: `"admin@assistanthub"`) |
| `password_sha256` | SHA-256 of `Settings.DefaultTenant.AdminPassword` (default: `"password"`) |
| `first_name` | `"Admin"` |
| `last_name` | `"User"` |
| `is_admin` | `true` |
| `is_tenant_admin` | `true` |
| `active` | `true` |
| `created_utc` | `DateTime.UtcNow` |
| `last_update_utc` | `DateTime.UtcNow` |

#### 9.3.4 Step 3: Create Default Credential

- [x] Insert into `credentials` table:

| Field | Value |
|-------|-------|
| `id` | `IdGenerator.NewCredentialId()` (e.g. `cred_...`) |
| `tenant_id` | `"default"` |
| `user_id` | Admin user ID from step 2 |
| `name` | `"Default admin credential"` |
| `bearer_token` | `"default"` |
| `active` | `true` |
| `created_utc` | `DateTime.UtcNow` |
| `last_update_utc` | `DateTime.UtcNow` |

#### 9.3.5 Step 4: Create Default Ingestion Rule

- [x] Insert into `ingestion_rules` table:

| Field | Value |
|-------|-------|
| `id` | `IdGenerator.NewIngestionRuleId()` (e.g. `irule_...`) |
| `tenant_id` | `"default"` |
| `name` | `"Default"` |
| `description` | `"Default ingestion rule"` |
| `bucket` | `"default"` |
| `collection_name` | `"default"` |
| `collection_id` | `"default"` |
| `chunking_json` | Default `IngestionChunkingConfig` serialized |
| `embedding_json` | Default `IngestionEmbeddingConfig` serialized |
| `summarization_json` | `null` |
| `atomization_json` | `null` |
| `labels_json` | `null` |
| `tags_json` | `null` |
| `created_utc` | `DateTime.UtcNow` |
| `last_update_utc` | `DateTime.UtcNow` |

#### 9.3.6 Step 5: Provision RecallDB Tenant and Collection

- [x] Create RecallDB tenant via API:
  - `PUT {RecallDb.Endpoint}/v1.0/tenants`
  - Body: `{ "Id": "default", "Name": "Default Tenant" }`
  - Header: `Authorization: Bearer {RecallDb.AccessKey}`
  - Log warning if this fails (RecallDB may not be available yet)
- [x] Create default collection in RecallDB:
  - `PUT {RecallDb.Endpoint}/v1.0/tenants/default/collections`
  - Body: `{ "Id": "default", "Name": "default" }`
  - Header: `Authorization: Bearer {RecallDb.AccessKey}`
  - Log warning if this fails

#### 9.3.7 Step 6: Console Output

- [x] Print provisioned credentials to console for operator reference:

```
*** Default tenant credentials ***
Tenant ID   : default
Tenant Name : Default Tenant
Admin Email : admin@assistanthub
Password    : password
Bearer Token: default
```

#### 9.3.8 New Tenant Provisioning (Non-Default)

When a global admin creates a new tenant via `PUT /v1.0/tenants`, the `TenantProvisioningService` must automatically create all the same default records, but with the new tenant's ID:

- [x] Create tenant row in database
- [x] Provision RecallDB tenant: `PUT {RecallDb.Endpoint}/v1.0/tenants` with body `{ "Id": "{newTenantId}", "Name": "{tenantName}" }`
- [x] Create default RecallDB collection: `PUT {RecallDb.Endpoint}/v1.0/tenants/{newTenantId}/collections` with body `{ "Name": "default" }`
- [x] Create default admin user for tenant:

| Field | Value |
|-------|-------|
| `tenant_id` | New tenant ID |
| `email` | `"admin@{tenantName.ToLower().Replace(" ", "")}"` |
| `password_sha256` | SHA-256 of `"password"` |
| `is_admin` | `true` |
| `is_tenant_admin` | `true` |

- [x] Create default credential for tenant:

| Field | Value |
|-------|-------|
| `tenant_id` | New tenant ID |
| `user_id` | New admin user ID |
| `name` | `"Default admin credential"` |
| `bearer_token` | `IdGenerator.NewBearerToken()` (auto-generated, 64 chars) |

- [x] Create default ingestion rule for tenant:

| Field | Value |
|-------|-------|
| `tenant_id` | New tenant ID |
| `name` | `"Default"` |
| `bucket` | `"default"` (shared S3 bucket; objects isolated by tenant prefix) |
| `collection_name` | `"default"` |
| `collection_id` | RecallDB collection ID from above |

- [x] Return provisioned credentials in the API response so the global admin can distribute them
- [x] Log provisioned credentials to server console

#### 9.3.9 Constants Updates

- [x] Add to `Constants.cs`:
  - `TenantIdentifierPrefix = "ten_"`
  - `TenantsTable = "tenants"`
  - `DefaultTenantId = "default"`
  - `DefaultTenantName = "Default Tenant"`
- [x] Keep existing constants (`DefaultAdminEmail`, `DefaultAdminPassword`, etc.) as fallbacks, but prefer values from `Settings.DefaultTenant` when available

#### 9.3.10 Migration: Existing v0.4.0 Deployments

When upgrading from v0.4.0 to v0.4.0, the migration SQL (009) runs first and creates the `tenants` table with a `"default"` row and backfills all existing records. After migration, `Tenant.GetCountAsync() > 0`, so `InitializeFirstRunAsync` is skipped — preserving all existing data under the `"default"` tenant. No duplicate records are created.

- [x] Verify migration SQL inserts the default tenant BEFORE the first-run check executes
- [x] Verify existing admin user gets `is_tenant_admin = 1` set by migration SQL
- [x] Verify existing credentials, assistants, documents, ingestion rules, chat history, and feedback all get `tenant_id = 'default'` via migration backfill
- [x] Document that after upgrade, the existing admin user retains full access as both global admin and tenant admin of the default tenant

---

## 10. Frontend: Dashboard

### 10.1 Authentication Context Overhaul

- [x] Update `Login.jsx` to support tenant-aware login:
  - [x] Bearer token login (auto-resolves tenant)
  - [x] Email + password + tenant ID login
- [x] Create/update `AuthContext.jsx` (React Context):
  - [x] Store: `isAuthenticated`, `isGlobalAdmin`, `isTenantAdmin`, `tenantId`, `tenantName`, `userId`, `email`
  - [x] Persist to `localStorage`: token, server URL, tenant info
  - [x] On load: call `/v1.0/whoami` to restore session
  - [x] `logout()` clears all stored state

### 10.2 New View: `TenantsView`

- [x] Create `dashboard/src/views/TenantsView.jsx`
- [x] Visible to global admins only
- [x] Features:
  - [x] List all tenants with pagination
  - [x] Create tenant (name, labels, tags) → auto-provisions resources
  - [x] Edit tenant (name, active status, labels, tags)
  - [x] Delete tenant (with confirmation warning about cascade)
  - [x] View tenant details (ID, created date, resource counts)
  - [x] Quick-nav to tenant's users, credentials, assistants

### 10.3 Update Navigation / Sidebar

- [x] Add "Tenants" menu item (global admin only)
- [x] Show current tenant name in top bar / header
- [x] Show role badge (Global Admin / Tenant Admin / User)
- [x] Group navigation:
  - **Administration** (admin only): Tenants, Users, Credentials
  - **Assistants**: Assistants, Documents, Ingestion Rules
  - **Infrastructure** (admin only): Buckets, Collections, Embedding Endpoints, Completion Endpoints, Models
  - **Analytics**: History, Feedback
  - **System** (global admin only): Configuration

### 10.4 Update `UsersView`

- [x] Scope to current tenant (from auth context)
- [x] Global admin: dropdown to select tenant or view all
- [x] Add `IsTenantAdmin` toggle in user create/edit form
- [x] Show tenant name column if global admin viewing all tenants

### 10.5 Update `CredentialsView`

- [x] Scope to current tenant
- [x] Global admin: dropdown to select tenant or view all
- [x] Show tenant name column if viewing all

### 10.6 Update `AssistantsView`

- [x] Scope to current tenant (all assistants in tenant, not just user's)
- [x] Tenant admin sees all tenant assistants
- [x] Regular user sees only their own assistants
- [x] Global admin can view across tenants

### 10.7 Update `DocumentsView`

- [x] Scope to current tenant
- [x] Filter documents by tenant

### 10.8 Update `IngestionRulesView`

- [x] Scope to current tenant
- [x] Ingestion rules are now per-tenant, not global

### 10.9 Update `HistoryView`

- [x] Scope to current tenant
- [x] Global admin can view across tenants

### 10.10 Update `FeedbackView`

- [x] Scope to current tenant

### 10.11 Update `BucketsView` and `ObjectsView`

- [x] Show only tenant-prefixed objects for non-global-admin
- [x] Global admin sees all objects (with tenant prefix visible)

### 10.12 Update `CollectionsView` and `RecordsView`

- [x] Scope RecallDB collection operations to tenant's RecallDB tenant ID
- [x] Pass dynamic tenant ID instead of hardcoded `"default"`

### 10.13 Update API Client (`api.js` or equivalent)

- [x] Add tenant CRUD methods:
  - [x] `createTenant(data)`
  - [x] `getTenants()`
  - [x] `getTenant(id)`
  - [x] `updateTenant(id, data)`
  - [x] `deleteTenant(id)`
- [x] Update user/credential methods to use `/v1.0/tenants/{tenantId}/users` paths
- [x] Add `whoami()` method
- [x] Update auth flow to store and send tenant context

### 10.14 Update `SetupWizard.jsx`

- [x] Update wizard to work within tenant context
- [x] On first setup, wizard creates resources in the default tenant
- [x] Show tenant info in wizard summary

### 10.15 Update `ChatView` (Embedded Chat)

- [x] No changes needed (public chat resolves tenant from assistant)
- [x] Verify chat widget works across tenants

---

## 11. External Service Integration

### 11.1 RecallDB Integration

Currently: single hardcoded `TenantId = "default"` in `RecallDbSettings`.

Target: each AssistantHub tenant maps to its own RecallDB tenant.

- [x] Remove `TenantId` from `RecallDbSettings`
- [x] On tenant creation (`TenantProvisioningService`):
  - [x] Call RecallDB `PUT /v1.0/tenants` to create a corresponding tenant
  - [x] Use the AssistantHub tenant ID as RecallDB tenant ID (or store a mapping)
  - [x] Create a default RecallDB user and credential for the AssistantHub tenant
- [x] Update `RetrievalService`:
  - [x] Accept `tenantId` parameter
  - [x] Build RecallDB URLs using `tenantId`: `/v1.0/tenants/{tenantId}/collections/{collectionId}/search`
- [x] Update `IngestionService`:
  - [x] Use tenant-specific RecallDB tenant for storing vectors
- [x] Update `CollectionHandler`:
  - [x] CRUD operations scoped to tenant's RecallDB tenant
- [x] On tenant deletion:
  - [x] Delete RecallDB tenant: `DELETE /v1.0/tenants/{tenantId}?force=true`

### 11.2 S3 / Less3 Integration

Currently: all objects stored in a shared bucket with no tenant isolation.

Target: tenant isolation via per-tenant S3 buckets.

- [x] Per-tenant bucket naming: `{tenantId}_{bucketName}` (e.g. `ten_abc123_default`)
- [x] Auto-provision default S3 bucket during tenant creation
- [x] Auto-delete all tenant S3 buckets (with objects) during tenant deletion
- [x] Enforce `{tenantId}_` bucket prefix in `BucketHandler` for all bucket and object operations
- [x] Enforce `{tenantId}_` bucket prefix in `IngestionRuleHandler` on create/update
- [x] Migration: existing objects in shared `default` bucket remain; new tenants get isolated buckets

### 11.3 DocumentAtom Integration

- [x] No direct tenant changes needed (DocumentAtom is stateless — receives document, returns cells)
- [x] Ensure tenant context is passed through for logging/tracing

### 11.4 Partio (Chunking) Integration

- [x] If Partio is also multi-tenant, use tenant-specific Partio credentials
- [x] If Partio is shared infrastructure, no changes needed (stateless processing)
- [x] Current integration uses admin API key (`partioadmin`) — this is fine for shared use
- [x] Ensure embedding endpoint IDs passed to Partio are valid for the tenant

### 11.5 Ollama Integration

- [x] No tenant changes needed (Ollama models are shared infrastructure)
- [x] Models are globally available to all tenants

---

## 12. API Contract Changes

### 12.1 New Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| PUT | `/v1.0/tenants` | Global Admin | Create tenant (with auto-provisioning) |
| GET | `/v1.0/tenants` | Any Authenticated | List tenants: global admin sees all (full enumeration); tenant admin and regular user see only their own tenant (single-object enumeration result) |
| POST | `/v1.0/tenants/enumerate` | Any Authenticated | Enumerate tenants with pagination: same role-based scoping as GET above |
| GET | `/v1.0/tenants/{id}` | Any Authenticated | Get tenant: global admin can read any; non-global-admin can only read own tenant (`403` otherwise) |
| PUT | `/v1.0/tenants/{id}` | Global Admin | Update tenant |
| DELETE | `/v1.0/tenants/{id}` | Global Admin | Delete tenant (cascade) |
| HEAD | `/v1.0/tenants/{id}` | Any Authenticated | Check tenant exists: global admin can check any; non-global-admin can only check own tenant (`403` otherwise) |
| GET | `/v1.0/whoami` | Any Authenticated | Get current auth context (role, tenant, user info) |

### 12.2 Moved Endpoints (Breaking Changes)

| Old Path | New Path | Notes |
|----------|----------|-------|
| `PUT /v1.0/users` | `PUT /v1.0/tenants/{tenantId}/users` | Tenant-scoped |
| `GET /v1.0/users` | `GET /v1.0/tenants/{tenantId}/users` | Tenant-scoped |
| `GET /v1.0/users/{id}` | `GET /v1.0/tenants/{tenantId}/users/{id}` | Tenant-scoped |
| `PUT /v1.0/users/{id}` | `PUT /v1.0/tenants/{tenantId}/users/{id}` | Tenant-scoped |
| `DELETE /v1.0/users/{id}` | `DELETE /v1.0/tenants/{tenantId}/users/{id}` | Tenant-scoped |
| `HEAD /v1.0/users/{id}` | `HEAD /v1.0/tenants/{tenantId}/users/{id}` | Tenant-scoped |
| `PUT /v1.0/credentials` | `PUT /v1.0/tenants/{tenantId}/credentials` | Tenant-scoped |
| `GET /v1.0/credentials` | `GET /v1.0/tenants/{tenantId}/credentials` | Tenant-scoped |
| `GET /v1.0/credentials/{id}` | `GET /v1.0/tenants/{tenantId}/credentials/{id}` | Tenant-scoped |
| `PUT /v1.0/credentials/{id}` | `PUT /v1.0/tenants/{tenantId}/credentials/{id}` | Tenant-scoped |
| `DELETE /v1.0/credentials/{id}` | `DELETE /v1.0/tenants/{tenantId}/credentials/{id}` | Tenant-scoped |
| `HEAD /v1.0/credentials/{id}` | `HEAD /v1.0/tenants/{tenantId}/credentials/{id}` | Tenant-scoped |

### 12.3 Modified Endpoints (Response Changes)

All list/enumerate endpoints now return only tenant-scoped results:

- [x] `GET /v1.0/assistants` — returns only assistants in caller's tenant
- [x] `GET /v1.0/documents` — returns only documents in caller's tenant
- [x] `GET /v1.0/ingestion-rules` — returns only rules in caller's tenant
- [x] `GET /v1.0/history` — returns only history in caller's tenant
- [x] `GET /v1.0/feedback` — returns only feedback in caller's tenant
- [x] `GET /v1.0/threads` — returns only threads in caller's tenant
- [x] `GET /v1.0/collections` — returns only collections in caller's RecallDB tenant

### 12.4 Modified Authentication Response

`POST /v1.0/authenticate` response adds fields:

```json
{
  "Token": "cred_...",
  "User": { ... },
  "TenantId": "ten_...",
  "TenantName": "Acme Corp",
  "IsGlobalAdmin": false,
  "IsTenantAdmin": true
}
```

### 12.5 Unchanged Endpoints

Public chat endpoints remain unchanged:
- `POST /v1.0/assistants/{id}/chat`
- `POST /v1.0/assistants/{id}/feedback`
- `POST /v1.0/assistants/{id}/threads`
- `GET /v1.0/assistants/{id}/public`
- All other public endpoints

---

## 13. Tenant Administration Experience

### 13.1 Tenant Lifecycle

```
Create Tenant (Global Admin)
  │
  ├── Insert tenant row in database
  ├── Create RecallDB tenant (via API)
  ├── Create default admin user (admin@{tenantName})
  ├── Create default credential (auto-generated bearer token)
  ├── Create default ingestion rule
  ├── Log provisioned credentials to server console
  │
  ▼
Tenant Active
  │
  ├── Tenant admin logs in, manages users/assistants/documents
  ├── Users create assistants, upload documents, chat
  │
  ▼
Disable Tenant (Global Admin)
  │
  ├── Set tenant.active = false
  ├── All auth attempts for tenant return 401
  ├── Public chat for tenant's assistants still works (or not — decide)
  │
  ▼
Delete Tenant (Global Admin)
  │
  ├── Delete all chat_history rows for tenant
  ├── Delete all assistant_feedback rows for tenant
  ├── Delete all assistant_documents rows for tenant (+ S3 cleanup)
  ├── Delete all assistant_settings rows (via assistant cascade)
  ├── Delete all assistants rows for tenant
  ├── Delete all ingestion_rules rows for tenant
  ├── Delete all credentials rows for tenant
  ├── Delete all users rows for tenant
  ├── Delete RecallDB tenant (force=true, drops collections)
  ├── Delete S3 objects with tenant prefix
  ├── Delete tenant row
  └── Delete processing logs for tenant
```

### 13.2 Tenant Admin Capabilities

A tenant admin (`is_tenant_admin = true`) can:

- [x] View tenant details (their own tenant only)
- [x] Manage users within their tenant (create, update, deactivate)
- [x] Manage credentials within their tenant
- [x] View all assistants in their tenant (not just their own)
- [x] Manage ingestion rules for their tenant
- [x] View all chat history and feedback in their tenant
- [x] Manage collections and documents in their tenant

A tenant admin CANNOT:

- [x] Create or delete tenants
- [x] Access other tenants' data
- [x] Access system configuration
- [x] Manage embedding/completion endpoints (if kept global)

### 13.3 Global Admin Capabilities

A global admin can do everything a tenant admin can, PLUS:

- [x] Create, update, and delete tenants
- [x] Access any tenant's data
- [x] Manage system configuration
- [x] Manage global infrastructure (endpoints, models, buckets)
- [x] View cross-tenant analytics

---

## 14. Migration Script (v0.4.0 → v0.4.0)

### 14.1 Database Migration

File: `migrations/009_add_multi_tenancy.sql`

Steps executed automatically on v0.4.0 startup:

- [x] 1. Create `tenants` table
- [x] 2. Insert default tenant: `('default', 'Default Tenant', 1, now, now)`
- [x] 3. Add `tenant_id` column to `users` with default `'default'`
- [x] 4. Add `is_tenant_admin` column to `users` with default `0`
- [x] 5. Add `tenant_id` column to `credentials` with default `'default'`
- [x] 6. Add `tenant_id` column to `assistants` with default `'default'`
- [x] 7. Add `tenant_id` column to `assistant_documents` with default `'default'`
- [x] 8. Add `tenant_id` column to `assistant_feedback` with default `'default'`
- [x] 9. Add `tenant_id` column to `ingestion_rules` with default `'default'`
- [x] 10. Add `tenant_id` column to `chat_history` with default `'default'`
- [x] 11. Backfill all existing rows: `UPDATE {table} SET tenant_id = 'default' WHERE tenant_id IS NULL`
- [x] 12. Create all new indexes (per Section 3.2)
- [x] 13. Set existing admin user's `is_tenant_admin = 1`

### 14.2 RecallDB Migration

- [x] Verify RecallDB's `default` tenant exists (it should from RecallDB's own default setup)
- [x] If AssistantHub tenant IDs differ from RecallDB tenant IDs, create mapping
- [x] Recommended: use the same `"default"` tenant ID in both systems

### 14.3 S3 Migration

- [x] **Option A (Recommended):** Leave existing S3 objects in place; update code to check for objects both with and without tenant prefix for backward compatibility during transition
- [x] **Option B:** Run a one-time script to move all existing objects under `default/` prefix
- [x] Document the chosen approach in the upgrade guide

### 14.4 Configuration Migration

- [x] Add `AdminApiKeys` to `assistanthub.json` (new required field)
- [x] Remove `RecallDb.TenantId` from `assistanthub.json`
- [x] Add `DefaultTenant` section to `assistanthub.json`
- [x] Document all config changes in upgrade guide

### 14.5 Processing Log Migration

- [x] Existing logs remain in current directory structure
- [x] New logs will be under `{logDir}/{tenantId}/{documentId}/`
- [x] No migration of existing logs needed (they are ephemeral)

---

## 15. Docker & Deployment

### 15.1 Update `compose.yaml`

- [x] Add `ASSISTANTHUB_ADMIN_API_KEYS` environment variable
- [x] Remove `RECALLDB_TENANT_ID` if passed as env var
- [x] Update `assistanthub.json` volume mount with new config structure
- [x] No new containers needed (multi-tenancy is application-level)

### 15.2 Update `assistanthub.json` in Docker Config

- [x] Update `docker/assistanthub/assistanthub.json` with:
  - [x] `AdminApiKeys: ["assistanthubadmin"]`
  - [x] Remove `RecallDb.TenantId`
  - [x] Add `DefaultTenant` configuration

### 15.3 Update Dashboard Nginx Config (if any)

- [x] No changes needed (dashboard is static SPA, routes handled client-side)

### 15.4 Health Check Updates

- [x] Update health check to report version `v0.4.0`
- [x] Optionally add tenant count to health response

---

## 16. Postman Collection

### 16.1 Update Existing Collection

- [x] Update `postman/AssistantHub.postman_collection.json`

### 16.2 Add Tenant Management Folder

- [x] Create Tenant
- [x] List Tenants
- [x] Get Tenant
- [x] Update Tenant
- [x] Delete Tenant
- [x] Check Tenant Exists

### 16.3 Update User Management Folder

- [x] Update all user endpoints to new `/v1.0/tenants/{tenantId}/users` paths
- [x] Add `{{tenantId}}` collection variable

### 16.4 Update Credential Management Folder

- [x] Update all credential endpoints to new `/v1.0/tenants/{tenantId}/credentials` paths

### 16.5 Add Authentication Requests

- [x] Add "Authenticate (Bearer Token)" request
- [x] Add "Authenticate (Email/Password with Tenant)" request
- [x] Add "Who Am I" request (`GET /v1.0/whoami`)

### 16.6 Update Collection Variables

- [x] Add `{{tenantId}}` — default: `default`
- [x] Add `{{adminApiKey}}` — default: `assistanthubadmin`
- [x] Update `{{bearerToken}}` examples

### 16.7 Add Environment Templates

- [x] "Local Development" environment with default values
- [x] "Production" environment template with placeholder values

---

## 17. Documentation

### 17.1 Update `README.md`

- [x] Update architecture overview to mention multi-tenancy
- [x] Update quick-start guide with tenant context
- [x] Update API examples with tenant-scoped requests

### 17.2 Create `UPGRADING.md` (v0.4.0 → v0.4.0)

- [x] Step-by-step upgrade instructions:
  1. Stop existing deployment
  2. Back up database
  3. Update Docker images to v0.4.0
  4. Update `assistanthub.json` configuration (detail all changes)
  5. Start deployment (migration runs automatically)
  6. Verify default tenant created
  7. Update any API integrations to use new endpoints
  8. Test authentication with new flow
- [x] Breaking changes summary
- [x] New configuration keys
- [x] Removed configuration keys
- [x] Endpoint migration table (old → new)
- [x] Known issues and workarounds

### 17.3 Create `TENANT_GUIDE.md`

- [x] Tenant concepts and terminology
- [x] Creating your first tenant
- [x] Managing tenant users and credentials
- [x] Tenant admin vs global admin
- [x] Tenant isolation guarantees
- [x] Provisioning flow (what gets auto-created)
- [x] Decommissioning a tenant
- [x] FAQ

### 17.4 Update `CLAUDE.md`

- [x] Update with multi-tenant architecture notes
- [x] Document tenant scoping conventions
- [x] Document AuthContext pattern
- [x] Document database query tenant filtering requirement

### 17.5 API Documentation

- [x] Update any existing API docs with new/changed endpoints
- [x] Document new authentication response fields
- [x] Document tenant management API
- [x] Document authorization model (global admin / tenant admin / user)

---

## 18. Testing Plan

### 18.1 Test.Database Project Updates

The `Test.Database` project (`src/Test.Database/`) is a custom console-based test framework with 8 test classes and ~132 tests. It runs against real databases (SQLite, PostgreSQL, SQL Server, MySQL) via command-line arguments. All tests must be updated for multi-tenancy.

#### 18.1.1 New Test Class: `TenantTests`

- [x] Create `src/Test.Database/Tests/TenantTests.cs`
- [x] Follow existing per-entity pattern (static class, `RunAllAsync(DatabaseDriverBase driver, CancellationToken ct)`)
- [x] Tests to implement:
  - [x] `Create` — create tenant with all fields (Name, Active, Labels, Tags)
  - [x] `Create_Defaults` — create tenant with only Name, verify Active defaults to true
  - [x] `Read` — read tenant by ID
  - [x] `ReadByName` — lookup tenant by name
  - [x] `ReadByName_NotFound` — non-existent name returns null
  - [x] `Read_NotFound` — non-existent ID returns null
  - [x] `Exists_True` — exists for created tenant
  - [x] `Exists_False` — exists for non-existent tenant
  - [x] `Update` — update Name, Active, Labels, Tags
  - [x] `Update_VerifyPersistence` — re-read after update to verify changes
  - [x] `GetCount` — total tenant count
  - [x] `Enumerate_Default` — basic enumeration
  - [x] `Enumerate_Pagination_Page1` — first page with continuation token
  - [x] `Enumerate_Pagination_Page2` — second page from continuation token
  - [x] `Enumerate_AllPages` — full pagination cycle
  - [x] `Enumerate_Ordering_Ascending` — sort by CreatedUtc ASC
  - [x] `Enumerate_Ordering_Descending` — sort by CreatedUtc DESC
  - [x] `Delete` — delete tenant and verify gone
  - [x] `Delete_NonExistent` — delete non-existent doesn't throw
- [x] Register in `Program.cs`: call `TenantTests.RunAllAsync(driver, ct)` **before** all other test classes (tenants are parent entities)

#### 18.1.2 Update Test Data Setup: All Tests Need a Tenant

Every existing test class creates entities inline. Since all entities now require `tenant_id`, each test class must create (or reuse) a tenant before creating its test entities.

**Pattern to apply across all 8 existing test classes:**

- [x] At the top of each `RunAllAsync` method, create a test tenant:
  ```csharp
  TenantMetadata testTenant = new TenantMetadata { Name = "Test Tenant for {EntityName}" };
  testTenant = await driver.Tenant.CreateAsync(testTenant, ct);
  string tenantId = testTenant.Id;
  ```
- [x] Pass `tenantId` to all entity creation calls
- [x] At the end of each `RunAllAsync`, clean up the test tenant (which should cascade or be cleaned up in `Program.cs` cleanup)

#### 18.1.3 Update `UserTests`

- [x] All user creation calls must set `TenantId = tenantId`
- [x] Add `IsTenantAdmin` field to create/update tests
- [x] Update `ReadByEmail` to use `ReadByEmailAsync(tenantId, email)` (now composite lookup)
- [x] Update all `Enumerate` calls to pass `tenantId` parameter
- [x] Add new tests:
  - [x] `Create_TenantAdmin` — user with `IsTenantAdmin = true`
  - [x] `ReadByEmail_WrongTenant` — email exists in tenant A, lookup in tenant B returns null
  - [x] `Enumerate_TenantScoped` — create users in two tenants, enumerate one, verify only that tenant's users returned
  - [x] `Enumerate_CrossTenantIsolation` — verify enumeration for tenant A does not return tenant B users

#### 18.1.4 Update `CredentialTests`

- [x] All credential creation calls must set `TenantId = tenantId`
- [x] Update all `Enumerate` calls to pass `tenantId` parameter
- [x] Add new tests:
  - [x] `ReadByBearerToken_ResolveTenant` — verify `ReadByBearerTokenAsync` returns credential with correct `TenantId`
  - [x] `Enumerate_TenantScoped` — create credentials in two tenants, verify isolation
  - [x] `DeleteByUserId_TenantScoped` — delete by user ID only affects same tenant

#### 18.1.5 Update `AssistantTests`

- [x] All assistant creation calls must set `TenantId = tenantId`
- [x] Update all `Enumerate` calls to pass `tenantId` parameter
- [x] Add new tests:
  - [x] `Enumerate_TenantScoped` — create assistants in two tenants, verify isolation
  - [x] `Enumerate_ByUserIdAndTenantId` — filter by both user and tenant

#### 18.1.6 Update `AssistantSettingsTests`

- [x] Parent assistant must have `TenantId` set
- [x] No direct `tenant_id` on settings, but verify scoping through parent assistant
- [x] Add new test:
  - [x] `ReadByAssistantId_CrossTenant` — verify settings for assistant in tenant A not accessible when querying tenant B's assistant

#### 18.1.7 Update `AssistantDocumentTests`

- [x] All document creation calls must set `TenantId = tenantId`
- [x] Update all `Enumerate` calls to pass `tenantId` parameter
- [x] Add new tests:
  - [x] `Enumerate_TenantScoped` — create documents in two tenants, verify isolation
  - [x] `Enumerate_BucketFilter_TenantScoped` — bucket filter works within tenant scope

#### 18.1.8 Update `AssistantFeedbackTests`

- [x] All feedback creation calls must set `TenantId = tenantId`
- [x] Update all `Enumerate` calls to pass `tenantId` parameter
- [x] Add new tests:
  - [x] `Enumerate_TenantScoped` — create feedback in two tenants, verify isolation
  - [x] `DeleteByAssistantId_TenantScoped` — delete by assistant ID only affects same tenant

#### 18.1.9 Update `IngestionRuleTests`

- [x] All ingestion rule creation calls must set `TenantId = tenantId`
- [x] Update all `Enumerate` calls to pass `tenantId` parameter
- [x] Add new tests:
  - [x] `Enumerate_TenantScoped` — create rules in two tenants, verify isolation
  - [x] `ReadByName_TenantScoped` — same name in two tenants, each resolves to correct rule
  - [x] `Create_DuplicateNameDifferentTenants` — same name allowed across tenants (composite unique constraint)
  - [x] `Create_DuplicateNameSameTenant` — same name in same tenant fails (composite unique constraint)

#### 18.1.10 Update `ChatHistoryTests`

- [x] All chat history creation calls must set `TenantId = tenantId`
- [x] Update all `Enumerate` calls to pass `tenantId` parameter
- [x] Add new tests:
  - [x] `Enumerate_TenantScoped` — create history in two tenants, verify isolation
  - [x] `DeleteExpired_TenantScoped` — expired deletion respects tenant scope
  - [x] `Enumerate_ThreadIdFilter_TenantScoped` — thread filter works within tenant scope

#### 18.1.11 Update `Program.cs`

- [x] Add `TenantTests.RunAllAsync(driver, ct)` call — run **first** (tenants are parent entities)
- [x] Update cleanup table list to include `tenants` table at the end (after all child tables):
  ```csharp
  string[] cleanupTables = new[] {
      "chat_history", "assistant_feedback", "assistant_settings",
      "assistant_documents", "ingestion_rules", "credentials",
      "assistants", "users", "tenants"  // <-- add tenants last
  };
  ```
- [x] Verify cleanup order respects foreign key dependencies (children before parents)

#### 18.1.12 Update `TestRunner.cs` / `AssertHelper.cs`

- [x] No structural changes needed to the test framework itself
- [x] Optionally add `AssertHelper.HasTenantId(entity, expectedTenantId)` convenience method
- [x] Verify `TestResult` reporting still works with increased test count (~160+ tests after additions)

### 18.2 Database Migration Tests

- [x] Test migration on each database type (SQLite, PostgreSQL, SQL Server, MySQL)
- [x] Verify existing data preserved with `tenant_id = 'default'`
- [x] Verify default tenant created
- [x] Verify indexes created correctly
- [x] Test idempotent migration (running twice doesn't fail)

### 18.3 Authentication Tests

- [x] Admin API key authentication returns `IsGlobalAdmin = true`
- [x] Bearer token authentication resolves correct tenant
- [x] Email/password + tenant ID authentication works
- [x] Inactive tenant returns 401
- [x] Inactive user returns 401
- [x] Inactive credential returns 401
- [x] Invalid token returns 401

### 18.4 Tenant Isolation Tests

- [x] User in Tenant A cannot see Tenant B's assistants
- [x] User in Tenant A cannot see Tenant B's documents
- [x] User in Tenant A cannot see Tenant B's ingestion rules
- [x] User in Tenant A cannot see Tenant B's chat history
- [x] User in Tenant A cannot see Tenant B's feedback
- [x] User in Tenant A cannot access Tenant B's S3 objects
- [x] User in Tenant A cannot search Tenant B's RecallDB collections
- [x] Tenant admin in Tenant A cannot manage Tenant B's users
- [x] Public chat for Tenant A's assistant doesn't leak Tenant B's data

### 18.5 Tenant Lifecycle Tests

- [x] Create tenant provisions all default resources
- [x] RecallDB tenant created on tenant creation
- [x] Default user/credential created and functional
- [x] Disable tenant blocks all auth for that tenant
- [x] Delete tenant cascades to all child resources
- [x] Delete tenant cleans up RecallDB tenant
- [x] Delete tenant cleans up S3 objects

### 18.6 Global Admin Tests

- [x] Global admin can list all tenants
- [x] Global admin can access any tenant's resources
- [x] Global admin can create/delete tenants
- [x] Global admin can manage system configuration

### 18.7 Tenant Admin Tests

- [x] Tenant admin can manage users in their tenant
- [x] Tenant admin can see all assistants in their tenant
- [x] Tenant admin cannot access other tenants
- [x] Tenant admin cannot create/delete tenants
- [x] Tenant admin cannot access system configuration

### 18.8 Regular User Tests

- [x] User can only see their own assistants
- [x] User can create assistants in their tenant
- [x] User cannot manage other users
- [x] User cannot access admin endpoints

### 18.9 Backward Compatibility Tests

- [x] Existing `default` bearer token still works after migration
- [x] Existing assistants accessible after migration
- [x] Existing documents downloadable after migration
- [x] Existing chat history visible after migration
- [x] Public chat endpoints work without changes

### 18.10 End-to-End Tests

- [x] Full flow: create tenant → create user → login → create assistant → upload document → ingest → chat → receive RAG response
- [x] Multi-tenant flow: two tenants, verify complete isolation
- [x] Migration flow: deploy v0.4.0, populate data, upgrade to v0.4.0, verify everything works

---

## 19. Implementation Order

Recommended sequence for implementation, with dependencies noted:

### Phase 1: Core Infrastructure (Foundation)

| # | Task | Depends On | Files |
|---|------|-----------|-------|
| 1.1 | Create `TenantMetadata` model | — | `Models/TenantMetadata.cs` |
| 1.2 | Create `AuthContext` model | — | `Models/AuthContext.cs` |
| 1.3 | Add `NewTenantId()` to `IdGenerator` | — | `Helpers/IdGenerator.cs` |
| 1.4 | Add `TenantId`, `IsTenantAdmin` to `UserMaster` | — | `Models/UserMaster.cs` |
| 1.5 | Add `TenantId` to `Credential` | — | `Models/Credential.cs` |
| 1.6 | Add `TenantId` to `Assistant` | — | `Models/Assistant.cs` |
| 1.7 | Add `TenantId` to `AssistantDocument` | — | `Models/AssistantDocument.cs` |
| 1.8 | Add `TenantId` to `AssistantFeedback` | — | `Models/AssistantFeedback.cs` |
| 1.9 | Add `TenantId` to `IngestionRule` | — | `Models/IngestionRule.cs` |
| 1.10 | Add `TenantId` to `ChatHistory` | — | `Models/ChatHistory.cs` |

### Phase 2: Database Layer

| # | Task | Depends On | Files |
|---|------|-----------|-------|
| 2.1 | Create `ITenantMethods` interface | 1.1 | `Database/Interfaces/ITenantMethods.cs` |
| 2.2 | Create tenants table schema (all 4 drivers) | 2.1 | `Database/*/Queries/TableQueries.cs` |
| 2.3 | Implement `TenantMethods` (all 4 drivers) | 2.1, 2.2 | `Database/*/Implementations/TenantMethods.cs` |
| 2.4 | Add `Tenant` property to `DatabaseDriverBase` | 2.3 | `Database/DatabaseDriverBase.cs` |
| 2.5 | Create migration SQL `009_add_multi_tenancy.sql` | 2.2 | `migrations/009_add_multi_tenancy.sql` |
| 2.6 | Update all existing table schemas (add tenant_id columns) | 2.5 | `Database/*/Queries/TableQueries.cs` |
| 2.7 | Update all existing method interfaces (add tenantId params) | 1.4–1.10 | `Database/Interfaces/I*Methods.cs` |
| 2.8 | Update all existing implementations (all 4 drivers × all interfaces) | 2.6, 2.7 | `Database/*/Implementations/*.cs` |
| 2.9 | Update `DataTableHelper` mappings | 2.6 | `Helpers/DataTableHelper.cs` |

### Phase 3: Authentication & Authorization

| # | Task | Depends On | Files |
|---|------|-----------|-------|
| 3.1 | Update settings classes (AdminApiKeys, remove TenantId) | — | `Settings/*.cs` |
| 3.2 | Refactor `AuthenticationService` to return `AuthContext` | 1.2, 2.8 | `Services/AuthenticationService.cs` |
| 3.3 | Add admin API key validation | 3.1, 3.2 | `Services/AuthenticationService.cs` |
| 3.4 | Add tenant validation to auth flow | 3.2 | `Services/AuthenticationService.cs` |
| 3.5 | Create authorization helper methods | 3.2 | `Handlers/AuthorizationHelpers.cs` or similar |
| 3.6 | Update authentication middleware in server | 3.2 | `AssistantHubServer.cs` |

### Phase 4: API Handlers

| # | Task | Depends On | Files |
|---|------|-----------|-------|
| 4.1 | Create `TenantHandler` | 2.3, 3.5 | `Handlers/TenantHandler.cs` |
| 4.2 | Update `AuthenticationHandler` | 3.2 | `Handlers/AuthenticationHandler.cs` |
| 4.3 | Update `UserHandler` (new routes) | 2.8, 3.5 | `Handlers/UserHandler.cs` |
| 4.4 | Update `CredentialHandler` (new routes) | 2.8, 3.5 | `Handlers/CredentialHandler.cs` |
| 4.5 | Update `AssistantHandler` | 2.8, 3.5 | `Handlers/AssistantHandler.cs` |
| 4.6 | Update `AssistantSettingsHandler` | 4.5 | `Handlers/AssistantSettingsHandler.cs` |
| 4.7 | Update `DocumentHandler` | 2.8, 3.5 | `Handlers/DocumentHandler.cs` |
| 4.8 | Update `IngestionRuleHandler` | 2.8, 3.5 | `Handlers/IngestionRuleHandler.cs` |
| 4.9 | Update `ChatHandler` | 2.8 | `Handlers/ChatHandler.cs` |
| 4.10 | Update `FeedbackHandler` | 2.8, 3.5 | `Handlers/FeedbackHandler.cs` |
| 4.11 | Update `HistoryHandler` | 2.8, 3.5 | `Handlers/HistoryHandler.cs` |
| 4.12 | Update `CollectionHandler` | 2.8, 3.5 | `Handlers/CollectionHandler.cs` |
| 4.13 | Update `BucketHandler` | 3.5 | `Handlers/BucketHandler.cs` |
| 4.14 | Update `EmbeddingEndpointHandler` | 3.5 | `Handlers/EmbeddingEndpointHandler.cs` |
| 4.15 | Update `CompletionEndpointHandler` | 3.5 | `Handlers/CompletionEndpointHandler.cs` |
| 4.16 | Add `/v1.0/whoami` endpoint | 3.2 | `Handlers/AuthenticationHandler.cs` |
| 4.17 | Register all routes in server | 4.1–4.16 | `AssistantHubServer.cs` |

### Phase 5: Services

| # | Task | Depends On | Files |
|---|------|-----------|-------|
| 5.1 | Update `StorageService` (S3 tenant prefixing) | 3.5 | `Services/StorageService.cs` |
| 5.2 | Update `IngestionService` (tenant context) | 5.1 | `Services/IngestionService.cs` |
| 5.3 | Update `RetrievalService` (tenant RecallDB routing) | 2.8 | `Services/RetrievalService.cs` |
| 5.4 | Update `ProcessingLogService` (tenant namespacing) | — | `Services/ProcessingLogService.cs` |
| 5.5 | Create `TenantProvisioningService` | 2.3, 5.1 | `Services/TenantProvisioningService.cs` |

### Phase 6: First-Run Initialization & Migration

| # | Task | Depends On | Files |
|---|------|-----------|-------|
| 6.1 | Implement startup migration logic (run `009_add_multi_tenancy.sql`) | 2.5 | `Database/DatabaseDriverBase.cs` |
| 6.2 | Add `TenantIdentifierPrefix`, `TenantsTable`, `DefaultTenantId`, `DefaultTenantName` to Constants | — | `Constants.cs` |
| 6.3 | Rewrite `InitializeFirstRunAsync`: change trigger from `userCount == 0` to `tenantCount == 0` | 2.4 | `AssistantHubServer.cs` |
| 6.4 | First-run: create default tenant record (`id = "default"`) | 6.3, 2.3 | `AssistantHubServer.cs` |
| 6.5 | First-run: create default admin user with `tenant_id = "default"`, `is_tenant_admin = true` | 6.4 | `AssistantHubServer.cs` |
| 6.6 | First-run: create default credential with `tenant_id = "default"` | 6.5 | `AssistantHubServer.cs` |
| 6.7 | First-run: create default ingestion rule with `tenant_id = "default"` | 6.4 | `AssistantHubServer.cs` |
| 6.8 | First-run: provision RecallDB tenant (`PUT /v1.0/tenants`, body `{ Id: "default" }`) | 6.4 | `AssistantHubServer.cs` |
| 6.9 | First-run: create default RecallDB collection under `default` tenant | 6.8 | `AssistantHubServer.cs` |
| 6.10 | First-run: print provisioned tenant credentials to console | 6.6 | `AssistantHubServer.cs` |
| 6.11 | Wire `TenantProvisioningService` into `TenantHandler.CreateTenant` for non-default tenants | 5.5, 4.1 | `Handlers/TenantHandler.cs` |
| 6.12 | Update `assistanthub.json` config (add `AdminApiKeys`, `DefaultTenant`; remove `RecallDb.TenantId`) | 3.1 | `docker/assistanthub/assistanthub.json` |
| 6.13 | Verify migration → first-run ordering: migration inserts default tenant before first-run check | 6.1, 6.3 | Integration test |
| 6.14 | Verify v0.4.0 → v0.4.0 upgrade: existing data backfilled, no duplicate records created | 6.1 | Integration test |

### Phase 7: Frontend

| # | Task | Depends On | Files |
|---|------|-----------|-------|
| 7.1 | Update `AuthContext.jsx` | 4.2, 4.16 | `dashboard/src/context/AuthContext.jsx` |
| 7.2 | Update `Login.jsx` | 7.1 | `dashboard/src/views/Login.jsx` |
| 7.3 | Update API client | 4.1–4.16 | `dashboard/src/utils/api.js` |
| 7.4 | Create `TenantsView.jsx` | 7.1, 7.3 | `dashboard/src/views/TenantsView.jsx` |
| 7.5 | Update navigation/sidebar | 7.1 | `dashboard/src/components/Sidebar.jsx` (or equivalent) |
| 7.6 | Update `UsersView.jsx` | 7.3 | `dashboard/src/views/UsersView.jsx` |
| 7.7 | Update `CredentialsView.jsx` | 7.3 | `dashboard/src/views/CredentialsView.jsx` |
| 7.8 | Update `AssistantsView.jsx` | 7.3 | `dashboard/src/views/AssistantsView.jsx` |
| 7.9 | Update `DocumentsView.jsx` | 7.3 | `dashboard/src/views/DocumentsView.jsx` |
| 7.10 | Update `IngestionRulesView.jsx` | 7.3 | `dashboard/src/views/IngestionRulesView.jsx` |
| 7.11 | Update `HistoryView.jsx` | 7.3 | `dashboard/src/views/HistoryView.jsx` |
| 7.12 | Update `FeedbackView.jsx` | 7.3 | `dashboard/src/views/FeedbackView.jsx` |
| 7.13 | Update `BucketsView.jsx` / `ObjectsView.jsx` | 7.3 | `dashboard/src/views/BucketsView.jsx` |
| 7.14 | Update `CollectionsView.jsx` / `RecordsView.jsx` | 7.3 | `dashboard/src/views/CollectionsView.jsx` |
| 7.15 | Update `SetupWizard.jsx` | 7.1 | `dashboard/src/components/SetupWizard.jsx` |

### Phase 8: Docker & Deployment

| # | Task | Depends On | Files |
|---|------|-----------|-------|
| 8.1 | Update `compose.yaml` | 6.3 | `docker/compose.yaml` |
| 8.2 | Update Docker config files | 6.3 | `docker/assistanthub/assistanthub.json` |
| 8.3 | Test full Docker deployment | 8.1, 8.2 | — |

### Phase 9: Postman & Documentation

| # | Task | Depends On | Files |
|---|------|-----------|-------|
| 9.1 | Update Postman collection | 4.1–4.17 | `postman/AssistantHub.postman_collection.json` |
| 9.2 | Create `UPGRADING.md` | All | `UPGRADING.md` |
| 9.3 | Create `TENANT_GUIDE.md` | All | `TENANT_GUIDE.md` |
| 9.4 | Update `README.md` | All | `README.md` |
| 9.5 | Update `CLAUDE.md` | All | `CLAUDE.md` |

### Phase 10: Test.Database Updates

| # | Task | Depends On | Files |
|---|------|-----------|-------|
| 10.1 | Create `TenantTests.cs` (new test class, ~19 tests) | 2.3 | `Test.Database/Tests/TenantTests.cs` |
| 10.2 | Update `Program.cs`: add `TenantTests`, update cleanup table list to include `tenants` last | 10.1 | `Test.Database/Program.cs` |
| 10.3 | Update `UserTests.cs`: add `tenant_id` to all creates, add `IsTenantAdmin` tests, add tenant-scoped `ReadByEmail`, add cross-tenant isolation tests | 2.8 | `Test.Database/Tests/UserTests.cs` |
| 10.4 | Update `CredentialTests.cs`: add `tenant_id` to all creates, add tenant-scoped enumeration, add `ReadByBearerToken` tenant resolution test | 2.8 | `Test.Database/Tests/CredentialTests.cs` |
| 10.5 | Update `AssistantTests.cs`: add `tenant_id` to all creates, add tenant-scoped enumeration and isolation tests | 2.8 | `Test.Database/Tests/AssistantTests.cs` |
| 10.6 | Update `AssistantSettingsTests.cs`: ensure parent assistant has `tenant_id`, add cross-tenant test | 2.8 | `Test.Database/Tests/AssistantSettingsTests.cs` |
| 10.7 | Update `AssistantDocumentTests.cs`: add `tenant_id` to all creates, add tenant-scoped enumeration | 2.8 | `Test.Database/Tests/AssistantDocumentTests.cs` |
| 10.8 | Update `AssistantFeedbackTests.cs`: add `tenant_id` to all creates, add tenant-scoped enumeration | 2.8 | `Test.Database/Tests/AssistantFeedbackTests.cs` |
| 10.9 | Update `IngestionRuleTests.cs`: add `tenant_id` to all creates, add composite unique constraint tests, add tenant-scoped enumeration | 2.8 | `Test.Database/Tests/IngestionRuleTests.cs` |
| 10.10 | Update `ChatHistoryTests.cs`: add `tenant_id` to all creates, add tenant-scoped enumeration and expiration tests | 2.8 | `Test.Database/Tests/ChatHistoryTests.cs` |
| 10.11 | Run full test suite against all 4 database types (SQLite, PostgreSQL, SQL Server, MySQL) | 10.1–10.10 | — |

### Phase 11: Integration & E2E Testing

| # | Task | Depends On | Files |
|---|------|-----------|-------|
| 11.1 | Database migration tests (v0.4.0 → v0.4.0 upgrade) | Phase 6 | Manual / automated |
| 11.2 | Authentication tests (admin API key, bearer, email/password) | Phase 3 | Manual / automated |
| 11.3 | Tenant isolation tests (cross-tenant data leak verification) | Phase 4 | Manual / automated |
| 11.4 | Tenant lifecycle tests (create, disable, delete + cascade) | Phase 5 | Manual / automated |
| 11.5 | Backward compatibility tests (existing default data still works) | Phase 6 | Manual / automated |
| 11.6 | End-to-end tests (full multi-tenant flow) | Phase 7 | Manual / automated |

---

## Appendix A: File Change Summary

| Category | Files to Create | Files to Modify |
|----------|----------------|-----------------|
| Models | 2 (TenantMetadata, AuthContext) | 7 (UserMaster, Credential, Assistant, AssistantDocument, AssistantFeedback, IngestionRule, ChatHistory) |
| Database Interfaces | 1 (ITenantMethods) | ~8 (all existing I*Methods) |
| Database Implementations | 4 (TenantMethods × 4 drivers) | ~32 (8 methods × 4 drivers) |
| Database Queries | 0 | 4 (TableQueries × 4 drivers) |
| Migrations | 1 (009_add_multi_tenancy.sql) | 0 |
| Services | 1 (TenantProvisioningService) | 4 (Auth, Storage, Ingestion, Retrieval, ProcessingLog) |
| Handlers | 1 (TenantHandler) | ~14 (all existing handlers) |
| Server | 0 | 1 (AssistantHubServer.cs) |
| Settings | 1 (DefaultTenantSettings) | ~2 (ServerSettings, RecallDbSettings) |
| Helpers | 0 | 2 (IdGenerator, DataTableHelper) |
| Constants | 0 | 1 (Constants.cs) |
| Test.Database | 1 (TenantTests.cs) | 9 (Program.cs + all 8 existing test classes) |
| Frontend | 1 (TenantsView.jsx) | ~14 (Login, AuthContext, api.js, Sidebar, all views) |
| Config | 0 | 2 (assistanthub.json, compose.yaml) |
| Postman | 0 | 1 (collection JSON) |
| Documentation | 2 (UPGRADING.md, TENANT_GUIDE.md) | 2 (README.md, CLAUDE.md) |
| **Totals** | **~15 new files** | **~100+ modified files** |

## Appendix B: Decision Log

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Isolation model | Row-level (shared schema) | Consistent with RecallDB/Partio/Verbex; simpler operations |
| Tenant ID format | K-sortable with `ten_` prefix | Consistent with existing ID patterns and sibling projects |
| Endpoint scoping | Global (shared) | Endpoints are infrastructure; per-tenant adds overhead without benefit |
| S3 isolation | Key prefix (`{tenantId}/...`) | Simple, no bucket-per-tenant overhead, works with single Less3 instance |
| RecallDB mapping | 1:1 tenant mapping (same ID) | Simplest; avoids maintaining a separate mapping table |
| User/Credential routes | Moved under `/v1.0/tenants/{id}/` | Matches RecallDB/Verbex patterns; clear tenant scoping in URL |
| Other resource routes | Keep flat, scope via auth context | Less API churn; tenant derived implicitly from authenticated user |
| Public chat | Derive tenant from assistant | No auth required; assistant lookup provides tenant context |
| Default tenant on migration | ID = `"default"` | Matches RecallDB's default; seamless for existing single-tenant deployments |
| Breaking changes | Accepted | v0.4.0 is a major feature release; upgrade guide mitigates impact |
