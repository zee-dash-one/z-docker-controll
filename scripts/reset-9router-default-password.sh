#!/bin/bash
set -euo pipefail

echo "Stopping 9router..."
docker stop 9router

echo "Wiping volume 9router-data (fresh init)..."
docker run --rm -v 9router-data:/data alpine:3.20 \
  sh -c 'rm -rf /data/* /data/.[!.]* /data/..?* 2>/dev/null || true; ls -la /data'

echo "Starting 9router..."
docker start 9router

echo "Waiting for HTTP..."
for i in $(seq 1 30); do
  if curl -sf -o /dev/null http://127.0.0.1:20128/; then
    echo "Up after ${i}s"
    break
  fi
  sleep 1
done

echo "Testing login with default password 123456..."
RESP=$(curl -si http://127.0.0.1:20128/api/auth/login \
  -X POST -H "Content-Type: application/json" \
  -d '{"password":"123456"}' || true)
echo "$RESP" | head -40

if echo "$RESP" | grep -qiE 'auth_token|Set-Cookie|200|mustChangePassword|"ok"\s*:\s*true'; then
  echo "LOGIN_OK"
else
  echo "LOGIN_CHECK_MANUAL — response above"
fi

echo "Data after init:"
docker exec 9router sh -c 'ls -la /app/data; ls -la /app/data/db 2>/dev/null || true'
