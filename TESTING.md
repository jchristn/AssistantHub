# Running Tests

AssistantHub currently uses two primary .NET test entrypoints plus per-SDK integration runners:

- `src/Test.Automated` for console-driven suite execution and summaries
- `src/Test.XUnit` for xUnit integration coverage
- `sdk/csharp/Test.Sdk`, `sdk/js/test_sdk.mjs`, and `sdk/python/test_sdk.py` for SDK integration coverage

## Build First

```bash
dotnet build src/AssistantHub.sln
```

## Root Test Runners

Run both primary .NET test projects:

```bash
./run-tests.sh
run-tests.bat
./run-tests.ps1
```

These wrappers execute:

- `dotnet run --project src/Test.Automated`
- `dotnet test src/Test.XUnit --no-build`

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

## Test.XUnit

Run the xUnit integration project:

```bash
dotnet test src/Test.XUnit/Test.XUnit.csproj --no-build --verbosity normal
```

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

### Python SDK

From `sdk/python`:

```bash
pip install -e .
python test_sdk.py
```

Optional environment variables:

- `ASSISTANTHUB_URL` default: `http://localhost:6600`
- `ASSISTANTHUB_API_KEY` default: `default`
