# Territory.Utilities.Compute

**Pure** static helpers for isometric grid math (**no** `MonoBehaviour`). **TECH-37** / **TECH-36** program.

- **Authority:** Scene state and **cell** reads remain on **`Territory.Core.GridManager`** — use **`GetCell(x, y)`** (do not bypass **`gridArray`** / **`cellArray`** per **invariants**).
- **Parity:** **`IsometricGridMath`** mirrors **`GridManager`** planar conversion and **`tools/compute-lib`** golden fixtures; keep formulas in sync when **`tileWidth`** / **`tileHeight`** conventions change.

See **`isometric-geography-system.md`** §1.1 / §1.3 and **`tools/compute-lib/README.md`**.
