# Requirements: Z-Lab 2.0

**Status:** Alinhado ao monitor do z-server-01  
**Authority:** [`docs/SPEC-ZLAB.md`](../docs/SPEC-ZLAB.md) 2.0  
**Runtime:** `%AppData%\ZLab\settings.json` (`pollSeconds`, `logTailLines`)  
**Language of UI:** Portuguese (Brazil)

A V1 pedia catálogo JSON, Docker no WSL, backup, portproxy e parar tudo ao sair. Isso não é mais requisito do exe.

## 1. Scope

1. Tray + panel that operates the six Compose services on `192.168.2.37`.
2. Password SSH session. No disk key. Host key must already be in `known_hosts`.
3. Closed commands only (`sudo -n` + absolute paths). Update is two calls: `pull`, then `up -d` when pull exits 0.
4. Host readings on the poll interval, plus per-service `ps -a --format json`.
5. Wake-on-LAN, then wait for TCP 22 (2 minutes, 5 second pause) before the password dialog.
6. Shutdown and reboot ask for confirmation. Exit does not stop containers.

Out of scope for this version: volume backup, LAN portproxy, Cloudflare tunnel, HTTP/TCP health, `docker exec`, applying sudoers, wiping WSL Docker.

## 2. Functional requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-SSH-01 | Every session asks for user and password. The user field starts empty. Host and port are fixed and shown read-only. | Must |
| FR-SSH-02 | Password is held in memory and cleared when the session ends or the process exits. It is not written to JSON, logs, or argv. | Must |
| FR-SSH-03 | The client offers password authentication only. A host key that does not match `known_hosts` aborts the session. | Must |
| FR-SSH-04 | One SSH connection per session. A dropped socket reconnects with the same password. Authentication failure ends the session. | Must |
| FR-CAT-01 | The six service ids are fixed in code. The app does not load `services.json` as a catalog. | Must |
| FR-CAT-02 | `%AppData%\ZLab\settings.json` stores `pollSeconds` (default 8) and `logTailLines` (default 80). | Must |
| FR-OPS-01 | Start / stop / create / logs / stats / update use only `RemoteCommands` strings. | Must |
| FR-OPS-02 | Create (`up -d`) is enabled only when the container is absent. Start is enabled only when it is stopped. | Must |
| FR-OPS-03 | Update asks for confirmation, runs `pull`, and runs `up -d` only after exit code 0. | Must |
| FR-OPS-04 | Tailnet services send `--profile tailnet` after `compose`. | Must |
| FR-OBS-01 | Each poll reads uptime, `/proc/loadavg`, `free -h`, and `df` of `/`, `/var/lib/docker`, and `/data`. | Must |
| FR-OBS-02 | A failed `/data` reading shows absence. The app does not create that directory. | Must |
| FR-OBS-03 | Tray balloons fire only on Up ↔ Down/Absent transitions after the first sample. | Must |
| FR-LAN-01 | Open URL is offered for 9Router and OmniRoute on the server LAN addresses. Tailnet HTTPS names are labels. | Must |
| FR-PWR-01 | Liga sends the 102-byte magic packet and then waits for port 22. | Must |
| FR-PWR-02 | Desligar and Reiniciar show the confirmation texts from the plan before sending the command. | Must |
| FR-EXIT-01 | Closing the panel returns to the tray. Sair asks for confirmation and does not stop remote containers. | Must |

## 3. Acceptance (operator, still manual)

- [ ] Login with password against z-server-01, with the host key already known.
- [ ] Poll shows host text and six statuses.
- [ ] Subir / Parar / Criar / Atualizar / Logs / Uso on one service.
- [ ] Liga times out honestly if the BIOS wake task is still open.
- [ ] Desligar and Reiniciar require confirmation.
- [ ] Sair leaves the server containers running.
