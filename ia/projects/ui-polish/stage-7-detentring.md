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
