# Spec: lan-expose

**Slug:** `lan-expose`  
**Title:** exposeLan helper (portproxy + firewall, Admin)  
**Priority:** Should (SPEC RF17) — still in V1 todo as verifiable chunk  
**Depends on:** `catalog-store`, `tray-panel-ui`  
**Sources:** SPEC-ZLAB RF17 · FR-LAN-01–02 · inventory §1.1 · OQ-03

---

## 1. Goal

For items with `exposeLan == true`, offer an operator helper that maps the item’s `hostPort` to the current WSL IP via Windows portproxy and ensures a named firewall rule (`firewallRuleName`), requesting Admin elevation instead of failing silently.

---

## 2. In scope

| Capability | Behavior |
|------------|----------|
| Offer action | Show **Expor LAN** only when `exposeLan` is true (and item enabled for ops) |
| Portproxy | `netsh interface portproxy` listen on host port → WSL IP:`container`/host mapping per inventory practice |
| Firewall | Named rule using `firewallRuleName` from item |
| Elevation | If Admin required → UAC / elevated helper; never silent no-op |
| Feedback | Success cue or explicit PT-BR failure/denial message |
| Re-apply note | WSL IP changes after `wsl --shutdown` / reboot — operator may need to re-run expose (document in notes/UI hint) |

## 3. Out of scope

- Third-party tunnels (ngrok etc.)
- Exposing Z-Lab itself as a network service
- Polished one-click scheduling (roadmap V2.1)
- Auto re-apply on every WSL IP change without operator action (nice-to-have, not Must)

---

## 4. Requirements traceability

| ID | Statement |
|----|-----------|
| FR-LAN-01 / RF17 | Helper when `exposeLan` |
| FR-LAN-02 | Admin elevation indication; no silent failure |
| Seed | `9router` / `omniroute` ship with `exposeLan: true` and rule names |
| Juice | Elevation prompt; success/fail PT-BR |

---

## 5. Functional behavior (testable)

### 5.1 Hide vs show

- `exposeLan == false` → no Expor control (AC-J04 pattern).
- `exposeLan == true` on enabled item → control available.

### 5.2 Apply

1. Resolve current WSL IP (e.g. `wsl hostname -I` / documented approach).
2. Configure portproxy for `hostPort` → WSL IP.
3. Ensure firewall rule `firewallRuleName` allows the port as designed.
4. On success → visible acknowledgement (panel).
5. On elevation denied or netsh fail → explicit PT-BR error; no fake success.

### 5.3 Privileges

Prefer launching elevated helper/script over pretending success when elevation is required and missing.

---

## 6. Acceptance criteria

| ID | Pass if |
|----|---------|
| LX-AC-01 | Enabled item with `exposeLan: true` shows Expor LAN; `false` hides it. |
| LX-AC-02 | Successful expose results in portproxy entry for the item’s `hostPort` toward current WSL IP (verifiable via `netsh interface portproxy show all`). |
| LX-AC-03 | Firewall rule name matches `firewallRuleName` when rule creation is part of the helper. |
| LX-AC-04 | Running without required elevation surfaces Admin/helper prompt or clear PT-BR denial — not silent no-op (FR-LAN-02). |
| LX-AC-05 | Failure path never reports success (juice / trust). |

---

## 7. UX notes (from ui-ux.md)

| Topic | Guidance |
|-------|----------|
| Placement | Riskier action on card — after observe actions, before Desativar |
| Seriousness | Admin prompt is appropriate; do not soft-fail |
| Copy | PT-BR success (“exposto na LAN” style) and failure; optional hint that reboot/WSL restart may require re-apply |
| Hide | Absence of button when `exposeLan` false — no error spam |
| Tone | Lab tool competence — LAN consumers never see Z-Lab UI |

---

## 8. Technical how-to (from technologies.md)

| Topic | Decision |
|-------|----------|
| Type | `LanExposeHelper` |
| Tools | `netsh` portproxy + firewall; Admin/script elevation |
| Distro | Align with inventory (`Ubuntu-24.04`); WSL IP is dynamic |
| Tests | Unit-test argument building / dry-run seams; live netsh may be manual on operator PC |
| Non-goal | No paid tunnel SaaS |

---

## 9. Open questions

| # | Question | Interim |
|---|----------|---------|
| OQ-03 | Minimum messaging when Admin denied (partial success)? | V1: treat as failure with explicit PT-BR message; no partial-success green state |
