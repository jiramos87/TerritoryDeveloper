---
purpose: "TECH-216 — MainMenuController UiButtonHover call sites."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-216 — MainMenuController UiButtonHover call sites

> **Issue:** [TECH-216](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Wire `BlipEngine.Play(BlipId.UiButtonHover)` on `PointerEnter` for each MainMenu button. Uses Unity `EventTrigger` component added programmatically next to existing `onClick.AddListener` registration. Satisfies Blip master plan Stage 3.2 Exit bullet 1 (UI hover lane). Orchestrator: [`ia/projects/blip-master-plan.md`](blip-master-plan.md) Stage 3.2.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Hover SFX fires on each MainMenu button PointerEnter.
2. Programmatic wiring — no prefab / Inspector edits; keeps diff code-only.
3. No new fields (invariant #4), no hot-path lookups (invariant #3).

### 2.2 Non-Goals

1. In-game HUD button hovers — deferred.
2. Keyboard focus-gained SFX — deferred post-MVP.
3. Click SFX — covered by TECH-215.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Hover menu button, hear SFX | Each button PointerEnter fires `UiButtonHover` patch |
| 2 | Developer | Wire hover next to onClick block | Single helper `AddHoverBlip(button)` or inline loop in `RegisterButtonListeners` |

## 4. Current State

### 4.1 Domain behavior

No hover SFX anywhere in game. `EventTrigger` not currently on MainMenu buttons.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/MainMenuController.cs` — `RegisterButtonListeners` / `Start` line ~133 where `onClick.AddListener` calls live.
- `UnityEngine.EventSystems.EventTrigger` — `GetOrAddComponent`, add `Entry { eventID = PointerEnter, callback += BlipEngine.Play(BlipId.UiButtonHover) }`.
- Button fields: `continueButton`, `newGameButton`, `loadCityButton`, `optionsButton`, `loadCityBackButton`, `optionsBackButton`.

## 5. Proposed Design

### 5.1 Target behavior

Move mouse over any MainMenu button → single `UiButtonHover` blip. 30 ms (or patch-authored) cooldown gates rapid re-enter glitch.

### 5.2 Architecture

Private helper `void AddHoverBlip(Button btn)` — null-guard, `var trig = btn.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();` + new `EventTrigger.Entry { eventID = EventTriggerType.PointerEnter }` w/ callback lambda calling `BlipEngine.Play(BlipId.UiButtonHover)` + `trig.triggers.Add(entry)`. Called once per button field in `RegisterButtonListeners` alongside `onClick.AddListener`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-15 | Programmatic `EventTrigger` add | Keeps diff code-only; no prefab churn | Inspector-authored EventTrigger (rejected — harder to review + regen) |

## 7. Implementation Plan

### Phase 1 — Hover helper + per-button wiring

- [ ] Add `AddHoverBlip(Button)` private helper to `MainMenuController.cs` using `EventTrigger` + `PointerEnter` + `BlipEngine.Play`.
- [ ] Call helper for each of 6 button fields in `RegisterButtonListeners` / `Start`.
- [ ] Null-guard each button ref (handle scenes w/ disabled buttons).
- [ ] `npm run unity:compile-check` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Hover wiring compiles | Unity compile | `npm run unity:compile-check` | |
| Hover audible in Play Mode | Manual | Play Mode; move mouse over each button | |
| IA validation | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `AddHoverBlip(Button)` helper added to `MainMenuController.cs`.
- [ ] All 6 button fields receive hover wiring.
- [ ] No new fields; no `FindObjectOfType` on hover path (invariants #3, #4).
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## Open Questions

1. None — wiring-only; patch + facade shipped upstream.
