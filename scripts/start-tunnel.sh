#!/bin/bash
# Tunel SSH do WSL (Ubuntu-24.04) para o z-server-01.
# O WSL ja esta em NAT. O Tailscale do WSL e o container userspace;
# os nos 100.x (zlab, 9router-serve, mausbot-serve) nao tem sshd.
# O SSH do servidor e melchior@192.168.2.37, porta 22.
# Reexecutar este script derruba o tunel anterior e abre outro.

set -euo pipefail

KEY="$HOME/.ssh/id_ed25519_zserver"
SRC="/mnt/c/Users/htzc/.ssh/id_ed25519_zserver"
TARGET="melchior@192.168.2.37"
SOCK="$HOME/.ssh/zlab-tunnel.sock"

mkdir -p "$HOME/.ssh"
chmod 700 "$HOME/.ssh"
if [[ ! -f "$KEY" ]]; then
  cp "$SRC" "$KEY"
fi
chmod 600 "$KEY"

if [[ -S "$SOCK" ]]; then
  ssh -S "$SOCK" -O exit "$TARGET" >/dev/null 2>&1 || true
  rm -f "$SOCK"
fi

ssh -i "$KEY" -f -N -M \
  -S "$SOCK" \
  -o BatchMode=yes \
  -o ExitOnForwardFailure=yes \
  -o ServerAliveInterval=30 \
  -o StrictHostKeyChecking=accept-new \
  -L 127.0.0.1:20128:127.0.0.1:20128 \
  -L 127.0.0.1:20129:127.0.0.1:20129 \
  -L 127.0.0.1:8799:127.0.0.1:8799 \
  "$TARGET"

echo "Tunel ativo: 20128 (9Router), 20129 (OmniRoute), 8799 (MausBot)"
