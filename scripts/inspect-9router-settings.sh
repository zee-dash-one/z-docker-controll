#!/bin/bash
set -euo pipefail

echo "=== settings rows ==="
docker exec 9router sh -c 'apk add --no-cache sqlite >/dev/null 2>&1 || true; which sqlite3; sqlite3 /app/data/db/data.sqlite ".tables"; sqlite3 /app/data/db/data.sqlite "SELECT id, substr(data,1,200) FROM settings LIMIT 5;"' 2>&1 || \
docker run --rm -v 9router-data:/data alpine:3.20 sh -c 'apk add --no-cache sqlite >/dev/null; sqlite3 /data/db/data.sqlite ".tables"; sqlite3 /data/db/data.sqlite "SELECT id, data FROM settings;"'

echo "=== current env ==="
docker exec 9router env | grep -iE 'PASS|INITIAL|AUTH' || true
