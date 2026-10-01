# UI / UX — Look & Feel: Z-Lab (V1)

**Status:** Approved for implementation alignment  
**Authority:** [`docs/SPEC-ZLAB.md`](../docs/SPEC-ZLAB.md) v0.5 · [`.docs/requirements.md`](requirements.md) · [`.docs/technologies.md`](technologies.md)  
**Product:** Z-Lab (`ZLab.exe`) — Windows tray + panel  
**UI stack:** .NET 8 **WinForms** · operator language **PT-BR**  
**Brand asset:** [`assets/z-lab.ico`](../assets/z-lab.ico) / [`src/ZLab/Assets/z-lab.ico`](../src/ZLab/Assets/z-lab.ico) — **orange Z + gray 9**

> If this file and SPEC-ZLAB diverge, SPEC wins until both are updated together.  
> Do **not** invent conversion rates, “X% of users”, or study numbers. Emotion guidance below cites named design principles only.

---

## 1. Product tone & look-and-feel

Z-Lab is a **lab operator tool**: calm, dense, trustworthy, operational. Not arcade, not marketing landing, not a dashboard of decorative cards.

| Axis | Direction |
|------|-----------|
| Personality | Competent lab bench — clear verbs, honest status, no cheerleading |
| Density | Prefer readable status + actions over whitespace-heavy marketing layouts |
| Motion | Minimal; WinForms busy labels and control enable/disable — no particle FX, no custom animation framework in V1 |
| Sound | **Silent by default** — no sound pack; OS balloon / MessageBox only (see §5) |
| Brand signal | Product name **Z-Lab** in title bar + tray; custom `z-lab.ico` everywhere (never generic app icon) |

**WinForms constraint (technologies.md):** chrome is native WinForms controls (`MainForm`, `NotifyIcon`, `MessageBox`, `FolderBrowserDialog`). Do not plan WPF styles, WinUI toasts, Electron shells, or a separate “modern toast” system in V1.

---

## 2. Information hierarchy

Visual priority must match operator tasks: **see what’s running → act → inspect → manage catalog → exit safely**.

### 2.1 Surface order (panel)

Matches SPEC §7.2 layout:

```text
┌─ Z-Lab ─────────────────── Modo: docker | wsl docker ─────┐  ← L1 brand + engine mode
│ Pasta backup: … [Alterar]   Config: [Abrir] [Recarregar]  │  ← L2 global settings / tools
│ Serviços ativos                                            │  ← L3 section label
│ ┌─ card ─┐ ┌─ card ─┐ …   (flow/grid, N items)            │  ← L4 enabled service cards
│ Catálogo (desativados)                                     │  ← L5 quieter section
│ ○ item [Ativar] …  (badge Desativado)                      │  ← L6 catalog rows
│ Logs ──────────────────────────────────────────────────── │  ← L7 shared logs pane
└────────────────────────────────────────────────────────────┘
```

| Level | Element | Role |
|-------|---------|------|
| L1 | Title **Z-Lab** + Docker mode label | Orientation: “I’m in Z-Lab; which CLI path is live?” |
| L2 | Backup folder + Abrir pasta / Recarregar | Infrequent global ops — visible but not louder than service cards |
| L3–L4 | **Serviços ativos** + enabled cards | Primary workspace; full operational actions |
| L5–L6 | **Catálogo** + disabled items | Secondary; activate only; visually quieter than L4 |
| L7 | Logs | Detail surface; subordinate to card status |

**Tray (ambient layer):** icon = app alive; tooltip = product + brief status cue when known; context menu = per-enabled-item actions + optional quick Ativar + **Sair**. Double-click opens panel. Closing the panel returns to tray — **not** Exit.

### 2.2 Card vs catalog contrast

| Region | Look | Affordances |
|--------|------|-------------|
| Enabled card | Stronger border/background weight; status chip prominent; action buttons visible | Iniciar / Parar / Reiniciar / Status / Stats / Logs / Atualizar + conditional Abrir / Backup / Expor LAN / Desativar |
| Catalog row | Flatter, grayed or muted; badge **Desativado** | **Ativar** (+ notes); no operational ghosts that look clickable |

Empty active area: PT-BR empty-state pointing to Catálogo → Ativar (requirements juice addendum) — never a silent blank grid.

### 2.3 In-card hierarchy

1. **displayName** (primary)  
2. **Status** text/color (Up / Parado / Ausente / Iniciando…)  
3. Secondary meta (port, category) in quieter type  
4. Action row — primary verbs first (Iniciar/Parar), then observe (Logs/Stats), then riskier (Atualizar, Backup, Expor), then Desativar  

Busy exclusivity: while a mutating action runs on a card, disable conflicting mutating controls on **that** card; keep tray/panel chrome responsive (NFR-03).

---

## 3. Color palette

Derived from brand mark (**orange Z**, **gray 9**) and lab status semantics. Values are implementation targets for WinForms `Color` / ARGB — tune slightly for system theme contrast, keep **hue roles** stable.

### 3.1 Brand & chrome

| Token | Hex (approx.) | Use |
|-------|---------------|-----|
| `brand.orange` | `#E67A2E` | Icon Z; subtle accents (section underline, focus cue) — **not** full-window wash |
| `brand.gray9` | `#6B7280` | Icon “9”; secondary labels, catalog muted text |
| `chrome.bg` | `#F3F4F6` | Panel background (light lab bench) |
| `chrome.surface` | `#FFFFFF` | Enabled card face |
| `chrome.surfaceMuted` | `#E5E7EB` | Catalog rows / disabled surface |
| `chrome.border` | `#D1D5DB` | Card and section borders |
| `chrome.text` | `#111827` | Primary PT-BR copy |
| `chrome.textMuted` | `#6B7280` | Meta, notes, placeholders |

Avoid purple-on-white marketing gradients, glow effects, and dark-mode-first skins in V1 unless the operator OS high-contrast theme forces system colors.

### 3.2 Status semantics

| Token | Hex (approx.) | Meaning |
|-------|---------------|---------|
| `status.up` | `#15803D` | Container Up / healthy engine state |
| `status.down` | `#B45309` | Stopped / Exited / “caiu” (attention, not panic) |
| `status.absent` | `#6B7280` | Not created yet — Iniciar is the path |
| `status.busy` | `#C2410C` (or brand orange) | Iniciando / Parando / Atualizando / Backup… |
| `status.error` | `#B91C1C` | Failed op; Exit-stop failure |
| `status.info` | `#1D4ED8` | Neutral info (mode label, Recarregar ok) |

**Badge Desativado:** muted fill (`chrome.surfaceMuted`) + `brand.gray9` text — readable, never looks like an error.

### 3.3 Contrast & accessibility (qualitative)

- Status meaning must never rely on color alone: always pair with PT-BR text (`Up`, `Parado`, `Desativado`, `Iniciando…`).  
- Prefer system fonts already legible in WinForms (e.g. Segoe UI) at sizes that keep action buttons easily hit-targetable.  
- Error and confirm dialogs use standard `MessageBox` iconography so severity is recognizable without custom art.

---

## 4. Typography & iconography

| Role | Guidance |
|------|----------|
| Window / tray title | **Z-Lab** exactly (not RouterCtrl / Z-ROUTER) |
| Section headers | Slightly stronger weight: “Serviços ativos”, “Catálogo (desativados)”, “Logs” |
| Card title | `displayName` from catalog |
| Body / buttons | Operational PT-BR verbs per SPEC §7.3 and requirements FR-UX-06 |
| Logs | Monospace-friendly control (`TextBox`/`RichTextBox` with Consolas or equivalent) for log lines |

**Icon:** single product icon `z-lab.ico` for exe, form, and tray. Do not mix legacy icons.

**Microcopy seeds (SPEC §7.3)** — keep tone consistent elsewhere:

| Situation | Text |
|-----------|------|
| Desativado | “Desativado — Ative para controlar este serviço.” |
| Sair | “Parar os serviços gerenciados e sair do Z-Lab? Se algum não parar, o app não fecha.” |
| Ativar | “Serviço ativado. Use Iniciar para criar/subir o container se ainda não existir.” |

Also required: backup **secrets warning** before/during backup (FR-BKP-06 / RNF05).

---

## 5. Sensory behavior (visual · sound · haptic)

V1 sensory budget is intentionally small and OS-native.

### 5.1 Visual

| Channel | Behavior |
|---------|----------|
| Status paint | Poll-driven color + text on cards; first poll after start is **silent baseline** (no balloon flood) |
| Busy | Per-item label (“Iniciando…”, “Parando…”, …) + disabled conflicting actions |
| Success | Brief in-panel acknowledgement (card/status line or toolbar) — not a second balloon for click-driven ops |
| Failure | Inline/card PT-BR error; Exit failure → **MessageBox** and process stays |
| Tray balloon | **Transition-only** for `managed && enabled`: caiu / subiu / atualizou / backup result |
| Conditional actions | Hide Abrir / Backup / Expor when not applicable — absence is the signal (no error spam) |

### 5.2 Sound

| Rule | Detail |
|------|--------|
| Default | **No** app-authored sounds, jingles, or alert WAVs |
| OS may beep | Only if the operator’s Windows MessageBox / system settings do so — do not add extra `SystemSounds` calls for routine ops |
| Rationale | Lab operators often work near focused terminals; ambient balloons already cover background change |

### 5.3 Haptic / other

None. Desktop WinForms tray app — no vibration API targets in V1.

### 5.4 Explicit non-goals (sensory)

Per requirements juice addendum and technologies.md: no particle effects, no sound packs, no NuGet toast framework, no decorative motion that implies a game client.

---

## 6. Evidence-based emotion strategies

Guidance below uses **named HCI / design principles**. No fabricated percentages or study statistics.

### 6.1 Trust over delight

| Principle | Application in Z-Lab |
|-----------|----------------------|
| **Norman’s behavioral level** (usefulness through feedback) | Every meaningful click gets a visible reaction within one interaction beat (busy → complete/fail); juice is status honesty, not ornament |
| **Visibility of system status** (Nielsen) | Poll + card status + Docker mode label; never leave a long Docker op looking frozen |
| **Match between system and real world** | PT-BR operational verbs (Iniciar/Parar/Ativar), not raw docker jargon as primary labels |
| **Error prevention** | Confirm **Atualizar** and **Sair**; **Parar** without confirm (reversible start); never offer prune / volume delete |
| **User control** | Fechar painel ≠ Sair; failed Exit **blocks** process end so the operator keeps control |

Emotional target: **calm confidence** (“I know what the lab is doing”) and **safety under Exit**, not excitement.

### 6.2 Affect by state

| Operator feeling to support | Design move |
|-----------------------------|-------------|
| Orientation | Brand icon + Z-Lab title always; enabled vs catalog clearly split |
| Competence | Primary actions on enabled cards; catalog only Ativar |
| Low anxiety on background change | Transition-only tray notify; silent first poll |
| Respect for attention | No sound pack; balloons reserved for real transitions |
| Seriousness for risk | Secrets warning on backup; Admin prompt for LAN expose — never silent no-op |
| Relief on cancel | Declined Atualizar/Sair = intentional no-op, no false success |

### 6.3 What not to do (emotion anti-patterns)

- Celebratory copy or gamification for routine start/stop  
- Invented urgency (red everything, pulsing tray) for steady Down state — one “caiu” transition is enough  
- English-only paths that break operator trust in a PT-BR product  
- Visual noise that competes with status (pill clusters, stat strips unrelated to docker stats)

---

## 7. Component mapping (WinForms)

| UX piece | Likely type / home | Notes |
|----------|--------------------|-------|
| Tray | `NotifyIcon` in `TrayApp` | Icon `z-lab.ico`; balloons for transitions |
| Panel | `MainForm` | Layout §2.1; flow/grid for N cards |
| Enabled card | `Panel`/`GroupBox`/`UserControl` | Status + actions; busy disable |
| Catalog row | Lighter `Panel` or list row | Badge Desativado + Ativar |
| Logs | Multiline text control | Empty-state PT-BR copy |
| Confirms / Exit fail | `MessageBox` | SPEC copy; block exit on stop fail |
| Backup folder | `FolderBrowserDialog` | When `backupFolder` empty |
| Engine mode | Status label in header | `docker` \| `wsl docker` |

Async: all Docker/file work off UI thread; show progress **or** disabled busy control (technologies §9.1 / NFR-03).

---

## 8. Acceptance cues for look-and-feel

QA can spot-check without new product features:

| ID | Cue |
|----|-----|
| LF-01 | Title/tray/exe show **Z-Lab** + `z-lab.ico` (orange Z + gray 9), not legacy names/icons |
| LF-02 | Enabled cards visually stronger than catalog; Desativado badge present on disabled |
| LF-03 | Status uses text + color; catalog never looks like the primary ops area |
| LF-04 | No custom sound on start/stop/notify beyond OS defaults |
| LF-05 | Busy/error/empty/hidden-action behaviors match requirements juice map (AC-J01–J06) |
| LF-06 | UI strings on primary paths are PT-BR |
| LF-07 | Layout scales as flow/grid for N items — not a hard-coded two-column product model |

---

## 9. Traceability

| Topic | Source |
|-------|--------|
| Layout wireframe, microcopy | SPEC-ZLAB §7 |
| Tray / Exit / enabled vs catalog | requirements FR-UX-*, FR-CAT-*, juice addendum |
| WinForms, NotifyIcon, no toast NuGet | technologies.md §1, §7 |
| Brand icon paths | SPEC §1 / technologies folder layout |
| Action→reaction juice | requirements “UX / Juice addendum” |

---

*End of UI/UX look-and-feel artefact. Interaction juice details live in requirements; this file owns visual identity, hierarchy, palette, and sensory/emotion direction for V1.*
