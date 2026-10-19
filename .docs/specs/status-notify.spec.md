# Spec: status-notify

**Slug:** `status-notify`  
**Title:** Poll plus transition balloons  
**Priority:** Must  
**Depends on:** `docker-ops`, `tray-panel-ui`  
**Sources:** FR-OBS-01–03

## Goal

On each `pollSeconds` tick, read host commands and `ps -a --format json` for the six services. Balloon only when a service moves between up and down/absent after a baseline sample.

## In scope

| Event | Balloon |
|-------|---------|
| First sample | none |
| Up → Down or Absent | “caiu” |
| Down or Absent → Up | “subiu” |
| Disconnect → Unknown | none |

`/data` exit non-zero displays `ausente`.

## Out of scope

HTTP/TCP health, Prometheus, stats on every tick (Uso is a button).

## Acceptance

- [ ] `StatusTransitionDetector` still suppresses the first sample
- [ ] Host text and six statuses refresh without rebuilding the cards
