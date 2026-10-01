#!/bin/bash
set -e
docker exec 9router cat /entrypoint.sh || true
echo "==== PKG ===="
docker exec 9router cat /app/package.json | head -50 || true
echo "==== GREP ===="
docker exec 9router sh -c 'grep -riE "password|admin|defaultPass|INITIAL|setup" /app --include="*.md" --include="*.js" --include="*.mjs" --include="*.ts" --include="*.json" 2>/dev/null' | head -60 || true
echo "==== ENV ===="
docker exec 9router sh -c 'env | sort' | head -80 || true
