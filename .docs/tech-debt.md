# Z-Lab — Tech debt (QA notes)

Suite: run `dotnet test ZLab.sln -c Release` after flicker / daily backup / tray status work.

## Automated coverage gaps

| Area | Covered in unit tests | Still manual / operator |
|------|----------------------|-------------------------|
| catalog-store | CS-AC-01–07 via `ServiceCatalogStoreTests` | — |
| docker-ops | Args builders, status parse, no-prune surface | Live Iniciar/Parar/Atualizar against WSL Docker (DO-AC-01–06) |
| status-notify | Transition detector (SN-AC-02–05 core) | Poll interval wiring + balloon delivery on real tray |
| backup-data | Stamp/auto/custom naming + daily scheduler rules | Hot/cold `docker cp` / backup-all / live daily job on volumes |
| lan-expose | Impl present (`LanExposeHelper`); no dedicated tests yet | netsh portproxy + Admin elevation on host |
| tray-panel-ui / exit-shutdown | Impl in `MainForm` / `TrayApp` | WinForms/tray/MessageBox ACs (UI-*, EX-*); confirm no flicker + tray Status line |

## Suggested follow-ups (non-blocking for slug checkboxes)

1. Add injectable-runner unit tests for `LanExposeHelper` (portproxy/firewall arg builders + elevation result).
2. Extract stop-all filter + exit-outcome logic from `TrayApp.RequestExitAsync` for pure unit tests (EX-AC-03/04/07).
3. Optional smoke script for live Docker ACs on operator WSL2 machine.
4. Anti-flicker: panel poll uses `ApplyStatuses` in-place; structural rebuild only on enable/disable/reload (UI-AC-12).

## Network policy

**IPv6 is not used in this lab.** Keep WSL/Docker IPv4-only (`scripts/fix-wsl-docker-ipv4.sh`, `.cursor/rules/no-ipv6.mdc`, SPEC RNF07). Do not treat dual-stack/IPv6 enablement as a fix for registry pull errors.