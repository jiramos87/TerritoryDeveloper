# Plan — BUG-42: Water shores, cliffs, neighbor refresh, lake fallback

> **Backlog:** [BUG-42](../BACKLOG.md)  
> **Specs:** [.cursor/specs/isometric-geography-system.md](../.cursor/specs/isometric-geography-system.md), [.cursor/specs/bugs/cliff-water-shore-sorting.md](../.cursor/specs/bugs/cliff-water-shore-sorting.md), [.cursor/specs/rivers.md](../.cursor/specs/rivers.md), [.cursor/specs/water-system-refactor.md](../.cursor/specs/water-system-refactor.md)

## Goal

Eliminate black voids and broken edges at lake and river boundaries by ensuring:

1. **Height data** stays consistent: `HeightMap` ↔ `Cell.height` whenever procedural water or terraform runs.
2. **Cliffs** are instantiated wherever a land cell borders lower terrain (subject to existing suppression rules so we do not duplicate water-shore ramps on the same face).
3. **Lake fallback** terraforming produces **coherent** rim and corner heights relative to **lake surface**, including **border corner cells** (concave/convex) that must sit at **lake surface height**, not “stuck” at an elevated neighbor’s terrain height.
4. **Refresh scope** after lake-shore updates: **minimal audit** — update the shore cell **and** only the **single land neighbor outward** from the water body (opposite direction from water), unless a concrete bug proves a wider halo is needed.

**Out of scope for this plan (defer):** **North/west cliff prefabs** for interior cells; **map-border** N/W visibility improvements are a future issue.

## Principles (locked for this workstream)

- **`HeightMap` is authoritative** at the start of river/lake generation; any terraform during fallback **must** update `HeightMap` and matching `Cell` data together.
- **Water surface height** for a body/segment: terrain height hosting that water at that location after coherent terraform (aligned with existing `WaterBody` / placement rules).
- **No two independent cliff systems on the same cardinal face:** either the face is expressed by **water-shore prefab geometry** (including built-in cliff faces on the asset) **or** by **`PlaceCliffWallStack`**, not both conflicting on the same edge. (Shore cells may still have cliff segments on **other** faces.)

## Phases

### Phase 1 — Inventory and trace

- Map every code path that **modifies terrain height** or **places/refreshes water** for lakes and rivers: `TerrainManager`, `WaterManager`, `GeographyManager`, `ProceduralRiverGenerator`, lake fallback / `LakeFeasibility` / depression-fill hooks.
- For each path, document whether it calls **`UpdateTileElevation`** (or equivalent) on **affected cells** and **whether neighbors** get refreshed.
- Reproduce BUG-42 with **`[TerrainDebug]`** on instrumented cells (`TerrainManager` terrain debug cells) and save **before/after** child lists (Grass vs Cliff vs shore).

### Phase 2 — Height consistency (fallback + procedural)

- Audit **lake fallback** terraform: list all cells whose height is written; verify **corner border cells** end at **lake surface** where the design requires continuity (fix off-by-one or “inherited from neighbor” bugs).
- Ensure **river** carve/terraform paths update **`HeightMap` and `Cell.height`** in the same transactions as documented in `rivers.md` (§4.3–4.4).
- Add or tighten **assertions in editor/debug** (optional): mismatch between `heightMap` and `Cell.height` logs a single `[TerrainDebug]` line.

### Phase 3 — Cliff instantiation after any terrain mutation

- Define a single internal contract: **after** height changes in a region, **cliff placement** runs for every **high cell** toward **lower cardinals** that require vertical faces, respecting:
  - `ShouldSuppressCliffFaceTowardLowerCell` (no duplicate ramp + cliff on same logical drop),
  - underwater cull and BUG-40 sorting caps,
  - water-shore eligibility vs rim (`DetermineWaterShorePrefabs` / surface gate).
- Ensure **river** and **lake** refresh entry points invoke this contract (not only “happy path” initial map gen).

### Phase 4 — Lake shore refresh and minimal neighbor audit

- Implement or extend **`RefreshLakeShore`** so that for each affected shore cell:
  1. Recompute shore prefabs and cliffs for that cell.
  2. **Audit exactly one outward land neighbor** (away from water along the relevant cardinal for that shore segment), and recompute its terrain/cliffs so rim adjacency stays valid.
- Validate against **Bay**, **cardinal shore**, and **corner** patterns; widen halo only if a case fails with evidence.

### Phase 5 — Visual follow-ups (BUG-42 backlog scope)

- **Waterfall / cascade** prefabs where **Δh** and flow direction require them (per BACKLOG).
- **Water-cliff** wall variants on S/E where art must read as water-facing.
- Regression pass on **lake + river adjacency** at **same surface height**; document or enforce generator constraint so **different-surface** adjacency is rare or handled by waterfall assets.

### Phase 6 — Documentation

- Keep **`isometric-geography-system.md`** aligned with implemented behavior (§2.4–2.5, §5.6.1, §5.7).
- Update **`cliff-water-shore-sorting.md`**: mark resolved vs open after implementation.

## Verification

- New game: N runs with lakes-only, rivers-only, lake+river, forced fallback lake.
- Hierarchy spot-check: cells at drops show **Cliff** children where expected (no black voids).
- No systematic z-fighting; spot-check BUG-40 caps still hold.

## Risks

- Tightening refresh scope to **one** outward neighbor might miss an edge case; keep a feature flag or one-line debug to expand to 8-neighbor temporarily.
- Terraform order: fixing height without reordering **PlaceWaterShore** vs **PlaceCliffWalls** can reintroduce SS1 issues; follow single pipeline order per cell.

**Last updated:** 2026-03-25
