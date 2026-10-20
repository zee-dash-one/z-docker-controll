# Tailscale no WSL → SOCKS5 no Windows

Um único container (`nbpitang-wsl-1`) em `Ubuntu-24.04` para o NBPITANG-491 alcançar `*.mandrill-court.ts.net` **sem** o app Tailscale. O NetExtender em túnel dividido continua dono das rotas. O proxy manual do Windows fica desligado; só esses nomes da tailnet passam pelo SOCKS.

## Pré-requisitos

1. Docker no WSL limpo de 9Router / OmniRoute / MausBot (script `scripts/wipe-wsl-docker.ps1`, palavra `APAGAR`).
2. Auth key em `.env` (copie de `.env.example`). Gere em Settings → Keys com tag **`tag:winwsl`**, reusable, não ephemeral. O agente não gera nem cola a chave.
3. Na ACL, um device com tag **não** entra em `autogroup:member`. Inclua um grant, por exemplo:
   `{"src": ["tag:winwsl"], "dst": ["tag:zlab", "tag:ninerouter", "tag:mausbot"], "ip": ["*"]}`
   (ou `dst: ["*"]` se preferir).

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

## Opera GX (só a tailnet)

O Opera GX avalia o script pelo Windows, e esse leitor ignora `SOCKS5`. Por isso o script devolve o proxy HTTP `127.0.0.1:1056` (o mesmo container resolve o nome). `ProxyEnable` continua `0`. O resto do tráfego fica `DIRECT`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File C:\GIT\ZAPFCOORP\router-ctrl\scripts\tailnet-pac.ps1
```

Feche o Opera GX por completo (ícone perto do relógio, Sair) e abra de novo.

Para desfazer: apague `AutoConfigURL` e a tarefa `ZLab-TailnetPac`.

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
