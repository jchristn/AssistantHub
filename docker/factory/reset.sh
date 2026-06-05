#!/bin/bash
#
# reset.sh - Reset AssistantHub docker environment to factory defaults
#
# This script destroys all runtime data (PostgreSQL data, logs, object
# storage, request history) and restores factory-default configuration.
#
# Usage: ./factory/reset.sh [--include-models]
#   --include-models  Also remove downloaded Ollama models (requires re-download)
#

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
FACTORY_DIR="$SCRIPT_DIR"
INCLUDE_MODELS=false

for arg in "$@"; do
  case "$arg" in
    --include-models) INCLUDE_MODELS=true ;;
  esac
done

# -------------------------------------------------------------------------
# Confirmation prompt
# -------------------------------------------------------------------------
echo ""
echo "=========================================================="
echo "  AssistantHub - Reset to Factory Defaults"
echo "=========================================================="
echo ""
echo "WARNING: This is a DESTRUCTIVE action. The following will"
echo "be permanently deleted:"
echo ""
echo "  - All PostgreSQL data (AssistantHub, Less3, Partio,"
echo "    RecallDB collections, embeddings, tenants, users)"
echo "  - Stale local SQLite database files from older deployments"
echo "  - All object storage files (uploaded documents)"
echo "  - All log files and processing logs"
echo "  - All Partio request history"
echo "  - Service configuration changes"
if [ "$INCLUDE_MODELS" = true ]; then
  echo "  - All downloaded Ollama models"
fi
echo ""
echo "Service configuration will be restored to factory defaults."
echo ""
read -r -p "Type 'RESET' to confirm: " CONFIRM
echo ""

if [ "$CONFIRM" != "RESET" ]; then
  echo "Aborted. No changes were made."
  exit 1
fi

# -------------------------------------------------------------------------
# Ensure containers are stopped
# -------------------------------------------------------------------------
echo "[1/6] Stopping containers..."
cd "$DOCKER_DIR"
docker compose down 2>/dev/null || true

# -------------------------------------------------------------------------
# Remove Docker named volumes
# -------------------------------------------------------------------------
echo "[2/6] Removing Docker volumes..."
docker volume rm docker_postgres-data 2>/dev/null || true
docker volume rm postgres-data 2>/dev/null || true
docker volume rm docker_pgvector-data 2>/dev/null || true
docker volume rm pgvector-data 2>/dev/null || true
if [ "$INCLUDE_MODELS" = true ]; then
  docker volume rm docker_ollama-models 2>/dev/null || docker volume rm ollama-models 2>/dev/null || true
  echo "        Removed postgres-data and ollama-models volumes"
else
  echo "        Removed postgres-data volume (ollama-models preserved)"
fi

# -------------------------------------------------------------------------
# Restore factory configuration and clear stale SQLite files
# -------------------------------------------------------------------------
echo "[3/6] Restoring factory configuration..."

# AssistantHub
rm -f "$DOCKER_DIR/assistanthub/data/assistanthub.db"
rm -f "$DOCKER_DIR/assistanthub/data/assistanthub.db-shm"
rm -f "$DOCKER_DIR/assistanthub/data/assistanthub.db-wal"
cp "$FACTORY_DIR/assistanthub.json" "$DOCKER_DIR/assistanthub/assistanthub.json"
echo "        Restored assistanthub.json and removed stale AssistantHub SQLite files"

# Less3
rm -f "$DOCKER_DIR/less3/less3.db"
rm -f "$DOCKER_DIR/less3/less3.db-shm"
rm -f "$DOCKER_DIR/less3/less3.db-wal"
cp "$FACTORY_DIR/less3.system.json" "$DOCKER_DIR/less3/system.json"
echo "        Restored Less3 system.json and removed stale Less3 SQLite files"

# Partio
rm -f "$DOCKER_DIR/partio/data/partio.db"
rm -f "$DOCKER_DIR/partio/data/partio.db-shm"
rm -f "$DOCKER_DIR/partio/data/partio.db-wal"
cp "$FACTORY_DIR/partio.json" "$DOCKER_DIR/partio/partio.json"
echo "        Restored partio.json and removed stale Partio SQLite files"

# RecallDB
cp "$FACTORY_DIR/recalldb.json" "$DOCKER_DIR/recalldb/recalldb.json"
echo "        Restored recalldb.json"

# -------------------------------------------------------------------------
# Clear object storage
# -------------------------------------------------------------------------
echo "[4/6] Clearing object storage..."
rm -rf "$DOCKER_DIR/less3/disk/"*/Objects/*
rm -rf "$DOCKER_DIR/less3/temp/"*
echo "        Cleared Less3 objects and temp files"

# -------------------------------------------------------------------------
# Clear logs and request history
# -------------------------------------------------------------------------
echo "[5/6] Clearing logs and history..."

rm -f "$DOCKER_DIR/assistanthub/logs/"*
rm -rf "$DOCKER_DIR/assistanthub/processing-logs/"*
rm -rf "$DOCKER_DIR/assistanthub/crawl-enumerations/"*
echo "        Cleared AssistantHub logs, processing logs, and crawl enumerations"

rm -f "$DOCKER_DIR/less3/logs/"*
echo "        Cleared Less3 logs"

rm -f "$DOCKER_DIR/documentatom/logs/"*
echo "        Cleared DocumentAtom logs"

rm -f "$DOCKER_DIR/partio/logs/"*
rm -f "$DOCKER_DIR/partio/request-history/"*
echo "        Cleared Partio logs and request history"

# -------------------------------------------------------------------------
# Done
# -------------------------------------------------------------------------
echo "[6/6] Factory reset complete."
echo ""
echo "To start the environment:"
echo "  cd $DOCKER_DIR"
echo "  docker compose up -d"
echo ""
