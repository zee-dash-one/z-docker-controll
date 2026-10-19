# Spec: backup-data

**Slug:** `backup-data`  
**Title:** Retired from the exe — technical debt  
**Priority:** Not in 2.0  
**Sources:** [tech-debt.md](../tech-debt.md)

## Goal

Click backup and the daily job are not part of the z-server-01 monitor. Volume copy from WSL Docker is not invoked by `Program`.

## In scope

Nothing in the running app.

## Out of scope (debt)

Copying `9router-data`, `omniroute-data`, and `mausbot-data` off the server into a folder the operator picks. The old `BackupService` types remain in the tree until that debt is either rebuilt against the server or deleted.

## Acceptance

- [ ] The panel has no backup button and no daily scheduler
- [ ] Exit does not run a backup
