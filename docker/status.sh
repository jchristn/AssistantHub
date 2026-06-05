#!/usr/bin/env sh
set -eu

docker ps -a --format "table {{.ID}}\t{{.Names}}\t{{.CreatedAt}}\t{{.Status}}\t{{.Ports}}"
