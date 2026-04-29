# Game UI Design System — Render Layer Extensions

> **Source type:** Extensions doc for existing `game-ui-design-system` master plan.
> **Companion to:** `docs/game-ui-design-system-exploration.md` (Approach G — Claude Design + IR JSON + Unity bridge bake).
> **Why this doc exists:** Stage 6 (HUD migration) shipped the data adapter wiring (CityStats / EconomyManager / TimeManager → baked StudioControl SO refs) but the HUD bar is invisible at runtime. Diagnosis: render layer never landed. Gap is systemic — Stages 7 (toolbar/overlay), 8 (modal/screen), 9 (onboarding/tooltips) all assume StudioControl prefabs render, but the prefabs carry only state-holder MonoBehaviours (`SegmentedReadout`, `IlluminatedButton`, `VUMeter`, `StudioControlBase`) with no `Image` / `CanvasRenderer` / `OnDrawGizmos`. The hud-bar prefab itself is parented under `Game Managers` Transform — outside any UI Canvas — so even if the children rendered, no rasterization would occur.

---

## Decision — extension scope

Insert one new Stage that lands the render layer + UI Canvas root + HUD visual closeout. New Stage numbered **Stage 10** (append-only per `master-plan-extend` boundary), but flagged in Notes as a **logical prerequisite to Stages 7–9** — execution order is 1→6→10→7→8→9, not literal stage numbering. User reorders execution via the lifecycle dispatcher, not via plan rewrite.

Why one Stage and not split:

- Per-kind renderer impls are cohesive (one C# file each, identical pattern: `Awake` cache `Image`/`CanvasRenderer`, subscribe to base state events, write tint/alpha/sprite per state change).
- Render layer is useless without canvas root reparent — atomic delivery unit.
- Visual smoke test ties everything together.

## Locked decisions

- **Render owner = per-kind renderer MonoBehaviour** sibling-attached to each StudioControl. State-holder + renderer split preserved (state stays pure, render reacts via events). No render code in `StudioControlBase` / subclasses.
- **Canvas root = scene-level `UI Canvas` GameObject** (Screen Space — Overlay), parent for hud-bar + future toolbar/modal roots. Distinct from `Game Managers` (logic) and `World` (camera-space content).
- **Bake-time injection** — when `BakePrefab` materializes a StudioControl prefab from IR JSON, it also attaches the matching `*Renderer` component + a child `RawImage` / `Image` GameObject as needed. No runtime `AddComponent` paths.
- **HUD silent channels closed** — `HudBarDataAdapter._populationReadout` + `_happinessNeedle` Inspector refs bound on hud-bar PrefabInstance in MainScene.
- Existing UI Polish bucket retired status preserved — render lives in `game-ui-design-system`, not in a separate bucket.

## Architecture — render component map

```
Assets/Scripts/UI/StudioControls/
  ├─ StudioControlBase.cs            (existing — state holder)
  ├─ SegmentedReadout.cs             (existing — Detail + DigitCount state)
  ├─ IlluminatedButton.cs            (existing — Pressed + Halo state)
  ├─ VUMeter.cs                      (existing — Detail state)
  └─ Renderers/                      (NEW)
      ├─ StudioControlRendererBase.cs    (NEW — abstract base; cache CanvasRenderer + Image; subscribe StudioControlBase.OnStateChanged)
      ├─ SegmentedReadoutRenderer.cs     (NEW — render Detail string into UnityEngine.UI.Text or TMP_Text)
      ├─ IlluminatedButtonRenderer.cs    (NEW — toggle Image color/alpha on Pressed; pulse halo on Halo)
      └─ VUMeterRenderer.cs              (NEW — render needle angle from Detail value via RectTransform.localRotation)

Assets/Scripts/UI/Bake/
  └─ BakePrefab.cs                   (existing — extend EmitStudioControl to also attach matching Renderer + child Image)

Assets/Scenes/MainScene.unity
  ├─ UI Canvas (NEW root GameObject)        (Canvas + CanvasScaler + GraphicRaycaster)
  │   └─ EventSystem (NEW sibling)
  └─ Game Managers
      └─ hud-bar (existing PrefabInstance — REPARENT under UI Canvas;
         bind _populationReadout + _happinessNeedle on HudBarDataAdapter)
```

## Subsystem Impact

- **UI render path** (new) — first per-kind renderer family for game-side uGUI; mirrors the Stage 4 state-holder split.
- **BakePrefab pipeline** — extends emitter with renderer attachment; touches `Assets/Scripts/UI/Bake/BakePrefab.cs` only.
- **Scene wiring** — adds `UI Canvas` + `EventSystem` to MainScene; hud-bar reparent + Inspector channel binds.
- **Invariants flagged:** #3 (cache `UiTheme` in Awake, no per-frame `FindObjectOfType`) — renderers must cache theme refs in `Awake`. #4 (Inspector first, `FindObjectOfType` fallback) — `HudBarDataAdapter` channel binds use Inspector slots.
- **Out of scope:** JuiceLayer animation curves (Stage 5 already shipped); StudioControl behavior changes; toolbar / modal render — Stages 7–8 inherit the pattern landed here.

## Implementation Points — staged skeleton

Single Stage; 6 tasks. Order: renderer base → per-kind renderers → bake injection → scene canvas root → hud-bar reparent + binds → visual smoke test.

1. **Renderer base + first kind (SegmentedReadout).** Author `StudioControlRendererBase.cs` (abstract; caches `CanvasRenderer` + child `Image` ref in `Awake`; subscribes `StudioControlBase.OnStateChanged`) + `SegmentedReadoutRenderer.cs` (writes `Detail` string into a child `TMP_Text` component). Establishes the per-kind renderer pattern.

2. **Remaining per-kind renderers.** Author `IlluminatedButtonRenderer.cs` (toggles tint/alpha on `Pressed`; emits halo pulse on `Halo`) + `VUMeterRenderer.cs` (rotates child needle `RectTransform` by `Detail` value mapped to angle range). Pattern matches T1.

3. **BakePrefab renderer injection.** Extend `BakePrefab.cs` `EmitStudioControl` (or equivalent) to attach matching `*Renderer` component + spawn child `Image` / `TMP_Text` GameObject when the IR kind requires render output. Re-bake all StudioControl prefabs under `Assets/UI/Prefabs/Generated/` via existing bake CLI.

4. **UI Canvas scene root.** Add `UI Canvas` (Screen Space — Overlay, `CanvasScaler` Scale-With-Screen-Size, reference resolution from `UiTheme`) + sibling `EventSystem` GameObject to `MainScene.unity` as scene-level root. Distinct from `Game Managers` / `World`.

5. **HUD reparent + channel close.** Reparent hud-bar PrefabInstance from `Game Managers` Transform under `UI Canvas`. Bind `HudBarDataAdapter._populationReadout` to the population `SegmentedReadout` ref + `_happinessNeedle` to the happiness `NeedleBallistics` (or `VUMeter` when needle-juice sibling absent) ref via Inspector.

6. **Visual PlayMode smoke test.** Add `HudBarVisualSmokeTest.cs` under `Assets/Scripts/Tests/UI/HUD/`. Enters PlayMode, asserts `Image.color.a > 0` on each rendered StudioControl child of hud-bar, asserts `TMP_Text.text` non-empty on city-name + population + money readouts, asserts needle `RectTransform.localRotation.eulerAngles.z` reflects happiness sample. Closes the visibility regression hole that Stage 6 parity test silently passed through.

## Relevant surfaces (for new Stage authoring)

- `Assets/Scripts/UI/StudioControls/*.cs` — existing state holders to wrap with renderers.
- `Assets/Scripts/UI/HUD/HudBarDataAdapter.cs` — channel binds land here (Inspector-side).
- `Assets/Scripts/UI/Bake/BakePrefab.cs` — bake-time injection extension point.
- `Assets/Scenes/MainScene.unity` — canvas root + reparent + Inspector binds.
- `Assets/Scripts/Tests/UI/HUD/HudBarParityTest.cs` — existing test passes silently when needle null; new smoke test runs alongside, NOT a replacement.
- `Assets/UI/Prefabs/Generated/hud-bar.prefab` — re-baked after T3.
- `Assets/Scripts/Managers/GameManagers/UiTheme.cs` — reference resolution + tint sources for renderers (cache in `Awake` per invariant #3).
- Stage 4 + Stage 5 + Stage 6 outputs — render layer composes on top.

## Scope boundary

- **Out:** Toolbar / overlay surface render (Stage 7 inherits pattern). Modal / screen render (Stage 8). Onboarding / tooltip render (Stage 9). Any catalog-side schema change. Any JuiceLayer change.
- **In:** Render layer for the 3 StudioControl kinds shipped in Stage 4 + the HUD bar visual closeout only.

## Locked decisions delta (for orchestrator header sync)

- Render owner = per-kind `*Renderer` sibling MonoBehaviour; state holder stays pure.
- UI Canvas root is scene-level (Screen Space — Overlay), distinct from `Game Managers` + `World`.
- Bake-time renderer injection — no runtime `AddComponent`.
