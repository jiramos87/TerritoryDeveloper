# Water & Terrain System — Reference Spec

> Deep reference for heightmap, slopes, water bodies, cliffs, shores, and cascades.
> Canonical detail lives in `isometric-geography-system.md` — this spec provides navigational context and key rules.

For shared vocabulary (**map border**, **open water**, **shore band**, **interstate** vs **street**, **road validation pipeline**), see [glossary.md](glossary.md) and geo §13–§14.5.

## Height model (geography spec §2)

- `HeightMap[x,y]` is the authoritative terrain elevation.
- `Cell.height` must always match `HeightMap[x,y]` — sync both on every write (§2.4).
- Water cells: `Cell.height = S` (surface height) for visual placement; `H_bed` is the physical bed.

## Slopes (spec §3–§4)

- `TerrainSlopeType` determined by comparing neighbor heights.
- Shore slope eligibility has additional constraints (§4.2): `h ≤ V + MAX` where `V = max(MIN_HEIGHT, S−1)`.

## Layered visual model (spec §5.6–§5.9)

| Layer | Description |
|-------|------------|
| Open water | Base water tile at surface height |
| Water-shore art | Shore prefabs on land cells adjacent to water |
| Cliff stacks | Vertical walls on south + east faces (N/W not instantiated) |
| Cascades | Waterfall visuals between distinct water surfaces on cardinal edges |

### Cliff rules
- Only **south and east** visible faces are instantiated (§5.7).
- Cliff stacks use serialized face nudges + water-shore Y adjustments.

### Cascade rules
- Only between **distinct** logical surfaces (§5.6.2).
- Lake surface-step contact is forbidden — lake edges at different S skip cascades (§11.7).

## Water map and lakes (spec §11)

- `WaterMap` stores per-cell body ids; `WaterBody` holds surface height.
- Lake generation via depression-fill algorithm.
- Multi-body junctions: Pass A → Pass B → lake-river fallback → place water → cascade cliffs → shore terrain (§11.7).

## Procedural rivers (spec §12)

- Generated after lakes, before interstate.
- `H_bed` monotonically non-increasing toward exit (§12.4).
- Symmetric banks: `H_bank = H_bed + 1`.
- Corner promotion for shore continuity (§12.5).

## Shore band constraint (spec §2.4.1)

Land cells Moore-adjacent to water must have `height ≤ min(S)` of adjacent water cells.

## Key files

| File | Role |
|------|------|
| `TerrainManager.cs` | Heightmap generation, slopes, cliff stacks |
| `WaterManager.cs` | Water body generation, shore/cliff/cascade visuals |
| `WaterMap.cs` | Per-cell water body storage |
| `HeightMap.cs` | Heightmap data structure |
| `GeographyManager.cs` | Initialization orchestrator |
| `WaterBody.cs` | Water body data class |
| `CliffFace.cs` | Cliff face visual component |
| `LakeFeasibility.cs` | Lake generation feasibility checks |
| `ProceduralRiver*.cs` | River generation |
