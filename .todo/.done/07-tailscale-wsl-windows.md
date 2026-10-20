# 07: Tailscale no WSL exposto para o Windows

Status: concluída em 2026-10-19

Depende de: 06. Leia antes o `00-LEIA-ME.md` e `C:\GIT\ZAPFCOORP\z-server-01\README.md`.

## Objetivo

Depois da limpeza do Docker no WSL, deixar **um único** container Tailscale em `Ubuntu-24.04` para o Windows usar a tailnet `mandrill-court.ts.net` **sem** instalar o aplicativo Tailscale. O NetExtender em túnel dividido continua dono das rotas do Windows.

Os serviços (9Router, OmniRoute, MausBot e os três nós Tailscale) continuam no z-server-01. Esta tarefa não os recria no WSL e não mexe no servidor.

## Aceite

- [x] Um único projeto Tailscale no Docker do WSL, userspace, hostname `nbpitang-wsl` (`tag:winwsl`).
- [x] Porta publicada só em `127.0.0.1:1055` (SOCKS5). Nenhum bind em `0.0.0.0`.
- [x] `curl.exe --socks5-hostname 127.0.0.1:1055` alcança os três nomes (`zlab` 307, `9router-serve` 307, `mausbot-serve` 200).
- [x] Proxy do sistema e rotas do NetExtender sem mudança nesta tarefa. (Não instalamos o app Tailscale; o peer `NBPITANG-491` já aparecia na tailnet.)
- [x] 9Router, OmniRoute e MausBot não foram recriados no WSL. O z-server-01 não foi alterado.
- [x] Nenhuma auth key no Git, no log ou no Registro.

## Registro de execução

- **Data:** 2026-10-19
- **Wipe:** `wipe-wsl-docker-inner.sh` falhava por CRLF; corrigido (LF + normalização no `.ps1`). Depois: Docker limpo; hosts sem `9router-local`/`omniroute-local`; portproxy limpo pelo Hil (admin).
- **Compose:** `wsl/tailscale-windows/` — userspace, `TS_HOSTNAME=nbpitang-wsl`, `TS_EXTRA_ARGS=--advertise-tags=tag:winwsl`, `TS_ACCEPT_DNS=true`, `TS_SOCKS5_SERVER=0.0.0.0:1055`, publish `127.0.0.1:1055:1055`, `dns: [100.100.100.100]` (senão o SOCKS usa `127.0.0.11` e não resolve `*.ts.net`).
- **ACL:** grant `tag:winwsl` → servidores; auth key reusable com `tag:winwsl` (Hil, fora do Git).
- **Prova:** `curl.exe --socks5-hostname 127.0.0.1:1055 https://zlab|9router-serve|mausbot-serve.mandrill-court.ts.net` → 307 / 307 / 200.
- **Sugestão:** no admin, remover o nó órfão antigo `nbpitang-wsl` se ainda constar (o display às vezes mostra `-1`).

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Hil, um SOCKS em 127.0.0.1 e o NetExtender respira aliviado.
