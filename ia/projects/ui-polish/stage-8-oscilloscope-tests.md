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
