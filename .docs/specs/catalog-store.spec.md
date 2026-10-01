# Spec: catalog-store

**Slug:** `catalog-store`  
**Title:** JSON catalog load/save AppData, seed default, enable/disable  
**Priority:** Must (foundation)  
**Depends on:** —  
**Sources:** SPEC-ZLAB §5, §6.1 · FR-CAT-01–08 · AC-02, AC-04–06 · NFR-06

---

## 1. Goal

Provide the sole persistence boundary for the versioned services catalog: seed `%AppData%\ZLab\services.json` on first run from `config/services.default.json`, load/save/reload without process restart, and persist **Ativar / Desativar** (`enabled`) so UI and file stay aligned.

---

## 2. In scope

| Capability | Required behavior |
|------------|-------------------|
| First-run seed | If runtime file missing → copy default seed to `%AppData%\ZLab\services.json` |
| Load | Parse `version`, `settings`, `services[]` into in-memory document |
| Save | Persist document (atomic write preferred); round-trip `enabled` and other fields |
| SetEnabled | Toggle `enabled` for an `id`, save, return updated document |
| Reload | Re-read runtime file into memory without restarting the process |
| Open config folder | Ability for callers to open `%AppData%\ZLab` (FR-CAT-07 / RF19) |
| Default enabled set | Seed has `enabled: true` only for `9router` and `omniroute`; others `false` |

## 3. Out of scope (this slug)

- Rendering cards / tray menus (→ `tray-panel-ui`)
- Docker start/stop (→ `docker-ops`)
- Visual env/ports editor or create-service wizard (V2 / FR-OUT-03)
- Migrating schema beyond `version: 1` loaders

---

## 4. Requirements traceability

| ID | Statement |
|----|-----------|
| FR-CAT-01 / RF01 | Versioned JSON with `settings` + `ServiceItem` list |
| FR-CAT-02 | First run copies default seed |
| FR-CAT-03 / RF02 | Only `9router`, `omniroute` enabled in seed |
| FR-CAT-05 / RF04 | Ativar/Desativar persists `enabled` |
| FR-CAT-06 / RF18 | Recarregar refreshes from file without restart |
| FR-CAT-07 / RF19 | Open `%AppData%\ZLab` |
| FR-CAT-08 | Disabled items must not expose ops (store supplies `enabled`; UI enforces) |
| NFR-06 | After Ativar/Desativar + restart, `enabled` matches last JSON |

---

## 5. Functional behavior (testable)

### 5.1 Paths

| Role | Path |
|------|------|
| Seed (immutable by running app) | `config/services.default.json` (repo / Content copy to output) |
| Runtime | `%AppData%\ZLab\services.json` |

### 5.2 First run

1. App starts; runtime path does not exist (or directory missing).
2. Create `%AppData%\ZLab` if needed.
3. Copy seed bytes to runtime path.
4. Load succeeds; document contains both default-enabled gateways and disabled catalog templates.

### 5.3 Enable / disable

1. `SetEnabled(id, true|false)` updates that item only.
2. Write runtime JSON.
3. Subsequent `Load` / process restart reflects the same `enabled` values.
4. Does **not** create/start/stop containers by itself.

### 5.4 Reload failure

- Invalid / unreadable JSON → keep last good in-memory model; surface error to caller (PT-BR at UI layer).
- Do not blank the catalog to empty on parse failure.

### 5.5 Settings surface (store owns fields)

Store must round-trip `settings.backupFolder`, `settings.pollSeconds` (default 8), `settings.logTailLines` (default 80) per seed. Folder picker UX is in backup/UI slugs; store persists the string.

---

## 6. Acceptance criteria

| ID | Pass if |
|----|---------|
| CS-AC-01 | Missing runtime → file created from seed; contains `9router`/`omniroute` `enabled: true` and other catalog ids `enabled: false` (AC-02). |
| CS-AC-02 | `SetEnabled("postgres", true)` writes `enabled: true` for that id in runtime JSON (AC-04 data half). |
| CS-AC-03 | `SetEnabled` on an enabled id to `false` persists `enabled: false` (AC-05 data half). |
| CS-AC-04 | Manual edit of runtime JSON + Reload returns new values without process restart (AC-06 data half). |
| CS-AC-05 | Process restart after enable toggles reloads last persisted `enabled` (NFR-06). |
| CS-AC-06 | Corrupt JSON on Reload does not wipe last good in-memory document; error reported. |
| CS-AC-07 | Seed file on disk is not mutated by Ativar/Desativar (only AppData runtime). |

---

## 7. UX notes (from ui-ux.md / requirements juice)

| Concern | Guidance |
|---------|----------|
| Operator language | Persist is silent; **Ativar** / **Desativar** / **Recarregar** feedback lives in panel (busy “Recarregando…”, success ack, or keep prior UI on fail — AC-J06). |
| Microcopy after Ativar | “Serviço ativado. Use Iniciar para criar/subir o container se ainda não existir.” (SPEC §7.3) — store does not show MessageBox; UI does. |
| Desativado | Catalog badge copy: “Desativado — Ative para controlar este serviço.” |
| Abrir pasta | Opens OS folder; short PT-BR error if open fails; no modal on success. |
| Emotion | Trust via durable config — operator edits JSON safely; Reload never false-success on parse fail. |

---

## 8. Technical how-to (from technologies.md)

| Topic | Decision |
|-------|----------|
| Type | `ServiceCatalogStore` in `src/ZLab/Services/` |
| Models | `ServicesDocument`, `AppSettings`, `ServiceItem` (+ nested health/backup specs) |
| Serialization | `System.Text.Json`, camelCase matching seed; tolerate unknown fields within `version: 1` |
| Write | Atomic temp + replace recommended |
| Composition | Manual wiring in `Program.cs`; no DI framework required |
| Tests | xUnit; file I/O against temp AppData-like dirs; no live Docker |

Suggested operations (contracts, not code): `EnsureSeeded()`, `Load()`, `Save()`, `SetEnabled(id, enabled)`, `Reload()`, `GetConfigDirectory()`.

---

## 9. Open questions

None for V1 store core. Recarregar priority remains Should in requirements (OQ-01) but this slug still implements Reload for AC-06.
