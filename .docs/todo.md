# Z-Lab 2.0 — Todo

**Authority:** [`docs/SPEC-ZLAB.md`](../docs/SPEC-ZLAB.md)  
**Debt and open questions:** [tech-debt.md](tech-debt.md)

## In the exe

- [x] Closed command catalog (`RemoteCommands`) and unit tests
- [x] SSH.NET session, login dialog, `known_hosts` check, password cleared on session end
- [x] Wake-on-LAN packet and TCP/22 wait
- [x] Panel: host readings, six cards, LAN open, tailnet labels, power confirms
- [x] Exit does not stop remote containers
- [x] AppData settings for poll interval; service list is not JSON

## Operator, not run by the agent

- [ ] Confirm `sshd` accepts password for `melchior` (`zlab/aplicar-sudoers.sh` prints the check)
- [ ] `visudo` the file `z-server-01/zlab/sudoers-zlab-monitor`, then remove generic NOPASSWD and the `docker` group, with a key session still open
- [ ] If `reboot` is a symlink to `systemctl`, edit that sudoers line first
- [ ] After the monitor talks to the server, run `scripts/wipe-wsl-docker.ps1` and type `APAGAR`
- [ ] After that wipe, one WSL Docker project only: Tailscale, so Windows can use the tailnet without the Tailscale app (NetExtender). 9Router, OmniRoute, and MausBot stay on z-server-01. How the traffic reaches Windows is still open.
- [ ] BIOS task 08 still gates whether Liga actually powers the machine on

## Debt

- [ ] Backup of server volumes (click and daily) — not in this version
- [ ] Delete unused V1 types from `ZLab.Core` after the WSL wipe
