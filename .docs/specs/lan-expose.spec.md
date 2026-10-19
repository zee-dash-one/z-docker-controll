# Spec: lan-expose

**Slug:** `lan-expose`  
**Title:** LAN URLs on the server; no portproxy  
**Priority:** Must for the two open buttons; portproxy retired  
**Sources:** FR-LAN-01

## Goal

9Router opens `http://192.168.0.112:20128`. OmniRoute opens `http://192.168.0.112:20129`. MausBot has no LAN URL. The three `https://*.mandrill-court.ts.net` names are labels. The Windows Tailscale app is not installed.

## In scope

`Process.Start` on the two HTTP URLs when the card is up.

## Out of scope

`netsh interface portproxy`, firewall rules, WSL IP discovery. Those leftovers are removed only when the operator runs `scripts/wipe-wsl-docker.ps1`.

## Acceptance

- [ ] Abrir is hidden on MausBot and on the three Tailscale cards
- [ ] The exe does not call `LanExposeHelper`
