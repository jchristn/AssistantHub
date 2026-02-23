#!/bin/sh

# Generate runtime config from environment variables
cat > /usr/share/nginx/html/config.js <<EOF
window.ASSISTANTHUB_SERVER_URL = "${ASSISTANTHUB_SERVER_URL:-}";
EOF

exec "$@"
