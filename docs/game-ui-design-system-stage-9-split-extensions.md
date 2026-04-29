# Game UI Design System — Stage 9 Split Extensions

> **Source type:** Extensions doc for existing `game-ui-design-system` master plan.
> **Companion to:** `docs/game-ui-design-system-exploration.md` + `docs/game-ui-design-system-render-layer-extensions.md`.
> **Why this doc exists:** Original Stage 9 ("Onboarding + glossary + tooltips + splash + city stats handoff close") fired sizing-gate WARN (4 Path B tasks + T9.2 fan-out 3). User chose split into half-A + half-B for cleaner verify clusters + two player-visible checkpoints. Stage 9 now narrowed to half-A (Tooltips IR + render layer wiring, 3 tasks). Half-B (surface adapters + legacy decommission + full-flow smoke) appended here as **Stage 11**.

---

## Decision — extension scope

Append one new Stage (Stage 11) covering the half-B surface adapters + legacy decommission + full-flow PlayMode smoke. New Stage 11 numbered append-only per `master-plan-extend` boundary; logical execution order = 6→10→7→8→9→11 (numbering reflects authoring order; Stage 11 always runs after Stage 9).

Why split now (recap):

- Stage 9 half-A (T9.1–T9.3) already lands the IR + per-kind renderers + tooltip controller + scene root. Surface adapters depend on those primitives existing first.
- Half-B = 3 cohesive adapter tasks + 1 decommission + smoke close. Same shape as Stage 8 adapter cluster (Stage 6 `HudBarDataAdapter` precedent).
- Player-visible checkpoint at end of Stage 9: tooltips work on hover. Player-visible checkpoint at end of Stage 11: splash + onboarding + glossary + city-stats handoff all live; legacy `UIManager.CityStats.cs` partial decommissioned; MVP UI complete.

## Locked decisions

- Adapter pattern = per-surface `*DataAdapter.cs` MonoBehaviour bridging sim producers ↔ baked StudioControl SO refs (Stage 6 `HudBarDataAdapter` precedent reused for SplashAdapter / OnboardingAdapter / GlossaryPanelAdapter / CityStatsHandoffAdapter).
- Renderers + IR + tooltip controller already shipped by Stage 9 — Stage 11 consumes them, does not re-author.
- Legacy decommission target: `UIManager.CityStats.cs` partial (city stats deep-dive panel) parallels Stage 6 `UIManager.Hud.cs` decommission. Lessons-learned migrate to canonical docs before delete (per IF→THEN guardrail in `invariants.md`).
- Full-flow smoke = single PlayMode test exercising splash → onboarding → tooltip hover → glossary open → city-stats handoff → close, asserting baked surfaces render visible (alpha > 0, text non-empty) at each step.

## Architecture — half-B component map

```
Assets/Scripts/UI/Splash/
  └─ SplashAdapter.cs                      (NEW — MainMenuController state ↔ baked splash SOs)

Assets/Scripts/UI/Onboarding/
  └─ OnboardingAdapter.cs                  (NEW — first-launch flow state ↔ baked onboarding-overlay SOs;
                                            persists "onboarding-complete" PlayerPrefs flag)

Assets/Scripts/UI/Glossary/
  └─ GlossaryPanelAdapter.cs               (NEW — glossary term registry ↔ baked glossary-panel SOs;
                                            ThemedList slot population from term registry)

Assets/Scripts/UI/CityStats/
  └─ CityStatsHandoffAdapter.cs            (NEW — CityStats producer ↔ baked city-stats-handoff panel SOs)

Assets/Scripts/Managers/GameManagers/
  └─ UIManager.CityStats.cs                (LEGACY — decommission; surface migrates to baked panel +
                                            CityStatsHandoffAdapter per Stage 6 HUD-bar precedent)

Assets/Scripts/Tests/UI/
  └─ FullFlowSmokeTest.cs                  (NEW — PlayMode test: splash → onboarding → tooltip hover →
                                            glossary open → city-stats handoff close; asserts each
                                            surface renders visible at each step)

Assets/Scenes/MainScene.unity
  └─ UI Canvas (Stage 10 root)
      ├─ splash (PrefabInstance — NEW; bind SplashAdapter Inspector slots)
      ├─ onboarding-overlay (PrefabInstance — NEW; bind OnboardingAdapter slots)
      ├─ glossary-panel (PrefabInstance — NEW; bind GlossaryPanelAdapter slots)
      └─ city-stats-handoff (PrefabInstance — NEW; bind CityStatsHandoffAdapter slots;
                              REPLACES procedural city-stats panel from UIManager.CityStats.cs)
```

## Subsystem Impact

- **UI surface adapters** — extends Stage 6 / 8 pattern to splash / onboarding / glossary / city-stats handoff.
- **Legacy decommission** — `UIManager.CityStats.cs` partial removed; lessons-learned (panel layout, data binding choices) migrated to glossary or to `docs/agent-lifecycle.md` if process-relevant.
- **MainScene wiring** — 4 new PrefabInstances under `UI Canvas` root + Inspector slot binds for each adapter.
- **Test infra** — `FullFlowSmokeTest.cs` exercises end-to-end UX path; runs as PlayMode test in `verify:local`.
- **Invariants flagged:** #3 (cache `UiTheme` in `Awake`), #4 (Inspector first, fallback).
- **Out of scope:** any new IR / renderer / tooltip-controller logic (Stage 9 ships those); JuiceLayer changes; catalog-side schema changes.

## Implementation Points — staged skeleton

Single Stage; 3 tasks. Order: surface adapters → legacy decommission → full-flow smoke + close.

1. **Splash + onboarding + glossary adapters + scene wiring.** Author `SplashAdapter.cs` + `OnboardingAdapter.cs` + `GlossaryPanelAdapter.cs` under their respective `Assets/Scripts/UI/{Splash,Onboarding,Glossary}/` directories. Each subscribes the relevant producer (MainMenuController for splash; first-launch flow + PlayerPrefs for onboarding; glossary term registry for ThemedList slot population) and writes to baked StudioControl/Themed* SO refs via Inspector slots. Reparent the 3 PrefabInstances under `MainScene.unity` `UI Canvas`. Bind Inspector slots.

2. **City stats handoff adapter + legacy decommission.** Author `CityStatsHandoffAdapter.cs` under `Assets/Scripts/UI/CityStats/`. Subscribes `CityStats` producer; writes to baked `city-stats-handoff` panel SOs. Reparent PrefabInstance under `UI Canvas`. Migrate lessons-learned from `UIManager.CityStats.cs` to glossary / canonical docs as applicable (per IF→THEN guardrail). Delete `UIManager.CityStats.cs` partial. Update any references in `UIManager.cs` partial root + lifecycle.

3. **Full-flow PlayMode smoke + Stage close.** Author `FullFlowSmokeTest.cs` under `Assets/Scripts/Tests/UI/`. Test enters PlayMode, drives splash → onboarding (consume + dismiss) → tooltip hover on a labeled UI element → glossary panel open + scroll → city-stats handoff panel open. Asserts at each step: relevant surface's `Image.color.a > 0`, `TMP_Text.text` non-empty, no console errors, no missing Inspector references. Run via `unity:testmode-batch` scenario.

## Relevant surfaces (for Stage 11 authoring)

- `Assets/Scripts/UI/HUD/HudBarDataAdapter.cs` — Stage 6 adapter precedent (canonical pattern source).
- `Assets/Scripts/Managers/GameManagers/UIManager.CityStats.cs` — legacy decommission target.
- `Assets/Scripts/Managers/GameManagers/UIManager.cs` — partial root; references to `UIManager.CityStats.cs` removed.
- `Assets/Scripts/Managers/GameManagers/MainMenuController.cs` — splash producer (or equivalent main-menu state surface).
- `Assets/Scripts/Managers/GameManagers/CityStats.cs` — city stats producer.
- `Assets/Scenes/MainScene.unity` — 4 new PrefabInstance reparent + Inspector slot binds.
- Stage 9 (half-A) outputs — IR + renderers + tooltip controller already shipped.
- Stage 10 outputs — `UI Canvas` scene root + `StudioControlRendererBase` family.
- `Assets/UI/Prefabs/Generated/{splash,onboarding-overlay,glossary-panel,city-stats-handoff}.prefab` — baked from Stage 9 T9.1.

## Scope boundary

- **Out:** Any new IR row / renderer / tooltip-controller logic (Stage 9 owns); JuiceLayer changes; catalog schema changes; any UI surface beyond the 4 adapters listed.
- **In:** 4 surface adapters (splash, onboarding, glossary, city-stats handoff), legacy `UIManager.CityStats.cs` decommission, full-flow PlayMode smoke.

## Locked decisions delta (for orchestrator header sync)

- Half-B surface adapters use Stage 6 `HudBarDataAdapter` pattern (one `*DataAdapter.cs` per baked surface; Inspector slot binds; no runtime `AddComponent`).
- `UIManager.CityStats.cs` partial decommissioned in this Stage; pattern parallels Stage 6 `UIManager.Hud.cs` decommission.
- Full-flow PlayMode smoke (`FullFlowSmokeTest.cs`) is the MVP UI close gate — exercises splash → onboarding → tooltip → glossary → city-stats handoff in one PlayMode run.
