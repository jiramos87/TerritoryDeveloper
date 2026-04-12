# Territory.Utilities.Compute

**Pure** static helpers (**no** `MonoBehaviour`). **TECH-37**/**TECH-39** archived ([`BACKLOG-ARCHIVE.md`](../../../../BACKLOG-ARCHIVE.md)); **TECH-38** open on [`BACKLOG.md`](../../../../BACKLOG.md) **§ Compute-lib program**.

- **`IsometricGridMath`** — planar **World ↔ Grid** ( **`GridManager.GetGridPosition`** / **`GetWorldPositionVector`** delegate here); golden parity with **`tools/compute-lib/test/fixtures/world-to-grid.json`**.
- **`UrbanGrowthRingMath`** — **urban growth ring** bands vs centroid + radius (single pole); **`ClassifyRingMultipolar`** for minimum distance to multiple poles (**FEAT-47** direction). **`UrbanMetrics`** delegates to this class.
- **`GridDistanceMath`** — **Chebyshev** / Manhattan on integer cells (previews / future **`grid_distance`** MCP); not **geo** §10 path costs.

- **Authority:** Scene state and **cell** reads remain on **`Territory.Core.GridManager`** — use **`GetCell(x, y)`** (do not bypass **`gridArray`** / **`cellArray`** per **invariants**).

See **`isometric-geography-system.md`** §1.1 / §1.3, **simulation-system** §Rings, and **`tools/compute-lib/README.md`**.
