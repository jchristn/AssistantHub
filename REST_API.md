# AssistantHub REST API Reference

Base URL: `http://localhost:8800`

All API endpoints are versioned under `/v1.0/`. Responses use `application/json` content type unless otherwise noted.

## Table of Contents

- [Authentication](#authentication)
- [Error Responses](#error-responses)
- [Pagination](#pagination)
- [Health](#health)
- [Tenants (Global Admin Only)](#tenants-global-admin-only)
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
- [Models](#models)
- [Public Endpoints](#public-endpoints)
  - [Public Info](#get-v10assistantsassistantidpublic)
  - [Create Thread](#post-v10assistantsassistantidthreads)
  - [Chat](#post-v10assistantsassistantidchat)
  - [Generate](#post-v10assistantsassistantidgenerate)
  - [Compact](#post-v10assistantsassistantidcompact)
  - [Feedback](#post-v10assistantsassistantidfeedback)
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
  "Version": "1.0.0",
  "Timestamp": "2025-01-01T12:00:00Z"
}
```

### HEAD /

Returns 200 OK with no body. Useful for health checks. **Unauthenticated.**

**Response:** `200 OK` (empty body)

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
  "IsProtected": false
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

## Users (Admin Only)

All user endpoints require authentication with an admin bearer token. Non-admin users receive `403 Forbidden`.

### PUT /v1.0/users

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
  "Active": true,
  "IsProtected": false,
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `400` -- Email is required.
- `409` -- A user with this email already exists.

### GET /v1.0/users

List all users with pagination.

**Auth:** Required (admin only)

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing `UserMaster` objects.

### GET /v1.0/users/{userId}

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
  "Active": true,
  "IsProtected": true,
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `404` -- User not found.

### PUT /v1.0/users/{userId}

Update an existing user. The `Id` and `CreatedUtc` fields are preserved from the existing record. If `PasswordSha256` is omitted or empty, the existing password is kept.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Email": "updated@example.com",
  "FirstName": "Jane",
  "LastName": "Smith",
  "IsAdmin": false,
  "Active": true,
  "IsProtected": false
}
```

**Response (200 OK):** The updated `UserMaster` object.

**Error Responses:**
- `404` -- User not found.

### DELETE /v1.0/users/{userId}

Delete a user and all associated credentials (cascading delete).

**Auth:** Required (admin only)

**Response:** `204 No Content`

**Error Responses:**
- `403` -- User is protected. Deactivate by setting `Active` to `false` instead.
- `404` -- User not found.

### HEAD /v1.0/users/{userId}

Check whether a user exists.

**Auth:** Required (admin only)

**Response:**
- `200 OK` -- User exists.
- `404 Not Found` -- User does not exist.

---

## Credentials (Admin Only)

All credential endpoints require authentication with an admin bearer token.

### PUT /v1.0/credentials

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

### GET /v1.0/credentials

List all credentials with pagination.

**Auth:** Required (admin only)

**Query Parameters:** See [Pagination](#pagination).

**Response (200 OK):** Paginated envelope containing `Credential` objects.

### GET /v1.0/credentials/{credentialId}

Retrieve a single credential by ID.

**Auth:** Required (admin only)

**Response (200 OK):**

```json
{
  "Id": "cred_abc123...",
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

### PUT /v1.0/credentials/{credentialId}

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

### DELETE /v1.0/credentials/{credentialId}

Delete a credential.

**Auth:** Required (admin only)

**Response:** `204 No Content`

**Error Responses:**
- `403` -- Credential is protected. Deactivate by setting `Active` to `false` instead.
- `404` -- Credential not found.

### HEAD /v1.0/credentials/{credentialId}

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
    "Order": "PreChunking",
    "SummarizationPrompt": "Summarize the following text concisely.",
    "MaxSummaryTokens": 1024,
    "MinCellLength": 1,
    "MaxParallelTasks": 4,
    "MaxRetriesPerSummary": 3,
    "MaxRetries": 3,
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
| `CompletionEndpointId`   | string  | null         | ID of the completion endpoint to use for summarization.          |
| `Order`                  | string  | PreChunking  | When to summarize: `PreChunking` or `PostChunking`.              |
| `SummarizationPrompt`    | string  | null         | Custom prompt for the summarization model.                       |
| `MaxSummaryTokens`       | int     | 1024         | Maximum tokens for each summary response.                        |
| `MinCellLength`          | int     | 1            | Minimum cell text length to trigger summarization.               |
| `MaxParallelTasks`       | int     | 4            | Maximum concurrent summarization tasks.                          |
| `MaxRetriesPerSummary`   | int     | 3            | Retries per individual summary request.                          |
| `MaxRetries`             | int     | 3            | Total retries across the summarization pipeline.                 |
| `TimeoutMs`              | int     | 300000       | Timeout in milliseconds for the summarization pipeline.          |

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
    "Order": "PreChunking",
    "SummarizationPrompt": "Summarize the following text concisely.",
    "MaxSummaryTokens": 1024,
    "MinCellLength": 1,
    "MaxParallelTasks": 4,
    "MaxRetriesPerSummary": 3,
    "MaxRetries": 3,
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
  "Name": "MiniLM Embeddings",
  "Model": "all-MiniLM-L6-v2",
  "Endpoint": "http://localhost:11434",
  "ApiFormat": "Ollama",
  "ApiKey": null,
  "Active": true,
  "HealthCheck": {
    "IntervalMs": 30000,
    "TimeoutMs": 5000,
    "UnhealthyThreshold": 3
  }
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

### GET /v1.0/endpoints/embedding/{endpointId}/health

Check the health of an embedding endpoint.

**Auth:** Required (admin only)

**Response (200 OK):** Health status from Partio.

---

## Completion Endpoints (Admin Only)

Manage completion (inference) endpoints on the Partio service. These endpoints define which LLM and API to use for summarization during document ingestion. All routes are proxied to Partio.

### PUT /v1.0/endpoints/completion

Create a new completion endpoint.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Name": "GPT-4o Summarizer",
  "Model": "gpt-4o",
  "Endpoint": "https://api.openai.com/v1",
  "ApiFormat": "OpenAI",
  "ApiKey": "sk-...",
  "Active": true,
  "HealthCheck": {
    "IntervalMs": 30000,
    "TimeoutMs": 5000,
    "UnhealthyThreshold": 3
  }
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

### GET /v1.0/endpoints/completion/{endpointId}/health

Check the health of a completion endpoint.

**Auth:** Required (admin only)

**Response (200 OK):** Health status from Partio.

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
  "Model": "gpt-4o",
  "EnableRag": false,
  "EnableRetrievalGate": false,
  "EnableQueryRewrite": false,
  "QueryRewritePrompt": null,
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
  "EmbeddingEndpointId": "ep_def456...",
  "Title": "My Support Bot",
  "LogoUrl": "https://example.com/logo.png",
  "FaviconUrl": "https://example.com/favicon.ico",
  "Streaming": true,
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
| `Model`                    | string  | Model name/identifier (e.g., `gpt-4o`, `llama3`).                          |
| `EnableRag`                | bool    | Enable RAG retrieval for chat. Default `false`.                             |
| `EnableRetrievalGate`      | bool    | Enable LLM-based retrieval gate. When enabled, an LLM call classifies whether each user message requires new document retrieval (`RETRIEVE`) or can be answered from existing conversation context (`SKIP`). Only applies when `EnableRag` is `true`. Default `false`. |
| `EnableQueryRewrite`       | bool    | Whether LLM-based query rewrite is enabled. When enabled, the user's prompt is rewritten into multiple semantically varied queries before retrieval to improve recall. Default `false`. |
| `QueryRewritePrompt`       | string? | The prompt template used for query rewriting. Must contain the `{prompt}` placeholder which is replaced with the user's message. When null or empty, a built-in default prompt is used. |
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
| `InferenceEndpointId`      | string  | Managed completion endpoint ID for inference (overrides global setting).    |
| `EmbeddingEndpointId`      | string  | Managed embedding endpoint ID for RAG retrieval (overrides global setting). |
| `Title`                    | string  | Title displayed as the heading on the chat window. Null uses assistant name.|
| `LogoUrl`                  | string  | URL for the logo image in the chat window (max 192x192). Null uses default.|
| `FaviconUrl`               | string  | URL for the browser tab favicon. Null uses default AssistantHub favicon.    |
| `Streaming`                | bool    | Enable SSE streaming for chat responses. Default `true`.                    |

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
  "Model": "gpt-4o",
  "EnableRag": false,
  "EnableRetrievalGate": false,
  "EnableQueryRewrite": false,
  "QueryRewritePrompt": null,
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
  "InferenceEndpointId": null,
  "EmbeddingEndpointId": null,
  "Title": "My Support Bot",
  "LogoUrl": "https://example.com/logo.png",
  "FaviconUrl": "https://example.com/favicon.ico",
  "Streaming": true
}
```

**Response (200 OK):** The created or updated `AssistantSettings` object.

**Error Responses:**
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
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Document Status Values:**

| Status                  | Description                                      |
|-------------------------|--------------------------------------------------|
| `Uploading`             | File is being uploaded to object storage.         |
| `Uploaded`              | File successfully uploaded to object storage.     |
| `TypeDetecting`         | Detecting the document type/format.               |
| `TypeDetectionSuccess`  | Document type detected successfully.              |
| `TypeDetectionFailed`   | Failed to detect document type.                   |
| `Processing`            | Extracting text content from the document.        |
| `ProcessingChunks`      | Splitting extracted text into chunks.             |
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
  "AssistantResponse": "To reset your password, navigate to Settings > Security...",
  "CreatedUtc": "2025-01-01T12:00:00Z",
  "LastUpdateUtc": "2025-01-01T12:00:00Z"
}
```

**Field Descriptions:**

| Field                  | Type     | Description                                                  |
|------------------------|----------|--------------------------------------------------------------|
| `Id`                   | string   | Unique identifier (chist_ prefix).                           |
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
| `AssistantResponse`    | string   | The assistant's full response text.                          |

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
| `SizeBytes`     | long     | Model size on disk in bytes (0 for OpenAI).         |
| `ModifiedUtc`   | datetime | Last modified timestamp (UTC).                      |
| `OwnedBy`       | string   | Model owner (OpenAI only, null for Ollama).         |
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
  "stream": false
}
```

| Field         | Type   | Required | Description                                                    |
|---------------|--------|----------|----------------------------------------------------------------|
| `model`       | string | No       | Model override (falls back to assistant settings).             |
| `messages`    | array  | Yes      | Array of message objects with `role` and `content`.            |
| `temperature` | double | No       | Sampling temperature override (0.0-2.0).                       |
| `top_p`       | double | No       | Top-p override (0.0-1.0).                                      |
| `max_tokens`  | int    | No       | Max tokens override.                                           |
| `stream`      | bool   | No       | Ignored; streaming is controlled by the assistant `Streaming` setting. |

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
| `model`       | string | No       | Model override (falls back to assistant settings).             |
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
| `model`       | string | No       | Model override (falls back to assistant settings).             |
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
  "FeedbackText": "This was exactly what I needed!"
}
```

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
  "CreatedUtc": "2025-01-01T12:00:00Z",
  "LastUpdateUtc": "2025-01-01T12:00:00Z"
}
```

**Error Responses:**
- `400` -- Invalid request body.
- `404` -- Assistant not found or not active.

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
