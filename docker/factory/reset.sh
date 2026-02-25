#!/bin/bash
#
# reset.sh - Reset AssistantHub docker environment to factory defaults
#
# This script destroys all runtime data (databases, logs, object storage,
# vector data) and restores factory-default databases. Configuration files
# are preserved.
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
echo "  - All SQLite databases (AssistantHub, Less3, Partio)"
echo "  - All PostgreSQL/pgvector data (RecallDB collections,"
echo "    embeddings, tenants, users)"
echo "  - All object storage files (uploaded documents)"
echo "  - All log files and processing logs"
echo "  - All Partio request history"
if [ "$INCLUDE_MODELS" = true ]; then
  echo "  - All downloaded Ollama models"
fi
echo ""
echo "Configuration files will NOT be modified."
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
docker volume rm docker_pgvector-data 2>/dev/null || docker volume rm pgvector-data 2>/dev/null || true
if [ "$INCLUDE_MODELS" = true ]; then
  docker volume rm docker_ollama-models 2>/dev/null || docker volume rm ollama-models 2>/dev/null || true
  echo "        Removed pgvector-data and ollama-models volumes"
else
  echo "        Removed pgvector-data volume (ollama-models preserved)"
fi

# -------------------------------------------------------------------------
# Restore factory databases
# -------------------------------------------------------------------------
echo "[3/6] Restoring factory databases..."

# AssistantHub
rm -f "$DOCKER_DIR/assistanthub/data/assistanthub.db"
rm -f "$DOCKER_DIR/assistanthub/data/assistanthub.db-shm"
rm -f "$DOCKER_DIR/assistanthub/data/assistanthub.db-wal"
cp "$FACTORY_DIR/assistanthub.db" "$DOCKER_DIR/assistanthub/data/assistanthub.db"
cp "$FACTORY_DIR/assistanthub.db-shm" "$DOCKER_DIR/assistanthub/data/assistanthub.db-shm" 2>/dev/null || true
cp "$FACTORY_DIR/assistanthub.db-wal" "$DOCKER_DIR/assistanthub/data/assistanthub.db-wal" 2>/dev/null || true
echo "        Restored assistanthub.db"

# Less3
rm -f "$DOCKER_DIR/less3/less3.db"
cp "$FACTORY_DIR/less3.db" "$DOCKER_DIR/less3/less3.db"
echo "        Restored less3.db"

# Partio
rm -f "$DOCKER_DIR/partio/data/partio.db"
rm -f "$DOCKER_DIR/partio/data/partio.db-shm"
rm -f "$DOCKER_DIR/partio/data/partio.db-wal"
cp "$FACTORY_DIR/partio.db" "$DOCKER_DIR/partio/data/partio.db"
echo "        Restored partio.db"

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
echo "        Cleared AssistantHub logs and processing logs"

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
