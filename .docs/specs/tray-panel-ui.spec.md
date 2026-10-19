# Spec: tray-panel-ui

**Slug:** `tray-panel-ui`  
**Title:** Tray, six cards, host strip, PT-BR  
**Priority:** Must  
**Depends on:** `docker-ops`  
**Sources:** SPEC-ZLAB 2.0 · [ui-ux.md](../ui-ux.md)

## Goal

Tray-resident Z-Lab. Double-click opens the panel. Closing the panel does not exit. Icon `z-lab.ico`.

## In scope

| Surface | Behavior |
|---------|----------|
| Top | Session label, Liga, Desligar, Reiniciar, Entrar, Encerrar sessão, Sair |
| Host | Raw text of uptime, load, memory, three disks |
| Cards | Six services, verbs from the docker-ops spec, Abrir only for the two LAN URLs |
| Tailnet | HTTPS name as a label |
| Log pane | Remote stdout/stderr. No password |
| Tray menu | Status, Subir, Parar per service; Encerrar sessão; Sair |

## Out of scope

Disabled-template catalog, backup toolbar, Expor LAN, Tunnel, mode label `wsl docker`.

## Acceptance

- [ ] Layout matches [ui-ux.md](../ui-ux.md)
- [ ] Operator strings for these actions are PT-BR
