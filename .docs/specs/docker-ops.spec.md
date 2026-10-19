# Spec: docker-ops

**Slug:** `docker-ops`  
**Title:** Closed Compose commands on z-server-01 over SSH  
**Priority:** Must  
**Depends on:** `catalog-store`  
**Sources:** SPEC-ZLAB 2.0 · FR-OPS-01–04 · FR-SSH-*

## Goal

Each button runs one prebuilt `sudo -n` command. Update is two commands. The WSL Docker CLI is not the engine.

## In scope

| Action | Command |
|--------|---------|
| Subir | `start <serviço>` when the container is stopped |
| Parar | `stop <serviço>` when it is up |
| Criar | `up -d <serviço>` when it is absent |
| Atualizar | confirm, `pull`, then `up -d` only if pull exits 0 |
| Estado | `ps -a --format json <serviço>` on the poll |
| Logs | `logs --tail 80 <serviço>` |
| Uso | `docker stats --no-stream --format {{.CPUPerc}}/{{.MemUsage}}/{{.PIDs}} <serviço>` |

Tailnet ids insert `--profile tailnet` immediately after `compose`. Paths are absolute (`/usr/bin/docker`, project `/opt/zlab`).

## Out of scope

`docker run` on WSL, restart of a single container, `down`, `rm`, `prune`, `exec`, portproxy.

## Acceptance

- [ ] `RemoteCommandCatalogTests` locks the argv, including rejection of unknown names
- [ ] A failed pull does not send `up -d` (`ServerMonitor.UpdateAsync`)
