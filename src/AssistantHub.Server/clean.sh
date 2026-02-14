#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
rm -f "$SCRIPT_DIR/assistanthub.json"
rm -rf "$SCRIPT_DIR/logs"
rm -f "$SCRIPT_DIR/assistanthub.db"
echo "Cleanup complete."
