#!/bin/sh
# Writes the basic-auth file from WEB_BASIC_AUTH ("user:hash", hash from
# `openssl passwd -apr1`). Without it the file stays empty and every request
# gets 401: the site fails closed instead of opening to everyone.
set -eu
file=/etc/nginx/htpasswd
printf '%s\n' "${WEB_BASIC_AUTH:-}" > "$file"
chown root:nginx "$file"
chmod 640 "$file"
if [ -z "${WEB_BASIC_AUTH:-}" ]; then
    echo "10-basic-auth.sh: WEB_BASIC_AUTH is empty, every request will be rejected" >&2
fi
