# AssistantHub REST API Reference

Base URL: `http://localhost:8800`

All API endpoints are versioned under `/v1.0/`. Responses use `application/json` content type unless otherwise noted.

## Table of Contents

- [Authentication](#authentication)
- [Error Responses](#error-responses)
- [Pagination](#pagination)
- [Health](#health)
- [Users (Admin Only)](#users-admin-only)
- [Credentials (Admin Only)](#credentials-admin-only)
- [Buckets (Admin Only)](#buckets-admin-only)
- [Bucket Objects (Admin Only)](#bucket-objects-admin-only)
- [Collections (Admin Only)](#collections-admin-only)
- [Collection Records (Admin Only)](#collection-records-admin-only)
- [Ingestion Rules](#ingestion-rules)
- [Assistants](#assistants)
- [Assistant Settings](#assistant-settings)
- [Documents](#documents)
- [Feedback (Authenticated)](#feedback-authenticated)
- [History (Authenticated)](#history-authenticated)
- [Threads (Authenticated)](#threads-authenticated)
- [Models](#models)
- [Public Endpoints](#public-endpoints)
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
  "ErrorMessage": null
}
```

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
  "Active": true
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
  "Active": true
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
  "Active": true
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
  "Active": false
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
- `404` -- Credential not found.

### HEAD /v1.0/credentials/{credentialId}

Check whether a credential exists.

**Auth:** Required (admin only)

**Response:**
- `200 OK` -- Credential exists.
- `404 Not Found` -- Credential does not exist.

---

## Buckets (Admin Only)

All bucket endpoints require authentication with an admin bearer token. Buckets are managed on the configured S3-compatible storage server (Less3).

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

## Bucket Objects (Admin Only)

Manage objects within S3-compatible storage buckets. Object keys may contain path separators (`/`) and are passed as query parameters.

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

Ingestion rules define how documents are processed, chunked, and embedded. Each rule specifies a target S3 bucket and RecallDB collection, along with optional chunking and embedding configuration.

### PUT /v1.0/ingestion-rules

Create a new ingestion rule.

**Auth:** Required (admin only)

**Request Body:**

```json
{
  "Name": "Knowledge Base Documents",
  "Description": "Process PDF and text documents for the support knowledge base.",
  "Bucket": "kb-documents",
  "Collection": "collection-uuid-here",
  "Labels": ["support", "knowledge-base"],
  "Tags": { "department": "engineering", "priority": "high" },
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
    "Model": null,
    "L2Normalization": false
  }
}
```

**Response (201 Created):**

```json
{
  "Id": "irule_abc123...",
  "Name": "Knowledge Base Documents",
  "Description": "Process PDF and text documents for the support knowledge base.",
  "Bucket": "kb-documents",
  "Collection": "collection-uuid-here",
  "Labels": ["support", "knowledge-base"],
  "Tags": { "department": "engineering", "priority": "high" },
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
    "Model": null,
    "L2Normalization": false
  },
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `400` -- Name is required.
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
  "CollectionId": "collection-uuid",
  "RetrievalTopK": 5,
  "RetrievalScoreThreshold": 0.7,
  "InferenceProvider": "OpenAI",
  "InferenceEndpoint": null,
  "InferenceApiKey": null,
  "Title": "My Support Bot",
  "LogoUrl": "https://example.com/logo.png",
  "FaviconUrl": "https://example.com/favicon.ico",
  "CreatedUtc": "2025-01-01T00:00:00Z",
  "LastUpdateUtc": "2025-01-01T00:00:00Z"
}
```

**Field Descriptions:**

| Field                      | Type   | Description                                                                 |
|----------------------------|--------|-----------------------------------------------------------------------------|
| `Temperature`              | double | Sampling temperature (0.0 to 2.0).                                          |
| `TopP`                     | double | Top-p nucleus sampling (0.0 to 1.0).                                        |
| `SystemPrompt`             | string | System prompt sent to the LLM.                                              |
| `MaxTokens`                | int    | Maximum tokens to generate in a response.                                   |
| `ContextWindow`            | int    | Context window size in tokens.                                              |
| `Model`                    | string | Model name/identifier (e.g., `gpt-4o`, `llama3`).                          |
| `EnableRag`                | bool   | Enable RAG retrieval for chat. Default `false`.                             |
| `CollectionId`             | string | RecallDb collection ID for document retrieval.                              |
| `RetrievalTopK`            | int    | Number of top document chunks to retrieve.                                  |
| `RetrievalScoreThreshold`  | double | Minimum similarity score threshold (0.0 to 1.0).                           |
| `InferenceProvider`        | string | LLM provider: `OpenAI` or `Ollama`.                                        |
| `InferenceEndpoint`        | string | Custom endpoint URL (overrides global setting if set).                      |
| `InferenceApiKey`          | string | Custom API key (overrides global setting if set).                           |
| `Title`                    | string | Title displayed as the heading on the chat window. Null uses assistant name.|
| `LogoUrl`                  | string | URL for the logo image in the chat window (max 192x192). Null uses default.|
| `FaviconUrl`               | string | URL for the browser tab favicon. Null uses default AssistantHub favicon.    |
| `Streaming`                | bool   | Enable SSE streaming for chat responses. Default `false`.                   |

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
  "CollectionId": "my-collection-id",
  "RetrievalTopK": 10,
  "RetrievalScoreThreshold": 0.6,
  "InferenceProvider": "OpenAI",
  "InferenceEndpoint": null,
  "InferenceApiKey": null,
  "Title": "My Support Bot",
  "LogoUrl": "https://example.com/logo.png",
  "FaviconUrl": "https://example.com/favicon.ico",
  "Streaming": false
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

### HEAD /v1.0/documents/{documentId}

Check whether a document exists.

**Auth:** Required

**Response:**
- `200 OK` -- Document exists.
- `404 Not Found` -- Document does not exist.

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
  "RetrievalContext": "Chunk 1: To reset your password...",
  "PromptSentUtc": "2025-01-01T12:00:00.150Z",
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
| `RetrievalContext`     | string   | Retrieved context chunks (null if no RAG).                   |
| `PromptSentUtc`        | datetime | UTC timestamp when the prompt was sent to the model.         |
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

**Response (200 OK -- Success):**

```json
{
  "Success": true,
  "Name": "gemma3:4b",
  "Message": "Model pull completed successfully."
}
```

**Response (200 OK -- Failure):**

```json
{
  "Success": false,
  "Name": "gemma3:4b",
  "Message": "Model pull failed."
}
```

**Error Responses:**
- `400` -- Model name is required, or pull is not supported by the configured provider.
- `403` -- Not an admin user.
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

**Streaming Response (200 OK, `Content-Type: text/event-stream`):**

When `Streaming` is enabled in assistant settings, the response is an SSE stream:

```
data: {"id":"chatcmpl-abc123...","object":"chat.completion.chunk","created":1700000000,"model":"gpt-4o","choices":[{"index":0,"delta":{"role":"assistant"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123...","object":"chat.completion.chunk","created":1700000000,"model":"gpt-4o","choices":[{"index":0,"delta":{"content":"To"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123...","object":"chat.completion.chunk","created":1700000000,"model":"gpt-4o","choices":[{"index":0,"delta":{"content":" reset"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123...","object":"chat.completion.chunk","created":1700000000,"model":"gpt-4o","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}

data: [DONE]
```

**Error Responses:**
- `400` -- At least one message is required.
- `404` -- Assistant not found or not active.
- `500` -- Assistant settings not configured.
- `502` -- Inference failed.

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
