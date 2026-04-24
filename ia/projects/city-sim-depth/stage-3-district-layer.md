### Stage 3 — Signal Layer Foundation / District Layer

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship the district aggregation layer: `DistrictMap` auto-derived from `UrbanCentroidService` centroid+rings, `DistrictManager` MonoBehaviour, `DistrictAggregator` mean/P90 rollup, `DistrictSignalCache` read API (including Bucket 8 `GetAll` stub), and save/load round-trip for `DistrictMap`.

**Exit:**

- `DistrictMap` assigns each cell a district id by ring band from `UrbanCentroidService`.
- `DistrictSignalCache.Get(districtId, Crime)` returns P90; `Get(districtId, PollutionAir)` returns mean; empty district returns `NaN`.
- `DistrictMap` round-trips through `GameSaveManager` save/load without corruption.
- `npm run validate:all` passes.
- Phase 1 — District data model + DistrictManager.
- Phase 2 — DistrictAggregator + DistrictSignalCache + save/load.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | District + DistrictMap | _pending_ | _pending_ | `District` record (`int id`, `string name`) + `DistrictMap` (`int[,]` sized from `GridManager`); `Assign(x,y,districtId)`, `Get(x,y)`. Auto-derivation: district id = ring-band index from `UrbanCentroidService.GetRingBand(x,y)` (single-centroid MVP per locked decision). |
| T3.2 | DistrictManager MonoBehaviour | _pending_ | _pending_ | `DistrictManager` MonoBehaviour — owns `DistrictMap`; `RederiveFromCentroid()` called after `UrbanCentroidService.RecalculateFromGrid` each tick via `SignalTickScheduler` pre-pass; exposes `GetDistrictId(x,y)` + `DistrictCount`; `[SerializeField] UrbanCentroidService` + `FindObjectOfType` fallback; invariant #6 (new MonoBehaviour, not added to `GridManager`). |
| T3.3 | DistrictAggregator | _pending_ | _pending_ | `DistrictAggregator` — called by `SignalTickScheduler` after diffusion pass; for each signal, iterates all cells bucketing values by district id; computes mean or P90 per `SignalMetadataRegistry.rollupRule`; P90 impl: sort bucket, index at `(int)floor(n * 0.9)`; writes result to `DistrictSignalCache`; empty district (0 cells) → writes `float.NaN`. |
| T3.4 | DistrictSignalCache + save/load | _pending_ | _pending_ | `DistrictSignalCache` — `float[districtCount, signalCount]` table; `Get(districtId, signal)` returns float (`NaN` if empty); `GetAll(districtId)` returns `Dictionary<SimulationSignal, float>` (12 entries — Bucket 8 read-model facade stub). Add `districtMapData` serialization field to `GameSaveManager` save DTO (version bump); restore on load; migration: if absent in old save, set `needsRederive = true` (re-derive on first tick). |

---
