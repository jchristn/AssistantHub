#!/usr/bin/env bash
set -euo pipefail

if [ -z "${1:-}" ]; then
    echo "Usage: build-all.sh <tag>"
    echo "Example: build-all.sh v0.12.0"
    exit 1
fi

TAG="$1"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

"${SCRIPT_DIR}/build-dashboard.sh" "${TAG}"
"${SCRIPT_DIR}/build-server.sh" "${TAG}"
"${SCRIPT_DIR}/build-mcp.sh" "${TAG}"

echo "Done."
