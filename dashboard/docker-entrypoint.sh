#!/bin/sh

# Generate runtime config from environment variables
cat > /usr/share/nginx/html/config.js <<EOF
window.ASSISTANTHUB_SERVER_URL = "${ASSISTANTHUB_SERVER_URL:-}";
window.DASHBOARD_INGEST_MAX_PARALLEL_INGESTIONS = "${DASHBOARD_INGEST_MAX_PARALLEL_INGESTIONS:-5}";
EOF

exec "$@"
