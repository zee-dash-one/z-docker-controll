# Spec: tray-panel-ui

**Slug:** `tray-panel-ui`  
**Title:** MainForm grid, catalog Desativado, PT-BR, Z-Lab icon  
**Priority:** Must  
**Depends on:** `catalog-store`, `docker-ops`  
**Sources:** SPEC-ZLAB §7 · FR-UX-01–08 · FR-CAT-04, FR-CAT-08 · AC-01, AC-03–06, AC-11–14 · LF-01–07 · juice AC-J*

---

## 1. Goal

Deliver the operator shell: tray-resident **Z-Lab** with custom icon, double-click panel showing **enabled service cards** (flow/grid for N items) plus **Catálogo (desativados)**, PT-BR microcopy, and wiring to catalog store and container ops — without treating window close as Exit.

---

## 2. In scope

| Surface | Behavior |
|---------|----------|
| Brand | Title **Z-Lab**; icon `z-lab.ico` on exe, form, tray |
| Tray | Start minimized to tray; double-click opens panel; close panel → tray (containers not stopped); submenu per enabled item shows **Status:** line (non-clickable) then actions; NotifyIcon tooltip summarizes statuses |
| Panel L1–L7 | Layout per SPEC §7.2 / ui-ux §2.1: mode label, backup folder tools, Serviços ativos, Catálogo, Logs |
| Enabled cards | Full ops for enabled items only; Desativar; conditional Abrir/Backup/Expor |
| Catalog | Visible disabled items; badge Desativado; **Ativar** only (no ops ghosts) |
| Recarregar / Abrir config | Toolbar actions calling store |
| Language | All primary operator strings PT-BR |
| Empty states | Empty active area → point to Catálogo → Ativar; empty logs → explicit copy |

## 3. Out of scope (this slug)

- StatusWatcher timer logic (→ `status-notify`; UI may host NotifyIcon used by watcher)
- Full backup algorithm (→ `backup-data`; UI may host buttons/folder browser)
- LAN netsh details (→ `lan-expose`)
- Exit stop-all semantics (→ `exit-shutdown`; UI may show Sair entry that calls that flow)
- Modern toast NuGet, sound packs, particle FX

---

## 4. Requirements traceability

| ID | Statement |
|----|-----------|
| FR-UX-01 / RF15 / AC-01 | Name Z-Lab + custom icon |
| FR-UX-02 / RF16 / AC-11 | Tray residency; close ≠ exit |
| FR-UX-06–07 / RNF03 | PT-BR; distinct enabled vs catalog; N-item grid |
| FR-CAT-04 / RF03 | Disabled visible with Desativado |
| FR-CAT-08 / AC-03 | Disabled: no operational actions |
| FR-UX-08 | Tray submenu per enabled item + Exit; optional quick Ativar |
| AC-J01–J06, LF-* | Busy, empty, hidden actions, branding cues |

---

## 5. Functional behavior (testable)

### 5.1 Startup

1. Process starts → tray icon visible with `z-lab.ico`.
2. Panel not required to show until double-click (SPEC decision: tray only on start).
3. Catalog store EnsureSeeded/Load already done by composition root.

### 5.2 Panel sections

- **Serviços ativos:** one card per `enabled == true`; stronger surface than catalog.
- **Catálogo (desativados):** items with `enabled == false`; muted; Ativar + notes.
- Header shows Docker mode from `DockerCli` when known.
- Logs pane shared; monospace-friendly control.

### 5.3 Enable gates

- Operational buttons only on enabled cards.
- After Ativar: item moves to active area with controls; store persisted.
- After Desativar: operational actions removed; appears in catalog with Desativado.

### 5.4 Conditional actions

| Condition | UI |
|-----------|-----|
| `openUrl` empty | Hide Abrir |
| `backup.mode == none` | Hide Backup |
| `exposeLan == false` | Hide Expor LAN |
| Not Up | Stats inactive/disabled with reason |

### 5.5 Window close

Closing MainForm hides to tray; does **not** stop containers; does **not** end process.

---

## 6. Acceptance criteria

| ID | Pass if |
|----|---------|
| UI-AC-01 | Title/tray/exe show **Z-Lab** and `z-lab.ico` (AC-01, LF-01). |
| UI-AC-02 | Only enabled items show operational controls (AC-03). |
| UI-AC-03 | Ativar on catalog item (e.g. postgres) persists + moves to active area with controls (AC-04). |
| UI-AC-04 | Desativar removes ops, shows Desativado in catalog, persists JSON (AC-05). |
| UI-AC-05 | Recarregar after manual JSON edit refreshes UI without restart (AC-06); fail keeps prior UI (AC-J06). |
| UI-AC-06 | Close panel → tray; containers not stopped by close alone (AC-11). |
| UI-AC-07 | Primary path strings PT-BR including Desativado / Ativar / toolbar (AC-13, LF-06). |
| UI-AC-08 | Layout is flow/grid for N items — not hard-coded two-column product model (FR-UX-07, LF-07). |
| UI-AC-09 | Empty active area and empty logs show PT-BR empty copy (AC-J03). |
| UI-AC-10 | Iniciar/Parar/Reiniciar/Atualizar show per-item busy before completion (AC-J01). |
| UI-AC-11 | No V1 UI for HTTP health, pgDump, visual env editor, OS autostart, compose, prune (AC-14–15). |
| UI-AC-12 | Status poll updates card labels in-place (no full card rebuild / flicker). |
| UI-AC-13 | Tray submenu shows `Status: …` under each enabled service name; tooltip summarizes enabled statuses. |

---

## 7. UX notes (from ui-ux.md)

| Topic | Guidance |
|-------|----------|
| Tone | Calm lab bench — clear verbs, honest status; no gamification |
| Hierarchy | See running → act → inspect → catalog → exit safely |
| Palette | Brand orange `#E67A2E` accents only; chrome light gray bench; status colors with text |
| Catalog | `chrome.surfaceMuted` + gray Desativado badge — never looks like error |
| Motion | Busy labels + enable/disable only; status poll is in-place (ApplyStatuses), not Controls.Clear |
| Sound | Silent by default; no app sound pack |
| Card order | displayName → status → meta → actions (Iniciar/Parar first; Atualizar/Backup/Expor later; Desativar last) |
| Microcopy | SPEC §7.3 seeds for Desativado, Ativar, Sair (Sair dialog owned with exit-shutdown) |

---

## 8. Technical how-to (from technologies.md)

| Topic | Decision |
|-------|----------|
| Stack | .NET 8 WinForms (`net8.0-windows`), `MainForm.cs`, `TrayApp.cs`, `NotifyIcon` |
| Assets | `src/ZLab/Assets/z-lab.ico` |
| Async | All Docker/file work off UI thread; progress or disabled busy control visible |
| Composition | Manual wiring; panel calls `ServiceCatalogStore` + `ContainerService` |
| Forbidden UI | No Electron/WPF; no toast NuGet in V1 |

---

## 9. Open questions

| # | Note |
|---|------|
| OQ-04 | Tray quick-Ativar for catalog items is Should/optional in V1 — implement if cheap; not required for this slug’s Must ACs |
