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

- Tools use lowercase slash-delimited names.
- CRUD-style tools follow `domain/action`.
- Sub-resource tools follow `domain/subdomain/action`.
- HTTP `HEAD` existence checks are normalized as `*/exists`.
- Public assistant metadata helpers remain under the `assistant/*` namespace.

Examples:

- `tenant/create`
- `assistant/settings/get`
- `bucket/object/upload`
- `requesthistory/summary`
- `eval/judge-prompt/default`

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

- `configuration/get` returns a redacted configuration by default.
- `assistant/settings/get` and `assistant/settings/update` return redacted secret-bearing fields by default.
- `credential/list`, `credential/get`, `credential/create`, and `credential/update` redact bearer tokens by default.
- Set `includeSecrets=true` only when the caller explicitly needs the raw secret values.

## Binary And Streaming Rules

Binary wrappers:

- `document/upload`
- `document/download`
- `bucket/object/upload`
- `bucket/object/download`

Binary contract:

- uploads use `contentBase64`
- downloads return `FileName`, `ContentType`, `Size`, `ContentBase64`, and `Source`
- default inline size limit is `5242880` bytes (`Storage.MaxInlineBinaryBytes`)
- oversized binary payloads return an error instead of an external URL

Streaming status:

- Eval SSE is not exposed through MCP in this release.
- Public assistant chat/generate/compact/feedback/download flows are not exposed through MCP in this release.
- Use the REST API directly for these streaming or interaction-heavy routes.

## Tool Families

| Family | Representative tools |
|---|---|
| System | `system/health`, `system/whoami`, `system/openapi` |
| Authentication | `auth/authenticate` |
| Tenants / Users / Credentials | `tenant/*`, `user/*`, `credential/*` |
| Storage | `bucket/*`, `bucket/object/*` |
| Collections | `collection/*`, `collection/record/*` |
| Assistants | `assistant/*`, `assistant/settings/*` |
| Documents / Ingestion | `document/*`, `ingestionrule/*` |
| Monitoring | `history/*`, `thread/*`, `requesthistory/*` |
| Endpoint management | `embeddingendpoint/*`, `completionendpoint/*`, `model/*` |
| Crawl | `crawlplan/*`, `crawloperation/*` |
| Evaluation | `eval/fact/*`, `eval/run/*`, `eval/result/get`, `eval/judge-prompt/default` |
| Runtime configuration | `configuration/get`, `configuration/update` |

## Route Coverage Matrix

`Mapped` means there is an MCP tool for the route family. `Deferred` means the route exists in REST but is intentionally not exposed from the current MCP release.

| REST surface | MCP tools | Status | Notes |
|---|---|---|---|
| `GET /`, `HEAD /`, `GET /openapi.json`, `GET /v1.0/whoami` | `system/health`, `system/openapi`, `system/whoami` | Mapped | Health/head collapse into `system/health` |
| `POST /v1.0/authenticate` | `auth/authenticate` | Mapped | Useful for diagnosing upstream auth |
| `tenants` CRUD + HEAD | `tenant/list`, `tenant/get`, `tenant/create`, `tenant/update`, `tenant/delete`, `tenant/exists` | Mapped | |
| `users` CRUD + HEAD | `user/list`, `user/get`, `user/create`, `user/update`, `user/delete`, `user/exists` | Mapped | |
| `credentials` CRUD + HEAD | `credential/list`, `credential/get`, `credential/create`, `credential/update`, `credential/delete`, `credential/exists` | Mapped | `includeSecrets` opt-in |
| `buckets` CRUD + HEAD | `bucket/list`, `bucket/get`, `bucket/create`, `bucket/delete`, `bucket/exists` | Mapped | |
| `bucket objects` list/put/delete/metadata/download/upload | `bucket/object/put`, `bucket/object/list`, `bucket/object/metadata`, `bucket/object/delete`, `bucket/object/download`, `bucket/object/upload` | Mapped | Binary transfers use base64 |
| `collections` CRUD + HEAD + distinct metadata | `collection/list`, `collection/get`, `collection/create`, `collection/update`, `collection/delete`, `collection/exists`, `collection/labels/distinct`, `collection/tags/distinct` | Mapped | |
| `collection records` list/get/create/delete/batch-delete | `collection/record/list`, `collection/record/get`, `collection/record/create`, `collection/record/delete`, `collection/record/batch-delete` | Mapped | |
| `assistants` CRUD + HEAD | `assistant/list`, `assistant/get`, `assistant/create`, `assistant/update`, `assistant/delete`, `assistant/exists` | Mapped | |
| `assistant settings` get/update/slack verify | `assistant/settings/get`, `assistant/settings/update`, `assistant/settings/slack/verify` | Mapped | `includeSecrets` opt-in |
| `assistant public info + labels/tags` | `assistant/public/get`, `assistant/labels/distinct`, `assistant/tags/distinct` | Mapped | Public metadata only |
| `documents` list/get/upload/delete/HEAD/log/download/bulk-delete | `document/list`, `document/get`, `document/upload`, `document/delete`, `document/exists`, `document/processing-log`, `document/download`, `document/bulk-delete` | Mapped | Binary transfers use base64 |
| `ingestion-rules` CRUD + HEAD | `ingestionrule/list`, `ingestionrule/get`, `ingestionrule/create`, `ingestionrule/update`, `ingestionrule/delete`, `ingestionrule/exists` | Mapped | |
| `feedback` list/get/delete | `feedback/list`, `feedback/get`, `feedback/delete` | Mapped | |
| `history` list/get/delete | `history/list`, `history/get`, `history/delete` | Mapped | |
| `threads` list/get/create/delete | `thread/list`, `thread/get`, `thread/create`, `thread/delete` | Mapped | |
| `requesthistory` list/summary/get/detail/delete/bulk-delete | `requesthistory/list`, `requesthistory/summary`, `requesthistory/get`, `requesthistory/detail`, `requesthistory/delete`, `requesthistory/bulk-delete` | Mapped | |
| `embedding endpoints` CRUD + HEAD + health + test | `embeddingendpoint/list`, `embeddingendpoint/get`, `embeddingendpoint/create`, `embeddingendpoint/update`, `embeddingendpoint/delete`, `embeddingendpoint/exists`, `embeddingendpoint/health`, `embeddingendpoint/test` | Mapped | |
| `completion endpoints` CRUD + HEAD + health + test | `completionendpoint/list`, `completionendpoint/get`, `completionendpoint/create`, `completionendpoint/update`, `completionendpoint/delete`, `completionendpoint/exists`, `completionendpoint/health`, `completionendpoint/test` | Mapped | |
| `models` list/pull/pull-status/delete | `model/list`, `model/pull`, `model/pull/status`, `model/delete` | Mapped | |
| `crawlplans` CRUD + HEAD + start/stop/connectivity/enumerate | `crawlplan/list`, `crawlplan/get`, `crawlplan/create`, `crawlplan/update`, `crawlplan/delete`, `crawlplan/exists`, `crawlplan/start`, `crawlplan/stop`, `crawlplan/connectivity`, `crawlplan/enumerate` | Mapped | |
| `crawl operations` list/get/delete/statistics/enumeration | `crawloperation/list`, `crawloperation/get`, `crawloperation/delete`, `crawloperation/statistics`, `crawloperation/enumeration` | Mapped | |
| `eval facts` CRUD | `eval/fact/list`, `eval/fact/get`, `eval/fact/create`, `eval/fact/update`, `eval/fact/delete` | Mapped | |
| `eval runs` create/list/get/delete/results | `eval/run/create`, `eval/run/list`, `eval/run/get`, `eval/run/delete`, `eval/run/results` | Mapped | |
| `eval result` get + judge prompt | `eval/result/get`, `eval/judge-prompt/default` | Mapped | |
| `eval stream` | None | Deferred | Use REST SSE endpoint |
| `configuration` get/update | `configuration/get`, `configuration/update` | Mapped | `configuration/get` redacts by default |
| Public assistant `chat`, `generate`, `compact`, `feedback`, `documents/{id}/download` | None | Deferred | Management-first release; use REST directly |

## Example Tool Calls

Get runtime OpenAPI from MCP:

```json
{
  "tool": "system/openapi",
  "arguments": {}
}
```

Get redacted runtime configuration:

```json
{
  "tool": "configuration/get",
  "arguments": {}
}
```

Create a credential and intentionally return the bearer token:

```json
{
  "tool": "credential/create",
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
  "tool": "document/download",
  "arguments": {
    "documentId": "adoc_123"
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
