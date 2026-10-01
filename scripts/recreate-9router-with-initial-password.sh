#!/usr/bin/env bash
# Wipe 9router data and recreate container with INITIAL_PASSWORD so remote login works.
set -euo pipefail

PASS="${1:-123456}"
NAME=9router
IMAGE=decolua/9router:latest
HOST_PORT=20128
CT_PORT=20128

echo "=== stop/rm container (keep named volume for wipe) ==="
docker stop "$NAME" >/dev/null 2>&1 || true
docker rm "$NAME" >/dev/null 2>&1 || true

echo "=== wipe volume 9router-data ==="
docker run --rm -v 9router-data:/data alpine:3.20 \
  sh -c 'rm -rf /data/* /data/.[!.]* /data/..?* 2>/dev/null || true; ls -la /data'

echo "=== create with INITIAL_PASSWORD ==="
docker run -d \
  --name "$NAME" \
  --restart unless-stopped \
  -p "${HOST_PORT}:${CT_PORT}" \
  -v 9router-data:/app/data \
  -e "INITIAL_PASSWORD=${PASS}" \
  -e "PORT=${CT_PORT}" \
  -e "HOSTNAME=0.0.0.0" \
  -e "NODE_ENV=production" \
  -e "DATA_DIR=/app/data" \
  "$IMAGE"

echo "=== wait HTTP ==="
for i in $(seq 1 45); do
  if curl -sf -o /dev/null "http://127.0.0.1:${HOST_PORT}/"; then
    echo "Up after ${i}s"
    break
  fi
  sleep 1
done

echo "=== env ==="
docker exec "$NAME" sh -c 'env | grep -Ei "PASS|INITIAL" || true'

echo "=== login ==="
RESP=$(curl -sS -w "\nHTTP:%{http_code}" -X POST "http://127.0.0.1:${HOST_PORT}/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"password\":\"${PASS}\"}" || true)
echo "$RESP"

if echo "$RESP" | grep -q 'HTTP:200'; then
  echo "LOGIN_OK"
  exit 0
fi
echo "LOGIN_FAILED"
exit 1
