---
purpose: "TECH-215 — MainMenuController UiButtonClick call sites."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-215 — MainMenuController UiButtonClick call sites

> **Issue:** [TECH-215](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Wire `BlipEngine.Play(BlipId.UiButtonClick)` at top of each `MainMenuController` click handler. First audible Blip call site. Satisfies Blip master plan Stage 3.2 Exit bullet 1 (UI click lane). Orchestrator: [`ia/projects/blip-master-plan.md`](blip-master-plan.md) Stage 3.2.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Click SFX fires on every main-menu button click handler.
2. Zero new fields, zero new singletons, zero `FindObjectOfType` on hot path — `BlipEngine` static facade self-caches (invariants #3, #4).

### 2.2 Non-Goals

1. Hover call sites — separate task (TECH-216).
2. In-game UI panels beyond MainMenu.
3. Volume-slider / Settings UI — post-MVP per `docs/blip-post-mvp-extensions.md` §4.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Click menu button, hear SFX | Every click handler fires `UiButtonClick` patch via catalog → player |
| 2 | Developer | Add click SFX w/o manager wiring | One-line `BlipEngine.Play` add per handler, no field refs |

## 4. Current State

### 4.1 Domain behavior

MainMenu silent today. `BlipEngine.Play(BlipId.UiButtonClick)` available (Stage 2.3 facade) + patch SO authored (Stage 3.1 TECH-209). No call sites exist.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/MainMenuController.cs` — 515 lines, handlers wired in `RegisterButtonListeners` / `Start` line ~133.
- `Assets/Scripts/Audio/Blip/BlipEngine.cs` — static facade; `Play(BlipId, float pitchMult = 1f, float gainMult = 1f)`.
- `BlipId.UiButtonClick` enum value (Stage 1.2 TECH-112).

Handlers to touch: `OnContinueClicked`, `OnNewGameClicked`, `OnLoadCityClicked`, `OnOptionsClicked`, `CloseLoadCityPanel`, `CloseOptionsPanel`.

## 5. Proposed Design

### 5.1 Target behavior

Click any MainMenu button → `BlipEngine.Play(BlipId.UiButtonClick)` fires → catalog resolves patch → cooldown gate → baker caches clip → player pool plays on `Blip-UI` mixer group.

### 5.2 Architecture

One-line insert as first statement in each click handler body. No new using directives beyond existing `Territory.Audio` (or equivalent `Blip` namespace). No field additions.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-15 | Per-handler explicit `Play` call | Explicit > listener interception; matches orchestrator Stage 3.2 Exit | Single Unity EventSystem hook (deferred — breaks per-button granularity) |

## 7. Implementation Plan

### Phase 1 — Click SFX wiring

- [ ] Add `BlipEngine.Play(BlipId.UiButtonClick)` first statement in each of `OnContinueClicked`, `OnNewGameClicked`, `OnLoadCityClicked`, `OnOptionsClicked`, `CloseLoadCityPanel`, `CloseOptionsPanel`.
- [ ] Confirm no new `using` beyond existing Blip namespace import.
- [ ] `npm run unity:compile-check` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Click handlers compile w/ `BlipEngine.Play` | Unity compile | `npm run unity:compile-check` | |
| Click SFX audible in Play Mode | Manual | Enter Play Mode; click each button | Out-of-band verification; no automated Play Mode test for this stage |
| IA validation | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] All 6 click handlers fire `BlipEngine.Play(BlipId.UiButtonClick)` as first statement.
- [ ] No new fields / singletons / per-frame `FindObjectOfType` introduced (invariants #3, #4).
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## Open Questions

1. None — wiring-only task; patch authored + facade shipped; game rule is trivially derived from orchestrator Stage 3.2.
