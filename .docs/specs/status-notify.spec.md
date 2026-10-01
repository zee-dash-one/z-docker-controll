# Spec: status-notify

**Slug:** `status-notify`  
**Title:** StatusWatcher poll + transition-only balloons  
**Priority:** Must  
**Depends on:** `catalog-store`, `docker-ops`, `tray-panel-ui` (NotifyIcon host)  
**Sources:** SPEC-ZLAB §6.3 · FR-POLL-01 · FR-NOT-01–03 · RF08–10 · AC-09 · NFR-07

---

## 1. Goal

Poll container status every `settings.pollSeconds` (default 8) and raise tray notifications **only** on state transitions for items that are both `managed == true` and `enabled == true`, with a silent baseline on the first poll after start.

---

## 2. In scope

| Capability | Behavior |
|------------|----------|
| Poll loop | Periodic status fetch for relevant items |
| Filter | Notify only `managed && enabled` |
| Transition-only | Same state on consecutive polls → no new balloon |
| Baseline | First poll establishes prior state; no flood |
| Events | caiu / subiu; update result and backup result may be raised by ops/backup callers through the same notify channel |
| Panel paint | Status text/color on cards updates from poll (juice without balloons on first paint) |

## 3. Out of scope

- HTTP/TCP health (V2)
- Custom sound / toast framework
- Notifying disabled or unmanaged items
- Stopping containers on Down

---

## 4. Requirements traceability

| ID | Statement |
|----|-----------|
| FR-POLL-01 / RF08 / NFR-07 | Poll every `pollSeconds` (seed 8) |
| FR-NOT-01 / RF09 | Notify only managed+enabled |
| FR-NOT-02 / RF10 / AC-09 | Transitions only |
| FR-NOT-03 | Silent first poll baseline |
| Juice | Ambient balloons ≠ click-driven ops feedback |

---

## 5. Functional behavior (testable)

### 5.1 Start

1. Watcher starts after catalog load.
2. First successful poll per item records baseline; **no** caiu/subiu balloons.
3. Panel may still show status chips from that poll.

### 5.2 Transition

| Prior | Current | Balloon (managed+enabled) |
|-------|---------|---------------------------|
| Up (or running) | Down / exited / missing | One “caiu” |
| Down / missing | Up | One “subiu” |
| Unchanged | Unchanged | None |

Subsequent polls while still Down do not repeat “caiu”.

### 5.3 Enable/disable interaction

- Item becomes disabled → stop notifying for it (and remove from managed exit set is exit-shutdown’s concern).
- Item newly enabled → next poll baselines silently for that item (no instant spam).

### 5.4 Ops-driven notifications

Atualizar / Backup completion may emit one ok/fail notification for managed+enabled items (SPEC §6.3). Those are event-driven, not per-poll spam.

### 5.5 Interval

Changing `pollSeconds` in JSON + Recarregar should apply on next schedule cycle (Should-level polish; default 8 must work).

---

## 6. Acceptance criteria

| ID | Pass if |
|----|---------|
| SN-AC-01 | Poll interval uses settings (default 8s) (FR-POLL-01). |
| SN-AC-02 | Forced Up→Down for managed+enabled yields exactly one “caiu”; further Down polls yield zero additional (AC-09). |
| SN-AC-03 | First poll after process start produces no transition balloons (FR-NOT-03). |
| SN-AC-04 | Disabled or unmanaged items never receive transition balloons (FR-NOT-01). |
| SN-AC-05 | Down→Up yields one “subiu” then silence while Up (RF09–10). |
| SN-AC-06 | Panel status updates from poll without requiring balloons (ui-ux visibility of system status). |

---

## 7. UX notes (from ui-ux.md)

| Topic | Guidance |
|-------|----------|
| Tray role | Icon = alive; balloons = ambient reaction to real transitions only |
| Copy | PT-BR short phrases (caiu / subiu / atualizou / backup); no English-only notify path |
| Emotion | Low anxiety — one notify per transition; no pulsing urgency for steady Down |
| Color | Status chips use text+color; Down uses attention amber, not panic red for steady state |
| Click ops | In-panel busy/success — do not dual-spam balloons for every Iniciar click unless it is a managed transition after baseline |

---

## 8. Technical how-to (from technologies.md)

| Topic | Decision |
|-------|----------|
| Type | `StatusWatcher` |
| Engine I/O | Via `DockerCli` / status from `ContainerService` — no second CLI stack |
| Notify | WinForms `NotifyIcon` balloon / show balloon tip — no toast NuGet |
| Load | Avoid parallel CLI stampede; sequential or modest batch per poll |
| Tests | Fake clock + fake status provider; assert baseline silence and single fire per transition |

---

## 9. Open questions

None blocking. Exact PT-BR balloon wording can match existing SPEC patterns (“caiu” / “subiu”) as long as AC-09 holds.
