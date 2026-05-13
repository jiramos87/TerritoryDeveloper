# RegionScene Prototype — Exploration Seed

**Status:** Pre-design. Awaiting `/design-explore` grilling session.
**Gate:** Run only after `ui-toolkit-migration` plan ships.
**Source:** Derived from `docs/explorations/assets/city-scene-loading-research.md` Design Expansion — RegionScene + Zoom, corrected 2026-05-13.

---

## Problem Statement

The game currently has only one map view: CityScene, a 64×64 isometric grid where the player manages a city. There is no zoomed-out regional view. The RegionScene is a new map layer that shows the broader region the city sits within — other cities, roads, forests, terrain — at a coarser visual scale. The player needs to be able to navigate, terraform, and eventually found new cities from this view.

RegionScene must feel like a natural extension of CityScene: same isometric camera, same toolbar/HUD layout, same interaction model — but operating at region scale with its own set of tools and cell types.

---

## Known Design Decisions

### Grid

- RegionScene uses a **64×64 grid of region-cells** — same resolution as CityScene.
- Each region-cell is visually larger than a city-cell (exact world-unit size TBD during grilling).
- Region-cell terrain types: **grass, slopes, water slopes** — human-made prefabs (not reused from CityScene).

### City-to-region mapping rule

- **32×32 city-cells = 1 region-cell.**
- A standard 64×64 city therefore occupies **2×2 region-cells**.
- The player's city footprint is anchored at the **(0,0) corner** of its 2×2 region-cell area.
- Player city starts at the **center** of the 64×64 region grid (approximately cells [31–32, 31–32]).
- Future city sizes (non-64×64) use the same 32×32 chunk rule; transformation undefined beyond 64×64 for now.

### Neighbor regions

- The region grid shows **neighbor regions** surrounding the player's region. How these are generated (procedurally, from seed, pre-authored) is an open question.

### Basic UI (same layout as CityScene)

| Tool | Icon | Prefab |
|---|---|---|
| Road tool | Same icon as CityScene | New human-made region-road prefab |
| Forest tool | Same icon as CityScene | New human-made region-forest prefab |
| Bulldozer tool | Same icon as CityScene | — |
| **Found City** | New human-made icon | New human-made prefab |

- Same HUD bar layout as CityScene.
- Same toolbar layout as CityScene.
- Same picker widget as CityScene.
- Mini-map: same style as CityScene mini-map, but rendering RegionScene grid.

### Code structure

- `RegionGridManager` — new hub MonoBehaviour, inspector-attached in `RegionScene.unity`. Structurally mirrors `GridManager` but operates on region-cells.
- New domain services under `Domains/Geography/Services/` for region-cell rendering, region data.
- Hub constraint (invariant #13): existing hubs not renamed/moved/deleted. `RegionGridManager` = new file, new scene.

---

## Open Questions (to be grilled by design-explore)

### Grid + terrain
1. What is the world-unit size of one region-cell vs one city-cell? (Determines camera orthographic size for RegionScene.)
2. Does region terrain have elevation (height values)? Or is RegionScene flat with terrain types only?
3. How are slopes and water slopes oriented? Same isometric rules as CityScene (south+east cliff faces only)?
4. What does the default procedurally generated RegionScene terrain look like? How much water? Forest density?

### City footprint + neighbors
5. How are neighbor regions generated? Procedurally from a seed? Pre-authored stubs? Empty until player founds cities?
6. Do existing `RegionalMap` + `TerritoryData` data structures survive or get replaced by a new region-cell model?
7. What does a neighboring city look like in RegionScene before the player zooms in? A filled 2×2 block? A sprite?

### Tools
8. **Road tool**: What does a region-road connect? Cities? Can roads span across city footprints?
9. **Forest tool**: Plants forest at region-cell scale — does this affect the CityScene when the player zooms in?
10. **Bulldozer**: Removes roads, forests, or terrain features? Can it affect a city footprint cell?
11. **Found City**: Player selects empty region-cell(s) to place a new city. How many cells does founding require? Is there a minimum distance from existing cities? What happens to the region-cell terrain under the new city?

### UI + HUD
12. What stats does the HUD bar show in RegionScene? (CityScene shows population, funds, etc.) Region-level equivalents?
13. Does the picker widget in RegionScene work identically to CityScene? What categories does it show?
14. Does the mini-map auto-generate from the region grid, or does it require a separate render pass?

### Save + load
15. How is RegionScene state saved? Extension of existing `GameSaveData`? Separate save file?
16. When the player saves in CityScene, does the city footprint in RegionScene update automatically?

### Performance
17. 64×64 region-cells at region scale — does the existing `ChunkCullingSystem` pattern apply, or is a new culling strategy needed?

---

## Approaches

*To be developed during `/design-explore docs/explorations/region-scene-prototype.md` session.*

---

## Notes

- This exploration feeds `region-scene-prototype` master plan.
- `city-region-zoom-transition` plan depends on this plan shipping first.
- Approach D (Addressables + Tilemap migration) is deferred and does not block this prototype.
- Date seeded: 2026-05-13.
