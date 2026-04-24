### Stage 8 — New Simulation Signals / CrimeSystem

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `CrimeSystem` — `Crime` signal producer (density × low-service weight) + consumer (`ServicePolice` coverage reduces crime) + hotspot event emitter placeholder for Bucket 5.

**Exit:**

- `Crime` signal non-zero in high-density unpoliced zones after 10 ticks.
- `ServicePolice` coverage reduces crime in covered cells.
- `CrimeHotspotEvent(districtId, level)` emitted when P90 district crime > `crimeHotspotThreshold`.
- EditMode test: no-police district accumulates crime above threshold; police station drops below threshold.
- Phase 1 — CrimeSystem producer + ServicePolice consumer formula.
- Phase 2 — Hotspot event emitter + EditMode test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | CrimeSystem producer | _pending_ | _pending_ | `CrimeSystem` MonoBehaviour implements `ISignalProducer` — `Crime` source per cell = `zoneLevel * (1f - Mathf.Clamp01(ServicePolice.Get(x,y)))` (low police → high crime weight); diffusion radius=3; `[SerializeField] SignalFieldRegistry` + `FindObjectOfType` (invariants #3, #4). |
| T8.2 | ServicePolice crime consumer | _pending_ | _pending_ | Extend `CrimeSystem` to also implement `ISignalConsumer` — `ConsumeSignals` reads `ServicePolice` field post-diffusion; for cells where `ServicePolice.Get(x,y) > 0`, applies post-diffusion crime reduction multiplier `(1f - Mathf.Clamp01(policeValue * 0.5f))`; writes multiplied value back to `Crime` field in place. |
| T8.3 | CrimeHotspot event emitter | _pending_ | _pending_ | After each `CrimeSystem.ConsumeSignals`, iterate `DistrictSignalCache` P90 Crime per district; if P90 > `[SerializeField] float crimeHotspotThreshold = 2.5f`, emit `CrimeHotspotEvent { districtId, level }` via `GameNotificationManager`; Bucket 5 animation placeholder — event registered only, no animation instantiation. |
| T8.4 | CrimeSystem EditMode test | _pending_ | _pending_ | EditMode test — 10-cell high-density R zone, no police; run 30 ticks; assert `DistrictSignalCache.Get(0, Crime)` P90 > 2.5f. Second fixture: add police station within radius 3; run 30 ticks; assert P90 < 2.5f. |

---
