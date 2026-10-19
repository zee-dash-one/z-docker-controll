# Tailscale no WSL → SOCKS5 no Windows

Um único container (`nbpitang-wsl`) em `Ubuntu-24.04` para o NBPITANG-491 alcançar `*.mandrill-court.ts.net` **sem** o app Tailscale. O NetExtender em túnel dividido continua dono das rotas; o proxy do sistema (WinINET) **não** é alterado.

## Pré-requisitos

1. Docker no WSL limpo de 9Router / OmniRoute / MausBot (script `scripts/wipe-wsl-docker.ps1`, palavra `APAGAR`).
2. Auth key em `.env` (copie de `.env.example`). O agente não gera nem cola a chave.

## Subir

No PowerShell 5, a partir desta pasta (ou com caminho absoluto):

```powershell
Copy-Item .env.example .env
# edite .env e cole TS_AUTHKEY=...

wsl -d Ubuntu-24.04 -- bash -lc 'cd /mnt/c/GIT/ZAPFCOORP/router-ctrl/wsl/tailscale-windows && docker compose up -d'
```

## Testar (Windows)

```powershell
curl.exe --socks5 127.0.0.1:1055 https://zlab.mandrill-court.ts.net
```

Opcional: `9router-serve` ou `mausbot-serve` no mesmo domínio. Use `--socks5-hostname` se o curl local não resolver MagicDNS.

## Parar

```powershell
wsl -d Ubuntu-24.04 -- bash -lc 'cd /mnt/c/GIT/ZAPFCOORP/router-ctrl/wsl/tailscale-windows && docker compose down'
```

O volume `nbpitang-wsl-tailscale-state` guarda o estado do nó.

## O que este projeto não faz

- Não instala Tailscale no Windows.
- Não publica bind em `0.0.0.0`.
- Não recria 9Router, OmniRoute ou MausBot no WSL.
- Não mexe no Compose de `/opt/zlab` no z-server-01.
