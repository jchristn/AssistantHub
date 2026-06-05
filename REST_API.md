# AssistantHub REST API Reference

Base URL: `http://localhost:8800`

All API endpoints are versioned under `/v1.0/`. Responses use `application/json` content type unless otherwise noted.

AssistantHub also ships a standalone MCP server that maps the management surface in this document to MCP tools. See [MCP_API.md](MCP_API.md) for the MCP transport endpoints, tool names, redaction rules, and route coverage matrix.

## Table of Contents

- [Authentication](#authentication)
- [Error Responses](#error-responses)
- [Pagination](#pagination)
- [Health](#health)
- [OpenAPI](#openapi)
- [Tenants (Global Admin Only)](#tenants-global-admin-only)
- [Who Am I](#who-am-i)
- [Users (Admin Only)](#users-admin-only)
- [Credentials (Admin Only)](#credentials-admin-only)
- [Buckets (Tenant-Scoped)](#buckets-tenant-scoped)
- [Bucket Objects (Tenant-Scoped)](#bucket-objects-tenant-scoped)
- [Collections (Admin Only)](#collections-admin-only)
- [Collection Records (Admin Only)](#collection-records-admin-only)
- [Ingestion Rules](#ingestion-rules)
- [Embedding Endpoints (Admin Only)](#embedding-endpoints-admin-only)
- [Completion Endpoints (Admin Only)](#completion-endpoints-admin-only)
- [Assistants](#assistants)
- [Assistant Settings](#assistant-settings)
- [Documents](#documents)
- [Feedback (Authenticated)](#feedback-authenticated)
- [History (Authenticated)](#history-authenticated)
- [Threads (Authenticated)](#threads-authenticated)
- [Request History (Admin Or Tenant Admin)](#request-history-admin-or-tenant-admin)
- [Models](#models)
- [Public Endpoints](#public-endpoints)
  - [Public Info](#get-v10assistantsassistantidpublic)
  - [Create Thread](#post-v10assistantsassistantidthreads)
  - [Labels/Tags](#get-v10assistantsassistantidlabelsdistinct)
  - [Thread History](#get-v10assistantsassistantidthreadsthreadidhistory)
  - [Chat](#post-v10assistantsassistantidchat)
  - [Generate](#post-v10assistantsassistantidgenerate)
  - [Document Download](#get-v10assistantsassistantiddocumentsdocumentiddownload)
  - [Compact](#post-v10assistantsassistantidcompact)
  - [Feedback](#post-v10assistantsassistantidfeedback)
- [Crawl Plans (Admin Only)](#crawl-plans-admin-only)
- [Crawl Operations](#crawl-operations)
- [Eval (Authenticated)](#eval-authenticated)
- [Configuration (Admin Only)](#configuration-admin-only)
- [Configuration: ChatHistory Settings](#configuration-chathistory-settings)

---

## Authentication

Authenticated endpoints require a bearer token in the `Authorization` header:

```
Authorization: Bearer <token>
```

Alternatively, the token can be passed as a `token` query parameter:

```
GET /v1.0/assistants?token=<token>
```

### Authorization Tiers

| Role | How to Authenticate | Scope |
|------|-------------------|-------|
| **Global Admin** | Admin API key (from `AdminApiKeys` config), or any user with `IsAdmin=true` | All tenants — can manage tenants, users, and all resources |
| **Tenant Admin** | User with `IsTenantAdmin=true` | Single tenant — can manage users, credentials, assistants, ingestion rules within their tenant |
| **Tenant User** | Standard user | Single tenant — can create/manage own assistants and documents |

### POST /v1.0/authenticate

Authenticate using email/password or a bearer token. This endpoint is **unauthenticated**.

**Request Body (email/password):**

```json
{
  "Email": "admin@assistanthub.local",
  "Password": "admin"
}
```

**Request Body (bearer token):**

```json
{
  "BearerToken": "your-bearer-token"
}
```

**Response (200 OK):**

```json
{
  "Success": true,
  "User": {
    "Id": "usr_abc123...",
    "Email": "admin@assistanthub.local",
    "PasswordSha256": null,
    "FirstName": "Admin",
    "LastName": "User",
    "IsAdmin": true,
    "Active": true,
    "CreatedUtc": "2025-01-01T00:00:00Z",
    "LastUpdateUtc": "2025-01-01T00:00:00Z"
  },
  "Credential": {
    "Id": "cred_abc123...",
    "UserId": "usr_abc123...",
    "Name": "Default admin credential",
    "BearerToken": "abc123...",
    "Active": true,
    "CreatedUtc": "2025-01-01T00:00:00Z",
    "LastUpdateUtc": "2025-01-01T00:00:00Z"
  },
  "TenantId": "ten_abc123...",
  "TenantName": "Default",
  "IsGlobalAdmin": true,
  "IsTenantAdmin": true,
  "ErrorMessage": null
}
```

`IsGlobalAdmin` is `true` when the user has `IsAdmin=true` or when authenticating with an admin API key. For admin API key authentication, `User` and `Credential` will be `null`.

**Response (401 Unauthorized):**

```json
{
  "Success": false,
  "User": null,
  "Credential": null,
  "ErrorMessage": "Authentication failed."
}
```

---

## Error Responses

All error responses follow a consistent format:

```json
{
  "Error": "BadRequest",
  "Message": "Bad request. Please check your request and try again.",
  "StatusCode": 400,
  "Context": null,
  "Description": "Optional additional detail."
}
```

| Error Type             | HTTP Status | Message                                                                  |
|------------------------|-------------|--------------------------------------------------------------------------|
| `AuthenticationFailed` | 401         | Authentication failed. Please check your credentials.                    |
| `AuthorizationFailed`  | 403         | Authorization failed. You do not have permission to perform this action. |
| `BadRequest`           | 400         | Bad request. Please check your request and try again.                    |
| `NotFound`             | 404         | The requested resource was not found.                                    |
| `Conflict`             | 409         | A conflict occurred. The resource already exists or has been modified.    |
| `InternalError`        | 500         | An internal error occurred. Please try again later.                      |

---

## Pagination

List endpoints support pagination via query parameters:

| Parameter           | Type   | Default            | Description                                      |
|---------------------|--------|--------------------|--------------------------------------------------|
| `maxResults`        | int    | 100                | Maximum number of results to return (1-1000).     |
| `continuationToken` | string | null               | Token from a previous response for next page.     |
| `ordering`          | string | CreatedDescending  | Sort order (`CreatedDescending`, `CreatedAscending`). |
| `assistantId`       | string | null               | Filter results by assistant ID (where applicable).|
| `threadId`          | string | null               | Filter results by thread ID (history only).       |
| `bucketName`        | string | null               | Filter documents by bucket name (documents only). |
| `collectionId`      | string | null               | Filter documents by collection ID (documents only).|

**Paginated Response Envelope:**

```json
{
  "Success": true,
  "MaxResults": 100,
  "TotalRecords": 42,
  "RecordsRemaining": 0,
  "ContinuationToken": null,
  "EndOfResults": true,
  "Objects": [ ... ],
  "TotalMs": 12.5
}
```

---

## Health

### GET /

Returns server information. **Unauthenticated.**

**Response (200 OK):**

```json
{
  "Product": "AssistantHub",
  "Version": "0.13.0",
  "Timestamp": "2025-01-01T12:00:00Z"
}
```

### HEAD /

Returns 200 OK with no body. Useful for health checks. **Unauthenticated.**

**Response:** `200 OK` (empty body)

---

## OpenAPI

### GET /openapi.json

Returns the live runtime OpenAPI document generated from the currently registered AssistantHub route surface. **Unauthenticated.**

**Response (200 OK):**

- OpenAPI 3.0 JSON document containing the runtime `paths`, `tags`, `servers`, and `securitySchemes`

Operational note:

- The dashboard API Explorer consumes this route directly so the explorer reflects the running server rather than a manually maintained static spec.

---

## Tenants (Global Admin Only)

All tenant endpoints require global admin authentication (admin API key or user with `IsAdmin=true`).

### PUT /v1.0/tenants

Create and provision a new tenant. Auto-creates a RecallDB tenant, default S3 bucket, admin user, credential, and ingestion rule.

**Auth:** Required (global admin only)

**Request Body:**

```json
{
  "Name": "Acme Corp",
  "Active": true
}
```

**Response (200 OK):**

```json
{
  "Tenant": {
    "Id": "ten_abc123...",
    "Name": "Acme Corp",
    "Active": true,
    "IsProtected": false,
    "CreatedUtc": "2025-01-01T00:00:00Z",
    "LastUpdateUtc": "2025-01-01T00:00:00Z"
  },
  "User": {
    "Id": "usr_abc123...",
    "TenantId": "ten_abc123...",
    "Email": "admin@acme-corp",
    "IsAdmin": false,
    "IsTenantAdmin": true,
    "Active": true,
    "IsProtected": true
  },
  "Credential": {
    "Id": "cred_abc123...",
    "TenantId": "ten_abc123...",
    "UserId": "usr_abc123...",
    "Name": "Default admin credential",
    "BearerToken": "auto-generated-64-char-token",
    "Active": true,
    "IsProtected": true
  },
  "RecallDbTenantGuid": "guid...",
  "CollectionGuid": "guid...",
  "BucketName": "ten_abc123_default",
  "IngestionRuleId": "ir_abc123..."
}
```

### GET /v1.0/tenants

List all tenants with pagination.

**Auth:** Required (global admin only)

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing `TenantMetadata` objects.

### GET /v1.0/tenants/{tenantId}

Retrieve a single tenant by ID.

**Auth:** Required (global admin only)

**Response (200 OK):**

```json
{
  "Id": "ten_abc123...",
  "Name": "Acme Corp",
  "Active": true,
  "IsProtected": false,
  "Labels": [],
  "Tags": {},
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `404` -- Tenant not found.

### PUT /v1.0/tenants/{tenantId}

Update an existing tenant.

**Auth:** Required (global admin only)

**Request Body:**

```json
{
  "Name": "Acme Corp Updated",
  "Active": true,
  "IsProtected": false,
  "Labels": ["production"],
  "Tags": { "region": "us-east" }
}
```

**Response (200 OK):** The updated `TenantMetadata` object.

**Error Responses:**
- `404` -- Tenant not found.

### DELETE /v1.0/tenants/{tenantId}

Delete a tenant and deprovision all associated resources (users, credentials, assistants, documents, S3 buckets, RecallDB tenant).

**Auth:** Required (global admin only)

**Response:** `204 No Content`

**Error Responses:**
- `403` -- Tenant is protected. Deactivate by setting `Active` to `false` instead.
- `404` -- Tenant not found.

### HEAD /v1.0/tenants/{tenantId}

Check whether a tenant exists.

**Auth:** Required (global admin only)

**Response:**
- `200 OK` -- Tenant exists.
- `404 Not Found` -- Tenant does not exist.

---

## Who Am I

### GET /v1.0/whoami

Retrieve identity information for the currently authenticated user.

**Auth:** Required

**Response (200 OK):**

```json
{
  "isAuthenticated": true,
  "isGlobalAdmin": false,
  "isTenantAdmin": true,
  "tenantId": "ten_abc123...",
  "tenantName": "Default",
  "userId": "usr_abc123...",
  "email": "admin@assistanthub.local"
}
```

| Field              | Type   | Description                                      |
|--------------------|--------|--------------------------------------------------|
| `isAuthenticated`  | bool   | Whether the request was authenticated.           |
| `isGlobalAdmin`    | bool   | Whether the user has global admin privileges.    |
| `isTenantAdmin`    | bool   | Whether the user is a tenant admin.              |
| `tenantId`         | string | The user's tenant identifier.                    |
| `tenantName`       | string | The user's tenant name.                          |
| `userId`           | string | The user's identifier.                           |
| `email`            | string | The user's email address.                        |

---

## Users (Admin Only)

All user endpoints are tenant-scoped and require authentication with an admin bearer token. Non-admin users receive `403 Forbidden`.

### PUT /v1.0/tenants/{tenantId}/users

Create a new user.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Email": "newuser@example.com",
  "PasswordSha256": "sha256-hash-of-password",
  "FirstName": "Jane",
  "LastName": "Doe",
  "IsAdmin": false,
  "IsTenantAdmin": false,
  "Active": true,
  "IsProtected": false
}
```

**Response (201 Created):**

```json
{
  "Id": "usr_abc123...",
  "Email": "newuser@example.com",
  "PasswordSha256": "sha256-hash-of-password",
  "FirstName": "Jane",
  "LastName": "Doe",
  "IsAdmin": false,
  "IsTenantAdmin": false,
  "Active": true,
  "IsProtected": false,
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `400` -- Email is required.
- `409` -- A user with this email already exists.

### GET /v1.0/tenants/{tenantId}/users

List all users with pagination.

**Auth:** Required (admin only)

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing `UserMaster` objects.

### GET /v1.0/tenants/{tenantId}/users/{userId}

Retrieve a single user by ID.

**Auth:** Required (admin only)

**Response (200 OK):**

```json
{
  "Id": "usr_abc123...",
  "Email": "admin@assistanthub.local",
  "PasswordSha256": "...",
  "FirstName": "Admin",
  "LastName": "User",
  "IsAdmin": true,
  "IsTenantAdmin": false,
  "Active": true,
  "IsProtected": true,
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `404` -- User not found.

### PUT /v1.0/tenants/{tenantId}/users/{userId}

Update an existing user. The `Id` and `CreatedUtc` fields are preserved from the existing record. If `PasswordSha256` is omitted or empty, the existing password is kept.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Email": "updated@example.com",
  "FirstName": "Jane",
  "LastName": "Smith",
  "IsAdmin": false,
  "IsTenantAdmin": false,
  "Active": true,
  "IsProtected": false
}
```

**Response (200 OK):** The updated `UserMaster` object.

**Error Responses:**
- `404` -- User not found.

### DELETE /v1.0/tenants/{tenantId}/users/{userId}

Delete a user and all associated credentials (cascading delete).

**Auth:** Required (admin only)

**Response:** `204 No Content`

**Error Responses:**
- `403` -- User is protected. Deactivate by setting `Active` to `false` instead.
- `404` -- User not found.

### HEAD /v1.0/tenants/{tenantId}/users/{userId}

Check whether a user exists.

**Auth:** Required (admin only)

**Response:**
- `200 OK` -- User exists.
- `404 Not Found` -- User does not exist.

---

## Credentials (Admin Only)

All credential endpoints are tenant-scoped and require authentication with an admin bearer token.

### PUT /v1.0/tenants/{tenantId}/credentials

Create a new API credential. The `Id` and `BearerToken` are auto-generated by the server.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "UserId": "usr_abc123...",
  "Name": "My API Key",
  "Active": true,
  "IsProtected": false
}
```

**Response (201 Created):**

```json
{
  "Id": "cred_abc123...",
  "UserId": "usr_abc123...",
  "Name": "My API Key",
  "BearerToken": "auto-generated-64-char-token",
  "Active": true,
  "IsProtected": false,
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `400` -- UserId is required.
- `404` -- User not found.

### GET /v1.0/tenants/{tenantId}/credentials

List all credentials with pagination.

**Auth:** Required (admin only)

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing `Credential` objects.

### GET /v1.0/tenants/{tenantId}/credentials/{credentialId}

Retrieve a single credential by ID.

**Auth:** Required (admin only)

**Response (200 OK):**

```json
{
  "Id": "cred_abc123...",
  "TenantId": "ten_abc123...",
  "UserId": "usr_abc123...",
  "Name": "My API Key",
  "BearerToken": "abc123...",
  "Active": true,
  "IsProtected": false,
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `404` -- Credential not found.

### PUT /v1.0/tenants/{tenantId}/credentials/{credentialId}

Update an existing credential. The `Id`, `UserId`, and `BearerToken` fields are preserved from the existing record.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Name": "Renamed API Key",
  "Active": false,
  "IsProtected": false
}
```

**Response (200 OK):** The updated `Credential` object.

**Error Responses:**
- `404` -- Credential not found.

### DELETE /v1.0/tenants/{tenantId}/credentials/{credentialId}

Delete a credential.

**Auth:** Required (admin only)

**Response:** `204 No Content`

**Error Responses:**
- `403` -- Credential is protected. Deactivate by setting `Active` to `false` instead.
- `404` -- Credential not found.

### HEAD /v1.0/tenants/{tenantId}/credentials/{credentialId}

Check whether a credential exists.

**Auth:** Required (admin only)

**Response:**
- `200 OK` -- Credential exists.
- `404 Not Found` -- Credential does not exist.

---

## Buckets (Tenant-Scoped)

Bucket endpoints are tenant-scoped. Non-global-admin users can only access buckets prefixed with their tenant ID (`{tenantId}_`). Bucket creation and deletion require admin privileges. Buckets are managed on the configured S3-compatible storage server (Less3).

### PUT /v1.0/buckets

Create a new S3 bucket.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Name": "my-bucket"
}
```

**Response (201 Created):**

```json
{
  "Name": "my-bucket"
}
```

**Error Responses:**
- `400` -- Name is required.
- `409` -- Bucket already exists.

### GET /v1.0/buckets

List all buckets.

**Auth:** Required (admin only)

**Response (200 OK):**

```json
{
  "Objects": [
    { "Name": "my-bucket", "CreationDate": "2025-01-01T00:00:00Z" }
  ],
  "TotalRecords": 1
}
```

### GET /v1.0/buckets/{name}

Retrieve a single bucket by name.

**Auth:** Required (admin only)

**Response (200 OK):**

```json
{
  "Name": "my-bucket",
  "CreationDate": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `404` -- Bucket not found.

### DELETE /v1.0/buckets/{name}

Delete a bucket. The bucket must be empty.

**Auth:** Required (admin only)

**Response:** `204 No Content`

**Error Responses:**
- `404` -- Bucket not found.
- `409` -- Bucket is not empty.

### HEAD /v1.0/buckets/{name}

Check whether a bucket exists.

**Auth:** Required (admin only)

**Response:**
- `200 OK` -- Bucket exists.
- `404 Not Found` -- Bucket does not exist.

---

## Bucket Objects (Tenant-Scoped)

Manage objects within S3-compatible storage buckets. Object keys may contain path separators (`/`) and are passed as query parameters. Non-global-admin users can only access objects in buckets prefixed with their tenant ID.

### GET /v1.0/buckets/{name}/objects

List objects in a bucket with optional prefix-based filtering for directory-like navigation.

**Auth:** Required (admin only)

**Query Parameters:**

| Parameter   | Type   | Default | Description                              |
|-------------|--------|---------|------------------------------------------|
| `prefix`    | string | `""`    | Filter objects by key prefix.            |
| `delimiter` | string | `"/"`   | Delimiter for grouping common prefixes.  |

**Response (200 OK):**

```json
{
  "Prefix": "documents/",
  "Delimiter": "/",
  "CommonPrefixes": [
    { "Prefix": "documents/invoices/" }
  ],
  "Objects": [
    {
      "Key": "documents/readme.txt",
      "Size": 1024,
      "LastModified": "2025-01-01T12:00:00Z",
      "ETag": "\"d41d8cd98f00b204e9800998ecf8427e\""
    }
  ],
  "TotalRecords": 1
}
```

**Error Responses:**
- `404` -- Bucket not found.

### GET /v1.0/buckets/{name}/objects/metadata

Get metadata for a specific object.

**Auth:** Required (admin only)

**Query Parameters:**

| Parameter | Type   | Required | Description        |
|-----------|--------|----------|--------------------|
| `key`     | string | Yes      | The object key.    |

**Response (200 OK):**

```json
{
  "Key": "documents/readme.txt",
  "ContentLength": 1024,
  "ContentType": "text/plain",
  "LastModified": "2025-01-01T12:00:00Z",
  "ETag": "\"d41d8cd98f00b204e9800998ecf8427e\"",
  "Metadata": {}
}
```

**Error Responses:**
- `400` -- Key is required.
- `404` -- Object not found.

### PUT /v1.0/buckets/{name}/objects

Create an empty object (directory marker) in a bucket.

**Auth:** Required (admin only)

**Query Parameters:**

| Parameter | Type   | Required | Description                                          |
|-----------|--------|----------|------------------------------------------------------|
| `key`     | string | Yes      | The object key (typically ending in `/` for dirs).   |

**Response (201 Created):**

```json
{
  "Key": "documents/invoices/"
}
```

**Error Responses:**
- `400` -- Key is required.
- `404` -- Bucket not found.

### DELETE /v1.0/buckets/{name}/objects

Delete an object from a bucket.

**Auth:** Required (admin only)

**Query Parameters:**

| Parameter | Type   | Required | Description        |
|-----------|--------|----------|--------------------|
| `key`     | string | Yes      | The object key.    |

**Response:** `204 No Content`

**Error Responses:**
- `400` -- Key is required.
- `404` -- Object not found.

### POST /v1.0/buckets/{name}/objects/upload

Upload a file to a bucket. Sends raw binary content in the request body.

**Auth:** Required (admin only)

**Query Parameters:**

| Parameter | Type   | Required | Description                    |
|-----------|--------|----------|--------------------------------|
| `key`     | string | Yes      | The S3 key path for the file.  |

**Request Body:** Raw binary file content. Set the `Content-Type` header to the file's MIME type.

**Response (201 Created):**

```json
{
  "Key": "documents/guide.pdf",
  "Size": 1048576
}
```

**Error Responses:**
- `400` -- Key is required.
- `404` -- Bucket not found.

### GET /v1.0/buckets/{name}/objects/download

Download an object. Returns the raw object data with appropriate `Content-Type` and `Content-Disposition` headers.

**Auth:** Required (admin only)

**Query Parameters:**

| Parameter | Type   | Required | Description        |
|-----------|--------|----------|--------------------|
| `key`     | string | Yes      | The object key.    |

**Response:** Binary file data with `Content-Disposition: attachment`.

**Error Responses:**
- `400` -- Key is required.
- `404` -- Object not found.

---

## Collections (Admin Only)

All collection endpoints require authentication with an admin bearer token. Collections are managed via RecallDB.

### PUT /v1.0/collections

Create a new collection.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Name": "my-collection",
  "Description": "A collection for document embeddings.",
  "Dimensionality": 384,
  "Active": true
}
```

**Response:** The created collection object (proxied from RecallDB).

### GET /v1.0/collections

List collections with pagination.

**Auth:** Required (admin only)

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing collection objects.

### GET /v1.0/collections/{collectionId}

Retrieve a single collection by ID.

**Auth:** Required (admin only)

**Error Responses:**
- `404` -- Collection not found.

### PUT /v1.0/collections/{collectionId}

Update an existing collection.

**Auth:** Required (admin only)

### DELETE /v1.0/collections/{collectionId}

Delete a collection.

**Auth:** Required (admin only)

**Response:** `204 No Content`

### HEAD /v1.0/collections/{collectionId}

Check whether a collection exists.

**Auth:** Required (admin only)

**Response:**
- `200 OK` -- Collection exists.
- `404 Not Found` -- Collection does not exist.

---

## Collection Records (Admin Only)

Browse and manage records (documents) within a RecallDB collection.

### PUT /v1.0/collections/{collectionId}/records

Create a new record (document) in a collection. Proxied to RecallDB's document PUT endpoint.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Content": "The text content of the record.",
  "Embeddings": [0.1, -0.2, 0.3],
  "Metadata": {
    "SourceDocumentId": "doc-123",
    "ChunkIndex": 0
  }
}
```

| Field        | Type     | Required | Description                                |
|--------------|----------|----------|--------------------------------------------|
| `Content`    | string   | Yes      | The text content of the record.            |
| `Embeddings` | double[] | No       | Pre-computed embedding vector.             |
| `Metadata`   | object   | No       | Arbitrary key-value metadata.              |

**Response:** The created record object (proxied from RecallDB).

**Error Responses:**
- `400` -- Invalid request.
- `500` -- Internal error.

### GET /v1.0/collections/{collectionId}/records

List records in a collection with pagination. Proxied to RecallDB's document enumerate endpoint.

**Auth:** Required (admin only)

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing record objects.

### GET /v1.0/collections/{collectionId}/records/{recordId}

Retrieve a single record by ID.

**Auth:** Required (admin only)

**Response (200 OK):** The full record object from RecallDB.

**Error Responses:**
- `404` -- Record not found.

### DELETE /v1.0/collections/{collectionId}/records/{recordId}

Delete a record from a collection.

**Auth:** Required (admin only)

**Response:** `204 No Content`

**Error Responses:**
- `404` -- Record not found.

### POST /v1.0/collections/{collectionId}/records/batch/delete

Batch delete multiple records from a collection.

**Auth:** Required (global admin only)

**Request Body:**

```json
{
  "DocumentIds": ["record-id-1", "record-id-2", "record-id-3"]
}
```

| Field         | Type     | Required | Description                            |
|---------------|----------|----------|----------------------------------------|
| `DocumentIds` | string[] | Yes      | List of record IDs to delete.          |

**Response:** `204 No Content`

**Error Responses:**
- `400` -- Invalid request.
- `403` -- Not a global admin user.
- `500` -- Internal error.

### GET /v1.0/collections/{collectionId}/labels/distinct

Retrieve all distinct label values across records in a collection. Useful for populating filter UIs.

**Auth:** Required (admin only)

**Response (200 OK):**

```json
["finance", "quarterly-report", "internal", "draft"]
```

Returns a JSON array of unique label strings found in the collection.

**Error Responses:**
- `403` -- Not an admin user.
- `404` -- Collection not found.

### GET /v1.0/collections/{collectionId}/tags/distinct

Retrieve all distinct tag keys across records in a collection. Useful for populating filter UIs.

**Auth:** Required (admin only)

**Response (200 OK):**

```json
["department", "year", "status", "author"]
```

Returns a JSON array of unique tag key strings found in the collection.

**Error Responses:**
- `403` -- Not an admin user.
- `404` -- Collection not found.

---

## Ingestion Rules

Ingestion rules define how documents are processed, summarized, chunked, and embedded. Each rule specifies a target S3 bucket and RecallDB collection, along with optional summarization, chunking, and embedding configuration.

### PUT /v1.0/ingestion-rules

Create a new ingestion rule.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Name": "Knowledge Base Documents",
  "Description": "Process PDF and text documents for the support knowledge base.",
  "Bucket": "kb-documents",
  "CollectionName": "my-collection",
  "CollectionId": "collection-uuid-here",
  "Labels": ["support", "knowledge-base"],
  "Tags": { "department": "engineering", "priority": "high" },
  "Summarization": {
    "CompletionEndpointId": "endpoint-uuid-here",
    "Order": "BottomUp",
    "SummarizationPrompt": "Summarize the following text concisely: {content}",
    "MaxSummaryTokens": 1024,
    "MinCellLength": 128,
    "MaxParallelTasks": 1,
    "MaxRetriesPerSummary": 3,
    "MaxRetries": 9,
    "TimeoutMs": 300000
  },
  "Chunking": {
    "Strategy": "FixedTokenCount",
    "FixedTokenCount": 256,
    "OverlapCount": 32,
    "OverlapPercentage": null,
    "OverlapStrategy": null,
    "RowGroupSize": 5,
    "ContextPrefix": null,
    "RegexPattern": null
  },
  "Embedding": {
    "EmbeddingEndpointId": null,
    "L2Normalization": false
  }
}
```

**Summarization Configuration (optional):**

| Field                    | Type    | Default      | Description                                                      |
|--------------------------|---------|--------------|------------------------------------------------------------------|
| `CompletionEndpointId`   | string  | null         | ID of the completion endpoint to use for summarization. Required when summarization is enabled. |
| `Order`                  | string  | BottomUp     | Summarization traversal order: `BottomUp` or `TopDown`.          |
| `SummarizationPrompt`    | string  | null         | Custom prompt for the summarization model. Should contain the `{content}` placeholder. |
| `MaxSummaryTokens`       | int     | 1024         | Maximum tokens for each summary response. Minimum: 128.         |
| `MinCellLength`          | int     | 128          | Minimum cell content length (in characters) to trigger summarization. Minimum: 0. |
| `MaxParallelTasks`       | int     | 1            | Maximum concurrent summarization tasks. Minimum: 1.             |
| `MaxRetriesPerSummary`   | int     | 3            | Retries per individual summary request. Minimum: 0.             |
| `MaxRetries`             | int     | 9            | Global failure limit across all cells (circuit breaker). Minimum: 0. |
| `TimeoutMs`              | int     | 300000       | Timeout in milliseconds for each summarization request. Minimum: 100. |

**Chunking Configuration (optional):**

| Field               | Type    | Default         | Description                                                                                                  |
|---------------------|---------|-----------------|--------------------------------------------------------------------------------------------------------------|
| `Strategy`          | string  | FixedTokenCount | Chunking strategy: `None`, `FixedTokenCount`, `SentenceBased`, `ParagraphBased`, `RegexBased`, `WholeList`, `ListEntry`, `Row`, `RowWithHeaders`, `RowGroupWithHeaders`, `KeyValuePairs`, `WholeTable`. When set to `None`, chunking is skipped and the entire document is treated as a single chunk. |
| `FixedTokenCount`   | int     | 256             | Tokens per chunk (FixedTokenCount strategy). Minimum: 1.                                                     |
| `OverlapCount`      | int     | 0               | Number of overlapping tokens between consecutive chunks.                                                     |
| `OverlapPercentage` | double? | null            | Overlap as a fraction of chunk size (0.0–1.0). Alternative to OverlapCount.                                  |
| `OverlapStrategy`   | string  | null            | Overlap boundary strategy: `SlidingWindow`, `SentenceBoundaryAware`, or `SemanticBoundaryAware`.             |
| `RowGroupSize`      | int     | 5               | Rows per group for `RowGroupWithHeaders` strategy. Minimum: 1.                                               |
| `ContextPrefix`     | string  | null            | Optional text prepended to each chunk for additional context.                                                |
| `RegexPattern`      | string  | null            | Regex pattern for the `RegexBased` strategy.                                                                 |

**Response (201 Created):**

```json
{
  "Id": "irule_abc123...",
  "Name": "Knowledge Base Documents",
  "Description": "Process PDF and text documents for the support knowledge base.",
  "Bucket": "kb-documents",
  "CollectionName": "my-collection",
  "CollectionId": "collection-uuid-here",
  "Labels": ["support", "knowledge-base"],
  "Tags": { "department": "engineering", "priority": "high" },
  "Summarization": {
    "CompletionEndpointId": "endpoint-uuid-here",
    "Order": "BottomUp",
    "SummarizationPrompt": "Summarize the following text concisely: {content}",
    "MaxSummaryTokens": 1024,
    "MinCellLength": 128,
    "MaxParallelTasks": 1,
    "MaxRetriesPerSummary": 3,
    "MaxRetries": 9,
    "TimeoutMs": 300000
  },
  "Chunking": {
    "Strategy": "FixedTokenCount",
    "FixedTokenCount": 256,
    "OverlapCount": 32,
    "OverlapPercentage": null,
    "OverlapStrategy": null,
    "RowGroupSize": 5,
    "ContextPrefix": null,
    "RegexPattern": null
  },
  "Embedding": {
    "EmbeddingEndpointId": null,
    "L2Normalization": false
  },
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `400` -- Name is required; or summarization configuration is invalid.
- `403` -- Not an admin user.

### GET /v1.0/ingestion-rules

List ingestion rules with pagination.

**Auth:** Required

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing `IngestionRule` objects.

### GET /v1.0/ingestion-rules/{ruleId}

Retrieve a single ingestion rule by ID.

**Auth:** Required

**Response (200 OK):** An `IngestionRule` object.

**Error Responses:**
- `404` -- Ingestion rule not found.

### PUT /v1.0/ingestion-rules/{ruleId}

Update an existing ingestion rule. The `Id` and `CreatedUtc` fields are preserved from the existing record.

**Auth:** Required (admin only)

**Request Body:** Same format as create.

**Response (200 OK):** The updated `IngestionRule` object.

**Error Responses:**
- `403` -- Not an admin user.
- `404` -- Ingestion rule not found.

### DELETE /v1.0/ingestion-rules/{ruleId}

Delete an ingestion rule.

**Auth:** Required (admin only)

**Response:** `204 No Content`

**Error Responses:**
- `403` -- Not an admin user.
- `404` -- Ingestion rule not found.

### HEAD /v1.0/ingestion-rules/{ruleId}

Check whether an ingestion rule exists.

**Auth:** Required

**Response:**
- `200 OK` -- Ingestion rule exists.
- `404 Not Found` -- Ingestion rule does not exist.

---

## Embedding Endpoints (Admin Only)

Manage embedding endpoints on the Partio chunking service. These endpoints define which embedding model and API to use for vectorizing document chunks. All routes are proxied to Partio.

### PUT /v1.0/endpoints/embedding

Create a new embedding endpoint.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Name": "Gemini Embeddings",
  "Model": "text-embedding-004",
  "Endpoint": "https://generativelanguage.googleapis.com",
  "ApiFormat": "Gemini",
  "ApiKey": "AIza...",
  "Active": true,
  "HealthCheckEnabled": true,
  "HealthCheckUrl": "https://generativelanguage.googleapis.com/v1beta/models",
  "HealthCheckMethod": "GET",
  "HealthCheckIntervalMs": 30000,
  "HealthCheckTimeoutMs": 10000,
  "HealthCheckExpectedStatusCode": 200,
  "HealthyThreshold": 2,
  "UnhealthyThreshold": 2,
  "HealthCheckUseAuth": true
}
```

**Response:** The created endpoint object (proxied from Partio).

**Error Responses:**
- `403` -- Not an admin user.
- `502` -- Partio service unavailable.

### POST /v1.0/endpoints/embedding/enumerate

List all embedding endpoints.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "maxResults": 1000
}
```

**Response (200 OK):** Standard `EnumerationResult` envelope containing embedding endpoint objects.

### GET /v1.0/endpoints/embedding/{endpointId}

Retrieve a single embedding endpoint by ID.

**Auth:** Required (admin only)

**Error Responses:**
- `404` -- Endpoint not found.

### PUT /v1.0/endpoints/embedding/{endpointId}

Update an existing embedding endpoint.

**Auth:** Required (admin only)

**Request Body:** Same format as create.

**Response (200 OK):** The updated endpoint object.

**Error Responses:**
- `404` -- Endpoint not found.

### DELETE /v1.0/endpoints/embedding/{endpointId}

Delete an embedding endpoint.

**Auth:** Required (admin only)

**Response:** `204 No Content`

**Error Responses:**
- `404` -- Endpoint not found.

### HEAD /v1.0/endpoints/embedding/{endpointId}

Check whether an embedding endpoint exists.

**Auth:** Required (admin only)

**Response:**
- `200 OK` -- Endpoint exists.
- `404 Not Found` -- Endpoint does not exist.

### GET /v1.0/endpoints/embedding/health

Check the health of all embedding endpoints.

**Auth:** Required (admin only)

**Response (200 OK):** Health status for all embedding endpoints from Partio.

### GET /v1.0/endpoints/embedding/{endpointId}/health

Check the health of an embedding endpoint.

**Auth:** Required (admin only)

**Response (200 OK):** Health status from Partio.

### POST /v1.0/endpoints/embedding/{endpointId}/test

Run a smoke test against a specific embedding endpoint through AssistantHub's Partio proxy. This validates the AssistantHub-to-Partio-to-provider path and returns the explorer response payload from Partio.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Input": "AssistantHub embedding smoke test input",
  "L2Normalization": false
}
```

**Response (200 OK):**

```json
{
  "Success": true,
  "StatusCode": 200,
  "Error": null,
  "EndpointId": "ep_abc123",
  "Model": "gemini-embedding-001",
  "Input": "AssistantHub embedding smoke test input",
  "Embedding": [0.0123, -0.0456, 0.0789],
  "Dimensions": 768,
  "ResponseTimeMs": 243,
  "RequestHistoryId": "erh_abc123",
  "EmbeddingCalls": [
    {
      "Url": "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent",
      "Method": "POST",
      "StatusCode": 200,
      "ResponseTimeMs": 219,
      "Success": true,
      "Error": null,
      "TimestampUtc": "2026-03-20T12:00:00Z"
    }
  ]
}
```

**Error Responses:**
- `403` -- Not an admin user.
- `404` -- Endpoint not found.
- `502` -- Partio or upstream provider unavailable.

### POST /v1.0/endpoints/embedding/{endpointId}/load

Load or warm the configured embedding endpoint model through AssistantHub's Partio proxy. AssistantHub forwards the request to Partio and returns Partio's status code and response payload.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Strategy": "Auto",
  "TimeoutMs": 60000,
  "KeepAlive": "30m",
  "SampleInput": "Partio model load probe",
  "MaxTokens": 1,
  "RecordRequestHistory": true,
  "RequireNativeLoad": false
}
```

**Response (200 OK on success, mapped non-2xx on failure):**

```json
{
  "Success": true,
  "StatusCode": 200,
  "Outcome": "Loaded",
  "EndpointType": "Embedding",
  "EndpointId": "ep_abc123",
  "TenantId": "default",
  "ApiFormat": "Ollama",
  "Model": "nomic-embed-text",
  "Strategy": "NativeProviderLoad",
  "Message": "Ollama accepted the preload request.",
  "ResponseTimeMs": 482.5,
  "StartedUtc": "2026-06-05T18:00:00Z",
  "CompletedUtc": "2026-06-05T18:00:01Z",
  "RequestHistoryId": "req_abc123",
  "EmbeddingCalls": [],
  "CompletionCalls": null
}
```

**Error Responses:** Partio-mapped model-load errors are returned with the same response shape.
- `400` -- Invalid request body or unload-style keep-alive value.
- `403` -- Not an admin user.
- `404` -- Endpoint not found.
- `409` -- Native provider load was required but unsupported by the configured provider.
- `429` -- Endpoint concurrency limit reached.
- `502` -- Partio or upstream provider failure.
- `504` -- Upstream provider timeout.

---

## Completion Endpoints (Admin Only)

Manage completion (inference) endpoints on the Partio service. These endpoints define which LLM and API to use for summarization during document ingestion. All routes are proxied to Partio.

### PUT /v1.0/endpoints/completion

Create a new completion endpoint.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Name": "Gemini Summarizer",
  "Model": "gemini-2.5-flash",
  "Endpoint": "https://generativelanguage.googleapis.com",
  "ApiFormat": "Gemini",
  "ApiKey": "AIza...",
  "Active": true,
  "HealthCheckEnabled": true,
  "HealthCheckUrl": "https://generativelanguage.googleapis.com/v1beta/models",
  "HealthCheckMethod": "GET",
  "HealthCheckIntervalMs": 30000,
  "HealthCheckTimeoutMs": 10000,
  "HealthCheckExpectedStatusCode": 200,
  "HealthyThreshold": 2,
  "UnhealthyThreshold": 2,
  "HealthCheckUseAuth": true
}
```

**Response:** The created endpoint object (proxied from Partio).

**Error Responses:**
- `403` -- Not an admin user.
- `502` -- Partio service unavailable.

### POST /v1.0/endpoints/completion/enumerate

List all completion endpoints.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "maxResults": 1000
}
```

**Response (200 OK):** Standard `EnumerationResult` envelope containing completion endpoint objects.

### GET /v1.0/endpoints/completion/{endpointId}

Retrieve a single completion endpoint by ID.

**Auth:** Required (admin only)

**Error Responses:**
- `404` -- Endpoint not found.

### PUT /v1.0/endpoints/completion/{endpointId}

Update an existing completion endpoint.

**Auth:** Required (admin only)

**Request Body:** Same format as create.

**Response (200 OK):** The updated endpoint object.

**Error Responses:**
- `404` -- Endpoint not found.

### DELETE /v1.0/endpoints/completion/{endpointId}

Delete a completion endpoint.

**Auth:** Required (admin only)

**Response:** `204 No Content`

**Error Responses:**
- `404` -- Endpoint not found.

### HEAD /v1.0/endpoints/completion/{endpointId}

Check whether a completion endpoint exists.

**Auth:** Required (admin only)

**Response:**
- `200 OK` -- Endpoint exists.
- `404 Not Found` -- Endpoint does not exist.

### GET /v1.0/endpoints/completion/health

Check the health of all completion endpoints.

**Auth:** Required (admin only)

**Response (200 OK):** Health status for all completion endpoints from Partio.

### GET /v1.0/endpoints/completion/{endpointId}/health

Check the health of a completion endpoint.

**Auth:** Required (admin only)

**Response (200 OK):** Health status from Partio.

### POST /v1.0/endpoints/completion/{endpointId}/test

Run a smoke test against a specific completion endpoint through AssistantHub's Partio proxy. This validates the AssistantHub-to-Partio-to-provider path and returns the explorer response payload from Partio.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Prompt": "Respond with a one-sentence smoke test confirmation.",
  "SystemPrompt": "You are a concise and accurate assistant.",
  "MaxTokens": 512,
  "TimeoutMs": 60000
}
```

**Response (200 OK):**

```json
{
  "Success": true,
  "StatusCode": 200,
  "Error": null,
  "EndpointId": "cep_abc123",
  "Model": "gemini-2.5-flash",
  "Prompt": "Respond with a one-sentence smoke test confirmation.",
  "SystemPrompt": "You are a concise and accurate assistant.",
  "Output": "AssistantHub can successfully reach this inference endpoint.",
  "ResponseTimeMs": 418,
  "RequestHistoryId": "crh_abc123",
  "CompletionCalls": [
    {
      "Url": "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent",
      "Method": "POST",
      "StatusCode": 200,
      "ResponseTimeMs": 394,
      "Success": true,
      "Error": null,
      "TimestampUtc": "2026-03-20T12:00:00Z"
    }
  ]
}
```

**Error Responses:**
- `403` -- Not an admin user.
- `404` -- Endpoint not found.
- `502` -- Partio or upstream provider unavailable.

### POST /v1.0/endpoints/completion/{endpointId}/load

Load or warm the configured completion endpoint model through AssistantHub's Partio proxy. AssistantHub forwards the request to Partio and returns Partio's status code and response payload.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Strategy": "Auto",
  "TimeoutMs": 60000,
  "KeepAlive": "30m",
  "SampleInput": "Partio model load probe",
  "MaxTokens": 1,
  "RecordRequestHistory": true,
  "RequireNativeLoad": false
}
```

**Response (200 OK on success, mapped non-2xx on failure):**

```json
{
  "Success": true,
  "StatusCode": 200,
  "Outcome": "Loaded",
  "EndpointType": "Completion",
  "EndpointId": "cep_abc123",
  "TenantId": "default",
  "ApiFormat": "Ollama",
  "Model": "gemma3:4b",
  "Strategy": "NativeProviderLoad",
  "Message": "Ollama accepted the preload request.",
  "ResponseTimeMs": 482.5,
  "StartedUtc": "2026-06-05T18:00:00Z",
  "CompletedUtc": "2026-06-05T18:00:01Z",
  "RequestHistoryId": "req_abc123",
  "EmbeddingCalls": null,
  "CompletionCalls": []
}
```

**Error Responses:** Partio-mapped model-load errors are returned with the same response shape.
- `400` -- Invalid request body or unload-style keep-alive value.
- `403` -- Not an admin user.
- `404` -- Endpoint not found.
- `409` -- Native provider load was required but unsupported by the configured provider.
- `429` -- Endpoint concurrency limit reached.
- `502` -- Partio or upstream provider failure.
- `504` -- Upstream provider timeout.

---

## Assistants

Authenticated users can manage their own assistants. Admin users can see and manage all assistants.

### PUT /v1.0/assistants

Create a new assistant. The `UserId` is set automatically from the authenticated user. Default assistant settings are created alongside the assistant.

**Auth:** Required

**Request Body:**

```json
{
  "Name": "Customer Support Bot",
  "Description": "Answers questions about our product documentation.",
  "Active": true
}
```

**Response (201 Created):**

```json
{
  "Id": "asst_abc123...",
  "UserId": "usr_abc123...",
  "Name": "Customer Support Bot",
  "Description": "Answers questions about our product documentation.",
  "Active": true,
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `400` -- Name is required.

### GET /v1.0/assistants

List assistants. Non-admin users only see their own assistants; admin users see all.

**Auth:** Required

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing `Assistant` objects.

### GET /v1.0/assistants/{assistantId}

Retrieve a single assistant by ID.

**Auth:** Required (owner or admin)

**Response (200 OK):**

```json
{
  "Id": "asst_abc123...",
  "UserId": "usr_abc123...",
  "Name": "Customer Support Bot",
  "Description": "Answers questions about our product documentation.",
  "Active": true,
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `403` -- Not the owner and not an admin.
- `404` -- Assistant not found.

### PUT /v1.0/assistants/{assistantId}

Update an existing assistant. The `Id`, `UserId`, and `CreatedUtc` fields are preserved.

**Auth:** Required (owner or admin)

**Request Body:**

```json
{
  "Name": "Updated Bot Name",
  "Description": "Updated description.",
  "Active": true
}
```

**Response (200 OK):** The updated `Assistant` object.

**Error Responses:**
- `403` -- Not the owner and not an admin.
- `404` -- Assistant not found.

### DELETE /v1.0/assistants/{assistantId}

Delete an assistant and all associated settings, documents, and feedback (cascading delete).

**Auth:** Required (owner or admin)

**Response:** `204 No Content`

**Error Responses:**
- `403` -- Not the owner and not an admin.
- `404` -- Assistant not found.

### HEAD /v1.0/assistants/{assistantId}

Check whether an assistant exists and is accessible by the authenticated user.

**Auth:** Required (owner or admin)

**Response:**
- `200 OK` -- Assistant exists and is accessible.
- `403 Forbidden` -- Not the owner and not an admin.
- `404 Not Found` -- Assistant does not exist.

---

## Assistant Settings

Each assistant has an associated settings record that controls inference behavior.

### GET /v1.0/assistants/{assistantId}/settings

Retrieve settings for an assistant.

**Auth:** Required (owner or admin)

**Response (200 OK):**

```json
{
  "Id": "aset_abc123...",
  "AssistantId": "asst_abc123...",
  "Temperature": 0.7,
  "TopP": 1.0,
  "SystemPrompt": "You are a helpful assistant. Use the provided context to answer questions accurately.",
  "MaxTokens": 4096,
  "ContextWindow": 8192,
  "EnableRag": false,
  "EnableRetrievalGate": false,
  "EnableQueryRewrite": false,
  "QueryRewritePrompt": null,
  "EnableReranking": false,
  "RerankerTopK": 5,
  "RerankerScoreThreshold": 3.0,
  "RerankPrompt": null,
  "EnableCitations": false,
  "CitationLinkMode": "None",
  "CollectionId": "collection-uuid",
  "RetrievalTopK": 5,
  "RetrievalScoreThreshold": 0.7,
  "SearchMode": "Hybrid",
  "TextWeight": 0.3,
  "FullTextSearchType": "TsRank",
  "FullTextLanguage": "english",
  "FullTextNormalization": 32,
  "FullTextMinimumScore": null,
  "RetrievalIncludeNeighbors": 0,
  "InferenceEndpointId": "ep_abc123...",
  "RetrievalGateInferenceEndpointId": null,
  "QueryRewriteInferenceEndpointId": null,
  "RerankInferenceEndpointId": null,
  "EmbeddingEndpointId": "ep_def456...",
  "Title": "My Support Bot",
  "LogoUrl": "https://example.com/logo.png",
  "FaviconUrl": "https://example.com/favicon.ico",
  "RetrievalLabelFilter": null,
  "RetrievalTagFilter": null,
  "EvalJudgePrompt": null,
  "Streaming": true,
  "EnableSlack": false,
  "SlackAppToken": "xapp-***",
  "SlackBotToken": "xoxb-***",
  "SlackChannelId": "C12345678",
  "SlackMessagePrefix": "Hey bot,",
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Field Descriptions:**

| Field                      | Type    | Description                                                                 |
|----------------------------|---------|-----------------------------------------------------------------------------|
| `Temperature`              | double  | Sampling temperature (0.0 to 2.0).                                          |
| `TopP`                     | double  | Top-p nucleus sampling (0.0 to 1.0).                                        |
| `SystemPrompt`             | string  | System prompt sent to the LLM.                                              |
| `MaxTokens`                | int     | Maximum tokens to generate in a response.                                   |
| `ContextWindow`            | int     | Context window size in tokens.                                              |
| `EnableRag`                | bool    | Enable RAG retrieval for chat. Default `false`.                             |
| `EnableRetrievalGate`      | bool    | Enable LLM-based retrieval gate. When enabled, an LLM call classifies whether each user message requires new document retrieval (`RETRIEVE`) or can be answered from existing conversation context (`SKIP`). Only applies when `EnableRag` is `true`. Default `false`. |
| `EnableQueryRewrite`       | bool    | Whether LLM-based query rewrite is enabled. When enabled, the user's prompt is rewritten into multiple semantically varied queries before retrieval to improve recall. Default `false`. |
| `QueryRewritePrompt`       | string? | The prompt template used for query rewriting. Must contain the `{prompt}` placeholder which is replaced with the user's message. When null or empty, a built-in default prompt is used. |
| `EnableReranking`          | bool    | Enable LLM-based re-ranking of retrieved chunks. Default `false`. |
| `RerankerTopK`             | int     | Maximum chunks to keep after re-ranking (min 1). Default `5`. |
| `RerankerScoreThreshold`   | double  | Minimum LLM relevance score (0-10) to retain a chunk. Default `3.0`. |
| `RerankPrompt`             | string? | Custom re-ranking prompt template (must contain `{query}` and `{chunks}` placeholders). Default `null`. |
| `EnableCitations`          | bool    | Include citation metadata in chat responses. Requires `EnableRag` to also be `true`. Default `false`. |
| `CitationLinkMode`         | string  | Controls document download linking in citation cards. `None` (display-only), `Authenticated` (requires bearer token via `/v1.0/documents/{id}/download`), `Public` (unauthenticated server-proxied download via `/v1.0/assistants/{assistantId}/documents/{id}/download`). Default `None`. |
| `CollectionId`             | string  | RecallDb collection ID for document retrieval.                              |
| `RetrievalTopK`            | int     | Number of top document chunks to retrieve.                                  |
| `RetrievalScoreThreshold`  | double  | Minimum similarity score threshold (0.0 to 1.0).                           |
| `SearchMode`               | string  | Search mode for RAG retrieval: `Vector` (semantic similarity), `FullText` (keyword matching), or `Hybrid` (both combined). Default `Vector`. |
| `TextWeight`               | double  | Weight of full-text score in hybrid mode (0.0 to 1.0). Formula: `Score = (1 - TextWeight) * vectorScore + TextWeight * textScore`. Default `0.3`. |
| `FullTextSearchType`       | string  | Full-text ranking function: `TsRank` (term frequency) or `TsRankCd` (cover density, rewards term proximity). Default `TsRank`. |
| `FullTextLanguage`         | string  | PostgreSQL text search language for stemming and stop words. Values: `english`, `simple`, `spanish`, `french`, `german`. Default `english`. |
| `FullTextNormalization`    | int     | Score normalization bitmask. `32` = normalized 0-1 (recommended). `0` = raw scores. Default `32`. |
| `FullTextMinimumScore`     | double? | Minimum full-text relevance threshold. Documents below this TextScore are excluded. Null = no threshold. |
| `RetrievalIncludeNeighbors`| int     | Number of neighboring chunks to retrieve before and after each matched chunk (0–10). Provides surrounding document context for each search match. Neighbors are merged with the matched chunk to form a seamless context block for the LLM. Does not affect scoring, citation count, or top-K limits. Default `0` (no neighbors). |
| `InferenceEndpointId`      | string  | Managed completion endpoint ID for assistant responses. Required for assistant settings. |
| `RetrievalGateInferenceEndpointId` | string? | Optional managed completion endpoint ID for retrieval gate calls. Null or empty falls back to `InferenceEndpointId`. |
| `QueryRewriteInferenceEndpointId` | string? | Optional managed completion endpoint ID for query rewrite calls. Null or empty falls back to `InferenceEndpointId`. |
| `RerankInferenceEndpointId` | string? | Optional managed completion endpoint ID for re-ranking calls. Null or empty falls back to `InferenceEndpointId`. |
| `EmbeddingEndpointId`      | string  | Managed embedding endpoint ID for RAG retrieval (overrides global setting). |
| `Title`                    | string  | Title displayed as the heading on the chat window. Null uses assistant name.|
| `LogoUrl`                  | string  | URL for the logo image in the chat window (max 192x192). Null uses default.|
| `FaviconUrl`               | string  | URL for the browser tab favicon. Null uses default AssistantHub favicon.    |
| `RetrievalLabelFilter`     | string  | JSON-serialized label filter applied to all RAG retrievals for this assistant. Merged with per-request metadata filters. Null = no default label filter. |
| `RetrievalTagFilter`       | string  | JSON-serialized tag filter applied to all RAG retrievals for this assistant. Merged with per-request metadata filters. Null = no default tag filter. |
| `EvalJudgePrompt`          | string  | Custom judge prompt for evaluation runs on this assistant. Null uses the system default judge prompt. |
| `Streaming`                | bool    | Enable SSE streaming for chat responses. Default `true`.                    |
| `EnableSlack`              | bool    | Enable a per-assistant Slack Socket Mode worker. Default `false`.           |
| `SlackAppToken`            | string  | Slack app token for Socket Mode. Must start with `xapp-` when present.      |
| `SlackBotToken`            | string  | Slack bot token for API access. Must start with `xoxb-` when present.       |
| `SlackChannelId`           | string  | Slack channel ID used for configured channel traffic. Direct messages are also supported. |
| `SlackMessagePrefix`       | string  | Start-of-message indicator for configured channels. `@bot` mention also triggers the assistant. |

**Error Responses:**
- `403` -- Not the owner and not an admin.
- `404` -- Assistant or settings not found.

### PUT /v1.0/assistants/{assistantId}/settings

Create or update settings for an assistant. If settings already exist, they are updated; otherwise, new settings are created.

**Auth:** Required (owner or admin)

**Request Body:**

```json
{
  "Temperature": 0.5,
  "TopP": 0.9,
  "SystemPrompt": "You are a technical support specialist. Answer using the provided documentation.",
  "MaxTokens": 2048,
  "ContextWindow": 8192,
  "EnableRag": false,
  "EnableRetrievalGate": false,
  "EnableQueryRewrite": false,
  "QueryRewritePrompt": null,
  "EnableReranking": false,
  "RerankerTopK": 5,
  "RerankerScoreThreshold": 3.0,
  "RerankPrompt": null,
  "EnableCitations": false,
  "CitationLinkMode": "None",
  "CollectionId": "my-collection-id",
  "RetrievalTopK": 10,
  "RetrievalScoreThreshold": 0.6,
  "SearchMode": "Hybrid",
  "TextWeight": 0.3,
  "FullTextSearchType": "TsRank",
  "FullTextLanguage": "english",
  "FullTextNormalization": 32,
  "FullTextMinimumScore": null,
  "RetrievalIncludeNeighbors": 2,
  "InferenceEndpointId": "ep_abc123...",
  "RetrievalGateInferenceEndpointId": null,
  "QueryRewriteInferenceEndpointId": null,
  "RerankInferenceEndpointId": null,
  "EmbeddingEndpointId": null,
  "Title": "My Support Bot",
  "LogoUrl": "https://example.com/logo.png",
  "FaviconUrl": "https://example.com/favicon.ico",
  "RetrievalLabelFilter": null,
  "RetrievalTagFilter": null,
  "EvalJudgePrompt": null,
  "Streaming": true,
  "EnableSlack": true,
  "SlackAppToken": "xapp-***",
  "SlackBotToken": "xoxb-***",
  "SlackChannelId": "C12345678",
  "SlackMessagePrefix": "Hey bot,"
}
```

**Response (200 OK):** The created or updated `AssistantSettings` object.

`InferenceEndpointId` is required. Assistant settings do not define a separate response model; the selected completion endpoint is the source of truth for provider and model selection. Retrieval gate, query rewrite, and re-ranking can each use their own completion endpoint via the optional endpoint ID fields above; when those fields are null or empty, AssistantHub uses `InferenceEndpointId`.

**Error Responses:**
- `403` -- Not the owner and not an admin.
- `404` -- Assistant not found.

### POST /v1.0/assistants/{assistantId}/settings/slack/verify

Verify draft Slack settings before saving them to the assistant.

**Auth:** Required (owner or admin)

**Request Body:**

```json
{
  "EnableSlack": true,
  "SlackAppToken": "xapp-***",
  "SlackBotToken": "xoxb-***",
  "SlackChannelId": "C12345678",
  "SlackMessagePrefix": "Hey bot,"
}
```

**Response (200 OK):**

```json
{
  "Success": true,
  "BotToken": {
    "Success": true,
    "Message": "Bot token is valid."
  },
  "Channel": {
    "Success": true,
    "Message": "Channel lookup succeeded."
  },
  "SocketMode": {
    "Success": true,
    "Message": "Socket Mode connection succeeded."
  }
}
```

Notes:

- `SlackAppToken` must start with `xapp-`
- `SlackBotToken` must start with `xoxb-`
- `SlackChannelId` is required for configured channel traffic
- direct messages to the bot are also supported once Slack is enabled
- in configured channels, either `SlackMessagePrefix` or an `@bot` mention can trigger the assistant

**Error Responses:**
- `400` -- Invalid verification payload.
- `403` -- Not the owner and not an admin.
- `404` -- Assistant not found.

---

## Documents

Documents are uploaded via a JSON request body that references an ingestion rule. The ingestion rule defines the target S3 bucket, RecallDB collection, and processing configuration. Documents are automatically processed through the ingestion pipeline (storage, text extraction, chunking, embedding). On deletion, the S3 object and all associated RecallDB embeddings are cleaned up.

### PUT /v1.0/documents

Upload a new document using an ingestion rule.

**Auth:** Required

**Request Body:**

```json
{
  "IngestionRuleId": "irule_abc123...",
  "Name": "guide.pdf",
  "OriginalFilename": "guide.pdf",
  "ContentType": "application/pdf",
  "Labels": ["user-guide", "v2"],
  "Tags": { "version": "2.0" },
  "Base64Content": "JVBERi0xLjQK..."
}
```

| Field              | Type               | Required | Description                                                |
|--------------------|--------------------|----------|------------------------------------------------------------|
| `IngestionRuleId`  | string             | Yes      | The ingestion rule that defines processing configuration.  |
| `Name`             | string             | No       | Display name for the document.                             |
| `OriginalFilename` | string             | No       | Original filename of the uploaded file.                    |
| `ContentType`      | string             | No       | MIME type (defaults to `application/octet-stream`).        |
| `Labels`           | string[]           | No       | Per-document labels (merged with rule labels on ingestion).|
| `Tags`             | object             | No       | Per-document tags (merged with rule tags on ingestion).    |
| `Base64Content`    | string             | Yes      | Base64-encoded file content.                               |

**Response (201 Created):**

```json
{
  "Id": "adoc_abc123...",
  "Name": "guide.pdf",
  "OriginalFilename": "guide.pdf",
  "ContentType": "application/pdf",
  "SizeBytes": 1048576,
  "S3Key": "irule_abc123/adoc_abc123/guide.pdf",
  "Status": "Uploaded",
  "StatusMessage": "File uploaded successfully.",
  "IngestionRuleId": "irule_abc123...",
  "BucketName": "kb-documents",
  "CollectionId": "collection-uuid-here",
  "Labels": "[\"user-guide\",\"v2\"]",
  "Tags": "{\"version\":\"2.0\"}",
  "ChunkRecordIds": null,
  "CrawlPlanId": null,
  "CrawlOperationId": null,
  "SourceUrl": null,
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Document Status Values:**

| Status                  | Description                                      |
|-------------------------|--------------------------------------------------|
| `Pending`               | Document is queued for processing.                |
| `Uploading`             | File is being uploaded to object storage.         |
| `Uploaded`              | File successfully uploaded to object storage.     |
| `TypeDetecting`         | Detecting the document type/format.               |
| `TypeDetectionSuccess`  | Document type detected successfully.              |
| `TypeDetectionFailed`   | Failed to detect document type.                   |
| `Processing`            | Extracting text content from the document.        |
| `ProcessingChunks`      | Splitting extracted text into chunks.             |
| `Summarizing`           | Summarizing document content via LLM.             |
| `StoringEmbeddings`     | Computing and storing vector embeddings.          |
| `Completed`             | Document fully processed and ready for retrieval. |
| `Failed`                | Processing failed (see `StatusMessage`).          |

**Error Responses:**
- `400` -- `IngestionRuleId` is required; or `Base64Content` is missing/invalid.
- `404` -- Ingestion rule not found.
- `503` -- S3 storage is not configured.

### GET /v1.0/documents

List documents with pagination and optional filtering.

**Auth:** Required

**Query Parameters:** See [Pagination](#pagination), plus:

| Parameter      | Type   | Default | Description                                          |
|----------------|--------|---------|------------------------------------------------------|
| `bucketName`   | string | null    | Filter documents by S3 bucket name.                  |
| `collectionId` | string | null    | Filter documents by RecallDB collection identifier.  |

**Response (200 OK):** Paginated envelope containing `AssistantDocument` objects.

### GET /v1.0/documents/{documentId}

Retrieve a single document record by ID.

**Auth:** Required

**Response (200 OK):** An `AssistantDocument` object.

**Error Responses:**
- `404` -- Document not found.

### DELETE /v1.0/documents/{documentId}

Delete a document, its S3 object, and all associated RecallDB embeddings.

**Auth:** Required

**Response:** `204 No Content`

**Error Responses:**
- `404` -- Document not found.

### POST /v1.0/documents/delete

Bulk delete multiple documents, their S3 objects, and all associated RecallDB embeddings.

**Auth:** Required

**Request Body:**

```json
{
  "DocumentIds": ["adoc_abc123...", "adoc_def456...", "adoc_ghi789..."]
}
```

| Field         | Type     | Required | Description                            |
|---------------|----------|----------|----------------------------------------|
| `DocumentIds` | string[] | Yes      | List of document IDs to delete.        |

**Response:** `204 No Content`

**Error Responses:**
- `400` -- Invalid request.

### GET /v1.0/documents/{documentId}/processing-log

Retrieve the processing log for a document. The log contains details from the ingestion pipeline (text extraction, chunking, embedding) for debugging and monitoring.

**Auth:** Required

**Response (200 OK):**

```json
{
  "DocumentId": "adoc_abc123...",
  "Log": "2026-01-01T12:00:00Z [INFO] Starting document processing...\n2026-01-01T12:00:01Z [INFO] Text extraction complete: 15 cells...\n..."
}
```

**Error Responses:**
- `404` -- Document not found.

### HEAD /v1.0/documents/{documentId}

Check whether a document exists.

**Auth:** Required

**Response:**
- `200 OK` -- Document exists.
- `404 Not Found` -- Document does not exist.

### GET /v1.0/documents/{documentId}/download

Download the original document file from S3 storage.

**Auth:** Required

**Response:**
- `200 OK` -- File data with `Content-Type` from the document record and `Content-Disposition: attachment; filename="<original filename>"`.
- `404 Not Found` -- Document does not exist.
- `500 Internal Server Error` -- Failed to download from storage.

---

## Feedback (Authenticated)

Authenticated users can view and manage feedback for their assistants. Admin users can see all feedback.

### GET /v1.0/feedback

List all feedback records with pagination.

**Auth:** Required

**Query Parameters:** See [Pagination](#pagination). Use `assistantIdFilter` to filter by assistant.

**Response (200 OK):** Paginated envelope containing `AssistantFeedback` objects.

### GET /v1.0/feedback/{feedbackId}

Retrieve a single feedback record by ID.

**Auth:** Required

**Response (200 OK):**

```json
{
  "Id": "afb_abc123...",
  "AssistantId": "asst_abc123...",
  "UserMessage": "What is your return policy?",
  "AssistantResponse": "Our return policy allows returns within 30 days...",
  "Rating": "ThumbsUp",
  "FeedbackText": "Very helpful answer!",
  "MessageHistory": null,
  "CreatedUtc": "2025-01-01T12:00:00Z",
  "LastUpdateUtc": "2025-01-01T12:00:00Z"
}
```

**Error Responses:**
- `404` -- Feedback not found.

### DELETE /v1.0/feedback/{feedbackId}

Delete a feedback record.

**Auth:** Required

**Response:** `204 No Content`

**Error Responses:**
- `404` -- Feedback not found.

---

## History (Authenticated)

Authenticated users can view and manage chat history for their assistants. Admin users can see all history. History entries are created automatically when the `X-Thread-ID` header is provided on chat requests.

In v0.12.0, assistant history records also include provider-agnostic performance telemetry. `TraceId` links the chat history row to request history and logs. `RequestHistoryId` links directly to the captured HTTP request. `PerformanceJson` stores a versioned `AssistantPerformanceTelemetry` payload with per-stage timings, endpoint limiter wait time, request-to-headers timing, time to first token, generation timing, token counts, and provider-native metrics when available.

### GET /v1.0/history

List all chat history records with pagination.

**Auth:** Required

**Query Parameters:** See [Pagination](#pagination). Use `assistantId` to filter by assistant and `threadId` to filter by thread.

**Response (200 OK):** Paginated envelope containing `ChatHistory` objects.

### GET /v1.0/history/{historyId}

Retrieve a single chat history record by ID.

**Auth:** Required

**Response (200 OK):**

```json
{
  "Id": "chist_abc123...",
  "TraceId": "trace_abc123...",
  "RequestHistoryId": "req_abc123...",
  "PerformanceSchemaVersion": 1,
  "PerformanceJson": "{\"SchemaVersion\":1,\"TraceId\":\"trace_abc123...\",\"Stages\":[{\"Name\":\"final_inference\",\"Kind\":\"inference\",\"DurationMs\":890.75,\"ClientTimings\":{\"RequestToHeadersMs\":850.0,\"HeadersToFirstTokenMs\":120.5,\"FirstTokenToLastTokenMs\":770.25},\"ProviderMetrics\":{\"LoadMs\":0,\"PromptEvalMs\":110.0,\"GenerationMs\":770.25}}]}",
  "TenantId": "default",
  "ThreadId": "thr_abc123...",
  "AssistantId": "asst_abc123...",
  "CollectionId": "collection-uuid",
  "UserMessageUtc": "2025-01-01T12:00:00Z",
  "UserMessage": "How do I reset my password?",
  "RetrievalStartUtc": "2025-01-01T12:00:00.100Z",
  "RetrievalDurationMs": 45.23,
  "RetrievalGateDecision": "RETRIEVE",
  "RetrievalGateDurationMs": 120.50,
  "QueryRewriteResult": null,
  "QueryRewriteDurationMs": 0,
  "RerankDurationMs": 0,
  "RerankInputCount": 0,
  "RerankOutputCount": 0,
  "RetrievalContext": "Chunk 1: To reset your password...",
  "PromptSentUtc": "2025-01-01T12:00:00.150Z",
  "PromptTokens": 1250,
  "CompletionTokens": 87,
  "TokensPerSecondOverall": 97.65,
  "TokensPerSecondGeneration": 145.00,
  "EndpointResolutionDurationMs": 45.12,
  "CompactionDurationMs": 0,
  "InferenceConnectionDurationMs": 850.00,
  "TimeToFirstTokenMs": 120.50,
  "TimeToLastTokenMs": 890.75,
  "MetadataFilter": null,
  "Origin": null,
  "AssistantResponse": "To reset your password, navigate to Settings > Security...",
  "CreatedUtc": "2025-01-01T12:00:00Z",
  "LastUpdateUtc": "2025-01-01T12:00:00Z"
}
```

**Field Descriptions:**

| Field                  | Type     | Description                                                  |
|------------------------|----------|--------------------------------------------------------------|
| `Id`                   | string   | Unique identifier (chist_ prefix).                           |
| `TraceId`              | string   | Correlation identifier shared by chat history, request history, telemetry events, and logs. |
| `RequestHistoryId`     | string   | Linked request-history record ID when the chat request was captured. |
| `PerformanceSchemaVersion` | int  | Version number for the `PerformanceJson` payload.            |
| `PerformanceJson`      | string   | Serialized provider-agnostic `AssistantPerformanceTelemetry` payload. Null for old rows or rows without telemetry. |
| `TenantId`             | string   | Tenant that owns the history row.                            |
| `ThreadId`             | string   | Conversation thread identifier (thr_ prefix).                |
| `AssistantId`          | string   | The assistant that handled the conversation.                 |
| `CollectionId`         | string   | RecallDB collection used for retrieval (may be null).        |
| `UserMessageUtc`       | datetime | UTC timestamp when the user message was received.            |
| `UserMessage`          | string   | The user's message text.                                     |
| `RetrievalStartUtc`    | datetime | UTC timestamp when RAG retrieval started (null if no RAG).   |
| `RetrievalDurationMs`  | double   | RAG retrieval duration in milliseconds.                      |
| `RetrievalGateDecision`| string   | Retrieval gate decision: `RETRIEVE`, `SKIP`, or null (gate disabled). |
| `RetrievalGateDurationMs` | double | Duration of the retrieval gate LLM call in milliseconds.    |
| `QueryRewriteResult`   | string?  | Newline-separated list of rewritten query prompts returned by the query rewrite LLM call. Null when query rewrite is disabled or not triggered. |
| `QueryRewriteDurationMs` | double | Duration of the query rewrite LLM call in milliseconds.      |
| `RerankDurationMs`     | double   | Duration of the re-ranking LLM call in milliseconds.         |
| `RerankInputCount`     | int      | Number of chunks sent to the re-ranker.                      |
| `RerankOutputCount`    | int      | Number of chunks that survived re-ranking.                   |
| `RetrievalContext`     | string   | Retrieved context chunks (null if no RAG).                   |
| `PromptSentUtc`        | datetime | UTC timestamp when the prompt was sent to the model.         |
| `PromptTokens`         | int      | Estimated prompt token count sent to the model.              |
| `CompletionTokens`     | int      | Estimated completion token count from the model's response.  |
| `TokensPerSecondOverall` | double | Tokens per second (overall): CompletionTokens / (TimeToLastTokenMs / 1000). End-to-end throughput from prompt sent to last token. |
| `TokensPerSecondGeneration` | double | Tokens per second (generation only): CompletionTokens / ((TimeToLastTokenMs - TimeToFirstTokenMs) / 1000). Pure generation throughput excluding prompt processing. |
| `EndpointResolutionDurationMs` | double | Time to resolve inference endpoint via Partio (ms). 0 if not configured. |
| `CompactionDurationMs` | double   | Time spent in conversation compaction (ms). 0 if skipped.    |
| `InferenceConnectionDurationMs` | double | Time from HTTP request sent to response headers received (ms). Includes network latency and model loading. |
| `TimeToFirstTokenMs`   | double   | Time to first token from the model in milliseconds.          |
| `TimeToLastTokenMs`    | double   | Time to last token from the model in milliseconds.           |
| `MetadataFilter`       | string   | JSON-serialized metadata filter applied during retrieval (null if none). |
| `Origin`               | string   | Origin of the chat request (e.g. `web`, `slack`, `api`). Null if not set. |
| `AssistantResponse`    | string   | The assistant's full response text.                          |

**PerformanceJson Contract:**

`PerformanceJson` is serialized JSON. Clients can parse it as:

```json
{
  "SchemaVersion": 1,
  "TraceId": "trace_abc123...",
  "ChatHistoryId": "chist_abc123...",
  "RequestHistoryId": "req_abc123...",
  "WallTimeMs": 890.75,
  "CreatedUtc": "2025-01-01T12:00:00Z",
  "Stages": [
    {
      "Name": "final_inference",
      "Kind": "inference",
      "Sequence": 70,
      "EndpointId": "cep_abc123...",
      "EndpointName": "local-gemma",
      "EndpointType": "inference",
      "Provider": "Ollama",
      "ApiFormat": "Ollama",
      "Model": "gemma3:4b",
      "DurationMs": 890.75,
      "Success": true,
      "HttpStatusCode": 200,
      "ClientTimings": {
        "EndpointLimiterWaitMs": 0,
        "RequestToHeadersMs": 850,
        "HeadersToFirstTokenMs": 120.5,
        "FirstTokenToLastTokenMs": 770.25,
        "TotalMs": 890.75
      },
      "Tokens": {
        "Input": 1250,
        "Output": 87,
        "Total": 1337
      },
      "ProviderMetrics": {
        "QueueMs": null,
        "LoadMs": 0,
        "PromptEvalMs": 110,
        "GenerationMs": 770.25,
        "TotalMs": 880.25,
        "TokensPerSecond": 112.9,
        "RequestId": null
      }
    }
  ]
}
```

Known stage names include `retrieval_gate`, `query_rewrite`, `retrieval`, `rerank`, `endpoint_resolution`, `context_compaction`, and `final_inference`. Provider-specific fields that are not available are returned as null or omitted; they are not coerced to zero.

**Error Responses:**
- `404` -- History entry not found.

### DELETE /v1.0/history/{historyId}

Delete a chat history record.

**Auth:** Required

**Response:** `204 No Content`

**Error Responses:**
- `404` -- History entry not found.

---

## Threads (Authenticated)

### GET /v1.0/threads

List distinct conversation threads grouped from chat history records.

**Auth:** Required

**Query Parameters:** Use `assistantId` to filter by assistant.

**Response (200 OK):**

```json
[
  {
    "ThreadId": "thr_abc123...",
    "AssistantId": "asst_abc123...",
    "FirstMessageUtc": "2025-01-01T12:00:00Z",
    "LastMessageUtc": "2025-01-01T12:05:00Z",
    "TurnCount": 5
  }
]
```

| Field             | Type     | Description                                  |
|-------------------|----------|----------------------------------------------|
| `ThreadId`        | string   | Conversation thread identifier.              |
| `AssistantId`     | string   | The assistant for this thread.               |
| `FirstMessageUtc` | datetime | Timestamp of the first message in the thread.|
| `LastMessageUtc`  | datetime | Timestamp of the last message in the thread. |
| `TurnCount`       | int      | Number of conversation turns in the thread.  |

---

## Assistant Analytics (Authenticated)

Assistant analytics summarizes the v0.12.0 chat/request telemetry for a single assistant without returning raw prompts, responses, request bodies, response bodies, headers, or secrets. Analytics are scoped to surviving Assistant History rows; Request History is joined only as supporting timing/status telemetry, so deleting assistant history removes those turns from Assistant Analytics while leaving the audit log intact. Authenticated assistant owners can view their own assistant analytics. Tenant admins can view analytics for assistants in their tenant. Global admins can view all tenants.

All analytics endpoints support these range parameters:

| Parameter       | Type     | Default   | Description |
|-----------------|----------|-----------|-------------|
| `range`         | string   | `lastDay` | Preset range: `lastHour`, `lastDay`, `lastWeek`, or `lastMonth`. |
| `startUtc`      | datetime | null      | Explicit UTC start. Must be paired with `endUtc`; overrides `range`. |
| `endUtc`        | datetime | null      | Explicit UTC end. Must be paired with `startUtc`; overrides `range`. |
| `bucketSeconds` | int      | automatic | Optional bucket width. The server caps responses to 240 buckets. |

The resolved `Range` object is returned in every response with `RangeId`, `StartUtc`, `EndUtc`, `BucketSeconds`, and `BucketCount`.

### GET /v1.0/assistants/{assistantId}/analytics/overview

Returns one summary row for the selected assistant and range.

**Response fields include:** `RequestCount`, `SuccessCount`, `FailureCount`, `SuccessRate`, `AverageDurationMs`, `P50DurationMs`, `P90DurationMs`, `P95DurationMs`, `P99DurationMs`, `MaxDurationMs`, `TelemetryEventCount`, `RequestsWithTelemetry`, `TelemetryCoverageRate`, `DominantStage`, `TopEndpointId`, `TopEndpointName`, `TopEndpointProvider`, `TopEndpointModel`, `FeedbackCount`, `ThumbsUpCount`, `ThumbsDownCount`, and `NegativeFeedbackRate`.

### GET /v1.0/assistants/{assistantId}/analytics/timeseries

Returns chart-ready time series. Optional filters:

| Parameter    | Type   | Description |
|--------------|--------|-------------|
| `metrics`    | string | Comma-separated metric names. Omit to return all supported metrics. |
| `stage`      | string | Filter performance events to a stage such as `retrieval`, `query_rewrite`, `rerank`, or `final_inference`. |
| `endpointId` | string | Filter to one endpoint. |
| `model`      | string | Filter to one model name. |

Supported metric names include `request_count`, `success_count`, `failure_count`, `success_rate`, `avg_duration_ms`, `p95_duration_ms`, `p99_duration_ms`, `max_duration_ms`, `endpoint_limiter_wait_avg_ms`, `endpoint_limiter_wait_p95_ms`, `endpoint_wait_calls`, `provider_load_avg_ms`, `provider_generation_avg_ms`, `provider_tokens_per_second_avg`, `input_tokens`, `output_tokens`, `total_tokens`, `retrieval_query_count_avg`, `chunks_output_avg`, `query_rewrite_calls`, `rerank_calls`, and `final_inference_calls`.

### GET /v1.0/assistants/{assistantId}/analytics/stages

Returns per-bucket stage summaries with `Stage`, `Kind`, `Calls`, `Failures`, `SkippedCount`, `AverageDurationMs`, `P95DurationMs`, and `MaxDurationMs`.

Optional filters: `stage`, `endpointId`, `endpointType`, and `model`.

### GET /v1.0/assistants/{assistantId}/analytics/endpoints

Returns ranked endpoint/model/provider summaries. Optional filters: `stage`, `endpointId`, `endpointType`, `model`, and `limit` (default `25`, max `250`).

Each summary includes endpoint metadata, call/failure counts, duration percentiles, limiter wait, request-to-headers timing, provider load/generation timing, average tokens per second, and total input/output tokens.

### GET /v1.0/assistants/{assistantId}/analytics/slowest

Returns the slowest request-history rows in the selected range, optionally filtered by `stage`, `endpointId`, `endpointType`, `model`, and `limit` (default `25`, max `250`).

Rows include `RequestHistoryId`, `ChatHistoryId`, `TraceId`, `CreatedUtc`, `StatusCode`, `Success`, `DurationMs`, `RequestPath`, and dominant-stage endpoint/model metadata.

### GET /v1.0/assistants/{assistantId}/analytics/feedback

Returns feedback totals and per-bucket feedback counts: `ThumbsUpCount`, `ThumbsDownCount`, `UnknownCount`, `TotalCount`, and `NegativeRate`.

---

## Request History (Admin Or Tenant Admin)

Request history captures HTTP request and response metadata for AssistantHub system APIs and assistant-facing APIs. Global admins can view all tenants; tenant admins are restricted to their tenant.

### GET /v1.0/requesthistory

List request-history entries.

**Auth:** Required (global admin or tenant admin)

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `maxResults` | int | Maximum results to return. |
| `continuationToken` | string | Continuation token from a prior response. |
| `ordering` | string | `CreatedDescending` or `CreatedAscending`. |
| `startUtc` | datetime | Inclusive UTC start time filter. |
| `endUtc` | datetime | Inclusive UTC end time filter. |
| `method` | string | Filter by HTTP method. |
| `path` | string | Filter by request-path substring. |
| `statusCode` | int | Filter by HTTP response status. |
| `success` | bool | Filter to success or failure traffic. |
| `tenantId` | string | Global-admin-only tenant filter. |
| `assistantId` | string | Filter by assistant identifier. |
| `threadId` | string | Filter by thread identifier. |
| `requestType` | string | `SystemApi` or `AssistantApi`. |
| `sourceType` | string | `dashboard`, `api`, `public`, or `public-assistant`. |
| `search` | string | Free-text search across request path, URL, and principal fields. |

**Response (200 OK):** Paginated envelope containing lightweight `RequestHistoryEntry` objects without requiring full body hydration in the grid path.

### GET /v1.0/requesthistory/summary

Summarize request-history activity into time buckets for charts and dashboards.

**Auth:** Required (global admin or tenant admin)

**Query Parameters:** Same filters as `GET /v1.0/requesthistory`, plus:

| Parameter | Type | Description |
|-----------|------|-------------|
| `bucketMinutes` | int | Width of each time bucket in minutes. |

**Response (200 OK):**

```json
{
  "TotalCount": 42,
  "TotalSuccess": 38,
  "TotalFailure": 4,
  "AverageDurationMs": 128.4,
  "Buckets": [
    {
      "BucketStartUtc": "2025-01-01T12:00:00Z",
      "BucketEndUtc": "2025-01-01T13:00:00Z",
      "RequestCount": 12,
      "SuccessCount": 11,
      "FailureCount": 1,
      "AverageDurationMs": 110.2
    }
  ]
}
```

### GET /v1.0/requesthistory/{requestId}

Get a fully hydrated request-history entry by ID.

**Auth:** Required (global admin or tenant admin)

**Response (200 OK):**

```json
{
  "Id": "req_abc123...",
  "TraceId": "trace_abc123...",
  "ChatHistoryId": "chist_abc123...",
  "TenantId": "default",
  "AssistantId": "asst_abc123...",
  "ThreadId": "thr_abc123...",
  "RequestType": "AssistantApi",
  "SourceType": "public-assistant",
  "HttpMethod": "POST",
  "RequestPath": "/v1.0/assistants/asst_abc123/chat",
  "RequestUrl": "/v1.0/assistants/asst_abc123/chat",
  "StatusCode": 200,
  "Success": true,
  "DurationMs": 842.6,
  "RequestHeaders": {
    "Content-Type": "application/json",
    "X-Thread-ID": "thr_abc123..."
  },
  "ResponseHeaders": {
    "Content-Type": "text/event-stream"
  },
  "RequestBody": "{\"messages\":[{\"role\":\"user\",\"content\":\"Hello\"}]}",
  "ResponseBody": "[server-sent events stream omitted]",
  "CreatedUtc": "2025-01-01T12:00:00Z",
  "LastUpdateUtc": "2025-01-01T12:00:00Z"
}
```

`TraceId` and `ChatHistoryId` are populated for assistant chat requests when a chat history row is produced. The dashboard uses `ChatHistoryId` to load the linked chat row and display the same v0.12.0 performance timing breakdown in the request-history detail view.

### GET /v1.0/requesthistory/{requestId}/detail

Alias for the fully hydrated request-history detail view.

**Auth:** Required (global admin or tenant admin)

### DELETE /v1.0/requesthistory/{requestId}

Delete a single request-history entry.

**Auth:** Required (global admin or tenant admin)

**Response:** `204 No Content`

### DELETE /v1.0/requesthistory/bulk

Delete all request-history entries matching the current query filters.

**Auth:** Required (global admin or tenant admin)

**Query Parameters:** Same filters as `GET /v1.0/requesthistory`

**Response (200 OK):**

```json
{
  "DeletedCount": 12
}
```

---

## Models

List available models on the configured inference provider and pull new models (Ollama only).

### GET /v1.0/models

List all models available on the configured inference provider.

**Auth:** Required (any user)

**Response (200 OK):**

```json
[
  {
    "Name": "gemma3:4b",
    "SizeBytes": 3300000000,
    "ModifiedUtc": "2025-06-01T10:00:00Z",
    "OwnedBy": null,
    "PullSupported": true
  }
]
```

**Response Fields:**

| Field           | Type     | Description                                         |
|-----------------|----------|-----------------------------------------------------|
| `Name`          | string   | Model name (e.g. `gemma3:4b`, `gpt-4o`).           |
| `SizeBytes`     | long     | Model size on disk in bytes (0 for cloud providers). |
| `ModifiedUtc`   | datetime | Last modified timestamp (UTC).                      |
| `OwnedBy`       | string   | Model owner when supplied by the provider.          |
| `PullSupported` | bool     | Whether the provider supports pulling new models.   |

**Error Responses:**
- `500` -- Internal error.

### POST /v1.0/models/pull

Pull (download) a model on the configured inference provider. Only supported for Ollama.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Name": "gemma3:4b"
}
```

**Response (202 Accepted):**

```json
{
  "ModelName": "gemma3:4b",
  "Status": "starting"
}
```

The pull operation runs asynchronously. Use `GET /v1.0/models/pull/status` to poll for progress.

**Error Responses:**
- `400` -- Model name is required, or pull is not supported by the configured provider.
- `403` -- Not an admin user.
- `500` -- Internal error.

### GET /v1.0/models/pull/status

Poll the status of a model pull operation.

**Auth:** Required (admin only)

**Response (200 OK):**

```json
{
  "ModelName": "gemma3:4b",
  "Status": "downloading",
  "StartedUtc": "2026-01-01T12:00:00Z",
  "IsComplete": false,
  "HasError": false,
  "ErrorMessage": null,
  "CurrentStep": "pulling manifest",
  "TotalSize": 3300000000,
  "CompletedSize": 1200000000
}
```

| Field           | Type     | Description                                         |
|-----------------|----------|-----------------------------------------------------|
| `ModelName`     | string   | Name of the model being pulled.                     |
| `Status`        | string   | Current status of the pull operation.               |
| `StartedUtc`    | datetime | UTC timestamp when the pull started.                |
| `IsComplete`    | bool     | Whether the pull has finished.                      |
| `HasError`      | bool     | Whether the pull encountered an error.              |
| `ErrorMessage`  | string   | Error details if `HasError` is true; null otherwise.|
| `CurrentStep`   | string   | Current step in the pull process (null if idle).    |
| `TotalSize`     | long?    | Total download size in bytes (null if unknown).     |
| `CompletedSize` | long?    | Bytes downloaded so far (null if unknown).          |

**Error Responses:**
- `403` -- Not an admin user.
- `404` -- No pull operation in progress.

### DELETE /v1.0/models/{modelName}

Delete (remove) a model from the configured inference provider. Only supported for Ollama.

**Auth:** Required (global admin only)

**Response:** `204 No Content`

**Error Responses:**
- `403` -- Not a global admin user.
- `404` -- Model not found.
- `500` -- Internal error.

---

## Public Endpoints

These endpoints do not require authentication and are intended for end-user-facing integrations.

### GET /v1.0/assistants/{assistantId}/public

Retrieve public information about an assistant. Returns basic details and appearance settings for active assistants.

**Auth:** None

**Response (200 OK):**

```json
{
  "Id": "asst_abc123...",
  "Name": "Customer Support Bot",
  "Description": "Answers questions about our product documentation.",
  "Title": "My Support Bot",
  "LogoUrl": "https://example.com/logo.png",
  "FaviconUrl": "https://example.com/favicon.ico"
}
```

| Field        | Type   | Description                                                                      |
|--------------|--------|----------------------------------------------------------------------------------|
| `Id`         | string | The assistant's unique identifier.                                               |
| `Name`       | string | Display name of the assistant.                                                   |
| `Description`| string | Description of the assistant (may be null).                                      |
| `Title`      | string | Custom chat window heading (null if not set; falls back to Name on the client).  |
| `LogoUrl`    | string | URL for the chat logo image, max 192x192 (null uses default AssistantHub logo).  |
| `FaviconUrl` | string | URL for the browser tab favicon (null uses default AssistantHub favicon).         |

**Error Responses:**
- `404` -- Assistant not found or not active.

### POST /v1.0/assistants/{assistantId}/threads

Create a new conversation thread for an assistant. Returns a thread ID that can be passed as the `X-Thread-ID` header on subsequent chat requests to enable history tracking.

**Auth:** None

**Response (201 Created):**

```json
{
  "ThreadId": "thr_abc123..."
}
```

**Error Responses:**
- `404` -- Assistant not found or not active.

### GET /v1.0/assistants/{assistantId}/labels/distinct

Retrieve all distinct label values for the collection associated with an assistant. Intended for populating filter controls in public chat UIs.

**Auth:** None

**Response (200 OK):**

```json
["finance", "quarterly-report", "internal"]
```

Returns a JSON array of unique label strings. The endpoint looks up the assistant's configured `CollectionId` and proxies to RecallDB.

**Error Responses:**
- `404` -- Assistant not found or not active.

### GET /v1.0/assistants/{assistantId}/tags/distinct

Retrieve all distinct tag keys for the collection associated with an assistant. Intended for populating filter controls in public chat UIs.

**Auth:** None

**Response (200 OK):**

```json
["department", "year", "status"]
```

Returns a JSON array of unique tag key strings. The endpoint looks up the assistant's configured `CollectionId` and proxies to RecallDB.

**Error Responses:**
- `404` -- Assistant not found or not active.

### GET /v1.0/assistants/{assistantId}/threads/{threadId}/history

Retrieve conversation history for a specific thread. Returns all chat history entries for the given thread in chronological order.

**Auth:** None

**Response (200 OK):** A list of `ChatHistory` objects for the thread.

**Error Responses:**
- `404` -- Assistant not found, not active, or thread not found.

### POST /v1.0/assistants/{assistantId}/chat

Send a chat completion request using the OpenAI-compatible format. The server retrieves relevant document chunks via vector similarity search, injects them into the system message, and forwards the conversation to the configured LLM.

If the assistant has `Streaming` enabled, the response is delivered as Server-Sent Events (SSE). Otherwise, a standard JSON response is returned.

When the conversation history approaches the context window limit, older messages are automatically summarized (compacted). During streaming, a status event with `"status": "Compacting the conversation..."` is sent.

**Auth:** None

**Request Headers:**

| Header         | Required | Description                                                                      |
|----------------|----------|----------------------------------------------------------------------------------|
| `X-Thread-ID`  | No       | Thread ID from `POST /v1.0/assistants/{assistantId}/threads`. When provided, the server records timing metrics and conversation history for this turn. |

**Request Body:**

```json
{
  "model": "gpt-4o",
  "messages": [
    { "role": "system", "content": "You are a helpful assistant." },
    { "role": "user", "content": "How do I reset my password?" }
  ],
  "temperature": 0.7,
  "top_p": 1.0,
  "max_tokens": 4096,
  "stream": false,
  "metadata_filter": {
    "required_labels": ["finance", "quarterly-report"],
    "excluded_labels": ["draft"],
    "required_tags": [
      { "key": "department", "condition": "Equals", "value": "accounting" }
    ],
    "excluded_tags": [
      { "key": "status", "condition": "Equals", "value": "archived" }
    ]
  }
}
```

| Field             | Type   | Required | Description                                                    |
|-------------------|--------|----------|----------------------------------------------------------------|
| `model`           | string | No       | Model override (otherwise uses the model configured on the assistant's managed inference endpoint). |
| `messages`        | array  | Yes      | Array of message objects with `role` and `content`.            |
| `temperature`     | double | No       | Sampling temperature override (0.0-2.0).                       |
| `top_p`           | double | No       | Top-p override (0.0-1.0).                                      |
| `max_tokens`      | int    | No       | Max tokens override.                                           |
| `stream`          | bool   | No       | Ignored; streaming is controlled by the assistant `Streaming` setting. |
| `metadata_filter` | object | No       | Metadata filter to restrict retrieval (see below). Merged with assistant-level defaults. |

**Metadata Filter Object:**

| Field             | Type   | Description                                                    |
|-------------------|--------|----------------------------------------------------------------|
| `required_labels` | array  | Labels that must be present on retrieved documents.            |
| `excluded_labels` | array  | Labels that must NOT be present on retrieved documents.        |
| `required_tags`   | array  | Tag conditions that must all match. Each has `key`, `condition`, `value`. |
| `excluded_tags`   | array  | Tag conditions that must NOT match. Same structure as required_tags. |

**Tag Condition Operators:** `Equals`, `NotEquals`, `GreaterThan`, `LessThan`, `Contains`, `ContainsNot`, `StartsWith`, `EndsWith`, `IsNull`, `IsNotNull`

When `metadata_filter` is omitted or null, no filtering is applied. If the assistant also has default filters configured, they are merged with request-level filters (unions of required/excluded lists).

**Non-Streaming Response (200 OK):**

```json
{
  "id": "chatcmpl-abc123...",
  "object": "chat.completion",
  "created": 1700000000,
  "model": "gpt-4o",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "To reset your password, navigate to Settings > Security and click 'Reset Password'..."
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 50,
    "completion_tokens": 30,
    "total_tokens": 80
  }
}
```

The response may also include `retrieval` (when RAG is enabled) and `citations` (when citations are enabled) fields:

| Field | Type | Description |
|-------|------|-------------|
| `citations` | object \| null | Citation metadata (only when `EnableCitations` is true and RAG is active) |
| `citations.sources` | array | Source documents provided as context, each with `index`, `document_id`, `document_name`, `content_type`, `score`, `excerpt`, `download_url` |
| `citations.referenced_indices` | array of int | 1-based indices from `sources` that the model actually cited in its response |

**Streaming Response (200 OK, `Content-Type: text/event-stream`):**

When `Streaming` is enabled in assistant settings, the response is an SSE stream:

```
data: {"id":"chatcmpl-abc123...","object":"chat.completion.chunk","created":1700000000,"model":"gpt-4o","choices":[{"index":0,"delta":{"role":"assistant"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123...","object":"chat.completion.chunk","created":1700000000,"model":"gpt-4o","choices":[{"index":0,"delta":{"content":"To"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123...","object":"chat.completion.chunk","created":1700000000,"model":"gpt-4o","choices":[{"index":0,"delta":{"content":" reset"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123...","object":"chat.completion.chunk","created":1700000000,"model":"gpt-4o","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}

data: [DONE]
```

The final chunk (with `finish_reason: "stop"`) includes `usage`, `retrieval` (if RAG enabled),
and `citations` (if citations enabled) fields.

#### Citations

When `EnableCitations` is `true` (and RAG is active), the system:

1. Labels each retrieved context chunk with a bracket index `[1]`, `[2]`, etc. and its source document name
2. Instructs the model to cite sources using bracket notation
3. After inference, scans the response for bracket references and validates them against the source manifest
4. Returns a `citations` object with the full source manifest and the validated referenced indices

**Example response fragment:**
```json
{
  "citations": {
    "sources": [
      {
        "index": 1,
        "document_id": "adoc_abc123",
        "document_name": "Q3 Earnings Report.pdf",
        "content_type": "application/pdf",
        "score": 0.87,
        "excerpt": "Revenue grew 15% year-over-year to $4.2B...",
        "download_url": "/v1.0/assistants/asst_abc123/documents/adoc_abc123/download"
      }
    ],
    "referenced_indices": [1]
  }
}
```

**Notes:**
- `referenced_indices` only contains indices that appear as `[N]` in the response text AND exist in the source manifest
- Invalid references (e.g., `[99]` when only 3 sources exist) are silently dropped
- `sources` always contains all retrieved chunks, not just the ones that were cited
- `download_url` is populated based on `CitationLinkMode`: `null` for `None`, `/v1.0/documents/{id}/download` (authenticated) for `Authenticated`, or `/v1.0/assistants/{assistantId}/documents/{id}/download` (unauthenticated, server-proxied) for `Public`

**Error Responses:**
- `400` -- At least one message is required.
- `404` -- Assistant not found or not active.
- `500` -- Assistant settings not configured.
- `502` -- Inference failed.

### POST /v1.0/assistants/{assistantId}/generate

Lightweight inference-only endpoint. Sends messages directly to the configured LLM without RAG retrieval, system prompt injection, conversation compaction, or chat history persistence. Useful for auxiliary tasks like title generation where the full chat pipeline is unnecessary.

**Auth:** None

**Request Body:**

```json
{
  "model": "gpt-4o",
  "messages": [
    { "role": "user", "content": "What is the capital of France?" },
    { "role": "assistant", "content": "The capital of France is Paris." },
    { "role": "user", "content": "Generate a short title (max 6 words) for this conversation. Reply with ONLY the title text, nothing else." }
  ],
  "temperature": 0.7,
  "top_p": 1.0,
  "max_tokens": 4096
}
```

| Field         | Type   | Required | Description                                                    |
|---------------|--------|----------|----------------------------------------------------------------|
| `model`       | string | No       | Model override (otherwise uses the model configured on the assistant's managed inference endpoint). |
| `messages`    | array  | Yes      | Array of message objects with `role` and `content`.            |
| `temperature` | double | No       | Sampling temperature override (0.0-2.0).                       |
| `top_p`       | double | No       | Top-p override (0.0-1.0).                                      |
| `max_tokens`  | int    | No       | Max tokens override.                                           |

**Response (200 OK):**

```json
{
  "id": "chatcmpl-abc123...",
  "object": "chat.completion",
  "created": 1700000000,
  "model": "gpt-4o",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "European Capital Cities"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 50,
    "completion_tokens": 5,
    "total_tokens": 55
  }
}
```

**Error Responses:**
- `400` -- At least one message is required.
- `404` -- Assistant not found or not active.
- `500` -- Assistant settings not configured.
- `502` -- Inference failed.

### GET /v1.0/assistants/{assistantId}/documents/{documentId}/download

Public document download endpoint for citation linking. Proxies the file from S3 storage through the server. Only available when the assistant's `CitationLinkMode` is `Public`.

**Auth:** None (gated by `CitationLinkMode` setting)

**Response:**
- `200 OK` -- File data with `Content-Type` from the document record and `Content-Disposition: attachment; filename="<original filename>"`.
- `403 Forbidden` -- Assistant's `CitationLinkMode` is not `Public`.
- `404 Not Found` -- Assistant, document, or S3 object does not exist.
- `500 Internal Server Error` -- Failed to download from storage.

### POST /v1.0/assistants/{assistantId}/compact

Force conversation compaction. Summarizes the provided message history into a shorter form to free up context window space. Useful for long conversations where the client wants to explicitly trigger compaction rather than waiting for automatic compaction during chat.

**Auth:** None

**Request Body:**

```json
{
  "messages": [
    { "role": "user", "content": "What is machine learning?" },
    { "role": "assistant", "content": "Machine learning is a subset of artificial intelligence..." },
    { "role": "user", "content": "How does supervised learning work?" },
    { "role": "assistant", "content": "Supervised learning uses labeled training data..." }
  ],
  "model": "gemma3:4b",
  "temperature": 0.7,
  "max_tokens": 4096
}
```

| Field         | Type   | Required | Description                                                    |
|---------------|--------|----------|----------------------------------------------------------------|
| `messages`    | array  | Yes      | Array of message objects with `role` and `content`.            |
| `model`       | string | No       | Model override (otherwise uses the model configured on the assistant's managed inference endpoint). |
| `temperature` | double | No       | Sampling temperature override (0.0-2.0).                       |
| `top_p`       | double | No       | Top-p override (0.0-1.0).                                      |
| `max_tokens`  | int    | No       | Max tokens override.                                           |

**Response (200 OK):**

```json
{
  "messages": [
    { "role": "user", "content": "What is machine learning?" },
    { "role": "assistant", "content": "Previous conversation summary: We discussed machine learning fundamentals and supervised learning techniques..." }
  ],
  "usage": {
    "promptTokens": 250,
    "totalTokens": 350,
    "contextWindow": 8192
  }
}
```

**Error Responses:**
- `400` -- At least one message is required.
- `404` -- Assistant not found or not active.
- `500` -- Assistant settings not configured.
- `502` -- Inference failed during compaction.

### POST /v1.0/assistants/{assistantId}/feedback

Submit feedback for an assistant response.

**Auth:** None

**Request Body:**

```json
{
  "AssistantId": "asst_abc123...",
  "UserMessage": "How do I reset my password?",
  "AssistantResponse": "To reset your password, navigate to Settings...",
  "Rating": "ThumbsUp",
  "FeedbackText": "This was exactly what I needed!",
  "MessageHistory": "[{\"role\":\"user\",\"content\":\"How do I reset my password?\"},{\"role\":\"assistant\",\"content\":\"To reset your password, navigate to Settings...\"}]"
}
```

| Field              | Type   | Required | Description                                                    |
|--------------------|--------|----------|----------------------------------------------------------------|
| `AssistantId`      | string | Yes      | The assistant this feedback is for.                            |
| `UserMessage`      | string | No       | The user's message that prompted the response.                 |
| `AssistantResponse`| string | No       | The assistant's response being rated.                          |
| `Rating`           | string | Yes      | Feedback rating: `ThumbsUp` or `ThumbsDown`.                  |
| `FeedbackText`     | string | No       | Optional free-text feedback from the user.                     |
| `MessageHistory`   | string | No       | JSON-serialized conversation history leading to this response. |

**Rating Values:**
- `ThumbsUp`
- `ThumbsDown`

**Response (201 Created):**

```json
{
  "Id": "afb_abc123...",
  "AssistantId": "asst_abc123...",
  "UserMessage": "How do I reset my password?",
  "AssistantResponse": "To reset your password, navigate to Settings...",
  "Rating": "ThumbsUp",
  "FeedbackText": "This was exactly what I needed!",
  "MessageHistory": "[{\"role\":\"user\",\"content\":\"How do I reset my password?\"},{\"role\":\"assistant\",\"content\":\"To reset your password, navigate to Settings...\"}]",
  "CreatedUtc": "2025-01-01T12:00:00Z",
  "LastUpdateUtc": "2025-01-01T12:00:00Z"
}
```

**Error Responses:**
- `400` -- Invalid request body.
- `404` -- Assistant not found or not active.

---

## Crawl Plans (Admin Only)

Manage web crawl plans that define how content is discovered and ingested from external sources.

### PUT /v1.0/crawlplans

Create a new crawl plan.

**Auth:** Required

**Request Body:**

```json
{
  "Name": "Documentation Crawler",
  "RepositoryType": "Web",
  "IngestionSettings": {
    "IngestionRuleId": "irule_abc123..."
  },
  "RepositorySettings": {
    "BaseUrl": "https://docs.example.com",
    "AuthType": "None",
    "MaxDepth": 3
  },
  "Schedule": {
    "Interval": "Days",
    "IntervalCount": 1
  },
  "Filter": {
    "IncludePatterns": ["*.html", "*.htm"],
    "ExcludePatterns": ["*/admin/*"]
  },
  "ProcessAdditions": true,
  "ProcessUpdates": true,
  "ProcessDeletions": false,
  "MaxDrainTasks": 8,
  "RetentionDays": 7
}
```

| Field               | Type   | Default | Description                                                    |
|---------------------|--------|---------|----------------------------------------------------------------|
| `Name`              | string | "My crawl plan" | Display name for the crawl plan.                        |
| `RepositoryType`    | string | Web     | Repository type: `Web`.                                        |
| `IngestionSettings` | object | null    | Ingestion configuration (e.g., `IngestionRuleId`).             |
| `RepositorySettings`| object | null    | Repository-specific settings (e.g., `BaseUrl`, `AuthType`).   |
| `Schedule`          | object | null    | Schedule configuration (`Interval`: `OneTime`, `Minutes`, `Hours`, `Days`, `Weeks`; `IntervalCount`). |
| `Filter`            | object | null    | URL/path filter settings.                                      |
| `ProcessAdditions`  | bool   | true    | Whether to process newly discovered content.                   |
| `ProcessUpdates`    | bool   | true    | Whether to re-process updated content.                         |
| `ProcessDeletions`  | bool   | false   | Whether to delete content removed from source.                 |
| `MaxDrainTasks`     | int    | 8       | Maximum concurrent drain tasks (1-64).                         |
| `RetentionDays`     | int    | 7       | Days to retain crawl operations (0-14).                        |

**Web Authentication Types (`AuthType`):** `None`, `Basic`, `ApiKey`, `BearerToken`

**Response (201 Created):** The created `CrawlPlan` object.

### GET /v1.0/crawlplans

List crawl plans with pagination.

**Auth:** Required

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing `CrawlPlan` objects.

### GET /v1.0/crawlplans/{id}

Retrieve a single crawl plan by ID.

**Auth:** Required

**Response (200 OK):** A `CrawlPlan` object.

**Error Responses:**
- `404` -- Crawl plan not found.

### PUT /v1.0/crawlplans/{id}

Update an existing crawl plan.

**Auth:** Required

**Request Body:** Same format as create.

**Response (200 OK):** The updated `CrawlPlan` object.

**Error Responses:**
- `404` -- Crawl plan not found.

### DELETE /v1.0/crawlplans/{id}

Delete a crawl plan.

**Auth:** Required (admin only)

**Response:** `204 No Content`

**Error Responses:**
- `404` -- Crawl plan not found.

### HEAD /v1.0/crawlplans/{id}

Check whether a crawl plan exists.

**Auth:** Required

**Response:**
- `200 OK` -- Crawl plan exists.
- `404 Not Found` -- Crawl plan does not exist.

### POST /v1.0/crawlplans/{id}/start

Start a crawl operation for the given plan.

**Auth:** Required (admin only)

**Response (200 OK):** The updated `CrawlPlan` object with `State` set to `Running`.

**Error Responses:**
- `404` -- Crawl plan not found.

### POST /v1.0/crawlplans/{id}/stop

Stop a running crawl operation.

**Auth:** Required (admin only)

**Response (200 OK):** The updated `CrawlPlan` object with `State` set to `Stopped`.

**Error Responses:**
- `404` -- Crawl plan not found.

### POST /v1.0/crawlplans/{id}/connectivity

Test connectivity to the crawl plan's target repository.

**Auth:** Required

**Response (200 OK):**

```json
{
  "Success": true
}
```

**Error Responses:**
- `404` -- Crawl plan not found.

### GET /v1.0/crawlplans/{id}/enumerate

Enumerate contents available at the crawl plan's target repository without performing a crawl.

**Auth:** Required

**Response (200 OK):** A list of `CrawledObject` items discovered at the target.

**Error Responses:**
- `404` -- Crawl plan not found.

**Crawl Plan State Values:**

| State     | Description                   |
|-----------|-------------------------------|
| `Stopped` | Crawl plan is not running.    |
| `Running` | Crawl plan is actively crawling. |

---

## Crawl Operations

View and manage crawl operation records created by crawl plan executions.

### GET /v1.0/crawlplans/{planId}/operations

List crawl operations for a plan with pagination.

**Auth:** Required

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing `CrawlOperation` objects.

### GET /v1.0/crawlplans/{planId}/operations/statistics

Get aggregate statistics across all operations for a crawl plan.

**Auth:** Required

**Response (200 OK):**

```json
{
  "LastRun": "2026-01-15T10:00:00Z",
  "NextRun": "2026-01-16T10:00:00Z",
  "FailedRunCount": 1,
  "SuccessfulRunCount": 12,
  "MinRuntimeMs": 5000.0,
  "MaxRuntimeMs": 45000.0,
  "AvgRuntimeMs": 15000.0,
  "ObjectCount": 150,
  "BytesCrawled": 10485760
}
```

### GET /v1.0/crawlplans/{planId}/operations/{id}

Retrieve a single crawl operation by ID.

**Auth:** Required

**Response (200 OK):** A `CrawlOperation` object.

**Error Responses:**
- `404` -- Crawl operation not found.

### GET /v1.0/crawlplans/{planId}/operations/{id}/statistics

Get statistics for a single crawl operation.

**Auth:** Required

**Response (200 OK):** Statistics for the specific operation.

### DELETE /v1.0/crawlplans/{planId}/operations/{id}

Delete a crawl operation record.

**Auth:** Required (admin only)

**Response:** `204 No Content`

**Error Responses:**
- `404` -- Crawl operation not found.

### GET /v1.0/crawlplans/{planId}/operations/{id}/enumeration

Retrieve the enumeration file for a crawl operation (the list of objects discovered during enumeration).

**Auth:** Required

**Response (200 OK):** JSON content of the enumeration file.

**Error Responses:**
- `404` -- Crawl operation or enumeration file not found.

**Crawl Operation State Values:**

| State          | Description                                  |
|----------------|----------------------------------------------|
| `NotStarted`   | Operation has not yet begun.                 |
| `Starting`     | Operation is initializing.                   |
| `Enumerating`  | Discovering content at the target repository.|
| `Retrieving`   | Downloading and processing discovered content.|
| `Success`      | Operation completed successfully.            |
| `Failed`       | Operation failed (see `StatusMessage`).      |
| `Stopped`      | Operation was manually stopped.              |
| `Canceled`     | Operation was canceled.                      |

---

## Eval (Authenticated)

RAG evaluation endpoints for testing assistant quality. Create facts (question/expected-answer pairs), run evaluation batches, and review results.

### PUT /v1.0/eval/facts

Create a new evaluation fact.

**Auth:** Required

**Request Body:**

```json
{
  "AssistantId": "asst_abc123...",
  "Category": "Product Knowledge",
  "Question": "What is the return policy?",
  "ExpectedFacts": "[\"30 days\", \"full refund\", \"original receipt required\"]"
}
```

| Field           | Type   | Required | Description                                              |
|-----------------|--------|----------|----------------------------------------------------------|
| `AssistantId`   | string | Yes      | The assistant to evaluate against.                       |
| `Category`      | string | No       | Category for organizing facts.                           |
| `Question`      | string | No       | The question to ask the assistant.                       |
| `ExpectedFacts` | string | No       | JSON array of expected facts in the response.            |

**Response (201 Created):** The created `EvalFact` object.

**Error Responses:**
- `400` -- AssistantId is required.

### GET /v1.0/eval/facts

List evaluation facts with pagination.

**Auth:** Required

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing `EvalFact` objects.

### GET /v1.0/eval/facts/{factId}

Retrieve a single evaluation fact by ID.

**Auth:** Required

**Response (200 OK):** An `EvalFact` object.

**Error Responses:**
- `404` -- Fact not found.

### PUT /v1.0/eval/facts/{factId}

Update an existing evaluation fact. Only `Category`, `Question`, and `ExpectedFacts` fields are updated; `Id`, `TenantId`, `AssistantId`, and `CreatedUtc` are preserved.

**Auth:** Required

**Request Body:** Same format as create.

**Response (200 OK):** The updated `EvalFact` object.

**Error Responses:**
- `404` -- Fact not found.

### DELETE /v1.0/eval/facts/{factId}

Delete an evaluation fact.

**Auth:** Required

**Response:** `204 No Content`

**Error Responses:**
- `404` -- Fact not found.

### POST /v1.0/eval/runs

Start a new evaluation run. Executes all facts for the specified assistant asynchronously.

**Auth:** Required

**Request Body:**

```json
{
  "AssistantId": "asst_abc123...",
  "JudgePrompt": null
}
```

| Field         | Type   | Required | Description                                              |
|---------------|--------|----------|----------------------------------------------------------|
| `AssistantId` | string | Yes      | The assistant to evaluate.                               |
| `JudgePrompt` | string | No      | Custom judge prompt override. Null uses the default.     |

**Response (201 Created):** The created `EvalRun` object.

### GET /v1.0/eval/runs

List evaluation runs with pagination.

**Auth:** Required

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing `EvalRun` objects.

### GET /v1.0/eval/runs/{runId}

Retrieve a single evaluation run by ID.

**Auth:** Required

**Response (200 OK):**

```json
{
  "Id": "erun_abc123...",
  "TenantId": "ten_abc123...",
  "AssistantId": "asst_abc123...",
  "Status": "Completed",
  "TotalFacts": 10,
  "FactsEvaluated": 10,
  "FactsPassed": 8,
  "FactsFailed": 2,
  "PassRate": 80.0,
  "JudgePrompt": null,
  "StartedUtc": "2026-01-01T12:00:00Z",
  "CompletedUtc": "2026-01-01T12:01:30Z",
  "CreatedUtc": "2026-01-01T12:00:00Z"
}
```

**Eval Run Status Values:** `Pending`, `Running`, `Completed`, `Failed`

**Error Responses:**
- `404` -- Run not found.

### DELETE /v1.0/eval/runs/{runId}

Delete an evaluation run.

**Auth:** Required

**Response:** `204 No Content`

**Error Responses:**
- `404` -- Run not found.

### GET /v1.0/eval/runs/{runId}/results

Retrieve all results for an evaluation run.

**Auth:** Required

**Response (200 OK):** A list of `EvalResult` objects:

```json
[
  {
    "Id": "eres_abc123...",
    "RunId": "erun_abc123...",
    "FactId": "ef_abc123...",
    "Question": "What is the return policy?",
    "ExpectedFacts": "[\"30 days\", \"full refund\"]",
    "LlmResponse": "Our return policy allows returns within 30 days for a full refund...",
    "FactVerdicts": "[{\"Fact\":\"30 days\",\"Pass\":true,\"Reasoning\":\"The response states returns are allowed within 30 days.\"},{\"Fact\":\"full refund\",\"Pass\":true,\"Reasoning\":\"The response mentions a full refund is provided.\"}]",
    "OverallPass": true,
    "DurationMs": 1500,
    "CreatedUtc": "2026-01-01T12:00:05Z"
  }
]
```

### GET /v1.0/eval/results/{resultId}

Retrieve a single evaluation result by ID.

**Auth:** Required

**Response (200 OK):** An `EvalResult` object.

**Error Responses:**
- `404` -- Result not found.

### GET /v1.0/eval/runs/{runId}/stream

Stream evaluation run progress via Server-Sent Events (SSE). Events are sent as each fact is evaluated.

**Auth:** Required

**Event Types:**
- `update` -- Progress update with the current `EvalRun` state.
- `done` -- Run is complete.

**Error Responses:**
- `404` -- Run not found.

### GET /v1.0/eval/judge-prompt/default

Retrieve the default judge prompt template used for evaluation scoring.

**Auth:** None

**Response (200 OK):**

```json
{
  "Prompt": "..."
}
```

---

## Configuration (Admin Only)

Manage server configuration at runtime. Changes are persisted to the `assistanthub.json` settings file on disk.

### GET /v1.0/configuration

Retrieve the current server configuration.

**Auth:** Required (admin only)

**Response (200 OK):** Returns the full `AssistantHubSettings` object including all sections: `Webserver`, `Database`, `S3`, `DocumentAtom`, `Chunking`, `Inference`, `RecallDb`, `ProcessingLog`, `ChatHistory`, and `Logging`.

### PUT /v1.0/configuration

Update the server configuration. The updated settings are saved to disk.

**Auth:** Required (admin only)

**Request Body:** A full or partial `AssistantHubSettings` object. See the [Configuration](#configuration) section in the README for the complete schema.

**Response (200 OK):** The updated `AssistantHubSettings` object.

**Error Responses:**
- `400` -- Invalid request body.
- `403` -- Not an admin user.

---

## Configuration: ChatHistory Settings

Chat history retention is configured in the server settings file under the `ChatHistory` section:

```json
{
  "ChatHistory": {
    "RetentionDays": 7
  }
}
```

| Field           | Type | Default | Description                                                       |
|-----------------|------|---------|-------------------------------------------------------------------|
| `RetentionDays` | int  | 7       | Number of days to retain chat history records. Records older than this are automatically deleted by the background cleanup service (runs every hour). Set to 0 to disable retention (keep records indefinitely). |
