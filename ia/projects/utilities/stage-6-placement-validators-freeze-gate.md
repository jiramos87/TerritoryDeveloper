### Stage 6 — Infrastructure buildings + terrain-sensitive placement / Placement validators + freeze gate

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Terrain-sensitive placement: check Moore-adjacent water for water/sewage treatment; terrain tag for large regionals (hydro / wind). No in-range indicator. Also gate manual placement when `ExpansionFrozen` per Implementation Points §2.

**Exit:**

- `InfrastructureBuildingService.ValidatePlacement(def, x, y)` returns `Valid | InvalidTerrain | InvalidFrozen`; no UI indicator emitted (discover-by-try per locked decision).
- Helper uses Moore-adjacent cell query via `GridManager.GetCell(x, y)` (invariant #5 — service is under `GameManagers/*Service.cs` carve-out, may touch `grid.cellArray` if needed; document reason).
- Manual placement entry point in existing `BuildingPlacementService` + `ZoneManager` placement path checks `DeficitResponseService.ExpansionFrozen` before calling validator.
- EditMode tests: validator returns correct verdict per terrain + freeze state.
- Phase 1 — Service + terrain checks.
- Phase 2 — Freeze-gate integration into placement entry points.
- Phase 3 — Validator EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | InfrastructureBuildingService scaffold | _pending_ | _pending_ | Add `InfrastructureBuildingService.cs` (MonoBehaviour, helper under `GameManagers/*Service.cs` per invariant #5 carve-out). `[SerializeField] private GridManager grid` + `FindObjectOfType` fallback. |
| T6.2 | Terrain validators | _pending_ | _pending_ | Implement per-`TerrainRequirement` check: `AdjacentWater` → any Moore-neighbor `CellData.IsWater`; `Mountain` → neighbor `heightTier >= MountainThreshold`; `OpenTerrain` → no buildings within radius 2. Document `cellArray` touch rationale per carve-out. |
| T6.3 | Freeze-gate wiring | _pending_ | _pending_ | Edit `BuildingPlacementService.cs` + `ZoneManager.cs` manual-placement entry points — early-return `InvalidFrozen` if `DeficitResponseService.ExpansionFrozen`. Cache the service ref in `Awake` (invariant #3). |
| T6.4 | Auto-path freeze-gate | _pending_ | _pending_ | Edit `AutoZoningManager.TrySpawn()` + `AutoRoadBuilder.ExtendRoad()` — check `ExpansionFrozen` before spawn / extend per Implementation Points §2. Single source of truth flag, no ad-hoc checks. |
| T6.5 | Placement EditMode tests | _pending_ | _pending_ | Add `InfrastructureBuildingTests.cs` validator suite — terrain matrix (water / mountain / open / forbidden) × freeze flag on/off. Assert verdict enums. |
