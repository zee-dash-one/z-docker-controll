# Spec: exit-shutdown

**Slug:** `exit-shutdown`  
**Title:** Sair confirms, stop managed+enabled, fail keeps app open  
**Priority:** Must  
**Depends on:** `catalog-store`, `docker-ops`, `tray-panel-ui`  
**Sources:** SPEC-ZLAB §6.4 · FR-UX-03–05 · RF12–13, RF16 · AC-08, AC-11–12 · AC-J02, AC-J05

---

## 1. Goal

Implement explicit **Sair**: confirm with stop-and-exit copy, stop every `managed && enabled` item, treat already absent/exited as success, **terminate the process only if every stop succeeds**; on any stop failure keep process and tray open with an error MessageBox. Closing the panel must not invoke this path.

---

## 2. In scope

| Capability | Behavior |
|------------|----------|
| Confirm | MessageBox / dialog with SPEC §7.3 Sair copy before any stop |
| Cancel confirm | No stops; no false success; app remains |
| Stop set | All items where `managed == true` **and** `enabled == true` |
| Absent/Exited | Count as success for that item |
| Failure | Show error; **do not** `Application.Exit` / kill process; tray stays |
| Success | Remove tray icon; end process |
| Distinction | Panel close / hide ≠ Sair |

## 3. Out of scope

- Autostart with Windows
- Stopping disabled or unmanaged items on Exit
- Stopping containers when merely closing the panel
- OS session-end hooks beyond explicit Sair (unless SE adds minimal cleanup later without changing AC-08)

---

## 4. Requirements traceability

| ID | Statement |
|----|-----------|
| FR-UX-03 / RF13 | Confirm Sair with managed-stop + “won’t close if stop fails” copy |
| FR-UX-04 / RF12 / AC-08 | Stop all managed+enabled; fail blocks exit |
| FR-UX-05 | Success → remove icon + end process |
| FR-UX-02 / AC-11 | Close panel ≠ exit |
| AC-12 | Sair requires confirm; Parar does not (Parar owned by docker-ops) |
| AC-J02 / AC-J05 | Cancel confirm no-op; failed stop keeps process + error |

---

## 5. Functional behavior (testable)

### 5.1 Happy path

1. Operator chooses Sair (tray or panel).
2. Confirm: “Parar os serviços gerenciados e sair do Z-Lab? Se algum não parar, o app não fecha.”
3. Accept → show exit-in-progress; disable new mutating ops.
4. Stop each managed+enabled (use item `stopTimeoutSeconds`).
5. All OK → remove NotifyIcon → process ends.

### 5.2 Stop failure

1. At least one stop returns failure.
2. MessageBox error (PT-BR); process still running; tray icon remains.
3. Operator can retry Sair or continue operating.

### 5.3 Cancel

1. Decline confirm → zero stops issued; UI unchanged.

### 5.4 Already stopped

- Engine reports exited/absent → treat as success for Exit accounting.

### 5.5 Panel close

- Hide form; tray remains; no stop-all.

---

## 6. Acceptance criteria

| ID | Pass if |
|----|---------|
| EX-AC-01 | Sair shows confirm with required copy themes (managed stop + won’t close on fail) (FR-UX-03). |
| EX-AC-02 | Confirm cancel performs no stops and does not exit (AC-J02). |
| EX-AC-03 | On confirm, every `managed && enabled` item is stopped (or already absent/exited) (RF12). |
| EX-AC-04 | Injected/forced stop failure → error shown, process + tray still alive (AC-08, AC-J05). |
| EX-AC-05 | All stops OK → process ends and tray icon is gone (FR-UX-05). |
| EX-AC-06 | Closing panel does not stop managed services or end process (AC-11). |
| EX-AC-07 | Disabled or unmanaged items are not required to stop for Exit success (filter contract). |

---

## 7. UX notes (from ui-ux.md)

| Topic | Guidance |
|-------|----------|
| Strongest safety juice | Failed Exit **blocks** close — MessageBox, process stays |
| Exit-in-progress | Exclusive with starting new mutating ops; show progress/errors |
| Do not | Stack second confirm on in-flight Exit |
| Fechar painel | No “exiting” chrome — must feel different from Sair |
| Emotion | User control + calm confidence; no celebratory exit animation |
| Tray menu | Sair entry uses same confirm + stop contract as panel |

---

## 8. Technical how-to (from technologies.md)

| Topic | Decision |
|-------|----------|
| Home | `TrayApp` Exit flow calling `ContainerService` stop for filtered items |
| UI | WinForms `MessageBox` for confirm and failure |
| Async | Stops awaitable; UI thread not blocked solid-frozen without cue |
| Filter | Read current catalog snapshot (`managed && enabled`) at confirm accept |
| Tests | Fake container stop results: all-ok exits; one-fail keeps “running” flag / does not call exit |

---

## 9. Open questions

None. Behavior is closed in SPEC §6.4 / §11.
