#!/bin/sh
set -eu

cat <<EOF >/usr/share/nginx/html/env.js
window.__env = {
  googleClientId: "${GOOGLE_CLIENT_ID:-}"
};
EOF

exec nginx -g 'daemon off;'
