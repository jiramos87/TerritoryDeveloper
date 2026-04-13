---
purpose: "TECH-101 — Scene-load suppression policy doc + glossary rows (Blip mixer group, Blip bootstrap)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-101 — Scene-load suppression policy doc + glossary rows (Blip mixer group, Blip bootstrap)

> **Issue:** [TECH-101](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Document the scene-load suppression policy: no Blip SFX fires until `BlipCatalog.Awake` completes + sets ready flag. Prevents boot-race clicks during `MainMenu → Game.unity` transition. Land **Blip mixer group** + **Blip bootstrap** glossary rows citing `blip-master-plan.md` Stage 1.1 + the suppression rule. Satisfies Stage 1.1 final Exit bullet ("Glossary rows land").

## 2. Goals and Non-Goals

### 2.1 Goals

1. Glossary row for **Blip mixer group** — three groups, `SfxVolume` exposed param, cited section.
2. Glossary row for **Blip bootstrap** — persistent prefab, `DontDestroyOnLoad`, scene-load suppression policy summary, cited section.
3. Policy comment block in `BlipBootstrap.cs` (or future `BlipCatalog.cs` skeleton comment — since catalog lands Step 2, MVP Step 1 keeps the note on `BlipBootstrap`) stating: "Blip remains silent until `BlipCatalog` sets ready flag (lands Step 2). Until then, any `BlipEngine.Play` call returns early."
4. Cross-reference from orchestrator (`blip-master-plan.md` Stage 1.1) remains valid — no edits required other than glossary link.

### 2.2 Non-Goals (Out of Scope)

1. No runtime ready-flag implementation — that lives on `BlipCatalog` (Step 2).
2. No spec change to `ia/specs/` — MVP doc lives in project spec + glossary only.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer loading glossary terms via MCP I want **Blip mixer group** + **Blip bootstrap** defined so I can discover them w/o reading full orchestrator. | `mcp__territory-ia__glossary_lookup "Blip bootstrap"` returns row w/ spec cite. |

## 4. Current State

### 4.1 Domain behavior

Glossary today has zero Blip rows (verified via `glossary_discover` at stage kickoff).

### 4.2 Systems map

- Edit: `ia/specs/glossary.md` — two new rows under an Audio category (create category if absent).
- Edit: `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` — suppression policy comment (alongside TECH-99 / TECH-100 body).

## 5. Proposed Design

### 5.1 Target behavior (product)

Glossary surfaces Blip subsystem vocabulary. No runtime change.

### 5.2 Architecture / implementation

Two rows, following glossary conventions:

- **Blip mixer group** — one of three routing groups (`Blip-UI`, `Blip-World`, `Blip-Ambient`) on `BlipMixer.mixer`. Master exposes `SfxVolume` dB param. Cite: `blip-master-plan.md` Stage 1.1.
- **Blip bootstrap** — persistent GameObject at `MainMenu.unity` root; `DontDestroyOnLoad`. Hosts Catalog / Player / MixerRouter / Cooldown child slots. Suppression policy: no `BlipEngine.Play` fires until `BlipCatalog` sets ready flag (Step 2). Cite: `blip-master-plan.md` Stage 1.1.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-13 | Glossary + code comment only (no reference-spec body) | MVP scope; `ia/specs/` = permanent (invariant #12) | New `ia/specs/blip.md` (rejected — premature; lands when subsystem graduates) |

## 7. Implementation Plan

### Phase 1 — Glossary + comment

- [ ] Add glossary rows (match canonical table format + alphabetization).
- [ ] Add suppression-policy comment block to `BlipBootstrap.cs`.
- [ ] `npm run validate:all` green (glossary row schema + dead-link checks).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Glossary schema | Node | `npm run validate:all` | Covers glossary table parse |
| Lookup surfaces | MCP | `glossary_lookup "Blip bootstrap"` returns row | Manual post-commit check |

## 8. Acceptance Criteria

- [ ] Two glossary rows committed (**Blip mixer group**, **Blip bootstrap**).
- [ ] Suppression-policy comment lives in `BlipBootstrap.cs`.
- [ ] `npm run validate:all` green.

## 10. Lessons Learned

- …

## Open Questions

None — documentation + glossary only.
