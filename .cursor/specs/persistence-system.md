# Persistence System — Reference Spec

> Deep reference for save/load pipeline, visual restore, and data serialization.
> For sorting order during load, see `isometric-geography-system.md` §7.4. For water persistence, see §11.5.

## Save

`GameSaveData` carries `List<CellData>` + `WaterMapData` (from `WaterMap.GetSerializableData()`). `WaterMapData` is a nested type inside `WaterMap.cs`.

**Interchange JSON** (tools, MCP, StreamingAssets config) is separate from Save data: schemas and DTOs use `artifact` / `schema_version` per `docs/schemas/README.md`. Do not treat `tools/reports/*-interchange-*.json` or `world_snapshot_dev` exports as Load pipeline inputs unless a dedicated issue defines migration (see `ARCHITECTURE.md` — Interchange JSON).

## Load pipeline

Restore order matters — do not reorder:

1. Restore heightmap
2. Restore water map (or legacy path when `waterMapData` is absent)
3. Restore grid cells
4. Sync water body ids with shore membership

Load does **NOT** run global slope restoration or sorting recalculation. Snapshot applies saved prefabs and `sortingOrder` directly.

## Visual restore details

- **Phase order** (`SortCellDataForVisualRestore`): water → grass/shore/slope → RCI overlays → **street**/**interstate** prefabs → building pivots → multi-cell non-pivots.
- **Building sort post-pass:** re-runs building sorting on each pivot after full grid restore.
- **Grass removal on place/restore:** `DestroyCellChildren(..., destroyFlatGrass: true)` when placing/restoring RCI and utility buildings.
- **Legacy saves:** saves without `waterMapData` are still supported via fallback path.

## Key files

| File | Role |
|------|------|
| `GameSaveManager.cs` | Serialize/deserialize orchestrator |
| `GameManager.cs` | Entry point, load flow |
| `GameBootstrap.cs` | Game loading bootstrap |
| `CellData.cs` | Serializable cell data |
| `WaterMap.cs` | Contains `WaterMapData` nested type |
