# Z-Lab V1 — Implementation Todo

**Authority:** [`docs/SPEC-ZLAB.md`](../docs/SPEC-ZLAB.md) v0.5 · [`.docs/requirements.md`](requirements.md)  
**Stack:** [`.docs/technologies.md`](technologies.md) · UX: [`.docs/ui-ux.md`](ui-ux.md)  
**Specs:** [`.docs/specs/`](specs/)

Dependency order top → bottom. Do not mark `[x]` until QA verify for that slug passes.

## Tasks

- [x] catalog-store: JSON catalog load/save AppData, seed default, enable/disable
- [x] docker-ops: Start/stop/restart/logs/stats/update/open via Docker CLI or WSL
- [x] tray-panel-ui: MainForm grid, catalog Desativado, PT-BR, Z-Lab icon
- [x] status-notify: StatusWatcher poll + transition-only balloons
- [x] backup-data: volumeCopy stamped folders + backup-all eligible
- [x] lan-expose: exposeLan helper (portproxy + firewall, Admin)
- [x] exit-shutdown: Sair confirms, stop managed+enabled, fail keeps app open

## Dependency notes

| Slug | Depends on | Why |
|------|------------|-----|
| catalog-store | — | Foundation: `ServicesDocument`, AppData path, Ativar/Desativar |
| docker-ops | catalog-store (models) | Ops need `ServiceItem` fields; CLI is sole engine I/O |
| tray-panel-ui | catalog-store, docker-ops | Panel binds store + ops; tray shell |
| status-notify | catalog-store, docker-ops, tray-panel-ui | Poll uses store filters + Docker status + NotifyIcon |
| backup-data | catalog-store, docker-ops, tray-panel-ui | Settings/folder UI + `docker cp` / volumes |
| lan-expose | catalog-store, tray-panel-ui | `exposeLan` / `firewallRuleName`; Admin UX |
| exit-shutdown | catalog-store, docker-ops, tray-panel-ui | Stop-all + confirm + process lifecycle |

`lan-expose` and `backup-data` may proceed in parallel after `tray-panel-ui` if staffing allows; both must complete before V1 acceptance that lists them.

## Definition of Done (per slug)

1. Spec file exists at `.docs/specs/{slug}.spec.md` and matches this todo title.
2. Implementation satisfies every **Acceptance** row in that spec (pass/fail).
3. No contradiction with SPEC-ZLAB v0.5 or `.docs/requirements.md` (SPEC wins on conflict).
4. Operator-facing strings for that slug’s surfaces are **PT-BR**.
5. Unit/contract tests cover the slug’s testable core where engine I/O can be mocked (`DockerCli` boundary).
6. No V1 out-of-scope features introduced (HTTP/TCP health, pgDump, visual env editor, OS autostart, compose orchestration, prune/delete volumes).
7. QA verify for the slug is green before the checkbox is marked `[x]`.

## Global V1 Done

All seven slugs `[x]` **and** requirements AC-01–AC-15 plus AC-J01–AC-J06 and SPEC §9 criteria are satisfied on the operator environment (Windows UI + WSL2 Docker).
