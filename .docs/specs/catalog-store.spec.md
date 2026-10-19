# Spec: catalog-store

**Slug:** `catalog-store`  
**Title:** Closed service list in code; poll settings in AppData  
**Priority:** Must  
**Sources:** SPEC-ZLAB 2.0 · FR-CAT-01–02

## Goal

The monitor does not invent a seventh service from JSON. `%AppData%\ZLab\settings.json` stores `pollSeconds` (default 8) and `logTailLines` (default 80).

## In scope

| Item | Behavior |
|------|----------|
| Services | The six ids in `RemoteCommands.Services` |
| Settings file | Created with defaults when missing |
| Old catalog | `config/services.default.json` is not a service seed. V1 fixture lives at `tests/ZLab.Tests/Fixtures/services.v1.json` for the unused store tests |

## Out of scope

Ativar/Desativar, templates (postgres, redis, …), reading `%AppData%\ZLab\services.json`.

## Acceptance

- [ ] A name outside the six throws from `RemoteCommands.Require`
- [ ] First run writes `settings.json` without a service array
