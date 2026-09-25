# Using Claude Or Cursor With AssistantHub MCP

AssistantHub includes a standalone MCP server under `src/AssistantHub.McpServer`. It exposes the platform management surface through MCP tools so Claude Code, Cursor, and other MCP-capable clients can inspect and administer tenants, assistants, credentials, storage, ingestion, endpoints, crawl plans, evaluation runs, history, request history, and runtime configuration.

## Prerequisites

- .NET 10 SDK
- A running AssistantHub server
- Claude Code, Cursor, or another MCP-capable client

## Step 1: Build

```bash
dotnet build src/AssistantHub.sln
```

## Step 2: Start AssistantHub Server

```bash
dotnet run --project src/AssistantHub.Server/AssistantHub.Server.csproj
```

Default REST endpoint: `http://localhost:8800`

## Step 3: Start AssistantHub MCP Server

```bash
dotnet run --project src/AssistantHub.McpServer/AssistantHub.McpServer.csproj
```

Default MCP endpoints:

- HTTP: `http://127.0.0.1:8820/rpc`
- TCP: `tcp://127.0.0.1:8821`
- WebSocket: `ws://127.0.0.1:8822/mcp`

Use `--showconfig` to inspect or bootstrap `assistanthub-mcp.json`:

```bash
dotnet run --project src/AssistantHub.McpServer/AssistantHub.McpServer.csproj -- --showconfig
```

## Step 4: Install MCP Configuration

From the built output directory:

```bash
cd src/AssistantHub.McpServer/bin/Debug/net10.0
./AssistantHub.McpServer install --dry-run
./AssistantHub.McpServer install
```

The install flow:

- updates `~/.claude.json`
- writes `~/.claude/agents/assistanthub.md`
- prints a Cursor `.cursor/mcp.json` snippet

## Step 5: Launch Your MCP Client

Claude Code:

```bash
claude --agent assistanthub
```

Cursor:

- add the printed `mcpServers.assistanthub` entry to `.cursor/mcp.json`
- restart Cursor after the MCP config changes

## Example Prompts

- List the available tenants and show me which assistants belong to the default tenant.
- Create a new assistant for tenant `ten_default`, then show its settings with secrets redacted.
- Summarize the last 24 hours of request history and highlight the top failing routes.
- List embedding and completion endpoints, then test the unhealthy ones.
- Export the current runtime configuration without secrets and tell me which external services it points to.

## Notes

- Secret-bearing fields are redacted by default for credentials, assistant settings, and runtime configuration.
- Set `includeSecrets=true` only when the client explicitly needs raw secret values.
- Binary MCP wrappers use base64 and enforce the configured inline limit.
- Public assistant chat/generate/compact/feedback flows and eval SSE remain REST-only in this release.

See [MCP_API.md](../MCP_API.md) for the full tool catalog and route coverage matrix.
