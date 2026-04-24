### Stage 8 — Infrastructure buildings + terrain-sensitive placement / Natural wealth adjacency probe

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Water treatment gets a per-adjacent-water-cell production bonus via `WaterBodyAdjacencyProbe`. Forests / mountains stay ambient-only (no pool feed — locked). Sea access bonus flagged for Bucket 4-b / landmarks as TODO-comment link (not implemented here).

**Exit:**

- `WaterBodyAdjacencyProbe.cs` MonoBehaviour helper — given a placed water-treatment building, counts Moore-adjacent `CellData.IsWater` cells, adds a synthetic `IUtilityContributor` (`kind = Water`, rate = cells × perCellBonus) registered via `registry.Register`. Deregistered on demolish.
- Scriptable value `perCellBonus` (e.g. 0.5/tick) tunable on `InfrastructureBuildingDef` extension `waterAdjacencyBonusPerCell`.
- Forests / mountains bonus surface is left untouched (no edits to `ForestManager.cs`; ambient bonus already flows through existing air-quality path).
- TODO comment in `WaterBodyAdjacencyProbe.cs` linking sea-access bonus to Bucket 4-b landmarks doc.
- EditMode tests: 0 / 1 / 4 adjacent water cells → production = 0 / 0.5 / 2.0 per tick.
- Phase 1 — Probe + synthetic contributor.
- Phase 2 — Def field + lifecycle hook.
- Phase 3 — Probe EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | WaterBodyAdjacencyProbe scaffold | _pending_ | _pending_ | Add `Assets/Scripts/Managers/GameManagers/WaterBodyAdjacencyProbe.cs` — helper service (invariant #5 carve-out); `[SerializeField] GridManager grid`. Expose `int CountAdjacentWater(int x, int y)` using 8-neighbor Moore walk via `grid.GetCell`. |
| T8.2 | Synthetic water-bonus contributor | _pending_ | _pending_ | Add nested `WaterAdjacencyBonusContributor : IUtilityContributor` — rate computed at register time from probe count × `perCellBonus`. Registered on water-treatment place, deregistered on demolish. |
| T8.3 | Def bonus field + hook | _pending_ | _pending_ | Extend `InfrastructureBuildingDef` w/ `float waterAdjacencyBonusPerCell` (default 0.5). `InfrastructureBuildingService.Place` calls probe + registers bonus when def has nonzero field + terrainReq is `AdjacentWater`. |
| T8.4 | Probe EditMode tests | _pending_ | _pending_ | Add `WaterBodyAdjacencyProbeTests.cs` — fixture grid w/ water clusters; assert 0/1/4/8 neighbor counts; assert synthetic contributor rate = count × bonus. |
