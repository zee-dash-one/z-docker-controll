#!/bin/bash
set -euo pipefail
BK="/mnt/c/HIL/BACKUP/9router_2026-09-29_1603/vol0/data"
echo "Backup contents:"
ls -la "$BK"
ls -la "$BK/db"
echo "Stopping 9router..."
docker stop 9router
echo "Restoring into volume 9router-data..."
docker run --rm \
  -v 9router-data:/to \
  -v "$BK:/from:ro" \
  alpine:3.20 \
  sh -c 'rm -rf /to/* /to/.[!.]* 2>/dev/null || true; cp -a /from/. /to/; echo AFTER; ls -la /to; ls -la /to/db'
echo "Starting 9router..."
docker start 9router
sleep 2
docker exec 9router sh -c 'wc -c /app/data/jwt-secret /app/data/db/data.sqlite; ls -la /app/data /app/data/db'
echo "Restore complete."
