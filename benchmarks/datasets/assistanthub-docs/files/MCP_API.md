# AssistantHub MCP API Reference

`AssistantHub.McpServer` is a standalone Voltaic-based MCP server that exposes the AssistantHub management and configuration surface as MCP tools. It connects to an upstream AssistantHub REST server with the C# SDK and uses a small REST proxy for exact REST pass-through cases such as configuration replacement, binary download wrappers, and HEAD-style existence checks.

This document is the source of truth for the MCP surface. For the underlying HTTP API, see [REST_API.md](REST_API.md). For Claude/Cursor setup guidance, see [docs/CLAUDE_MCP.md](docs/CLAUDE_MCP.md).

## Transports

Default transports:

| Transport | Default endpoint | Notes |
|---|---|---|
| HTTP JSON-RPC | `http://127.0.0.1:8820/rpc` | Voltaic HTTP transport |
| HTTP events | `http://127.0.0.1:8820/events` | Used by the HTTP MCP transport |
| TCP | `tcp://127.0.0.1:8821` | Voltaic TCP transport |
| WebSocket | `ws://127.0.0.1:8822/mcp` | Voltaic WebSocket transport |

Container defaults use `0.0.0.0` for the bind host so the ports are reachable outside the container.

## Quick Start

Build the solution:

```bash
dotnet build src/AssistantHub.sln
```

Run AssistantHub Server:

```bash
dotnet run --project src/AssistantHub.Server/AssistantHub.Server.csproj
```

Run the MCP server:

```bash
dotnet run --project src/AssistantHub.McpServer/AssistantHub.McpServer.csproj
```

Preview the generated MCP configuration:

```bash
dotnet run --project src/AssistantHub.McpServer/AssistantHub.McpServer.csproj -- --showconfig
```

Install Claude/Cursor snippets from the built MCP server:

```bash
cd src/AssistantHub.McpServer/bin/Debug/net10.0
./AssistantHub.McpServer install --dry-run
./AssistantHub.McpServer install
```

## Configuration

Default config file: `assistanthub-mcp.json`

Example:

```json
{
  "AssistantHub": {
    "Endpoint": "http://localhost:8800",
    "ApiKey": "default"
  },
  "Http": {
    "Hostname": "127.0.0.1",
    "Port": 8820
  },
  "Tcp": {
    "Address": "127.0.0.1",
    "Port": 8821
  },
  "WebSocket": {
    "Hostname": "127.0.0.1",
    "Port": 8822
  },
  "Storage": {
    "BackupsDirectory": "./backups/",
    "TempDirectory": "./temp/",
    "MaxInlineBinaryBytes": 5242880
  }
}
```

Supported environment overrides:

| Variable | Purpose |
|---|---|
| `ASSISTANTHUB_ENDPOINT` | Upstream AssistantHub server URL |
| `ASSISTANTHUB_API_KEY` | Upstream bearer token or admin API key |
| `MCP_HTTP_HOSTNAME` | HTTP bind host |
| `MCP_HTTP_PORT` | HTTP bind port |
| `MCP_TCP_ADDRESS` | TCP bind address |
| `MCP_TCP_PORT` | TCP bind port |
| `MCP_WS_HOSTNAME` | WebSocket bind host |
| `MCP_WS_PORT` | WebSocket bind port |
| `MCP_CONSOLE_LOGGING` | Console logging toggle |

## Tool Naming Rules

- Tools use lowercase underscore-delimited names that match `^[a-zA-Z0-9_-]{1,64}$`, the tool-name rule of the MCP specification and of LLM tool-calling APIs (Claude, OpenAI), so clients can pass them to a model unchanged. Before v0.17.0 names were slash-delimited (`assistant/list` is now `assistant_list`).
- CRUD-style tools follow `domain_action`.
- Sub-resource tools follow `domain_subdomain_action`.
- HTTP `HEAD` existence checks are normalized as `*_exists`.
- Public assistant metadata helpers remain under the `assistant_*` namespace.

Examples:

- `tenant_create`
- `assistant_settings_get`
- `assistantanalytics_overview`
- `bucket_object_upload`
- `requesthistory_summary`
- `eval_judge-prompt_default`

## Secret Handling

Redaction is on by default for responses serialized through the MCP helper layer. The redactor masks these property names case-insensitively:

- `BearerToken`
- `Password`
- `AdminPassword`
- `DefaultAdminPassword`
- `AdminApiKeys`
- `ApiKey`
- `ApiKeyValue`
- `AccessKey`
- `SecretKey`
- `SlackAppToken`
- `SlackBotToken`

Important behaviors:

- `configuration_get` returns a redacted configuration by default.
- `assistant_settings_get` and `assistant_settings_update` return redacted secret-bearing fields by default.
- `credential_list`, `credential_get`, `credential_create`, and `credential_update` redact bearer tokens by default.
- Set `includeSecrets=true` only when the caller explicitly needs the raw secret values.

## Binary And Streaming Rules

Binary wrappers:

- `document_upload`
- `document_download`
- `bucket_object_upload`
- `bucket_object_download`

Binary contract:

- uploads use `contentBase64`
- downloads return `FileName`, `ContentType`, `Size`, `ContentBase64`, and `Source`
- default inline size limit is `5242880` bytes (`Storage.MaxInlineBinaryBytes`)
- oversized binary payloads return an error instead of an external URL

Streaming status:

- Eval SSE is not exposed through MCP in this release.
- Public assistant chat/generate/compact/feedback/download flows are not exposed through MCP in this release.
- Assistant public document listing is exposed as `assistant_documents_list` and mirrors `GET /v1.0/assistants/{assistantId}/documents`. Sending attached-document chat requests remains REST-only in this release; send `attached_document_ids` to `POST /v1.0/assistants/{assistantId}/chat`.
- Model-directed runtime tools such as collection search, Verbex search, S3 object reads, and Tavily web search are not exposed as MCP tools for public chat users. Configure assistant tool policy, optional `ToolRoutingInferenceEndpointId`, and optional default-off `ExposeThinking` through the REST API, SDKs, or dashboard. Admins can inspect redacted global Tavily readiness through `GET /v1.0/configuration/external-search/status` or the SDK status helpers. When enabled, REST assistant chat executes those tools server-side against explicit OpenAI-compatible or Ollama tool-capable completion endpoints; a dedicated tool-routing endpoint may decide tool calls while `InferenceEndpointId` still writes final answers. Streaming REST chat can emit safe tool-progress SSE events, including heartbeat events for long-running tool calls, and can emit provider thinking deltas only when the assistant permits it, while MCP remains management-only for this release.
- Use the REST API directly for these streaming or interaction-heavy routes.

## Tool Families

| Family | Representative tools |
|---|---|
| System | `system_health`, `system_whoami`, `system_openapi` |
| Authentication | `auth_authenticate` |
| Tenants / Users / Credentials | `tenant_*`, `user_*`, `credential_*` |
| Storage | `bucket_*`, `bucket_object_*` |
| Collections | `collection_*`, `collection_record_*`, `collection_search` |
| Indices | `index_*`, `index_record_*`, `index_search` |
| Assistants | `assistant_*`, `assistant_settings_*`, `assistant_tool-calls_*` |
| Documents / Ingestion | `document_*`, `ingestionrule_*` |
| Monitoring | `history_*`, `thread_*`, `requesthistory_*`, `assistantanalytics_*` |
| Endpoint management | `embeddingendpoint_*`, `completionendpoint_*`, `model_*` |
| Crawl | `crawlplan_*`, `crawloperation_*` |
| Evaluation | `eval_fact_*`, `eval_run_*`, `eval_result_get`, `eval_judge-prompt_default` |
| Runtime configuration | `configuration_get`, `configuration_update` |

## Crawl Plan Repository Types

The `crawlplan_create` and `crawlplan_update` tools accept a `planJson` string containing the same `CrawlPlan` JSON contract used by REST and the SDKs. Supported `RepositoryType` values are:

| Value | Settings type | Notes |
|---|---|---|
| `Web` | `WebCrawlRepositorySettings` | Uses `StartUrl`, web authentication fields, link-following settings, sitemap/robots settings, depth, parallelism, and crawl delay. |
| `CIFS` | `CifsCrawlRepositorySettings` | Uses `CifsHostname`, `CifsUsername`, `CifsPassword`, `CifsShareName`, and `IncludeSubdirectories`. |
| `NFS` | `NfsCrawlRepositorySettings` | Uses `NfsHostname`, `NfsUserId`, `NfsGroupId`, `NfsShareName`, `NfsVersion`, and `IncludeSubdirectories`. |

CIFS passwords, web passwords, bearer tokens, and API keys are secret-bearing repository settings. MCP responses are redacted by default when serialized through the helper layer; callers should keep `includeSecrets=false` unless raw settings are explicitly needed.

Example CIFS `planJson` payload:

```json
{
  "Name": "CIFS Share Crawl",
  "RepositoryType": "CIFS",
  "RepositorySettings": {
    "RepositoryType": "CIFS",
    "CifsHostname": "fileserver.example.com",
    "CifsUsername": "crawler",
    "CifsPassword": "secret",
    "CifsShareName": "content",
    "IncludeSubdirectories": true
  }
}
```

Example NFS `planJson` payload:

```json
{
  "Name": "NFS Export Crawl",
  "RepositoryType": "NFS",
  "RepositorySettings": {
    "RepositoryType": "NFS",
    "NfsHostname": "nfs.example.com",
    "NfsUserId": 1000,
    "NfsGroupId": 1000,
    "NfsShareName": "/exports/content",
    "NfsVersion": "V3",
    "IncludeSubdirectories": true
  }
}
```

## Route Coverage Matrix

`Mapped` means there is an MCP tool for the route family. `Deferred` means the route exists in REST but is intentionally not exposed from the current MCP release.

| REST surface | MCP tools | Status | Notes |
|---|---|---|---|
| `GET /`, `HEAD /`, `GET /openapi.json`, `GET /v1.0/openapi.json`, `GET /v1.0/whoami` | `system_health`, `system_openapi`, `system_whoami` | Mapped | Health/head collapse into `system_health`; both OpenAPI routes return the same document |
| `GET /swagger` | None | Deferred | Browser Swagger UI; use `system_openapi` for the OpenAPI JSON from MCP |
| `POST /v1.0/authenticate` | `auth_authenticate` | Mapped | Useful for diagnosing upstream auth |
| `tenants` CRUD + HEAD | `tenant_list`, `tenant_get`, `tenant_create`, `tenant_update`, `tenant_delete`, `tenant_exists` | Mapped | |
| tenant-scoped `users` CRUD + HEAD | `user_list`, `user_get`, `user_create`, `user_update`, `user_delete`, `user_exists` | Mapped | REST path is `/v1.0/tenants/{tenantId}/users...` |
| tenant-scoped `credentials` CRUD + HEAD | `credential_list`, `credential_get`, `credential_create`, `credential_update`, `credential_delete`, `credential_exists` | Mapped | REST path is `/v1.0/tenants/{tenantId}/credentials...`; `includeSecrets` opt-in |
| `buckets` CRUD + HEAD | `bucket_list`, `bucket_get`, `bucket_create`, `bucket_delete`, `bucket_exists` | Mapped | |
| `bucket objects` list/put/delete/metadata/download/upload | `bucket_object_put`, `bucket_object_list`, `bucket_object_metadata`, `bucket_object_delete`, `bucket_object_download`, `bucket_object_upload` | Mapped | Binary transfers use base64 |
| `collections` CRUD + HEAD + distinct metadata | `collection_list`, `collection_get`, `collection_create`, `collection_update`, `collection_delete`, `collection_exists`, `collection_labels_distinct`, `collection_tags_distinct` | Mapped | |
| `collection records` list/get/create/delete/batch-delete | `collection_record_list`, `collection_record_get`, `collection_record_create`, `collection_record_delete`, `collection_record_batch-delete` | Mapped | |
| `collection search` | `collection_search` | Mapped | Marshals RecallDB search requests through AssistantHub |
| `indices` CRUD + HEAD + labels/tags/custom metadata + top terms | `index_list`, `index_get`, `index_create`, `index_update`, `index_delete`, `index_exists`, `index_labels_update`, `index_tags_update`, `index_custom-metadata_update`, `index_terms_top` | Mapped | Marshals Verbex index requests through AssistantHub |
| `index records` list/get/create/batch-create/delete/batch-delete/HEAD/metadata | `index_record_list`, `index_record_get`, `index_record_create`, `index_record_create-batch`, `index_record_delete`, `index_record_batch-delete`, `index_record_exists`, `index_record_exists-batch`, `index_record_labels_update`, `index_record_tags_update`, `index_record_custom-metadata_update` | Mapped | AssistantHub uses `records`; Verbex upstream uses `documents` |
| `index search` | `index_search` | Mapped | Marshals Verbex search requests through AssistantHub |
| `assistants` CRUD + HEAD | `assistant_list`, `assistant_get`, `assistant_create`, `assistant_update`, `assistant_delete`, `assistant_exists` | Mapped | |
| `assistant settings` get/update/slack verify + tool policy helpers | `assistant_settings_get`, `assistant_settings_update`, `assistant_settings_slack_verify`, `assistant_settings_tools_list`, `assistant_settings_tools_validate`, `assistant_settings_tools_test` | Mapped | `includeSecrets` opt-in; tool validation and dry-run diagnostics return redacted policy results and stable `ErrorCodes` by default |
| `assistant analytics` overview/timeseries/stages/endpoints/slowest/feedback | `assistantanalytics_overview`, `assistantanalytics_timeseries`, `assistantanalytics_stages`, `assistantanalytics_endpoints`, `assistantanalytics_slowest`, `assistantanalytics_feedback` | Mapped | Uses `assistantId` plus optional `AssistantAnalyticsQuery` JSON |
| `assistant tool-call traces` list/get/delete/bulk delete | `assistant_tool-calls_list`, `assistant_tool-calls_get`, `assistant_tool-calls_delete`, `assistant_tool-calls_delete-bulk` | Mapped | Redacted trace records only; supports `EnumerationQuery` filters for trace, tool, success, denied, chat-history, request-history, and time fields |
| `assistant public info + public documents + labels/tags` | `assistant_public_get`, `assistant_documents_list`, `assistant_labels_distinct`, `assistant_tags_distinct` | Mapped | Public metadata only; document list supports `queryJson`, text `query`, and `contentType` filters |
| `documents` list/get/upload/delete/HEAD/log/download/bulk-delete/reindex | `document_list`, `document_get`, `document_upload`, `document_delete`, `document_exists`, `document_processing-log`, `document_download`, `document_bulk-delete`, `document_reindex`, `document_reindex-batch` | Mapped | Binary transfers use base64; reindex tools backfill Verbex |
| `ingestion-rules` CRUD + HEAD | `ingestionrule_list`, `ingestionrule_get`, `ingestionrule_create`, `ingestionrule_update`, `ingestionrule_delete`, `ingestionrule_exists` | Mapped | |
| `feedback` list/get/delete | `feedback_list`, `feedback_get`, `feedback_delete` | Mapped | |
| `history` list/get/delete | `history_list`, `history_get`, `history_delete` | Mapped | |
| `threads` list/get/create/delete | `thread_list`, `thread_get`, `thread_create`, `thread_delete` | Mapped | |
| `requesthistory` list/summary/get/detail/delete/bulk-delete | `requesthistory_list`, `requesthistory_summary`, `requesthistory_get`, `requesthistory_detail`, `requesthistory_delete`, `requesthistory_bulk-delete` | Mapped | |
| `embedding endpoints` CRUD + HEAD + health + test | `embeddingendpoint_list`, `embeddingendpoint_get`, `embeddingendpoint_create`, `embeddingendpoint_update`, `embeddingendpoint_delete`, `embeddingendpoint_exists`, `embeddingendpoint_health`, `embeddingendpoint_test` | Mapped | Create/update accept the same body as REST, including the `MaxConcurrentRequests`, `MaxQueueDepth`, and `MaximumTimeoutMs` passthrough fields |
| `embedding endpoint load` | None | Deferred | Use REST `POST /v1.0/endpoints/embedding/{endpointId}/load` |
| `completion endpoints` CRUD + HEAD + health + test | `completionendpoint_list`, `completionendpoint_get`, `completionendpoint_create`, `completionendpoint_update`, `completionendpoint_delete`, `completionendpoint_exists`, `completionendpoint_health`, `completionendpoint_test` | Mapped | Create/update accept the same body as REST, including the `MaxConcurrentRequests`, `MaxQueueDepth`, and `MaximumTimeoutMs` passthrough fields |
| `completion endpoint load` | None | Deferred | Use REST `POST /v1.0/endpoints/completion/{endpointId}/load` |
| `models` list/pull/pull-status/delete | `model_list`, `model_pull`, `model_pull_status`, `model_delete` | Mapped | |
| `crawlplans` CRUD + HEAD + start/stop/connectivity/enumerate | `crawlplan_list`, `crawlplan_get`, `crawlplan_create`, `crawlplan_update`, `crawlplan_delete`, `crawlplan_exists`, `crawlplan_start`, `crawlplan_stop`, `crawlplan_connectivity`, `crawlplan_enumerate` | Mapped | |
| `crawl plan draft connectivity` | None | Deferred | Use REST `POST /v1.0/crawlplans/connectivity` to test unsaved repository settings |
| `crawl operations` list/get/delete/statistics/enumeration | `crawloperation_list`, `crawloperation_get`, `crawloperation_delete`, `crawloperation_statistics`, `crawloperation_enumeration` | Mapped | |
| `eval facts` CRUD | `eval_fact_list`, `eval_fact_get`, `eval_fact_create`, `eval_fact_update`, `eval_fact_delete` | Mapped | |
| `eval runs` create/list/get/delete/results | `eval_run_create`, `eval_run_list`, `eval_run_get`, `eval_run_delete`, `eval_run_results` | Mapped | |
| `eval result` get + judge prompt | `eval_result_get`, `eval_judge-prompt_default` | Mapped | |
| `eval stream` | None | Deferred | Use REST SSE endpoint |
| `configuration` get/update | `configuration_get`, `configuration_update` | Mapped | `configuration_get` redacts by default |
| Public assistant `chat/open`, `chat`, `generate`, `compact`, `feedback`, `documents/{id}/download` | None | Deferred | Use REST directly; public document listing is mapped above |

## Example Tool Calls

Get runtime OpenAPI from MCP:

```json
{
  "tool": "system_openapi",
  "arguments": {}
}
```

Get redacted runtime configuration:

```json
{
  "tool": "configuration_get",
  "arguments": {}
}
```

Create a credential and intentionally return the bearer token:

```json
{
  "tool": "credential_create",
  "arguments": {
    "tenantId": "ten_123",
    "credentialJson": "{\"UserId\":\"usr_123\",\"Name\":\"Automation key\",\"Active\":true}",
    "includeSecrets": true
  }
}
```

Download a document through the MCP wrapper:

```json
{
  "tool": "document_download",
  "arguments": {
    "documentId": "adoc_123"
  }
}
```

Reindex one completed document into Verbex:

```json
{
  "tool": "document_reindex",
  "arguments": {
    "documentId": "adoc_123"
  }
}
```

Reindex a page of completed documents into Verbex:

```json
{
  "tool": "document_reindex-batch",
  "arguments": {
    "requestJson": "{\"IncludeAlreadyIndexed\":false}",
    "queryJson": "{\"MaxResults\":50}"
  }
}
```

Search the default Verbex text index:

```json
{
  "tool": "index_search",
  "arguments": {
    "indexId": "default",
    "requestJson": "{\"Query\":\"deployment reset\",\"MaxResults\":10,\"IncludeMatchedTerms\":true,\"IncludeTermDetails\":true,\"IncludeDocumentTermStats\":true}"
  }
}
```

Search a RecallDB collection:

```json
{
  "tool": "collection_search",
  "arguments": {
    "collectionId": "default",
    "requestJson": "{\"Query\":\"deployment reset\",\"MaxResults\":10}"
  }
}
```

## Testing

Focused MCP validation:

```bash
$env:ASSISTANTHUB_TEST_SUITES="mcp"
dotnet run --project src/Test.Automated/Test.Automated.csproj
```

Preserve spawned server and MCP artifacts for investigation:

```bash
$env:ASSISTANTHUB_TEST_KEEP_ARTIFACTS="1"
```
