# Spec: docker-ops

**Slug:** `docker-ops`  
**Title:** Start/stop/restart/logs/stats/update/open via Docker CLI or WSL  
**Priority:** Must  
**Depends on:** `catalog-store` (ServiceItem fields)  
**Sources:** SPEC-ZLAB §6.2 · FR-OPS-01–09 · RF05–07, RF13–14 · AC-07, AC-12, AC-15 · NFR-03–05

---

## 1. Goal

Map each **enabled** `ServiceItem` to container engine operations through a single CLI boundary (`docker.exe` or `wsl docker`), without freezing the UI thread, and without offering destructive engine cleanup.

---

## 2. In scope

| Action (PT-BR UI label) | Behavior |
|-------------------------|----------|
| Iniciar | If container absent → create/run from template fields; if exists → start |
| Parar | `stop` with item `stopTimeoutSeconds`; **no** confirmation |
| Reiniciar | `restart` |
| Status | Container/engine state (`health.type: docker` only in V1) |
| Stats | Live resource stats when Up; unavailable when not Up |
| Logs | Tail `settings.logTailLines` (default 80) |
| Atualizar | Confirm → pull image → recreate preserving mounts/volumes |
| Abrir | If `openUrl` non-empty → open OS default browser; if empty → action hidden by UI |

Engine discovery: prefer host `docker.exe` when usable; else `wsl -d Ubuntu-24.04 docker …`; expose mode label `docker` \| `wsl docker` to UI.

## 3. Out of scope (this slug)

- Poll timer / balloons (→ `status-notify`)
- Backup volume copy (→ `backup-data`)
- LAN portproxy (→ `lan-expose`)
- HTTP/TCP health probes (V2)
- `docker system prune`, volume delete (forbidden V1)
- Compose multi-service orchestration

---

## 4. Requirements traceability

| ID | Statement |
|----|-----------|
| FR-OPS-01–08 / RF05–07 | Per-item ops for enabled items |
| FR-OPS-09 / RF14 | Host Docker CLI or WSL Docker |
| RF13 / AC-12 | Confirm Atualizar; Parar without confirm |
| RNF04 / NFR-03 | Async; UI stays responsive |
| RNF05 | Do not log `env` secrets |
| RNF06 / AC-15 | No prune / delete-volume affordances |
| AC-07 | Status/stats/logs/update work against live engine for existing enabled containers |

---

## 5. Functional behavior (testable)

### 5.1 Iniciar

- Absent container → `docker run` (or equivalent) using `image`, ports, `volumes`, `env`, `containerName` from item.
- Existing stopped → `docker start`.
- Failure → structured error string (PT-BR at UI); prior state unchanged when start fails.

### 5.2 Parar / Reiniciar

- Parar uses `-t` / timeout = `stopTimeoutSeconds` (defaults per seed; OmniRoute 40).
- No confirm dialog for Parar.
- Reiniciar settles to reported engine state (typically Up).

### 5.3 Observability

- Status from inspect/ps-style engine state (not HTTP).
- Stats only meaningful when Up; callers/UI disable or mark inactive otherwise.
- Logs return last N lines; empty output is a valid empty result (UI shows empty copy).

### 5.4 Atualizar

1. Caller shows confirm; decline → **no-op** (no pull).
2. On accept: pull `image`, recreate container preserving mounts/volumes defined on the item.
3. Report ok/fail to caller (notify layer may balloon once for managed items).

### 5.5 Abrir

- Non-empty `openUrl` → OS open; empty → UI hides control (ops may no-op if called).

### 5.6 Mode & unavailability

- If neither host nor WSL Docker works → clear unavailability; mutating ops fail with same reason.
- Active mode string available for panel header (“Modo: …”).

### 5.7 Safety

- No API or method that runs prune or deletes volumes.
- CLI argv arrays preferred; do not log secret env values.

---

## 6. Acceptance criteria

| ID | Pass if |
|----|---------|
| DO-AC-01 | For an existing enabled container, Status returns engine state; Stats when Up; Logs return a tail; Atualizar after confirm completes pull/recreate path (AC-07). |
| DO-AC-02 | Iniciar creates/runs when absent and starts when present (FR-OPS-01). |
| DO-AC-03 | Parar uses item timeout and requires no confirmation API (AC-12). |
| DO-AC-04 | Declined Atualizar confirm path performs no pull/recreate (AC-J02 / §5.3 requirements). |
| DO-AC-05 | When host docker unavailable, WSL path is used if available; mode label reflects path (FR-OPS-09). |
| DO-AC-06 | Both paths unavailable → ops fail with clear reason; no crash. |
| DO-AC-07 | Public surface has no prune / volume-delete operation (AC-15). |
| DO-AC-08 | Long ops are awaitable off UI thread (contract: async API); secrets not written to logs (NFR-03/04). |

---

## 7. UX notes (from ui-ux.md / juice)

| Action | Immediate | Complete | Fail / cancel |
|--------|-----------|----------|---------------|
| Iniciar | Busy “Iniciando…”; disable conflicting mutating actions on that card | Status → Up / created | Inline PT-BR error; re-enable |
| Parar | “Parando…”; no confirm | Stopped/Exited | Error on card |
| Reiniciar | “Reiniciando…” | Settled status | Error + restore |
| Atualizar | Confirm first; then “Atualizando…” | Success on card | Decline = silent no-op; fail = error |
| Logs | Loading cue in logs pane | Tail or empty-state copy | PT-BR error in logs area |
| Stats | Only when Up | Refresh without freeze | Short error; no fake-live stale |
| Abrir | Hidden if no URL | OS opens URL | Brief card error if open fails |

Busy exclusivity is per-item; panel chrome remains usable (NFR-03). Status meaning uses **text + color** (`status.up` / `down` / `absent` / `busy` / `error` tokens from ui-ux.md).

---

## 8. Technical how-to (from technologies.md)

| Topic | Decision |
|-------|----------|
| Types | `DockerCli` (process spawn, mode discovery) + `ContainerService` (item → ops) |
| Distro constant | `Ubuntu-24.04` per SPEC inventory |
| Prefer | CLI over Docker.DotNet SDK in V1 |
| Publish/run | Built in WSL SDK 8; runs on Windows host |
| Tests | Mock `DockerCli`; assert argv construction and branching (absent vs start, timeout, no prune) |

`ContainerService` is the only place UI should call for start/stop/restart/update/stats/logs/run; `DockerCli` is the sole I/O boundary to the engine.

---

## 9. Open questions

None blocking V1. Exact recreate flags stay implementation detail as long as mounts/volumes are preserved per SPEC §6.2.
