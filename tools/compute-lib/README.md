# territory-compute-lib

Shared **TypeScript** package for **pure** isometric math and **Zod** schemas consumed by **`tools/mcp-ia-server/`** (see **TECH-37** **§ Completed** — [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md)).

## Authority

**Unity** / **C#** (`GridManager`, scene state) is **authoritative** for gameplay. This library duplicates **only** formulas verified by **golden** tests (and documented in **isometric-geography-system.md** §1.1 / §1.3). On conflict, **C#** wins; update **fixtures** and this package together.

## World ↔ Grid (planar)

- **Forward (grid → world):** `gridToWorldPlanar` — optional `heightLevel` uses the same `(height - 1) * (tileHeight / 2)` vertical offset as `GridManager.GetWorldPositionVector`.
- **Inverse (world → grid):** `worldToGridPlanar` — matches `GridManager.GetGridPosition` (ignores terrain height / sorting pick).

Optional `originX` / `originY` subtract from world inputs before the inverse (add on forward if extended in a later revision).

## Golden tests

Fixture: `test/fixtures/world-to-grid.json`. Run:

```bash
cd tools/compute-lib && npm install && npm test
```

## MCP

The **`isometric_world_to_grid`** **territory-ia** tool validates input with `isometricWorldToGridInputSchema` and calls `worldToGridPlanar`.
