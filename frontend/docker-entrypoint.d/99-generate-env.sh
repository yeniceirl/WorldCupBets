#!/bin/sh
set -eu

json_escape() {
    printf '%s' "$1" | sed 's/\\/\\\\/g; s/"/\\"/g'
}

case "${ENABLE_DEV_LOGIN:-false}" in
    true|TRUE|1|yes|YES) enable_dev_login=true ;;
    *) enable_dev_login=false ;;
esac

cat > /usr/share/nginx/html/env.js <<EOF
window.__env = {
  googleClientId: "$(json_escape "${GOOGLE_CLIENT_ID:-}")",
  enableDevLogin: ${enable_dev_login}
};
EOF
