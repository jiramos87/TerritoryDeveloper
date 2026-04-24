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
