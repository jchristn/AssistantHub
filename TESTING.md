# Running Tests

AssistantHub uses Touchstone-backed .NET test projects plus per-SDK integration runners:

- `src/Test.Shared` holds the shared test suites and the Touchstone suite catalog
- `src/Test.Automated` runs the shared suites through `Touchstone.Cli`
- `src/Test.Xunit` adapts the shared Touchstone catalog to xUnit
- `src/Test.Nunit` adapts the shared Touchstone catalog to NUnit
- `sdk/csharp/Test.Sdk`, `sdk/js/test_sdk.mjs`, and `sdk/python/test_sdk.py` for SDK integration coverage

## Build First

```bash
dotnet build src/AssistantHub.sln
```

## Root Test Runners

Run all primary .NET test projects:

```bash
./run-tests.sh
run-tests.bat
./run-tests.ps1
```

These wrappers execute:

- `dotnet run --project src/Test.Automated`
- `dotnet test src/Test.Xunit --no-build`
- `dotnet test src/Test.Nunit --no-build`

## Test.Automated

Run all automated suites:

```bash
dotnet run --project src/Test.Automated/Test.Automated.csproj
```

Optional environment variables:

| Variable | Purpose |
|---|---|
| `ASSISTANTHUB_TEST_SUITES` | Comma/semicolon/space-delimited subset of suites to run. Supported values: `model`, `service`, `api`, `integration`, `mcp`. |
| `ASSISTANTHUB_TEST_KEEP_ARTIFACTS` | Preserve spawned server/MCP temp directories, logs, and databases for inspection when set to `1`, `true`, or `yes`. |

Examples:

```powershell
$env:ASSISTANTHUB_TEST_SUITES = "mcp"
dotnet run --project src/Test.Automated/Test.Automated.csproj
```

```powershell
$env:ASSISTANTHUB_TEST_SUITES = "api integration mcp"
$env:ASSISTANTHUB_TEST_KEEP_ARTIFACTS = "1"
dotnet run --project src/Test.Automated/Test.Automated.csproj
```

## Test.Xunit

Run the xUnit adapter project:

```bash
dotnet test src/Test.Xunit/Test.Xunit.csproj --no-build --verbosity normal
```

## Test.Nunit

Run the NUnit adapter project:

```bash
dotnet test src/Test.Nunit/Test.Nunit.csproj --no-build --verbosity normal
```

## Attached Documents and Tool Policy

The local model, service, and API suites cover the non-external parts of attached-document chat and the initial tool-call policy surface:

- `model`: `attached_document_ids` request serialization, retrieval attachment metadata serialization, tool-result DTO serialization, and Tavily-compatible settings defaults
- `service`: attached-document validation, RecallDB request body document filters, hybrid fallback filter preservation, multi-query filter invariants, tool registry resolution, tool executor errors, output limits, collection search, collection document enumeration, Verbex search, Tavily request shaping with mocked HTTP, and non-streaming tool-call chat orchestration with mocked endpoint/model/tool services
- `api`: OpenAPI route/schema coverage, REST/Postman route parity, attached-document Postman examples, and safe public document selection schema checks

Focused local runs:

```powershell
$env:ASSISTANTHUB_TEST_SUITES = "model"
dotnet run --project src/Test.Automated/Test.Automated.csproj
```

```powershell
$env:ASSISTANTHUB_TEST_SUITES = "service"
dotnet run --project src/Test.Automated/Test.Automated.csproj
```

```powershell
$env:ASSISTANTHUB_TEST_SUITES = "api"
dotnet run --project src/Test.Automated/Test.Automated.csproj
```

```powershell
$env:ASSISTANTHUB_TEST_SUITES = "integration"
dotnet run --project src/Test.Automated/Test.Automated.csproj
```

SDK local contract runs do not require a live AssistantHub server:

```powershell
dotnet run --project sdk/csharp/Test.Sdk/Test.Sdk.csproj -- local-only=true
node sdk/js/test_sdk.mjs --local-only
python sdk/python/test_sdk.py --local-only
```

Frontend build validation:

```powershell
Set-Location dashboard
npm.cmd run build
```

External validation is still required for browser layout checks, live Docker ingestion with two known documents, Swagger rendering at `http://localhost:8800/swagger`, real Tavily credentials, and any real S3 object-read or bucket-enumeration workflow.

## MCP-Focused Validation

The MCP suite exercises:

- HTTP, TCP, and WebSocket MCP server startup
- `system/health` and `system/openapi`
- tenant CRUD
- assistant CRUD
- configuration redaction and `includeSecrets=true`
- request-history capture, detail, and summary
- `install --dry-run`

Focused MCP run:

```powershell
$env:ASSISTANTHUB_TEST_SUITES = "mcp"
dotnet run --project src/Test.Automated/Test.Automated.csproj
```

Keep artifacts for live inspection:

```powershell
$env:ASSISTANTHUB_TEST_SUITES = "mcp"
$env:ASSISTANTHUB_TEST_KEEP_ARTIFACTS = "1"
dotnet run --project src/Test.Automated/Test.Automated.csproj
```

## SDK Tests

### C# SDK

From `sdk/csharp`:

```bash
dotnet run --project Test.Sdk/Test.Sdk.csproj
```

Optional environment variables:

- `ASSISTANTHUB_BASE_URL` default: `http://localhost:6600`
- `ASSISTANTHUB_API_KEY` default: `default`
- `ASSISTANTHUB_TENANT_ID` default: `default`

### JavaScript SDK

From `sdk/js`:

```bash
npm install
npm run build
node test_sdk.mjs
```

Optional environment variables:

- `ASSISTANTHUB_URL` default: `http://localhost:6600`
- `ASSISTANTHUB_API_KEY` default: `default`

## File-Server Crawler Validation

The default automated suite covers repository enum/model serialization, polymorphic repository settings, crawler factory registration, SDK payload models, and dashboard builds. CIFS and NFS network connectivity still requires an environment with reachable file servers.

Manual validation checklist:

- Create Web, CIFS, and NFS crawl plans in the dashboard Create Crawl Plan modal.
- For CIFS, verify the AssistantHub server or container can resolve and reach the configured host/share with the configured username and password.
- For NFS, verify the AssistantHub server or container can resolve and reach the export, and that `NfsUserId`, `NfsGroupId`, and `NfsVersion` match the export policy.
- In the local Docker deployment, `localhost` inside the server container is not the host machine. The compose stack provides `host.docker.internal`, and AssistantHub maps loopback CIFS/NFS hostnames to that alias when it is resolvable.
- Run `POST /v1.0/crawlplans/{id}/connectivity` before starting a full crawl.
- Run `GET /v1.0/crawlplans/{id}/enumerate` and confirm file metadata is returned without downloading file bytes during enumeration.
- Start a crawl against a small test share/export and confirm AssistantDocument records are created and ingestion starts for accepted files.

The default Docker Compose stack does not add Samba or NFS fixtures. CIFS/NFS crawlers connect to remote file servers over the network and do not require extra AssistantHub volume mounts for remote repositories.

### Python SDK

From `sdk/python`:

```bash
pip install -e .
python test_sdk.py
```

Optional environment variables:

- `ASSISTANTHUB_URL` default: `http://localhost:6600`
- `ASSISTANTHUB_API_KEY` default: `default`
