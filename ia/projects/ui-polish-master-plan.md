# UI Polish — Master Plan (Flagship polish for Main HUD + Toolbar + Overlays)

> **Status:** In Progress — Step 1 / Stage 1.1
>
> **Scope:** Bucket 6 of polished-ambitious MVP. Three concentric rings (token / primitive / juice) + flagship studio-rack polish on Main HUD + Toolbar + overlay toggles + CityStats handoff. Info panels / settings / save-load / pause / onboarding / glossary / tooltips stay functional-tier (consume primitives only; no juice). Web dashboard OUT. Accessibility / localisation / gamepad / touch OUT (bucket-level hard-deferral).
>
> **Exploration source:** `docs/ui-polish-exploration.md` (§Design Expansion — Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Review Notes).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach E — Hybrid UiTheme-first + Shell/data separation + Shared Juice Layer.
> - Flagship studio-rack treatment on Main HUD + Toolbar + Overlays ONLY. Other screens consume primitives, no juice.
> - CityStats dashboard layout / charts / data wiring owned by CityStats bucket (`ia/projects/citystats-overhaul-master-plan.md`). This plan owns primitives + tokens + juice helpers only.
> - `UIManager` accretion forbidden — touch only via new `UIManager.ThemeBroadcast.cs` partial.
> - Invariant #3 hard (no per-frame `FindObjectOfType`); BUG-14 resolves in Step 5 HUD migration.
> - Invariant #4 hard (no new singletons) — `JuiceLayer` + `ThemeBroadcaster` are scene MonoBehaviours, Inspector-wired, `FindObjectOfType` fallback in `Awake` only.
> - Bucket 7 SFX wiring exposed via optional `ISfxEmitter` interface — NOT wired in this bucket.
> - `OnValidate` repaint broadcast guarded by `#if UNITY_EDITOR`.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `docs/ui-polish-exploration.md` — full design + architecture + 4 examples. §Design Expansion is ground truth.
> - `ia/specs/ui-design-system.md` — existing UiTheme spec. This plan extends §1 (tokens) + §1.5 (motion) + §2 (components) normatively.
> - `ia/projects/citystats-overhaul-master-plan.md` — downstream consumer of token + primitive + juice handoff (FEAT-51).
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase, ≤6 soft).
> - `ia/rules/invariants.md` — #3 (no per-frame `FindObjectOfType`), #4 (no new singletons), #6 (no `UIManager` bloat).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

### Stage 1 — Token ring extension / Token schema extension + default asset defaults

**Status:** In Progress (TECH-309, TECH-310, TECH-311, TECH-312, TECH-313 filed)

**Objectives:** Extend `UiTheme.cs` fields + update `DefaultUiTheme.asset` defaults. Spec + glossary land in same stage so terminology + asset ship together.

**Exit:**

- `UiTheme.cs` carries new `[Serializable] StudioRackBlock` + `MotionBlock` nested classes with named fields per exploration §Design Expansion.
- `DefaultUiTheme.asset` Inspector values populated for every new field (dark-first + studio-rack palette).
- `ui-design-system.md` §1 + §1.5 sections extended; normative token names match field names.
- Glossary rows added: `UiTheme token ring`, `Studio-rack token`, `Motion token`.
- Phase 1 — Schema field expansion + asset defaults.
- Phase 2 — Spec + glossary updates.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | StudioRackBlock schema | **TECH-309** | Draft | Add `[Serializable] class StudioRackBlock` to `UiTheme.cs` with fields: `ledHues` (`Color[]`), `vuGradientStops` (`Gradient`), `knobDetentColor` (`Color`), `faderTrackGradient` (`Gradient`), `oscilloscopeTrace` (`Color`), `oscilloscopeGlowColor` (`Color`), `shadowDepthStops` (`float[3]`), `glowRadius` (`float`), `glowColor` (`Color`), `sparklePalette` (`Color[]`). Expose via `public StudioRackBlock studioRack` on `UiTheme`. Serializable defaults inert. |
| T1.2 | MotionBlock schema + curves | **TECH-310** | Draft | Add `[Serializable] class MotionBlock` to `UiTheme.cs` with semantic fields: `moneyTick` (duration + curve), `alertPulse`, `needleAttack`, `needleRelease`, `sparkleDuration`, `panelElevate`. Each a `[Serializable] struct MotionEntry { float durationSeconds; AnimationCurve easing; }`. Expose via `public MotionBlock motion` on `UiTheme`. |
| T1.3 | DefaultUiTheme.asset defaults | **TECH-311** | Draft | Populate `Assets/UI/Theme/DefaultUiTheme.asset` Inspector values for every new `studioRack` + `motion` field per `ui-design-system §7.1` dark-first palette. LED hues: green/amber/red triad. VU gradient: green → amber → red. Motion defaults: `moneyTick.durationSeconds = 0.28f`; `needleAttack = 0.08f`; `needleRelease = 0.40f`. |
| T1.4 | ui-design-system spec §1 + §1.5 | **TECH-312** | Draft | Extend `ia/specs/ui-design-system.md` §1 (palette / spacing) with studio-rack token names + role. Extend §1.5 (motion) with motion token catalog. Every field in `StudioRackBlock` + `MotionBlock` cited normatively. Link from §2 to anchor primitives-to-tokens mapping. |
| T1.5 | Glossary rows | **TECH-313** | Draft | Add rows to `ia/specs/glossary.md`: `UiTheme token ring` (defn: extended token catalog under `UiTheme` SO covering surface / accent / studio-rack / motion blocks), `Studio-rack token` (LED / VU / knob / fader / oscilloscope visual params), `Motion token` (semantic named duration + easing curve entry under `UiTheme.motion`). |

### Stage 2 — Token ring extension / OnValidate repaint broadcast + tests

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Wire `OnValidate` broadcast in `UiTheme.cs` (`#if UNITY_EDITOR` gated). Provide a minimal EditMode test fixture that confirms token edits fire broadcast in Editor. No runtime subscribers yet (primitives land in Step 2) — this stage proves the broadcast hook exists + is inert in Player builds.

**Exit:**

- `UiTheme.OnValidate` calls `ThemeBroadcaster.BroadcastAll()` **only** under `#if UNITY_EDITOR` guard (never compiled into Player).
- Stub `ThemeBroadcaster` MonoBehaviour in scene with `BroadcastAll()` method that discovers `IThemed` via `FindObjectsOfType<MonoBehaviour>()` — `Awake`-cached scan list, not per-frame.
- EditMode test confirms `OnValidate` calls broadcaster exactly once per edit; never under Play mode hot loop.
- `#if !UNITY_EDITOR` compile path clean — player build ships without broadcaster references from `OnValidate`.
- Phase 1 — Broadcaster stub + Editor-gated broadcast.
- Phase 2 — EditMode fixture + compile-check.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | ThemeBroadcaster stub MonoBehaviour | _pending_ | _pending_ | Create `Assets/Scripts/UI/Theme/ThemeBroadcaster.cs` — scene MonoBehaviour with `[SerializeField] UiTheme theme` + `BroadcastAll()` method. Discovery via `FindObjectsOfType<MonoBehaviour>()` in `Awake`, filter `IThemed`, cache list. No per-frame scan. `IThemed` interface stub lands here too (empty `void ApplyTheme(UiTheme)` — real impls in Step 2). |
| T2.2 | UiTheme.OnValidate Editor gate | _pending_ | _pending_ | Add `OnValidate` to `UiTheme.cs` under `#if UNITY_EDITOR` guard. Find active `ThemeBroadcaster` via `UnityEngine.Object.FindObjectsOfType<ThemeBroadcaster>()` (Editor-only — no Player impact). Call `BroadcastAll()` on each. No-op if none found. Wrap every broadcast ref in `#if UNITY_EDITOR` so Player build is untouched. |
| T2.3 | EditMode test — OnValidate fires broadcast | _pending_ | _pending_ | New `Assets/Tests/EditMode/UI/UiTheme_OnValidateTests.cs`: load test `UiTheme` + scene `ThemeBroadcaster` + one mock `IThemed`. Assert `ApplyTheme` called once after `EditorUtility.SetDirty` + `OnValidate` invocation. Covers Review Note D (Editor gate). |
| T2.4 | Compile-check + Player path | _pending_ | _pending_ | Run `npm run unity:compile-check` for Editor. Verify `#if !UNITY_EDITOR` compile path (Player build) has zero references to `ThemeBroadcaster` from `UiTheme.OnValidate`. Add a trivial `UNITY_EDITOR == false` conditional compile test in a smoke fixture if needed. Update `ia/specs/ui-design-system.md` §1.5 note that Editor-only broadcast exists. |

---

### Stage 3 — ThemedPrimitive ring / IThemed + ThemedPrimitiveBase

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Finalize `IThemed` contract + `ThemedPrimitiveBase` with `Awake`-cached theme resolution. Covers invariant #3 + #4 foundation pattern every downstream primitive inherits.

**Exit:**

- `Assets/Scripts/UI/Primitives/IThemed.cs` — full interface with `void ApplyTheme(UiTheme)` + XML doc.
- `Assets/Scripts/UI/Primitives/ThemedPrimitiveBase.cs` — `abstract class ThemedPrimitiveBase : MonoBehaviour, IThemed` with `[SerializeField] protected UiTheme theme` + `Awake`-cached `FindObjectOfType<UiTheme>()` fallback + `ApplyTheme(theme)` call.
- EditMode coverage — base class `Awake` binding path + fallback path.
- Phase 1 — Contract + base class.
- Phase 2 — Base class tests + glossary row.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | IThemed interface (finalize) | _pending_ | _pending_ | Promote Stage 1.2 stub `IThemed.cs` to final form. Add XML doc citing token swap lifecycle + `Awake`-cache rule + Editor `OnValidate` entry path. Put under `Assets/Scripts/UI/Primitives/IThemed.cs`. Move stub from Stage 1.2 if it landed elsewhere. |
| T3.2 | ThemedPrimitiveBase MonoBehaviour | _pending_ | _pending_ | `Assets/Scripts/UI/Primitives/ThemedPrimitiveBase.cs` — `abstract class ThemedPrimitiveBase : MonoBehaviour, IThemed`. `[SerializeField] protected UiTheme theme`. `Awake`: if `theme == null` → `theme = FindObjectOfType<UiTheme>()` once; call `ApplyTheme(theme)`. `ApplyTheme` abstract. Invariant #3 rationale in XML doc: no per-frame scan. |
| T3.3 | EditMode tests for base + fallback | _pending_ | _pending_ | `Assets/Tests/EditMode/UI/ThemedPrimitiveBaseTests.cs`: two fixtures — (a) `theme` serialized → `Awake` calls `ApplyTheme` once with serialized ref; (b) `theme` null → `FindObjectOfType` fallback resolves before `ApplyTheme`. Mock subclass captures call count. |
| T3.4 | Glossary rows (primitive contracts) | _pending_ | _pending_ | Add to `ia/specs/glossary.md`: `Themed primitive` (MonoBehaviour under `Assets/Scripts/UI/Primitives/*` implementing `IThemed`), `IThemed contract` (single-method repaint interface), `ThemedPrimitiveBase` (abstract base with `Awake`-cached theme). |

### Stage 4 — ThemedPrimitive ring / Primitives batch A (Panel / Button / Label / Icon / Tooltip)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship first 5 structural primitives — layout + typography + iconography focused. Each inherits `ThemedPrimitiveBase`, reads tokens in `ApplyTheme`, writes to uGUI `Image` / `TextMeshProUGUI` / child references. Allocation-free repaint path.

**Exit:**

- `ThemedPanel.cs` — `Image.color = theme.surfaceBase`; optional `ShadowDepth` slot (wired in Step 4).
- `ThemedButton.cs` — background + label color + hover / pressed variants from token accent ladder.
- `ThemedLabel.cs` — font / size / color from `UiTheme` typography fields.
- `ThemedIcon.cs` — tint from token accent ladder; sprite ref external.
- `ThemedTooltip.cs` — panel + label composition; delay / fade duration from `theme.motion.alertPulse` or dedicated tooltip entry.
- All under `Assets/Scripts/UI/Primitives/`.
- Phase 1 — Layout + typography primitives (Panel / Label / Icon).
- Phase 2 — Interactive + composite (Button / Tooltip).

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | ThemedPanel | _pending_ | _pending_ | `Assets/Scripts/UI/Primitives/ThemedPanel.cs` — `class ThemedPanel : ThemedPrimitiveBase`. Requires `Image` component. `ApplyTheme`: `image.color = theme.surfaceBase`; `image.sprite = theme.panelBackgroundSprite` (if field exists; else skip). Optional `[SerializeField] PanelElevation elevation` enum → maps to `theme.studioRack.shadowDepthStops[idx]`. |
| T4.2 | ThemedLabel | _pending_ | _pending_ | `ThemedLabel.cs` — wraps `TextMeshProUGUI`. `ApplyTheme`: `text.font = theme.primaryFont`; `text.color = theme.textPrimary`; `text.fontSize = theme.typography.bodySize` (or expose `LabelTier` enum mapping to h1/h2/body/caption sizes). |
| T4.3 | ThemedIcon | _pending_ | _pending_ | `ThemedIcon.cs` — wraps `Image` sized via token grid (`theme.iconGrid`). `ApplyTheme`: `image.color = theme.accentPrimary` (or enum-selected accent tier). Sprite ref Inspector-set per instance. |
| T4.4 | ThemedButton | _pending_ | _pending_ | `ThemedButton.cs` — requires `Button` + background `Image` + `ThemedLabel` child. `ApplyTheme`: sets base + hover + pressed + disabled color transitions on `Button.colors` from accent ladder. Hover pulse amplitude reads `theme.motion.alertPulse` (juice wires in Step 4). |
| T4.5 | ThemedTooltip | _pending_ | _pending_ | `ThemedTooltip.cs` — composite: `ThemedPanel` + `ThemedLabel`. `ApplyTheme` cascades to children (cached `Awake`). Fade in/out duration reads `theme.motion` dedicated tooltip entry (add to `MotionBlock` in Stage 1.1 if not present — cross-check before filing). |

### Stage 5 — ThemedPrimitive ring / Primitives batch B (Tab / Slider / Toggle / List / OverlayToggleRow) + broadcaster wiring

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship remaining 5 structural primitives + promote `ThemeBroadcaster` stub to full impl + land `UIManager.ThemeBroadcast.cs` partial. Token-swap end-to-end test lives here.

**Exit:**

- `ThemedTabBar`, `ThemedSlider`, `ThemedToggle`, `ThemedList`, `ThemedOverlayToggleRow` shipped under `Assets/Scripts/UI/Primitives/`.
- `ThemeBroadcaster.cs` full impl — `Awake` scans `FindObjectsOfType<MonoBehaviour>()`, filters `IThemed`, caches list; `BroadcastAll()` iterates cached list; additional subscribers registered via `Register(IThemed)` for runtime spawns.
- `UIManager.ThemeBroadcast.cs` partial — `[SerializeField] private ThemeBroadcaster themeBroadcaster` + `partial void Start_ThemeBroadcast()` hook + single `themeBroadcaster.BroadcastAll()` call. Invoked from existing `UIManager.Start` via partial method pattern; no body changes to other partials.
- EditMode test: scene w/ 10 primitives + `ThemeBroadcaster`; token field edit → all 10 repaint (assert `ApplyTheme` called on each).
- Phase 1 — Batch B primitives.
- Phase 2 — Broadcaster full impl + UIManager partial.
- Phase 3 — Integration test + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | ThemedTabBar + ThemedList | _pending_ | _pending_ | `ThemedTabBar.cs` — horizontal tab row; each tab is a `ThemedButton` variant with selected/unselected color states. `ThemedList.cs` — vertical list container; row spacing from `theme.spacing.listRow`; alternate row bg from `theme.surfaceAlt`. Both `ApplyTheme` cascade to children. |
| T5.2 | ThemedSlider + ThemedToggle | _pending_ | _pending_ | `ThemedSlider.cs` — wraps uGUI `Slider`; track / fill / handle colors from accent ladder. `ThemedToggle.cs` — wraps `Toggle`; on/off colors + checkmark icon tint from token accent. Both `ApplyTheme` writes to child `Image.color` fields cached `Awake`. |
| T5.3 | ThemedOverlayToggleRow | _pending_ | _pending_ | `ThemedOverlayToggleRow.cs` — composite for Bucket 2 signal overlays (pollution / crime / traffic / etc.). Holds `ThemedIcon` + `ThemedLabel` + a small active-state indicator slot (filled by `LED` in Step 3). `ApplyTheme` cascades to named children. One instance per signal. |
| T5.4 | ThemeBroadcaster full impl | _pending_ | _pending_ | Promote Stage 1.2 stub `ThemeBroadcaster.cs` to full runtime impl. `Awake`: single scan `FindObjectsOfType<MonoBehaviour>()` → filter `IThemed` → cache `List<IThemed>`. Expose `Register(IThemed)` / `Unregister(IThemed)` for runtime spawns. `BroadcastAll()` foreach cached list → `ApplyTheme(theme)`. Invariant #3 compliant — scan only in `Awake` + explicit `Register`. |
| T5.5 | UIManager.ThemeBroadcast partial | _pending_ | _pending_ | New file `Assets/Scripts/Managers/GameManagers/UIManager.ThemeBroadcast.cs`. `partial class UIManager` with `[SerializeField] private ThemeBroadcaster themeBroadcaster;` + `private void Start_ThemeBroadcast() { themeBroadcaster?.BroadcastAll(); }`. Wire the call from existing `UIManager.cs` `Start` via a single added line (the only edit to an existing partial; document the one-line exemption in commit). |
| T5.6 | Token-swap integration test + glossary | _pending_ | _pending_ | EditMode / PlayMode test scene with all 10 primitives + `ThemeBroadcaster`. Token edit → `BroadcastAll()` → assert `ApplyTheme` called once per primitive. Assert allocation-free repaint via `GC.GetTotalMemory` delta check. Glossary row: `ThemeBroadcaster` (scene MonoBehaviour, `Awake`-cached `IThemed` list, invariant #3 + #4 compliant). |

---

### Stage 6 — StudioControl ring / IStudioControl + StudioControlBase + contract tests

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Lock the signal-binding contract + base class every widget inherits. Prove allocation-free `Update` path before any widget ships.

**Exit:**

- `IStudioControl.cs` final signature (per exploration §CityStats handoff table).
- `StudioControlBase.cs` — inherits `ThemedPrimitiveBase`; caches `Func<float> _source`; `Update` reads `_source?.Invoke() ?? 0f` into `Value` then calls abstract `RenderValue(float)`.
- PlayMode test — bind source → 1000 `Update` frames → `GC.Alloc` delta == 0.
- Phase 1 — Contract + base class.
- Phase 2 — Alloc-free PlayMode fixture.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | IStudioControl interface | _pending_ | _pending_ | `Assets/Scripts/UI/StudioControls/IStudioControl.cs` — `float Value`, `Vector2 Range`, `AnimationCurve ValueToDisplay`, `void BindSignalSource(Func<float>)`, `void Unbind()`. XML doc: bind once in `Awake` / `OnEnable`; never rebind per frame. Matches exploration §CityStats handoff table row 3. |
| T6.2 | StudioControlBase abstract class | _pending_ | _pending_ | `StudioControlBase.cs` — `abstract class StudioControlBase : ThemedPrimitiveBase, IStudioControl`. Fields: `protected Func<float> _source; public float Value { get; private set; }; public Vector2 Range; public AnimationCurve ValueToDisplay;`. `Update`: if `_source != null` → `Value = _source.Invoke()` → call `protected abstract void RenderValue(float normalized)` with `Mathf.InverseLerp(Range.x, Range.y, Value)`. |
| T6.3 | Alloc-free PlayMode fixture | _pending_ | _pending_ | `Assets/Tests/PlayMode/UI/StudioControlAllocTests.cs`: mock widget subclass; bind `() => Time.time`; run 1000 frames via `yield return null`; assert `GC.GetTotalMemory(false)` delta < 64 bytes (headroom for Unity internals). Covers Review Note C concern — baseline for JuiceLayer tuning in Step 4. |
| T6.4 | Glossary rows (studio contracts) | _pending_ | _pending_ | Add to `ia/specs/glossary.md`: `StudioControl primitive` (interactive MonoBehaviour under `Assets/Scripts/UI/StudioControls/*` implementing `IStudioControl` + `IThemed`), `IStudioControl contract` (value / range / curve / bind-source interface), `Signal source binding` (Awake-cached `Func<float>` read each `Update` without alloc). |

### Stage 7 — StudioControl ring / Simple widgets (LED / IlluminatedButton / SegmentedReadout / DetentRing)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship 4 simple widgets — no ring-buffer / needle-ballistics state; direct value → visual mapping. Exercises `StudioControlBase` contract in simplest form.

**Exit:**

- `LED.cs` — hue + on/off + pulse amplitude from tokens; `RenderValue(normalized)` sets `Image.color` intensity.
- `IlluminatedButton.cs` — `ThemedButton` + `LED` child; click handler raises event; LED state driven by bound source.
- `SegmentedReadout.cs` — 7-segment style numeric display; renders `Value` formatted per `DisplayFormat` enum (int / currency / time).
- `DetentRing.cs` — non-interactive indicator dots around a `Knob` socket; `RenderValue` lights nearest dot.
- Prefabs per widget under `Assets/UI/Prefabs/StudioControls/`.
- Phase 1 — LED + IlluminatedButton.
- Phase 2 — SegmentedReadout + DetentRing + prefabs.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | LED widget | _pending_ | _pending_ | `Assets/Scripts/UI/StudioControls/LED.cs` — `class LED : StudioControlBase`. `[SerializeField] int hueIndex` → picks from `theme.studioRack.ledHues`. `RenderValue(n)`: sets child `Image.color = baseHue * n` + optional glow pulse amplitude scaled by `n`. `ApplyTheme` re-reads hue. |
| T7.2 | IlluminatedButton widget | _pending_ | _pending_ | `IlluminatedButton.cs` — composite: `ThemedButton` + child `LED`. Exposes `UnityEvent OnPress`. Bound source drives LED brightness; click raises event + pulses LED via `PulseOnEvent` helper (wires in Step 4 — optional `ISfxEmitter` hook exposed but not wired — Review Note E). |
| T7.3 | SegmentedReadout widget | _pending_ | _pending_ | `SegmentedReadout.cs` — `[SerializeField] DisplayFormat format` enum (Int / Currency / Time / Percent). `RenderValue(n)`: formats `Value` per enum, writes to `TextMeshProUGUI` child w/ segmented-display font. Char width from `theme.typography.monoWidth`. |
| T7.4 | DetentRing widget + prefabs | _pending_ | _pending_ | `DetentRing.cs` — non-interactive; `[SerializeField] int detentCount`. `RenderValue(n)`: picks `index = Mathf.RoundToInt(n * (detentCount - 1))`, sets child `Image[]` colors (active = `theme.studioRack.knobDetentColor`, inactive = dim). Author 4 prefabs under `Assets/UI/Prefabs/StudioControls/` — one per widget landed in this stage. |

### Stage 8 — StudioControl ring / Complex widgets (Knob / Fader / VUMeter / Oscilloscope) + tests

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship 4 complex widgets with animation state (drag handling / needle ballistics / ring-buffer traces). All alloc-free `Update`. PlayMode tests cover value binding + visual state + GC delta.

**Exit:**

- `Knob.cs` — drag-to-rotate handler; snaps to detents if `DetentRing` child present; exposes `OnValueChanged` event for external bindings (e.g. tax-rate knob per exploration Example 2).
- `Fader.cs` — vertical track + cap; drag changes `Value`; optional level-meter strip alongside.
- `VUMeter.cs` — needle + gradient strip w/ attack/release/peak-hold ballistics from `theme.motion.needleAttack/Release`.
- `Oscilloscope.cs` — rolling ring-buffer trace driven by bound source; buffer size configurable; redraw via `LineRenderer` or cached `Mesh`.
- Prefabs per widget under `Assets/UI/Prefabs/StudioControls/`.
- PlayMode tests — bind source, drive value curve, assert visual state + `GC.Alloc` delta == 0 across 1000 frames.
- Phase 1 — Knob + Fader (drag-interactive).
- Phase 2 — VUMeter + Oscilloscope (animation state).
- Phase 3 — Prefabs + PlayMode test suite.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | Knob widget | _pending_ | _pending_ | `Knob.cs` — `class Knob : StudioControlBase, IDragHandler, IPointerDownHandler`. Drag delta maps to `Value` via `ValueToDisplay` curve. Optional `[SerializeField] DetentRing detents` child; if set, `Value` snaps to nearest detent step. `UnityEvent<float> OnValueChanged`. `RenderValue(n)`: rotates indicator child `transform.localRotation`. Covers exploration Example 2. |
| T8.2 | Fader widget | _pending_ | _pending_ | `Fader.cs` — vertical `Slider`-like drag. `[SerializeField] RectTransform cap` + `RectTransform track`. Drag updates `Value`; `RenderValue(n)` moves cap along track. Track gradient from `theme.studioRack.faderTrackGradient`. Optional adjacent `LED` strip bound to same value for level-meter appearance. |
| T8.3 | VUMeter widget | _pending_ | _pending_ | `VUMeter.cs` — needle + gradient strip. State: `_displayed`, `_peakHold`, `_peakHoldTimer`. `Update`: reads `_source` → target; displaced via attack/release from `theme.motion.needleAttack/Release` (exponential smoothing); peak-hold pin decays after `peakHoldDuration`. `RenderValue` rotates needle + repaints gradient mask. Covers exploration Example 1. |
| T8.4 | Oscilloscope widget | _pending_ | _pending_ | `Oscilloscope.cs` — `[SerializeField] int bufferSize = 256`. Ring buffer `float[bufferSize]` allocated once in `Awake`. `Update`: writes `Value` at `_head` index → increments. `RenderValue` updates `LineRenderer.SetPositions` via cached `Vector3[]` — zero alloc. Trace color from `theme.studioRack.oscilloscopeTrace` + glow. |
| T8.5 | Prefabs (4 complex widgets) | _pending_ | _pending_ | Author prefabs under `Assets/UI/Prefabs/StudioControls/`: `Knob.prefab`, `Fader.prefab`, `VUMeter.prefab`, `Oscilloscope.prefab`. Each pre-wires `StudioControlBase` serialized fields (theme, range, curve) + primitive children (panel bg, label, indicator sprites). |
| T8.6 | PlayMode tests + widget glossary | _pending_ | _pending_ | `Assets/Tests/PlayMode/UI/StudioControlWidgetsTests.cs`: per widget — bind `() => Mathf.Sin(Time.time)`; run 1000 frames; assert `GC.Alloc` delta == 0; assert indicator child moves / rotates per expected curve. Add glossary rows: `Knob`, `Fader`, `VU meter`, `Oscilloscope`, `Illuminated button`, `Segmented readout`, `Detent ring`, `LED`. |

---

### Stage 9 — JuiceLayer ring / JuiceLayer + helpers batch A (TweenCounter / PulseOnEvent / ShadowDepth)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Scene-root `JuiceLayer` + first 3 helpers landing numeric tween + event pulse + elevation. Prove pooling pattern once before remaining helpers.

**Exit:**

- `JuiceLayer.cs` scene component with `[SerializeField] UiTheme theme` + helper pool references + `Awake` fallback lookup. No singleton.
- `TweenCounter` — poolable struct/class; `Animate(from, to, motionEntry, callback)` reads `theme.motion.moneyTick`; updated via `JuiceLayer.Update` central tick loop.
- `PulseOnEvent` — MonoBehaviour slot on any primitive; `Trigger()` applies scale + glow pulse over `theme.motion.alertPulse`.
- `ShadowDepth` — `MonoBehaviour` on `ThemedPanel`; renders shadow sprite offset + alpha per `theme.studioRack.shadowDepthStops[tier]`.
- Phase 1 — JuiceLayer scene MonoBehaviour + central tick loop.
- Phase 2 — Helpers A (TweenCounter / PulseOnEvent / ShadowDepth).

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | JuiceLayer scene MonoBehaviour | _pending_ | _pending_ | `Assets/Scripts/UI/Juice/JuiceLayer.cs` — `class JuiceLayer : MonoBehaviour`. Inspector field `[SerializeField] UiTheme theme` + `Awake` fallback. Pool containers for active tweens (`List<TweenCounter> _activeTweens`). `Update` iterates active pools in-place (backward loop for removal). Primitives find it via `FindObjectOfType<JuiceLayer>()` in `Awake` only. Invariant #3 + #4 compliant. |
| T9.2 | TweenCounter helper | _pending_ | _pending_ | `Assets/Scripts/UI/Juice/Helpers/TweenCounter.cs` — value-type struct or poolable class. `Animate(from, to, MotionEntry entry, Action<float> onUpdate)` — pushed into `JuiceLayer._activeTweens`; ticked per frame. Easing curve from `entry.easing`; duration from `entry.durationSeconds`. Callback writes lerped value to caller (e.g. `SegmentedReadout.SetValue`). Covers Example 4. |
| T9.3 | PulseOnEvent + ShadowDepth | _pending_ | _pending_ | `PulseOnEvent.cs` MonoBehaviour — slot on any primitive; `Trigger()` pushes scale + glow animation into `JuiceLayer` tween pool reading `theme.motion.alertPulse`. `ShadowDepth.cs` MonoBehaviour — paired with `ThemedPanel`; reads `[SerializeField] int tier` → indexes `theme.studioRack.shadowDepthStops[tier]` → offsets child shadow sprite + sets alpha. Both `IThemed` so token swap retunes. |

### Stage 10 — JuiceLayer ring / Helpers batch B (SparkleBurst / NeedleBallistics / OscilloscopeSweep) + alloc tests

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Remaining 3 helpers + allocation-free verification suite + retrofit Step 3 widgets (VUMeter / Oscilloscope) to consume shared helpers instead of inlined logic.

**Exit:**

- `SparkleBurst` — particle burst helper using pooled `ParticleSystem` + token palette.
- `NeedleBallistics` — struct helper consumed by `VUMeter`; replaces inline state from Stage 3.3.
- `OscilloscopeSweep` — ring-buffer helper consumed by `Oscilloscope`; replaces inline buffer.
- VUMeter + Oscilloscope retrofitted to use helpers (reduces Stage 3.3 debt).
- PlayMode test suite — 6 helpers each running 1000 frames `GC.Alloc` == 0. Covers Review Note C baseline for `/verify-loop` profiler validation.
- Phase 1 — SparkleBurst w/ pooled particles.
- Phase 2 — NeedleBallistics + OscilloscopeSweep + widget retrofit.
- Phase 3 — Alloc-free suite + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | SparkleBurst helper | _pending_ | _pending_ | `SparkleBurst.cs` MonoBehaviour — pooled `ParticleSystem` with color-over-lifetime from `theme.studioRack.sparklePalette`. `Burst(Vector2 position)` emits fixed particle count; duration from `theme.motion.sparkleDuration`. `ParticleSystem.Emit` uses `EmitParams` struct — no alloc. |
| T10.2 | NeedleBallistics struct | _pending_ | _pending_ | `NeedleBallistics.cs` — value-type struct w/ `_displayed`, `_peakHold`, `_peakTimer`. `Tick(float target, float dt, MotionEntry attack, MotionEntry release, float peakHoldSeconds) → float`. Called from `VUMeter.Update` — replaces inline state from T3.3.3. Returns interpolated needle position. |
| T10.3 | OscilloscopeSweep + widget retrofit | _pending_ | _pending_ | `OscilloscopeSweep.cs` — ring-buffer helper class; owns `float[bufferSize]` + `_head`. `Write(float sample)` + `CopyTo(Vector3[] positions)`. Retrofit `Oscilloscope.cs` (T3.3.4) to delegate to this helper. Refit `VUMeter.cs` (T3.3.3) to delegate to `NeedleBallistics` struct. |
| T10.4 | Alloc-free PlayMode suite | _pending_ | _pending_ | `Assets/Tests/PlayMode/UI/JuiceAllocTests.cs`: per helper — 1000 frames of activity (active tween, repeated pulse, continuous sparkle loop, needle sweep, oscilloscope write). Assert `GC.GetTotalMemory` delta < 64 bytes. Baseline captured for `/verify-loop` profiler validation on closeout. |
| T10.5 | Juice glossary rows | _pending_ | _pending_ | Add to `ia/specs/glossary.md`: `Juice layer` (scene MonoBehaviour hosting pooled tween / particle / pulse helpers), `Tween counter`, `Pulse on event`, `Sparkle burst`, `Shadow depth`, `Needle ballistics`, `Oscilloscope sweep`. Each row cites token contract + alloc-free guarantee. |

---

### Stage 11 — Flagship HUD + Toolbar + overlay polish / HUD migration + BUG-14 + TECH-72

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Replace HUD row widgets with primitives + studio controls + juice. Resolve BUG-14 + TECH-72 in the same stage (natural scope — both touch HUD / uGUI controllers).

**Exit:**

- HUD row widgets (money / pop / date / happiness / speed / scale) migrated.
- Money readout animates transactions via `TweenCounter` + `SparkleBurst` (positive) / `PulseOnEvent` (negative).
- Happiness `VUMeter` bound to `CityStatsUIController` facade getter per exploration Example 1.
- BUG-14 fixed (Grep-verified: zero `FindObjectOfType` in `Update` across `Assets/Scripts/Managers/GameManagers/UIManager.*.cs` + new UI widget dirs).
- TECH-72 fixed (HUD / uGUI scene hierarchy cleaned; prefab catalog migration noted).
- Phase 1 — Money + date + scale row migration.
- Phase 2 — Happiness + speed + HUD bg + BUG-14 sweep.
- Phase 3 — TECH-72 scene hygiene + integration test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | Money readout migration | _pending_ | _pending_ | Replace existing money HUD row widget with `SegmentedReadout` + `TweenCounter` binding. `EconomyManager.OnTransaction(delta)` → HUD listener calls `TweenCounter.Animate(old, new, theme.motion.moneyTick, v => readout.SetValue(v))`. On positive delta: `SparkleBurst.Burst(readoutPos)`. On negative: `PulseOnEvent.Trigger()` in `accentNegative`. Covers exploration Example 4. |
| T11.2 | Date + scale indicator migration | _pending_ | _pending_ | Date row → `ThemedLabel` + `SegmentedReadout` (day/month/year). Scale indicator → `LED` row with multi-hue (one LED per active scale: city / district / region — per multi-scale plan). Bound to existing scale manager facade. |
| T11.3 | Happiness VU + speed buttons | _pending_ | _pending_ | Happiness row → `VUMeter` prefab; bind `() => citystats.GetScalar("happiness.cityAverage")` in `HUDController.Awake`. Speed controls → 4 `IlluminatedButton` instances (pause / slow / normal / fast); active state driven by `TimeManager.speed` getter. Button press events wire to existing `TimeManager.SetSpeed`. |
| T11.4 | HUD panel bg + BUG-14 sweep | _pending_ | _pending_ | HUD bar root → `ThemedPanel` + `ShadowDepth` tier 2 elevation. BUG-14 sweep: Grep `FindObjectOfType` across `Assets/Scripts/Managers/GameManagers/UIManager.*.cs` + any HUD controller under `Assets/Scripts/UI/*`; every offending site → cache in `Awake` or move to serialized ref. Update BUG-14 notes with verification command. |
| T11.5 | TECH-72 scene hygiene | _pending_ | _pending_ | Clean HUD scene hierarchy per TECH-72 scope — prefab consolidation, RectTransform anchoring review, Canvas group organization. Migrate HUD row instances into prefab catalog under `Assets/UI/Prefabs/HUD/`. Update TECH-72 acceptance notes with before/after hierarchy depth + prefab count. |
| T11.6 | HUD PlayMode integration test | _pending_ | _pending_ | `Assets/Tests/PlayMode/UI/HudIntegrationTests.cs`: load scene with HUD + mock economy / citystats facades; trigger transaction → assert `SegmentedReadout` value tweens + sparkle/pulse fires. Assert `GC.Alloc` delta bounded across 10 transactions. Close BUG-14 + TECH-72 verification gate here. |

### Stage 12 — Flagship HUD + Toolbar + overlay polish / Toolbar + overlay migration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Migrate toolbar rows + overlay toggle row. Bucket 2 signal scalars bound via `IStudioControl.BindSignalSource` into LED indicators. Tool-select pulse closes the flagship polish loop for Step 5.

**Exit:**

- Toolbar rows → `ThemedPanel` + `IlluminatedButton` clusters; tool-category switch raises `PulseOnEvent`.
- Overlay toggle row: one `ThemedOverlayToggleRow` per Bucket 2 signal (6 signals). Each row carries `ThemedIcon` + `ThemedLabel` + `LED` active-state indicator.
- `LED.BindSignalSource(() => overlayManager.GetSignalIntensity(signalKind))` wired in `ToolbarController.Awake`.
- Toolbar bg `ThemedPanel` + `ShadowDepth` per tier.
- `npm run validate:all` + `npm run unity:compile-check` green.
- Phase 1 — Toolbar button migration + tool-select pulse.
- Phase 2 — Overlay toggle row + Bucket 2 signal binding.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | Toolbar cluster migration | _pending_ | _pending_ | Existing toolbar zoning / service / transport buttons → `IlluminatedButton` clusters under `ThemedPanel` rows. Active-state LED bound to `ToolbarController.currentTool` matcher. Row bg `ThemedPanel` + `ShadowDepth` tier 1. |
| T12.2 | Tool-category pulse | _pending_ | _pending_ | On `ToolbarController.SetCategory(newCategory)` event → `PulseOnEvent.Trigger()` on newly-active cluster + inverse pulse on deactivating cluster. Duration from `theme.motion.alertPulse`. No per-frame scan; event-driven only. |
| T12.3 | Overlay toggle rows (6 signals) | _pending_ | _pending_ | Instantiate 6 `ThemedOverlayToggleRow` instances under toolbar overlay area — one per Bucket 2 signal (pollution / crime / traffic / happiness / zone / desirability). Each row: icon + label + `LED`. Toggle click → `OverlayRenderer.SetSignalEnabled(kind, bool)`. |
| T12.4 | LED signal-intensity binding | _pending_ | _pending_ | In `ToolbarController.Awake`, each overlay row's `LED` gets `BindSignalSource(() => overlayManager.GetSignalIntensity(kind))`. Verify zero per-frame alloc across 1000 frames. Confirms invariant #3 + completes flagship polish exit criteria. |

---

### Stage 13 — CityStats handoff artifacts / Spec publication + glossary gap audit

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Promote the token / primitive / juice catalog to normative status in `ui-design-system.md` §1–§2 + §1.5. Audit glossary for missing Stats-dashboard-consumable rows; patch gaps.

**Exit:**

- `ui-design-system.md` §1 (palette + tokens) + §1.5 (motion) + §2 (components) carry normative references to every shipped primitive + StudioControl + juice helper + token block.
- Glossary gap audit table captured in stage handoff (not persisted — validation only); any missing rows added.
- Phase 1 — ui-design-system.md normative promotion.
- Phase 2 — Glossary audit + gap patch.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T13.1 | ui-design-system §1 + §1.5 promotion | _pending_ | _pending_ | Edit `ia/specs/ui-design-system.md` §1 — add "Extended token catalog" subsection listing every field in `UiTheme.studioRack` + `UiTheme.motion` with role description. §1.5 — motion block becomes normative (semantically-named entries + durations / curves). Cite back to `docs/ui-polish-exploration.md` as source of record. |
| T13.2 | ui-design-system §2 components | _pending_ | _pending_ | `ia/specs/ui-design-system.md` §2 extended with ThemedPrimitive family (10) + StudioControl family (8) + JuiceLayer + 6 helpers — each as a subsection with type name + file path + contract ref (`IThemed` / `IStudioControl`). Marks the catalog normative (CityStats must reference, not reinvent). |
| T13.3 | Glossary gap audit + patch | _pending_ | _pending_ | Cross-check terms in exploration §CityStats handoff table against `ia/specs/glossary.md`. Expected rows: token ring, studio-rack token, motion token, themed primitive, studio-control primitive, knob, fader, VU meter, oscilloscope, illuminated button, segmented readout, detent ring, LED, juice layer, tween counter, pulse on event, sparkle burst, shadow depth, needle ballistics, oscilloscope sweep, IThemed contract, IStudioControl contract, ThemeBroadcaster, signal source binding. Any missing → add in this task. |

### Stage 14 — CityStats handoff artifacts / Cross-plan update + handoff notify

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Edit `citystats-overhaul-master-plan.md` Dashboard step acceptance to reference this bucket's catalog. Seed a handoff notify so CityStats team picks up the contract without re-deriving.

**Exit:**

- `ia/projects/citystats-overhaul-master-plan.md` Dashboard step acceptance reads: "consumes UiTheme + StudioControl + JuiceLayer per `docs/ui-polish-exploration.md` §Design Expansion → CityStats handoff".
- Handoff notify entry added to CityStats master plan Decision Log (or equivalent section) pointing at this plan + the contract table.
- FEAT-51 backlog entry `Notes` field updated referencing this handoff (via `backlog_issue FEAT-51` MCP tool → confirm ref).
- Phase 1 — citystats-overhaul-master-plan update.
- Phase 2 — Handoff notify + FEAT-51 note.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T14.1 | citystats-overhaul Dashboard acceptance | _pending_ | _pending_ | Edit `ia/projects/citystats-overhaul-master-plan.md` — locate Dashboard step (or nearest equivalent section). Append to Exit criteria: "consumes UiTheme + StudioControl + JuiceLayer per `docs/ui-polish-exploration.md` §Design Expansion → CityStats handoff". Do NOT add new steps / stages / tasks to that master plan — acceptance bullet only. |
| T14.2 | citystats-overhaul Relevant surfaces | _pending_ | _pending_ | Add to `citystats-overhaul-master-plan.md` Dashboard step Relevant surfaces: `docs/ui-polish-exploration.md §Design Expansion → CityStats handoff`, `ia/specs/ui-design-system.md §2 Components`, `ia/projects/ui-polish-master-plan.md` (Steps 1–4 outputs). |
| T14.3 | FEAT-51 note + handoff notify | _pending_ | _pending_ | Via MCP `backlog_issue FEAT-51` → confirm open → add handoff note to the FEAT-51 `ia/backlog/FEAT-51.yaml` `notes` field (per `ia/skills/project-new/SKILL.md` update pattern) citing ui-polish catalog availability. Seed a handoff entry at the top of `citystats-overhaul-master-plan.md` Decision Log pointing at this plan + contract table. |

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) runs.
- Run `claude-personal "/stage-file ia/projects/ui-polish-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/ui-polish-exploration.md` + downstream plans (citystats-overhaul).
- Respect ring order strictly — do NOT start Step 2 until Step 1 Final; do NOT start Step 3 until Step 2 Final; Step 4 may parallelize with Step 3 Stage 3.3 retrofit window (see Stage 4.2 note). Step 5 blocked on Steps 2+3+4 Final. Step 6 (handoff) can run after Step 4 Final — does not block on Step 5.
- Keep this orchestrator synced with any umbrella issue (full-game-mvp tracker row) — per `/closeout` umbrella-sync rule.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers `Status: Final`; the file stays.
- Silently promote functional-tier screens (info panels / settings / save-load / pause / onboarding / glossary / tooltips) into flagship juice scope — they stay primitives-only per Locked decisions. Studio-rack on those surfaces is post-MVP.
- Edit `UIManager.cs` or existing `UIManager.*.cs` partials beyond the single one-line `Start_ThemeBroadcast()` call from Stage 2.3 T2.3.5 — invariant #6.
- Add per-frame `FindObjectOfType` anywhere — invariant #3 enforced by BUG-14 verification in Stage 5.1 T5.1.4.
- Add new singletons — `JuiceLayer` + `ThemeBroadcaster` + `UiTheme` all scene / asset components; invariant #4.
- Wire Bucket 7 SFX hooks in this bucket — `ISfxEmitter` interface exposed but intentionally unwired (Review Note E). Bucket 7 owns wiring.
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.

---
