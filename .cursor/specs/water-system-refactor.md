# Water System Refactor — Technical Overview

> **Backlog:** [FEAT-37](../../BACKLOG.md) (Medium priority) · **Planning pass:** [TECH-12](../../BACKLOG.md) (define objectives, rules, bugs, scope, child issues before implementation)  
> **Status:** Draft — design and phased delivery TBD  
> **Related:** [BUG-08](../../BACKLOG.md) (generation polish), [FEAT-15](../../BACKLOG.md) (ports / sea), bridge specs (e.g. `.cursor/specs/bridge-and-junction-fixes.md`)

## 1. Problem synthesis

Today, water is effectively tied to a **single global water level** (conceptually “height 0”): procedural bodies and the drawing tool place water as a **flat surface** at that level. Terrain around it uses the heightmap and slopes, so **lakes often read as deep pits** with the water plane at the bottom, dark vertical gaps, and weak embankment visuals. Raising the apparent water surface by even **one height step** would already improve readability; the real goal is larger: **water should be modeled as water masses hosted by terrain**—i.e. persistence of water where the terrain allows—**not** as a universal Z-plane independent of local elevation.

This is a **major feature epic** with a **large refactor** of `WaterManager`, `WaterMap`, cell/water data, rendering/sorting, and downstream systems (roads/bridges, zoning/buildings near water, forests, demand/services, save/load).

## 2. Goals

1. **Per-body or per-cell water surface elevation** — Water bodies can exist at **multiple height levels**, aligned with local terrain so lakes sit naturally in bowls or on plateaus without mandatory “bottomless pit” framing.
2. **Unify the mental model** — All water sources (procedural seas/lakes/rivers, player-painted water) are **the same abstraction**: stored water in cells that the terrain can **hold** (volume + surface height), not a special global layer only at y=0.
3. **Geological variety (conceptual, can be phased)** — Support distinguishing, in data and eventually gameplay/visuals:
   - **Cliffs and deep sinks** vs **shallow basins**
   - **High-altitude lakes** (mountain / mesa)
   - **Directed flow** — Rivers from higher to lower terrain (gradient-based paths)
   - **Coastal / sea** regions with **tide direction** (or dominant swell), distinct from inland still water
4. **Reduce coupling** — Keep `GridManager` thin; extend or add helpers (`WaterMap`, future `WaterSurfaceService` or similar) per project rules.

## 3. Current architecture touchpoints

| Area | Role today |
|------|------------|
| `GeographyManager.InitializeGeography()` | Order: terrain → **water** → forests → grid … |
| `WaterManager` / `WaterMap` | Generation, placement, likely assumptions about flat water height |
| `TerrainManager` / `HeightMap` | Elevations and slopes; water must **agree** with these per cell |
| `GridManager` | Cell visuals, sorting; water cells interact with terrain and overlays |
| `Cell` / `CellData` | Serialization; any new water fields must round-trip in save/load |
| `ZoneManager`, `ForestManager`, `RoadManager` | Adjacency rules, bridges, future slope water |

Initialization and dependency overview: see root `ARCHITECTURE.md` (Terrain layer, Geography flow).

## 4. Non-goals (initial draft)

- Final art for every water type (may reuse flat tiles at first).
- Full fluid simulation (Navier–Stokes); **direction** and **height** can be **data-driven** first.
- Shipping all sub-features in one PR; this should be **incremental**.

## 5. Suggested phases (for planning only)

Phases are **not** committed ordering until a dedicated design pass; they split risk.

| Phase | Theme | Outcomes (examples) |
|-------|--------|---------------------|
| **A** | Data model | Water surface height per body or per cell in `WaterMap` / cell flags; migration path for saves |
| **B** | Visual / sorting | Correct draw order vs terrain at same cell height; fewer “void” artifacts at lake edges |
| **C** | Tools & generation | Procedural + paint tool respect new model; optional default level above legacy “0” for readability |
| **D** | Flow & coast | River graph or flow field; sea edge + tide **direction** as data (animation later) |
| **E** | Gameplay integration | Bridges, buildings on/adjacent water, forests — align rules with new model |

**Slope water** (water following or crossing sloped terrain) is **high complexity**—likely late phase, may share logic with road/terrain slope handling and sorting.

## 6. Risks and open questions

- **Save compatibility:** Version `CellData` or water chunk format when adding height/body IDs.
- **Performance:** Larger water graphs (flow) may need caching; avoid per-frame `FindObjectOfType`.
- **Sorting:** Water at arbitrary heights must stay consistent with `GridSortingOrderService` and multi-cell buildings.
- **Bridges / interstate:** Overlap with road prefab and terraform validation — coordinate with `RoadManager` / bridge specs.

## 7. Documentation maintenance

**Before** large implementation (see **TECH-12** in [BACKLOG.md](../../BACKLOG.md)): lock **objectives**, **rules**, **in-scope bugs**, **non-goals**, and **phased child issues** here and in **FEAT-37**.

When implementation starts:

- Update this spec with **decided** data structures and **public APIs**.
- Update `ARCHITECTURE.md` (Terrain layer / Geography bullets) if initialization order or dependencies change.
- Add a row under `AGENTS.md` “What to Read” if the primary entry point shifts from “only `WaterManager`” to a new helper.

## 8. References

- `ARCHITECTURE.md` — Initialization, `WaterManager` dependencies  
- `BACKLOG.md` — **FEAT-37**, **BUG-08**, **FEAT-15**  
- `.cursor/rules/managers-guide.mdc` — Manager responsibilities  
- Bridge / junction context: `.cursor/specs/bridge-and-junction-fixes.md`
