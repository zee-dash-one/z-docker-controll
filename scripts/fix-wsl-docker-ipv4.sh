#!/usr/bin/env bash
# Force IPv4 for Docker Hub + CloudFront pulls on WSL.
#
# Project policy: DO NOT use IPv6 in this lab (see .cursor/rules/no-ipv6.mdc,
# README "Rede: IPv4 only", docs/SPEC-ZLAB.md RNF07).
#
# Run:
#   wsl -d Ubuntu-24.04 -u root -- bash /mnt/c/GIT/ZAPFCOORP/router-ctrl/scripts/fix-wsl-docker-ipv4.sh
set -euo pipefail

sysctl -w net.ipv6.conf.all.disable_ipv6=1
sysctl -w net.ipv6.conf.default.disable_ipv6=1
sysctl -w net.ipv6.conf.lo.disable_ipv6=1 || true
mkdir -p /etc/sysctl.d
printf '%s\n' \
  'net.ipv6.conf.all.disable_ipv6 = 1' \
  'net.ipv6.conf.default.disable_ipv6 = 1' \
  > /etc/sysctl.d/99-zlab-no-ipv6.conf

if grep -q '^#precedence ::ffff:0:0/96  100' /etc/gai.conf 2>/dev/null; then
  sed -i 's/^#precedence ::ffff:0:0\/96  100/precedence ::ffff:0:0\/96  100/' /etc/gai.conf
fi

pin_host() {
  local name="$1"
  local ip
  ip=$(getent ahostsv4 "$name" | awk '{print $1; exit}' || true)
  [ -n "${ip:-}" ] || return 0
  if grep -qE "[[:space:]]${name}([[:space:]]|$)" /etc/hosts; then
    sed -i -E "s/^.*[[:space:]]${name}([[:space:]].*)?$/${ip} ${name}/" /etc/hosts
  else
    echo "${ip} ${name}" >> /etc/hosts
  fi
  echo "pinned ${name} -> ${ip}"
}

pin_host registry-1.docker.io
pin_host auth.docker.io
pin_host production.cloudfront.docker.com

systemctl restart docker 2>/dev/null || service docker restart 2>/dev/null || true
sleep 3

echo "=== test pull ==="
docker pull decolua/9router:latest
echo EXIT:$?
