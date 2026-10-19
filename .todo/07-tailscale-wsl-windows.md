# 07: Tailscale no WSL exposto para o Windows

Status: bloqueada

Depende de: 06. Leia antes o `00-LEIA-ME.md` e `C:\GIT\ZAPFCOORP\z-server-01\README.md`.

## Objetivo

Depois da limpeza do Docker no WSL, deixar **um único** container Tailscale em `Ubuntu-24.04` para o Windows usar a tailnet `mandrill-court.ts.net` **sem** instalar o aplicativo Tailscale. O NetExtender em túnel dividido continua dono das rotas do Windows.

Os serviços (9Router, OmniRoute, MausBot e os três nós Tailscale) continuam no z-server-01. Esta tarefa não os recria no WSL e não mexe no servidor.

## O que já existe (não repetir)

- No servidor, três nós já publicam HTTPS: `https://zlab.mandrill-court.ts.net`, `https://9router-serve.mandrill-court.ts.net`, `https://mausbot-serve.mandrill-court.ts.net`. Fonte: README do z-server-01.
- A pasta `C:\GIT\ZAPFCOORP\tailscale-9router` e o container antigo `tailscale-9router` eram o sidecar da época em que 9Router/OmniRoute rodavam no WSL. **Não reaproveite** esse compose, essa tag `tag:9router` nem esse hostname. A pasta pode ficar no disco; o container novo é outro projeto.
- O Z-Lab 2.0 não sobe Docker no WSL. Esta tarefa é script/compose local, fora do `Program`.
- `scripts/wipe-wsl-docker.ps1` apaga containers e volumes nomeados do Docker no WSL (pede a palavra `APAGAR`). Não foi executado. **Não rode esse script** nesta tarefa, a menos que o Hil digite `APAGAR` no terminal na hora. Se ainda houver 9Router, OmniRoute ou MausBot no `docker ps` do WSL, pare com `Status: bloqueada` e peça a limpeza.

## Desenho fechado

O userspace do Tailscale dentro do Docker **não** cria interface de rede no Windows. Por isso a exposição é só localhost:

| O quê | Valor |
|---|---|
| Onde | WSL `Ubuntu-24.04`, Docker |
| Imagem | `tailscale/tailscale:latest` |
| Modo | userspace (`TS_USERSPACE=true`). Sem `--tun`, sem exit node, sem subnet router, sem Funnel |
| Hostname do nó | `nbpitang-wsl` (não pode colidir com `zlab`, `9router-serve`, `mausbot-serve`) |
| Estado | volume nomeado novo, só deste projeto |
| Auth key | `.env` fora do Git, modo que o Windows já usa para segredo local. O agente não gera, não cola e não commita a chave |
| Entrada no Windows | SOCKS5 em `127.0.0.1:1055` apenas (`-p 127.0.0.1:1055:1055`). Nada em `0.0.0.0` |
| Proxy do Windows | **não** alterar o proxy do sistema (WinINET). Isso brigaria com o NetExtender |
| App Tailscale no Windows | não instalar |

O navegador do Windows usa esse SOCKS só se o Hil apontar o navegador (ou um atalho) para `127.0.0.1:1055`. A prova automática é o `curl.exe` com `--socks5`, não uma rota nova.

## Passos

1. Leia o README do z-server-01 (seção Tailscale), `.docs/tech-debt.md` item do Tailscale no WSL, `.docs/plans/zlab-no-z-server-01.md` seção 10, e `scripts/wipe-wsl-docker.ps1`.
2. Confira `wsl -d Ubuntu-24.04 -e docker ps -a`. Se existir container de 9Router, OmniRoute, MausBot ou o sidecar antigo ainda necessário para algo do Hil, não apague: bloqueie e descreva o que viu.
3. Crie um compose versionado neste repo (pasta nova, por exemplo `wsl/tailscale-windows/`), com o serviço único da tabela acima. `.env.example` só com o nome da variável `TS_AUTHKEY=` vazia. O `.env` real entra no `.gitignore` se o compose ficar neste repo.
4. Documente no README do router-ctrl, em poucas linhas: como subir, como testar com `curl.exe --socks5 127.0.0.1:1055 https://zlab.mandrill-court.ts.net`, e que o proxy do sistema fica intocado.
5. Sem a auth key no ambiente, não invente login. Deixe o compose pronto e marque o que o Hil ainda cola no `.env`.
6. Não altere `RemoteCommands`, os cards do Z-Lab, nem o Compose de `/opt/zlab`.

## Aceite

- [ ] Um único projeto Tailscale no Docker do WSL, userspace, hostname `nbpitang-wsl`.
- [ ] Porta publicada só em `127.0.0.1:1055` (SOCKS5). Nenhum bind em `0.0.0.0`.
- [ ] `curl.exe --socks5 127.0.0.1:1055` alcança pelo menos um dos três nomes `*.mandrill-court.ts.net` (quando a auth key estiver no `.env`).
- [ ] Aplicativo Tailscale não instalado no Windows. Proxy do sistema e rotas do NetExtender sem mudança.
- [ ] 9Router, OmniRoute e MausBot não foram recriados no WSL. O z-server-01 não foi alterado.
- [ ] Nenhuma auth key, senha ou token no Git, no log ou no Registro de execução.

## Registro de execução

- **Data:** 2026-10-19
- **Passo 2 (`docker ps -a` no Ubuntu-24.04):** ainda há containers antigos — `mausbot` (Up), `9router` (Exited), `omniroute` (Exited). Por isso **Status: bloqueada**. Não rodei `wipe-wsl-docker.ps1` (exige `APAGAR` digitado pelo Hil).
- **Pronto no repo:** `wsl/tailscale-windows/` (`docker-compose.yml`, `.env.example`, README), `.gitignore` para `.env`, seção no README do router-ctrl e changelog. Compose: userspace, hostname `nbpitang-wsl`, `TS_SOCKS5_SERVER=0.0.0.0:1055` + publish `127.0.0.1:1055:1055`, volume `nbpitang-wsl-tailscale-state`. Sem `up`, sem auth key.
- **Falta (Hil):**
  1. Rodar `scripts/wipe-wsl-docker.ps1` e digitar `APAGAR`.
  2. Copiar `.env.example` → `.env` e colar `TS_AUTHKEY`.
  3. `docker compose up -d` na pasta do projeto; provar com `curl.exe --socks5 127.0.0.1:1055 https://zlab.mandrill-court.ts.net`.
  4. Avisar o agente para reabrir a tarefa, marcar aceite e `Status: concluída`.

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Hil, um SOCKS em 127.0.0.1 e o NetExtender respira aliviado.
