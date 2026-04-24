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

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 1 Task specs (TECH-309..313) aligned w/ Stage block + §Plan Author + backlog yaml locator mirror. No fix tuples. Downstream: `/ship-stage` Pass 1 per task.
