# Spec: exit-shutdown

**Slug:** `exit-shutdown`  
**Title:** Leave the server running; confirm power buttons  
**Priority:** Must  
**Sources:** FR-PWR-02 · FR-EXIT-01

## Goal

Sair confirms and exits the process without `docker compose stop`. Desligar and Reiniciar are separate buttons with the confirmation texts in `RemoteCommands`.

## In scope

| Action | Effect |
|--------|--------|
| Close panel | Hide, stay in the tray |
| Encerrar sessão | Disconnect SSH and zero the local password buffer |
| Sair | Confirm: “Sair do Z-Lab? Os containers no z-server-01 continuam em execução.” |
| Desligar | Confirm, then `sudo -n /usr/sbin/shutdown -h now` |
| Reiniciar | Confirm, then `sudo -n /usr/sbin/reboot` |
| Cancel | Send nothing |

When port 22 is closed, compose and power buttons are inert. Liga remains.

## Out of scope

Stop-all on exit. Fail-closed “keep the app open because a container did not stop”.

## Acceptance

- [ ] `TrayApp.ExitConfirmText` does not mention stopping services
- [ ] Cancel on Desligar/Reiniciar does not open an SSH command
