---
name: debug-geography-water
purpose: >-
  Agent-led diagnosis of CityScene terrain / water-body / shore-prefab bugs using
  unity_export_cell_chunk v2 payload (heightmap + body inventory + WaterMap-authoritative
  flags) cross-referenced against shore decision tree in TerrainManager.Impl.cs.
audience: agent
loaded_by: "skill:debug-geography-water"
slices_via: spec_section
description: >-
  Debug geography / water-body / shore-prefab issues using IDE agent bridge cell-chunk
  export (v2 payload — bodies[] + waterMapBodyId + waterMapIsWater) and rule_content
  unity-invariants. Triggers: "water shore wrong", "elevated water tile", "orphan water cell",
  "thin land strip", "shore prefab corner bug", "lake gen produced 1-cell strip", "compare
  CityCell vs WaterMap", "body surface height mismatch". Prerequisites: DATABASE_URL,
  migration 0008, Unity Editor on REPO_ROOT, db:bridge-preflight green, Play Mode + GridManager
  ready.
phases: []
triggers:
  - water shore wrong
  - elevated water tile
  - orphan water cell
  - thin land strip
  - shore prefab corner bug
  - lake gen produced 1-cell strip
  - compare CityCell vs WaterMap
  - body surface height mismatch
model: inherit
tools_role: custom
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# Debug geography / water — cell_chunk v2 + shore decision tree

Use when a CityScene shows wrong shore prefab, elevated water tiles, orphan water cells, or 1-cell-wide land strips between same-surface bodies. The cell_chunk v2 payload exposes the heightmap raster + per-body surface heights + WaterMap-authoritative water flags in a single bridge call — enough to pinpoint the buggy cell + trace the decision path through `TerrainManager.Impl.cs::DetermineWaterShorePrefabs` and `WaterManager.IsOpenWaterForShoreTopology` without source-level guesses.

**Related:** [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) (generic bridge env) · [`debug-sorting-order`](../debug-sorting-order/SKILL.md) (sister recipe — same export-then-analyse pattern, different domain) · [`close-dev-loop`](../close-dev-loop/SKILL.md) (Play Mode before/after evidence).

## Prerequisites

| Requirement | Notes |
|---|---|
| `DATABASE_URL` or `config/postgres-dev.json` | Bridge job queue + cell_chunk Postgres row destination |
| Migration `0008_agent_bridge_job.sql` | `npm run db:migrate` |
| Unity Editor on `REPO_ROOT` | `AgentBridgeCommandRunner` dequeue |
| Editor in Play Mode + `GridManager.isInitialized` | `unity_bridge_command kind=enter_play_mode` first; export rejects Edit Mode with `requires Play Mode` |
| `npm run db:bridge-preflight` exit 0 | Catches missing migration / TCP unreachable before bridge call |
| Latest assembly loaded | If Editor is in Play Mode with stale C#, exit + recompile before re-entering |

## Cell chunk v2 payload (Postgres row → `editor_export_terrain_cell_chunk.document`)

```jsonc
{
  "artifact": "terrain_cell_chunk",
  "schema_version": 2,
  "origin_x": 0, "origin_y": 0,
  "width": 32, "height": 32,
  "cells": [
    {
      "x": 18, "y": 7,
      "height": 1,                   // HeightMap.GetHeight authoritative
      "prefabName": "north-slope-water-prefab",
      "waterBodyType": "None",       // CityCell.waterBodyType (cached, may be stale)
      "waterBodyId": 3,              // CityCell.waterBodyId (cached, shore affiliation for land cells)
      "waterMapBodyId": 0,           // WaterMap.GetWaterBodyId — authoritative
      "waterMapIsWater": false       // WaterMap.IsWater — authoritative
    }
  ],
  "bodies": [
    { "bodyId": 3, "classification": "Lake",  "surfaceHeight": 0, "cellCount": 17 },
    { "bodyId": 11, "classification": "River", "surfaceHeight": 0, "cellCount": 33 }
  ]
}
```

**Authority hierarchy** (top is ground truth):
1. `cells[i].height` — `HeightMap.GetHeight(x, y)` — the terrain bed.
2. `cells[i].waterMapIsWater` / `cells[i].waterMapBodyId` — `WaterMap` — what `DetermineWaterShorePrefabs` and `WaterManager.IsOpenWaterForShoreTopology` actually read at decision time.
3. `bodies[]` — `WaterMap.GetBodies()` — body-level facts (`SurfaceHeight`, `Classification`, `CellCount`).
4. `cells[i].waterBodyType` / `cells[i].waterBodyId` — `CityCell` cached fields — may drift behind WaterMap (orphan cells are cells where these disagree with `waterMap*` fields).

## Phase 0 — bridge preflight + Play Mode

```bash
npm run db:bridge-preflight       # exit 0 required
# unity_bridge_command kind=get_play_mode_status — confirm play_mode_ready + grid_width/height
# unity_bridge_command kind=enter_play_mode if play_mode_state != play_mode_ready
```

Guardrail: re-entering Play Mode regenerates the scene (procedural seed). If you need to keep the buggy scene state across the next debug pass, **do not** call `exit_play_mode` until you've extracted the cell chunk.

## Phase 1 — export

```jsonc
unity_export_cell_chunk({
  "origin_x": 0, "origin_y": 0,
  "chunk_width": 32, "chunk_height": 32,
  "timeout_ms": 60000,
  "agent_id": "geo-water-debug"
})
```

Returns `postgres_only: true` — payload lands in `editor_export_terrain_cell_chunk`. Pull latest row:

```bash
PGPASSWORD=postgres psql -h localhost -p 5434 -U postgres -d territory_ia_dev \
  -t -A -c "SELECT document::text FROM editor_export_terrain_cell_chunk ORDER BY id DESC LIMIT 1;" \
  > /tmp/water-chunk.json
```

## Phase 2 — render heightmap + zone overlay

ASCII renderer pattern (run via `node -e`):

```js
const j = JSON.parse(require("fs").readFileSync("/tmp/water-chunk.json", "utf8"));
const grid = Array.from({length: j.height}, () => Array(j.width).fill(null));
for (const c of j.cells) grid[c.y][c.x] = c;
// Heightmap rendered with y descending so screen top = high y.
for (let y = j.height - 1; y >= 0; y--) {
  let row = `y${String(y).padStart(2)}: `;
  for (let x = 0; x < j.width; x++) row += grid[y][x].height + " ";
  console.log(row);
}
// Zone overlay — W{bodyId} for water, L for land, h for shore prefab carrier.
for (let y = j.height - 1; y >= 0; y--) {
  let row = `y${String(y).padStart(2)}: `;
  for (let x = 0; x < j.width; x++) {
    const c = grid[y][x];
    if (c.waterMapIsWater) row += `W${c.waterMapBodyId}`.padEnd(4);
    else if (/slope-water|bay-slope|upslope/.test(c.prefabName || "")) row += "h   ";
    else row += "L   ";
  }
  console.log(row);
}
```

## Phase 3 — body inventory check

```js
for (const b of j.bodies) console.log(`W${b.bodyId} ${b.classification} surface=${b.surfaceHeight} cellCount=${b.cellCount}`);
```

Red flags:
- Two bodies with same `surfaceHeight` adjacent in the grid → candidate for `RunMergeAdjacentBodiesWithSameSurface` or `CollapseOneCellLandStripsBetweenSameSurfaceBodies` (post-2026-05-15 pass) to merge — bug if they haven't merged.
- A body with `cellCount: 0` → leaked body entry, should be removed by `RemoveCellFromBody` cleanup.
- `surfaceHeight` ≠ 0 for `classification: "Sea"` → invariant violation (Sea bodies live at `WaterManager.seaLevel`).

## Phase 4 — orphan-cell scan

```js
const orphans = j.cells.filter(c =>
  (c.waterBodyType !== "None" || (c.waterBodyId > 0 && /water1-/.test(c.prefabName || ""))) &&
  !c.waterMapIsWater
);
for (const c of orphans) console.log(`ORPHAN (${c.x},${c.y}) cellSays=${c.waterBodyType}/${c.waterBodyId} prefab=${c.prefabName} waterMapSays=land`);
```

If any → upstream demotion (`ApplyLakeHighToRiverLowContactFallback`, `JunctionMerge` reassignment, `ProceduralRiverGenerator` truncation) cleared the WaterMap entry without restoring the CityCell visual. Look for the elevated-water-tile bug pattern (orphan rendered at fallback seaLevel-1, clipping below terrain). Fixed in `WaterManager.Visuals.cs::CleanupOrphanWaterCell` (2026-05-15).

## Phase 5 — multi-body junction scan (shore-prefab decisions)

```js
const dir = {N:[1,0], S:[-1,0], E:[0,-1], W:[0,1]};
const junctions = [];
for (const c of j.cells) {
  if (c.waterMapIsWater || c.waterBodyId === 0) continue;
  const owner = c.waterBodyId;
  for (const [name, [dx, dy]] of Object.entries(dir)) {
    const n = grid[c.y + dy]?.[c.x + dx];
    if (!n || !n.waterMapIsWater) continue;
    if (n.waterMapBodyId !== owner) junctions.push({x:c.x, y:c.y, owner, dir:name, otherId:n.waterMapBodyId});
  }
}
```

Cross-body cardinals where same-`surfaceHeight` bodies meet → these cells SHOULD see all adjacent water as one continuous mass for shore-prefab purposes. Decision tree in `TerrainManager.Impl.cs::DetermineWaterShorePrefabs` calls `WaterManager.IsOpenWaterForShoreTopology` which (post-2026-05-15) returns true for any neighbor body sharing `SurfaceHeight` with the owner body. Different-surface neighbors (cascade) stay excluded — `IsMultiSurfacePerpendicularWaterCorner` handles those upstream.

## Phase 6 — shore decision tree (read-only navigation)

When a cell's chosen prefab disagrees with expected geometry, trace through `TerrainManager.Impl.cs::DetermineWaterShorePrefabs` in this order (post-2026-05-15 routing):

1. Border short-circuit (`isAtSouthBorder` / `isAtNorthBorder` / `isAtWestBorder` / `isAtEastBorder`).
2. `ShouldPlaceShoreEnd` (cascade/junction lower-pool cap — diagonal `*SlopeWaterPrefab`).
3. `IsMultiSurfacePerpendicularWaterCorner` per quadrant (different-surface bodies).
4. `TrySelectShoreForExactlyThreeCardinalWaters` (3 cardinals water, diagonal tie-break).
5. Perpendicular pairs (S+E, S+W, N+E, N+W) → `SelectPerpendicularWaterCornerPrefabs` (Bay vs SlopeWater vs Upslope — covers axis-aligned rectangle corner check + multi-cell diagonal land edge detection).
6. Single-cardinal branches (E, W, N, S) with upslope variants for N+E/N+W/S+E/S+W secondary pair.
7. Diagonal-only branches (`BuildDiagonalOnlyShorePrefabs`) — only when no cardinal water present.

`PatternWater(nx, ny)` lookup inside `DetermineWaterShorePrefabs` calls `WaterManager.NeighborMatchesShoreOwnerForJunctionTopology` (wraps `IsOpenWaterForShoreTopology`). To debug "why didn't branch X fire", inspect:
- `cells[i].waterBodyId` (shore affiliation) — drives PatternWater filtering.
- Adjacent body surface heights from `bodies[]` — same-surface neighbors count as water (per fix); different-surface do not.

## Phase 7 — verify the fix

After source change, exit + re-enter Play Mode (regenerates scene) → re-export → diff body inventory + orphan count + junction list. Same-seed comparison not available (procedural seed reshuffles per Play Mode entry); confirm structural invariants instead:
- Orphan count = 0.
- No two same-surface mergeable bodies separated by exactly 1 land cell (the `CollapseOneCellLandStripsBetweenSameSurfaceBodies` post-pass).
- Multi-body junctions render corner-slope prefabs (not pure-cardinal) where expected.

## Checklist

| Check | Pass | Note |
|---|---|---|
| `npm run db:bridge-preflight` exit 0 | | |
| Editor in `play_mode_ready` + `GridManager.isInitialized` | | |
| `unity_export_cell_chunk` returns `postgres_only: true` | | |
| Latest `editor_export_terrain_cell_chunk` row has `document->>'schema_version' = '2'` | | |
| Orphan-cell scan = 0 (no `cells[].waterMapIsWater=false` AND `cells[].waterBodyType ≠ "None"`) | | |
| Body inventory: every `Sea` body has `surfaceHeight = 0` | | |
| No 1-cell-wide land strip between same-surface mergeable bodies | | |
| Shore prefab at user-reported buggy cell matches phase-6 trace | | |

## MCP slice usage

| Tool | Role |
|---|---|
| `unity_bridge_command` | Enter Play Mode + status checks |
| `unity_export_cell_chunk` | Single-call full-map dump (chunk up to 128×128) |
| `unity_compile` | Compile snapshot if Editor sat on stale assembly |
| `rule_content unity-invariants` | Heightmap ↔ CityCell.height parity guardrails |
| `spec_section architecture/data-flows` | Init order: lake fill → merge → river gen → merge → strip collapse → UpdateWaterVisuals |
| `csharp_class_summary WaterMap` | Public surface of WaterMap (bodies, IsWater, GetBodies) |
| `csharp_class_summary TerrainManager` | Public surface of shore + cascade APIs |

## One-line agent flow

Preflight → enter_play_mode (or assert play_mode_ready) → unity_export_cell_chunk (full map) → psql latest row → node ASCII render → body inventory + orphan + junction scans → trace phase-6 → fix → exit + re-enter Play Mode → re-export → diff.

## Changelog

- 2026-05-15 — initial. Captures methodology used to fix elevated-water-tile orphan (`WaterManager.Visuals.cs::CleanupOrphanWaterCell`), shore-affiliation same-surface unification (`WaterManager.Membership.cs::IsOpenWaterForShoreTopology`), and 1-cell-strip collapse (`WaterMap.cs::CollapseOneCellLandStripsBetweenSameSurfaceBodies`). Cell chunk payload upgraded v1 → v2 (`InterchangeJsonReportsMenu.BuildCellChunkInterchangeJsonString`) to expose `bodies[]` + `cells[].waterMap*` flags.
