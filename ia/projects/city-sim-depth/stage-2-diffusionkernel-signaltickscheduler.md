### Stage 2 — Signal Layer Foundation / DiffusionKernel + SignalTickScheduler

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship the per-signal diffusion pass and the tick scheduler that orchestrates producer → diffusion → consumer flow per simulation tick. After this stage a seeded signal field diffuses and decays correctly.

**Exit:**

- `DiffusionKernel.Apply` passes Example 1 numeric assertions (±0.2 tolerance; no negative values anywhere in output).
- `SignalTickScheduler.Tick(TickContext)` called from `SimulationManager.ProcessSimulationTick` between `UrbanCentroidService.RecalculateFromGrid` and `AutoRoadBuilder.ProcessTick`.
- No `FindObjectOfType` in `Update` or per-frame loops — invariant #3 verified.
- EditMode DiffusionKernel test passes.
- Phase 1 — DiffusionKernel implementation + test.
- Phase 2 — SignalTickScheduler MonoBehaviour + SimulationManager wiring.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | DiffusionKernel core | _pending_ | _pending_ | `DiffusionKernel` static class — `Apply(SignalField field, float[,] sourceAccum, SignalMetadata meta)`: reads `sourceAccum`, blurs via separable horizontal + vertical Gaussian passes (kernel radius from `meta.diffusionRadius`), decays by `meta.decayPerStep`, writes back to `field`; clamp floor 0 on output; anisotropy scales H vs V pass sigma from `meta.anisotropy`. |
| T2.2 | DiffusionKernel EditMode test | _pending_ | _pending_ | EditMode test — 64×64 `SignalField`; 3 heavy-industry sources at (10,10),(11,10),(10,11) weight +4.0; 5-cell forest sink centered (15,15) weight −0.5; `DiffusionSpec{Radius=6, DecayPerStep=0.15f, Anisotropy=(0,0)}`; run `Apply`; assert (10,10)≈2.7 ±0.2, neighbors ≈1.8–2.2, (15,15)=0 (clamped); assert zero negative values. |
| T2.3 | SignalTickScheduler MonoBehaviour | _pending_ | _pending_ | `SignalTickScheduler` MonoBehaviour — `List<ISignalProducer>` + `List<ISignalConsumer>` resolved once in `Awake` via `FindObjectsOfType` (cached — not called in `Update`; invariant #3); `Tick(TickContext)` loop: clear source accum buffers → call all producers → `DiffusionKernel.Apply` per signal → call all consumers; `[SerializeField]` refs for `SignalFieldRegistry`, `SignalMetadataRegistry`. |
| T2.4 | SimulationManager tick wiring | _pending_ | _pending_ | Edit `SimulationManager.ProcessSimulationTick` (line 61, `SimulationManager.cs`) — insert `if (signalTickScheduler != null) signalTickScheduler.Tick(ctx)` after `urbanCentroidService.RecalculateFromGrid()` call (~line 74) and before `autoRoadBuilder.ProcessTick()` (~line 77). Add `[SerializeField] SignalTickScheduler signalTickScheduler` field + `FindObjectOfType` fallback in `Start`. |

---
