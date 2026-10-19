# Spec: Z-Lab

**Versão:** 2.0  
**Data:** 2026-10-19  
**Idioma:** Português (Brasil)  
**Status:** Contrato vigente do monitor do z-server-01  
**Nome do produto:** **Z-Lab** (`ZLab.exe`)  
**Plano de origem:** [`.docs/plans/zlab-no-z-server-01.md`](../.docs/plans/zlab-no-z-server-01.md)  
**Histórico:** a V1 (v0.5) operava Docker no WSL. Esse comportamento saiu do exe. O texto antigo permanece no Git.

---

## 1. Resumo

O Z-Lab é um programa Windows de bandeja. O alvo é o z-server-01 (`192.168.2.37`, conta pedida no diálogo, Compose em `/opt/zlab`).

A lista tem seis serviços e não cresce por JSON: `9router`, `omniroute`, `mausbot`, `tailscale`, `tailscale-9router`, `tailscale-mausbot`. Os três últimos usam `--profile tailnet`.

Cada sessão pede usuário e senha. A senha fica na memória e é zerada ao encerrar a sessão ou sair. A chave `id_ed25519_zserver` não autentica o exe. A chave de host tem de já estar em `known_hosts`.

## 2. O que o painel faz

- Lê uptime, carga, memória e disco (`/`, `/var/lib/docker`, `/data`) no intervalo `pollSeconds` (padrão 8).
- Por serviço: Subir, Parar, Atualizar (`pull` e, se o código for 0, `up -d`), Criar (`up -d` só sem container), Logs, Uso.
- Abre `http://192.168.2.37:20128` e `:20129`. Os nomes `https://*.mandrill-court.ts.net` são rótulo.
- Liga envia o pacote mágico (MAC `FC:AA:14:F9:80:D4`, UDP 9, broadcast `192.168.2.255`) e espera a porta 22 por até 2 minutos.
- Desligar e Reiniciar pedem confirmação e mandam `shutdown -h now` ou `reboot`.
- Sair não para container no servidor.

## 3. Fora do exe

- Backup de volume (débito).
- Portproxy, tunnel Cloudflare, hosts locais, catálogo postgres/redis.
- `docker compose down`, `rm`, `prune`, `exec`.
- Aplicar o sudoers e apagar o Docker do WSL: procedimentos manuais, ainda não executados.
- Health HTTP/TCP.
- Instalar o app Tailscale no Windows. O NetExtender em túnel dividido conflita com ele.
- O Docker do WSL depois da limpeza. O plano seguinte é um único container Tailscale nesse Docker, para o Windows usar a tailnet. 9Router, OmniRoute e MausBot não voltam para o WSL. Isso não faz parte deste exe.

## 4. Aceite

Usuário e senha em toda sessão, comandos só da lista fechada, um serviço por vez, perfil `tailnet` nos três nós, URLs da LAN, Wake-on-LAN no MAC já usado pelo script do servidor, desligar e reiniciar com confirmação.
