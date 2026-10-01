# Spec: backup-data

**Slug:** `backup-data`  
**Title:** volumeCopy stamped/named/auto folders + backup-all + daily overwrite slot  
**Priority:** Must  
**Depends on:** `catalog-store`, `docker-ops`, `tray-panel-ui`  
**Sources:** SPEC-ZLAB §6.5 · FR-BKP-01–06 · RF11 · RNF05 · AC-10 · OQ-02 (continue on mid-batch fail)

---

## 1. Goal

Back up eligible service data with `backup.mode == volumeCopy` into stamped or named folders (manual) or an overwritable `auto/{id}` slot (daily), under `settings.backupFolder`, without deleting Docker volumes, with secrets warning and optional Stop→Backup→Start after hot failure (manual only).

---

## 2. In scope

| Capability | Behavior |
|------------|----------|
| Eligibility | Offer Backup only when `backup.mode != none` (`volumeCopy` in V1 seed) |
| Folder | Use `settings.backupFolder`; if empty → `FolderBrowserDialog` before proceed; persist choice via store when appropriate |
| Manual artifact | `{id}_{yyyy-MM-dd_HHmm}\` or `{id}_{nome}` (optional name dialog); must not delete source volume; does not touch `auto/` |
| Auto artifact | `{backupFolder}\auto\{id}\` overwritten once per day while app is open (single slot per service) |
| Copy strategy | Container Up/Down → `docker cp` with WSL host path mapping; Absent + named volume → alpine helper |
| 9Router volume | Seed/migration: `9router-data:/app/data` |
| Scheduling | `backupDailyEnabled`, `backupDailyTime` (default `03:00`), `lastAutoBackupDate`; hot-only (no cold prompt); retry ≤ every 30 min on failure |
| Hot attempt | Manual: try while running; on failure **and container Up** offer Stop → Backup → Start; cancel leaves state unchanged. Already stopped → docker cp / volume helper (no stop prompt) |
| Backup all | Action for all eligible items; continue remaining on mid-batch failure; summary of ok/fail/skipped |
| Warning | UI warns backups may contain secrets/keys |
| Notify | Managed+enabled may get one backup ok/fail balloon; daily job uses balloon on complete/fail |

## 3. Out of scope

- `pgDump` / engine-specific dumps (V2)
- Uploading backups to cloud
- Deleting volumes or prune
- OS Task Scheduler / run when app is closed

---

## 4. Requirements traceability

| ID | Statement |
|----|-----------|
| FR-BKP-01–06 / RF11 | volumeCopy rules, folder, stamp, hot+fallback, backup-all, secrets warning |
| AC-10 | Stamped folder created; volume not deleted |
| RNF05 / NFR-04 | Secrets warning; do not log secrets |
| Juice | Busy “Backup…”; batch progress; continue-not-abort (requirements OQ-02 closure) |

---

## 5. Functional behavior (testable)

### 5.1 Single-item backup

1. Guard: `mode == none` → action not offered / no-op.
2. Show secrets warning before/during.
3. Resolve folder (settings or picker).
4. Prompt optional name; empty → stamp; else `{id}_{sanitized}`.
5. Create destination subdirectory; copy volume/data via `docker cp` / volume copy strategy.
6. Source volume still present after success.
7. Hot fail → prompt optional cold path; decline → unchanged.

### 5.2 Backup all eligible

1. Eligible set = enabled items with `backup.mode != none` (disabled catalog items are not operated).
2. Run sequentially or with clear per-item progress.
3. On one failure → continue others; end with summary (succeeded / failed / skipped).
4. Successful artifacts retained even if later items fail.

### 5.3 Daily auto

1. While process alive: if enabled, folder set, not yet today, local time ≥ `backupDailyTime` → backup all eligible into `auto/{id}` (overwrite).
2. Empty folder → do not run; one discreet warning.
3. Failure → balloon; retry at most every 30 minutes same day.
4. Full cycle success → persist `lastAutoBackupDate`.

### 5.4 Safety

- Never call volume delete or prune as part of backup.
- Do not print env secrets into app logs during backup.

---

## 6. Acceptance criteria

| ID | Pass if |
|----|---------|
| BK-AC-01 | volumeCopy backup creates `{id}_{yyyy-MM-dd_HHmm}` under configured path; Docker volume still exists (AC-10). |
| BK-AC-02 | Items with `backup.mode == none` have no Backup action and are excluded from backup-all eligible set (FR-BKP-01, §5.6). |
| BK-AC-03 | Empty `backupFolder` triggers folder picker before copy proceeds (FR-BKP-02). |
| BK-AC-04 | Secrets warning is visible in the backup flow (FR-BKP-06). |
| BK-AC-05 | Hot failure offers Stop→Backup→Start; cancel leaves containers/data unchanged (§5.2 requirements). |
| BK-AC-06 | Backup-all continues after a mid-batch failure and reports summary (requirements juice / OQ-02). |
| BK-AC-07 | No UI path deletes volumes during backup (RNF06). |
| BK-AC-08 | Manual named backup creates `{id}_{nome}`; does not overwrite `auto/{id}`. |
| BK-AC-09 | Daily job writes/overwrites `auto/{id}` and records `lastAutoBackupDate` on success. |

---

## 7. UX notes (from ui-ux.md)

| Topic | Guidance |
|-------|----------|
| Tone | Seriousness for risk — secrets warning is mandatory, not cute |
| Busy | Per-item “Backup…”; batch shows which item is running |
| Success | In-panel path/folder acknowledgement; optional one managed balloon |
| Folder control | L2 toolbar: Pasta backup + Alterar (`FolderBrowserDialog`) |
| Hide | No Backup button when mode none (absence = signal) |
| Emotion | Trust: never imply data was wiped; cold-path is optional and cancel-safe |
| Name | Optional name dialog before manual backup |

---

## 8. Technical how-to (from technologies.md)

| Topic | Decision |
|-------|----------|
| Type | `BackupService`, `BackupFolderNaming`, `ScheduledBackupService`, `DailyBackupScheduler` |
| I/O | `docker cp` / volume copy via `DockerCli`; output under operator folder |
| Modes | `volumeCopy` \| `none` only in V1 |
| UI | WinForms `FolderBrowserDialog`; MessageBox/prompts PT-BR |
| Tests | Temp dirs + naming/scheduler unit tests; eligibility filter; no delete-volume calls |

---

## 9. Open questions

Batch continue-on-fail is the V1 default per requirements juice addendum; if SPEC later mandates abort, update this spec and AC together.
