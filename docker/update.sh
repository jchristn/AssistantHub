#!/bin/bash
#
# update.sh - Pull and restart the AssistantHub docker deployment
#
# Usage: ./update.sh
#

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

echo ""
echo "=========================================================="
echo "  AssistantHub - Update Docker Deployment"
echo "=========================================================="
echo ""

echo "[1/3] Stopping containers..."
docker compose down

echo ""
echo "[2/3] Pulling images..."
docker compose pull

echo ""
echo "[3/3] Starting containers..."
docker compose up -d

echo ""
echo "Update complete."
