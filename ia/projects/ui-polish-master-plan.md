# UI Polish — Master Plan (Flagship polish for Main HUD + Toolbar + Overlays)

> **Status:** Draft — Step 1 / Stage 1.1 pending (no BACKLOG rows filed yet)
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

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Step 1 — Token ring extension

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 1):** 0 filed

**Objectives:** Extend existing `UiTheme` ScriptableObject with full studio-rack token block + motion block. Update `DefaultUiTheme.asset` with dark-first defaults. Broadcast on `OnValidate` (Editor-only gate) so token edits repaint live. Foundation for all downstream rings — primitives + studio controls + juice helpers read every visual + motion parameter from tokens, never hard-coded.

**Exit criteria:**

- `Assets/Scripts/Managers/GameManagers/UiTheme.cs` extended with studio-rack block (LED hues, VU gradient stops, knob detent color, fader track gradient, oscilloscope trace, shadow depth stops, glow radius + color, sparkle palette) + motion block (`moneyTick`, `alertPulse`, `needleAttack`, `needleRelease`, `sparkleDuration`, `panelElevate`, easing curves). Existing fields untouched.
- `Assets/UI/Theme/DefaultUiTheme.asset` updated with dark-first + studio-rack default values per `ui-design-system §7.1`.
- `OnValidate` Editor repaint broadcast in place, wrapped `#if UNITY_EDITOR`.
- `ia/specs/ui-design-system.md` §1 + §1.5 extended with the new token catalog (normative).
- Glossary rows land: `UiTheme token ring`, `Studio-rack token`, `Motion token`.
- `npm run unity:compile-check` green; `npm run validate:all` green.

**Art:** None. SO field expansion + asset default edits only.

**Relevant surfaces (load when step opens):**
- `docs/ui-polish-exploration.md` §Design Expansion → Architecture (Token ring) + Subsystem Impact (row 1: `UiTheme.cs`).
- `ia/specs/ui-design-system.md` §1 (palette / spacing / typography), §1.5 (motion), §7.1 (dark-first defaults).
- `Assets/Scripts/Managers/GameManagers/UiTheme.cs` — existing ScriptableObject; extend additively.
- `Assets/UI/Theme/DefaultUiTheme.asset` — only authored asset instance; update defaults in Inspector.
- `ia/rules/invariants.md` #3 (`OnValidate` must not leak per-frame `FindObjectOfType` — Editor gate only).

#### Stage 1.1 — Token schema extension + default asset defaults

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Extend `UiTheme.cs` fields + update `DefaultUiTheme.asset` defaults. Spec + glossary land in same stage so terminology + asset ship together.

**Exit:**

- `UiTheme.cs` carries new `[Serializable] StudioRackBlock` + `MotionBlock` nested classes with named fields per exploration §Design Expansion.
- `DefaultUiTheme.asset` Inspector values populated for every new field (dark-first + studio-rack palette).
- `ui-design-system.md` §1 + §1.5 sections extended; normative token names match field names.
- Glossary rows added: `UiTheme token ring`, `Studio-rack token`, `Motion token`.

**Phases:**

- [ ] Phase 1 — Schema field expansion + asset defaults.
- [ ] Phase 2 — Spec + glossary updates.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.1.1 | StudioRackBlock schema | 1 | **TECH-309** | Draft | Add `[Serializable] class StudioRackBlock` to `UiTheme.cs` with fields: `ledHues` (`Color[]`), `vuGradientStops` (`Gradient`), `knobDetentColor` (`Color`), `faderTrackGradient` (`Gradient`), `oscilloscopeTrace` (`Color`), `oscilloscopeGlowColor` (`Color`), `shadowDepthStops` (`float[3]`), `glowRadius` (`float`), `glowColor` (`Color`), `sparklePalette` (`Color[]`). Expose via `public StudioRackBlock studioRack` on `UiTheme`. Serializable defaults inert. |
| T1.1.2 | MotionBlock schema + curves | 1 | **TECH-310** | Draft | Add `[Serializable] class MotionBlock` to `UiTheme.cs` with semantic fields: `moneyTick` (duration + curve), `alertPulse`, `needleAttack`, `needleRelease`, `sparkleDuration`, `panelElevate`. Each a `[Serializable] struct MotionEntry { float durationSeconds; AnimationCurve easing; }`. Expose via `public MotionBlock motion` on `UiTheme`. |
| T1.1.3 | DefaultUiTheme.asset defaults | 1 | **TECH-311** | Draft | Populate `Assets/UI/Theme/DefaultUiTheme.asset` Inspector values for every new `studioRack` + `motion` field per `ui-design-system §7.1` dark-first palette. LED hues: green/amber/red triad. VU gradient: green → amber → red. Motion defaults: `moneyTick.durationSeconds = 0.28f`; `needleAttack = 0.08f`; `needleRelease = 0.40f`. |
| T1.1.4 | ui-design-system spec §1 + §1.5 | 2 | **TECH-312** | Draft | Extend `ia/specs/ui-design-system.md` §1 (palette / spacing) with studio-rack token names + role. Extend §1.5 (motion) with motion token catalog. Every field in `StudioRackBlock` + `MotionBlock` cited normatively. Link from §2 to anchor primitives-to-tokens mapping. |
| T1.1.5 | Glossary rows | 2 | **TECH-313** | Draft | Add rows to `ia/specs/glossary.md`: `UiTheme token ring` (defn: extended token catalog under `UiTheme` SO covering surface / accent / studio-rack / motion blocks), `Studio-rack token` (LED / VU / knob / fader / oscilloscope visual params), `Motion token` (semantic named duration + easing curve entry under `UiTheme.motion`). |

#### Stage 1.2 — OnValidate repaint broadcast + tests

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Wire `OnValidate` broadcast in `UiTheme.cs` (`#if UNITY_EDITOR` gated). Provide a minimal EditMode test fixture that confirms token edits fire broadcast in Editor. No runtime subscribers yet (primitives land in Step 2) — this stage proves the broadcast hook exists + is inert in Player builds.

**Exit:**

- `UiTheme.OnValidate` calls `ThemeBroadcaster.BroadcastAll()` **only** under `#if UNITY_EDITOR` guard (never compiled into Player).
- Stub `ThemeBroadcaster` MonoBehaviour in scene with `BroadcastAll()` method that discovers `IThemed` via `FindObjectsOfType<MonoBehaviour>()` — `Awake`-cached scan list, not per-frame.
- EditMode test confirms `OnValidate` calls broadcaster exactly once per edit; never under Play mode hot loop.
- `#if !UNITY_EDITOR` compile path clean — player build ships without broadcaster references from `OnValidate`.

**Phases:**

- [ ] Phase 1 — Broadcaster stub + Editor-gated broadcast.
- [ ] Phase 2 — EditMode fixture + compile-check.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.2.1 | ThemeBroadcaster stub MonoBehaviour | 1 | _pending_ | _pending_ | Create `Assets/Scripts/UI/Theme/ThemeBroadcaster.cs` — scene MonoBehaviour with `[SerializeField] UiTheme theme` + `BroadcastAll()` method. Discovery via `FindObjectsOfType<MonoBehaviour>()` in `Awake`, filter `IThemed`, cache list. No per-frame scan. `IThemed` interface stub lands here too (empty `void ApplyTheme(UiTheme)` — real impls in Step 2). |
| T1.2.2 | UiTheme.OnValidate Editor gate | 1 | _pending_ | _pending_ | Add `OnValidate` to `UiTheme.cs` under `#if UNITY_EDITOR` guard. Find active `ThemeBroadcaster` via `UnityEngine.Object.FindObjectsOfType<ThemeBroadcaster>()` (Editor-only — no Player impact). Call `BroadcastAll()` on each. No-op if none found. Wrap every broadcast ref in `#if UNITY_EDITOR` so Player build is untouched. |
| T1.2.3 | EditMode test — OnValidate fires broadcast | 2 | _pending_ | _pending_ | New `Assets/Tests/EditMode/UI/UiTheme_OnValidateTests.cs`: load test `UiTheme` + scene `ThemeBroadcaster` + one mock `IThemed`. Assert `ApplyTheme` called once after `EditorUtility.SetDirty` + `OnValidate` invocation. Covers Review Note D (Editor gate). |
| T1.2.4 | Compile-check + Player path | 2 | _pending_ | _pending_ | Run `npm run unity:compile-check` for Editor. Verify `#if !UNITY_EDITOR` compile path (Player build) has zero references to `ThemeBroadcaster` from `UiTheme.OnValidate`. Add a trivial `UNITY_EDITOR == false` conditional compile test in a smoke fixture if needed. Update `ia/specs/ui-design-system.md` §1.5 note that Editor-only broadcast exists. |

---

### Step 2 — ThemedPrimitive ring

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 2):** 0 filed

**Objectives:** Author 10 structural primitives on top of Step 1 tokens. Contracts + base class first, then two batches of 5 implementations each, then broadcaster wiring via new `UIManager.ThemeBroadcast.cs` partial. Every primitive implements `IThemed` → repaint on token swap. Foundation for StudioControl ring (Step 3) which extends `ThemedPrimitiveBase`.

**Exit criteria:**

- `IThemed` interface lives under `Assets/Scripts/UI/Primitives/IThemed.cs` with `void ApplyTheme(UiTheme)`.
- `ThemedPrimitiveBase : MonoBehaviour, IThemed` — `Awake`-cached `UiTheme` ref + `FindObjectOfType` fallback (invariant #3 compliant — never in `Update`).
- 10 concrete primitives land: `ThemedPanel`, `ThemedButton`, `ThemedTabBar`, `ThemedTooltip`, `ThemedSlider`, `ThemedToggle`, `ThemedLabel`, `ThemedList`, `ThemedIcon`, `ThemedOverlayToggleRow`.
- `ThemeBroadcaster` (Stage 1.2 stub) promoted to full impl with `[SerializeField]` reference from `UIManager`.
- `UIManager.ThemeBroadcast.cs` new partial carries the single `Start`-time `themeBroadcaster.BroadcastAll()` call. Zero edits to existing `UIManager.*.cs` partials.
- EditMode test coverage — token swap repaints all primitives in test scene; allocation-free `ApplyTheme` paths.
- Glossary rows added: `Themed primitive`, `IThemed contract`, `ThemeBroadcaster`.

**Art:** None. Code + interface + prefab test scene only.

**Relevant surfaces (load when step opens):**
- `docs/ui-polish-exploration.md` §Design Expansion → Architecture (Primitive ring) + Subsystem Impact (rows 2–3: `UIManager` partial + `ThemedPrimitive` family).
- Step 1 outputs: `UiTheme.cs` extended schema, `DefaultUiTheme.asset` defaults, stub `ThemeBroadcaster.cs` + `IThemed.cs`.
- `Assets/Scripts/Managers/GameManagers/UIManager.cs` + `UIManager.*.cs` partials — read only; do NOT edit. New partial file location: `Assets/Scripts/Managers/GameManagers/UIManager.ThemeBroadcast.cs` (new).
- `ia/specs/ui-design-system.md` §2 (Components) — normative primitive list.
- `ia/rules/invariants.md` #3 (cache refs in `Awake`), #4 (no singletons), #6 (no `UIManager` bloat — new partial file only).

#### Stage 2.1 — IThemed + ThemedPrimitiveBase

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Finalize `IThemed` contract + `ThemedPrimitiveBase` with `Awake`-cached theme resolution. Covers invariant #3 + #4 foundation pattern every downstream primitive inherits.

**Exit:**

- `Assets/Scripts/UI/Primitives/IThemed.cs` — full interface with `void ApplyTheme(UiTheme)` + XML doc.
- `Assets/Scripts/UI/Primitives/ThemedPrimitiveBase.cs` — `abstract class ThemedPrimitiveBase : MonoBehaviour, IThemed` with `[SerializeField] protected UiTheme theme` + `Awake`-cached `FindObjectOfType<UiTheme>()` fallback + `ApplyTheme(theme)` call.
- EditMode coverage — base class `Awake` binding path + fallback path.

**Phases:**

- [ ] Phase 1 — Contract + base class.
- [ ] Phase 2 — Base class tests + glossary row.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | IThemed interface (finalize) | 1 | _pending_ | _pending_ | Promote Stage 1.2 stub `IThemed.cs` to final form. Add XML doc citing token swap lifecycle + `Awake`-cache rule + Editor `OnValidate` entry path. Put under `Assets/Scripts/UI/Primitives/IThemed.cs`. Move stub from Stage 1.2 if it landed elsewhere. |
| T2.1.2 | ThemedPrimitiveBase MonoBehaviour | 1 | _pending_ | _pending_ | `Assets/Scripts/UI/Primitives/ThemedPrimitiveBase.cs` — `abstract class ThemedPrimitiveBase : MonoBehaviour, IThemed`. `[SerializeField] protected UiTheme theme`. `Awake`: if `theme == null` → `theme = FindObjectOfType<UiTheme>()` once; call `ApplyTheme(theme)`. `ApplyTheme` abstract. Invariant #3 rationale in XML doc: no per-frame scan. |
| T2.1.3 | EditMode tests for base + fallback | 2 | _pending_ | _pending_ | `Assets/Tests/EditMode/UI/ThemedPrimitiveBaseTests.cs`: two fixtures — (a) `theme` serialized → `Awake` calls `ApplyTheme` once with serialized ref; (b) `theme` null → `FindObjectOfType` fallback resolves before `ApplyTheme`. Mock subclass captures call count. |
| T2.1.4 | Glossary rows (primitive contracts) | 2 | _pending_ | _pending_ | Add to `ia/specs/glossary.md`: `Themed primitive` (MonoBehaviour under `Assets/Scripts/UI/Primitives/*` implementing `IThemed`), `IThemed contract` (single-method repaint interface), `ThemedPrimitiveBase` (abstract base with `Awake`-cached theme). |

#### Stage 2.2 — Primitives batch A (Panel / Button / Label / Icon / Tooltip)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship first 5 structural primitives — layout + typography + iconography focused. Each inherits `ThemedPrimitiveBase`, reads tokens in `ApplyTheme`, writes to uGUI `Image` / `TextMeshProUGUI` / child references. Allocation-free repaint path.

**Exit:**

- `ThemedPanel.cs` — `Image.color = theme.surfaceBase`; optional `ShadowDepth` slot (wired in Step 4).
- `ThemedButton.cs` — background + label color + hover / pressed variants from token accent ladder.
- `ThemedLabel.cs` — font / size / color from `UiTheme` typography fields.
- `ThemedIcon.cs` — tint from token accent ladder; sprite ref external.
- `ThemedTooltip.cs` — panel + label composition; delay / fade duration from `theme.motion.alertPulse` or dedicated tooltip entry.
- All under `Assets/Scripts/UI/Primitives/`.

**Phases:**

- [ ] Phase 1 — Layout + typography primitives (Panel / Label / Icon).
- [ ] Phase 2 — Interactive + composite (Button / Tooltip).

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | ThemedPanel | 1 | _pending_ | _pending_ | `Assets/Scripts/UI/Primitives/ThemedPanel.cs` — `class ThemedPanel : ThemedPrimitiveBase`. Requires `Image` component. `ApplyTheme`: `image.color = theme.surfaceBase`; `image.sprite = theme.panelBackgroundSprite` (if field exists; else skip). Optional `[SerializeField] PanelElevation elevation` enum → maps to `theme.studioRack.shadowDepthStops[idx]`. |
| T2.2.2 | ThemedLabel | 1 | _pending_ | _pending_ | `ThemedLabel.cs` — wraps `TextMeshProUGUI`. `ApplyTheme`: `text.font = theme.primaryFont`; `text.color = theme.textPrimary`; `text.fontSize = theme.typography.bodySize` (or expose `LabelTier` enum mapping to h1/h2/body/caption sizes). |
| T2.2.3 | ThemedIcon | 1 | _pending_ | _pending_ | `ThemedIcon.cs` — wraps `Image` sized via token grid (`theme.iconGrid`). `ApplyTheme`: `image.color = theme.accentPrimary` (or enum-selected accent tier). Sprite ref Inspector-set per instance. |
| T2.2.4 | ThemedButton | 2 | _pending_ | _pending_ | `ThemedButton.cs` — requires `Button` + background `Image` + `ThemedLabel` child. `ApplyTheme`: sets base + hover + pressed + disabled color transitions on `Button.colors` from accent ladder. Hover pulse amplitude reads `theme.motion.alertPulse` (juice wires in Step 4). |
| T2.2.5 | ThemedTooltip | 2 | _pending_ | _pending_ | `ThemedTooltip.cs` — composite: `ThemedPanel` + `ThemedLabel`. `ApplyTheme` cascades to children (cached `Awake`). Fade in/out duration reads `theme.motion` dedicated tooltip entry (add to `MotionBlock` in Stage 1.1 if not present — cross-check before filing). |

#### Stage 2.3 — Primitives batch B (Tab / Slider / Toggle / List / OverlayToggleRow) + broadcaster wiring

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship remaining 5 structural primitives + promote `ThemeBroadcaster` stub to full impl + land `UIManager.ThemeBroadcast.cs` partial. Token-swap end-to-end test lives here.

**Exit:**

- `ThemedTabBar`, `ThemedSlider`, `ThemedToggle`, `ThemedList`, `ThemedOverlayToggleRow` shipped under `Assets/Scripts/UI/Primitives/`.
- `ThemeBroadcaster.cs` full impl — `Awake` scans `FindObjectsOfType<MonoBehaviour>()`, filters `IThemed`, caches list; `BroadcastAll()` iterates cached list; additional subscribers registered via `Register(IThemed)` for runtime spawns.
- `UIManager.ThemeBroadcast.cs` partial — `[SerializeField] private ThemeBroadcaster themeBroadcaster` + `partial void Start_ThemeBroadcast()` hook + single `themeBroadcaster.BroadcastAll()` call. Invoked from existing `UIManager.Start` via partial method pattern; no body changes to other partials.
- EditMode test: scene w/ 10 primitives + `ThemeBroadcaster`; token field edit → all 10 repaint (assert `ApplyTheme` called on each).

**Phases:**

- [ ] Phase 1 — Batch B primitives.
- [ ] Phase 2 — Broadcaster full impl + UIManager partial.
- [ ] Phase 3 — Integration test + glossary.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.3.1 | ThemedTabBar + ThemedList | 1 | _pending_ | _pending_ | `ThemedTabBar.cs` — horizontal tab row; each tab is a `ThemedButton` variant with selected/unselected color states. `ThemedList.cs` — vertical list container; row spacing from `theme.spacing.listRow`; alternate row bg from `theme.surfaceAlt`. Both `ApplyTheme` cascade to children. |
| T2.3.2 | ThemedSlider + ThemedToggle | 1 | _pending_ | _pending_ | `ThemedSlider.cs` — wraps uGUI `Slider`; track / fill / handle colors from accent ladder. `ThemedToggle.cs` — wraps `Toggle`; on/off colors + checkmark icon tint from token accent. Both `ApplyTheme` writes to child `Image.color` fields cached `Awake`. |
| T2.3.3 | ThemedOverlayToggleRow | 1 | _pending_ | _pending_ | `ThemedOverlayToggleRow.cs` — composite for Bucket 2 signal overlays (pollution / crime / traffic / etc.). Holds `ThemedIcon` + `ThemedLabel` + a small active-state indicator slot (filled by `LED` in Step 3). `ApplyTheme` cascades to named children. One instance per signal. |
| T2.3.4 | ThemeBroadcaster full impl | 2 | _pending_ | _pending_ | Promote Stage 1.2 stub `ThemeBroadcaster.cs` to full runtime impl. `Awake`: single scan `FindObjectsOfType<MonoBehaviour>()` → filter `IThemed` → cache `List<IThemed>`. Expose `Register(IThemed)` / `Unregister(IThemed)` for runtime spawns. `BroadcastAll()` foreach cached list → `ApplyTheme(theme)`. Invariant #3 compliant — scan only in `Awake` + explicit `Register`. |
| T2.3.5 | UIManager.ThemeBroadcast partial | 2 | _pending_ | _pending_ | New file `Assets/Scripts/Managers/GameManagers/UIManager.ThemeBroadcast.cs`. `partial class UIManager` with `[SerializeField] private ThemeBroadcaster themeBroadcaster;` + `private void Start_ThemeBroadcast() { themeBroadcaster?.BroadcastAll(); }`. Wire the call from existing `UIManager.cs` `Start` via a single added line (the only edit to an existing partial; document the one-line exemption in commit). |
| T2.3.6 | Token-swap integration test + glossary | 3 | _pending_ | _pending_ | EditMode / PlayMode test scene with all 10 primitives + `ThemeBroadcaster`. Token edit → `BroadcastAll()` → assert `ApplyTheme` called once per primitive. Assert allocation-free repaint via `GC.GetTotalMemory` delta check. Glossary row: `ThemeBroadcaster` (scene MonoBehaviour, `Awake`-cached `IThemed` list, invariant #3 + #4 compliant). |

---

### Step 3 — StudioControl ring

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 3):** 0 filed

**Objectives:** Author 8 studio-rack interactives on top of Step 2 primitives + Step 1 tokens. Contracts + base first, then 4 simple widgets (LED / IlluminatedButton / SegmentedReadout / DetentRing), then 4 complex widgets (Knob / Fader / VUMeter / Oscilloscope) w/ animation state + prefabs + tests. Each implements `IStudioControl` → external code binds a signal source (`Func<float>`) once; widget `Update` reads cached delegate only (no allocations, invariant #3 compliant).

**Exit criteria:**

- `IStudioControl` interface under `Assets/Scripts/UI/StudioControls/IStudioControl.cs` — `float Value`, `Vector2 Range`, `AnimationCurve ValueToDisplay`, `BindSignalSource(Func<float>)`, `Unbind()`.
- `StudioControlBase : ThemedPrimitiveBase, IStudioControl` — caches `Func<float>` source; `Update` reads once per frame into `Value`.
- 8 implementations under `Assets/Scripts/UI/StudioControls/`: `Knob`, `Fader`, `LED`, `VUMeter`, `Oscilloscope`, `IlluminatedButton`, `SegmentedReadout`, `DetentRing`.
- Prefabs under `Assets/UI/Prefabs/StudioControls/*` — one per widget, pre-wired to primitive children + `StudioControlBase` serialized fields.
- PlayMode test coverage — bind source → `Update` reads it → visual state transitions without GC alloc (`GC.GetTotalMemory` before/after 1000 frames).
- Glossary rows: `StudioControl primitive`, `IStudioControl contract`, `Knob`, `Fader`, `VU meter`, `Oscilloscope`, `Illuminated button`, `Segmented readout`, `Detent ring`, `LED`.

**Art:** Prefab authoring only — no new sprite assets (consumes existing / Bucket 5 outputs via sprite refs on primitives).

**Relevant surfaces (load when step opens):**
- `docs/ui-polish-exploration.md` §Design Expansion → Architecture (StudioControl family) + Examples 1–2 (VU meter binding, Knob with detent + LED).
- Step 1 outputs: `UiTheme.studioRack` + `UiTheme.motion`.
- Step 2 outputs: `ThemedPrimitiveBase`, `IThemed`, `ThemeBroadcaster`.
- `ia/rules/invariants.md` #3 (cache `Func<float>` source in `Awake` / `OnEnable`; `Update` reads only).

#### Stage 3.1 — IStudioControl + StudioControlBase + contract tests

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Lock the signal-binding contract + base class every widget inherits. Prove allocation-free `Update` path before any widget ships.

**Exit:**

- `IStudioControl.cs` final signature (per exploration §CityStats handoff table).
- `StudioControlBase.cs` — inherits `ThemedPrimitiveBase`; caches `Func<float> _source`; `Update` reads `_source?.Invoke() ?? 0f` into `Value` then calls abstract `RenderValue(float)`.
- PlayMode test — bind source → 1000 `Update` frames → `GC.Alloc` delta == 0.

**Phases:**

- [ ] Phase 1 — Contract + base class.
- [ ] Phase 2 — Alloc-free PlayMode fixture.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.1.1 | IStudioControl interface | 1 | _pending_ | _pending_ | `Assets/Scripts/UI/StudioControls/IStudioControl.cs` — `float Value`, `Vector2 Range`, `AnimationCurve ValueToDisplay`, `void BindSignalSource(Func<float>)`, `void Unbind()`. XML doc: bind once in `Awake` / `OnEnable`; never rebind per frame. Matches exploration §CityStats handoff table row 3. |
| T3.1.2 | StudioControlBase abstract class | 1 | _pending_ | _pending_ | `StudioControlBase.cs` — `abstract class StudioControlBase : ThemedPrimitiveBase, IStudioControl`. Fields: `protected Func<float> _source; public float Value { get; private set; }; public Vector2 Range; public AnimationCurve ValueToDisplay;`. `Update`: if `_source != null` → `Value = _source.Invoke()` → call `protected abstract void RenderValue(float normalized)` with `Mathf.InverseLerp(Range.x, Range.y, Value)`. |
| T3.1.3 | Alloc-free PlayMode fixture | 2 | _pending_ | _pending_ | `Assets/Tests/PlayMode/UI/StudioControlAllocTests.cs`: mock widget subclass; bind `() => Time.time`; run 1000 frames via `yield return null`; assert `GC.GetTotalMemory(false)` delta < 64 bytes (headroom for Unity internals). Covers Review Note C concern — baseline for JuiceLayer tuning in Step 4. |
| T3.1.4 | Glossary rows (studio contracts) | 2 | _pending_ | _pending_ | Add to `ia/specs/glossary.md`: `StudioControl primitive` (interactive MonoBehaviour under `Assets/Scripts/UI/StudioControls/*` implementing `IStudioControl` + `IThemed`), `IStudioControl contract` (value / range / curve / bind-source interface), `Signal source binding` (Awake-cached `Func<float>` read each `Update` without alloc). |

#### Stage 3.2 — Simple widgets (LED / IlluminatedButton / SegmentedReadout / DetentRing)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship 4 simple widgets — no ring-buffer / needle-ballistics state; direct value → visual mapping. Exercises `StudioControlBase` contract in simplest form.

**Exit:**

- `LED.cs` — hue + on/off + pulse amplitude from tokens; `RenderValue(normalized)` sets `Image.color` intensity.
- `IlluminatedButton.cs` — `ThemedButton` + `LED` child; click handler raises event; LED state driven by bound source.
- `SegmentedReadout.cs` — 7-segment style numeric display; renders `Value` formatted per `DisplayFormat` enum (int / currency / time).
- `DetentRing.cs` — non-interactive indicator dots around a `Knob` socket; `RenderValue` lights nearest dot.
- Prefabs per widget under `Assets/UI/Prefabs/StudioControls/`.

**Phases:**

- [ ] Phase 1 — LED + IlluminatedButton.
- [ ] Phase 2 — SegmentedReadout + DetentRing + prefabs.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.2.1 | LED widget | 1 | _pending_ | _pending_ | `Assets/Scripts/UI/StudioControls/LED.cs` — `class LED : StudioControlBase`. `[SerializeField] int hueIndex` → picks from `theme.studioRack.ledHues`. `RenderValue(n)`: sets child `Image.color = baseHue * n` + optional glow pulse amplitude scaled by `n`. `ApplyTheme` re-reads hue. |
| T3.2.2 | IlluminatedButton widget | 1 | _pending_ | _pending_ | `IlluminatedButton.cs` — composite: `ThemedButton` + child `LED`. Exposes `UnityEvent OnPress`. Bound source drives LED brightness; click raises event + pulses LED via `PulseOnEvent` helper (wires in Step 4 — optional `ISfxEmitter` hook exposed but not wired — Review Note E). |
| T3.2.3 | SegmentedReadout widget | 2 | _pending_ | _pending_ | `SegmentedReadout.cs` — `[SerializeField] DisplayFormat format` enum (Int / Currency / Time / Percent). `RenderValue(n)`: formats `Value` per enum, writes to `TextMeshProUGUI` child w/ segmented-display font. Char width from `theme.typography.monoWidth`. |
| T3.2.4 | DetentRing widget + prefabs | 2 | _pending_ | _pending_ | `DetentRing.cs` — non-interactive; `[SerializeField] int detentCount`. `RenderValue(n)`: picks `index = Mathf.RoundToInt(n * (detentCount - 1))`, sets child `Image[]` colors (active = `theme.studioRack.knobDetentColor`, inactive = dim). Author 4 prefabs under `Assets/UI/Prefabs/StudioControls/` — one per widget landed in this stage. |

#### Stage 3.3 — Complex widgets (Knob / Fader / VUMeter / Oscilloscope) + tests

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship 4 complex widgets with animation state (drag handling / needle ballistics / ring-buffer traces). All alloc-free `Update`. PlayMode tests cover value binding + visual state + GC delta.

**Exit:**

- `Knob.cs` — drag-to-rotate handler; snaps to detents if `DetentRing` child present; exposes `OnValueChanged` event for external bindings (e.g. tax-rate knob per exploration Example 2).
- `Fader.cs` — vertical track + cap; drag changes `Value`; optional level-meter strip alongside.
- `VUMeter.cs` — needle + gradient strip w/ attack/release/peak-hold ballistics from `theme.motion.needleAttack/Release`.
- `Oscilloscope.cs` — rolling ring-buffer trace driven by bound source; buffer size configurable; redraw via `LineRenderer` or cached `Mesh`.
- Prefabs per widget under `Assets/UI/Prefabs/StudioControls/`.
- PlayMode tests — bind source, drive value curve, assert visual state + `GC.Alloc` delta == 0 across 1000 frames.

**Phases:**

- [ ] Phase 1 — Knob + Fader (drag-interactive).
- [ ] Phase 2 — VUMeter + Oscilloscope (animation state).
- [ ] Phase 3 — Prefabs + PlayMode test suite.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.3.1 | Knob widget | 1 | _pending_ | _pending_ | `Knob.cs` — `class Knob : StudioControlBase, IDragHandler, IPointerDownHandler`. Drag delta maps to `Value` via `ValueToDisplay` curve. Optional `[SerializeField] DetentRing detents` child; if set, `Value` snaps to nearest detent step. `UnityEvent<float> OnValueChanged`. `RenderValue(n)`: rotates indicator child `transform.localRotation`. Covers exploration Example 2. |
| T3.3.2 | Fader widget | 1 | _pending_ | _pending_ | `Fader.cs` — vertical `Slider`-like drag. `[SerializeField] RectTransform cap` + `RectTransform track`. Drag updates `Value`; `RenderValue(n)` moves cap along track. Track gradient from `theme.studioRack.faderTrackGradient`. Optional adjacent `LED` strip bound to same value for level-meter appearance. |
| T3.3.3 | VUMeter widget | 2 | _pending_ | _pending_ | `VUMeter.cs` — needle + gradient strip. State: `_displayed`, `_peakHold`, `_peakHoldTimer`. `Update`: reads `_source` → target; displaced via attack/release from `theme.motion.needleAttack/Release` (exponential smoothing); peak-hold pin decays after `peakHoldDuration`. `RenderValue` rotates needle + repaints gradient mask. Covers exploration Example 1. |
| T3.3.4 | Oscilloscope widget | 2 | _pending_ | _pending_ | `Oscilloscope.cs` — `[SerializeField] int bufferSize = 256`. Ring buffer `float[bufferSize]` allocated once in `Awake`. `Update`: writes `Value` at `_head` index → increments. `RenderValue` updates `LineRenderer.SetPositions` via cached `Vector3[]` — zero alloc. Trace color from `theme.studioRack.oscilloscopeTrace` + glow. |
| T3.3.5 | Prefabs (4 complex widgets) | 3 | _pending_ | _pending_ | Author prefabs under `Assets/UI/Prefabs/StudioControls/`: `Knob.prefab`, `Fader.prefab`, `VUMeter.prefab`, `Oscilloscope.prefab`. Each pre-wires `StudioControlBase` serialized fields (theme, range, curve) + primitive children (panel bg, label, indicator sprites). |
| T3.3.6 | PlayMode tests + widget glossary | 3 | _pending_ | _pending_ | `Assets/Tests/PlayMode/UI/StudioControlWidgetsTests.cs`: per widget — bind `() => Mathf.Sin(Time.time)`; run 1000 frames; assert `GC.Alloc` delta == 0; assert indicator child moves / rotates per expected curve. Add glossary rows: `Knob`, `Fader`, `VU meter`, `Oscilloscope`, `Illuminated button`, `Segmented readout`, `Detent ring`, `LED`. |

---

### Step 4 — JuiceLayer ring

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 4):** 0 filed

**Objectives:** Ship the shared `JuiceLayer` scene MonoBehaviour + 6 helpers (`TweenCounter`, `PulseOnEvent`, `SparkleBurst`, `ShadowDepth`, `NeedleBallistics`, `OscilloscopeSweep`). Every helper reads parameters from `UiTheme.motion` — token swap retunes motion globally. Pool tween state + reuse particle buffers so hot-loop `Update` paths are allocation-free. Review Note C flagged JuiceLayer allocation budget — profiler verification runs in `/verify-loop` on closeout.

**Exit criteria:**

- `JuiceLayer.cs` under `Assets/Scripts/UI/Juice/` — scene MonoBehaviour; Inspector-wired from `UIManager`; `FindObjectOfType<JuiceLayer>()` fallback for primitives resolving it in `Awake`.
- 6 helpers under `Assets/Scripts/UI/Juice/Helpers/` — `TweenCounter`, `PulseOnEvent`, `SparkleBurst`, `ShadowDepth`, `NeedleBallistics`, `OscilloscopeSweep`. Some are value-type structs / poolable classes, not MonoBehaviours.
- All helpers consume `UiTheme.motion` entries — no hard-coded durations / easings.
- PlayMode alloc-free coverage — each helper runs 1000 frames with `GC.Alloc` delta == 0.
- Glossary rows: `Juice layer`, `Tween counter`, `Pulse on event`, `Sparkle burst`, `Shadow depth`, `Needle ballistics`, `Oscilloscope sweep`.

**Art:** One particle material + atlas for `SparkleBurst` (lives under `Assets/UI/Effects/` — coordinate sprite source with Bucket 5 sprite-gen; fallback to placeholder star sprite).

**Relevant surfaces (load when step opens):**
- `docs/ui-polish-exploration.md` §Design Expansion → Architecture (Juice ring) + Examples 3–4 (theme swap repaint, animated money counter).
- Step 1 outputs: `UiTheme.motion`.
- Step 2 outputs: `ThemedPrimitiveBase` (helpers slot onto primitives as serialized fields).
- Step 3 outputs: `StudioControlBase` (`NeedleBallistics` + `OscilloscopeSweep` consumed by VUMeter + Oscilloscope widgets — retrofit after Step 3 if widget Stage 3.3 shipped stand-in inline logic).
- `ia/rules/invariants.md` #3 (alloc-free `Update`), #4 (`JuiceLayer` scene component, not singleton).

#### Stage 4.1 — JuiceLayer + helpers batch A (TweenCounter / PulseOnEvent / ShadowDepth)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Scene-root `JuiceLayer` + first 3 helpers landing numeric tween + event pulse + elevation. Prove pooling pattern once before remaining helpers.

**Exit:**

- `JuiceLayer.cs` scene component with `[SerializeField] UiTheme theme` + helper pool references + `Awake` fallback lookup. No singleton.
- `TweenCounter` — poolable struct/class; `Animate(from, to, motionEntry, callback)` reads `theme.motion.moneyTick`; updated via `JuiceLayer.Update` central tick loop.
- `PulseOnEvent` — MonoBehaviour slot on any primitive; `Trigger()` applies scale + glow pulse over `theme.motion.alertPulse`.
- `ShadowDepth` — `MonoBehaviour` on `ThemedPanel`; renders shadow sprite offset + alpha per `theme.studioRack.shadowDepthStops[tier]`.

**Phases:**

- [ ] Phase 1 — JuiceLayer scene MonoBehaviour + central tick loop.
- [ ] Phase 2 — Helpers A (TweenCounter / PulseOnEvent / ShadowDepth).

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.1.1 | JuiceLayer scene MonoBehaviour | 1 | _pending_ | _pending_ | `Assets/Scripts/UI/Juice/JuiceLayer.cs` — `class JuiceLayer : MonoBehaviour`. Inspector field `[SerializeField] UiTheme theme` + `Awake` fallback. Pool containers for active tweens (`List<TweenCounter> _activeTweens`). `Update` iterates active pools in-place (backward loop for removal). Primitives find it via `FindObjectOfType<JuiceLayer>()` in `Awake` only. Invariant #3 + #4 compliant. |
| T4.1.2 | TweenCounter helper | 2 | _pending_ | _pending_ | `Assets/Scripts/UI/Juice/Helpers/TweenCounter.cs` — value-type struct or poolable class. `Animate(from, to, MotionEntry entry, Action<float> onUpdate)` — pushed into `JuiceLayer._activeTweens`; ticked per frame. Easing curve from `entry.easing`; duration from `entry.durationSeconds`. Callback writes lerped value to caller (e.g. `SegmentedReadout.SetValue`). Covers Example 4. |
| T4.1.3 | PulseOnEvent + ShadowDepth | 2 | _pending_ | _pending_ | `PulseOnEvent.cs` MonoBehaviour — slot on any primitive; `Trigger()` pushes scale + glow animation into `JuiceLayer` tween pool reading `theme.motion.alertPulse`. `ShadowDepth.cs` MonoBehaviour — paired with `ThemedPanel`; reads `[SerializeField] int tier` → indexes `theme.studioRack.shadowDepthStops[tier]` → offsets child shadow sprite + sets alpha. Both `IThemed` so token swap retunes. |

#### Stage 4.2 — Helpers batch B (SparkleBurst / NeedleBallistics / OscilloscopeSweep) + alloc tests

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Remaining 3 helpers + allocation-free verification suite + retrofit Step 3 widgets (VUMeter / Oscilloscope) to consume shared helpers instead of inlined logic.

**Exit:**

- `SparkleBurst` — particle burst helper using pooled `ParticleSystem` + token palette.
- `NeedleBallistics` — struct helper consumed by `VUMeter`; replaces inline state from Stage 3.3.
- `OscilloscopeSweep` — ring-buffer helper consumed by `Oscilloscope`; replaces inline buffer.
- VUMeter + Oscilloscope retrofitted to use helpers (reduces Stage 3.3 debt).
- PlayMode test suite — 6 helpers each running 1000 frames `GC.Alloc` == 0. Covers Review Note C baseline for `/verify-loop` profiler validation.

**Phases:**

- [ ] Phase 1 — SparkleBurst w/ pooled particles.
- [ ] Phase 2 — NeedleBallistics + OscilloscopeSweep + widget retrofit.
- [ ] Phase 3 — Alloc-free suite + glossary.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.2.1 | SparkleBurst helper | 1 | _pending_ | _pending_ | `SparkleBurst.cs` MonoBehaviour — pooled `ParticleSystem` with color-over-lifetime from `theme.studioRack.sparklePalette`. `Burst(Vector2 position)` emits fixed particle count; duration from `theme.motion.sparkleDuration`. `ParticleSystem.Emit` uses `EmitParams` struct — no alloc. |
| T4.2.2 | NeedleBallistics struct | 2 | _pending_ | _pending_ | `NeedleBallistics.cs` — value-type struct w/ `_displayed`, `_peakHold`, `_peakTimer`. `Tick(float target, float dt, MotionEntry attack, MotionEntry release, float peakHoldSeconds) → float`. Called from `VUMeter.Update` — replaces inline state from T3.3.3. Returns interpolated needle position. |
| T4.2.3 | OscilloscopeSweep + widget retrofit | 2 | _pending_ | _pending_ | `OscilloscopeSweep.cs` — ring-buffer helper class; owns `float[bufferSize]` + `_head`. `Write(float sample)` + `CopyTo(Vector3[] positions)`. Retrofit `Oscilloscope.cs` (T3.3.4) to delegate to this helper. Refit `VUMeter.cs` (T3.3.3) to delegate to `NeedleBallistics` struct. |
| T4.2.4 | Alloc-free PlayMode suite | 3 | _pending_ | _pending_ | `Assets/Tests/PlayMode/UI/JuiceAllocTests.cs`: per helper — 1000 frames of activity (active tween, repeated pulse, continuous sparkle loop, needle sweep, oscilloscope write). Assert `GC.GetTotalMemory` delta < 64 bytes. Baseline captured for `/verify-loop` profiler validation on closeout. |
| T4.2.5 | Juice glossary rows | 3 | _pending_ | _pending_ | Add to `ia/specs/glossary.md`: `Juice layer` (scene MonoBehaviour hosting pooled tween / particle / pulse helpers), `Tween counter`, `Pulse on event`, `Sparkle burst`, `Shadow depth`, `Needle ballistics`, `Oscilloscope sweep`. Each row cites token contract + alloc-free guarantee. |

---

### Step 5 — Flagship HUD + Toolbar + overlay polish

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 5):** 0 filed

**Objectives:** Migrate existing HUD row widgets (money / pop / date / happiness / speed / scale) + toolbar rows + overlay toggle row to the three new rings. Studio-rack treatment lands here. Resolves BUG-14 (per-frame `FindObjectOfType`) and TECH-72 (HUD / uGUI scene hygiene) as part of prefab catalog migration. This is the first user-visible slice — tester sees the HUD get studio-rack aesthetic + alive motion.

**Exit criteria:**

- Main HUD bar rebuilt as `ThemedPanel` + StudioControl children: money = `SegmentedReadout` + `TweenCounter`; happiness = `VUMeter` + `NeedleBallistics`; speed = `IlluminatedButton` cluster; scale = `LED` row; date = `ThemedLabel` + `SegmentedReadout`; HUD bg = `ThemedPanel` + `ShadowDepth`.
- Toolbar rebuilt as `ThemedPanel` + `IlluminatedButton` clusters; tool-select pulse via `PulseOnEvent`; background depth via `ShadowDepth`.
- Overlay toggle row → `ThemedOverlayToggleRow` per Bucket 2 signal (pollution / crime / traffic / happiness / zone / desirability) with `LED` active-state. Signal scalars bound via `IStudioControl.BindSignalSource` in `HUDController.Awake` (or equivalent existing UI bootstrap point).
- BUG-14 resolved — all affected UI controllers cache refs in `Awake`; verified by Grep for `FindObjectOfType` in `Update`.
- TECH-72 resolved — HUD / uGUI scene hygiene cleanup as part of prefab migration.
- `npm run unity:compile-check` + `npm run validate:all` green.

**Art:** Consumes existing sprite set + any Bucket 5 sprite-gen outputs available. No new authored art in this step.

**Relevant surfaces (load when step opens):**
- `docs/ui-polish-exploration.md` §Design Expansion → Implementation Points P4 + P5 + Example 1 (VU binding) + Example 4 (money counter).
- Step 1 + 2 + 3 + 4 outputs (full stack — tokens + primitives + controls + juice).
- `Assets/Scripts/Managers/GameManagers/UIManager.Hud.cs` + `UIManager.Toolbar.cs` + `UIManager.ToolbarChrome.cs` — existing HUD / toolbar logic; migrate references, do NOT bloat partials (invariant #6).
- BUG-14 + TECH-72 backlog entries (open; `backlog_issue BUG-14`, `backlog_issue TECH-72`).
- `ia/rules/invariants.md` #3 (final verification sweep).

#### Stage 5.1 — HUD migration + BUG-14 + TECH-72

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Replace HUD row widgets with primitives + studio controls + juice. Resolve BUG-14 + TECH-72 in the same stage (natural scope — both touch HUD / uGUI controllers).

**Exit:**

- HUD row widgets (money / pop / date / happiness / speed / scale) migrated.
- Money readout animates transactions via `TweenCounter` + `SparkleBurst` (positive) / `PulseOnEvent` (negative).
- Happiness `VUMeter` bound to `CityStatsUIController` facade getter per exploration Example 1.
- BUG-14 fixed (Grep-verified: zero `FindObjectOfType` in `Update` across `Assets/Scripts/Managers/GameManagers/UIManager.*.cs` + new UI widget dirs).
- TECH-72 fixed (HUD / uGUI scene hierarchy cleaned; prefab catalog migration noted).

**Phases:**

- [ ] Phase 1 — Money + date + scale row migration.
- [ ] Phase 2 — Happiness + speed + HUD bg + BUG-14 sweep.
- [ ] Phase 3 — TECH-72 scene hygiene + integration test.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T5.1.1 | Money readout migration | 1 | _pending_ | _pending_ | Replace existing money HUD row widget with `SegmentedReadout` + `TweenCounter` binding. `EconomyManager.OnTransaction(delta)` → HUD listener calls `TweenCounter.Animate(old, new, theme.motion.moneyTick, v => readout.SetValue(v))`. On positive delta: `SparkleBurst.Burst(readoutPos)`. On negative: `PulseOnEvent.Trigger()` in `accentNegative`. Covers exploration Example 4. |
| T5.1.2 | Date + scale indicator migration | 1 | _pending_ | _pending_ | Date row → `ThemedLabel` + `SegmentedReadout` (day/month/year). Scale indicator → `LED` row with multi-hue (one LED per active scale: city / district / region — per multi-scale plan). Bound to existing scale manager facade. |
| T5.1.3 | Happiness VU + speed buttons | 2 | _pending_ | _pending_ | Happiness row → `VUMeter` prefab; bind `() => citystats.GetScalar("happiness.cityAverage")` in `HUDController.Awake`. Speed controls → 4 `IlluminatedButton` instances (pause / slow / normal / fast); active state driven by `TimeManager.speed` getter. Button press events wire to existing `TimeManager.SetSpeed`. |
| T5.1.4 | HUD panel bg + BUG-14 sweep | 2 | _pending_ | _pending_ | HUD bar root → `ThemedPanel` + `ShadowDepth` tier 2 elevation. BUG-14 sweep: Grep `FindObjectOfType` across `Assets/Scripts/Managers/GameManagers/UIManager.*.cs` + any HUD controller under `Assets/Scripts/UI/*`; every offending site → cache in `Awake` or move to serialized ref. Update BUG-14 notes with verification command. |
| T5.1.5 | TECH-72 scene hygiene | 3 | _pending_ | _pending_ | Clean HUD scene hierarchy per TECH-72 scope — prefab consolidation, RectTransform anchoring review, Canvas group organization. Migrate HUD row instances into prefab catalog under `Assets/UI/Prefabs/HUD/`. Update TECH-72 acceptance notes with before/after hierarchy depth + prefab count. |
| T5.1.6 | HUD PlayMode integration test | 3 | _pending_ | _pending_ | `Assets/Tests/PlayMode/UI/HudIntegrationTests.cs`: load scene with HUD + mock economy / citystats facades; trigger transaction → assert `SegmentedReadout` value tweens + sparkle/pulse fires. Assert `GC.Alloc` delta bounded across 10 transactions. Close BUG-14 + TECH-72 verification gate here. |

#### Stage 5.2 — Toolbar + overlay migration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Migrate toolbar rows + overlay toggle row. Bucket 2 signal scalars bound via `IStudioControl.BindSignalSource` into LED indicators. Tool-select pulse closes the flagship polish loop for Step 5.

**Exit:**

- Toolbar rows → `ThemedPanel` + `IlluminatedButton` clusters; tool-category switch raises `PulseOnEvent`.
- Overlay toggle row: one `ThemedOverlayToggleRow` per Bucket 2 signal (6 signals). Each row carries `ThemedIcon` + `ThemedLabel` + `LED` active-state indicator.
- `LED.BindSignalSource(() => overlayManager.GetSignalIntensity(signalKind))` wired in `ToolbarController.Awake`.
- Toolbar bg `ThemedPanel` + `ShadowDepth` per tier.
- `npm run validate:all` + `npm run unity:compile-check` green.

**Phases:**

- [ ] Phase 1 — Toolbar button migration + tool-select pulse.
- [ ] Phase 2 — Overlay toggle row + Bucket 2 signal binding.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T5.2.1 | Toolbar cluster migration | 1 | _pending_ | _pending_ | Existing toolbar zoning / service / transport buttons → `IlluminatedButton` clusters under `ThemedPanel` rows. Active-state LED bound to `ToolbarController.currentTool` matcher. Row bg `ThemedPanel` + `ShadowDepth` tier 1. |
| T5.2.2 | Tool-category pulse | 1 | _pending_ | _pending_ | On `ToolbarController.SetCategory(newCategory)` event → `PulseOnEvent.Trigger()` on newly-active cluster + inverse pulse on deactivating cluster. Duration from `theme.motion.alertPulse`. No per-frame scan; event-driven only. |
| T5.2.3 | Overlay toggle rows (6 signals) | 2 | _pending_ | _pending_ | Instantiate 6 `ThemedOverlayToggleRow` instances under toolbar overlay area — one per Bucket 2 signal (pollution / crime / traffic / happiness / zone / desirability). Each row: icon + label + `LED`. Toggle click → `OverlayRenderer.SetSignalEnabled(kind, bool)`. |
| T5.2.4 | LED signal-intensity binding | 2 | _pending_ | _pending_ | In `ToolbarController.Awake`, each overlay row's `LED` gets `BindSignalSource(() => overlayManager.GetSignalIntensity(kind))`. Verify zero per-frame alloc across 1000 frames. Confirms invariant #3 + completes flagship polish exit criteria. |

---

### Step 6 — CityStats handoff artifacts

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 6):** 0 filed

**Objectives:** Publish the handoff contract artifacts so CityStats overhaul bucket (FEAT-51) can consume the token + primitive + juice catalog cleanly. No layout / data wiring in this step — only spec sections, glossary rows, cross-plan references, and a handoff notify. Downstream consumer: `ia/projects/citystats-overhaul-master-plan.md` Dashboard step acceptance.

**Exit criteria:**

- `ia/specs/ui-design-system.md` gains a normative "Extended token catalog" anchor in §1 / §1.5 (referenced from CityStats).
- New handoff section in `docs/ui-polish-exploration.md` §CityStats handoff (already exists in exploration — confirm normative; add updated primitive + helper signatures if any drifted during implementation).
- `ia/projects/citystats-overhaul-master-plan.md` Dashboard step acceptance updated: reads "consumes UiTheme + StudioControl + JuiceLayer per `docs/ui-polish-exploration.md` §Design Expansion → CityStats handoff".
- Glossary rows for every term the Stats dashboard will consume, confirmed landed across Steps 1–5; gap audit + patch here.
- Handoff notify: bucket-close message seeded in `ia/projects/citystats-overhaul-master-plan.md` pointing at the contract table.

**Art:** None. Documentation + cross-plan edits only.

**Relevant surfaces (load when step opens):**
- `docs/ui-polish-exploration.md` §Design Expansion → CityStats handoff (contract table).
- `ia/projects/citystats-overhaul-master-plan.md` — Dashboard step (downstream consumer; FEAT-51 tracker).
- `ia/specs/ui-design-system.md` + `ia/specs/glossary.md`.
- Steps 1–5 outputs — full token + primitive + studio-control + juice-helper catalog.

#### Stage 6.1 — Spec publication + glossary gap audit

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Promote the token / primitive / juice catalog to normative status in `ui-design-system.md` §1–§2 + §1.5. Audit glossary for missing Stats-dashboard-consumable rows; patch gaps.

**Exit:**

- `ui-design-system.md` §1 (palette + tokens) + §1.5 (motion) + §2 (components) carry normative references to every shipped primitive + StudioControl + juice helper + token block.
- Glossary gap audit table captured in stage handoff (not persisted — validation only); any missing rows added.

**Phases:**

- [ ] Phase 1 — ui-design-system.md normative promotion.
- [ ] Phase 2 — Glossary audit + gap patch.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T6.1.1 | ui-design-system §1 + §1.5 promotion | 1 | _pending_ | _pending_ | Edit `ia/specs/ui-design-system.md` §1 — add "Extended token catalog" subsection listing every field in `UiTheme.studioRack` + `UiTheme.motion` with role description. §1.5 — motion block becomes normative (semantically-named entries + durations / curves). Cite back to `docs/ui-polish-exploration.md` as source of record. |
| T6.1.2 | ui-design-system §2 components | 1 | _pending_ | _pending_ | `ia/specs/ui-design-system.md` §2 extended with ThemedPrimitive family (10) + StudioControl family (8) + JuiceLayer + 6 helpers — each as a subsection with type name + file path + contract ref (`IThemed` / `IStudioControl`). Marks the catalog normative (CityStats must reference, not reinvent). |
| T6.1.3 | Glossary gap audit + patch | 2 | _pending_ | _pending_ | Cross-check terms in exploration §CityStats handoff table against `ia/specs/glossary.md`. Expected rows: token ring, studio-rack token, motion token, themed primitive, studio-control primitive, knob, fader, VU meter, oscilloscope, illuminated button, segmented readout, detent ring, LED, juice layer, tween counter, pulse on event, sparkle burst, shadow depth, needle ballistics, oscilloscope sweep, IThemed contract, IStudioControl contract, ThemeBroadcaster, signal source binding. Any missing → add in this task. |

#### Stage 6.2 — Cross-plan update + handoff notify

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Edit `citystats-overhaul-master-plan.md` Dashboard step acceptance to reference this bucket's catalog. Seed a handoff notify so CityStats team picks up the contract without re-deriving.

**Exit:**

- `ia/projects/citystats-overhaul-master-plan.md` Dashboard step acceptance reads: "consumes UiTheme + StudioControl + JuiceLayer per `docs/ui-polish-exploration.md` §Design Expansion → CityStats handoff".
- Handoff notify entry added to CityStats master plan Decision Log (or equivalent section) pointing at this plan + the contract table.
- FEAT-51 backlog entry `Notes` field updated referencing this handoff (via `backlog_issue FEAT-51` MCP tool → confirm ref).

**Phases:**

- [ ] Phase 1 — citystats-overhaul-master-plan update.
- [ ] Phase 2 — Handoff notify + FEAT-51 note.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T6.2.1 | citystats-overhaul Dashboard acceptance | 1 | _pending_ | _pending_ | Edit `ia/projects/citystats-overhaul-master-plan.md` — locate Dashboard step (or nearest equivalent section). Append to Exit criteria: "consumes UiTheme + StudioControl + JuiceLayer per `docs/ui-polish-exploration.md` §Design Expansion → CityStats handoff". Do NOT add new steps / stages / tasks to that master plan — acceptance bullet only. |
| T6.2.2 | citystats-overhaul Relevant surfaces | 1 | _pending_ | _pending_ | Add to `citystats-overhaul-master-plan.md` Dashboard step Relevant surfaces: `docs/ui-polish-exploration.md §Design Expansion → CityStats handoff`, `ia/specs/ui-design-system.md §2 Components`, `ia/projects/ui-polish-master-plan.md` (Steps 1–4 outputs). |
| T6.2.3 | FEAT-51 note + handoff notify | 2 | _pending_ | _pending_ | Via MCP `backlog_issue FEAT-51` → confirm open → add handoff note to the FEAT-51 `ia/backlog/FEAT-51.yaml` `notes` field (per `ia/skills/project-new/SKILL.md` update pattern) citing ui-polish catalog availability. Seed a handoff entry at the top of `citystats-overhaul-master-plan.md` Decision Log pointing at this plan + contract table. |

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `project-stage-close` runs.
- Run `claude-personal "/stage-file ia/projects/ui-polish-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/ui-polish-exploration.md` + downstream plans (citystats-overhaul).
- Respect ring order strictly — do NOT start Step 2 until Step 1 Final; do NOT start Step 3 until Step 2 Final; Step 4 may parallelize with Step 3 Stage 3.3 retrofit window (see Stage 4.2 note). Step 5 blocked on Steps 2+3+4 Final. Step 6 (handoff) can run after Step 4 Final — does not block on Step 5.
- Keep this orchestrator synced with any umbrella issue (full-game-mvp tracker row) — per `project-spec-close` skill umbrella-sync rule.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers `Status: Final`; the file stays.
- Silently promote functional-tier screens (info panels / settings / save-load / pause / onboarding / glossary / tooltips) into flagship juice scope — they stay primitives-only per Locked decisions. Studio-rack on those surfaces is post-MVP.
- Edit `UIManager.cs` or existing `UIManager.*.cs` partials beyond the single one-line `Start_ThemeBroadcast()` call from Stage 2.3 T2.3.5 — invariant #6.
- Add per-frame `FindObjectOfType` anywhere — invariant #3 enforced by BUG-14 verification in Stage 5.1 T5.1.4.
- Add new singletons — `JuiceLayer` + `ThemeBroadcaster` + `UiTheme` all scene / asset components; invariant #4.
- Wire Bucket 7 SFX hooks in this bucket — `ISfxEmitter` interface exposed but intentionally unwired (Review Note E). Bucket 7 owns wiring.
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.

---
