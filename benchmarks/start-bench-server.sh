#!/usr/bin/env bash
# Start an AssistantHub server built from the working tree against the isolated benchmark stack
# (benchmarks/docker/compose.yaml). REST on :38800, admin API key "benchadmin", SQLite database and logs under
# benchmarks/.run/server/. Build first: dotnet build src/AssistantHub.sln -c Release
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RUN="$ROOT/benchmarks/.run/server"
DLL="$ROOT/src/AssistantHub.Server/bin/Release/net10.0/AssistantHub.Server.dll"
if [ ! -f "$DLL" ]; then
  echo "Server not built. Run: dotnet build src/AssistantHub.sln -c Release" >&2
  exit 1
fi
mkdir -p "$RUN"
cp "$ROOT/benchmarks/docker/config/assistanthub.json" "$RUN/assistanthub.json"
cd "$RUN"
exec dotnet "$DLL" "$@"
